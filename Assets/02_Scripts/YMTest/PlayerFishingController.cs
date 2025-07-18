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

    // ▼▼▼ [릴 기능 추가] ▼▼▼
    [Header("릴 설정")]
    [Tooltip("찌가 이 거리 안으로 들어오면 회수된 것으로 간주합니다.")]
    [SerializeField] private float retrievalDistance = 1.5f;
    // ▲▲▲▲▲ [릴 기능 추가] ▲▲▲▲▲

    [Networked]
    public NetworkBool IsFishing { get; set; }

    [Networked]
    private NetworkObject SpawnedRod { get; set; }

    [Networked]
    public NetworkObject CurrentBobber { get; private set; }

    private RiggingManager _riggingManager;
    public static PlayerFishingController Local { get; private set; }

    private XRBaseController _rightHandXRController;
    private bool _fishJustBit = false;

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
        if (HasInputAuthority)
        {
            if (castingHandler != null && castingHandler.castAction.action.WasReleasedThisFrame())
            {
                RPC_CastBobber(castingHandler.LastCastVelocity);
            }
        }
    }

    public override void Render()
    {
        if (HasInputAuthority && CurrentBobber != null)
        {
            var bobber = CurrentBobber.GetComponent<BobberController>();
            if (bobber == null) return;

            if (bobber.HasFishOn && !_fishJustBit)
            {
                _fishJustBit = true;
                SendHapticImpulse(vibrationAmplitude, vibrationDuration);
                Debug.Log("[Local Player] Bite detected! Sending haptics.");
            }
            else if (!bobber.HasFishOn && _fishJustBit)
            {
                _fishJustBit = false;
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

        if (isFishing)
        {
            if (fishingRodPrefab != null && SpawnedRod == null)
            {
                SpawnedRod = Runner.Spawn(fishingRodPrefab, Vector3.zero, Quaternion.identity, Object.InputAuthority);
            }
        }
        else
        {
            if (SpawnedRod != null) Runner.Despawn(SpawnedRod);
            if (CurrentBobber != null) Runner.Despawn(CurrentBobber);
            SpawnedRod = null;
            CurrentBobber = null;
        }

        RPC_UpdateVisuals(isFishing, SpawnedRod);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_CastBobber(Vector3 force)
    {
        if (IsFishing && CurrentBobber == null && bobberPrefab != null)
        {
            Transform rodTip = SpawnedRod?.GetComponentInChildren<RodInfo>()?.rodTip;
            Vector3 spawnPos = rodTip != null ? rodTip.position : transform.position;

            CurrentBobber = Runner.Spawn(bobberPrefab, spawnPos, Quaternion.identity, Object.InputAuthority);

            Rigidbody rb = CurrentBobber.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.AddForce(force, ForceMode.Impulse);
            }
        }
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
        else
        {
            Debug.LogWarning("진동을 주기 위한 오른손 컨트롤러(XRBaseController)를 찾지 못했습니다.");
        }
    }

    // ▼▼▼▼▼ [릴 기능 추가] ▼▼▼▼▼
    /// <summary>
    /// ReelController가 호출하여 릴을 감는 양을 전달합니다. (클라이언트에서 실행)
    /// </summary>
    public void ReelIn(float reelAmount)
    {
        // 찌가 없거나, 입력 권한이 없으면 실행하지 않습니다.
        if (CurrentBobber == null || !HasInputAuthority) return;

        // 서버에 릴을 감는 양을 전달합니다.
        RPC_ReelIn(reelAmount);
    }

    /// <summary>
    /// 서버에서 실행되어 실제로 찌를 움직이고 회수를 판정합니다.
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ReelIn(float reelAmount)
    {
        if (CurrentBobber == null) return;

        // 찌를 낚싯대 끝(rodTip) 방향으로 이동시킵니다.
        Transform rodTip = SpawnedRod?.GetComponentInChildren<RodInfo>()?.rodTip;
        if (rodTip == null) return; // 낚싯대 끝을 찾을 수 없으면 중단

        var bobberTransform = CurrentBobber.transform;
        // MoveTowards를 사용하여 찌를 낚싯대 끝으로 reelAmount만큼 이동
        bobberTransform.position = Vector3.MoveTowards(bobberTransform.position, rodTip.position, reelAmount);

        // 낚싯대 끝과의 거리를 확인하여 회수 처리
        if (Vector3.Distance(bobberTransform.position, rodTip.position) < retrievalDistance)
        {
            Debug.Log("찌 회수 완료!");
            Runner.Despawn(CurrentBobber); // 찌를 네트워크에서 파괴
            CurrentBobber = null; // 참조 제거
        }
    }
    // ▲▲▲▲▲ [릴 기능 추가] ▲▲▲▲▲


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
}