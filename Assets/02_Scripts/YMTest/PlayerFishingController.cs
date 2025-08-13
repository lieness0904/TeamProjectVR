using UnityEngine;
using Fusion;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using TMPro;
using UnityEngine.UI;

[RequireComponent(typeof(RiggingManager))]
public class PlayerFishingController : NetworkBehaviour
{
    #region 변수 선언
    [Header("프리팹 연결")]
    [SerializeField] private NetworkObject fishingRodPrefab;
    [SerializeField] private NetworkObject bobberPrefab;

    [Header("컴포넌트 연결")]
    [SerializeField] private CastingHandler castingHandler;

    [Header("진동 설정")]
    [SerializeField] private float vibrationAmplitude = 0.7f;
    [SerializeField] private float vibrationDuration = 1.0f;

    [Header("릴 설정")]
    [Tooltip("찌가 이 거리 안으로 들어오면 회수된 것으로 간주합니다.")]
    [SerializeField] private float retrievalDistance = 1.5f;

    [Header("낚시 디테일 설정")]
    [Tooltip("낚싯대에 찌가 매달릴 때의 기본 거리입니다.")]
    [SerializeField] private float bobberHangOffset = 0.2f;

    [Header("챔질(Hooking) 설정")]
    [Tooltip("이 속도를 넘는 컨트롤러 움직임을 챔질로 인식합니다.")]
    [SerializeField] private float hookVelocityThreshold = 3.0f;

    [Header("게이지 설정")]
    [SerializeField] private float gaugeDecreasePerSec = 50f; // 초당 자연 감소량

    [Header("잡은 물고기 설정")]
    [Tooltip("잡은 후 보여줄 때 물고기가 천천히 회전하는 속도")]
    [SerializeField] private float caughtFishRotationSpeed = 50f;

    [Header("낚시 실패 조건")]
    [Tooltip("물고기가 이 거리 이상 멀어지면 낚시에 실패합니다.")]
    [SerializeField] private float maxFishDistance = 30f;

    [Header("게이지 진동 설정")]
    [Tooltip("왼손 컨트롤러의 진동 세기 (낮음)")]
    [Range(0, 1)] public float lowTensionVibeAmplitude = 0.2f;
    [Tooltip("왼손 컨트롤러의 진동 세기 (중간)")]
    [Range(0, 1)] public float midTensionVibeAmplitude = 0.4f;
    [Tooltip("왼손 컨트롤러의 진동 세기 (높음)")]
    [Range(0, 1)] public float highTensionVibeAmplitude = 0.7f;
    [Tooltip("진동 시간")]
    [Range(0, 1)] public float vibeDuration = 0.1f;

    // --- 네트워크 동기화 변수 ---
    [Networked] private float tensionGauge { get; set; } = 0f;
    private const float maxGauge = 100f;
    private bool isReeling = false;

    [Networked] public NetworkBool IsFishing { get; set; }
    [Networked] private NetworkObject SpawnedRod { get; set; }
    [Networked] public NetworkObject CurrentBobber { get; private set; }
    [Networked] private TickTimer CastingCooldown { get; set; }

    [Networked] public NetworkBool IsFighting { get; set; }
    [Networked] public NetworkObject HookedFish { get; set; }

    [Networked] private Vector3 FleeDirection { get; set; }
    [Networked] private TickTimer FleeStateTimer { get; set; }
    [Networked] private NetworkBool IsCurrentlyFleeing { get; set; }

    [Networked] private TickTimer DistanceDisplayDelayTimer { get; set; }

    // --- 로컬 변수 (클라이언트측에서만 사용) ---
    private bool _missWindowActive = false;
    private float _missWindowEndTime = 0f;
    private RiggingManager _riggingManager;
    public static PlayerFishingController Local { get; private set; }
    private XRBaseController _rightHandXRController;
    private bool _fishJustBit = false;
    private Transform _rodTip;
    private Rigidbody _rodTipRb;
    private Vector3 _lastControllerPos;
    private Vector3 _currentControllerVel;
    private XRBaseController _leftHandXRController;
    private float _hapticTimer = 0f;

    private PlayerInventory playerInventory;
    #endregion

