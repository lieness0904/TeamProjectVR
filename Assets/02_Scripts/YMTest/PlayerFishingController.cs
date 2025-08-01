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

    [Header("전투 UI")]
    [Tooltip("'HIT!' 메시지를 표시할 UI 텍스트")]
    [SerializeField] private TextMeshProUGUI hitText;

    [Header("게이지 설정")]
    [SerializeField] private float gaugeDecreasePerSec = 50f; // 초당 자연 감소량

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

    // --- [추가] 물고기 도망 상태 관련 변수 ---
    [Networked] private Vector3 FleeDirection { get; set; }
    [Networked] private TickTimer FleeStateTimer { get; set; }
    [Networked] private NetworkBool IsCurrentlyFleeing { get; set; }

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
    private Transform _hookTransform;
    private XRBaseController _leftHandXRController;
    private float _hapticTimer = 0f;

    #endregion

    private void Awake()
    {
        _riggingManager = GetComponent<RiggingManager>();
        if (_riggingManager != null)
        {
            // 오른손 컨트롤러 참조
            if (_riggingManager.rightHandController != null)
            {
                _rightHandXRController = _riggingManager.rightHandController.GetComponent<XRBaseController>();
            }

            // [추가] 왼손 컨트롤러 참조 (RiggingManager에 leftHandController가 연결되어 있어야 합니다)
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

        // 2. MISS 판정 로직 (입질 끝나고 2초 동안)
        if (_missWindowActive && Time.time <= _missWindowEndTime)
        {
            if (CurrentBobber != null && CurrentBobber.TryGetComponent<BobberController>(out var missBobber))
            {
                if (!missBobber.HasFishOn && !IsFighting)
                {
                    if (_currentControllerVel.y > hookVelocityThreshold)
                    {
                        missBobber.ShowBobberText("MISS", 1.2f);
                        _missWindowActive = false; // MISS 윈도우 비활성화
                    }
                }
            }
        }
        if (_missWindowActive && Time.time > _missWindowEndTime)
            _missWindowActive = false; // 시간초과 시 자동 종료

        // 3. 챔질(HIT) 판정 로직 (입질 왔을 때만)
        if (CurrentBobber != null && CurrentBobber.TryGetComponent<BobberController>(out var bobber))
        {
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

        // 4. 릴링 진동 처리 함수 호출
        HandleReelingHaptics();

        // 5. [추가] 전투 중 찌와의 남은 거리 표시
        if (IsFighting)
        {
            if (hitText != null && _rodTip != null && CurrentBobber != null)
            {
                // "HIT!" 메시지 코루틴이 끝난 후 비활성화 되었을 수 있으므로 다시 활성화
                if (!hitText.gameObject.activeSelf)
                {
                    hitText.gameObject.SetActive(true);
                }

                // 낚싯대 끝과 찌 사이의 거리를 계산
                float distance = Vector3.Distance(_rodTip.position, CurrentBobber.transform.position);

                // 텍스트 내용 업데이트 (소수점 한 자리까지 "F1" 포맷으로)
                hitText.text = $"{distance:F1}m";
            }
        }
        // 전투가 종료되면 HIT/MISS를 표시하는 다른 로직들이 텍스트를 비활성화하므로
        // 여기서 별도로 비활성화 코드를 넣을 필요는 없습니다.
    }

    private void HandleReelingHaptics()
    {
        if (!IsFighting || !isReeling) return;

        float normalizedGauge = tensionGauge / maxGauge;
        _hapticTimer -= Time.deltaTime;

        if (normalizedGauge >= 0.8f) // 80% 이상
        {
            SendDualHapticImpulse(highTensionVibeAmplitude, vibeDuration);
        }
        else if (normalizedGauge >= 0.5f) // 50% 이상
        {
            if (_hapticTimer <= 0f)
            {
                SendDualHapticImpulse(midTensionVibeAmplitude, vibeDuration);
                _hapticTimer = 0.5f;
            }
        }
        else // 50% 미만
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
        if (_leftHandXRController != null)
        {
            _leftHandXRController.SendHapticImpulse(amplitude, duration);
        }
        if (_rightHandXRController != null)
        {
            _rightHandXRController.SendHapticImpulse(amplitude, duration);
        }
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
        // 게이지 계산, 실패 판정 등 중요한 로직은 서버/호스트(State Authority)에서만 처리
        if (HasStateAuthority)
        {
            // 전투 중에만 게이지 계산
            if (IsFighting)
            {
                if (isReeling)
                {
                    // 릴 잡고 있으면: 1초당 (10 * 물고기 무게) 만큼 게이지 증가
                    float increasePerSecond = 10f;
                    if (HookedFish != null && HookedFish.TryGetComponent<FishData>(out var fishData))
                    {
                        increasePerSecond *= fishData.finalWeight;
                    }
                    tensionGauge = Mathf.Min(tensionGauge + increasePerSecond * Runner.DeltaTime, maxGauge);
                }
                else // isReeling이 false일 때
                {
                    // 1. 게이지는 항상 자연 감소
                    tensionGauge = Mathf.Max(tensionGauge - gaugeDecreasePerSec * Runner.DeltaTime, 0f);

                    // 2. [교체] 물고기 도망 상태 머신 로직
                    if (FleeStateTimer.Expired(Runner))
                    {
                        // 타이머 만료 시, '도망'/'휴식' 상태를 전환
                        IsCurrentlyFleeing = !IsCurrentlyFleeing;

                        if (IsCurrentlyFleeing)
                        {
                            // '도망' 상태로 전환: 3~6초 타이머 설정 및 새 방향 계산
                            FleeStateTimer = TickTimer.CreateFromSeconds(Runner, Random.Range(3f, 6f));

                            Vector3 awayDirection = (CurrentBobber.transform.position - transform.position);
                            awayDirection.y = 0;
                            Quaternion randomRotation = Quaternion.Euler(0, Random.Range(-45f, 45f), 0);
                            FleeDirection = randomRotation * awayDirection.normalized;
                        }
                        else
                        {
                            // '휴식' 상태로 전환: 3초 타이머 설정
                            FleeStateTimer = TickTimer.CreateFromSeconds(Runner, 3f);
                        }
                    }

                    // '도망' 상태일 때만 실제로 물고기를 움직임
                    if (IsCurrentlyFleeing)
                    {
                        if (CurrentBobber != null && HookedFish != null && HookedFish.TryGetComponent<FishData>(out var fishData))
                        {
                            if (CurrentBobber.TryGetComponent<Rigidbody>(out var bobberRigidbody))
                            {
                                float distance = fishData.strength * Runner.DeltaTime;
                                bobberRigidbody.MovePosition(bobberRigidbody.position + FleeDirection * distance);
                            }
                        }
                    }
                }
            }

            // 게이지 100% 도달 시 실패 처리
            if (tensionGauge >= maxGauge && IsFighting)
            {
                RPC_OnFishingFailed();
            }
        }

        // 캐스팅 입력은 입력을 소유한 클라이언트에서만 처리
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
        // 진동, UI 업데이트 등 시각/촉각 효과는 로컬 플레이어에게 즉각적으로 보이도록 Render에서 처리
        if (HasInputAuthority && CurrentBobber != null)
        {
            var bobber = CurrentBobber.GetComponent<BobberController>();
            if (bobber != null)
            {
                if (bobber.HasFishOn && !_fishJustBit)
                {
                    _fishJustBit = true;
                    SendHapticImpulse(vibrationAmplitude, vibrationDuration);
                    if (Object.HasInputAuthority) RPC_ShowHitMessage();
                }
                else if (!bobber.HasFishOn && _fishJustBit)
                {
                    _fishJustBit = false;
                    _missWindowActive = true;
                    _missWindowEndTime = Time.time + 2.0f;
                    if (hitText != null && hitText.gameObject.activeSelf)
                    {
                        hitText.gameObject.SetActive(false);
                    }
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
            if (CurrentBobber != null && CurrentBobber.TryGetComponent<BobberController>(out var bobber))
            {
                bobber.HasFishOn = false;
                bobber.HookedFish = null;
                bobber.ShowBobberText("HIT!", 1.5f);
            }
            if (CurrentBobber != null && CurrentBobber.TryGetComponent<ConfigurableJoint>(out var joint)) Destroy(joint);
            AttachFishToHook();
            RPC_ShowHitMessage();
            tensionGauge = 0f;

            // --- [추가] 물고기 도망 상태 초기화 ---
            IsCurrentlyFleeing = true; // 처음엔 무조건 도망가는 상태로 시작
            FleeStateTimer = TickTimer.CreateFromSeconds(Runner, Random.Range(3f, 6f));

            // 초기 도망 방향 설정 (플레이어 반대편을 기준으로 랜덤 각도)
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

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_ShowHitMessage()
    {
        if (hitText != null)
        {
            StopAllCoroutines();
            StartCoroutine(ShowHitTextRoutine());
        }
    }

    private IEnumerator ShowHitTextRoutine()
    {
        hitText.gameObject.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        hitText.gameObject.SetActive(false);
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
                    var fishTransform = HookedFish.transform;
                    Transform headEnd = FindDeepChild(fishTransform, "HEAD_end");
                    var rodLine = SpawnedRod?.GetComponentInChildren<RodLineController>()?.GetCurrentHookTransform();
                    Transform attachPoint = (rodLine != null) ? FindDeepChild(rodLine, "FishAttachPoint") : null;
                    Transform playerTr = Camera.main?.transform;

                    if (headEnd != null && attachPoint != null && playerTr != null)
                    {
                        Vector3 offset = attachPoint.position - headEnd.position;
                        fishTransform.position += offset;
                        Vector3 toPlayer = playerTr.position - attachPoint.position;
                        if (toPlayer.sqrMagnitude > 0.0001f)
                        {
                            fishTransform.rotation = Quaternion.LookRotation(toPlayer, Vector3.up);
                        }
                    }
                }

                if (Vector3.Distance(bobberRigidbody.position, _rodTip.position) < retrievalDistance)
                {
                    AttachBobberWithJoint(CurrentBobber, _rodTipRb);
                    if (IsFighting)
                    {
                        Debug.Log("낚시 성공! 물고기가 매달려 있습니다. 10초 후 사라집니다.");
                        IsFighting = false;
                        tensionGauge = 0f;

                        if (HookedFish != null)
                        {
                            // --- [복원된 로직] 잡힌 물고기 자세 보정 ---
                            var fishTransform = HookedFish.transform;
                            fishTransform.localRotation = Quaternion.Euler(-90, 0, 0);

                            Transform headEnd = FindDeepChild(fishTransform, "HEAD_end");
                            var rodLine = SpawnedRod?.GetComponentInChildren<RodLineController>()?.GetCurrentHookTransform();
                            Transform attachPoint = rodLine != null ? FindDeepChild(rodLine, "FishAttachPoint") : null;

                            if (headEnd != null && attachPoint != null)
                            {
                                Vector3 offset = attachPoint.position - headEnd.position;
                                fishTransform.position += offset;
                            }
                            // --- [복원된 로직 끝] ---

                            // 10초 지연 후 Despawn 처리
                            StartCoroutine(DelayedDespawnRoutine(HookedFish));
                        }
                    }
                }
            }
        }
    }

    private IEnumerator DelayedDespawnRoutine(NetworkObject fishToDespawn)
    {
        // 1. 10초 대기
        yield return new WaitForSeconds(10f);

        // 2. 10초 후 서버에 해당 오브젝트가 아직 존재하고, 유효하다면 Despawn
        //    (RPC가 서버에서 실행되므로 이 코드는 서버에서만 동작함)
        if (Runner != null && fishToDespawn != null && fishToDespawn.IsValid)
        {
            Runner.Despawn(fishToDespawn);
        }

        // HookedFish가 방금 Despawn한 물고기와 동일하다면, 참조를 제거
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

    // 이 함수는 더 이상 게이지를 직접 계산하지 않음.
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
        Debug.Log($"낚시 실패! 게이지 100% 초과. 플레이어: {Object.InputAuthority}");

        if (SpawnedRod != null) Runner.Despawn(SpawnedRod);
        if (CurrentBobber != null) Runner.Despawn(CurrentBobber);
        if (HookedFish != null) Runner.Despawn(HookedFish);

        // 상태 변수는 RPC를 호출한 클라이언트에서도 즉시 초기화하여 빠른 반응성 제공
        // 최종 상태는 서버의 다음 스냅샷을 통해 동기화됨
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
}