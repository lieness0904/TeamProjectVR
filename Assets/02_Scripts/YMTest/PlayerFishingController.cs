using UnityEngine;
using Fusion;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using TMPro;
using UnityEngine.UI; // 게이지 UI 연동을 위해 추가

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
    [Tooltip("찌가 낚싯대를 따라오는 부드러운 정도입니다. 값이 높을수록 빨리 따라옵니다.")]
    [SerializeField] private float bobberDampingSpeed = 10f;

    [Header("챔질(Hooking) 설정")]
    [Tooltip("이 속도를 넘는 컨트롤러 움직임을 챔질로 인식합니다.")]
    [SerializeField] private float hookVelocityThreshold = 3.0f;

    [Header("전투 UI")]
    [Tooltip("'HIT!' 메시지를 표시할 UI 텍스트")]
    [SerializeField] private TextMeshProUGUI hitText;

    // 게이지 관련 변수
    private float tensionGauge = 0f;          // 게이지(0~100)
    private const float maxGauge = 100f;
    private const float gaugeDecreasePerSec = 2f; // 1초에 자연 감소량(%)
    private float reelDistanceAccumulator = 0f;   // 릴 감은 거리 누적(한 바퀴 처리용)
    private bool isReeling = false;               // 릴 감기 중 여부

    [Networked] public NetworkBool IsFishing { get; set; }
    [Networked] private NetworkObject SpawnedRod { get; set; }
    [Networked] public NetworkObject CurrentBobber { get; private set; }
    [Networked] private TickTimer CastingCooldown { get; set; }

    [Networked] public NetworkBool IsFighting { get; set; }
    [Networked] public NetworkObject HookedFish { get; set; }
    // --- MISS 로직을 위한 변수 ---
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

    #endregion

    private void Awake()
    {
        _riggingManager = GetComponent<RiggingManager>();
        if (_riggingManager != null && _riggingManager.rightHandController != null)
        {
            _rightHandXRController = _riggingManager.rightHandController.GetComponent<XRBaseController>();
        }
    }

    private void Update()
    {
        if (!HasInputAuthority) return;

        // 1. 오른쪽 컨트롤러의 현재 속도를 계속 계산합니다.
        if (_riggingManager.rightHandController != null)
        {
            Vector3 currentPos = _riggingManager.rightHandController.position;
            _currentControllerVel = (currentPos - _lastControllerPos) / Time.deltaTime;
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
                        // MISS 표시 (찌 위에)
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

        // ★★★ 게이지 자연 감소 처리 ★★★
        if (!isReeling && IsFighting) // 전투 중, 릴을 감고 있지 않으면 자연감소
        {
            float prevGauge = tensionGauge;
            tensionGauge = Mathf.Max(tensionGauge - gaugeDecreasePerSec * Time.deltaTime, 0f);

            
            if (Mathf.Abs(prevGauge - tensionGauge) > 0.01f)
            {
                UpdateGaugeUI();
            }
        }

        // ★★★ 게이지 100% 이상이면 낚시 실패 처리 ★★★
        if (tensionGauge >= maxGauge && IsFighting)
        {
            OnFishingFailed();
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
        if (Local == this)
        {
            Local = null;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (HasInputAuthority && castingHandler != null && castingHandler.castAction.action.WasReleasedThisFrame())
        {
            if (CastingCooldown.ExpiredOrNotRunning(Runner))
            {
                if (!IsFighting && CurrentBobber != null && CurrentBobber.GetComponent<ConfigurableJoint>() != null)
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
                    if (Object.HasInputAuthority)
                    {
                        RPC_ShowHitMessage();
                    }
                }
                else if (!bobber.HasFishOn && _fishJustBit)
                {
                    // ▼▼▼ 진동(입질) 끝난 직후 MISS 윈도우 활성화 ▼▼▼
                    _fishJustBit = false;
                    _missWindowActive = true;
                    _missWindowEndTime = Time.time + 2.0f;  // 2초 동안 MISS 체크

                    if (hitText != null && hitText.gameObject.activeSelf)
                    {
                        hitText.gameObject.SetActive(false);
                    }
                }
            }
        }
    }

    #endregion

    #region 낚시 상태 및 RPC 함수
    public void ToggleFishingState()
    {
        if (HasInputAuthority)
        {
            RPC_SetFishingState(!IsFishing);
        }
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
                _rodTip = SpawnedRod?.GetComponentInChildren<RodInfo>()?.rodTip;
                _rodTipRb = _rodTip?.GetComponent<Rigidbody>();
            }
            if (bobberPrefab != null && CurrentBobber == null && _rodTip != null)
            {
                Vector3 spawnPos = _rodTip.position - (_rodTip.up * bobberHangOffset);
                CurrentBobber = Runner.Spawn(bobberPrefab, spawnPos, Quaternion.identity, Object.InputAuthority);
                if (CurrentBobber.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.isKinematic = false;
                }
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

            // 게이지 리셋
            tensionGauge = 0f;
            UpdateGaugeUI();
        }
        RPC_UpdateVisuals(isFishing, SpawnedRod);
    }

    // ▼▼▼ 챔질 시도 RPC - Hook에 Fish(HEAD_end) 연결 ▼▼▼
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_AttemptHook(NetworkObject fishToHook)
    {
        Debug.Log($"[RPC_AttemptHook] 호출. IsFighting: {IsFighting}, fishToHook: {(fishToHook != null ? fishToHook.name : "null")}");

        if (IsFighting || fishToHook == null)
        {
            Debug.LogWarning($"[RPC_AttemptHook] 실패 - IsFighting: {IsFighting}, fishToHook null? {fishToHook == null}");
            return;
        }

        HookedFish = fishToHook;

        if (HookedFish != null && HookedFish.TryGetComponent<FishData>(out var fishData))
        {
            Debug.Log($"[RPC_AttemptHook] 물고기({HookedFish.name}) FishData 있음. InitializeFish() 호출");
            fishData.InitializeFish();
            HookedFish.gameObject.SetActive(true);

            IsFighting = true;

            // ★ 찌의 BobberController에서 "HIT!" 메시지 표시
            if (CurrentBobber != null && CurrentBobber.TryGetComponent<BobberController>(out var bobber))
            {
                bobber.HasFishOn = false;
                bobber.HookedFish = null;
                bobber.ShowBobberText("HIT!", 1.5f); // << 이 부분이 추가됨
            }

            if (CurrentBobber != null && CurrentBobber.TryGetComponent<ConfigurableJoint>(out var joint))
            {
                Destroy(joint);
            }

            AttachFishToHook();

            // 기존 UI 메시지 (사용하지 않으면 삭제해도 됨)
            RPC_ShowHitMessage();

            // ★★★ 게이지도 전투 시작 시 0으로 리셋 ★★★
            tensionGauge = 0f;
            UpdateGaugeUI();

            Debug.Log($"[Server] 챔질 성공! 물고기 {HookedFish.name}와 전투 시작.");
        }
        else if (HookedFish != null)
        {
            Debug.LogWarning("[RPC_AttemptHook] FishData 없음, 또는 이상상황. Despawn 호출");
            Runner.Despawn(HookedFish);
            HookedFish = null;
        }
    }

    /// <summary>
    /// 챔질 성공 시, 바늘(Hook)에 Fish(HEAD_end)를 자동 연결
    /// </summary>
    private void AttachFishToHook()
    {
        var rodLine = SpawnedRod != null ? SpawnedRod.GetComponentInChildren<RodLineController>() : null;
        if (rodLine == null)
        {
            Debug.LogWarning("[AttachFishToHook] RodLineController를 찾지 못함");
            return;
        }

        Transform hookTransform = rodLine.GetCurrentHookTransform();
        if (hookTransform == null)
        {
            Debug.LogWarning("[AttachFishToHook] 바늘(Hook) 오브젝트를 찾지 못함");
            return;
        }

        Transform attachPoint = FindDeepChild(hookTransform, "FishAttachPoint");
        if (attachPoint == null)
        {
            Debug.LogWarning("[AttachFishToHook] FishAttachPoint를 찾지 못함. 프리팹 구조를 확인하세요.");
            return;
        }

        if (HookedFish != null)
        {
            var fishTransform = HookedFish.transform;
            Transform headEnd = FindDeepChild(fishTransform, "HEAD_end");
            if (headEnd == null)
            {
                Debug.LogWarning("[AttachFishToHook] HEAD_end 오브젝트를 찾지 못함");
                return;
            }

            fishTransform.SetParent(attachPoint);
            fishTransform.localPosition = Vector3.zero;

            Vector3 offset = attachPoint.position - headEnd.position;
            fishTransform.position += offset;

            if (fishTransform.TryGetComponent<Rigidbody>(out var fishRb))
            {
                fishRb.isKinematic = true;
                fishRb.useGravity = false;
            }

            Debug.Log("[AttachFishToHook] 001이 FishAttachPoint에 연결되고, HEAD_end가 바늘 위치에 정확히 이동됨");
        }
        else
        {
            Debug.LogWarning("[AttachFishToHook] HookedFish 오브젝트를 찾지 못함");
        }
    }

    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child;
            var result = FindDeepChild(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
    // ▲▲▲ 챔질 성공 시 바늘-물고기 연결 ▲▲▲

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

                // ★ 릴 감기 중, 입 위치 고정 & 플레이어를 바라보는 회전 적용 ★
                if (HookedFish != null)
                {
                    var fishTransform = HookedFish.transform;
                    Transform headEnd = FindDeepChild(fishTransform, "HEAD_end");
                    var rodLine = SpawnedRod != null ? SpawnedRod.GetComponentInChildren<RodLineController>()?.GetCurrentHookTransform() : null;
                    Transform attachPoint = rodLine != null ? FindDeepChild(rodLine, "FishAttachPoint") : null;

                    Transform playerTr = Camera.main != null ? Camera.main.transform : null;

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

                    tensionGauge = 0f;
                    reelDistanceAccumulator = 0f;
                    isReeling = false;
                    IsFighting = false;
                    UpdateGaugeUI(true);

                    if (HookedFish != null)
                    {
                        var fishTransform = HookedFish.transform;
                        fishTransform.localRotation = Quaternion.Euler(-90, 0, 0);

                        Transform headEnd = FindDeepChild(fishTransform, "HEAD_end");
                        var rodLine = SpawnedRod != null ? SpawnedRod.GetComponentInChildren<RodLineController>()?.GetCurrentHookTransform() : null;
                        Transform attachPoint = rodLine != null ? FindDeepChild(rodLine, "FishAttachPoint") : null;

                        if (headEnd != null && attachPoint != null)
                        {
                            Vector3 offset = attachPoint.position - headEnd.position;
                            fishTransform.position += offset;
                        }
                    }
                }
            }
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
        var limit = new SoftJointLimit();
        limit.limit = bobberHangOffset;
        joint.linearLimit = limit;
    }
    #endregion

    #region 게이지(장력) 시스템 함수
    // 릴을 감기 시작할 때 호출 (ReelController에서 호출)
    public void StartReeling()
    {
        isReeling = true;
       
    }

    // 릴 감기를 멈출 때 호출 (ReelController에서 호출)
    public void StopReeling()
    {
        isReeling = false;
       
    }

    // 릴을 감을 때마다 호출 (거리 단위: meter)
    public void ReelInGauge(float reelAmount)
    {
        if (!IsFighting || HookedFish == null) return;

        // 1. 감은 거리 누적
        reelDistanceAccumulator += reelAmount;

        // 2. 한 바퀴(1m) 이상 감았을 때 게이지 증가
        while (reelDistanceAccumulator >= 1f)
        {
            float strength = 0f;
            if (HookedFish != null && HookedFish.TryGetComponent<FishData>(out var fishData))
                strength = fishData.strength;

            // 게이지: 1% + 힘(strength) 만큼 상승
            tensionGauge = Mathf.Min(tensionGauge + (1f + strength), maxGauge);
            reelDistanceAccumulator -= 1f;
            UpdateGaugeUI();
        }
    }

    // 게이지 UI 연동 함수
    private void UpdateGaugeUI(bool isInactive = false)
    {
        if (CurrentBobber != null)
        {
            var slider = CurrentBobber.GetComponentInChildren<Slider>();
            if (slider != null)
            {
                if (isInactive)
                {
                    slider.gameObject.SetActive(false); // 게이지바 비활성화
                    return;
                }
                slider.gameObject.SetActive(true); // 게이지바 활성화
                float normalizedGauge = tensionGauge / 100f;
                slider.value = normalizedGauge;

                var fill = slider.fillRect.GetComponent<UnityEngine.UI.Image>();
                if (fill != null)
                {
                    if (normalizedGauge <= 0.5f)
                        fill.color = Color.green;
                    else if (normalizedGauge <= 0.8f)
                        fill.color = Color.yellow;
                    else
                        fill.color = Color.red;
                }
            }
        }
    }


    // 낚시 실패 처리 (게이지 100% 이상)
    private void OnFishingFailed()
    {
        Debug.Log("낚시 실패! 게이지 100% 초과, 낚시대 초기화.");

        // 낚시대/찌/릴/물고기 제거 및 상태 리셋
        if (SpawnedRod != null) Runner.Despawn(SpawnedRod);
        if (CurrentBobber != null) Runner.Despawn(CurrentBobber);
        if (HookedFish != null) Runner.Despawn(HookedFish);

        SpawnedRod = null;
        CurrentBobber = null;
        HookedFish = null;
        IsFighting = false;
        tensionGauge = 0f;
        reelDistanceAccumulator = 0f;
        isReeling = false;
        _rodTip = null;
        _rodTipRb = null;
        UpdateGaugeUI();
    }
    #endregion

    #region 기타 유틸리티 함수
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
        if (rod == null) return;
        if (rod.TryGetComponent<NetworkTransform>(out var networkTransform))
        {
            if (isFishing)
            {
                if (_riggingManager.rightHandController != null)
                {
                    networkTransform.enabled = false;
                    rod.transform.SetParent(_riggingManager.rightHandController);
                    rod.transform.localPosition = new Vector3(0, 0, 0.05f);
                    rod.transform.localRotation = Quaternion.Euler(0, 0, 90);
                    rod.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
                }
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

    // 릴 감기(거리만큼 감기) → 릴 감기 동작 시 StartReeling/StopReeling도 연동 필요!
    public void ReelIn(float reelAmount)
    {
        if (CurrentBobber == null || !HasInputAuthority) return;
        StartReeling();
        ReelInGauge(reelAmount); // ★ 게이지 증가 로직 추가
        RPC_ReelIn(reelAmount);
    }
    public void EndReel()
    {
        StopReeling();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!HasInputAuthority) return;
        if (other.CompareTag("CastingZone"))
        {
            FishingUIManager.Instance?.ShowButton();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (!HasInputAuthority) return;
        if (other.CompareTag("CastingZone"))
        {
            FishingUIManager.Instance?.HideButton();
        }
    }
    #endregion
}
