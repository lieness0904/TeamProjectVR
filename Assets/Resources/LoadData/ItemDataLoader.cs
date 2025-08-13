using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemDataLoader : MonoBehaviour
{
    public static ItemDataLoader Instance { get; private set; }
    public List<ItemData> LoadedItems { get; private set; } = new List<ItemData>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadItemsFromJson();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadItemsFromJson()
    {
        TextAsset jsonFile = Resources.Load<TextAsset>("LoadData/Items"); // 또는 "LoadData/items"
        if (jsonFile != null)
        {
            string rawJson = jsonFile.text;

            // BOM 제거
            if (!string.IsNullOrEmpty(rawJson) && rawJson[0] == '\uFEFF')
            {
                Debug.LogWarning("[ItemDataLoader] BOM 발견 → 제거");
                rawJson = rawJson.Substring(1);
            }
            rawJson = rawJson.Trim();

            // ✅ JSON 형식 가드: '{' 또는 '[' 로 시작하지 않으면 경고
            if (!(rawJson.StartsWith("{") || rawJson.StartsWith("[")))
            {
                Debug.LogError($"[ItemDataLoader] 이 파일 JSON 아님. Resources 경로=LoadData/ItemData, 에셋명={jsonFile.name}\n" +
                               $"앞 40자: {rawJson.Substring(0, Mathf.Min(40, rawJson.Length))}");
                return;
            }

            try
            {
                ItemDatabase database = JsonUtility.FromJson<ItemDatabase>(rawJson);
                if (database == null || database.items == null)
                {
                    Debug.LogError("[ItemDataLoader] 파싱은 됐지만 데이터가 null. 키 이름(items) / 필드명 확인.");
                    return;
                }
                LoadedItems = database.items;
                Debug.Log($"아이템 {LoadedItems.Count}개 로드 완료");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ItemDataLoader] JSON 파싱 실패: {e.Message}");
                Debug.Log($"[ItemDataLoader] RAW JSON (앞 200자):\n{rawJson.Substring(0, Mathf.Min(200, rawJson.Length))}");
            }
        }
        else
        {
            Debug.LogError("itemdata.json 을 못 찾음. 경로는 Assets/Resources/LoadData/ItemData.json 이어야 함.");
        }
    }

}
