// HUDLifecycle.cs
using UnityEngine;

public class HUDLifecycle : MonoBehaviour
{
    [SerializeField] private bool hideOnEnd = true;

    void Awake()
    {
        // 시작엔 꺼져있게(씬에서 OFF로 뒀으면 생략 가능)
        gameObject.SetActive(false);
    }

    void OnEnable()
    {
        var gm = FarmGameManager.Instance;
        if (gm != null)
        {
            gm.OnGameStarted += ShowHUD;
            gm.OnGameEnded += OnGameEnded;
        }
    }

    void OnDisable()
    {
        var gm = FarmGameManager.Instance;
        if (gm != null)
        {
            gm.OnGameStarted -= ShowHUD;
            gm.OnGameEnded -= OnGameEnded;
        }
    }

    void ShowHUD()
    {
        gameObject.SetActive(true);
    }

    void OnGameEnded()
    {
        if (hideOnEnd) gameObject.SetActive(false);
    }
}
