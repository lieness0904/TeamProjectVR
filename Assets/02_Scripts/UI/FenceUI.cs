using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FenceUI : MonoBehaviour
{
    [SerializeField] private GameObject fenceUI;

    private void Awake()
    {
        if (fenceUI != null)
            fenceUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (fenceUI != null)
                StartCoroutine(ShowUIRoutine());
        }
    }

    private IEnumerator ShowUIRoutine()
    {
        fenceUI.SetActive(true);
        yield return new WaitForSeconds(1f);
        fenceUI.SetActive(false);
    }
}
