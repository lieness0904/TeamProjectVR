using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class HeadHider : NetworkBehaviour
{
    [SerializeField] private GameObject headObject;

    public override void Spawned()
    {
        if (HasInputAuthority && headObject != null)
        {
            headObject.SetActive(false);
        }
    }
}
