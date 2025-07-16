using UnityEngine;
using Fusion;
using UnityEngine.InputSystem; // InputAction을 직접 사용하기 위해 추가

[RequireComponent(typeof(RiggingManager))]
public class PlayerFishingController : NetworkBehaviour
{
    [Header("프리팹 연결")]
    [SerializeField] private NetworkObject fishingRodPrefab;
    [SerializeField] private NetworkObject bobberPrefab;

    [Header("컴포넌트 연결")]
    // 오른손 컨트롤러에 있는 CastingHandler를 여기에 연결해야 합니다.
    [SerializeField] private CastingHandler castingHandler;

    [Networked]
    public NetworkBool IsFishing { get; set; }

    [Networked]
    private NetworkObject SpawnedRod { get; set; }

    [Networked]
    public NetworkObject CurrentBobber { get; private set; }

    private RiggingManager _riggingManager;
    public static PlayerFishingController Local { get; private set; }

    private void Awake()
    {
        _riggingManager = GetComponent<RiggingManager>();
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
            // ▼▼▼▼▼ [핵심 최종 수정] ▼▼▼▼▼
            // 클라이언트 측의 IsFishing 확인 로직을 제거하여 타이밍 문제를 해결합니다.
            if (castingHandler != null && castingHandler.castAction.action.WasReleasedThisFrame())
            {
                // CastingHandler에 저장된 마지막 캐스팅 힘으로 RPC를 호출합니다.
                RPC_CastBobber(castingHandler.LastCastVelocity);
            }
            // ▲▲▲▲▲ [핵심 최종 수정] ▲▲▲▲▲
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
        // 최종 결정은 서버가 내립니다. 낚시 중이 아니면 찌를 생성하지 않습니다.
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
        if (isFishing && rod != null)
        {
            Transform rightHand = _riggingManager.rightHandController;
            if (rightHand != null)
            {
                rod.transform.SetParent(rightHand, false);
                rod.transform.localPosition = new Vector3(0, 0, 1f);
                rod.transform.localRotation = Quaternion.identity;
            }
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