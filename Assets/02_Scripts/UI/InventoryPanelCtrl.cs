using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class InventoryPanelCtrl : MonoBehaviour
{
    public int slotsPerPage = 9;
    public int totalItems = 21;

    private int currentPage = 0;

    public void ShowPage(int pageIndex)
    {
        currentPage = Mathf.Clamp(pageIndex, 0, GetMaxPage());
        Debug.Log($"[InventoryPanelCtrl] 페이지 {currentPage + 1} / {GetMaxPage() + 1}");
    }

    public void NextPage()
    {
        if (currentPage < GetMaxPage())
            ShowPage(currentPage + 1);
    }

    public void PrevPage()
    {
        if (currentPage > 0)
            ShowPage(currentPage - 1);
    }

    private int GetMaxPage()
    {
        return Mathf.CeilToInt((float)totalItems / slotsPerPage) - 1;
    }
}

