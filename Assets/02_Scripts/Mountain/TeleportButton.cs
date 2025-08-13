using System.Collections;
using System.Collections.Generic;
using Fusion;
using System.Linq;
using UnityEngine;

public class TeleportButton : MonoBehaviour
{
    [SerializeField] private Transform teleportTarget;

    // 버튼 OnPressed / XRI Select Entered 에 연결
    public void TeleportLocalPlayer()
    {
        if (!teleportTarget)
        {
            Debug.LogWarning("[TP] Teleport target not assigned.");
            return;
        }

        // 로컬 입력 권한 플레이어 찾기
        var local = FindObjectsOfType<NetworkObject>(true)
            .FirstOrDefault(o => o.HasInputAuthority);

        if (!local)
        {
            Debug.LogWarning("[TP] Local player not found yet.");
            return;
        }

        var exec = local.GetComponent<TeleportExecutor>();
        if (!exec)
        {
            Debug.LogWarning("[TP] TeleportExecutor missing on player.");
            return;
        }

        exec.RequestTeleport(teleportTarget.position, teleportTarget.rotation);
    }
}
