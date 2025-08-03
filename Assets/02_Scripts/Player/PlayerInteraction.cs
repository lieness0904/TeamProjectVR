using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class PlayerInteraction : NetworkBehaviour
{
    [Header("Ray Interactors")]
    public XRRayInteractor leftRay;
    public XRRayInteractor rightRay;

    [Header("Trigger Actions")]
    public InputActionProperty leftTriggerAction;
    public InputActionProperty rightTriggerAction;

    private NetworkCharacterController controller;

    private void Awake()
    {
        controller = GetComponent<NetworkCharacterController>();
    }

    void Update()
    {
        if (!Object.HasInputAuthority) return;

        if (leftTriggerAction.action.WasPressedThisFrame())
            TryInteract(leftRay);

        if (rightTriggerAction.action.WasPressedThisFrame())
            TryInteract(rightRay);
    }

    void TryInteract(XRRayInteractor ray)
    {
        if (ray != null && ray.TryGetCurrent3DRaycastHit(out RaycastHit hit))
        {
            var door = hit.collider.GetComponent<DoorInteraction>();
            if (door != null)
            {
                door.Interact(this); // 문에게 상호작용 요청
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void RpcTeleport(Vector3 targetPos)
    {
        if (controller != null)
        {
            controller.Teleport(targetPos);
            Debug.Log($"[PlayerInteraction] Teleported to {targetPos}");
        }
        else
        {
            Debug.LogError("[PlayerInteraction] Controller is null, cannot teleport.");
        }
    }
}
