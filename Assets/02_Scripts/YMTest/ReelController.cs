using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Fusion; // 포톤 퓨전 네임스페이스 추가

// MonoBehaviour에서 NetworkBehaviour로 변경
public class ReelController : NetworkBehaviour
{
    [Header("새로운 릴링 방식 설정")]
    [Tooltip("손잡이가 1초에 회전하는 각도입니다. (예: 360은 1초에 한 바퀴)")]
    public float fixedRotationSpeed = 360f;

    
    [Tooltip("움직임으로 감지할 최소 거리입니다. (손떨림으로 인한 오작동 방지)")]
    public float movementThreshold = 0.005f;

    [Header("연결")]
    [Tooltip("실제로 회전시킬 손잡이 파트(Part_7)입니다.")]
    public Transform handleToRotate;

    private PlayerFishingController _playerFishingController;
    private XRBaseInteractor _interactor = null;
    private bool _isGrabbed = false;
    private Vector3 _lastPosition; // 이전 프레임의 컨트롤러 위치

    // --- [수정] Start() 대신 Spawned() 사용 ---
    public override void Spawned()
    {
        // 이 오브젝트(낚싯대)의 소유권을 가진 플레이어를 찾습니다.
        var playerObject = Runner.GetPlayerObject(Object.InputAuthority);
        if (playerObject != null)
        {
            // 찾은 플레이어 오브젝트에서 PlayerFishingController 컴포넌트를 가져옵니다.
            _playerFishingController = playerObject.GetComponent<PlayerFishingController>();
            if (_playerFishingController == null)
            {
                Debug.LogError("ReelController: PlayerFishingController를 찾지 못했습니다!", playerObject);
            }
        }
        else
        {
            Debug.LogError("ReelController: 이 낚싯대의 소유 플레이어를 찾지 못했습니다!", this.gameObject);
        }
    }

    void Update()
    {
        // 릴을 잡고 있을 때만 시각적으로 핸들을 회전시킵니다.
        if (_isGrabbed)
        {
            if (handleToRotate != null)
            {
                handleToRotate.Rotate(0, 0, fixedRotationSpeed * Time.deltaTime, Space.Self);
            }
        }
    }


    public void OnGrab(SelectEnterEventArgs args)
    {
        _isGrabbed = true; // <-- 이 줄을 추가해주세요.
        if (_playerFishingController != null)
        {
            _playerFishingController.RPC_SetReelingState(true);
        }
        Debug.Log("릴을 잡았습니다 (상태 True 전송).");
    }

    public void OnRelease(SelectExitEventArgs args)
    {
        _isGrabbed = false; // <-- 이 줄을 추가해주세요.
        if (_playerFishingController != null)
        {
            _playerFishingController.RPC_SetReelingState(false);
        }
        Debug.Log("릴을 놓았습니다 (상태 False 전송).");
    }

}