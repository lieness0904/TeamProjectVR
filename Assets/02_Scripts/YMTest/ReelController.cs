using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using Fusion;

public class ReelController : NetworkBehaviour
{
    [Header("연결")]
    [Tooltip("실제로 회전시킬 손잡이 파트(Part_7)입니다. 반드시 할당해주세요.")]
    public Transform handleToRotate;

    [Header("설정")]
    [Tooltip("컨트롤러 회전 속도를 줄 감는 속도로 변환하는 배율입니다. 0.01 ~ 0.05 사이 값으로 시작해보세요.")]
    public float reelSpeedMultiplier = 0.02f;

    [Tooltip("핸들의 시각적 회전 속도를 증폭시키는 배율입니다. 1보다 큰 값을 사용하세요.")]
    public float visualRotationMultiplier = 1.5f;

    // --- Private 변수 ---
    private PlayerFishingController _playerFishingController;
    private XRBaseInteractor _interactor = null;
    private bool _isGrabbed = false;
    private Vector3 _lastPosition; // 이전 프레임의 컨트롤러 위치

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

    // --- [변경] 기존 Update() 함수를 아래 내용으로 덮어쓰세요. ---
    // --- [변경] 기존 Update() 함수를 아래 내용으로 덮어쓰세요. ---
    void Update()
    {
        // 릴을 잡고 있고, 잡은 컨트롤러와 회전시킬 핸들이 모두 지정되어 있을 때만 실행
        if (_isGrabbed && _interactor != null && handleToRotate != null)
        {
            // 1. 현재 컨트롤러 위치 가져오기
            Vector3 currentPosition = _interactor.transform.position;

            // 2. 릴의 중심(pivot)에서 '이전 위치'와 '현재 위치'까지의 방향 벡터 계산
            Vector3 lastVector = _lastPosition - transform.position;
            Vector3 currentVector = currentPosition - transform.position;

            // 3. 두 벡터 사이의 각도 계산
            float angle = Vector3.SignedAngle(lastVector, currentVector, transform.forward);

            // --- [핵심 수정] ---
            // 4. 계산된 각도가 0보다 '작을' 때 (반대 방향이 정방향이므로)만 회전 및 속도 전송
            if (angle < 0) // 부등호 방향을 > 에서 < 로 변경했습니다.
            {
                // [시각적 처리] -angle을 사용하여 값을 양수로 만들어 정방향으로 회전시킵니다.
                handleToRotate.Rotate(0, 0, angle * visualRotationMultiplier, Space.Self);

                // [게임플레이 처리] 실제 줄 감는 속도 계산 및 전송
                if (_playerFishingController != null && Time.deltaTime > 0)
                {
                    // -angle을 사용하여 속도를 양수로 계산합니다.
                    float angularSpeed = -angle / Time.deltaTime;
                    float linearSpeed = angularSpeed * reelSpeedMultiplier;
                    _playerFishingController.RPC_SetReelSpeed(linearSpeed);
                }
            }
            else
            {
                // 각도가 0 또는 양수(이제는 역방향)일 경우, 속도를 0으로 보내 릴링을 멈춤
                if (_playerFishingController != null)
                {
                    _playerFishingController.RPC_SetReelSpeed(0f);
                }
            }

            // 5. 다음 프레임을 위해 현재 위치 저장
            _lastPosition = currentPosition;
        }
    }

    public void OnGrab(SelectEnterEventArgs args)
    {
        _isGrabbed = true;
        // 잡은 컨트롤러(interactor) 정보 저장
        _interactor = args.interactorObject as XRBaseInteractor;
        if (_interactor != null)
        {
            // 처음 잡았을 때의 위치 저장
            _lastPosition = _interactor.transform.position;
        }
        // 더 이상 여기서 RPC를 호출하지 않습니다. Update에서 매 프레임 속도를 보냅니다.
    }

    public void OnRelease(SelectExitEventArgs args)
    {
        _isGrabbed = false;
        // 컨트롤러 정보 초기화
        _interactor = null;

        // 릴을 놓았을 때 릴링 속도를 0으로 설정하도록 RPC를 호출합니다.
        if (_playerFishingController != null)
        {
            _playerFishingController.RPC_SetReelSpeed(0f);
        }
    }
}