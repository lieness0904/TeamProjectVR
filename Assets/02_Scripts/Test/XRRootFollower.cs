using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XRRootFollower : NetworkBehaviour
{
    public Transform xrOrigin;

    public override void FixedUpdateNetwork()
    {
        if (HasInputAuthority && xrOrigin != null)
        {
            transform.position = xrOrigin.position;
            transform.rotation = Quaternion.Euler(0, xrOrigin.eulerAngles.y, 0); // 필요 시 Y축만 반영
        }
    }
}
