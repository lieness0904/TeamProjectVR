using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SelectMap : MonoBehaviour
{
    [Header("맵 데이터")]
    public MapData[] maps;

    [Header("UI 컴포넌트")]
    public Image mapImage;
    public TMP_Text mapName;
    //public TMP_Text mapDesc;

    private int currentIndex = 0;

    void Start()
    {
        UpdateUI();
    }

    void UpdateUI()
    {
        if (maps == null || maps.Length == 0) return;

        var map = maps[currentIndex];
        mapImage.sprite = map.previewImage;
        mapName.text = map.mapName;
        //mapDesc.text = map.description;
    }

    // VR UI Button에서 호출할 함수들
    public void Next()
    {
        currentIndex = (currentIndex + 1) % maps.Length;
        UpdateUI();
    }

    public void Previous()
    {
        currentIndex = (currentIndex - 1 + maps.Length) % maps.Length;
        UpdateUI();
    }

    public void Select()
    {
        string sceneName = maps[currentIndex].sceneName;

        if (!string.IsNullOrEmpty(sceneName))
        {
            //Debug.Log("씬 전환: " + sceneName);
            SceneManager.LoadScene(sceneName);
        }
        else
        {
            //Debug.LogWarning("선택된 맵의 씬 이름이 비어 있습니다.");
        }
    }
}