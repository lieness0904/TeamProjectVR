using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class UIGameHUD : MonoBehaviour
{
    [Header("Top Center (Watcher State)")]
    [SerializeField] private TextMeshProUGUI watcherStateText;

    [Header("Top Left (Oranges)")]
    [SerializeField] private TextMeshProUGUI orangeCountText;

    [Header("Top Right (Round)")]
    [SerializeField] private TextMeshProUGUI roundText;

    [Header("Center Announce (Big Messages)")]
    [SerializeField] private CanvasGroup announceGroup;
    [SerializeField] private TextMeshProUGUI announceText;
    [SerializeField] private float announceFadeIn = 0.15f;
    [SerializeField] private float announceHold = 1.2f;
    [SerializeField] private float announceFadeOut = 0.25f;

    private Coroutine announceCo;

    private void OnEnable()
    {
        var gm = FarmGameManager.Instance;
        if (gm == null) return;

        gm.OnGameStarted += HandleGameStarted;
        gm.OnGameEnded += HandleGameEnded;
        gm.OnSessionOrangeCountChanged += HandleCountChanged;
        gm.OnWatcherFacingBackChanged += HandleWatcherState;
        gm.OnRoundChanged += HandleRoundChanged;
        gm.OnCaught += HandleCaught;
    }

    private void OnDisable()
    {
        var gm = FarmGameManager.Instance;
        if (gm == null) return;

        gm.OnGameStarted -= HandleGameStarted;
        gm.OnGameEnded -= HandleGameEnded;
        gm.OnSessionOrangeCountChanged -= HandleCountChanged;
        gm.OnWatcherFacingBackChanged -= HandleWatcherState;
        gm.OnRoundChanged -= HandleRoundChanged;
        gm.OnCaught -= HandleCaught;
    }

    private void Start()
    {
        SetWatcherState(false);       // 기본 휴식중
        SetOrangeCount(0);            // 0개
        SetRound(0, 10);              // 총 라운드는 첫 브로드캐스트 오면 갱신됨
        HideAnnounceImmediate();
    }

    private void HandleGameStarted()
    {
        ShowAnnounce("게임 시작!");
    }

    private void HandleGameEnded()
    {
        ShowAnnounce("게임 종료!");
    }

    private void HandleCountChanged(int count)
    {
        SetOrangeCount(count);
    }

    private void HandleWatcherState(bool isWatching)
    {
        SetWatcherState(isWatching);
    }

    private void HandleRoundChanged(int current, int total)
    {
        SetRound(current, total);
    }

    private void HandleCaught()
    {
        ShowAnnounce("들켰다!");
    }

    // === UI setters ===
    private void SetWatcherState(bool watching)
    {
        if (watcherStateText == null) return;
        watcherStateText.text = watching ? "멈춰!!" : "귤 따자!!";
    }

    private void SetOrangeCount(int count)
    {
        if (orangeCountText == null) return;
        orangeCountText.text = $"{count}개";
    }

    private void SetRound(int current, int total)
    {
        if (roundText == null) return;
        roundText.text = $"라운드: {current}/{total}";
    }

    // === Announce helpers ===
    private void ShowAnnounce(string msg)
    {
        if (announceText == null || announceGroup == null) return;
        announceText.text = msg;

        if (announceCo != null) StopCoroutine(announceCo);
        announceCo = StartCoroutine(CoAnnounce());
    }

    private IEnumerator CoAnnounce()
    {
        // fade in
        announceGroup.gameObject.SetActive(true);
        announceGroup.alpha = 0f;
        float t = 0f;
        while (t < announceFadeIn)
        {
            t += Time.deltaTime;
            announceGroup.alpha = Mathf.Clamp01(t / announceFadeIn);
            yield return null;
        }
        announceGroup.alpha = 1f;

        // hold
        yield return new WaitForSeconds(announceHold);

        // fade out
        t = 0f;
        while (t < announceFadeOut)
        {
            t += Time.deltaTime;
            announceGroup.alpha = 1f - Mathf.Clamp01(t / announceFadeOut);
            yield return null;
        }
        announceGroup.alpha = 0f;
        announceGroup.gameObject.SetActive(false);
    }

    private void HideAnnounceImmediate()
    {
        if (announceGroup == null) return;
        announceGroup.alpha = 0f;
        announceGroup.gameObject.SetActive(false);
    }
}
