using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PhoneUIController : MonoBehaviour
{
    public GameObject phoneUI;

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
            }
        }
    }

    private void ResetPanels()
    {
        if (chatPanel != null) chatPanel.SetActive(false);
        if (settingPanel != null) settingPanel.SetActive(false);
        if (inventoryPanel != null) inventoryPanel.SetActive(false);
    }
}

