using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.EventSystems;

public class UIAutoSnapper : MonoBehaviour
{
    public XRRayInteractor interactor;
    public float snapDistance = 0.03f; // 5cm 이내에서 스냅
    public LayerMask uiLayer;

    private void Update()
    {
        if (interactor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            if (((1 << hit.collider.gameObject.layer) & uiLayer) != 0)
            {
                // 버튼 중앙으로 Ray endpoint를 순간이동시킴
                Vector3 buttonCenter = hit.collider.bounds.center;
                float dist = Vector3.Distance(hit.point, buttonCenter);

                if (dist <= snapDistance)
                {
                    interactor.attachTransform.position = buttonCenter;
                    interactor.attachTransform.rotation = Quaternion.LookRotation(buttonCenter - interactor.rayOriginTransform.position);
                }
            }
        }
    }
}
