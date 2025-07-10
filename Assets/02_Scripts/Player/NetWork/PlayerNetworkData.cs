using Fusion;
using TMPro;
using UnityEngine;
using Photon.Voice.Unity;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.InputSystem; // InputActionAsset을 위해 추가

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

    [Header("VR 컨트롤러 Tramsform")]
    public Transform leftHandController;
    public Transform rightHandController;

    [Header("입력 액션")] // 입력 액션을 직접 제어하기 위해 추가
    public InputActionAsset inputActionAsset;

    [Networked] private Vector3 LeftHandPos { get; set; }
    [Networked] private Quaternion LeftHandRot { get; set; }
    [Networked] private Vector3 RightHandPos { get; set; }
    [Networked] private Quaternion RightHandRot { get; set; }

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

            string id;
            if (PlayerDataManager.Instance != null)
            {
                id = PlayerDataManager.Instance.UserID;
                if (string.IsNullOrEmpty(id))
                {
                    id = "Guest";
                }
            }
            else
            {
                id = "TestGuest";
                Debug.LogWarning("[PlayerNetworkData] PlayerDataManager.Instance를 찾을 수 없어 임시 ID를 사용합니다.");
            }
            PlayerName = id;

            Debug.Log("VR 모드로 컨트롤러를 설정합니다.");
            if (characterControllerDriver != null) characterControllerDriver.enabled = true;
            if (riggingManager != null) riggingManager.enabled = true;
            if (vrCastingController != null) vrCastingController.enabled = true;

            // --- [추가된 부분] 입력 액션을 스크립트로 직접 활성화 ---
            if (inputActionAsset != null)
            {
                inputActionAsset.Enable();
                Debug.Log("[PlayerNetworkData] Input Actions enabled via script.");
            }
            else
            {
                Debug.LogError("[PlayerNetworkData] InputActionAsset이 Inspector에 할당되지 않았습니다!", this);
            }
            // --------------------------------------------------

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