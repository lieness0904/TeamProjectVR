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
            GameObject xrOriginPrefab = Resources.Load<GameObject>("LoadAssets/XR Origin (Action-based)");
            if (xrOriginPrefab != null)
            {
                GameObject xrOriginInstance = Instantiate(xrOriginPrefab);
                xrOriginInstance.transform.position = this.transform.position; // 인스턴스 위치/회전
                xrOriginInstance.transform.rotation = this.transform.rotation;

                XROrigin xr = xrOriginInstance.GetComponent<XROrigin>();
                if (xr != null)
                {
                    hmd = xr.Camera?.transform;
                    var leftHandObj = xr.transform.Find("Camera Offset/Left Controller");
                    var rightHandObj = xr.transform.Find("Camera Offset/Right Controller");
                    if (leftHandObj == null || rightHandObj == null)
                        Debug.LogWarning("XR Origin에서 핸드 컨트롤러를 못 찾음! 경로/이름 체크");

                    leftHandController = leftHandObj;
                    rightHandController = rightHandObj;
                }
                else
                {
                    Debug.LogError("XROrigin 컴포넌트 못 찾음! 프리팹에 있는지 확인");
                }
            }
            else
            {
                Debug.LogError("XR Origin 프리팹을 Resources에서 못 찾음! 경로/이름 확인");
            }
            

            // IK Target 찾기
            if (headIK == null) headIK = transform.Find("HeadIK");
            if (leftHandIK == null) leftHandIK = transform.Find("LeftArmIK");
            if (rightHandIK == null) rightHandIK = transform.Find("RightArmIK");

            if (headIK == null || leftHandIK == null || rightHandIK == null)
                Debug.LogWarning("IK Target(HeadIK/LeftHandIK/RightHandIK) 중 못 찾은 게 있음! 외형 프리팹 구조 체크");
            Debug.Log($"hmd: {hmd}, leftHandController: {leftHandController}, rightHandController: {rightHandController}");
            Debug.Log($"headIK: {headIK}, leftHandIK: {leftHandIK}, rightHandIK: {rightHandIK}");
        }
    }
    private void LateUpdate()
    {
        if (!Object.HasInputAuthority) return;

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
            // 타인 캐릭터 → 네트워크에서 받은 값으로 IK 반영
            headIK.position = HeadPos;
            headIK.rotation = HeadRot;

            leftHandIK.position = LeftHandPos;
            leftHandIK.rotation = LeftHandRot;

            rightHandIK.position = RightHandPos;
            rightHandIK.rotation = RightHandRot;
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
