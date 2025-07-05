using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;

public class T_PlayerInteraction : NetworkBehaviour
{
    public float interactRange = 3f;
    public LayerMask interactLayer;

    void Update()
    {
        if (!HasInputAuthority) return;

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            Ray ray = new Ray(transform.position, transform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactLayer))
            {
                Debug.Log($"상호작용 대상: {hit.collider.name}");
                // 상호작용 로직 추가 가능
            }
        }
    }
}
