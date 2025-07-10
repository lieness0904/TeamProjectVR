using Fusion;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerVisual : NetworkBehaviour
{
    public GameObject xrOrigin;
    public GameObject fullBodyVisual;

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            if (xrOrigin != null) xrOrigin.SetActive(true);
            if (fullBodyVisual != null) fullBodyVisual.SetActive(true);
        }
        else
        {
            if (xrOrigin != null) xrOrigin.SetActive(false);
            if (fullBodyVisual != null) fullBodyVisual.SetActive(true);
        }
    }
}