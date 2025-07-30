using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class TeleportByRaycast : MonoBehaviour
{
    public LayerMask teleportLayer; 
    public Transform teleportPoint; 
    public ActionBasedController controller; 
    public ActionBasedContinuousMoveProvider moveProvider; 

    public float maxDistance = 10f;

    void Update()
    {
        if (controller.activateAction.action.WasPressedThisFrame())
        {
            if (Physics.Raycast(controller.transform.position, controller.transform.forward, out RaycastHit hit, maxDistance, teleportLayer))
            {
                CharacterController character = moveProvider.GetComponent<CharacterController>();
                if (character != null)
                {
                    character.enabled = false;
                    moveProvider.system.xrOrigin.transform.position = teleportPoint.position;
                    character.enabled = true;
                }
            }
        }
    }
}