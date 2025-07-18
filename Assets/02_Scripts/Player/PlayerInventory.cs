using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private InventoryManager inventoryManager;

    public List<InventoryItem> items = new();
    public int maxSlots = 27;

    private void Start()
    {
        if (PlayerPointManager.Instance != null && PlayerPointManager.Instance.GetPoints() > 0)
        {
            var text = GameObject.Find("PlayerPointText")?.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                PlayerPointManager.Instance.SetPointText(text);
                Debug.Log("[PlayerInventory] PointText 자동 연결 성공");
            }
        }
    }
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Z))
        {
            FindObjectOfType<PlayerInventory>().AddItem(100, 2); 
            FindObjectOfType<PlayerInventory>().AddItem(200, 2);
            FindObjectOfType<PlayerInventory>().AddItem(300, 2);
            PlayerPointManager.Instance.AddPoints(300); // 포인트 추가
        }
        if (Input.GetKeyDown(KeyCode.X))
        {
            FindObjectOfType<PlayerInventory>().RemoveItem(100, 1); 
            FindObjectOfType<PlayerInventory>().RemoveItem(200, 1); 
            FindObjectOfType<PlayerInventory>().RemoveItem(300, 1); 
        }
        if (Input.GetKeyDown(KeyCode.C))
        {
            string userId = PlayerDataManager.Instance?.UserID ?? "Guest";
            InventorySyncManager.Instance.SaveInventoryToServer(userId);
        }
    }
    public void AddItem(int id, int amount = 1)
    {
        // 1. 슬롯 수 제한 검사 (장비는 개별 슬롯 필요, 그 외는 스택 가능)
        ItemType itemType = InventoryHelper.GetItemType(id);
        int currentSlotCount = items.Count;

        if (itemType == ItemType.Equipment)
        {
            int emptySlots = maxSlots - currentSlotCount;
            if (amount > emptySlots)
            {
                Debug.LogWarning("[PlayerInventory] 장비 슬롯이 부족합니다.");
                return;
            }

            for (int i = 0; i < amount; i++)
            {
                items.Add(new InventoryItem(id, 1));
            }
        }
        else
        {
            // 기존 아이템이 있는 경우 스택
            InventoryItem existing = items.Find(i => i.id == id);
            if (existing != null)
            {
                existing.amount += amount;
            }
            else
            {
                if (currentSlotCount >= maxSlots)
                {
                    Debug.LogWarning("[PlayerInventory] 일반 아이템 슬롯이 가득 찼습니다.");
                    return;
                }
                items.Add(new InventoryItem(id, amount));
            }
        }

        UpdateInventoryUI(); // UI 업데이트
    }

    public void RemoveItem(int id, int amount = 1)
    {
        InventoryItem existing = items.Find(i => i.id == id);
        if (existing != null)
        {
            existing.amount -= amount;
            if (existing.amount <= 0)
                items.Remove(existing);
        }

        UpdateInventoryUI();
    }
    private void UpdateInventoryUI()
    {
        inventoryManager?.RefreshCurrentPanel(items);
    }

    public string ToJson()
    {
        InventoryItemListWrapper wrapper = new InventoryItemListWrapper { items = items };
        string json = JsonUtility.ToJson(wrapper);
        Debug.Log($"[PlayerInventory] ToJson 결과: {json}");
        return json;
    }

    public void LoadFromJson(string json)
    {
        if (string.IsNullOrEmpty(json))
        {
            Debug.LogWarning("[PlayerInventory] LoadFromJson: JSON이 비어 있습니다.");
            return;
        }

        Debug.Log($"[PlayerInventory] LoadFromJson: 받은 JSON = {json}");

        InventoryItemListWrapper wrapper = JsonUtility.FromJson<InventoryItemListWrapper>(json);
        if (wrapper != null && wrapper.items != null)
            items = wrapper.items;

        UpdateInventoryUI();
    }

    public bool HasItem(int id, int amount = 1)
    {
        InventoryItem item = items.Find(i => i.id == id);
        return item != null && item.amount >= amount;
    }

    public void ClearInventory()
    {
        items.Clear();
    }

    private void OnApplicationQuit()
    {
        string userId = PlayerDataManager.Instance?.UserID ?? "Guest";
        InventorySyncManager.Instance.SaveInventoryToServer(userId);
    }
}
