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
    [SerializeField] private float retrievalDistance = 1.5f; // 이 값을 조절하셨을 수 있습니다.

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

    // --- [수정된 함수] ---
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_CastBobber(Vector3 force)
    {
        // [진단 코드 추가] 캐스팅 조건이 맞지 않으면, 왜 무시되었는지 로그를 남기고 함수를 종료합니다.
        if (!IsFishing || CurrentBobber != null || bobberPrefab == null)
        {
            // 이 로그가 보인다면, CurrentBobber가 null이 아니어서 캐스팅이 실패한 것입니다.
            Debug.LogWarning($"[Server] Cast Ignored. IsFishing: {IsFishing}, CurrentBobber is null: {CurrentBobber == null}, bobberPrefab is null: {bobberPrefab == null}");
            return;
        }

        Transform rodTip = SpawnedRod?.GetComponentInChildren<RodInfo>()?.rodTip;
        Vector3 spawnPos = rodTip != null ? rodTip.position : transform.position;

        CurrentBobber = Runner.Spawn(bobberPrefab, spawnPos, Quaternion.identity, Object.InputAuthority);

        Rigidbody rb = CurrentBobber.GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.AddForce(force, ForceMode.Impulse);
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

    public void ReelIn(float reelAmount)
    {
        if (CurrentBobber == null || !HasInputAuthority) return;
        RPC_ReelIn(reelAmount);
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_ReelIn(float reelAmount)
    {
        if (CurrentBobber == null) return;

        Transform rodTip = SpawnedRod?.GetComponentInChildren<RodInfo>()?.rodTip;
        if (rodTip == null)
        {
            Debug.LogError("[Server] ReelIn 실패: 낚싯대 프리팹에서 'RodInfo' 컴포넌트 또는 'rodTip'을 찾을 수 없습니다!");
            return;
        }

        Rigidbody bobberRigidbody = CurrentBobber.GetComponent<Rigidbody>();
        if (bobberRigidbody == null)
        {
            Debug.LogError("[Server] ReelIn 실패: 찌(Bobber) 프리팹에 Rigidbody가 없습니다!");
            return;
        }

        bobberRigidbody.WakeUp();  // 리지드바디 깨우기 
        Vector3 newPosition = Vector3.MoveTowards(bobberRigidbody.position, rodTip.position, reelAmount);
        bobberRigidbody.MovePosition(newPosition);

        if (Vector3.Distance(bobberRigidbody.position, rodTip.position) < retrievalDistance)
        {
            Debug.Log("[Server] 찌 회수 완료!");
            Runner.Despawn(CurrentBobber);
            CurrentBobber = null;
        }
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
}