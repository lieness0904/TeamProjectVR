using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Fusion;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MapSelect : NetworkBehaviour
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

    public void SelectMap()
    {
        var map = maps[currentIndex];

        if (!map.sceneRef.IsValid)
        {
            Debug.LogWarning("[MapSelect] 선택된 맵의 SceneRef가 유효하지 않습니다.");
            return;
        }

        NetworkRunner runner = FindObjectOfType<NetworkRunner>();
        if (runner == null)
        {
            Debug.LogError("[MapSelect] NetworkRunner가 씬에 존재하지 않음");
            return;
        }

        if (!runner.IsServer)
        {
            Debug.LogWarning("[MapSelect] 서버(호스트)가 아니므로 씬 이동 권한 없음");
            return;
        }

        if (runner.SceneManager is NetworkSceneManagerDefault sceneManager)
        {
            Debug.Log("[MapSelect] 호스트가 씬 전환 시작: " + map.sceneRef);

            // 유효한 플래그 생성 (Single + ActiveOnLoad)
            var flagsEnum = typeof(NetworkLoadSceneParameters).Assembly
                .GetType("Fusion.NetworkLoadSceneParametersFlags");

            var flags = Enum.ToObject(flagsEnum, 3); // 1(Single) | 2(ActiveOnLoad) = 3

            var loadId = new NetworkSceneLoadId();

            var constructor = typeof(NetworkLoadSceneParameters)
                .GetConstructor(BindingFlags.NonPublic | BindingFlags.Instance, null,
                    new[] { typeof(NetworkSceneLoadId), flagsEnum }, null);

            var loadParams = (NetworkLoadSceneParameters)constructor.Invoke(new object[] { loadId, flags });

            runner.SceneManager.LoadScene(map.sceneRef, loadParams);
        }
        else
        {
            Debug.LogError("[MapSelect] SceneManager가 NetworkSceneManagerDefault 타입이 아님!");
        }
    }
}
