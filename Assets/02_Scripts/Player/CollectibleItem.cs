using UnityEngine;

public class CollectibleItem : MonoBehaviour
{
    //주워질 아이템 오브젝트의 인스펙터에서 simple interact 컴포넌트를 추가하고, 이벤트로 Collect() 메소드를 연결하세요.
    [Header("아이템 ID")]
    [SerializeField] private int itemId; //인스펙터에 아이템 ID와 갯수 설정하세욤.
    [Header("아이템 갯수")]
    [SerializeField] private int amount = 1;

    public void Collect()
    {
        PlayerInventory inventory = FindObjectOfType<PlayerInventory>();
        if (inventory != null)
            inventory.AddItem(itemId, amount);

        // 세션 카운트 반영
        if (FarmGameManager.Instance != null)
            FarmGameManager.Instance.AddSessionOranges(amount);

        Destroy(gameObject);
    }
}
