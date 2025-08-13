// VRWorldSpaceHUDFollower.cs
using UnityEngine;

public class VRWorldSpaceHUDFollower : MonoBehaviour
{
    [Header("타겟 (비워두면 자동 탐색)")]
    public Transform targetCamera;

    [Header("배치 옵션")]
    public float distance = 1.6f;   // 카메라 앞 거리(m)
    public float height = 0.0f;   // 기준에서 추가 높이
    public bool yawOnly = true;   // Yaw만 맞춰서 빌보드(추천)
    public float moveLerp = 12f;    // 위치 추적 속도
    public float rotLerp = 12f;    // 회전 추적 속도

    void LateUpdate()
    {
        if (targetCamera == null)
        {
            // FarmGameManager 경유 → 로컬 플레이어 카메라 먼저
            var gm = FarmGameManager.Instance;
            if (gm != null && gm.LocalPlayer != null)
                targetCamera = gm.LocalPlayer.GetComponentInChildren<Camera>(true)?.transform;

            if (targetCamera == null)
                targetCamera = Camera.main ? Camera.main.transform : FindObjectOfType<Camera>(true)?.transform;

            if (targetCamera == null) return;
        }

        // 평면 전방(헤드의 yaw 방향)
        Vector3 fwd = Vector3.ProjectOnPlane(targetCamera.forward, Vector3.up).normalized;
        if (fwd.sqrMagnitude < 0.0001f) fwd = targetCamera.forward;

        Vector3 targetPos = targetCamera.position + fwd * distance + Vector3.up * height;

        // 위치 스무딩
        transform.position = Vector3.Lerp(transform.position, targetPos, 1f - Mathf.Exp(-moveLerp * Time.deltaTime));

        // 회전: yawOnly면 수직축 기준, 아니면 카메라를 정면으로 보도록
        Quaternion targetRot = yawOnly
            ? Quaternion.LookRotation(fwd, Vector3.up)
            : Quaternion.LookRotation((transform.position - targetCamera.position).normalized, targetCamera.up);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-rotLerp * Time.deltaTime));
    }
}
