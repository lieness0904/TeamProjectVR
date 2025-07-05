using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class InventorySyncManager : MonoBehaviour
{
    public static InventorySyncManager Instance { get; private set; }

    private string scriptURL = "https://script.google.com/macros/s/AKfycbxsVIFFP0aRSLTljhqnSI0KAso8jv3Hx3UPdIiWsl1UynSyyk1EVABCf2Fpz6WwzcNn/exec";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void LoadInventoryFromServer(string userId)
    {
        StartCoroutine(LoadInventoryRoutine(userId));
    }

    public void SaveInventoryToServer(string userId)
    {
        StartCoroutine(SaveInventoryRoutine(userId));
    }

    private IEnumerator LoadInventoryRoutine(string userId)
    {
        WWWForm form = new WWWForm();
        form.AddField("action", "getInventory");
        form.AddField("userId", userId);

        using (UnityWebRequest www = UnityWebRequest.Post(scriptURL, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                string inventoryJson = www.downloadHandler.text;

                PlayerInventory inventory = FindObjectOfType<PlayerInventory>();
                if (inventory != null)
                {
                    inventory.LoadFromJson(inventoryJson);
                    Debug.Log("인벤토리 로드 완료: " + inventoryJson);
                }
            }
            else
            {
                Debug.LogError("인벤토리 로드 실패: " + www.error);
            }
        }
    }

    private IEnumerator SaveInventoryRoutine(string userId)
    {
        PlayerInventory inventory = FindObjectOfType<PlayerInventory>();
        if (inventory == null)
        {
            Debug.LogWarning("PlayerInventory 없음. 저장 생략");
            yield break;
        }

        string inventoryJson = inventory.ToJson();

        WWWForm form = new WWWForm();
        form.AddField("action", "saveInventory");
        form.AddField("userId", userId);
        form.AddField("inventory", inventoryJson);

        using (UnityWebRequest www = UnityWebRequest.Post(scriptURL, form))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("인벤토리 저장 성공");
            }
            else
            {
                Debug.LogError("인벤토리 저장 실패: " + www.error);
            }
        }
    }
}
