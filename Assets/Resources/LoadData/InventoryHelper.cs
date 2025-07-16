using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryHelper : MonoBehaviour
{
    public static ItemType GetItemType(int id)
    {
        if (ItemDataLoader.Instance == null)
        {
            Debug.LogError("[InventoryHelper] ItemDataLoader.Instance가 null입니다.");
            return ItemType.Common;
        }

        var data = ItemDataLoader.Instance.LoadedItems.Find(i => i.id == id);
        if (data == null)
        {
            Debug.LogError($"[InventoryHelper] ID {id}에 해당하는 아이템 데이터를 찾을 수 없습니다.");
            return ItemType.Common;
        }

        return data.itemType;
    }

    public static List<InventoryItem> FilterByType(List<InventoryItem> items, ItemType type)
    {
        return items.Where(i => GetItemType(i.id) == type).ToList();
    }
}
