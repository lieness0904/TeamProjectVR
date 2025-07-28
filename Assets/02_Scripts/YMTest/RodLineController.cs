using UnityEngine;
using Fusion;
using System.Collections.Generic; // List를 사용하기 위해 필요합니다.

[RequireComponent(typeof(LineRenderer))]
public class RodLineController : NetworkBehaviour
{
    [Header("낚싯줄 경로 설정")]
    [Tooltip("릴부터 시작해서 낚싯대 끝(RodTip)까지, 낚싯줄이 통과할 지점들을 순서대로 넣어주세요.")]
    public List<Transform> lineGuidePoints;

    [Header("캐스팅 전 찌")]
    [Tooltip("캐스팅 전, 낚싯줄 끝에 매달려 있을 가짜 찌(Bobber)입니다.")]
    [SerializeField] private Transform precastBobber;

    // --- Private 변수 ---
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

    // LateUpdate는 모든 물리/게임 로직이 끝난 후에 호출되어 시각적인 떨림을 방지합니다.
    void LateUpdate()
    {
        if (_playerFishingController == null) return;

        if (precastBobber != null)
        {
            precastBobber.gameObject.SetActive(_playerFishingController.CurrentBobber == null);
        }

        // 1. 실제 찌(CurrentBobber)가 있는지 확인하고, 없으면 가짜 찌(precastBobber)를 최종 목적지로 사용합니다.
        Transform lineEndPoint = _playerFishingController.CurrentBobber != null
                               ? _playerFishingController.CurrentBobber.transform
                               : precastBobber;

        if (lineEndPoint == null)
        {
            _lineRenderer.enabled = false;
            return;
        }

        _lineRenderer.enabled = true;

        // 2. Line Renderer가 그려야 할 점의 총개수를 계산합니다. (가이드 포인트 개수 + 끝점 1개)
        int totalPoints = lineGuidePoints.Count + 1;
        _lineRenderer.positionCount = totalPoints;

        // 3. 가이드 포인트들을 순서대로 Line Renderer에 설정합니다.
        for (int i = 0; i < lineGuidePoints.Count; i++)
        {
            if (lineGuidePoints[i] != null)
            {
                _lineRenderer.SetPosition(i, lineGuidePoints[i].position);
            }
        }

        // 4. 마지막 점을 최종 목적지(찌)의 위치로 설정합니다.
        _lineRenderer.SetPosition(totalPoints - 1, lineEndPoint.position);
    }
}