    private void Awake()
    {
        _riggingManager = GetComponent<RiggingManager>();
        if (_riggingManager != null)
        {
            if (_riggingManager.rightHandController != null)
            {
                _rightHandXRController = _riggingManager.rightHandController.GetComponent<XRBaseController>();
            }
            if (_riggingManager.leftHandController != null)
            {
                _leftHandXRController = _riggingManager.leftHandController.GetComponent<XRBaseController>();
            }
            else
            {
                Debug.LogWarning("RiggingManager에 leftHandController가 연결되지 않아 릴링 진동이 작동하지 않을 수 있습니다.");
            }
        }
    }

    private void Update()
    {
        if (!HasInputAuthority) return;

        // 1. 오른쪽 컨트롤러의 현재 속도를 계속 계산합니다. (챔질 판정용)
        if (_riggingManager.rightHandController != null)
        {
            Vector3 currentPos = _riggingManager.rightHandController.position;
            if (Time.deltaTime > 0)
            {
                _currentControllerVel = (currentPos - _lastControllerPos) / Time.deltaTime;
            }
            _lastControllerPos = currentPos;
        }

        // --- [수정된 구조] ---
        // BobberController 컴포넌트를 맨 위에서 한 번만 가져와서 재사용합니다.
        BobberController bobber = null;
        if (CurrentBobber != null)
        {
            CurrentBobber.TryGetComponent<BobberController>(out bobber);
        }

        // bobber가 성공적으로 찾아졌을 때만 관련 로직을 실행합니다.
        if (bobber != null)
        {
            // 2. MISS 판정 로직
            if (_missWindowActive && Time.time <= _missWindowEndTime)
            {
                if (!bobber.HasFishOn && !IsFighting)
                {
                    if (_currentControllerVel.y > hookVelocityThreshold)
                    {
                        bobber.ShowMessageText("MISS", 1.2f);
                        _missWindowActive = false;
                    }
                }
            }

            // 3. 챔질(HIT) 판정 로직
            if (bobber.HasFishOn && !IsFighting)
            {
                if (_currentControllerVel.y > hookVelocityThreshold)
                {
                    if (bobber.HookedFish != null)
                    {
                        RPC_AttemptHook(bobber.HookedFish);
                    }
                }
            }
        }
        // --- [여기까지 수정] ---

        if (_missWindowActive && Time.time > _missWindowEndTime)
            _missWindowActive = false;

        // 4. 릴링 진동 처리 함수 호출
        HandleReelingHaptics();

        // 5. 전투 중 찌와의 남은 거리 표시
        if (IsFighting && DistanceDisplayDelayTimer.ExpiredOrNotRunning(Runner))
        {
            if (_rodTip != null && bobber != null)
            {
                float distance = Vector3.Distance(_rodTip.position, CurrentBobber.transform.position);
                bobber.UpdateDistanceText($"{distance:F1}m");
            }
        }

        // 잡힌 물고기 회전 로직
        if (HasInputAuthority && HookedFish != null && !IsFighting)
        {
            var rodLine = SpawnedRod?.GetComponentInChildren<RodLineController>();
            var hookTransform = rodLine?.GetCurrentHookTransform();

            if (hookTransform != null)
            {
                hookTransform.Rotate(Vector3.up, caughtFishRotationSpeed * Time.deltaTime, Space.World);
            }
        }
    }

    private void HandleReelingHaptics()
    {
        if (!IsFighting || !isReeling) return;
        float normalizedGauge = tensionGauge / maxGauge;
        _hapticTimer -= Time.deltaTime;
        if (normalizedGauge >= 0.8f)
        {
            SendDualHapticImpulse(highTensionVibeAmplitude, vibeDuration);
        }
        else if (normalizedGauge >= 0.5f)
        {
            if (_hapticTimer <= 0f)
            {
                SendDualHapticImpulse(midTensionVibeAmplitude, vibeDuration);
                _hapticTimer = 0.5f;
            }
        }
        else
        {
            if (_hapticTimer <= 0f)
            {
                SendDualHapticImpulse(lowTensionVibeAmplitude, vibeDuration);
                _hapticTimer = 1.0f;
            }
        }
    }

    private void SendDualHapticImpulse(float amplitude, float duration)
    {
        if (_leftHandXRController != null) _leftHandXRController.SendHapticImpulse(amplitude, duration);
        if (_rightHandXRController != null) _rightHandXRController.SendHapticImpulse(amplitude, duration);
    }

