using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PhoneCtrl : MonoBehaviour
{
    public GameObject phoneUI;
    public GameObject menuPanel;
    public GameObject chatPanel;
    public GameObject settingPanel;
    public GameObject inventoryPanel;


    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            bool isNowActive = !phoneUI.activeSelf;
            phoneUI.SetActive(isNowActive);

            if (isNowActive)
            {
                // 켤 때 모든 하위 패널 비활성화
                ResetPanels();
                menuPanel.SetActive(true);
            }
        }
    }

    private void ResetPanels()
    {
        menuPanel.SetActive(false);
        chatPanel.SetActive(false);
        settingPanel.SetActive(false);
        inventoryPanel.SetActive(false);
    }
}

