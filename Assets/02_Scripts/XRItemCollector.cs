using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class XRItemCollector : MonoBehaviour
{
    [Header("설정")]
    public XRRayInteractor rayInteractor;

    private void OnEnable()
    {
        if (rayInteractor != null)
        {
            rayInteractor.selectEntered.AddListener(OnSelectEntered);
        }
    }

    private void OnDisable()
    {
        if (rayInteractor != null)
        {
            rayInteractor.selectEntered.RemoveListener(OnSelectEntered);
        }
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        Debug.Log("[XRItemCollector] SelectEntered 발생!");

        // 선택된 오브젝트의 루트에서 CollectibleItem을 찾아본다
        var go = args.interactableObject.transform.gameObject;
        var item = go.GetComponentInParent<CollectibleItem>();

        if (item != null)
        {
            Debug.Log($"[XRItemCollector] CollectibleItem 발견: {item.name}");
            item.Collect();
        }
        else
        {
            Debug.LogWarning("[XRItemCollector] 선택된 오브젝트에 CollectibleItem이 없음");
        }
    }
}
