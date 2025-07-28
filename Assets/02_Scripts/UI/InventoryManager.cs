using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    [Header("Inventory Panels")]
    public GameObject equipmentInventory;
    public GameObject consumableInventory;
    public GameObject commonInventory;

    [Header("Panel Controllers")]
    public InventoryPanelCtrl equipmentController;
    public InventoryPanelCtrl consumableController;
    public InventoryPanelCtrl commonController;

    [Header("Category Buttons")]
    public Button equipButton;
    public Button consumeButton;
    public Button commonButton;

    [Header("Paging Buttons")]
    public Button prevButton;
    public Button nextButton;

    [Header("Colors")]
    public Color selectedColor = new Color(1f, 0.5f, 0f); // 주황색
    public Color defaultColor = Color.white;

    private GameObject currentInventory;
    private InventoryPanelCtrl currentController;

    private void Start()
    {
        var playerInventory = FindObjectOfType<PlayerInventory>();

        equipmentController.ShowItem(playerInventory.items);
        consumableController.ShowItem(playerInventory.items);
        commonController.ShowItem(playerInventory.items);

        ShowInventory(equipmentInventory, equipmentController, equipButton);
    }

    private void ShowInventory(GameObject panel, InventoryPanelCtrl controller, Button highlightTarget)
    {
        // 패널 전부 끄고, 선택된 것만 켜기
        equipmentInventory.SetActive(false);
        consumableInventory.SetActive(false);
        commonInventory.SetActive(false);

        panel.SetActive(true);
        currentInventory = panel;
        currentController = controller;

        // 인벤토리 데이터 로드
        var playerInventory = FindObjectOfType<PlayerInventory>();
        controller.ShowItem(playerInventory.items);

        // 버튼 색상 하이라이트
        HighlightButton(highlightTarget);
    }

    private void HighlightButton(Button selected)
    {
        equipButton.image.color = defaultColor;
        consumeButton.image.color = defaultColor;
        commonButton.image.color = defaultColor;

        selected.image.color = selectedColor;
    }

    public void RefreshCurrentPanel(List<InventoryItem> items)
    {
        currentController?.ShowItem(items);
    }
    public void ShowEquipmentInventory() => ShowInventory(equipmentInventory, equipmentController, equipButton);
    public void ShowConsumableInventory() => ShowInventory(consumableInventory, consumableController, consumeButton);
    public void ShowCommonInventory() => ShowInventory(commonInventory, commonController, commonButton);
    public void PrevButton() => currentController?.PrevPage();
    public void NextButton() => currentController?.NextPage();
}