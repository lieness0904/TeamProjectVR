using UnityEngine;
using Fusion;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(RiggingManager))]
public class PlayerFishingController : NetworkBehaviour
{
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


    [Networked]
    public NetworkBool IsFishing { get; set; }
    [Networked]
    private NetworkObject SpawnedRod { get; set; }
    [Networked]
    public NetworkObject CurrentBobber { get; private set; }

    // ▼▼▼ [새로 추가된 변수] 캐스팅 쿨다운 타이머 ▼▼▼
    [Networked]
    private TickTimer CastingCooldown { get; set; }
    // ▲▲▲ [새로 추가된 변수] 캐스팅 쿨다운 타이머 ▲▲▲

    private RiggingManager _riggingManager;
    public static PlayerFishingController Local { get; private set; }
    private XRBaseController _rightHandXRController;
    private bool _fishJustBit = false;
    private Transform _rodTip;
    private Rigidbody _rodTipRb;


    private void Awake()
    {
        _riggingManager = GetComponent<RiggingManager>();
        if (_riggingManager != null && _riggingManager.rightHandController != null)
        {
            _rightHandXRController = _riggingManager.rightHandController.GetComponent<XRBaseController>();
        }
    }

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            Local = this;
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
            // ▼▼▼ [수정된 부분] 쿨다운 타이머가 끝났는지 확인하는 조건 추가 ▼▼▼
            if (CastingCooldown.ExpiredOrNotRunning(Runner))
            {
                if (CurrentBobber != null && CurrentBobber.GetComponent<ConfigurableJoint>() != null)
                {
                    RPC_CastBobber(castingHandler.LastCastVelocity);
                }
            }
            // ▲▲▲ [수정된 부분] ▲▲▲
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
                }
            }
        }
    }

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

        // ▼▼▼ [수정된 부분] 낚시 시작 시 쿨다운 타이머 설정 ▼▼▼
        if (isFishing)
        {
            // 낚시 시작 시 1초의 캐스팅 쿨다운을 설정합니다.
            CastingCooldown = TickTimer.CreateFromSeconds(Runner, 1.0f);
        }
        // ▲▲▲ [수정된 부분] ▲▲▲

        if (isFishing)
        {
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

                Rigidbody rb = CurrentBobber.GetComponent<Rigidbody>();
                if (rb != null)
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
            SpawnedRod = null;
            CurrentBobber = null;
            _rodTip = null;
            _rodTipRb = null;
        }
        RPC_UpdateVisuals(isFishing, SpawnedRod);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_CastBobber(Vector3 force)
    {
        if (CurrentBobber == null) return;

        var joint = CurrentBobber.GetComponent<ConfigurableJoint>();
        if (joint != null)
        {
            Destroy(joint);
        }

        Rigidbody rb = CurrentBobber.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(force, ForceMode.Impulse);
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ReelIn(float reelAmount)
    {
        if (CurrentBobber == null || _rodTip == null) return;

        Rigidbody bobberRigidbody = CurrentBobber.GetComponent<Rigidbody>();
        if (bobberRigidbody == null) return;

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

    #region 기존 함수들 (변경 없음)
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
        var networkTransform = rod.GetComponent<NetworkTransform>();
        if (networkTransform == null) return;
        if (isFishing)
        {
            Transform rightHand = _riggingManager.rightHandController;
            if (rightHand != null)
            {
                networkTransform.enabled = false;
                rod.transform.SetParent(rightHand);
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