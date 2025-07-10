using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XRRootFollower : NetworkBehaviour
{
    public Transform xrOrigin;

    void LateUpdate()
    {
        if (HasInputAuthority && xrOrigin != null)
        {
            transform.position = xrOrigin.position;
            transform.rotation = Quaternion.Euler(0, xrOrigin.eulerAngles.y, 0);
            Debug.Log($"[XRRootFollower] Cube 위치 갱신됨: {transform.position}");
        }
    }
}
