using UnityEngine;

// 플레이어 데이터를 게임 전체에서 접근할 수 있게 관리하는 싱글톤 클래스
public class PlayerDataManager : MonoBehaviour
{
    public static PlayerDataManager Instance { get; private set; }

    public string UserID; // 로그인한 유저의 아이디를 저장할 변수

    void Awake()
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
}