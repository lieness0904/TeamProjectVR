using UnityEngine;
using Fusion;

[RequireComponent(typeof(RiggingManager))]
public class PlayerFishingController : NetworkBehaviour
{
    [Header("낚시 설정")]
    [SerializeField] private NetworkObject fishingRodPrefab;

    [Networked]
    public NetworkBool IsFishing { get; set; }

    [Networked]
    private NetworkObject SpawnedRod { get; set; }

    private RiggingManager _riggingManager;
    // --- [추가] ---
    // 로컬 플레이어의 PlayerFishingController를 쉽게 찾을 수 있도록 static 변수 추가
    public static PlayerFishingController Local { get; private set; }

    private void Awake()
    {
        _riggingManager = GetComponent<RiggingManager>();
    }

    public override void Spawned()
    {
        // 이 오브젝트가 로컬 플레이어의 것이라면, static 변수에 자기 자신을 할당합니다.
        if (HasInputAuthority)
        {
            Local = this;
        }
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        // 오브젝트가 파괴될 때 static 참조를 비워줍니다.
        if (Local == this)
        {
            Local = null;
        }
    }

    // "낚시하기/그만하기" 버튼을 눌렀을 때 UI 매니저가 호출할 함수입니다.
    public void ToggleFishingState()
    {
        if (HasInputAuthority)
        {
            RPC_SetFishingState(!IsFishing);
        }
    }

    // [Client] -> [Server] : 클라이언트가 서버에게 낚시 상태 변경을 요청
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetFishingState(NetworkBool isFishing)
    {
        // 서버에서 플레이어의 낚시 상태를 변경합니다.
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
            if (SpawnedRod != null)
            {
                Runner.Despawn(SpawnedRod);
                SpawnedRod = null;
            }
        }

        // --- [핵심 변경사항] ---
        // 서버는 상태 변경 후, 모든 클라이언트에게 시각적 업데이트를 하라고 다시 RPC를 보냅니다.
        RPC_UpdateVisuals(isFishing, SpawnedRod);
    }

    // [Server] -> [All Clients] : 서버가 모든 클라이언트에게 시각적 업데이트를 명령
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_UpdateVisuals(NetworkBool isFishing, NetworkObject rod)
    {
        // 이 RPC를 받은 모든 클라이언트는 낚싯대를 손에 붙이는 로직을 실행합니다.
        AttachRodToHand(isFishing, rod);

        // 이 RPC를 받은 클라이언트 중, 로컬 플레이어만 UI를 업데이트합니다.
        if (HasInputAuthority)
        {
            // FishingUIManager 스크립트는 다음에 만들 예정입니다.
            // FindObjectOfType<FishingUIManager>()?.UpdateFishingButton(isFishing);
        }
    }

    // 스폰된 낚싯대를 오른손에 부착하는 로직
    private void AttachRodToHand(bool isFishing, NetworkObject rod)
    {
        if (isFishing && rod != null)
        {
            // 낚시 중이고 낚싯대가 유효하다면 오른손을 부모로 설정합니다.
            Transform rightHand = _riggingManager.rightHandController;
            if (rightHand != null)
            {
                rod.transform.SetParent(rightHand, false);
                rod.transform.localPosition = Vector3.zero;
                rod.transform.localRotation = Quaternion.identity;
            }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        // 이 로직은 로컬 플레이어에게만 작동해야 합니다.
        if (!HasInputAuthority)
        {
            return;
        }

        // 들어온 트리거의 태그가 "CastingZone"이면
        if (other.CompareTag("CastingZone"))
        {
            // UI 매니저에게 버튼을 보여달라고 요청합니다.
            FishingUIManager.Instance?.ShowButton();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 이 로직은 로컬 플레이어에게만 작동해야 합니다.
        if (!HasInputAuthority)
        {
            return;
        }

        // 나간 트리거의 태그가 "CastingZone"이면
        if (other.CompareTag("CastingZone"))
        {
            // UI 매니저에게 버튼을 숨겨달라고 요청합니다.
            FishingUIManager.Instance?.HideButton();
        }
    }
}