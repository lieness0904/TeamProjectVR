using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.XR.Interaction.Toolkit.BaseTeleportationInteractable;
using UnityEngine.InputSystem;

public class TeleportInter : MonoBehaviour
{
    public InputActionReference triggerAction;   // 트리거 입력
    public float rayDistance = 10f;
    public LayerMask interactLayerMask;          // 큐브가 있는 레이어
    public Transform player;                     // XR Origin

    private void Update()
    {
        if (triggerAction.action.WasPressedThisFrame())
        {
            Ray ray = new Ray(transform.position, transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, rayDistance, interactLayerMask))
            {
                Teleport teleport = hit.collider.GetComponent<Teleport>();
                if (teleport != null)
                {
                    teleport.TeleportPlayer();
                }
            }
        }
    }
}