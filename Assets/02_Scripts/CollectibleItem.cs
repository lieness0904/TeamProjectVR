using UnityEngine;

public class CollectibleItem : MonoBehaviour
{
    [Header("아이템 ID")]
    [SerializeField] private int itemId; //인스펙터에 아이템 ID와 갯수 설정하세욤.
    [Header("아이템 갯수")]
    [SerializeField] private int amount = 1;

    public void Collect()
    {
        PlayerInventory inventory = FindObjectOfType<PlayerInventory>();
        if (inventory == null)
        {
            Debug.LogError("[CollectibleItem] PlayerInventory를 찾을 수 없습니다.");
            return;
        }

        inventory.AddItem(itemId, amount);
        Destroy(gameObject); // 획득 성공 시 오브젝트 제거
    }
}
