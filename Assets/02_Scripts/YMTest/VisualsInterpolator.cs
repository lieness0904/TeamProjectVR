using UnityEngine;
using Fusion;

// 이 스크립트는 Network Transform 대신 수동으로 보간을 처리합니다.
public class VisualsInterpolator : MonoBehaviour
{
    [Header("보간 대상")]
    [Tooltip("실제로 우리 눈에 보이며, 부드럽게 움직일 자식 오브젝트를 연결해주세요.")]
    public Transform visualTarget;

    [Header("보간 속도")]
    [Tooltip("값이 높을수록 빠르게 따라잡습니다. (10~20 추천)")]
    public float interpolationSpeed = 15f;

    void LateUpdate()
    {
        if (visualTarget == null) return;

        // visualTarget의 위치를 이 스크립트가 붙어있는 부모 오브젝트의 위치로 부드럽게 이동시킵니다.
        visualTarget.position = Vector3.Lerp(
            visualTarget.position,
            transform.position,
            Time.deltaTime * interpolationSpeed
        );

        // 회전도 동일하게 부드럽게 처리합니다.
        visualTarget.rotation = Quaternion.Slerp(
            visualTarget.rotation,
            transform.rotation,
            Time.deltaTime * interpolationSpeed
        );
    }
}