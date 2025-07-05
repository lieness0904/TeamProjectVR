using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public List<InventoryItem> items = new();

    public void AddItem(int id, int amount = 1)
    {
        InventoryItem existing = items.Find(i => i.id == id);
        if (existing != null)
        {
            existing.amount += amount;
        }
        else
        {
            items.Add(new InventoryItem(id, amount));
        }
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
    }

    public string ToJson()
    {
        InventoryItemListWrapper wrapper = new InventoryItemListWrapper { items = items };
        return JsonUtility.ToJson(wrapper);
    }

    public void LoadFromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        InventoryItemListWrapper wrapper = JsonUtility.FromJson<InventoryItemListWrapper>(json);
        if (wrapper != null && wrapper.items != null)
            items = wrapper.items;
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
}
