using Fusion;
using UnityEngine;

public class RiggingManager : NetworkBehaviour
{
    // --- [변경 1] 인스펙터에서 직접 할당할 변수들 ---
    [Header("VR 장비 (입력 소스)")]
    public Transform hmd; // XR Origin > Camera Offset > Main Camera
    public Transform leftHandController; // XR Origin > Camera Offset > LeftHand Controller
    public Transform rightHandController; // XR Origin > Camera Offset > RightHand Controller

    [Header("아바타 IK 타겟 (동기화 대상)")]
    public Transform headIK; // 아바타의 머리 Bone
    public Transform leftHandIK; // 아바타의 왼손 Bone
    public Transform rightHandIK; // 아바타의 오른손 Bone

    [Header("오프셋 설정")]
    public Vector3 headPositionOffset;
    public Vector3 headRotationOffset;
    public Vector3 handPositionOffset;
    public Vector3 handRotationOffset;
    // ---------------------------------------------------

    // --- 네트워크 동기화 변수들 (이전과 동일) ---
    [Networked] private Vector3 NetworkHeadPos { get; set; }
    [Networked] private Quaternion NetworkHeadRot { get; set; }
    [Networked] private Vector3 NetworkLeftHandPos { get; set; }
    [Networked] private Quaternion NetworkLeftHandRot { get; set; }
    [Networked] private Vector3 NetworkRightHandPos { get; set; }
    [Networked] private Quaternion NetworkRightHandRot { get; set; }
    // ---------------------------------------------------

    // --- [변경 2] Spawned()와 LateUpdate()를 삭제하고 FixedUpdateNetwork()로 로직 통합 ---
    public override void FixedUpdateNetwork()
    {
        if (Object.HasInputAuthority)
        {
            // --- 이 오브젝트가 '나 자신'일 경우 ---
            // 1. 실제 VR 장비의 위치와 회전 값을 읽어옵니다.
            // 2. 오프셋을 적용합니다.
            // 3. 계산된 최종 값을 [Networked] 변수에 기록하여 다른 사람에게 전송합니다.

            // 머리 동기화
            Vector3 headPos = hmd.TransformPoint(headPositionOffset);
            Quaternion headRot = hmd.rotation * Quaternion.Euler(headRotationOffset);
            NetworkHeadPos = headPos;
            NetworkHeadRot = headRot;

            // 왼손 동기화
            Vector3 leftHandPos = leftHandController.TransformPoint(handPositionOffset);
            Quaternion leftHandRot = leftHandController.rotation * Quaternion.Euler(handRotationOffset);
            NetworkLeftHandPos = leftHandPos;
            NetworkLeftHandRot = leftHandRot;

            // 오른손 동기화
            Vector3 rightHandPos = rightHandController.TransformPoint(handPositionOffset);
            Quaternion rightHandRot = rightHandController.rotation * Quaternion.Euler(handRotationOffset);
            NetworkRightHandPos = rightHandPos;
            NetworkRightHandRot = rightHandRot;
        }
        else
        {
            // --- 이 오브젝트가 '다른 사람'일 경우 ---
            // 네트워크를 통해 수신한 [Networked] 변수의 값으로 아바타의 IK 타겟을 부드럽게 움직여줍니다.
            headIK.position = Vector3.Lerp(headIK.position, NetworkHeadPos, Time.deltaTime * 20f);
            headIK.rotation = Quaternion.Slerp(headIK.rotation, NetworkHeadRot, Time.deltaTime * 20f);

            leftHandIK.position = Vector3.Lerp(leftHandIK.position, NetworkLeftHandPos, Time.deltaTime * 20f);
            leftHandIK.rotation = Quaternion.Slerp(leftHandIK.rotation, NetworkLeftHandRot, Time.deltaTime * 20f);

            rightHandIK.position = Vector3.Lerp(rightHandIK.position, NetworkRightHandPos, Time.deltaTime * 20f);
            rightHandIK.rotation = Quaternion.Slerp(rightHandIK.rotation, NetworkRightHandRot, Time.deltaTime * 20f);
        }
    }
}