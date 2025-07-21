using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PhoneUIController : MonoBehaviour
{
    [Header("Root UI")]
    public GameObject phoneUI;

    [Header("Panels")]
    public GameObject menuPanel;
    public GameObject chatPanel;
    public GameObject settingPanel;
    public GameObject inventoryPanel;

    [Header("Optional")]
    public InventoryManager inventoryManager;

    [Header("Input")]
    public InputActionReference togglePhoneUIAction; // 메뉴버튼 연결용 (menuButton)

    private void OnEnable()
    {
        togglePhoneUIAction?.action.Enable();
    }

    private void OnDisable()
    {
        togglePhoneUIAction?.action.Disable();
    }

    void Update()
    {
        // 키보드용 (테스트용)
        if (Input.GetKeyDown(KeyCode.M))
        {
            TogglePhone();
        }

        //VR 메뉴 버튼 (왼손)
        if (togglePhoneUIAction != null && togglePhoneUIAction.action.WasPressedThisFrame())
        {
            TogglePhone();
        }
    }

    public void TogglePhone()
    {
        bool isNowActive = !phoneUI.activeSelf;
        phoneUI.SetActive(isNowActive);

        if (isNowActive)
        {
            ResetPanels();
            menuPanel.SetActive(true);
        }
    }

    public void OpenChat()
    {
        ResetPanels();
        chatPanel.SetActive(true);
    }

    public void OpenSetting()
    {
        ResetPanels();
        settingPanel.SetActive(true);
    }

    public void OpenInventory()
    {
        ResetPanels();
        inventoryPanel.SetActive(true);
    }

    public void GoBackToMenu()
    {
        ResetPanels();
        menuPanel.SetActive(true);
    }

    private void ResetPanels()
    {
        menuPanel.SetActive(false);
        chatPanel.SetActive(false);
        settingPanel.SetActive(false);
        inventoryPanel.SetActive(false);
    }
}
