using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class InventoryPanelCtrl : MonoBehaviour
{
    public int slotsPerPage = 9;
    public int totalItems = 27;
    public ItemType panelType; // 인스펙터에서 알맞게 설정

    private int currentPage = 0;
    private List<InventoryItem> currentItems = new();
    private InventoryItem currentItem;

    [Header("Item Info Panel")]
    [SerializeField] private GameObject itemInfoPanel;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI typeText;
    [SerializeField] private TextMeshProUGUI descText;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button dropButton;
    [SerializeField] private Button closeButton;

    [Header("Confirm Panel")]
    [SerializeField] private GameObject confirmPanel;
    [SerializeField] private Image confirmIcon;
    [SerializeField] private TextMeshProUGUI confirmNameText;
    [SerializeField] private TextMeshProUGUI confirmAmountText;
    [SerializeField] private Slider amountSlider;
    [SerializeField] private Button confirmSellButton;
    [SerializeField] private Button confirmDropButton;
    [SerializeField] private Button confirmCancelButton;

    public void NextPage()
    {
        if (currentPage < GetMaxPage())
        {
            ShowPage(currentPage + 1);
            UpdateSlots();
        }
    }
    public void PrevPage()
    {
        if (currentPage > 0)
        {
            ShowPage(currentPage - 1);
            UpdateSlots();
        }
    }
    private int GetMaxPage()
    {
        return Mathf.CeilToInt((float)totalItems / slotsPerPage) - 1;
    }

    public void ShowItem(List<InventoryItem> allItems)
    {
        ActivateOnlyCurrentPanel();

        var filteredItems = InventoryHelper.FilterByType(allItems, panelType);
        List<InventoryItem> expandedItems = new();

        foreach (var item in filteredItems)
        {
            if (panelType == ItemType.Equipment)
            {
                for (int i = 0; i < item.amount; i++)
                    expandedItems.Add(new InventoryItem(item.id, 1));
            }
            else
            {
                expandedItems.Add(item);
            }
        }

        currentItems = expandedItems;

        // 방어 코드 
        if (currentItems.Count == 0)
        {
            Debug.LogWarning("[InventoryPanelCtrl] currentItems가 비어있어 업데이트 생략");
            ClearAllSlots(); // 슬롯만 초기화
            return;
        }

        ShowPage(0);
        UpdateSlots();
    }


    public void ShowPage(int pageIndex)
    {
        int maxPage = Mathf.CeilToInt((float)currentItems.Count / slotsPerPage) - 1;
        currentPage = Mathf.Clamp(pageIndex, 0, maxPage);
        Debug.Log($"[InventoryPanelCtrl] 페이지 {currentPage + 1} / {maxPage + 1}");
    }

    private void UpdateSlots()
    {
        Transform grid = transform.Find("InventoryGrid");
        if (grid == null)
        {
            Debug.LogError("[InventoryPanelCtrl] InventoryGrid가 없습니다.");
            return;
        }

        List<Transform> slots = new();
        foreach (Transform child in grid)
            slots.Add(child);

        int startIndex = currentPage * slotsPerPage;

        for (int i = 0; i < slotsPerPage; i++)
        {
            if (i >= slots.Count) break;

            Transform slot = slots[i];
            ClearSlot(slot);

            int itemIndex = startIndex + i;

            if (itemIndex < 0 || itemIndex >= currentItems.Count)
                continue;

            var item = currentItems[itemIndex];
            Sprite sprite = GetSpriteById(item.id);

            if (sprite != null)
            {
                AddItemIconToSlot(slot, sprite, item.amount);
                AddClickListenerToSlot(slot, item); 
            }
        }
    }
    private void ClearSlot(Transform slot)
    {
        Transform itemSlotObj = slot.Find("ItemSlot");
        if (itemSlotObj != null)
        {
            itemSlotObj.gameObject.SetActive(false); 
        }

        Transform amountTextObj = slot.Find("AmountText");
        if (amountTextObj != null)
        {
            amountTextObj.gameObject.SetActive(false);
        }
    }
    private void ClearAllSlots()
    {
        Transform grid = transform.Find("InventoryGrid");
        if (grid == null) return;

        foreach (Transform child in grid)
            ClearSlot(child);
    }

    private void AddItemIconToSlot(Transform slot, Sprite sprite, int amount)
    {
        Debug.Log($"[AddItemIconToSlot] sprite: {sprite}, amount: {amount}");

        Transform itemSlotObj = slot.Find("ItemSlot");
        if (itemSlotObj != null)
        {
            itemSlotObj.gameObject.SetActive(true); 
            Image iconImage = itemSlotObj.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = sprite;
                iconImage.color = new Color(1, 1, 1, 1);
                iconImage.enabled = true;
            }
        }

        Transform amountTextObj = slot.Find("AmountText");
        if (amountTextObj != null)
        {
            TextMeshProUGUI amountText = amountTextObj.GetComponent<TextMeshProUGUI>();
            if (panelType != ItemType.Equipment && amount > 1)
            {
                amountTextObj.gameObject.SetActive(true); 
                amountText.text = "" + amount;
                amountText.enabled = true;
            }
            else
            {
                amountTextObj.gameObject.SetActive(false); 
            }
        }
    }
    private void AddClickListenerToSlot(Transform slot, InventoryItem item)
    {
        Button button = slot.GetComponent<Button>();
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => ShowItemInfo(item));
        }
    }
    private void ShowItemInfo(InventoryItem item)
    {
        var data = ItemDataLoader.Instance?.LoadedItems?.Find(x => x.id == item.id);
        if (data == null) return;

        itemInfoPanel.SetActive(true);
        iconImage.sprite = Resources.Load<Sprite>(data.iconPath);
        nameText.text = "이름 : " + data.name;
        typeText.text = "타입 : " + data.itemType.ToString();
        descText.text = "설명 : " + data.description;

        sellButton.onClick.RemoveAllListeners();
        dropButton.onClick.RemoveAllListeners();
        closeButton.onClick.RemoveAllListeners();

        sellButton.onClick.AddListener(() => ShowConfirmPanel(item, true));
        dropButton.onClick.AddListener(() => ShowConfirmPanel(item, false));
        closeButton.onClick.AddListener(CloseInfo);

        currentItem = item;
    }
    private void ShowConfirmPanel(InventoryItem item, bool isSell)
    {
        itemInfoPanel.SetActive(false);

        var data = ItemDataLoader.Instance?.LoadedItems?.Find(x => x.id == item.id);
        if (data == null) return;

        confirmPanel.SetActive(true);
        confirmIcon.sprite = Resources.Load<Sprite>(data.iconPath);
        confirmNameText.text = "이름 : " + data.name;

        amountSlider.minValue = 1;
        amountSlider.maxValue = item.amount;
        amountSlider.value = 1;

        confirmAmountText.text = "수량 : 1"; 
        // 슬라이더 값 변경될 때 수량 텍스트 갱신
    amountSlider.onValueChanged.RemoveAllListeners();
    amountSlider.onValueChanged.AddListener((value) =>
    {
        confirmAmountText.text = $"수량 : {Mathf.RoundToInt(value)}";
    });

        confirmSellButton.gameObject.SetActive(isSell);
        confirmDropButton.gameObject.SetActive(!isSell);

        confirmSellButton.onClick.RemoveAllListeners();
        confirmDropButton.onClick.RemoveAllListeners();
        confirmCancelButton.onClick.RemoveAllListeners();

        confirmSellButton.onClick.AddListener(() => ConfirmSell((int)amountSlider.value));
        confirmDropButton.onClick.AddListener(() => ConfirmDrop((int)amountSlider.value));
        confirmCancelButton.onClick.AddListener(() => confirmPanel.SetActive(false));
    }
    private void ConfirmSell(int quantity)
    {
        Debug.Log($"[ConfirmSell] 아이템 {currentItem.id} 수량 {quantity} 판매");
        PlayerInventory inv = FindObjectOfType<PlayerInventory>();
        inv.RemoveItem(currentItem.id, quantity);
        PlayerPointManager.Instance.AddPoints(currentItem.id * quantity); // 가격 처리 따로 필요
        confirmPanel.SetActive(false);
    }

    private void ConfirmDrop(int quantity)
    {
        Debug.Log($"[ConfirmDrop] 아이템 {currentItem.id} 수량 {quantity} 버림");
        PlayerInventory inv = FindObjectOfType<PlayerInventory>();
        inv.RemoveItem(currentItem.id, quantity);
        confirmPanel.SetActive(false);
    }

    private void CloseInfo() => itemInfoPanel.SetActive(false);

    private Sprite GetSpriteById(int id)
    {
        var data = ItemDataLoader.Instance?.LoadedItems?.Find(i => i.id == id);

        if (data == null)
        {
            Debug.LogError($"[GetSpriteById] 아이템 ID {id} 에 해당하는 데이터 없음");
            return null;
        }

        Debug.Log($"아이템 {id} 의 iconPath: {data.iconPath}, 타입: {data.itemType}");
        return Resources.Load<Sprite>(data.iconPath);
    }

    private void ActivateOnlyCurrentPanel()
    {
        // 부모의 형제 중 모든 InventoryPanelCtrl을 찾아서 비교
        InventoryPanelCtrl[] allPanels = transform.parent.GetComponentsInChildren<InventoryPanelCtrl>(true);

        foreach (var panel in allPanels)
        {
            panel.gameObject.SetActive(panel == this);
        }
    }
}

