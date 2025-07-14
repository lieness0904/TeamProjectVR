using UnityEngine;
using Fusion;

public class NetworkVRPlayer : NetworkBehaviour
{
    [Header("오브젝트 연결")]
    // 로컬 플레이어일 때 활성화될 VR 장비 rig
    public GameObject xrOrigin;
    // 리모트 플레이어일 때 활성화될 시각적 아바타
    public GameObject characterAvatar;

    [Header("동기화할 Transform")]
    // 실제 VR 장비의 Transform (입력 소스)
    public Transform xrHead;
    public Transform xrLeftHand;
    public Transform xrRightHand;

    // 다른 사람에게 보여질 아바타의 Transform (동기화 대상)
    public Transform avatarHead;
    public Transform avatarLeftHand;
    public Transform avatarRightHand;

    
    // 머리와 손의 위치/회전 값을 네트워크로 동기화하기 위한 변수
    [Networked] public Vector3 NetworkHeadPos { get; set; }
    [Networked] public Quaternion NetworkHeadRot { get; set; }
    [Networked] public Vector3 NetworkLeftHandPos { get; set; }
    [Networked] public Quaternion NetworkLeftHandRot { get; set; }
    [Networked] public Vector3 NetworkRightHandPos { get; set; }
    [Networked] public Quaternion NetworkRightHandRot { get; set; }


    public override void Spawned()
    {
        // 이 NetworkObject가 스폰될 때(생성될 때) 호출됩니다.
        if (Object.HasInputAuthority)
        {
            // 이 오브젝트가 '나 자신'이라면 (입력 권한이 있다면)
            // VR 장비를 활성화하고, 시각적 아바타는 비활성화합니다.
            xrOrigin.SetActive(true);
            characterAvatar.SetActive(false);
        }
        else
        {
            // 이 오브젝트가 '다른 사람'이라면
            // VR 장비는 비활성화하고, 시각적 아바타를 활성화합니다.
            xrOrigin.SetActive(false);
            characterAvatar.SetActive(true);
        }
    }

    public override void FixedUpdateNetwork()
    {
        // 고정된 네트워크 틱마다 호출됩니다.
        if (Object.HasInputAuthority)
        {
            // '나 자신'이라면, 실제 VR 장비의 위치/회전 값을 읽어서
            // [Networked] 변수에 기록합니다. 이 데이터가 다른 사람에게 전송됩니다.
            NetworkHeadPos = xrHead.position;
            NetworkHeadRot = xrHead.rotation;
            NetworkLeftHandPos = xrLeftHand.position;
            NetworkLeftHandRot = xrLeftHand.rotation;
            NetworkRightHandPos = xrRightHand.position;
            NetworkRightHandRot = xrRightHand.rotation;
        }
        else
        {
            // '다른 사람'이라면, 네트워크를 통해 수신한 [Networked] 변수의 값으로
            // 시각적 아바타의 머리와 손을 움직여줍니다.
            avatarHead.position = NetworkHeadPos;
            avatarHead.rotation = NetworkHeadRot;
            avatarLeftHand.position = NetworkLeftHandPos;
            avatarLeftHand.rotation = NetworkLeftHandRot;
            avatarRightHand.position = NetworkRightHandPos;
            avatarRightHand.rotation = NetworkRightHandRot;
        }
    }
}