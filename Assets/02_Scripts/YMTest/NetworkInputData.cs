using Fusion;
using UnityEngine;

// 네트워크를 통해 플레이어의 입력을 전달하기 위한 데이터 구조체입니다.
// 이 구조체는 컨트롤러의 움직임 방향과 점프, 기타 버튼 입력을 포함합니다.
public struct NetworkInputData : INetworkInput
{
    public Vector2 movementInput; // 왼쪽 조이스틱의 입력 값 (x, z 이동)
    public NetworkButtons buttons; // 버튼 입력 (점프 등)
}