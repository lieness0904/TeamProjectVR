using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PhoneMenuCtrl : MonoBehaviour
{
    public GameObject menuPanel;
    public GameObject chatPanel;
    public GameObject settingPanel;
    public GameObject inventoryPanel;

    public InventoryManager inventoryManager;

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
        //inventoryManager.InitInventory();
    }

    public void CloseAll()
    {
        menuPanel.SetActive(false);
        chatPanel.SetActive(false);
        settingPanel.SetActive(false);
        inventoryPanel.SetActive(false);
    }

    public void GoBackToMenu()
    {
        CloseAll();
        menuPanel.SetActive(true);
    }

    //public void ClosePhone()
    //{
    //    gameObject.SetActive(false);
    //}
}
