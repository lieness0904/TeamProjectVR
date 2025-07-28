using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
public class PlayerData : NetworkBehaviour
{
    [Networked] public string UserId { get; set; }
    [Networked] public int Points { get; set; }

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            UserId = PlayerDataManager.Instance.UserID;
            Points = PlayerDataManager.Instance.Points;
        }
    }
    public void TeleportTo(Vector3 position)
    {
        if (Object.HasInputAuthority)
        {
            var controller = GetComponent<NetworkCharacterController>();
            if (controller != null)
            {
                controller.Teleport(position);
            }
            else
            {
                transform.position = position; 
            }

            Debug.Log($"[Teleport] {UserId} -> {position}");
        }
    }
}
