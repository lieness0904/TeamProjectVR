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

    [Header("모드 전환 컴포넌트")]
    [SerializeField] private DesktopPlayerController desktopController;
    [SerializeField] private CharacterControllerDriver characterControllerDriver;
    [SerializeField] private RiggingManager riggingManager;
    [SerializeField] private CastingController vrCastingController;
    [SerializeField] private Camera playerCamera;

    // --- [추가 1] 손 컨트롤러 동기화를 위한 변수 ---
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
            Debug.Log("[PlayerNetworkData] 내 캐릭터가 스폰되었습니다. ID 및 컨트롤 모드를 설정합니다.");

            string id = PlayerDataManager.Instance.UserID;
            PlayerName = id;

            if (GameModeManager.Instance != null)
            {
                SetupControllerForMode(GameModeManager.Instance.CurrentMode);
            }
            else
            {
                Debug.LogError("[PlayerNetworkData] GameModeManager.Instance를 찾을 수 없습니다! 컨트롤러 설정에 실패했습니다.");
                SetupControllerForMode(GameModeManager.ControlMode.VR);
            }

            if (voiceRecorder != null)
            {
                voiceRecorder.enabled = true;
            }
        }

        UpdateNameUI();
    }

    // --- [추가 2] 네트워크 동기화를 위한 FixedUpdateNetwork 함수 ---
    public override void FixedUpdateNetwork()
    {
        if (Object.HasInputAuthority)
        {
            // 내가 조종하는 캐릭터라면, 내 로컬 컨트롤러의 위치/회전 값을 네트워크 변수에 기록합니다.
            // 이 데이터가 서버(호스트)로 전송됩니다.
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
    // ----------------------------------------------------

    private void SetupControllerForMode(GameModeManager.ControlMode mode)
    {
        if (mode == GameModeManager.ControlMode.Desktop)
        {
            Debug.Log("데스크톱 모드로 컨트롤러를 설정합니다.");
            if (leftHandController != null) leftHandController.gameObject.SetActive(false);
            if (rightHandController != null) rightHandController.gameObject.SetActive(false);

            if (desktopController != null)
            {
                desktopController.enabled = true;
                desktopController.cameraTransform = playerCamera.transform;
            }

            if (characterControllerDriver != null) characterControllerDriver.enabled = false;
            if (riggingManager != null) riggingManager.enabled = false;
            if (vrCastingController != null) vrCastingController.enabled = false;
        }
        else // VR 모드일 경우
        {
            Debug.Log("VR 모드로 컨트롤러를 설정합니다.");
            if (leftHandController != null) leftHandController.gameObject.SetActive(true);
            if (rightHandController != null) rightHandController.gameObject.SetActive(true);

            if (desktopController != null) desktopController.enabled = false;
            if (characterControllerDriver != null) characterControllerDriver.enabled = true;
            if (riggingManager != null) riggingManager.enabled = true;
            if (vrCastingController != null) vrCastingController.enabled = true;
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