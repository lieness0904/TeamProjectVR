using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class InventoryPageButton : MonoBehaviour
{
    public void NextActivePanelPage()
    {
        InventoryPanelCtrl[] panels = GetComponentsInChildren<InventoryPanelCtrl>(true);
        var activePanel = panels.FirstOrDefault(p => p.gameObject.activeSelf);
        if (activePanel != null)
            activePanel.NextPage();
    }

    public void PrevActivePanelPage()
    {
        InventoryPanelCtrl[] panels = GetComponentsInChildren<InventoryPanelCtrl>(true);
        var activePanel = panels.FirstOrDefault(p => p.gameObject.activeSelf);
        if (activePanel != null)
            activePanel.PrevPage();
    }
}
