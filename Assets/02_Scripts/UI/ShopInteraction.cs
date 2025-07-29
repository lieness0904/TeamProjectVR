using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//public class ShopInteraction : MonoBehaviour, IInteractable
//{
//    public GameObject shopButtonUI;
//    public GameObject shopUI;
//    public ShopManager shopManager;
//    [SerializeField] private float closeDistance = 2f;

//    private Transform targetPlayer;
//    private void Start()
//    {
//        if (shopButtonUI != null)
//            shopButtonUI.SetActive(false);
//    }
//    public void Interact(Transform interactor)
//    {
//        float dist = Vector3.Distance(transform.position, interactor.position);
//        if (dist <= closeDistance && shopButtonUI != null && !shopUI.activeSelf)
//        {
//            targetPlayer = interactor;
//            shopButtonUI.SetActive(true);
//            StartCoroutine(CheckDistanceRoutine());
//        }
//    }
//    public float GetInteractRange()
//    {
//        return 3f;
//    }
//    public void OnShopButtonClick()
//    {
//        shopManager.OpenShop();
//        shopButtonUI.SetActive(false);
//    }
//    private IEnumerator CheckDistanceRoutine()
//    {
//        while (shopButtonUI.activeSelf)
//        {
//            if (targetPlayer == null) yield break;

//            float dist = Vector3.Distance(transform.position, targetPlayer.position);
//            if (dist > closeDistance)
//            {
//                shopButtonUI.SetActive(false);
//                yield break;
//            }
//            yield return new WaitForSeconds(0.2f);
//        }
//    }
//}

public class ShopInteraction : MonoBehaviour
{
    public GameObject shopButtonUI; // 버튼 오브젝트
    public GameObject shopUI;       // Shop 패널 오브젝트
    public ShopManager shopManager; // 상점 로직 관리자

    private void Start()
    {
        if (shopButtonUI != null)
            shopButtonUI.SetActive(true);

        if (shopUI != null)
            shopUI.SetActive(false);
    }

    // 버튼에 연결될 함수
    public void OnShopButtonClick()
    {
        if (shopManager != null)
            shopManager.OpenShop();

        if (shopUI != null)
            shopUI.SetActive(true);

        if (shopButtonUI != null)
            shopButtonUI.SetActive(false);
    }
}
