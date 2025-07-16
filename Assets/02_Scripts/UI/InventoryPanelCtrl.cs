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

    public void NextPage()
    {
        if (currentPage < GetMaxPage())
            ShowPage(currentPage + 1);
    }
    public void PrevPage()
    {
        if (currentPage > 0)
            ShowPage(currentPage - 1);
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

            // 방어 코드
            if (itemIndex < 0 || itemIndex >= currentItems.Count)
                continue;

            var item = currentItems[itemIndex];
            Sprite sprite = GetSpriteById(item.id);
            if (sprite != null)
                AddItemIconToSlot(slot, sprite, item.amount);
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
        Transform itemSlotObj = slot.Find("ItemSlot");
        if (itemSlotObj != null)
        {
            itemSlotObj.gameObject.SetActive(true); // 여기 추가!
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

    private Sprite GetSpriteById(int id)
    {
        var data = ItemDataLoader.Instance.LoadedItems.Find(i => i.id == id);
        if (data == null)
            return null;
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