    #region Fusion 콜백 함수
    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            Local = this;
            if (_riggingManager.rightHandController != null)
                _lastControllerPos = _riggingManager.rightHandController.position;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Local == this) Local = null;
    }

    public override void FixedUpdateNetwork()
    {
        if (HasStateAuthority)
        {
            if (IsFighting)
            {
                if (isReeling)
                {
                    float increasePerSecond = 10f;
                    if (HookedFish != null && HookedFish.TryGetComponent<FishData>(out var fishData))
                    {
                        increasePerSecond *= fishData.finalWeight;
                    }
                    tensionGauge = Mathf.Min(tensionGauge + increasePerSecond * Runner.DeltaTime, maxGauge);
                }
                else
                {
                    tensionGauge = Mathf.Max(tensionGauge - gaugeDecreasePerSec * Runner.DeltaTime, 0f);
                    if (FleeStateTimer.Expired(Runner))
                    {
                        IsCurrentlyFleeing = !IsCurrentlyFleeing;
                        if (IsCurrentlyFleeing)
                        {
                            FleeStateTimer = TickTimer.CreateFromSeconds(Runner, Random.Range(3f, 6f));
                            Vector3 awayDirection = (CurrentBobber.transform.position - transform.position);
                            awayDirection.y = 0;
                            Quaternion randomRotation = Quaternion.Euler(0, Random.Range(-45f, 45f), 0);
                            FleeDirection = randomRotation * awayDirection.normalized;
                        }
                        else
                        {
                            FleeStateTimer = TickTimer.CreateFromSeconds(Runner, 3f);
                        }
                    }
                    if (IsCurrentlyFleeing)
                    {
                        if (CurrentBobber != null && HookedFish != null && HookedFish.TryGetComponent<FishData>(out var fishData))
                        {
                            float bobberDistance = fishData.strength * Runner.DeltaTime;
                            float hookDistance = bobberDistance * 2f;
                            if (CurrentBobber.TryGetComponent<Rigidbody>(out var bobberRigidbody))
                            {
                                bobberRigidbody.MovePosition(bobberRigidbody.position + FleeDirection * bobberDistance);
                            }
                            var rodLine = SpawnedRod?.GetComponentInChildren<RodLineController>();
                            var hookTransform = rodLine?.GetCurrentHookTransform();
                            if (hookTransform != null && hookTransform.TryGetComponent<Rigidbody>(out var hookRigidbody))
                            {
                                hookRigidbody.MovePosition(hookRigidbody.position + FleeDirection * hookDistance);
                                if (FleeDirection.sqrMagnitude > 0.01f)
                                {
                                    Quaternion targetRotation = Quaternion.LookRotation(FleeDirection);
                                    hookRigidbody.MoveRotation(Quaternion.Slerp(hookRigidbody.rotation, targetRotation, Runner.DeltaTime * 5f));
                                }
                            }
                        }
                    }
                }
            }
            if (tensionGauge >= maxGauge && IsFighting)
            {
                RPC_OnFishingFailed();
            }
            if (CurrentBobber != null && IsFighting)
            {
                float distanceToBobber = Vector3.Distance(transform.position, CurrentBobber.transform.position);
                if (distanceToBobber > maxFishDistance)
                {
                    RPC_OnFishingFailed();
                }
            }
        }
        if (HasInputAuthority && castingHandler != null && castingHandler.castAction.action.WasReleasedThisFrame())
        {
            if (CastingCooldown.ExpiredOrNotRunning(Runner))
            {
                if (!IsFighting && HookedFish == null && CurrentBobber != null && CurrentBobber.GetComponent<ConfigurableJoint>() != null)
                {
                    RPC_CastBobber(castingHandler.LastCastVelocity);
                }
            }
        }
    }

    public override void Render()
    {
        if (HasInputAuthority && CurrentBobber != null)
        {
            var bobber = CurrentBobber.GetComponent<BobberController>();
            if (bobber != null)
            {
                if (bobber.HasFishOn && !_fishJustBit)
                {
                    _fishJustBit = true;
                    SendHapticImpulse(vibrationAmplitude, vibrationDuration);
                }
                else if (!bobber.HasFishOn && _fishJustBit)
                {
                    _fishJustBit = false;
                    _missWindowActive = true;
                    _missWindowEndTime = Time.time + 2.0f;
                }
            }
        }
        UpdateGaugeUI();
    }
    #endregion

    #region 낚시 상태 및 RPC 함수
    public void ToggleFishingState()
    {
        if (HasInputAuthority) RPC_SetFishingState(!IsFishing);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetFishingState(NetworkBool isFishing)
    {
        IsFishing = isFishing;
        if (isFishing)
        {
            CastingCooldown = TickTimer.CreateFromSeconds(Runner, 1.0f);
            if (fishingRodPrefab != null && SpawnedRod == null)
            {
                SpawnedRod = Runner.Spawn(fishingRodPrefab, Vector3.zero, Quaternion.identity, Object.InputAuthority);
                if (SpawnedRod != null)
                {
                    _rodTip = SpawnedRod.GetComponentInChildren<RodInfo>()?.rodTip;
                    _rodTipRb = _rodTip?.GetComponent<Rigidbody>();
                }
            }
            if (bobberPrefab != null && CurrentBobber == null && _rodTip != null)
            {
                Vector3 spawnPos = _rodTip.position - (_rodTip.up * bobberHangOffset);
                CurrentBobber = Runner.Spawn(bobberPrefab, spawnPos, Quaternion.identity, Object.InputAuthority);
                if (CurrentBobber.TryGetComponent<Rigidbody>(out var rb)) rb.isKinematic = false;
                AttachBobberWithJoint(CurrentBobber, _rodTipRb);
            }
        }
        else
        {
            if (SpawnedRod != null) Runner.Despawn(SpawnedRod);
            if (CurrentBobber != null) Runner.Despawn(CurrentBobber);
            if (HookedFish != null) Runner.Despawn(HookedFish);
            SpawnedRod = null;
            CurrentBobber = null;
            HookedFish = null;
            IsFighting = false;
            _rodTip = null;
            _rodTipRb = null;
            tensionGauge = 0f;
        }
        RPC_UpdateVisuals(isFishing, SpawnedRod);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_AttemptHook(NetworkObject fishToHook)
    {
        if (IsFighting || fishToHook == null) return;
        HookedFish = fishToHook;
        if (HookedFish != null && HookedFish.TryGetComponent<FishData>(out var fishData))
        {
            fishData.InitializeFish();
            HookedFish.gameObject.SetActive(true);
            IsFighting = true;
            DistanceDisplayDelayTimer = TickTimer.CreateFromSeconds(Runner, 1.5f);
            if (HookedFish != null)
            {
                int outlineLayer = LayerMask.NameToLayer("FishOutline");
                SetLayerRecursively(HookedFish.gameObject, outlineLayer);
            }
            if (CurrentBobber != null && CurrentBobber.TryGetComponent<BobberController>(out var bobber))
            {
                bobber.HasFishOn = false;
                bobber.HookedFish = null;
                bobber.ShowHitText();
            }
            if (CurrentBobber != null && CurrentBobber.TryGetComponent<ConfigurableJoint>(out var joint)) Destroy(joint);
            AttachFishToHook();
            tensionGauge = 0f;
            IsCurrentlyFleeing = true;
            FleeStateTimer = TickTimer.CreateFromSeconds(Runner, Random.Range(3f, 6f));
            Vector3 awayDirection = (CurrentBobber.transform.position - transform.position);
            awayDirection.y = 0;
            Quaternion randomRotation = Quaternion.Euler(0, Random.Range(-45f, 45f), 0);
            FleeDirection = randomRotation * awayDirection.normalized;
        }
        else if (HookedFish != null)
        {
            Runner.Despawn(HookedFish);
            HookedFish = null;
        }
    }

    private void AttachFishToHook()
    {
        var rodLine = SpawnedRod != null ? SpawnedRod.GetComponentInChildren<RodLineController>() : null;
        if (rodLine == null) return;
        Transform hookTransform = rodLine.GetCurrentHookTransform();
        if (hookTransform == null) return;
        Transform attachPoint = FindDeepChild(hookTransform, "FishAttachPoint");
        if (attachPoint == null) return;
        if (HookedFish != null)
        {
            var fishTransform = HookedFish.transform;
            Transform headEnd = FindDeepChild(fishTransform, "HEAD_end");
            if (headEnd == null) return;
            fishTransform.SetParent(attachPoint);
            fishTransform.localPosition = Vector3.zero;
            Vector3 offset = attachPoint.position - headEnd.position;
            fishTransform.position += offset;
            if (fishTransform.TryGetComponent<Rigidbody>(out var fishRb))
            {
                fishRb.isKinematic = true;
                fishRb.useGravity = false;
            }
        }
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        if (parent.name == name) return parent;
        foreach (Transform child in parent)
        {
            var result = FindDeepChild(child, name);
            if (result != null) return result;
        }
        return null;
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_CastBobber(Vector3 force)
    {
        if (CurrentBobber == null) return;
        var joint = CurrentBobber.GetComponent<ConfigurableJoint>();
        if (joint != null) Destroy(joint);
        if (CurrentBobber.TryGetComponent<Rigidbody>(out var rb))
        {
            rb.AddForce(force, ForceMode.Impulse);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ReelIn(float reelAmount)
    {
        if (CurrentBobber == null || _rodTip == null) return;
        if (CurrentBobber.TryGetComponent<Rigidbody>(out var bobberRigidbody))
        {
            if (CurrentBobber.GetComponent<ConfigurableJoint>() == null)
            {
                Vector3 newPosition = Vector3.MoveTowards(bobberRigidbody.position, _rodTip.position, reelAmount);
                bobberRigidbody.MovePosition(newPosition);
                if (HookedFish != null && IsFighting)
                {
                    var rodLine = SpawnedRod?.GetComponentInChildren<RodLineController>();
                    var hookTransform = rodLine?.GetCurrentHookTransform();
                    var fishTransform = HookedFish.transform;
                    Transform playerTr = Camera.main?.transform;
                    if (hookTransform != null && playerTr != null)
                    {
                        Vector3 toPlayer = playerTr.position - hookTransform.position;
                        if (toPlayer.sqrMagnitude > 0.0001f)
                        {
                            hookTransform.rotation = Quaternion.LookRotation(toPlayer, Vector3.up);
                        }
                    }
                    Transform headEnd = FindDeepChild(fishTransform, "HEAD_end");
                    Transform attachPoint = (hookTransform != null) ? FindDeepChild(hookTransform, "FishAttachPoint") : null;
                    if (headEnd != null && attachPoint != null)
                    {
                        Vector3 offset = attachPoint.position - headEnd.position;
                        fishTransform.position += offset;
                    }
                }
                if (Vector3.Distance(bobberRigidbody.position, _rodTip.position) < retrievalDistance)
                {
                    AttachBobberWithJoint(CurrentBobber, _rodTipRb);
                    if (IsFighting)
                    {
                        var rodLineController = SpawnedRod?.GetComponentInChildren<RodLineController>();
                        var hookTransform = rodLineController?.GetCurrentHookTransform();
                        if (hookTransform != null)
                        {
                            var currentRotation = hookTransform.eulerAngles;
                            hookTransform.rotation = Quaternion.Euler(0f, currentRotation.y, currentRotation.z);
                        }
                        Debug.Log("낚시 성공! 물고기가 매달려 있습니다. 10초 후 사라집니다.");
                        var caughtFishUI = CurrentBobber.GetComponent<CaughtFishUI>();
                        var fishData = HookedFish.GetComponent<FishData>();
                        if (caughtFishUI != null && fishData != null)
                        {
                            caughtFishUI.ShowFishInfo(fishData, 10f);
                            playerInventory.AddItem(fishData.fishID, 1);
                        }
                        if (HookedFish != null)
                        {
                            int fishLayer = LayerMask.NameToLayer("Fish");
                            SetLayerRecursively(HookedFish.gameObject, fishLayer);
                        }
                        IsFighting = false;
                        if (CurrentBobber != null && CurrentBobber.TryGetComponent<BobberController>(out var bobber))
                        {
                            bobber.HideAllTexts();
                        }
                        tensionGauge = 0f;
                        if (HookedFish != null)
                        {
                            var fishTransform = HookedFish.transform;
                            fishTransform.localRotation = Quaternion.Euler(-90, 0, 0);
                            Transform headEnd = FindDeepChild(fishTransform, "HEAD_end");
                            Transform attachPoint = (hookTransform != null) ? FindDeepChild(hookTransform, "FishAttachPoint") : null;
                            if (headEnd != null && attachPoint != null)
                            {
                                Vector3 offset = attachPoint.position - headEnd.position;
                                fishTransform.position += offset;
                            }
                            StartCoroutine(DelayedDespawnRoutine(HookedFish));
                        }
                    }
                }
            }
        }
    }

    private IEnumerator DelayedDespawnRoutine(NetworkObject fishToDespawn)
    {
        yield return new WaitForSeconds(10f);
        if (Runner != null && fishToDespawn != null && fishToDespawn.IsValid)
        {
            Runner.Despawn(fishToDespawn);
        }
        if (HookedFish == fishToDespawn)
        {
            HookedFish = null;
        }
    }

    private void AttachBobberWithJoint(NetworkObject bobber, Rigidbody rodTipRb)
    {
        if (bobber == null || rodTipRb == null) return;
        var existingJoint = bobber.GetComponent<ConfigurableJoint>();
        if (existingJoint != null) Destroy(existingJoint);
        var joint = bobber.gameObject.AddComponent<ConfigurableJoint>();
        joint.connectedBody = rodTipRb;
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = Vector3.zero;
        joint.anchor = Vector3.zero;
        joint.xMotion = ConfigurableJointMotion.Limited;
        joint.yMotion = ConfigurableJointMotion.Limited;
        joint.zMotion = ConfigurableJointMotion.Limited;
        var limit = new SoftJointLimit { limit = bobberHangOffset };
        joint.linearLimit = limit;
    }
    #endregion

    #region 게이지 및 유틸리티 함수
    public void StartReeling()
    {
        if (HasInputAuthority) isReeling = true;
    }

    public void StopReeling()
    {
        if (HasInputAuthority) isReeling = false;
    }

    public void ReelIn(float reelAmount)
    {
        if (CurrentBobber == null || !HasInputAuthority) return;
        RPC_ReelIn(reelAmount);
    }

    public void EndReel()
    {
        StopReeling();
    }

    private void UpdateGaugeUI()
    {
        if (CurrentBobber == null) return;
        var slider = CurrentBobber.GetComponentInChildren<Slider>(true);
        if (slider != null)
        {
            if (IsFighting)
            {
                if (!slider.gameObject.activeSelf) slider.gameObject.SetActive(true);
                slider.value = tensionGauge / maxGauge;
            }
            else
            {
                if (slider.gameObject.activeSelf) slider.gameObject.SetActive(false);
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_OnFishingFailed()
    {
        if (CurrentBobber != null && CurrentBobber.TryGetComponent<BobberController>(out var bobber))
        {
            bobber.RPC_ShowMessage("줄이 끊어졌습니다.", 2f, true);
        }
        if (HookedFish != null)
        {
            int fishLayer = LayerMask.NameToLayer("Fish");
            SetLayerRecursively(HookedFish.gameObject, fishLayer);
        }
        Debug.Log($"낚시 실패! 게이지 100% 초과. 플레이어: {Object.InputAuthority}");

        if (SpawnedRod != null) Runner.Despawn(SpawnedRod);
        if (HookedFish != null) Runner.Despawn(HookedFish);

        SpawnedRod = null;
        CurrentBobber = null;
        HookedFish = null;
        IsFighting = false;
        tensionGauge = 0f;
        IsCurrentlyFleeing = false;
        FleeStateTimer = default;
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateVisuals(NetworkBool isFishing, NetworkObject rod)
    {
        AttachRodToHand(isFishing, rod);
        if (HasInputAuthority)
        {
            FishingUIManager.Instance?.UpdateFishingButton(isFishing);
        }
    }

    private void AttachRodToHand(bool isFishing, NetworkObject rod)
    {
        if (rod == null || _riggingManager?.rightHandController == null) return;
        if (rod.TryGetComponent<NetworkTransform>(out var networkTransform))
        {
            if (isFishing)
            {
                networkTransform.enabled = false;
                rod.transform.SetParent(_riggingManager.rightHandController);
                rod.transform.localPosition = new Vector3(0, 0, 0.05f);
                rod.transform.localRotation = Quaternion.Euler(0, 0, 90);
                rod.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            }
        }
    }

    private void SendHapticImpulse(float amplitude, float duration)
    {
        if (_rightHandXRController != null)
        {
            _rightHandXRController.SendHapticImpulse(amplitude, duration);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!HasInputAuthority) return;
        if (other.CompareTag("CastingZone")) FishingUIManager.Instance?.ShowButton();
    }

    private void OnTriggerExit(Collider other)
    {
        if (!HasInputAuthority) return;
        if (other.CompareTag("CastingZone")) FishingUIManager.Instance?.HideButton();
    }
    #endregion

    private void SetLayerRecursively(GameObject obj, int newLayer)
    {
        if (obj == null) return;
        obj.layer = newLayer;
        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, newLayer);
        }
    }
}