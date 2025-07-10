using Fusion;
using TMPro;
using UnityEngine;
using Photon.Voice.Unity;
using UnityEngine.XR.Interaction.Toolkit;

public class PlayerNetworkData : NetworkBehaviour
{
    [Networked]
    public NetworkString<_16> PlayerName { get; set; }

    [Header("UI")]
    public TextMeshProUGUI nameText;

    [Header("VR 전용 컴포넌트")]
    [SerializeField] private CharacterControllerDriver characterControllerDriver;
    [SerializeField] private RiggingManager riggingManager;
    [SerializeField] private CastingController vrCastingController;
    // [SerializeField] private Camera playerCamera; // 데스크톱 모드에서만 사용했으므로 주석 처리하거나 삭제해도 됩니다.

    // --- 손 컨트롤러 동기화를 위한 변수 ---
    [Header("VR 컨트롤러 Tramsform")]
    public Transform leftHandController;
    public Transform rightHandController;

    [Networked] private Vector3 LeftHandPos { get; set; }
    [Networked] private Quaternion LeftHandRot { get; set; }
    [Networked] private Vector3 RightHandPos { get; set; }
    [Networked] private Quaternion RightHandRot { get; set; }
    // -----------------------------------------

    private Recorder voiceRecorder;

    private void Awake()
    {
        voiceRecorder = GetComponent<Recorder>();
    }

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            Debug.Log("[PlayerNetworkData] 내 캐릭터가 스폰되었습니다. ID 및 VR 컨트롤러를 설정합니다.");

            // 유저 ID 설정
            string id = PlayerDataManager.Instance.UserID;
            PlayerName = id;

            // --- 컨트롤러 설정 (VR 모드 고정) ---
            Debug.Log("VR 모드로 컨트롤러를 설정합니다.");
            if (characterControllerDriver != null) characterControllerDriver.enabled = true;
            if (riggingManager != null) riggingManager.enabled = true;
            if (vrCastingController != null) vrCastingController.enabled = true;
            // ------------------------------------

            // 보이스 레코더 활성화
            if (voiceRecorder != null)
            {
                voiceRecorder.enabled = true;
            }
        }

        UpdateNameUI();
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasInputAuthority)
        {
            // 내가 조종하는 캐릭터라면, 내 로컬 컨트롤러의 위치/회전 값을 네트워크 변수에 기록합니다.
            if (leftHandController != null)
            {
                LeftHandPos = leftHandController.position;
                LeftHandRot = leftHandController.rotation;
            }
            if (rightHandController != null)
            {
                RightHandPos = rightHandController.position;
                RightHandRot = rightHandController.rotation;
            }
        }
        else
        {
            // 내가 조종하는 캐릭터가 아니라면 (다른 사람 캐릭터),
            // 네트워크를 통해 받은 위치/회전 값을 실제 컨트롤러 오브젝트에 적용합니다.
            if (leftHandController != null)
            {
                leftHandController.position = LeftHandPos;
                leftHandController.rotation = LeftHandRot;
            }
            if (rightHandController != null)
            {
                rightHandController.position = RightHandPos;
                rightHandController.rotation = RightHandRot;
            }
        }
    }

    public override void Render()
    {
        UpdateNameUI();
    }

    private void UpdateNameUI()
    {
        if (nameText != null && nameText.text != PlayerName.Value)
        {
            nameText.text = PlayerName.Value;
        }
    }
}