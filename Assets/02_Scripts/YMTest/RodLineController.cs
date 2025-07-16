using UnityEngine;
using Fusion;

[RequireComponent(typeof(LineRenderer))]
public class RodLineController : NetworkBehaviour
{
    [SerializeField] private Transform precastBobber;

    private LineRenderer _lineRenderer;
    private Transform _rodTip;
    private PlayerFishingController _playerFishingController;

    public override void Spawned()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        Debug.Log("--- RodLineController Spawned() Debug Start ---");

        // 1. RodInfo 컴포넌트 찾기
        RodInfo rodInfo = GetComponent<RodInfo>();
        if (rodInfo == null)
        {
            Debug.LogError("DEBUG: GetComponent<RodInfo>() FAILED. 'RodInfo' 컴포넌트를 이 오브젝트에서 찾을 수 없습니다.");
        }
        else
        {
            Debug.Log("DEBUG: GetComponent<RodInfo>() SUCCESS. 'RodInfo' 컴포넌트를 찾았습니다.");
            // 2. rodTip 변수 확인
            if (rodInfo.rodTip == null)
            {
                Debug.LogError("DEBUG: rodInfo.rodTip is NULL. 'RodInfo' 스크립트의 'Rod Tip' 변수가 할당되지 않았습니다.");
            }
            else
            {
                Debug.Log("DEBUG: rodInfo.rodTip is NOT NULL. 'Rod Tip' 변수가 할당되었습니다. 이름: " + rodInfo.rodTip.name);
                _rodTip = rodInfo.rodTip;
            }
        }

        // 3. PlayerObject 찾기
        var playerObject = Runner.GetPlayerObject(Object.InputAuthority);
        if (playerObject == null)
        {
            Debug.LogError("DEBUG: GetPlayerObject FAILED. 이 낚싯대를 소유한 플레이어 오브젝트를 찾을 수 없습니다.");
        }
        else
        {
            Debug.Log("DEBUG: GetPlayerObject SUCCESS. 플레이어 오브젝트를 찾았습니다. 이름: " + playerObject.name);
            // 4. PlayerFishingController 컴포넌트 찾기
            _playerFishingController = playerObject.GetComponent<PlayerFishingController>();
            if (_playerFishingController == null)
            {
                Debug.LogError("DEBUG: GetComponent<PlayerFishingController> FAILED. 플레이어 오브젝트에서 'PlayerFishingController' 컴포넌트를 찾을 수 없습니다.");
            }
            else
            {
                Debug.Log("DEBUG: GetComponent<PlayerFishingController> SUCCESS. 'PlayerFishingController'를 찾았습니다.");
            }
        }

        // 최종 확인 및 기존 에러 메시지
        if (_rodTip == null || _playerFishingController == null)
        {
            // 이 에러는 여전히 나타나지만, 위의 디버그 로그가 원인을 알려줄 것입니다.
            Debug.LogError("RodLineController가 낚싯대 끝(RodTip)이나 PlayerFishingController를 찾지 못했습니다!", this.gameObject);
            this.enabled = false;
            return;
        }

        _lineRenderer.enabled = false;
        Debug.Log("--- RodLineController Spawned() Debug End ---");
    }

    public override void Render()
    {
        if (_playerFishingController == null || _rodTip == null) return;

        NetworkObject currentBobber = _playerFishingController.CurrentBobber;
        _lineRenderer.enabled = true;

        if (currentBobber != null)
        {
            _lineRenderer.SetPosition(0, _rodTip.position);
            _lineRenderer.SetPosition(1, currentBobber.transform.position);
        }
        else if (precastBobber != null)
        {
            _lineRenderer.SetPosition(0, _rodTip.position);
            _lineRenderer.SetPosition(1, precastBobber.position);
        }
        else
        {
            _lineRenderer.enabled = false;
        }
    }
}