using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShopInteraction : MonoBehaviour
{
    public GameObject shopButtonUI;
    public GameObject shopUI;
    public ShopManager shopManager;

    private void Start()
    {
        shopButtonUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !shopUI.activeSelf)
        {
            shopButtonUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            shopButtonUI.SetActive(false);
        }
    }

    public void OnShopButtonClick()
    {
        shopManager.OpenShop();
        shopButtonUI.SetActive(false);
    }
}
