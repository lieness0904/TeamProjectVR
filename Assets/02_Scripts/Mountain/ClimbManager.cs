using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClimbManager : MonoBehaviour
{
    public ClimbHand leftHand;
    public ClimbHand rightHand;

    private Vector3 lastLeftPos;
    private Vector3 lastRightPos;

    void Update()
    {
        Vector3 movement = Vector3.zero;

        if (leftHand.IsGrabbing)
        {
            Vector3 delta = leftHand.CurrentPosition - lastLeftPos;
            movement -= delta;
        }

        if (rightHand.IsGrabbing)
        {
            Vector3 delta = rightHand.CurrentPosition - lastRightPos;
            movement -= delta;
        }

        if (leftHand.IsGrabbing || rightHand.IsGrabbing)
        {
            transform.position += movement;
        }

        lastLeftPos = leftHand.CurrentPosition;
        lastRightPos = rightHand.CurrentPosition;
    }
}