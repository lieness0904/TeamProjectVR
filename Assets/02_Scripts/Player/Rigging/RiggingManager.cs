using Fusion;
using UnityEngine;

public class RiggingManager : NetworkBehaviour
{
    // --- 인스펙터에서 직접 할당할 변수들 ---
    [Header("제어할 오브젝트")]
    public GameObject xrOrigin; // NetworkPlayer > XR_Origin
    public GameObject characterAvatar; // NetworkPlayer > FemaleCharacter (아바타)

    [Header("VR 장비 (입력 소스)")]
    public Transform hmd; // XR Origin > Camera Offset > Main Camera
    public Transform leftHandController; // XR Origin > Camera Offset > LeftHand Controller
    public Transform rightHandController; // XR Origin > Camera Offset > RightHand Controller

    [Header("아바타 IK 타겟 (동기화 대상)")]
    public Transform headIK; // 아바타의 머리 Bone
    public Transform leftHandIK; // 아바타의 왼손 Bone
    public Transform rightHandIK; // 아바타의 오른손 Bone

    [Header("하체 애니메이션")]
    public Animator animator;
    public float blendSmoothSpeed = 10f;

    [Header("왼손 IK 오프셋 설정")]
    public Vector3 leftHandPositionOffset = Vector3.zero;
    public Vector3 leftHandRotationOffset = Vector3.zero;

    [Header("오른손 IK 오프셋 설정")]
    public Vector3 rightHandPositionOffset = Vector3.zero;
    public Vector3 rightHandRotationOffset = Vector3.zero;

    // --- 네트워크 동기화 변수들 ---
    [Networked] private Vector3 NetworkHeadPos { get; set; }
    [Networked] private Quaternion NetworkHeadRot { get; set; }
    [Networked] private Vector3 NetworkLeftHandPos { get; set; }
    [Networked] private Quaternion NetworkLeftHandRot { get; set; }
    [Networked] private Vector3 NetworkRightHandPos { get; set; }
    [Networked] private Quaternion NetworkRightHandRot { get; set; }
    [Networked] private Vector2 NetworkMoveBlend { get; set; }
 
    private Vector3 lastHmdPosition;

    public override void Spawned()
    {
        // --- [추가된 로직] 이전에 NetworkVRPlayer가 하던 역할 ---
        // 이 NetworkObject가 스폰될 때(생성될 때) 호출됩니다.
        if (Object.HasInputAuthority)
        {
            // 이 오브젝트가 '나 자신'이라면 (입력 권한이 있다면)
            // VR 장비를 활성화하고, 시각적 아바타는 비활성화합니다.
            xrOrigin.SetActive(true);
            characterAvatar.SetActive(true);

            Debug.Log("Local Player Spawned: XR Origin Activated, Avatar Deactivated.");
        }
        else
        {
            // 이 오브젝트가 '다른 사람'이라면
            // VR 장비는 비활성화하고, 시각적 아바타를 활성화합니다.
            xrOrigin.SetActive(false);
            characterAvatar.SetActive(true);
            lastHmdPosition = hmd.position;
            Debug.Log("Remote Player Spawned: XR Origin Deactivated, Avatar Activated.");
        }
    }

    public override void FixedUpdateNetwork()
    {
        // VR 장비 참조가 하나라도 없으면 실행하지 않아 오류를 방지합니다. (안전장치)
        if (hmd == null || leftHandController == null || rightHandController == null) return;

        if (Object.HasInputAuthority)
        {
            // --- 오프셋 포함한 위치/회전 계산 ---
            Vector3 leftPos = leftHandController.position + leftHandController.rotation * leftHandPositionOffset;
            Quaternion leftRot = leftHandController.rotation * Quaternion.Euler(leftHandRotationOffset);

            Vector3 rightPos = rightHandController.position + rightHandController.rotation * rightHandPositionOffset;
            Quaternion rightRot = rightHandController.rotation * Quaternion.Euler(rightHandRotationOffset);

            // --- IK 타겟 위치 업데이트 ---
            leftHandIK.position = leftPos;
            leftHandIK.rotation = leftRot;

            rightHandIK.position = rightPos;
            rightHandIK.rotation = rightRot;

            headIK.position = hmd.position;
            headIK.rotation = hmd.rotation;

            // --- '나 자신'일 경우: VR 장비 값을 읽어 네트워크로 전송 ---
            NetworkHeadPos = hmd.position;
            NetworkHeadRot = hmd.rotation;
            NetworkLeftHandPos = leftHandController.position;
            NetworkLeftHandRot = leftHandController.rotation;
            NetworkRightHandPos = rightHandController.position;
            NetworkRightHandRot = rightHandController.rotation;
        }
        else
        {
            // --- '다른 사람'일 경우: 네트워크 값을 아바타 IK에 부드럽게 적용 ---
            headIK.position = Vector3.Lerp(headIK.position, NetworkHeadPos, Time.deltaTime * 20f);
            headIK.rotation = Quaternion.Slerp(headIK.rotation, NetworkHeadRot, Time.deltaTime * 20f);

            leftHandIK.position = Vector3.Lerp(leftHandIK.position, NetworkLeftHandPos, Time.deltaTime * 20f);
            leftHandIK.rotation = Quaternion.Slerp(leftHandIK.rotation, NetworkLeftHandRot, Time.deltaTime * 20f);

            rightHandIK.position = Vector3.Lerp(rightHandIK.position, NetworkRightHandPos, Time.deltaTime * 20f);
            rightHandIK.rotation = Quaternion.Slerp(rightHandIK.rotation, NetworkRightHandRot, Time.deltaTime * 20f);
        }

        // --- 애니메이션 파라미터 반영 ---
        if (animator != null)
        {
            Vector2 target;

            if (Object.HasInputAuthority)
            {
                // 로컬 플레이어: 직접 계산
                Vector3 velocity = (hmd.position - lastHmdPosition) / Time.fixedDeltaTime;
                Vector3 localVelocity = xrOrigin.transform.InverseTransformDirection(velocity);
                target = new Vector2(localVelocity.x, localVelocity.z);
                lastHmdPosition = hmd.position;

                NetworkMoveBlend = target;
            }
            else
            {
                // 원격 플레이어: 네트워크로 받은 값
                target = NetworkMoveBlend;
            }

            Vector2 current = new Vector2(animator.GetFloat("MoveX"), animator.GetFloat("MoveY"));
            Vector2 smoothed = Vector2.Lerp(current, target, Time.deltaTime * blendSmoothSpeed);

            animator.SetFloat("MoveX", smoothed.x);
            animator.SetFloat("MoveY", smoothed.y);
        }
    }
}