using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ShopItemSlot : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI nameText, categoryText, descText, priceText;
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_InputField quantityInput;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button plusButton;
    [SerializeField] private Button minusButton;

    private ItemData currentItem;

    private void Start()
    {
        plusButton.onClick.AddListener(PlusQuantity);
        minusButton.onClick.AddListener(MinusQuantity);

        // 안전장치: 초기 값 없으면 디폴트 1 넣기
        if (string.IsNullOrEmpty(quantityInput.text))
            quantityInput.text = "1";
    }

    public void Setup(ItemData data)
    {
        currentItem = data;
        nameText.text = "이름 : " + data.name;
        categoryText.text = "타입 : " + EnumHelper.GetDescription(data.itemType);
        descText.text = "설명 : " + data.description;
        priceText.text = "가격 : " + data.price + "포인트";

        // 아이콘 로드 예외 처리
        Sprite icon = Resources.Load<Sprite>(data.iconPath);
        if (icon == null)
            Debug.LogWarning($"아이콘 로드 실패: {data.iconPath}");
        iconImage.sprite = icon;

        buyButton.onClick.RemoveAllListeners(); // 기존 리스너 제거
        buyButton.onClick.AddListener(() =>
        {
            int quantity = int.TryParse(quantityInput.text, out int q) ? q : 1;
            ShopManager shop = FindObjectOfType<ShopManager>();
            shop.TryBuyItem(currentItem, quantity);
        });
    }
    private void PlusQuantity()
    {
        int val = int.TryParse(quantityInput.text, out int result) ? result : 1;
        quantityInput.text = (val + 1).ToString();
    }
    private void MinusQuantity()
    {
        int val = int.TryParse(quantityInput.text, out int result) ? result : 1;
        val = Mathf.Max(1, val - 1); // 1 이하로 내려가지 않게 막기
        quantityInput.text = val.ToString();
    }
}
