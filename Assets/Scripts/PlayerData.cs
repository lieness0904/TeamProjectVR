using UnityEngine;

// 플레이어 데이터를 게임 전체에서 접근 가능하도록 관리하는 싱글톤 클래스
public class PlayerData : MonoBehaviour
{
    // static 변수로 인스턴스를 저장하여 어디서든 PlayerData.Instance로 접근 가능
    public static PlayerData Instance { get; private set; }

    public string UserID { get; private set; }
    public float MaxFishSize { get; set; }
    public string FishCaughtList { get; set; }
    public int Points { get; set; }

    private void Awake()
    {
        // 씬에 PlayerData 인스턴스가 없는 경우, 현재 인스턴스를 할당
        if (Instance == null)
        {
            Instance = this;
            // 씬이 전환되어도 이 게임 오브젝트가 파괴되지 않도록 설정
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            // 이미 인스턴스가 존재하면, 현재 인스턴스는 중복이므로 파괴
            Destroy(gameObject);
        }
    }

    // 로그인 시 서버로부터 받은 데이터로 초기화하는 함수
    public void SetData(string id, float maxSize, string fishList, int pts)
    {
        UserID = id;
        MaxFishSize = maxSize;
        FishCaughtList = fishList;
        Points = pts;
    }
}