using Fusion;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
public class BobberController : NetworkBehaviour
{
    [Header("입질 설정")]
    [Tooltip("입질이 오기까지 최소 대기 시간")]
    [SerializeField] private float minWaitTime = 5f;
    [Tooltip("입질이 오기까지 최대 대기 시간")]
    [SerializeField] private float maxWaitTime = 15f;

    [Header("시각 효과")]
    [Tooltip("찌의 색깔을 바꿀 MeshRenderer")]
    [SerializeField] private MeshRenderer bobberRenderer;
    [Tooltip("평상시 찌 색깔")]
    [SerializeField] private Color defaultColor = Color.white;
    [Tooltip("입질이 왔을 때 찌 색깔")]
    [SerializeField] private Color biteColor = Color.red;

    // --- 네트워크 동기화 변수 ---
    [Networked]
    public NetworkBool IsInFishingZone { get; set; }

    [Networked]
    public NetworkBool HasFishOn { get; set; }

    // ▼▼▼ [수정된 변수] 프리팹 ID 대신, 스폰된 물고기 오브젝트 자체를 저장합니다. ▼▼▼
    [Networked]
    public NetworkObject HookedFish { get; set; }
    // ▲▲▲ [수정된 변수] ▲▲▲

    [Networked]
    private TickTimer BitingTimer { get; set; }

    private FishingZone currentZone;

    public override void Render()
    {
        if (bobberRenderer != null)
        {
            bobberRenderer.material.color = HasFishOn ? biteColor : defaultColor;
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority) return;

        if (IsInFishingZone && !HasFishOn && !BitingTimer.IsRunning)
        {
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            BitingTimer = TickTimer.CreateFromSeconds(Runner, waitTime);
        }

        if (BitingTimer.Expired(Runner))
        {
            // ▼▼▼ [완전히 변경된 로직] 입질 시 물고기를 즉시 스폰하고 비활성화합니다. ▼▼▼
            if (currentZone != null && currentZone.availableFishPrefabs.Count > 0)
            {
                // 1. 낚을 수 있는 물고기 목록에서 랜덤으로 프리팹 선택
                int randomIndex = Random.Range(0, currentZone.availableFishPrefabs.Count);
                GameObject fishPrefab = currentZone.availableFishPrefabs[randomIndex];

                // 2. 선택된 프리팹을 즉시 스폰
                HookedFish = Runner.Spawn(fishPrefab, transform.position, Quaternion.identity, Object.InputAuthority);

                // 3. 스폰된 물고기가 있고, 성공적으로 초기화되면
                if (HookedFish != null)
                {
                    // 4. 즉시 비활성화하여 숨겨둠 (챔질 성공 시 활성화할 예정)
                    HookedFish.gameObject.SetActive(false);

                    // 5. 입질 상태를 true로 변경
                    HasFishOn = true;
                    Debug.Log($"[Server] 입질 감지! 물고기 {fishPrefab.name} 스폰 및 비활성화.");
                }
            }
            // ▲▲▲ [완전히 변경된 로직] ▲▲▲

            BitingTimer = default;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<FishingZone>(out var zone))
        {
            currentZone = zone;
            if (HasStateAuthority)
            {
                IsInFishingZone = true;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.TryGetComponent<FishingZone>(out var zone))
        {
            currentZone = null;
            if (HasStateAuthority)
            {
                IsInFishingZone = false;
                HasFishOn = false;
                BitingTimer = default;

                // ▼▼▼ [추가된 로직] 챔질하지 않고 존을 벗어날 경우, 숨겨둔 물고기를 파괴하여 정리합니다. ▼▼▼
                if (HookedFish != null)
                {
                    Runner.Despawn(HookedFish);
                    HookedFish = null;
                }
                // ▲▲▲ [추가된 로직] ▲▲▲

                Debug.Log("[Server] 찌가 피싱존을 벗어남.");
            }
        }
    }
}