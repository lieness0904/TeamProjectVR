using Fusion;
using UnityEngine;

// 이 스크립트는 Rigidbody와 NetworkObject가 반드시 필요합니다.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(NetworkObject))]
// ▼▼▼▼▼ [수정된 부분] MonoBehaviour -> NetworkBehaviour 로 변경 ▼▼▼▼▼
public class BobberController : NetworkBehaviour
// ▲▲▲▲▲ [수정된 부분] MonoBehaviour -> NetworkBehaviour 로 변경 ▲▲▲▲▲
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
    // State Authority(서버/호스트)만이 이 값들을 변경할 수 있습니다.
    [Networked]
    public NetworkBool IsInFishingZone { get; set; }

    [Networked]
    public NetworkBool HasFishOn { get; set; }

    [Networked]
    private TickTimer BitingTimer { get; set; }

    // 모든 클라이언트에서 찌의 렌더링을 업데이트하기 위해 Render()를 사용합니다.
    public override void Render()
    {
        if (bobberRenderer != null)
        {
            // HasFishOn 상태에 따라 찌의 색깔을 즉시 변경합니다.
            bobberRenderer.material.color = HasFishOn ? biteColor : defaultColor;
        }
    }

    // FixedUpdateNetwork는 Fusion의 네트워크 로직을 위한 메인 업데이트 함수입니다.
    public override void FixedUpdateNetwork()
    {
        // 핵심 로직은 반드시 State Authority(서버/호스트)에서만 실행되어야 합니다.
        if (!HasStateAuthority) return;

        // 찌가 피싱존에 있고, 아직 물고기가 안 물었고, 타이머가 돌고 있지 않다면
        if (IsInFishingZone && !HasFishOn && !BitingTimer.IsRunning)
        {
            // 랜덤한 대기 시간으로 타이머를 생성하고 시작합니다.
            float waitTime = Random.Range(minWaitTime, maxWaitTime);
            BitingTimer = TickTimer.CreateFromSeconds(Runner, waitTime);
            Debug.Log($"[Server] 찌가 피싱존에 진입. {waitTime:F1}초 후 입질 예정.");
        }

        // 타이머가 만료되었는지 확인합니다.
        if (BitingTimer.Expired(Runner))
        {
            Debug.Log("[Server] 입질 감지!");
            HasFishOn = true; // 입질 상태를 true로 변경 (모든 클라이언트에 동기화됨)

            // 타이머를 리셋합니다.
            BitingTimer = default;
        }
    }

    // --- Unity 물리 콜백 ---

    private void OnTriggerEnter(Collider other)
    {
        // 상태 변경은 State Authority에서만 처리합니다.
        if (!HasStateAuthority) return;

        // FishingZone 컴포넌트를 가진 오브젝트와 충돌했는지 확인합니다.
        if (other.GetComponent<FishingZone>() != null)
        {
            IsInFishingZone = true;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 상태 변경은 State Authority에서만 처리합니다.
        if (!HasStateAuthority) return;

        if (other.GetComponent<FishingZone>() != null)
        {
            // 피싱존을 나가면 모든 상태를 초기화합니다.
            IsInFishingZone = false;
            HasFishOn = false;
            BitingTimer = default;
            Debug.Log("[Server] 찌가 피싱존을 벗어남.");
        }
    }
}