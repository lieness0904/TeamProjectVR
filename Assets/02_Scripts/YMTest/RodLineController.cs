using UnityEngine;
using Fusion;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class RodLineController : NetworkBehaviour
{
    [Header("낚싯줄 경로 설정")]
    [Tooltip("릴부터 시작해서 낚싯대 끝(RodTip)까지, 낚싯줄이 통과할 지점들을 순서대로 넣어주세요.")]
    public List<Transform> lineGuidePoints;

    // ▼▼▼ [삭제된 변수] ▼▼▼
    // [Tooltip("캐스팅 전, 낚싯줄 끝에 매달려 있을 가짜 찌(Bobber)입니다.")]
    // [SerializeField] private Transform precastBobber;
    // ▲▲▲ [삭제된 변수] ▲▲▲

    private LineRenderer _lineRenderer;
    private PlayerFishingController _playerFishingController;

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

        // ▼▼▼ [수정된 로직] ▼▼▼
        // 이제 '가짜 찌'를 확인하는 로직이 필요 없습니다.
        // 항상 PlayerFishingController의 CurrentBobber를 최종 목적지로 삼습니다.

        Transform lineEndPoint = _playerFishingController.CurrentBobber?.transform;

        // 1. CurrentBobber가 없으면(null이면) 낚싯줄을 그리지 않고 숨깁니다.
        if (lineEndPoint == null)
        {
            _lineRenderer.enabled = false;
            return;
        }

        // 2. CurrentBobber가 있으면 낚싯줄을 활성화하고 그립니다.
        _lineRenderer.enabled = true;

        int totalPoints = lineGuidePoints.Count + 1;
        _lineRenderer.positionCount = totalPoints;

        for (int i = 0; i < lineGuidePoints.Count; i++)
        {
            if (lineGuidePoints[i] != null)
            {
                _lineRenderer.SetPosition(i, lineGuidePoints[i].position);
            }
        }

        _lineRenderer.SetPosition(totalPoints - 1, lineEndPoint.position);
        // ▲▲▲ [수정된 로직] ▲▲▲
    }
}