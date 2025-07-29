using UnityEngine;
using Fusion;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class RodLineController : NetworkBehaviour
{
    [Header("낚싯줄 경로 설정")]
    [Tooltip("릴부터 RodTip까지, 낚싯줄이 통과할 지점들을 순서대로 넣어주세요.")]
    public List<Transform> lineGuidePoints;

    [Header("찌 아래 바늘(Hook) 설정")]
    [Tooltip("찌 하단에 생성할 바늘 프리팹(Hook)을 할당하세요.")]
    public GameObject hookPrefab;
    [Tooltip("찌 아래 바늘의 오프셋(찌 local축 기준, -Y가 아래입니다).")]
    public float hookOffset = 0.15f; // 줄 최대 길이(찌-바늘 거리)와 일치하게 조절
    [Tooltip("찌-바늘 최대 거리(SpringJoint maxDistance)")]
    public float maxSpringDistance = 0.7f;

    private LineRenderer _lineRenderer;
    private PlayerFishingController _playerFishingController;
    private GameObject spawnedHook;      // 런타임에 생성된 바늘
    private Transform hookEyeTransform;  // Hook의 HookEye 위치
    private SpringJoint hookSpringJoint; // 바늘과 찌를 연결하는 SpringJoint
    private Transform currentBobber;     // 찌 Transform
    private Rigidbody bobberRigidbody;   // 찌 Rigidbody

    public override void Spawned()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        var playerObject = Runner.GetPlayerObject(Object.InputAuthority);

        if (playerObject != null)
        {
            _playerFishingController = playerObject.GetComponent<PlayerFishingController>();
        }

        if (_playerFishingController == null)
        {
            Debug.LogError("RodLineController가 PlayerFishingController를 찾지 못했습니다!", this.gameObject);
            this.enabled = false;
            return;
        }

        if (lineGuidePoints == null || lineGuidePoints.Count == 0)
        {
            Debug.LogError("Line Guide Points가 설정되지 않았습니다!", this.gameObject);
            this.enabled = false;
            return;
        }
    }

    void LateUpdate()
    {
        if (_playerFishingController == null) return;

        // 1. 현재 찌 오브젝트를 가져옴
        currentBobber = _playerFishingController.CurrentBobber?.transform;
        bobberRigidbody = currentBobber != null ? currentBobber.GetComponent<Rigidbody>() : null;

        // 2. 찌가 없으면 낚싯줄/바늘 숨김
        if (currentBobber == null)
        {
            _lineRenderer.enabled = false;
            if (spawnedHook != null) spawnedHook.SetActive(false);
            return;
        }

        // 3. 찌가 있을 때 낚싯줄, 바늘 모두 활성화
        _lineRenderer.enabled = true;

        // 바늘(Hook) 생성 및 관리
        if (hookPrefab != null)
        {
            if (spawnedHook == null)
            {
                spawnedHook = Instantiate(hookPrefab);
                spawnedHook.name = "Hook_Instance";

                // HookEye 탐색
                hookEyeTransform = spawnedHook.transform.Find("HookEye");
                if (hookEyeTransform == null)
                    Debug.LogWarning("HookEye 트랜스폼을 찾지 못했습니다! Hook 프리팹에 'HookEye' 자식 오브젝트가 있는지 확인하세요.");

                // Rigidbody 필수
                var hookRb = spawnedHook.GetComponent<Rigidbody>();
                if (hookRb == null)
                    hookRb = spawnedHook.AddComponent<Rigidbody>();

                // SpringJoint 세팅
                hookSpringJoint = spawnedHook.GetComponent<SpringJoint>();
                if (hookSpringJoint == null)
                    hookSpringJoint = spawnedHook.AddComponent<SpringJoint>();

                hookSpringJoint.autoConfigureConnectedAnchor = false;
                hookSpringJoint.anchor = hookEyeTransform != null ?
                    spawnedHook.transform.InverseTransformPoint(hookEyeTransform.position) : Vector3.zero;
                hookSpringJoint.connectedAnchor = Vector3.zero; // 찌의 중심

                // Spring/Damper/거리 값 임시 (필요시 인스펙터에서 변경)
                hookSpringJoint.spring = 200f;
                hookSpringJoint.damper = 10f;
                hookSpringJoint.maxDistance = maxSpringDistance; // 줄 최대 거리와 맞춤

                hookSpringJoint.enableCollision = false;
            }

            if (!spawnedHook.activeSelf)
                spawnedHook.SetActive(true);

            // Joint 연결 관리 (찌가 바뀔 때마다 연결 갱신)
            if (hookSpringJoint != null && (hookSpringJoint.connectedBody != bobberRigidbody))
            {
                hookSpringJoint.connectedBody = bobberRigidbody;
            }

            // 물리적으로 움직이도록 Hook의 Rigidbody가 isKinematic=false 여야 함
            var rb = spawnedHook.GetComponent<Rigidbody>();
            if (rb != null && rb.isKinematic)
                rb.isKinematic = false;

            // HookEye의 월드 위치 (라인 끝점)
            Vector3 lineEndPos = (hookEyeTransform != null) ? hookEyeTransform.position : spawnedHook.transform.position;

            // 4. LineRenderer 포인트 설정 (Rod → RodTip → 찌 → 바늘(HookEye))
            int totalPoints = lineGuidePoints.Count + 2; // +1(찌) +1(바늘)
            _lineRenderer.positionCount = totalPoints;

            // 가이드 포인트 (릴 ~ RodTip)
            for (int i = 0; i < lineGuidePoints.Count; i++)
            {
                if (lineGuidePoints[i] != null)
                {
                    _lineRenderer.SetPosition(i, lineGuidePoints[i].position);
                }
            }

            // 찌 위치 (RodTip 바로 다음)
            _lineRenderer.SetPosition(lineGuidePoints.Count, currentBobber.position);
            // 바늘의 HookEye 위치 (마지막)
            _lineRenderer.SetPosition(totalPoints - 1, lineEndPos);
        }
        else
        {
            // Hook 프리팹이 할당되지 않았을 때: RodTip → 찌까지만 줄 그림
            int totalPoints = lineGuidePoints.Count + 1;
            _lineRenderer.positionCount = totalPoints;

            for (int i = 0; i < lineGuidePoints.Count; i++)
            {
                if (lineGuidePoints[i] != null)
                {
                    _lineRenderer.SetPosition(i, lineGuidePoints[i].position);
                }
            }

            _lineRenderer.SetPosition(totalPoints - 1, currentBobber.position);
        }
    }
}
