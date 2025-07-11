using UnityEngine;
using Fusion;

// 네트워크를 통해 주고받을 입력 데이터의 구조를 정의합니다.
// VR 컨트롤러의 주요 입력들을 포함합니다.
public struct NetworkInputData : INetworkInput
{
    // --- 버튼 입력 ---
    public NetworkBool IsPrimaryButton;   // A 또는 X 버튼
    public NetworkBool IsSecondaryButton; // B 또는 Y 버튼
    public NetworkBool IsGrabButton;      // 그립(Grip) 버튼
    public NetworkBool IsTriggerButton;   // 트리거(Trigger) 버튼

    // --- 아날로그 입력 ---
    public float TriggerValue;            // 트리거를 누른 강도 (0.0 ~ 1.0)
    public Vector2 Thumbstick;            // 썸스틱(조이스틱)의 X, Y 축
}