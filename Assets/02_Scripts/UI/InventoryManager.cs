using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    [Header("Inventory Panels")]
    public GameObject fishInventory;
    public GameObject treeInventory;
    public GameObject orangeInventory;

    [Header("Panel Controllers")]
    public InventoryPanelCtrl fishController;
    public InventoryPanelCtrl treeController;
    public InventoryPanelCtrl orangeController;

    [Header("Category Buttons")]
    public Button fishButton;
    public Button treeButton;
    public Button orangeButton;

    [Header("Paging Buttons")]
    public Button prevButton;
    public Button nextButton;

    [Header("Colors")]
    public Color selectedColor = new Color(1f, 0.5f, 0f); // 주황색
    public Color defaultColor = Color.white;

    private GameObject currentInventory;
    private InventoryPanelCtrl currentController;

    public void InitInventory()
    {
        ShowFishInventory();
    }

    public void ShowFishInventory()
    {
        ShowInventory(fishInventory, fishController);
        HighlightButton(fishButton);
    }

    public void ShowTreeInventory()
    {
        ShowInventory(treeInventory, treeController);
        HighlightButton(treeButton);
    }

    public void ShowOrangeInventory()
    {
        ShowInventory(orangeInventory, orangeController);
        HighlightButton(orangeButton);
    }

    private void ShowInventory(GameObject inventoryPanel, InventoryPanelCtrl controller)
    {
        fishInventory.SetActive(false);
        treeInventory.SetActive(false);
        orangeInventory.SetActive(false);

        inventoryPanel.SetActive(true);
        currentInventory = inventoryPanel;
        currentController = controller;

        currentController.ShowPage(0);
        BindPagingButtons();
    }

    private void HighlightButton(Button selected)
    {
        fishButton.image.color = defaultColor;
        treeButton.image.color = defaultColor;
        orangeButton.image.color = defaultColor;

        selected.image.color = selectedColor;
    }

    private void BindPagingButtons()
    {
        prevButton.onClick.RemoveAllListeners();
        nextButton.onClick.RemoveAllListeners();

        prevButton.onClick.AddListener(() => currentController.PrevPage());
        nextButton.onClick.AddListener(() => currentController.NextPage());
    }
}
