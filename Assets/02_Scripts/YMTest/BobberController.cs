using Fusion;
using UnityEngine;
using TMPro;

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

    [Header("UI 표시")]
    [Tooltip("찌 위에 표시될 Text (World Space Canvas)")]
    [SerializeField] private TextMeshProUGUI hitText;   // ★ 추가

    // --- 네트워크 동기화 변수 ---
    [Networked]
    public NetworkBool IsInFishingZone { get; set; }

    [Networked]
    public NetworkBool HasFishOn { get; set; }

    [Networked]
    public NetworkObject HookedFish { get; set; }

    [Networked]
    private TickTimer BitingTimer { get; set; }

    private FishingZone currentZone;

    // ★★★ MISS 처리를 위한 변수 추가 ★★★
    public float biteEndTime = -100f;

    // [HasFishOn]이 false로 변경될 때 꼭 이 함수로! (직접 = false 하지 마세요)
    public void SetHasFishOn(bool value)
    {
        if (HasFishOn && !value)
            biteEndTime = Time.time;
        HasFishOn = value;
    }
    // 입질 끝난 뒤 일정시간(MISS 윈도우) 체크
    public bool IsMissWindow(float window = 2f)
    {
        return (Time.time - biteEndTime) < window;
    }

    // 찌 위에 HIT/MISS 텍스트 띄우기 (플레이어에서 호출)
    public void ShowBobberText(string message, float duration)
    {
        if (hitText != null)
        {
            StopAllCoroutines();
            StartCoroutine(ShowTextRoutine(message, duration));
        }
    }

    private System.Collections.IEnumerator ShowTextRoutine(string message, float duration)
    {
        hitText.gameObject.SetActive(true);
        hitText.text = message;
        yield return new WaitForSeconds(duration);
        hitText.gameObject.SetActive(false);
    }

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
            // ▼▼▼ 입질 시 물고기를 즉시 스폰하고 비활성화 ▼▼▼
            if (currentZone != null && currentZone.availableFishPrefabs.Count > 0)
            {
                int randomIndex = Random.Range(0, currentZone.availableFishPrefabs.Count);
                GameObject fishPrefab = currentZone.availableFishPrefabs[randomIndex];
                HookedFish = Runner.Spawn(fishPrefab, transform.position, Quaternion.identity, Object.InputAuthority);

                if (HookedFish != null)
                {
                    HookedFish.gameObject.SetActive(false);
                    SetHasFishOn(true); // 꼭 SetHasFishOn 사용!
                    Debug.Log($"[Server] 입질 감지! 물고기 {fishPrefab.name} 스폰 및 비활성화.");
                }
            }
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
                SetHasFishOn(false); // 꼭 SetHasFishOn 사용!
                BitingTimer = default;

                if (HookedFish != null)
                {
                    Runner.Despawn(HookedFish);
                    HookedFish = null;
                }
                Debug.Log("[Server] 찌가 피싱존을 벗어남.");
            }
        }
    }
}
