using Fusion;
using TMPro;
using UnityEngine;
using Photon.Voice.Unity;
using UnityEngine.XR.Interaction.Toolkit;

public class PlayerNetworkData : NetworkBehaviour
{
    [Networked]
    public NetworkString<_16> PlayerName { get; set; }

    [Header("스폰 위치")]
    public Transform spawnPoint;

    [Header("UI")]
    public TextMeshProUGUI nameText;

    private Recorder voiceRecorder;

    [Header("모드 전환 컴포넌트")]
    [SerializeField] private DesktopPlayerController desktopController;
    [SerializeField] private CharacterControllerDriver characterControllerDriver;
    [SerializeField] private RiggingManager riggingManager;
    [SerializeField] private CastingController vrCastingController;
    [SerializeField] private Camera playerCamera;

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
                // 기본 모드(VR)로 설정
                SetupControllerForMode(GameModeManager.ControlMode.VR);
            }

            if (voiceRecorder != null)
            {
                voiceRecorder.enabled = true;
            }
        }

        UpdateNameUI();
    }

    private void SetupControllerForMode(GameModeManager.ControlMode mode)
    {
        if (mode == GameModeManager.ControlMode.Desktop)
        {
            Debug.Log("데스크톱 모드로 컨트롤러를 설정합니다.");

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