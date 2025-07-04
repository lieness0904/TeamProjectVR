using UnityEngine;

public class FishingDetector : MonoBehaviour
{
    [Header("낚시 제안 UI")]
    public GameObject fishingPromptUI; // 인스펙터에서 Canvas를 연결합니다.

    void Start()
    {
        // 시작할 때는 UI를 비활성화합니다.
        if (fishingPromptUI != null)
        {
            fishingPromptUI.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 들어온 트리거의 레이어가 FishingZone이면
        if (other.gameObject.layer == LayerMask.NameToLayer("FishingZone"))
        {
            Debug.Log("낚시 구역 진입");
            if (fishingPromptUI != null)
            {
                fishingPromptUI.SetActive(true);
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        // 나간 트리거의 레이어가 FishingZone이면
        if (other.gameObject.layer == LayerMask.NameToLayer("FishingZone"))
        {
            Debug.Log("낚시 구역 이탈");
            if (fishingPromptUI != null)
            {
                fishingPromptUI.SetActive(false);
            }
        }
    }
}