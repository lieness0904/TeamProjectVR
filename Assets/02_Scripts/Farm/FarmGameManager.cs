using UnityEngine;
using System;

public class FarmGameManager : MonoBehaviour
{
    public static FarmGameManager Instance { get; private set; }

    [Header("선택사항: 싱글플레이 테스트용 스폰")]
    [SerializeField] private bool spawnPlayerForSingle = false;
    [SerializeField] private GameObject xrOriginPrefab;

    public bool IsGameStarted { get; private set; }
    public PlayerMovementDetector LocalPlayer { get; private set; }

    public event Action<int, int> OnRoundChanged; // (current, total)

    public void BroadcastWatcherFacingBack(bool isWatching)
    {
        OnWatcherFacingBackChanged?.Invoke(isWatching);
    }

    // 돌하르방에서 라운드 시작 시 호출 (1부터 시작 권장)
    public void BroadcastRoundChanged(int current, int total)
    {
        OnRoundChanged?.Invoke(current, total);
    }

    // === 이벤트들 ===
    public event Action OnGameStarted;
    public event Action OnGameEnded;
    public event Action<bool> OnWatcherFacingBackChanged; // true=뒤돌아감(감시중), false=정면(감시 아님)
    public event Action<int> OnSessionOrangeCountChanged;
    public event Action OnCaught; // 들켰을 때(UI 깜빡임 등)

    // === 이번 게임(시작~종료) 동안 딴 귤 ===
    public int SessionOrangeCount { get; private set; }

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

        SessionOrangeCount = 0;
        OnSessionOrangeCountChanged?.Invoke(SessionOrangeCount);

        Debug.Log("게임 시작: 무궁화 꽃이 피었습니다");
        IsGameStarted = true;
        OnGameStarted?.Invoke();
    }

    public void EndGame()
    {
        Debug.Log("게임 종료!");
        IsGameStarted = false;
        OnGameEnded?.Invoke();
    }

    // 이번 게임(세션) 동안 귤 획득 반영
    public void AddSessionOranges(int amount)
    {
        if (!IsGameStarted) return;
        SessionOrangeCount += amount;
        if (SessionOrangeCount < 0) SessionOrangeCount = 0;
        OnSessionOrangeCountChanged?.Invoke(SessionOrangeCount);
    }

    // 들킴 처리: 세션 카운트 0으로 리셋
    public void CaughtByWatcher()
    {
        if (!IsGameStarted) return;

        SessionOrangeCount = 0;
        OnSessionOrangeCountChanged?.Invoke(SessionOrangeCount);
        OnCaught?.Invoke();

        Debug.Log("들켰다! 이번 라운드에서 딴 귤 몰수!");
    }
}
