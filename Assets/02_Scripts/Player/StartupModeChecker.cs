using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Management;
using UnityEngine.InputSystem.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

/// <summary>
/// 게임 시작 시 VR 기기 유무를 확인하고, 상황에 맞는 입력 모듈을 활성화하며,
/// 필요 시 사용자에게 컨트롤 모드 선택 UI를 표시합니다.
/// </summary>
public class StartupModeChecker : MonoBehaviour
{
    [Header("모드 선택 UI")]
    public GameObject modeSelectionPanel;
    public Button desktopModeButton;
    public Button quitButton;

    [Header("입력 모듈 (EventSystem)")]
    public XRUIInputModule vrInputModule; // VR 컨트롤러용 입력 모듈
    public InputSystemUIInputModule desktopInputModule; // 마우스/키보드용 입력 모듈

    [Header("플레이어 컨트롤러")]
    public GameObject xrOrigin; // VR 플레이어 (가상 컨트롤러 포함)

    void Start()
    {
        // 버튼에 기능 연결
        if (desktopModeButton != null)
            desktopModeButton.onClick.AddListener(OnSelectDesktopMode);
        if (quitButton != null)
            quitButton.onClick.AddListener(OnQuitGame);

        // UI 패널은 우선 숨김
        if (modeSelectionPanel != null)
            modeSelectionPanel.SetActive(false);

        CheckXRDevice();
    }

    private void CheckXRDevice()
    {
        // XRGeneralSettings를 통해 VR 기기가 활성화되었는지 확인
        if (XRGeneralSettings.Instance != null && XRGeneralSettings.Instance.Manager.activeLoader != null)
        {
            // --- VR 기기가 연결된 경우 ---
            Debug.Log($"VR 기기 '{XRGeneralSettings.Instance.Manager.activeLoader.name}'가 활성화되었습니다. VR 모드로 시작합니다.");
            GameModeManager.Instance.CurrentMode = GameModeManager.ControlMode.VR;

            // VR 입력 모듈 활성화, 데스크톱 모듈 비활성화
            if (vrInputModule != null) vrInputModule.enabled = true;
            if (desktopInputModule != null) desktopInputModule.enabled = false;

            // XR Origin 활성화
            if (xrOrigin != null) xrOrigin.SetActive(true);
        }
        else
        {
            // --- VR 기기가 없는 경우 ---
            Debug.Log("VR 기기가 감지되지 않았습니다. 데스크톱 모드 선택 UI를 표시합니다.");

            // 데스크톱 입력 모듈을 활성화하여 마우스로 버튼을 클릭할 수 있도록 준비
            if (vrInputModule != null) vrInputModule.enabled = false;
            if (desktopInputModule != null) desktopInputModule.enabled = true;

            // VR 컨트롤러는 비활성화
            if (xrOrigin != null) xrOrigin.SetActive(false);

            // 사용자에게 선택 UI 표시
            if (modeSelectionPanel != null)
                modeSelectionPanel.SetActive(true);
        }
    }

    public void OnSelectDesktopMode()
    {
        Debug.Log("데스크톱 모드가 선택되었습니다.");
        GameModeManager.Instance.CurrentMode = GameModeManager.ControlMode.Desktop;

        // 선택 완료 후 UI 숨김
        if (modeSelectionPanel != null)
            modeSelectionPanel.SetActive(false);
    }

    public void OnQuitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}