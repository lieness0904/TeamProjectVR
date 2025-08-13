// HUDLifecycle.cs
using UnityEngine;
using System.Collections;

public class HUDLifecycle : MonoBehaviour
{
    [SerializeField] private bool hideOnEnd = true;
    [SerializeField] private float hideDelayOnEnd = 1.6f; // announceHold(1.2)+여유


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
        if (!hideOnEnd) return;
        StopAllCoroutines();
        StartCoroutine(CoHide());
    }

    IEnumerator CoHide()
    {
        yield return new WaitForSeconds(hideDelayOnEnd);
        gameObject.SetActive(false);
    }
}
