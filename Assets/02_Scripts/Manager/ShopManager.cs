using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class ShopManager : MonoBehaviour
{
    [SerializeField] private GameObject shopPanel;
    [SerializeField] private GameObject shopItemSlotPrefab;
    [SerializeField] private Transform itemGridParent;
    [SerializeField] private TextAsset itemJson;
    [SerializeField] private List<int> shopItemIds = new(); // Inspector에서 id 직접 설정
    [SerializeField] private Button closeShopButton;
    [SerializeField] private GameObject purchaseCompleteUI;

    private List<ItemData> shopItems = new();

    private void Start()
    {
        shopPanel.SetActive(false);
        if (purchaseCompleteUI != null)
        {
            purchaseCompleteUI.SetActive(false);
        }
    }

    public void OpenShop()
    {
        shopPanel.SetActive(true);
        LoadItemData();
        PopulateShopUI();
    }
    public void CloseShop()
    {
        shopPanel.SetActive(false);
    }

    private void LoadItemData()
    {
        // itemJson 전체 불러옴
        var wrapper = JsonUtility.FromJson<ItemDatabase>(itemJson.text);
        // shopItemIds에 해당하는 아이템만 뽑아서 상점에서 판매
        shopItems = wrapper.items.Where(x => shopItemIds.Contains(x.id)).ToList();
    }

    private void PopulateShopUI()
    {
        foreach (Transform child in itemGridParent)
            Destroy(child.gameObject);

        foreach (var item in shopItems)
        {
            var slotObj = Instantiate(shopItemSlotPrefab, itemGridParent);
            var slot = slotObj.GetComponent<ShopItemSlot>();
            slot.Setup(item);
        }
    }

    public void TryBuyItem(ItemData item, int quantity)
    {
        int totalCost = item.price * quantity;
        if (quantity <= 0)
        {
            Debug.LogWarning("[ShopManager] 구매 수량은 1 이상이어야 합니다.");
            return;
        }
        if (PlayerPointManager.Instance.GetPoints() >= totalCost)
        {
            PlayerPointManager.Instance.AddPoints(-totalCost);
            FindObjectOfType<PlayerInventory>()?.AddItem(item.id, quantity);
            StartCoroutine(ShowPurchaseComplete());
        }
        else
        {
            Debug.LogWarning("[ShopManager] 포인트가 부족합니다. 현재 포인트: " + PlayerPointManager.Instance.GetPoints());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            shopPanel.SetActive(false);
        }
    }

    private IEnumerator ShowPurchaseComplete()
    {
        if (purchaseCompleteUI == null) yield break;

        purchaseCompleteUI.SetActive(true);
        yield return new WaitForSeconds(1.5f);
        purchaseCompleteUI.SetActive(false);
    }
}
