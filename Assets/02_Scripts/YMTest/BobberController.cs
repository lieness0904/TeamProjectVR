using Fusion;
using UnityEngine;
using TMPro;
using System.Collections.Generic; // List<T>를 사용하기 위해 추가

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
    [SerializeField] private TextMeshProUGUI hitText;

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

    // MISS 처리를 위한 변수
    public float biteEndTime = -100f;

    public void SetHasFishOn(bool value)
    {
        if (HasFishOn && !value)
            biteEndTime = Time.time;
        HasFishOn = value;
    }

    public bool IsMissWindow(float window = 2f)
    {
        return (Time.time - biteEndTime) < window;
    }

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
            if (currentZone != null && currentZone.availableFishPrefabs.Count > 0)
            {
                // --- [수정됨] 가중치에 따라 물고기를 선택하는 새 함수 호출 ---
                GameObject fishPrefab = SelectFishByWeight(currentZone.availableFishPrefabs);

                if (fishPrefab != null)
                {
                    HookedFish = Runner.Spawn(fishPrefab, transform.position, Quaternion.identity, Object.InputAuthority);

                    if (HookedFish != null)
                    {
                        HookedFish.gameObject.SetActive(false);
                        SetHasFishOn(true);
                        Debug.Log($"[Server] 입질 감지! 물고기 {fishPrefab.name} 스폰 및 비활성화.");
                    }
                }
            }
            BitingTimer = default;
        }
    }

    // --- [새로 추가된 함수] 확률 가중치에 따라 물고기를 선택 ---
    private GameObject SelectFishByWeight(List<GameObject> fishPrefabs)
    {
        if (fishPrefabs == null || fishPrefabs.Count == 0) return null;

        int totalWeight = 0;
        // 1. 모든 물고기의 확률 가중치를 더해 총합을 구합니다.
        foreach (var prefab in fishPrefabs)
        {
            if (prefab.TryGetComponent<FishData>(out var fishData))
            {
                totalWeight += fishData.appearanceWeight;
            }
        }

        // 모든 가중치가 0인 경우 등 예외 처리
        if (totalWeight <= 0)
        {
            // 가중치 계산이 불가능하면 기존처럼 완전 랜덤으로 하나를 선택
            Debug.LogWarning("모든 물고기의 appearanceWeight가 0이라 일반 랜덤으로 전환합니다.");
            return fishPrefabs[Random.Range(0, fishPrefabs.Count)];
        }

        // 2. 0부터 가중치 총합 사이의 랜덤한 숫자를 뽑습니다.
        int randomPoint = Random.Range(0, totalWeight);

        // 3. 물고기 목록을 순회하며 랜덤 숫자가 어느 물고기의 구간에 속하는지 찾습니다.
        foreach (var prefab in fishPrefabs)
        {
            if (prefab.TryGetComponent<FishData>(out var fishData))
            {
                if (randomPoint < fishData.appearanceWeight)
                {
                    // 현재 물고기 구간에 당첨!
                    return prefab;
                }
                else
                {
                    // 당첨되지 않았으면, 현재 물고기의 가중치만큼 랜덤 숫자를 줄여 다음 구간과 비교합니다.
                    randomPoint -= fishData.appearanceWeight;
                }
            }
        }

        // 혹시 모를 오류 발생 시 마지막 물고기를 반환합니다.
        return fishPrefabs[fishPrefabs.Count - 1];
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
            if (zone == currentZone) // 벗어난 구역이 현재 구역과 같을 때만 처리
            {
                currentZone = null;
                if (HasStateAuthority)
                {
                    IsInFishingZone = false;
                    SetHasFishOn(false);
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
}