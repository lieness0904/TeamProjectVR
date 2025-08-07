using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class LoadingScreenController : MonoBehaviour
{
    public GameObject loadingPanel;
    public GameObject dot1;
    public GameObject dot2;
    public GameObject dot3;

    [Header("페이드 및 애니메이션 설정")]
    public float dotInterval = 0.4f;
    public float fadeDuration = 0.5f;

    private Coroutine dotCoroutine;
    private Coroutine fadeCoroutine;
    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = loadingPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = loadingPanel.AddComponent<CanvasGroup>();
        }

        loadingPanel.SetActive(false);
        dot1.SetActive(false);
        dot2.SetActive(false);
        dot3.SetActive(false);
    }

    public void ShowLoading()
    {
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        loadingPanel.SetActive(true);
        canvasGroup.alpha = 0;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        fadeCoroutine = StartCoroutine(FadeCanvasGroup(canvasGroup, 0, 1, fadeDuration));

        if (dotCoroutine != null) StopCoroutine(dotCoroutine);
        dotCoroutine = StartCoroutine(AnimateDots());
    }

    public void HideLoading()
    {
        if (dotCoroutine != null) StopCoroutine(dotCoroutine);
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeOutAndDeactivate());
    }
    private IEnumerator FadeOutAndDeactivate()
    {
        yield return FadeCanvasGroup(canvasGroup, 1, 0, fadeDuration);
        loadingPanel.SetActive(false);
        canvasGroup.interactable = false;
        canvasGroup.blocksRaycasts = false;
    }

    private IEnumerator FadeCanvasGroup(CanvasGroup cg, float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            cg.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        cg.alpha = to;
    }
    private IEnumerator AnimateDots()
    {
        while (true)
        {
            dot1.SetActive(true);
            dot2.SetActive(false);
            dot3.SetActive(false);
            yield return new WaitForSeconds(dotInterval);

            dot1.SetActive(true);
            dot2.SetActive(true);
            dot3.SetActive(false);
            yield return new WaitForSeconds(dotInterval);

            dot1.SetActive(true);
            dot2.SetActive(true);
            dot3.SetActive(true);
            yield return new WaitForSeconds(dotInterval);

            dot1.SetActive(false);
            dot2.SetActive(false);
            dot3.SetActive(false);
            yield return new WaitForSeconds(dotInterval);
        }
    }

}