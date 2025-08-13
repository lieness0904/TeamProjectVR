using UnityEngine;
using System;

public class FarmGameManager : MonoBehaviour
{
    public static FarmGameManager Instance { get; private set; }

    [Header("싱글 테스트용 XR Origin 스폰")]
    [SerializeField] private bool spawnPlayerForSingle = false;
    [SerializeField] private GameObject xrOriginPrefab;

    [Header("HUD (월드 스페이스 캔버스 루트)")]
    [SerializeField] private GameObject hudCanvas;              // 있으면 드래그, 없으면 자동 탐색
    [SerializeField] private bool hideHudOnEnd = false;         // 게임 종료 시 HUD 끌지 여부

    public bool IsGameStarted { get; private set; }
    public PlayerMovementDetector LocalPlayer { get; private set; }

    // === 이벤트들 ===
    public event Action OnGameStarted;
    public event Action OnGameEnded;
    public event Action<bool> OnWatcherFacingBackChanged; // true=감시중(뒤돌아감), false=정면
    public event Action<int> OnSessionOrangeCountChanged;
    public event Action OnCaught;                         // 들켰을 때
    public event Action<int, int> OnRoundChanged;         // (current, total)

    // XR 카메라 준비 이벤트 (월드 HUD가 따라붙어야 할 때 유용)
    public event Action<Transform> OnLocalCameraReady;

    // === 이번 게임(세션) 동안 딴 귤 ===
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

        RaiseLocalCameraReady(); // 카메라 알림
    }

    public void UnregisterLocalPlayer(PlayerMovementDetector player)
    {
        if (LocalPlayer == player) LocalPlayer = null;
    }

    public void StartGame()
    {
        if (IsGameStarted) return;

        // 1) XR Origin 생성 및 등록
        if (spawnPlayerForSingle && LocalPlayer == null && xrOriginPrefab != null)
        {
            var xrOriginGO = Instantiate(xrOriginPrefab);
            var detector = xrOriginGO.GetComponent<PlayerMovementDetector>();
            if (detector != null)
                RegisterLocalPlayer(detector);
        }

        // 혹시 외부에서 이미 등록돼 있더라도 한 번 더 카메라 알림
        RaiseLocalCameraReady();

        // 2) HUD 켜고(없으면 찾아서) Event Camera 바인딩
        EnsureHUDVisibleAndBound();

        // 3) 세션 카운트 초기화 + 이벤트
        SessionOrangeCount = 0;
        OnSessionOrangeCountChanged?.Invoke(SessionOrangeCount);

        // 4) 게임 시작 브로드캐스트
        IsGameStarted = true;
        OnGameStarted?.Invoke();
        Debug.Log("게임 시작: 무궁화 꽃이 피었습니다");
    }

    public void EndGame()
    {
        Debug.Log("게임 종료!");
        IsGameStarted = false;
        OnGameEnded?.Invoke();

        if (hideHudOnEnd && hudCanvas != null && hudCanvas.activeSelf)
            hudCanvas.SetActive(false);
    }

    // === 브로드캐스트 유틸 ===
    public void BroadcastWatcherFacingBack(bool isWatching)
    {
        OnWatcherFacingBackChanged?.Invoke(isWatching);
    }

    public void BroadcastRoundChanged(int current, int total)
    {
        OnRoundChanged?.Invoke(current, total);
    }

    // === 세션 카운트 ===
    public void AddSessionOranges(int amount)
    {
        if (!IsGameStarted) return;
        SessionOrangeCount += amount;
        if (SessionOrangeCount < 0) SessionOrangeCount = 0;
        OnSessionOrangeCountChanged?.Invoke(SessionOrangeCount);
    }

    public void CaughtByWatcher()
    {
        if (!IsGameStarted) return;

        SessionOrangeCount = 0;
        OnSessionOrangeCountChanged?.Invoke(SessionOrangeCount);
        OnCaught?.Invoke();

        Debug.Log("들켰다! 이번 라운드에서 딴 귤 몰수!");
    }

    // === 내부 헬퍼 ===
    private void EnsureHUDVisibleAndBound()
    {
        // 1) HUD 오브젝트 확보
        if (hudCanvas == null)
        {
            // 로컬 플레이어 자식에서 먼저 찾기
            if (LocalPlayer != null)
            {
                var hud = LocalPlayer.GetComponentInChildren<UIGameHUD>(true);
                if (hud != null) hudCanvas = hud.gameObject;
            }
            // 그래도 없으면 씬 전체에서 찾기
            if (hudCanvas == null)
            {
                var hud = FindObjectOfType<UIGameHUD>(true);
                if (hud != null) hudCanvas = hud.gameObject;
            }
        }

        if (hudCanvas == null)
        {
            Debug.LogWarning("[FarmGameManager] HUDCanvas를 못 찾음. 인스펙터에 할당하거나 씬/프리팹 구조 확인해.");
            return;
        }

        // 2) Event Camera 바인딩 (World Space/ScreenSpaceCamera 모두)
        var canvas = hudCanvas.GetComponent<Canvas>();
        if (canvas != null &&
            (canvas.renderMode == RenderMode.WorldSpace || canvas.renderMode == RenderMode.ScreenSpaceCamera))
        {
            if (canvas.worldCamera == null)
            {
                var cam = GetLocalCamera();
                if (cam != null)
                    canvas.worldCamera = cam;
                else
                    Debug.LogWarning("[FarmGameManager] HUD Event Camera 못 찾음. XR 카메라 존재/태그 확인.");
            }
        }

        // 3) HUD 켜기 (월드 스페이스면 위치는 HUDFollower가 잡음)
        if (!hudCanvas.activeSelf)
            hudCanvas.SetActive(true);
    }

    private Camera GetLocalCamera()
    {
        if (LocalPlayer != null)
        {
            var cam = LocalPlayer.GetComponentInChildren<Camera>(true);
            if (cam != null) return cam;
        }
        if (Camera.main != null) return Camera.main;

        var any = FindObjectOfType<Camera>(true);
        return any;
    }

    private void RaiseLocalCameraReady()
    {
        var cam = GetLocalCamera();
        if (cam != null)
            OnLocalCameraReady?.Invoke(cam.transform);
    }
}
