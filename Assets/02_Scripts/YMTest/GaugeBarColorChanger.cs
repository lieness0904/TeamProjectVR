using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 게이지 슬라이더의 Fill 색상을 value에 따라 녹색-노랑-빨강으로 자동 변경
/// - 슬라이더의 min/max value가 0~1이든 0~100이든 자동 처리
/// - Fill Image 연결은 Inspector에서 하거나, 컴포넌트 자동 할당
/// </summary>
[RequireComponent(typeof(Slider))]
public class GaugeBarColorChanger : MonoBehaviour
{
    public Slider gaugeSlider; // Inspector 연결 가능
    public Image fillImage;    // Fill(Bar) 이미지

    void Reset()
    {
        // Inspector에서 연결 안 했다면 자동 할당
        if (gaugeSlider == null)
            gaugeSlider = GetComponent<Slider>();
        if (fillImage == null && gaugeSlider != null && gaugeSlider.fillRect != null)
            fillImage = gaugeSlider.fillRect.GetComponent<Image>();
    }

    void Awake()
    {
        if (gaugeSlider == null)
            gaugeSlider = GetComponent<Slider>();
        if (fillImage == null && gaugeSlider != null && gaugeSlider.fillRect != null)
            fillImage = gaugeSlider.fillRect.GetComponent<Image>();
    }

    void Update()
    {
        if (gaugeSlider == null || fillImage == null) return;

        float value = gaugeSlider.value;
        float min = gaugeSlider.minValue;
        float max = gaugeSlider.maxValue;

        // 슬라이더가 0~1 구간이면 변환
        float normalized = (max > min) ? (value - min) / (max - min) : 0f;
        // normalized: 0~1

        if (normalized <= 0.5f)
        {
            fillImage.color = Color.green;
        }
        else if (normalized <= 0.8f)
        {
            fillImage.color = Color.yellow;
        }
        else
        {
            fillImage.color = Color.red;
        }
    }
}
