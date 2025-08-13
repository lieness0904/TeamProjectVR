// DolhareubangWatcher.cs
using UnityEngine;
using System.Collections;

public class DolhareubangWatcher : MonoBehaviour
{
    [Header("회전 시간")]
    public float turnDuration = 1f;
    public Vector3 forwardRotation = Vector3.zero;
    public Vector3 backwardRotation = new Vector3(0, 90, 0);

    [Header("감시 시간")]
    public float minWatchTime = 2f;
    public float maxWatchTime = 4f;

    [Header("휴식 시간")]
    public float minRestTime = 3f;
    public float maxRestTime = 6f;

    public bool IsWatching { get; private set; }

    [Header("감시 반복 횟수")]
    public int maxRounds = 10;
    private int currentRoundIndex = 0; // 0에서 시작, 뒤돌 때 +1

    private void Start()
    {
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

            // 1) 휴식 상태
            IsWatching = false;
            FarmGameManager.Instance.BroadcastWatcherFacingBack(false);
            yield return new WaitForSeconds(Random.Range(minRestTime, maxRestTime));

            // 2) 회전해서 뒤돌기 (회전 중 상태를 UI에 띄우고 싶으면 여기서 한 번 브로드캐스트 추가해도 됨)
            yield return RotateTo(backwardRotation);

            // 3) 감시 시작 = 라운드 시작
            currentRoundIndex++;
            IsWatching = true;
            FarmGameManager.Instance.BroadcastWatcherFacingBack(true);
            FarmGameManager.Instance.BroadcastRoundChanged(currentRoundIndex, maxRounds);

            // 4) 감시 유지
            yield return new WaitForSeconds(Random.Range(minWatchTime, maxWatchTime));

            // 5) 다시 정면으로 회전
            yield return RotateTo(forwardRotation);
            IsWatching = false;
            FarmGameManager.Instance.BroadcastWatcherFacingBack(false);

            Debug.Log($"라운드 {currentRoundIndex} 종료");
        }
    }

    private IEnumerator RotateTo(Vector3 targetEuler)
    {
        Quaternion start = transform.rotation;
        Quaternion end = Quaternion.Euler(targetEuler);
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
