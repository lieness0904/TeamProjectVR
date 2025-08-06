using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class TeleportByRaycast : MonoBehaviour
{
    public LayerMask teleportLayer;
    public string teleportPointName = "Teleportpoint";
    public ActionBasedController controller; 
    public ActionBasedContinuousMoveProvider moveProvider; 

    public float maxDistance = 10f;
    private Transform teleportPoint;

    void Start()
    {
        // 씬에서 이름으로 TeleportPoint 찾아오기
        GameObject pointObject = GameObject.Find(teleportPointName);
        if (pointObject != null)
        {
            teleportPoint = pointObject.transform;
            Debug.Log($"[TeleportByRaycast] 목표 지점 '{teleportPointName}' 찾음");
        }
        else
        {
            Debug.LogError($"[TeleportByRaycast] 목표 지점 '{teleportPointName}' 를 찾을 수 없습니다!");
        }
    }

    void Update()
    {
        if (teleportPoint == null) return;

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