using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class TeleportButton : MonoBehaviour
{
    [SerializeField] private Transform teleportTarget;
    private XRBaseInteractable interactable;
    private Transform xrOrigin;

    void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();

        if (interactable == null)
        {
            Debug.LogError("[TeleportOnSelect] XRBaseInteractable이 없습니다.");
            return;
        }

        interactable.selectEntered.AddListener(OnTeleportSelect);
    }

    private void Start()
    {
        // 플레이어에서 XR Origin (Action-based) 찾기
        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            var originTransform = player.transform.Find("XR Origin (Action-based)");
            if (originTransform != null)
            {
                xrOrigin = originTransform;
                Debug.Log("[TeleportButton] XR Origin 찾음: " + xrOrigin.name);
            }
            else
            {
                Debug.LogWarning("[TeleportButton] XR Origin을 찾지 못했습니다.");
            }
        }
        else
        {
            Debug.LogWarning("[TeleportButton] Player 태그가 있는 오브젝트를 찾지 못했습니다.");
        }
    }

    private void OnDestroy()
    {
        if (interactable != null)
            interactable.selectEntered.RemoveListener(OnTeleportSelect);
    }

    void OnTeleportSelect(SelectEnterEventArgs args)
    {
        Debug.Log("[TeleportOnSelect] selectEntered 발생");
        if (xrOrigin != null && teleportTarget != null)
        {
            xrOrigin.position = teleportTarget.position;
            Debug.Log($"[TeleportOnSelect] XR Origin을 '{teleportTarget.name}' 위치로 이동시켰습니다.");
        }
        else
        {
            Debug.LogWarning("[TeleportOnSelect] XR Origin 또는 목표 위치가 설정되지 않았습니다.");
        }
    }

    // NetworkPlayer 안의 XR Origin 자동 탐색
    Transform FindXROrigin()
    {
        GameObject player = GameObject.FindWithTag("Player"); // NetworkPlayer에 "Player" 태그가 있어야 함
        if (player == null)
        {
            Debug.LogWarning("[TeleportOnSelect] Player 태그가 지정된 오브젝트를 찾을 수 없습니다.");
            return null;
        }

        Transform xr = player.transform.Find("XR Origin (Action-based)");
        if (xr == null)
        {
            Debug.LogWarning("[TeleportOnSelect] 'XR Origin (Action-based)' 자식을 찾을 수 없습니다.");
        }

        return xr;
    }
}