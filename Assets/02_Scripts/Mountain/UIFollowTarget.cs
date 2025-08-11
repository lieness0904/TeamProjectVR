using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;

public class UIFollowTarget : MonoBehaviour
{
    [Header("Follow / Attach")]
    [SerializeField] private bool attachAsChild = false;   // true면 카메라 자식으로 붙임
    [SerializeField] private float distance = 1f;        // 카메라 앞 거리
    [SerializeField] private float verticalOffset = 0.35f;// 살짝 아래쪽 배치
    [SerializeField] private float followLerp = 12f;       // 부드럽게 따라오는 정도
    [SerializeField] private bool faceCamera = true;       // 항상 카메라 바라보기

    private Transform cam;                                 // 따라갈 카메라

    private void OnEnable()
    {
        StartCoroutine(FindCameraRoutine());               // 런타임 스폰도 지원
    }

    private IEnumerator FindCameraRoutine()
    {
        // XROrigin 카메라 우선 → 없으면 MainCamera 태그 카메라를 계속 탐색
        while (cam == null)
        {
            var xr = FindObjectOfType<XROrigin>();
            if (xr != null && xr.Camera != null)
                cam = xr.Camera.transform;

            if (cam == null && Camera.main != null)
                cam = Camera.main.transform;

            yield return null; // 다음 프레임까지 대기하며 계속 탐색
        }

        if (attachAsChild)
        {
            transform.SetParent(cam, true); // 월드 좌표 유지한 채로 자식으로
        }
    }

    private void LateUpdate()
    {
        if (!cam) return;

        // 목표 위치(카메라 앞 distance, 약간 아래)
        Vector3 targetPos = cam.position + cam.forward * distance + cam.up * verticalOffset;

        if (attachAsChild)
        {
            // 자식으로 붙였을 때도 위치 고정
            transform.position = targetPos;
            if (faceCamera)
                transform.rotation = Quaternion.LookRotation(transform.position - cam.position, Vector3.up);
        }
        else
        {
            // 부드럽게 따라오기
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * followLerp);
            if (faceCamera)
            {
                var rot = Quaternion.LookRotation(transform.position - cam.position, Vector3.up);
                transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * followLerp);
            }
        }
    }
}
