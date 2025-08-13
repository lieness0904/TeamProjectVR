// VRWorldSpaceHUDFollower.cs
using UnityEngine;

public class VRWorldSpaceHUDFollower : MonoBehaviour
{
    [Header("타겟 (비워두면 자동 탐색)")]
    public Transform targetCamera;

    [Header("배치 옵션")]
    public float distance = 1.6f;   // 카메라 앞
    public float height = 0.0f;   // 위/아래
    public float lateralOffset = 0.0f; // ← 좌우( +는 플레이어 오른쪽 / -는 왼쪽 )
    public bool yawOnly = true;
    public float moveLerp = 12f;
    public float rotLerp = 12f;

    void LateUpdate()
    {
        if (!targetCamera)
        {
            var gm = FarmGameManager.Instance;
            if (gm && gm.LocalPlayer)
                targetCamera = gm.LocalPlayer.GetComponentInChildren<Camera>(true)?.transform;

            if (!targetCamera)
                targetCamera = Camera.main ? Camera.main.transform : FindObjectOfType<Camera>(true)?.transform;

            if (!targetCamera) return;
        }

        // 카메라의 수평 전방/오른쪽
        Vector3 fwd = Vector3.ProjectOnPlane(targetCamera.forward, Vector3.up).normalized;
        if (fwd.sqrMagnitude < 1e-4f) fwd = targetCamera.forward;

        Vector3 right = Vector3.Cross(Vector3.up, fwd).normalized; // 수평 Right

        // 오프셋 적용: 앞/좌우/높이
        Vector3 targetPos = targetCamera.position
                          + fwd * distance
                          + right * lateralOffset
                          + Vector3.up * height;

        // 위치/회전 스무딩
        transform.position = Vector3.Lerp(transform.position, targetPos, 1f - Mathf.Exp(-moveLerp * Time.deltaTime));

        Quaternion targetRot = yawOnly
            ? Quaternion.LookRotation(fwd, Vector3.up)
            : Quaternion.LookRotation((transform.position - targetCamera.position).normalized, targetCamera.up);

        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 1f - Mathf.Exp(-rotLerp * Time.deltaTime));
    }

    // 필요하면 코드로 바꾸기 쉽게
    public void SetLateral(float meters) => lateralOffset = meters;
}
