using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

public class CameraOffsetSolver : MonoBehaviour
{
    public float forwardOffset = 0.1f;  // 살짝 앞으로 밀기
    public float upwardOffset = 0f;     // 필요하면 위로도 이동

    private bool offsetApplied = false;

    void LateUpdate()
    {
        if (offsetApplied) return;

        XROrigin origin = GetComponent<XROrigin>();
        if (origin != null && origin.Camera != null)
        {
            Transform cam = origin.Camera.transform;

            // 현재 위치 기준으로 오프셋 적용 (Local 기준)
            Vector3 localOffset = new Vector3(0, upwardOffset, forwardOffset);
            cam.localPosition += localOffset;

            offsetApplied = true; // 한 번만 적용
        }
    }
}
