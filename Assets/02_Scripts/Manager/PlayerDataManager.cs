using System.Collections;
using UnityEngine;
using TMPro;

// 플레이어 데이터를 게임 전체에서 접근할 수 있게 관리하는 싱글톤 클래스
public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    public string UserID; // 로그인한 유저의 아이디를 저장할 변수
    public string InventoryJson;
    public int Points;
    public PlayerInventory playerInventory;

    public CustomizationData CustomizationData;

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

    public IEnumerator WaitAndApplyInventory()
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

        if (PlayerPointManager.Instance != null)
        {
            PlayerPointManager.Instance.Initialize(Points);
            Debug.Log("포인트 초기화 완료: " + Points);
        }
        else
        {
            Debug.LogWarning("PlayerPointManager 인스턴스 없음 - 포인트 초기화 실패");
        }
    }
}