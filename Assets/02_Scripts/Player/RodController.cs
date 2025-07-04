using UnityEngine;

public class RodController : MonoBehaviour
{
    [Header("낚싯대 프리팹")]
    public GameObject fishingRodPrefab;

    private GameObject currentRod;

    // Start 대신 Awake를 사용하여 더 일찍 실행되게 합니다.
    void Awake()
    {
        // 1. FishingManager 인스턴스가 있는지 확인합니다.
        if (FishingManager.instance == null)
        {
            Debug.LogError("FishingManager.instance를 찾지 못했습니다!");
            return;
        }

        // 2. 낚싯대 프리팹이 할당되었는지 확인합니다.
        if (fishingRodPrefab != null)
        {
            // 3. 낚싯대를 생성합니다.
            currentRod = Instantiate(fishingRodPrefab, transform);
            currentRod.transform.localPosition = new Vector3(0, -0.05f, 0.5f);
            currentRod.transform.localRotation = Quaternion.Euler(45f, 0, 0);

            // 4. 생성된 낚싯대에서 RodInfo 컴포넌트를 가져옵니다.
            RodInfo rodInfo = currentRod.GetComponentInChildren<RodInfo>();

            // 5. RodInfo와 그 안의 rodTip이 유효하다면, FishingManager에 할당합니다.
            if (rodInfo != null && rodInfo.rodTip != null)
            {
                FishingManager.instance.rodTip = rodInfo.rodTip;
                Debug.Log("성공: RodInfo를 통해 RodTip이 FishingManager에 할당되었습니다!");
            }
            else
            {
                Debug.LogError("실패: 생성된 낚싯대 프리팹에서 RodInfo 컴포넌트나 그 안의 RodTip을 찾을 수 없습니다.");
            }
        }
    }
}