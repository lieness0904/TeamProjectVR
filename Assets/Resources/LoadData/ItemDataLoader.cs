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
        TextAsset jsonFile = Resources.Load<TextAsset>("LoadData/ItemData");
        if (jsonFile != null)
        {
            ItemDatabase database = JsonUtility.FromJson<ItemDatabase>(jsonFile.text);
            LoadedItems = database.items;
            Debug.Log($"아이템 {LoadedItems.Count}개 로드 완료");
        }
        else
        {
            Debug.LogError("itemdata.json 파일을 찾을 수 없습니다. Resources/Data 폴더 확인 필요");
        }
    }
}
