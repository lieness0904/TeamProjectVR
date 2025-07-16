using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshPro를 사용하기 위해 필요합니다.

public class FishingUIManager : MonoBehaviour
{
    // 이 스크립트의 인스턴스를 어디서든 쉽게 접근할 수 있도록 static 변수를 만듭니다. (싱글톤 패턴)
    public static FishingUIManager Instance { get; private set; }

    [Header("UI 요소 연결")]
    [SerializeField] private GameObject fishingButtonObject; // 버튼 자체를 담을 게임 오브젝트
    [SerializeField] private Button fishingButton; // 버튼 컴포넌트
    [SerializeField] private TextMeshProUGUI buttonText; // 버튼의 텍스트 (TextMeshPro)

    private void Awake()
    {
        // 싱글톤 인스턴스 설정
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            // 이미 인스턴스가 존재하면 이 오브젝트는 파괴
            Destroy(gameObject);
            return;
        }

        // 버튼이 할당되어 있고, 클릭 리스너가 설정되지 않았다면 추가
        if (fishingButton != null)
        {
            fishingButton.onClick.AddListener(OnFishingButtonClick);
        }

        // 시작할 때는 버튼을 숨깁니다.
        if (fishingButtonObject != null)
        {
            fishingButtonObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // 오브젝트가 파괴될 때 리스너를 제거하여 메모리 누수를 방지합니다.
        if (fishingButton != null)
        {
            fishingButton.onClick.RemoveListener(OnFishingButtonClick);
        }
    }

    /// <summary>
    /// 낚시 버튼이 클릭되었을 때 호출될 함수
    /// </summary>
    private void OnFishingButtonClick()
    {
        // 로컬 플레이어의 PlayerFishingController를 찾아 낚시 상태 변경 함수를 호출합니다.
        if (PlayerFishingController.Local != null)
        {
            PlayerFishingController.Local.ToggleFishingState();
        }
    }

    /// <summary>
    /// PlayerFishingController가 호출할 함수. 낚시 상태에 따라 버튼의 텍스트를 변경합니다.
    /// </summary>
    /// <param name="isFishing">현재 낚시 중인지 여부</param>
    public void UpdateFishingButton(bool isFishing)
    {
        if (buttonText != null)
        {
            buttonText.text = isFishing ? "그만하기" : "낚시하기";
        }
    }

    /// <summary>
    /// 낚시 버튼을 화면에 보여줍니다.
    /// </summary>
    public void ShowButton()
    {
        if (fishingButtonObject != null)
        {
            fishingButtonObject.SetActive(true);
        }
    }

    /// <summary>
    /// 낚시 버튼을 화면에서 숨깁니다.
    /// </summary>
    public void HideButton()
    {
        if (fishingButtonObject != null)
        {
            fishingButtonObject.SetActive(false);
        }
    }
}