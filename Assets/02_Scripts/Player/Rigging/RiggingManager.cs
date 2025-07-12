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
        if (hmd == null || leftHandController == null || rightHandController == null) return;

        // 오프셋 적용한 손 위치
        Vector3 leftPos = leftHandController.position + leftHandController.rotation * leftHandPositionOffset;
        Quaternion leftRot = leftHandController.rotation * Quaternion.Euler(leftHandRotationOffset);

        Vector3 rightPos = rightHandController.position + rightHandController.rotation * rightHandPositionOffset;
        Quaternion rightRot = rightHandController.rotation * Quaternion.Euler(rightHandRotationOffset);

        // IK 위치 직접 적용
        if (Object.HasInputAuthority)
        {
            leftHandIK.position = leftPos;
            leftHandIK.rotation = leftRot;

            rightHandIK.position = rightPos;
            rightHandIK.rotation = rightRot;

            headIK.position = hmd.position;
            headIK.rotation = hmd.rotation;
        }

        // 호스트면 직접 할당, 아니면 RPC로 전달
        if (Object.HasStateAuthority)
        {
            NetworkHeadPos = hmd.position;
            NetworkHeadRot = hmd.rotation;
            NetworkLeftHandPos = leftPos;
            NetworkLeftHandRot = leftRot;
            NetworkRightHandPos = rightPos;
            NetworkRightHandRot = rightRot;
        }
        else
        {
            RPC_UpdateIK(hmd.position, hmd.rotation, leftPos, leftRot, rightPos, rightRot);
        }

        // 애니메이션 블렌드 계산
        Vector3 velocity = (hmd.position - lastHmdPosition) / Runner.DeltaTime;
        Vector3 localVelocity = xrOrigin.transform.InverseTransformDirection(velocity);
        lastHmdPosition = hmd.position;

        NetworkMoveBlend = new Vector2(localVelocity.x, localVelocity.z);
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_UpdateIK(Vector3 headPos, Quaternion headRot, Vector3 leftPos, Quaternion leftRot, Vector3 rightPos, Quaternion rightRot)
    {
        NetworkHeadPos = headPos;
        NetworkHeadRot = headRot;
        NetworkLeftHandPos = leftPos;
        NetworkLeftHandRot = leftRot;
        NetworkRightHandPos = rightPos;
        NetworkRightHandRot = rightRot;
    }
    public override void Render()
    {
        // IK 및 애니메이션은 로컬/리모트 모두 적용해야 함
        if (!Object.HasInputAuthority)
        {
            // 리모트 플레이어 IK 보간
            headIK.position = Vector3.Lerp(headIK.position, NetworkHeadPos, Runner.DeltaTime * 20f);
            headIK.rotation = Quaternion.Slerp(headIK.rotation, NetworkHeadRot, Runner.DeltaTime * 20f);

            Vector3 leftPos = NetworkLeftHandPos + NetworkLeftHandRot * leftHandPositionOffset;
            Quaternion leftRot = NetworkLeftHandRot * Quaternion.Euler(leftHandRotationOffset);
            leftHandIK.position = Vector3.Lerp(leftHandIK.position, leftPos, Runner.DeltaTime * 20f);
            leftHandIK.rotation = Quaternion.Slerp(leftHandIK.rotation, leftRot, Runner.DeltaTime * 20f);

            Vector3 rightPos = NetworkRightHandPos + NetworkRightHandRot * rightHandPositionOffset;
            Quaternion rightRot = NetworkRightHandRot * Quaternion.Euler(rightHandRotationOffset);
            rightHandIK.position = Vector3.Lerp(rightHandIK.position, rightPos, Runner.DeltaTime * 20f);
            rightHandIK.rotation = Quaternion.Slerp(rightHandIK.rotation, rightRot, Runner.DeltaTime * 20f);
        }

        // 애니메이션 블렌딩 (로컬 & 리모트 모두 적용)
        if (animator != null)
        {
            Vector2 current = new Vector2(animator.GetFloat("MoveX"), animator.GetFloat("MoveY"));
            Vector2 smoothed = Vector2.Lerp(current, NetworkMoveBlend, Runner.DeltaTime * blendSmoothSpeed);
            animator.SetFloat("MoveX", smoothed.x);
            animator.SetFloat("MoveY", smoothed.y);
        }
    }
}

