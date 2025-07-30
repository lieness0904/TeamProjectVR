using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.XR.Interaction.Toolkit.BaseTeleportationInteractable;
using UnityEngine.InputSystem;

public class Teleport : MonoBehaviour
{
    public Transform teleportTarget;  // 이동할 위치
    public Transform player;          // XR Origin

    public void TeleportPlayer()
    {
        if (teleportTarget != null && player != null)
        {
            player.position = teleportTarget.position;
        }
    }
}