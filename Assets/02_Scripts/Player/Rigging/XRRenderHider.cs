using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class XRRenderHider : NetworkBehaviour
{
    [SerializeField] private GameObject xrOriginRoot;

    public override void Spawned()
    {
        if (HasInputAuthority && xrOriginRoot != null)
        {
            foreach (var renderer in xrOriginRoot.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
            }
        }
    }
}
