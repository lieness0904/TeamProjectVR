using UnityEngine;

public class FishData : MonoBehaviour
{
    [Header("기본 정보 (프리팹 설정용)")]
    [Tooltip("물고기의 고유 ID (나중에 데이터 저장용)")]
    public int fishID;

    [Tooltip("물고기 이름")]
    public string fishName;

    [Tooltip("이 물고기 종류의 기본 무게")]
    public float baseWeight = 1.0f;

    [Tooltip("물고기 힘 (게이지 증가량에 영향을 줌)")]
    public float strength = 1.0f;

    [Tooltip("물고기의 기본 가격")]
    public int basePrice;

    [Header("크기 랜덤 설정")]
    [Tooltip("물고기 크기의 최소 배율 (예: 0.75 = 75%)")]
    public float minSizeMultiplier = 0.75f;

    [Tooltip("물고기 크기의 최대 배율 (예: 1.25 = 125%)")]
    public float maxSizeMultiplier = 1.25f;

    // --- 생성된 개체의 최종 정보 (코드를 통해 계산됨) ---
    // [HideInInspector]를 사용해 인스펙터 창에서는 보이지 않게 처리합니다.
    [HideInInspector] public float finalSizeMultiplier;
    [HideInInspector] public float finalWeight;
    [HideInInspector] public int finalPrice;

    /// <summary>
    /// 물고기가 스폰될 때 호출되어 최종 크기, 무게, 가격을 결정합니다.
    /// </summary>
    public void InitializeFish()
    {
        // 1. 최소/최대 배율 사이에서 랜덤한 최종 크기 배율을 결정합니다.
        finalSizeMultiplier = Random.Range(minSizeMultiplier, maxSizeMultiplier);

        // 2. 게임오브젝트의 스케일을 최종 크기 배율만큼 조절합니다.
        transform.localScale *= finalSizeMultiplier;

        // 3. 최종 무게와 가격을 계산합니다.
        finalWeight = baseWeight * finalSizeMultiplier;
        finalPrice = (int)(basePrice * finalSizeMultiplier);
    }
}