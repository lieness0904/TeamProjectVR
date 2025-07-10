using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class XRRootFollower : NetworkBehaviour
{
    public Transform xrOrigin;

    public Transform cameraOffset;

    public override void FixedUpdateNetwork()
    {
        if (HasInputAuthority && xrOrigin != null)
        {
            if (cameraOffset != null)
            {
                transform.position = cameraOffset.position;
                transform.rotation = Quaternion.Euler(0, cameraOffset.eulerAngles.y, 0);
            }
            Debug.Log($"카메라 오프셋 위치: {cameraOffset.position}");
        }
    }
}
