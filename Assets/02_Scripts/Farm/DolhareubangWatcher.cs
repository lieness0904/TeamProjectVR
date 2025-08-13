// DolhareubangWatcher.cs
using UnityEngine;
using System.Collections;

public class DolhareubangWatcher : MonoBehaviour
{
    [Header("회전 시간")]
    public float turnDuration = 1f;

    // 월드 기준 고정 각도
    public Vector3 forwardRotation = new Vector3(0, -90, 0); // 시작각
    public Vector3 backwardRotation = new Vector3(0, 90, 0); // 뒤돌았을 때

    [Header("감시 시간")]
    public float minWatchTime = 2f;
    public float maxWatchTime = 4f;

    [Header("휴식 시간")]
    public float minRestTime = 3f;
    public float maxRestTime = 6f;

    [Header("등장/퇴장(수직 이동)")]
    public float buryDepth = 1.0f;        // 처음에 지면 아래로 얼만큼 묻을지(m)
    public float emergeDuration = 3.0f;   // 올라오는 시간
    public float sinkDuration = 3.0f;   // 내려가는 시간

    [SerializeField] private float groundY = 0f;

    private Vector3 _groundPos;  // 씬 배치 기준(지면)
    private Vector3 _buriedPos;  // 지면 아래

    public bool IsWatching { get; private set; }

    [Header("감시 반복 횟수")]
    public int maxRounds = 10;
    private int currentRoundIndex = 0;

    private void Awake()
    {
        // 씬 배치 XZ는 유지, Y만 groundY로 고정
        var p = transform.position;
        _groundPos = new Vector3(p.x, groundY, p.z);

        // 땅 아래 위치 계산
        _buriedPos = _groundPos + Vector3.down * buryDepth;

        // 시작은 묻힌 상태
        transform.position = _buriedPos;
    }

    private void Start()
    {
        // 시작 시 월드 기준 각도 강제 세팅
        transform.rotation = Quaternion.Euler(forwardRotation);

        StartCoroutine(WatchingLoop());
    }

    private IEnumerator WatchingLoop()
    {
        while (true)
        {
            if (!FarmGameManager.Instance || !FarmGameManager.Instance.IsGameStarted)
            {
                yield return null;
                continue;
            }

            if (currentRoundIndex >= maxRounds)
            {
                FarmGameManager.Instance.EndGame();
                yield break;
            }

            // 1) 휴식(정면=-90°)
            IsWatching = false;
            FarmGameManager.Instance.BroadcastWatcherFacingBack(false);
            yield return new WaitForSeconds(Random.Range(minRestTime, maxRestTime));

            // 2) 뒤돌기(= +90° 절대값)
            yield return RotateTo(backwardRotation);

            // 3) 감시 시작
            currentRoundIndex++;
            IsWatching = true;
            FarmGameManager.Instance.BroadcastWatcherFacingBack(true);
            FarmGameManager.Instance.BroadcastRoundChanged(currentRoundIndex, maxRounds);

            // 4) 감시 유지
            yield return new WaitForSeconds(Random.Range(minWatchTime, maxWatchTime));

            // 5) 정면으로(= -90° 절대값)
            yield return RotateTo(forwardRotation);
            IsWatching = false;
            FarmGameManager.Instance.BroadcastWatcherFacingBack(false);
        }
    }

    public IEnumerator Emerge()
    {
        yield return MoveTo(_groundPos, emergeDuration);
    }

    public IEnumerator Sink()
    {
        yield return MoveTo(_buriedPos, sinkDuration);
    }


    private IEnumerator RotateTo(Vector3 worldEuler)
    {
        Quaternion start = transform.rotation;
        Quaternion end = Quaternion.Euler(worldEuler);
        float elapsed = 0f;

        while (elapsed < turnDuration)
        {
            transform.rotation = Quaternion.Slerp(start, end, elapsed / turnDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.rotation = end;
    }

    private IEnumerator MoveTo(Vector3 worldTargetPos, float duration)
    {
        Vector3 startPos = transform.position;
        float elapsed = 0f;
        duration = Mathf.Max(0.0001f, duration);

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            // 살짝 이징(부드럽게)
            t = t * t * (3f - 2f * t);
            transform.position = Vector3.Lerp(startPos, worldTargetPos, t);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.position = worldTargetPos;
    }
}
