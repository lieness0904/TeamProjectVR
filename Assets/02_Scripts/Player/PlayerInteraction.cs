using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Ray Interactors")]
    public XRRayInteractor leftRay;
    public XRRayInteractor rightRay;

    [Header("Trigger Actions")]
    public InputActionProperty leftTriggerAction;
    public InputActionProperty rightTriggerAction;

    void Update()
    {
        // 왼손 트리거
        if (leftTriggerAction.action.WasPressedThisFrame())
        {
            TryInteract(leftRay);
        }

        // 오른손 트리거
        if (rightTriggerAction.action.WasPressedThisFrame())
        {
            TryInteract(rightRay);
        }
    }

    void TryInteract(XRRayInteractor ray)
    {
        if (ray != null && ray.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            var interactable = hit.collider.GetComponent<IInteractable>();
            if (interactable != null)
            {
                interactable.Interact(transform); // 트리거 누른 플레이어 위치 전달
            }
        }
    }
}
