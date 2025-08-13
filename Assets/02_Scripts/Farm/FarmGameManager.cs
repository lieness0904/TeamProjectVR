using UnityEngine;
using System;
using System.Collections; // 코루틴용

public class FarmGameManager : MonoBehaviour
{
    public static FarmGameManager Instance { get; private set; }

    [Header("싱글 테스트용 XR Origin 스폰")]
    [SerializeField] private bool spawnPlayerForSingle = false;
    [SerializeField] private GameObject xrOriginPrefab;

    [Header("시작/종료 UI")]
    [SerializeField] private GameObject startCanvas; // 시작/설명 월드캔버스 루트(있으면 드래그)

    [Header("HUD (월드 스페이스 캔버스 루트)")]
    [SerializeField] private GameObject hudCanvas;      // 있으면 드래그, 없으면 자동 탐색
    [SerializeField] private bool hideHudOnEnd = false; // 종료 시 HUD 끌지 여부
    [SerializeField] private float hideHudDelay = 1.6f; // 종료 공지 보여줄 시간 (announceHold+여유)

    public bool IsGameStarted { get; private set; }
    public PlayerMovementDetector LocalPlayer { get; private set; }

    // === 이벤트들 ===
    public event Action OnGameStarted;
    public event Action OnGameEnded;
    public event Action<bool> OnWatcherFacingBackChanged; // true=감시중(뒤돌아감), false=정면
    public event Action<int> OnSessionOrangeCountChanged;
    public event Action OnCaught;                         // 들켰을 때
    public event Action<int, int> OnRoundChanged;         // (current, total)
    public event Action<Transform> OnLocalCameraReady;    // XR 카메라 Transform 전달

    // === 이번 게임(세션) 동안 딴 귤 ===
    public int SessionOrangeCount { get; private set; }

    private bool _isStarting = false;

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

        RaiseLocalCameraReady(); // 카메라 알림(월드 HUD/바인더가 이거 듣고 바인딩)
    }

    public void UnregisterLocalPlayer(PlayerMovementDetector player)
    {
        if (LocalPlayer == player) LocalPlayer = null;
    }

    // === 게임 시작: 시퀀스로 변경 (돌하르방이 먼저 등장하고 난 뒤 시작) ===
    public void StartGame()
    {
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(StartGameSequence());
    }

    private IEnumerator StartGameSequence()
    {
        if (_isStarting || IsGameStarted) yield break;
        _isStarting = true;

        // 0) 시작 UI 끄기
        if (startCanvas != null && startCanvas.activeSelf)
            startCanvas.SetActive(false);

        // 1) XR Origin 생성 및 등록
        if (spawnPlayerForSingle && LocalPlayer == null && xrOriginPrefab != null)
        {
            var xrOriginGO = Instantiate(xrOriginPrefab);
            var detector = xrOriginGO.GetComponent<PlayerMovementDetector>();
            if (detector != null)
                RegisterLocalPlayer(detector);
        }

        // 2) 카메라 알림 & HUD 켜기/바인딩
        RaiseLocalCameraReady();
        EnsureHUDVisibleAndBound();

        // 3) 돌하르방 등장(지하 → 지면). 모두 끝날 때까지 대기
        var watchers = FindObjectsOfType<DolhareubangWatcher>(true);
        foreach (var w in watchers)
        {
            // Emerge가 없다면 만들어야 함(앞서 준 Watcher 수정본 참고)
            yield return w.StartCoroutine(w.Emerge());
        }

        // 4) 세션 초기화 & "게임 시작" 브로드캐스트
        SessionOrangeCount = 0;
        OnSessionOrangeCountChanged?.Invoke(SessionOrangeCount);

        IsGameStarted = true;
        OnGameStarted?.Invoke();
        Debug.Log("게임 시작: 무궁화 꽃이 피었습니다");

        _isStarting = false;
    }

    public void EndGame()
    {
        if (!IsGameStarted) return;

        Debug.Log("게임 종료!");
        IsGameStarted = false;
        OnGameEnded?.Invoke();

        // 종료 후 HUD는 공지 보여줄 시간만큼 기다렸다가 선택적으로 끔
        if (hideHudOnEnd && hudCanvas != null && hudCanvas.activeSelf)
            StartCoroutine(CoHideHudAfterDelay(hideHudDelay));

        // 돌하르방 퇴장(지면 → 지하). 병렬이 필요하면 따로 요청해줘.
        StartCoroutine(SinkAllWatchers());

        // 시작 UI 다시 켜주기
        if (startCanvas != null && !startCanvas.activeSelf)
            startCanvas.SetActive(true);
    }

    private IEnumerator CoHideHudAfterDelay(float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        if (hudCanvas != null) hudCanvas.SetActive(false);
    }

    private IEnumerator SinkAllWatchers()
    {
        var watchers = FindObjectsOfType<DolhareubangWatcher>(true);
        foreach (var w in watchers)
            yield return w.StartCoroutine(w.Sink());
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
