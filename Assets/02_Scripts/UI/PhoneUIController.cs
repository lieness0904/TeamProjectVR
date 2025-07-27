using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Rukha93.ModularAnimeCharacter.Customization;
using Fusion;

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

    [Header("Customize")]
    public Button customizeButton;

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

    public void OpenCustomization()
    {
        // 현재 씬이 HouseScene일 때만 커스터마이징 진입 허용
        if (SceneManager.GetActiveScene().name == "HouseScene")
        {
            StartCoroutine(EnterCustomizationRoutine());
        }
        else
        {
            Debug.LogWarning("현재 씬에서는 커스터마이징을 열 수 없습니다.");
        }
    }

    private IEnumerator EnterCustomizationRoutine()
    {
        var runner = FindObjectOfType<NetworkRunner>();
        if (runner != null && runner.IsRunning)
        {
            Debug.Log("세션 종료 중...");
            yield return runner.Shutdown();
        }

        SceneManager.LoadScene("CustomizationScene", LoadSceneMode.Single);
    }
}
