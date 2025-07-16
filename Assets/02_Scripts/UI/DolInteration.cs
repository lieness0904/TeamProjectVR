using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
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
        if (infoUI == null) return;

        Transform cam = Camera.main.transform;

        // 카메라 앞에 UI 배치
        infoUI.transform.position = cam.position + cam.forward * 1f;

        // UI가 카메라를 정확히 바라보게
        infoUI.transform.rotation = Quaternion.LookRotation(infoUI.transform.position - cam.position);

        infoUI.SetActive(true);
    }

    public void HideInfo()
    {
        if (infoUI != null)
            infoUI.SetActive(false);
    }
}