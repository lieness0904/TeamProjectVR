using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Rukha93.ModularAnimeCharacter.Customization;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
using Rukha93.ModularAnimeCharacter.Customization.UI;
using Fusion;

public class CustomizationManager : MonoBehaviour
{
    public static CustomizationManager Instance { get; private set; }

    private CustomizationDemo current;
    public Button outButton;

    [SerializeField] private UICustomizationDemo demoUIReference;

    // 저장된 외형 정보
    public Dictionary<string, string> SavedCustomization { get; private set; }

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }
    private void Start()
    {
        // 씬에 배치된 CustomizationDemo 직접 찾기
        current = FindObjectOfType<CustomizationDemo>();

        if (current != null)
        {
            current.m_UI = demoUIReference;
            SetTarget(current);
        }

        if (outButton != null)
        {
            outButton.onClick.AddListener(OnCompleteCustomization);
        }
    }

    public void SetTarget(CustomizationDemo customization)
    {
        current = customization;
        current.gameObject.SetActive(true); // 혹은 활성화 외 초기화 호출
    }

    public void OnCompleteCustomization()
    {
        Debug.Log("Customization Completed!");

        if (current != null)
        {
            Dictionary<string, string> rawDict = current.GetCurrentCustomization();
            Dictionary<string, string> dict = new();

            foreach (var pair in rawDict)
            {
                dict[pair.Key] = pair.Value; 
            }

            CustomizationData data = new CustomizationData
            {
                body = dict.GetValueOrDefault("body"),
                head = dict.GetValueOrDefault("head"),
                top = dict.GetValueOrDefault("top"),
                bottom = dict.GetValueOrDefault("bottom"),
                shoes = dict.GetValueOrDefault("shoes"),
                outfit = dict.GetValueOrDefault("outfit"),
                hairstyle = dict.GetValueOrDefault("hairstyle"),
                acc_head = dict.GetValueOrDefault("acc_head"),
                gender = dict.GetValueOrDefault("gender"),
            };

            CustomizationDataStore.LatestDataJson = JsonUtility.ToJson(data);

            string userId = PlayerDataManager.Instance.UserID;
            SaveCustomizationToSheet(userId, CustomizationDataStore.LatestDataJson);

            Destroy(current.gameObject);
            current = null;
        }

        StartCoroutine(BackToHouseAndRestartFusion());
    }

    private IEnumerator BackToHouseAndRestartFusion()
    {
        SceneManager.LoadScene("HouseScene", LoadSceneMode.Single);
        yield return new WaitUntil(() => SceneManager.GetActiveScene().name == "HouseScene");
    }

    public void SaveCustomizationToSheet(string userId, string customizationJson)
    {
        StartCoroutine(SaveRoutine(userId, customizationJson));
    }

    private IEnumerator SaveRoutine(string userId, string customizationJson)
    {
        WWWForm form = new WWWForm();
        form.AddField("action", "saveData");
        form.AddField("userId", userId);
        form.AddField("customizationData", customizationJson);

        using UnityWebRequest www = UnityWebRequest.Post("https://script.google.com/macros/s/AKfycbxbEbhCsVmqMWCuZOtPEcfGFperFw3nRjDw5OsECes9IFx2pbeXZMFmMAS0E20XUVy3/exec", form);
        yield return www.SendWebRequest();

        if (www.result == UnityWebRequest.Result.Success)
        {
            Debug.Log("커스터마이징 저장 성공: " + www.downloadHandler.text);
        }
        else
        {
            Debug.LogError("커스터마이징 저장 실패: " + www.error);
        }
    }
}
