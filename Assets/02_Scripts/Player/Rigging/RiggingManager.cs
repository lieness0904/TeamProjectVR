using Fusion;
using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
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
    [Networked] public Vector3 HeadPos { get; set; }
    [Networked] public Quaternion HeadRot { get; set; }
    [Networked] public Vector3 LeftHandPos { get; set; }
    [Networked] public Quaternion LeftHandRot { get; set; }
    [Networked] public Vector3 RightHandPos { get; set; }
    [Networked] public Quaternion RightHandRot { get; set; }

    public float smoothValue = 0.1f;
    public float modelHeight = 1.67f;

    public override void Spawned()
    {
        if (HasInputAuthority)
        {
            // XR Origin/컨트롤러/IK 등 플레이어 프리팹 하위에 이미 있음 → Find로 할당
            if (HasInputAuthority)
            {
                // 하위 구조 경로 반드시 맞추기!
                var xrOrigin = transform.Find("XR Origin (Action-based)");
                if (xrOrigin == null)
                {
                    Debug.LogError("플레이어 프리팹에 XR Origin (Action-based) 오브젝트가 없습니다!");
                    return;
                }

                hmd = xrOrigin.Find("Camera Offset/Main Camera");
                leftHandController = xrOrigin.Find("Camera Offset/Left Controller");
                rightHandController = xrOrigin.Find("Camera Offset/Right Controller");

                if (hmd == null || leftHandController == null || rightHandController == null)
                    Debug.LogError("XR Origin 내부에 HMD/Hand Controller 경로를 다시 확인하세요!");

                // IK Target도 프리팹 내부에 이미 존재한다고 가정
                headIK = transform.Find("HeadIK");
                leftHandIK = transform.Find("LeftArmIK");
                rightHandIK = transform.Find("RightArmIK");
                if (headIK == null || leftHandIK == null || rightHandIK == null)
                    Debug.LogError("외형 프리팹에 IK Target 오브젝트가 빠졌거나 경로가 다름!");
            }
            else
            {
                // 프록시(남)일 땐 XR Origin 비활성화해도 무방
                var xrOrigin = transform.Find("XR Origin (Action-based)");
                if (xrOrigin != null) xrOrigin.gameObject.SetActive(false);

                // IK Target은 자기 위치에 있어야 하니 그대로 놔둠
                headIK = transform.Find("HeadIK");
                leftHandIK = transform.Find("LeftArmIK");
                rightHandIK = transform.Find("RightArmIK");
            }
        }
    }
    private void LateUpdate()
    {
        if (!HasInputAuthority || hmd == null) return;

        MappingHandTransform(leftHandIK, leftHandController, true);
        MappingHandTransform(rightHandIK, rightHandController, false);
        MappingBodyTransform(headIK, hmd);
        MappingHeadTransform(headIK, hmd);
    }

    private void MappingHandTransform(Transform ik, Transform controller, bool isLeft)
    {
        // ik의 Transform = controller의 Transform
        if (ik == null || controller == null) return;
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
        if (ik == null || hmd == null) return;
        ik.position = hmd.TransformPoint(headOffset[0]); 
        ik.rotation = hmd.rotation * Quaternion.Euler(headOffset[1]);
    }

    public override void FixedUpdateNetwork()
    {
        if (HasInputAuthority)
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
            // 타인 프록시는 네트워크 값만 IK에 반영
            if (headIK != null) { headIK.position = HeadPos; headIK.rotation = HeadRot; }
            if (leftHandIK != null) { leftHandIK.position = LeftHandPos; leftHandIK.rotation = LeftHandRot; }
            if (rightHandIK != null) { rightHandIK.position = RightHandPos; rightHandIK.rotation = RightHandRot; }
        }
        if (HasInputAuthority)
        {
            Debug.Log($"[IK Send] LocalPlayer:{Runner.LocalPlayer} | Authority:{Object.InputAuthority} | Head:{headIK.position} Left:{leftHandIK.position} Right:{rightHandIK.position}");
        }
        else
        {
            Debug.Log($"[IK Recv] LocalPlayer:{Runner.LocalPlayer} | Authority:{Object.InputAuthority} | Head:{HeadPos} Left:{LeftHandPos} Right:{RightHandPos}");
        }
    }
}
