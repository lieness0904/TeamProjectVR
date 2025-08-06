using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class test : MonoBehaviour
{
    public Transform xrOrigin;
    private Vector3 lastPosition;

    void Start()
    {
        if (xrOrigin != null)
            lastPosition = xrOrigin.position;
    }

    void Update()
    {
        if (xrOrigin == null) return;

        if (xrOrigin.position != lastPosition)
        {
            Debug.Log($"[XROriginMonitor] XR Origin 이동됨: {lastPosition} → {xrOrigin.position}");
            lastPosition = xrOrigin.position;
        }
    }
}