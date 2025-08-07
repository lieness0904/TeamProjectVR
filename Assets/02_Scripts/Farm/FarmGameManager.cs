using UnityEngine;

public class FarmGameManager : MonoBehaviour
{
    public static FarmGameManager Instance;

    [SerializeField] private GameObject xrOriginPrefab;
    private PlayerMovementDetector movementDetector;

    private void Awake()
    {
        Instance = this;
    }

    public bool IsGameStarted { get; private set; } = false;

    public void StartGame()
    {
        if (IsGameStarted) return;

        // XR Origin 인스턴스 생성
        var xrOriginGO = Instantiate(xrOriginPrefab);
        movementDetector = xrOriginGO.GetComponent<PlayerMovementDetector>();

        // 돌하르방 찾고 전달
        var watchers = FindObjectsOfType<DolhareubangWatcher>();
        if (watchers.Length > 0)
            movementDetector.SetWatcher(watchers[0]);

        Debug.Log("게임 시작: 무궁화 꽃이 피었습니다");
        IsGameStarted = true;
    }

    public void EndGame()
    {
        Debug.Log("게임 종료!");
        IsGameStarted = false;
    }
}
