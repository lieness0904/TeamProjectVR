using System.Collections;
using UnityEngine;

// 플레이어 데이터를 게임 전체에서 접근할 수 있게 관리하는 싱글톤 클래스
public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    public string UserID; // 로그인한 유저의 아이디를 저장할 변수
    public string InventoryJson;
    public PlayerInventory playerInventory;
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 이 오브젝트는 파괴되지 않음
        }
        else
        {
            Destroy(gameObject);
        }
    }
    private void Start()
    {
        StartCoroutine(WaitAndApplyInventory());   
    }

    private IEnumerator WaitAndApplyInventory()
    {
        yield return new WaitUntil(() => FindObjectOfType<PlayerInventory>() != null);

        playerInventory = FindObjectOfType<PlayerInventory>();
        if (!string.IsNullOrEmpty(InventoryJson))
        {
            playerInventory.LoadFromJson(InventoryJson);
            Debug.Log("PlayerInventory 연결 및 JSON 적용 완료");
        }
        else
        {
            Debug.LogWarning("InventoryJson 비어있음 - 서버 응답 확인 필요");
        }
    }
}