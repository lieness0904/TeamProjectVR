using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class DolInteration : MonoBehaviour
{
    public GameObject infoUI; // 설명 UI 캔버스

    void Start()
    {
        if (infoUI != null)
            infoUI.SetActive(false); // 처음엔 안 보이게
    }

    public void ShowInfo()
    {
        if (infoUI != null)
            infoUI.SetActive(true);
    }

    public void HideInfo()
    {
        if (infoUI != null)
            infoUI.SetActive(false);
    }
}