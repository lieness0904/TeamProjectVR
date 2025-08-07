using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 잡은 물고기의 정보를 UI 패널에 표시하고, 일정 시간 후 숨기는 역할을 합니다.
/// 이 스크립트는 NetworkedBobber 프리팹에 추가되어야 합니다.
/// </summary>
public class CaughtFishUI : MonoBehaviour
{
    [Header("UI 요소 연결")]
    [Tooltip("물고기 정보가 표시될 부모 패널 오브젝트")]
    public GameObject fishInfoPanel;

    //[Tooltip("물고기 이미지를 표시할 Image 컴포넌트 (선택 사항)")]
    //public Image fishImage;

    [Tooltip("물고기 이름을 표시할 TextMeshProUGUI")]
    public TextMeshProUGUI fishNameText;

    [Tooltip("물고기 무게를 표시할 TextMeshProUGUI")]
    public TextMeshProUGUI fishWeightText;

    [Tooltip("물고기 가격을 표시할 TextMeshProUGUI")]
    public TextMeshProUGUI fishPriceText;

    // 스크립트가 처음 활성화될 때 UI가 보이지 않도록 확실히 숨겨줍니다.
    private void Awake()
    {
        if (fishInfoPanel != null)
        {
            fishInfoPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 외부(PlayerFishingController)에서 호출할 메인 함수. 물고기 정보를 UI에 표시합니다.
    /// </summary>
    /// <param name="fishData">표시할 물고기의 데이터</param>
    /// <param name="duration">UI가 표시될 시간(초)</param>
    public void ShowFishInfo(FishData fishData, float duration)
    {
        // 필수 요소들이 없으면 함수를 실행하지 않습니다.
        if (fishInfoPanel == null || fishData == null)
        {
            Debug.LogError("FishInfoPanel 또는 fishData가 할당되지 않았습니다!");
            return;
        }

        // 이전에 실행되던 숨김 코루틴이 있다면 중지시킵니다.
        StopAllCoroutines();

        // 패널을 활성화하여 화면에 보여줍니다.
        fishInfoPanel.SetActive(true);

        // 전달받은 fishData를 사용해 각 UI 텍스트를 설정합니다.
        if(fishNameText != null) fishNameText.text = fishData.fishName;
        if (fishWeightText != null) fishWeightText.text = $"무게: {fishData.finalWeight * 1f:F2} kg"; // 소수점 2자리까지 표시
        if (fishPriceText != null) fishPriceText.text = $"가격: {fishData.finalPrice} G";

        // TODO: 물고기 이미지가 있다면 여기서 설정합니다.
        // 예: if(fishImage != null) fishImage.sprite = fishData.fishSprite;

        // 일정 시간 후에 패널을 자동으로 숨기는 코루틴을 시작합니다.
        StartCoroutine(HidePanelAfterDelay(duration));
    }

    /// <summary>
    /// 지정된 시간(delay)만큼 기다렸다가 패널을 비활성화하는 코루틴입니다.
    /// </summary>
    private IEnumerator HidePanelAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (fishInfoPanel != null)
        {
            fishInfoPanel.SetActive(false);
        }
    }
}