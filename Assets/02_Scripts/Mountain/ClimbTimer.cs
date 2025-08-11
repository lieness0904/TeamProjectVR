using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.XR.CoreUtils;
using UnityEngine;

public class ClimbTimer : MonoBehaviour
{
    [Header("Auto-wire by name if left empty")]
    [SerializeField] private TMP_Text currentTimeText;   // TimerCanvas/currentRecord
    [SerializeField] private TMP_Text countdownText;     // TimerCanvas/Canvas/CountdownText
    [SerializeField] private GameObject recordPanel;     // TimerCanvas/RecordPanel (또는 Canvas/RecordPanel)
    [SerializeField] private TMP_Text statusText;        // TimerCanvas/RecordPanel/status
    [SerializeField] private TMP_Text bestRecordText;    // TimerCanvas/RecordPanel/bestRecord

    [Header("Follow Target")]
    [SerializeField] private Transform followTarget;     // 플레이어 카메라(자동 탐색)
    [SerializeField] private Vector3 followOffset = new Vector3(0f, 0.35f, 1.2f);
    [SerializeField] private float followLerp = 15f;
    [SerializeField] private bool faceTarget = true;

    [Header("Options")]
    [SerializeField] private int countdownSeconds = 3;
    [SerializeField] private string prefsKey = "Climb_BestTime";

    public bool IsRunning { get; private set; }
    public float Elapsed { get; private set; }
    public float BestTime { get; private set; }

    Coroutine coCountdown;

    void Awake()
    {
        AutoWireIfNeeded();
        BestTime = PlayerPrefs.GetFloat(prefsKey, -1f);

        // 초기 표시 상태
        SafeSetActive(currentTimeText, false);
        SafeSetActive(countdownText, false);
        if (recordPanel) recordPanel.SetActive(false);
    }

    void OnEnable()
    {
        // 프리팹 스폰 대비: 카메라 자동 탐색 루틴
        StartCoroutine(FindCameraRoutine());
    }

    IEnumerator FindCameraRoutine()
    {
        while (!followTarget)
        {
            var xr = FindObjectOfType<XROrigin>();
            if (xr && xr.Camera) followTarget = xr.Camera.transform;
            if (!followTarget && Camera.main) followTarget = Camera.main.transform;
            yield return null; // 다음 프레임까지 대기하며 반복 탐색
        }
    }

    void LateUpdate()
    {
        // 따라오기
        if (followTarget)
        {
            Vector3 targetPos = followTarget.position + followTarget.forward * followOffset.z
                                                      + followTarget.up * followOffset.y
                                                      + followTarget.right * followOffset.x;

            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followLerp);

            if (faceTarget)
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(transform.position - followTarget.position, Vector3.up),
                    Time.deltaTime * followLerp
                );
        }

        // 타이머 갱신
        if (IsRunning)
        {
            Elapsed += Time.deltaTime;
            if (currentTimeText) currentTimeText.text = FormatTime(Elapsed);
        }
    }

    // 스타트 버튼에서 호출
    public void StartCountdown()
    {
        if (recordPanel) recordPanel.SetActive(false);
        SetRunning(false);
        SafeSetActive(currentTimeText, false);
        SafeSetActive(countdownText, true);

        if (coCountdown != null) StopCoroutine(coCountdown);
        coCountdown = StartCoroutine(CoCountdown(countdownSeconds));
    }

    IEnumerator CoCountdown(int seconds)
    {
        Elapsed = 0f;

        for (int i = seconds; i > 0; --i)
        {
            if (countdownText) countdownText.text = i.ToString();
            yield return new WaitForSeconds(1f);
        }

        if (countdownText) countdownText.text = "Go";
        yield return new WaitForSeconds(0.4f);

        SafeSetActive(countdownText, false);
        SafeSetActive(currentTimeText, true);

        // 타이머 시작
        Elapsed = 0f;
        SetRunning(true);
        if (currentTimeText) currentTimeText.text = FormatTime(Elapsed);
    }

    // 피니시 버튼에서 호출
    public void StopAndShowRecord()
    {
        if (!IsRunning) return;
        SetRunning(false);

        // 기록 갱신
        bool newBest = BestTime < 0f || Elapsed < BestTime;
        if (newBest)
        {
            BestTime = Elapsed;
            PlayerPrefs.SetFloat(prefsKey, BestTime);
            PlayerPrefs.Save();
        }

        // UI 표시
        SafeSetActive(currentTimeText, false);
        if (recordPanel) recordPanel.SetActive(true);

        // status = 이번 기록 표시(+ 신기록 여부)
        if (statusText)
        {
            string cur = $"Time  {FormatTime(Elapsed)}";
            statusText.text = newBest ? $"{cur}\nNew Record!" : cur;
        }

        // bestRecord = 최고기록 표시
        if (bestRecordText)
            bestRecordText.text = BestTime < 0f ? "Best  --:--.--" : $"Best  {FormatTime(BestTime)}";
    }

    public void HideRecordPanel()
    {
        if (recordPanel) recordPanel.SetActive(false);
    }

    void SetRunning(bool on)
    {
        IsRunning = on;
    }

    // 유틸
    void SafeSetActive(TMP_Text t, bool on)
    {
        if (t) t.gameObject.SetActive(on);
    }

    string FormatTime(float t)
    {
        int m = Mathf.FloorToInt(t / 60f);
        float s = t - m * 60f;
        return $"{m:00}:{s:00.00}";
    }

    void AutoWireIfNeeded()
    {
        // 이름/경로 기준 자동 연결 (새 구조 우선, 구 구조 폴백)
        if (!currentTimeText)
        {
            currentTimeText = FindChildByPath<TMP_Text>(transform, "currentRecord");
            if (!currentTimeText) currentTimeText = FindChildByPath<TMP_Text>(transform, "Text (TMP)");
        }

        if (!countdownText)
            countdownText = FindChildByPath<TMP_Text>(transform, "Canvas/CountdownText");

        if (!recordPanel)
        {
            var t = transform.Find("RecordPanel");
            if (!t) t = transform.Find("Canvas/RecordPanel"); // 폴백
            recordPanel = t ? t.gameObject : null;
        }

        if (!statusText)
        {
            statusText = FindChildByPath<TMP_Text>(transform, "RecordPanel/status");
            if (!statusText) statusText = FindChildByPath<TMP_Text>(transform, "Canvas/RecordPanel/status");
        }

        if (!bestRecordText)
        {
            bestRecordText = FindChildByPath<TMP_Text>(transform, "RecordPanel/bestRecord");
            if (!bestRecordText) bestRecordText = FindChildByPath<TMP_Text>(transform, "Canvas/RecordPanel/bestRecord");
        }
    }

    T FindChildByPath<T>(Transform root, string path) where T : Component
    {
        var tr = root.Find(path);
        return tr ? tr.GetComponent<T>() : null;
    }

    public void ClosePanel()
    {
        recordPanel.SetActive(false);
    }
}