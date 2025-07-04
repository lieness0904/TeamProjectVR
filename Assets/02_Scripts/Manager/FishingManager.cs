using UnityEngine;

public class FishingManager : MonoBehaviour
{
    public static FishingManager instance;

    [Header("낚시 관련 오브젝트")]
    public GameObject fishingRodObject; // 씬에 배치된 낚싯대를 여기에 연결
    public Transform rodTip;
    public GameObject bobberPrefab;
    public LayerMask fishingZoneLayer;

    private GameObject currentBobber;
    private LineRenderer fishingLine;
    private bool isFishing = false;

    void Awake()
    {
        if (instance == null) instance = this;
    }

    void Start()
    {
        // 게임 시작 시 낚싯대를 비활성화
        if (fishingRodObject != null)
        {
            fishingRodObject.SetActive(false);
        }
    }

    // '낚시하기' 버튼이 호출할 새로운 함수
    public void EnterFishingMode()
    {
        // 낚싯대를 활성화하고 UI를 숨김
        if (fishingRodObject != null)
        {
            fishingRodObject.SetActive(true);
        }
        FindObjectOfType<FishingDetector>().fishingPromptUI.SetActive(false);
    }

    // 실제 캐스팅을 실행하는 함수 (CastingController가 호출)
    public void CastBobber(Vector3 castVelocity)
    {
        currentBobber = Instantiate(bobberPrefab, rodTip.position, Quaternion.identity);
        Rigidbody bobberRb = currentBobber.GetComponent<Rigidbody>();

        // 계산된 힘으로 찌를 발사!
        bobberRb.AddForce(castVelocity, ForceMode.Impulse);

        fishingLine = rodTip.GetComponentInParent<LineRenderer>();
        if (fishingLine != null)
        {
            fishingLine.enabled = true;
            isFishing = true;
        }
    }

    void Update()
    {
        if (isFishing && fishingLine != null && currentBobber != null && rodTip != null)
        {
            fishingLine.SetPosition(0, rodTip.position);
            fishingLine.SetPosition(1, currentBobber.transform.position);
        }
    }

    public void CancelFishing()
    {
        if (fishingRodObject != null) fishingRodObject.SetActive(false);
        if (currentBobber != null) Destroy(currentBobber);
        if (fishingLine != null) fishingLine.enabled = false;
        isFishing = false;
    }
}