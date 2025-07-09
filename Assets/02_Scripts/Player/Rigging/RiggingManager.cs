using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class RiggingManager : NetworkBehaviour
{
    [Header("IK Targets")]
    public Transform leftHandIK;
    public Transform rightHandIK;
    public Transform headIK;

    [Header("Local Controllers")]
    public Transform leftHandController;
    public Transform rightHandController;
    public Transform hmd;

    [Header("Offsets")]
    public Vector3[] leftOffset; // 0:Position, 1:Rotation
    public Vector3[] rightOffset;
    public Vector3[] headOffset;

    // 네트워크로 보낼거
    [Networked] private Vector3 HeadPos { get; set; }
    [Networked] private Quaternion HeadRot { get; set; }
    [Networked] private Vector3 LeftHandPos { get; set; }
    [Networked] private Quaternion LeftHandRot { get; set; }
    [Networked] private Vector3 RightHandPos { get; set; }
    [Networked] private Quaternion RightHandRot { get; set; }

    public float smoothValue = 0.1f;
    public float modelHeight = 1.67f;

    private void LateUpdate()
    {
        MappingHandTransform(leftHandIK, leftHandController, true);
        MappingHandTransform(rightHandIK, rightHandController, false);
        MappingBodyTransform(headIK, hmd);
        MappingHeadTransform(headIK, hmd);
    }

    private void MappingHandTransform(Transform ik, Transform controller, bool isLeft)
    {
        // ik의 Transform = controller의 Transform

        var offset = isLeft ? leftOffset : rightOffset;

        ik.position = controller.TransformPoint(offset[0]);
        ik.rotation = controller.rotation * Quaternion.Euler(offset[1]);
        
    }
    private void MappingBodyTransform(Transform ik, Transform hmd)
    {
        this.transform.position = new Vector3(hmd.position.x, hmd.position.y - modelHeight, hmd.position.z);
        float yaw = hmd.eulerAngles.y;
        var targetRotation = new Vector3(this.transform.eulerAngles.x, yaw, this.transform.eulerAngles.z);
        this.transform.rotation = Quaternion.Lerp(this.transform.rotation, Quaternion.Euler(targetRotation), smoothValue);
    }
    private void MappingHeadTransform(Transform ik, Transform hmd)
    {
        ik.position = hmd.TransformPoint(headOffset[0]); 
        ik.rotation = hmd.rotation * Quaternion.Euler(headOffset[1]);
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasInputAuthority)
        {
            headIK.position = hmd.TransformPoint(headOffset[0]);
            headIK.rotation = hmd.rotation * Quaternion.Euler(headOffset[1]);

            leftHandIK.position = leftHandController.TransformPoint(leftOffset[0]);
            leftHandIK.rotation = leftHandController.rotation * Quaternion.Euler(leftOffset[1]);

            rightHandIK.position = rightHandController.TransformPoint(rightOffset[0]);
            rightHandIK.rotation = rightHandController.rotation * Quaternion.Euler(rightOffset[1]);

            // 네트워크 전송
            HeadPos = headIK.position;
            HeadRot = headIK.rotation;

            LeftHandPos = leftHandIK.position;
            LeftHandRot = leftHandIK.rotation;

            RightHandPos = rightHandIK.position;
            RightHandRot = rightHandIK.rotation;
        }
        else
        {
            // 타인 캐릭터 → 네트워크에서 받은 값으로 IK 반영
            headIK.position = HeadPos;
            headIK.rotation = HeadRot;

            leftHandIK.position = LeftHandPos;
            leftHandIK.rotation = LeftHandRot;

            rightHandIK.position = RightHandPos;
            rightHandIK.rotation = RightHandRot;
        }
    }
}
