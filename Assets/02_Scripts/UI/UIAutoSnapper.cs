using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.EventSystems;

public class UIAutoSnapper : MonoBehaviour
{
    public XRRayInteractor interactor;
    public float snapDistance = 0.03f; // 5cm 이내에서 스냅
    public float snapSpeed = 20f; // 붙는 속도
    public LayerMask uiLayer;

    private void Update()
    {
        if (interactor.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            if (((1 << hit.collider.gameObject.layer) & uiLayer) != 0)
            {
                Vector3 buttonCenter = hit.collider.bounds.center;
                float dist = Vector3.Distance(hit.point, buttonCenter);

                if (dist <= snapDistance)
                {
                    // 부드럽게 붙이기
                    interactor.attachTransform.position = Vector3.MoveTowards(
                        interactor.attachTransform.position,
                        buttonCenter,
                        Time.deltaTime * snapSpeed
                    );

                    interactor.attachTransform.rotation = Quaternion.Lerp(
                        interactor.attachTransform.rotation,
                        Quaternion.LookRotation(buttonCenter - interactor.rayOriginTransform.position),
                        Time.deltaTime * snapSpeed
                    );
                }
            }
        }
    }
}
