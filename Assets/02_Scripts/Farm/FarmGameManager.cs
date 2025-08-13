using UnityEngine;

public class FarmGameManager : MonoBehaviour
{
    public static FarmGameManager Instance { get; private set; }

    [Header("선택사항: 싱글플레이 테스트용 스폰")]
    [SerializeField] private bool spawnPlayerForSingle = false;
    [SerializeField] private GameObject xrOriginPrefab;

    public bool IsGameStarted { get; private set; }
    public PlayerMovementDetector LocalPlayer { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void RegisterLocalPlayer(PlayerMovementDetector player)
    {
        LocalPlayer = player;

        // 씬의 돌하르방을 찾아 플레이어에 전달
        var watchers = FindObjectsOfType<DolhareubangWatcher>();
        if (watchers.Length > 0)
            LocalPlayer.SetWatcher(watchers[0]);

        Debug.Log("로컬 플레이어 등록 완료");
    }

    public void UnregisterLocalPlayer(PlayerMovementDetector player)
    {
        if (LocalPlayer == player) LocalPlayer = null;
    }

    public void StartGame()
    {
        if (IsGameStarted) return;

        // 싱글플레이 테스트에서만 프리팹 스폰
        if (spawnPlayerForSingle && LocalPlayer == null && xrOriginPrefab != null)
        {
            var xrOriginGO = Instantiate(xrOriginPrefab);
            var detector = xrOriginGO.GetComponent<PlayerMovementDetector>();
            if (detector != null)
                RegisterLocalPlayer(detector);
        }

        Debug.Log("게임 시작: 무궁화 꽃이 피었습니다");
        IsGameStarted = true;
    }

    public void EndGame()
    {
        Debug.Log("게임 종료!");
        IsGameStarted = false;
    }
}
