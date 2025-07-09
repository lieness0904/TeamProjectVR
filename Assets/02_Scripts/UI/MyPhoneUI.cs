using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MyPhoneUI : MonoBehaviour
{
    public GameObject chatPanel;
    public GameObject settingPanel;
    public GameObject inventoryPanel;

    public void OpenChat()
    {
        CloseAll();
        chatPanel.SetActive(true);
    }

    public void OpenSetting()
    {
        CloseAll();
        settingPanel.SetActive(true);
    }

    public void OpenInventory()
    {
        CloseAll();
        inventoryPanel.SetActive(true);
    }

    public void CloseAll()
    {
        chatPanel.SetActive(false);
        settingPanel.SetActive(false);
        inventoryPanel.SetActive(false);
    }

    public void ClosePhone()
    {
        gameObject.SetActive(false);
    }
}
