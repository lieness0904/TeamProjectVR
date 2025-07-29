using UnityEngine;
using Fusion;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using TMPro;

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

    [Networked] public NetworkBool IsFishing { get; set; }
    [Networked] private NetworkObject SpawnedRod { get; set; }
    [Networked] public NetworkObject CurrentBobber { get; private set; }
    [Networked] private TickTimer CastingCooldown { get; set; }

    [Networked] public NetworkBool IsFighting { get; set; }
    [Networked] public NetworkObject HookedFish { get; set; }

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

        // 2. 챔질 조건을 확인합니다
        if (CurrentBobber != null && CurrentBobber.TryGetComponent<BobberController>(out var bobber))
        {
            // 디버그: 입질 상태 및 물고기 오브젝트 상태 출력
            Debug.Log($"[챔질 검사] HasFishOn: {bobber.HasFishOn}, IsFighting: {IsFighting}, HookedFish: {(bobber.HookedFish != null ? bobber.HookedFish.name : "null")}, 컨트롤러 속도Y: {_currentControllerVel.y}");

            if (bobber.HasFishOn && !IsFighting)
            {
                // 챔질 스냅 판정 (속도)
                if (_currentControllerVel.y > hookVelocityThreshold)
                {
                    Debug.Log($"[챔질 인식!] 컨트롤러Y 속도 = {_currentControllerVel.y} (임계치: {hookVelocityThreshold})");
                    if (bobber.HookedFish != null)
                    {
                        Debug.Log("[챔질] RPC_AttemptHook 호출 (물고기 오브젝트 존재)");
                        RPC_AttemptHook(bobber.HookedFish);
                    }
                    else
                    {
                        Debug.LogWarning("[챔질] 챔질시도 BUT 물고기 오브젝트가 null (bobber.HookedFish == null)");
                    }
                }
                else
                {
                    Debug.Log($"[챔질 미인식] 컨트롤러Y 속도 = {_currentControllerVel.y} (임계치: {hookVelocityThreshold})");
                }
            }
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
                    _fishJustBit = false;
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

            if (CurrentBobber.TryGetComponent<BobberController>(out var bobber))
            {
                bobber.HasFishOn = false;
                bobber.HookedFish = null;
            }
            if (CurrentBobber.TryGetComponent<ConfigurableJoint>(out var joint))
            {
                Destroy(joint);
            }

            AttachFishToHook();

            RPC_ShowHitMessage();
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
        // 1. 현재 RodLineController 찾기 (낚싯대에 존재)
        var rodLine = SpawnedRod != null ? SpawnedRod.GetComponentInChildren<RodLineController>() : null;
        if (rodLine == null)
        {
            Debug.LogWarning("[AttachFishToHook] RodLineController를 찾지 못함");
            return;
        }

        // 2. RodLineController가 관리하는 spawnedHook(바늘) Transform 얻기
        Transform hookTransform = rodLine.GetCurrentHookTransform();
        if (hookTransform == null)
        {
            Debug.LogWarning("[AttachFishToHook] 바늘(Hook) 오브젝트를 찾지 못함");
            return;
        }

        // 3. HookedFish의 Rigidbody 얻기
        if (HookedFish != null && HookedFish.TryGetComponent<Rigidbody>(out var fishRb))
        {
            // 기존에 달려있던 FixedJoint 있으면 제거 (안정성)
            var oldJoint = hookTransform.GetComponent<FixedJoint>();
            if (oldJoint != null) Destroy(oldJoint);

            // Hook(바늘)에 FixedJoint 추가 → 물고기 Rigidbody 연결
            var fixedJoint = hookTransform.gameObject.AddComponent<FixedJoint>();
            fixedJoint.connectedBody = fishRb;
            fixedJoint.breakForce = Mathf.Infinity;

            // 물고기 Rigidbody 활성화(중력 사용, 관성 허용)
            fishRb.isKinematic = false;
            fishRb.useGravity = true;

            Debug.Log($"[AttachFishToHook] {HookedFish.name}의 Rigidbody가 바늘에 FixedJoint로 연결됨");
        }
        else
        {
            Debug.LogWarning("[AttachFishToHook] 물고기 Rigidbody를 찾지 못함");
        }
    }

    /// <summary>
    /// 하위 모든 오브젝트에서 name으로 찾기 (재귀)
    /// </summary>
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
                if (Vector3.Distance(bobberRigidbody.position, _rodTip.position) < retrievalDistance)
                {
                    AttachBobberWithJoint(CurrentBobber, _rodTipRb);
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
    public void ReelIn(float reelAmount)
    {
        if (CurrentBobber == null || !HasInputAuthority) return;
        RPC_ReelIn(reelAmount);
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
