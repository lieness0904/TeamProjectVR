using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StickToWall : MonoBehaviour
{
    public Transform wall; // Climbing Wall
    public float offset = 0.01f;

    void Start()
    {
        foreach (Transform child in transform)
        {
            RaycastHit hit;
            Vector3 direction = -child.forward;

            if (Physics.Raycast(child.position, direction, out hit, 5f))
            {
                child.position = hit.point + hit.normal * offset;
                child.rotation = Quaternion.LookRotation(-hit.normal);
            }
        }
    }
}