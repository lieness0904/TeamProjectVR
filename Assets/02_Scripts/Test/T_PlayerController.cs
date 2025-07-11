using UnityEngine;
using Fusion;
using UnityEngine.InputSystem; // Input Action을 사용하기 위해 필수

/// <summary>
/// 이 스크립트는 더 이상 캐릭터를 직접 움직이지 않습니다.
/// 오직 로컬 플레이어의 VR 컨트롤러 입력을 읽어서
/// T_NetworkManager가 요청할 때 제공하는 역할을 담당합니다.
/// </summary>
public class T_PlayerController : NetworkBehaviour
{
    [Header("--- Left Hand Input Actions ---")]
    [SerializeField] private InputActionReference leftPrimaryButtonAction;   // X 버튼
    [SerializeField] private InputActionReference leftSecondaryButtonAction; // Y 버튼
    [SerializeField] private InputActionReference leftGripAction;            // 그립 버튼
    [SerializeField] private InputActionReference leftTriggerAction;         // 트리거 버튼
    [SerializeField] private InputActionReference leftTriggerValueAction;    // 트리거 누른 값 (0~1)
    [SerializeField] private InputActionReference leftThumbstickAction;      // 조이스틱

    [Header("--- Right Hand Input Actions ---")]
    [SerializeField] private InputActionReference rightPrimaryButtonAction;  // A 버튼
    [SerializeField] private InputActionReference rightSecondaryButtonAction;// B 버튼
    [SerializeField] private InputActionReference rightGripAction;           // 그립 버튼
    [SerializeField] private InputActionReference rightTriggerAction;        // 트리거 버튼
    [SerializeField] private InputActionReference rightTriggerValueAction;   // 트리거 누른 값 (0~1)
    [SerializeField] private InputActionReference rightThumbstickAction;     // 조이스틱

    /// <summary>
    /// T_NetworkManager가 호출하여 현재 프레임의 네트워크 입력을 가져가는 함수
    /// </summary>
    /// <returns>네트워크로 보낼 입력 데이터</returns>
    public NetworkInputData GetNetworkInput()
    {
        var data = new NetworkInputData();

        // --- 버튼 입력 처리 ---
        // 양손 중 하나라도 눌리면 true로 처리
        data.IsPrimaryButton = leftPrimaryButtonAction.action.IsPressed() || rightPrimaryButtonAction.action.IsPressed();
        data.IsSecondaryButton = leftSecondaryButtonAction.action.IsPressed() || rightSecondaryButtonAction.action.IsPressed();
        data.IsGrabButton = leftGripAction.action.IsPressed() || rightGripAction.action.IsPressed();
        data.IsTriggerButton = leftTriggerAction.action.IsPressed() || rightTriggerAction.action.IsPressed();

        // --- 아날로그 값 처리 ---
        // 여기서는 편의상 오른손 입력을 기준으로 하지만, 프로젝트에 맞게 수정할 수 있습니다.
        // 예를 들어 왼손잡이 모드를 지원하거나, 양손의 값을 조합할 수도 있습니다.
        data.TriggerValue = rightTriggerValueAction.action.ReadValue<float>();
        data.Thumbstick = rightThumbstickAction.action.ReadValue<Vector2>();

        return data;
    }
}