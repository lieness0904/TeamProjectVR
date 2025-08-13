// DolhareubangWatcher.cs
using UnityEngine;
using System.Collections;

public class DolhareubangWatcher : MonoBehaviour
{
    [Header("회전 시간")]
    public float turnDuration = 1f;

    // 월드 기준 고정 각도 (정면 0°, 뒤 180°)
    public Vector3 forwardRotation = new Vector3(0, 0, 0);     // 시작 각도
    public Vector3 backwardRotation = new Vector3(0, 180, 0);  // 뒤돌았을 때

    [Header("감시 시간")]
    public float minWatchTime = 2f;
    public float maxWatchTime = 4f;

    [Header("휴식 시간")]
    public float minRestTime = 3f;
    public float maxRestTime = 6f;

    public bool IsWatching { get; private set; }

    [Header("감시 반복 횟수")]
    public int maxRounds = 10;
    private int currentRoundIndex = 0;

    private void Start()
    {
        // 시작 시 월드 기준 각도 강제 세팅 (정면 0°)
        transform.rotation = Quaternion.Euler(forwardRotation);

        Debug.Log("돌하르방 감시 시작");
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

            // 1) 휴식(정면=0°)
            IsWatching = false;
            FarmGameManager.Instance.BroadcastWatcherFacingBack(false);
            yield return new WaitForSeconds(Random.Range(minRestTime, maxRestTime));

            // 2) 뒤돌기(= 180° 절대값)
            yield return RotateTo(backwardRotation);

            // 3) 감시 시작
            currentRoundIndex++;
            IsWatching = true;
            FarmGameManager.Instance.BroadcastWatcherFacingBack(true);
            FarmGameManager.Instance.BroadcastRoundChanged(currentRoundIndex, maxRounds);

            // 4) 감시 유지
            yield return new WaitForSeconds(Random.Range(minWatchTime, maxWatchTime));

            // 5) 정면으로(= 0° 절대값)
            yield return RotateTo(forwardRotation);
            IsWatching = false;
            FarmGameManager.Instance.BroadcastWatcherFacingBack(false);

            Debug.Log($"라운드 {currentRoundIndex} 종료");
        }
    }

    private IEnumerator RotateTo(Vector3 worldEuler)
    {
        Quaternion start = transform.rotation;
        Quaternion end = Quaternion.Euler(worldEuler); // 월드 절대 회전
        float elapsed = 0f;

        while (elapsed < turnDuration)
        {
            transform.rotation = Quaternion.Slerp(start, end, elapsed / turnDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.rotation = end;
    }
}
