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

    [Header("VR 컨트롤러 Tramsform")]
    public Transform leftHandController;
    public Transform rightHandController;

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

            // --- [수정된 부분] PlayerDataManager가 없을 때를 대비한 ID 설정 ---
            string id;
            if (PlayerDataManager.Instance != null)
            {
                id = PlayerDataManager.Instance.UserID;
                // 만약의 경우를 대비해 ID가 비어있으면 임시 ID 부여
                if (string.IsNullOrEmpty(id))
                {
                    id = "Guest";
                }
            }
            else
            {
                // PlayerDataManager가 없을 경우 (Lobby 씬에서 바로 시작한 경우) 임시 ID를 부여합니다.
                id = "TestGuest";
                Debug.LogWarning("[PlayerNetworkData] PlayerDataManager.Instance를 찾을 수 없어 임시 ID를 사용합니다.");
            }
            PlayerName = id;
            // ----------------------------------------------------------------

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