using UnityEngine;
using System;

public class FarmGameManager : MonoBehaviour
{
    public static FarmGameManager Instance { get; private set; }

    [Header("선택사항: 싱글플레이 테스트용 스폰")]
    [SerializeField] private bool spawnPlayerForSingle = false;
    [SerializeField] private GameObject xrOriginPrefab;

    [SerializeField] private GameObject hudCanvas; // XR 카메라 밑의 HUDCanvas를 드래그해서 넣어

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

    // FarmGameManager.cs (StartGame 내부에 추가)
    public void StartGame()
    {
        if (IsGameStarted) return;

        // XR Origin이 런타임에 생성된다면, 먼저 생성하고 RegisterLocalPlayer까지 끝낸 뒤에:
        if (spawnPlayerForSingle && LocalPlayer == null && xrOriginPrefab != null)
        {
            var xrOriginGO = Instantiate(xrOriginPrefab);
            var detector = xrOriginGO.GetComponent<PlayerMovementDetector>();
            if (detector != null)
                RegisterLocalPlayer(detector);
        }

        // === HUD 켜기 ===
        if (hudCanvas == null)
        {
            // 혹시 인스펙터에 안 넣었으면, 로컬 플레이어 카메라 자식에서 찾아본다
            var hud = LocalPlayer
                ? LocalPlayer.GetComponentInChildren<UIGameHUD>(true)
                : FindObjectOfType<UIGameHUD>(true);

            if (hud) hudCanvas = hud.gameObject;
        }

        if (hudCanvas != null && !hudCanvas.activeSelf)
            hudCanvas.SetActive(true);

        SessionOrangeCount = 0;
        OnSessionOrangeCountChanged?.Invoke(SessionOrangeCount);

        IsGameStarted = true;
        OnGameStarted?.Invoke();
        Debug.Log("게임 시작");
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
