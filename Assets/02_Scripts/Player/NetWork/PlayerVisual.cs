using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerVisual : NetworkBehaviour
{
    public GameObject bodyVisual;
    public GameObject xrRig;

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            // 내 플레이어
            bodyVisual.SetActive(false);
            xrRig.SetActive(true);
        }
        else
        {
            // 타인 플레이어
            bodyVisual.SetActive(true);
            xrRig.SetActive(false);
        }
    }
}
