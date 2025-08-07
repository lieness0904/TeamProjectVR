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
        if (inventory == null)
        {// 플레이어 인벤토리를 찾을 수 없는 경우
            //Debug.LogError("[CollectibleItem] PlayerInventory를 찾을 수 없습니다.");
            return;
        }

        inventory.AddItem(itemId, amount);
        Destroy(gameObject); // 아이템 획득 성공 시 오브젝트 제거
        //Debug.Log($"[CollectibleItem] 아이템 {itemId}을(를) {amount}개 획득했습니다.");
    }
}
