using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ReelController : MonoBehaviour
{
    [Header("새로운 릴링 방식 설정")]
    [Tooltip("손잡이가 1초에 회전하는 각도입니다. (예: 360은 1초에 한 바퀴)")]
    public float fixedRotationSpeed = 360f;

    [Tooltip("찌를 1초에 감아들이는 거리(미터)입니다.")]
    public float fixedReelInSpeed = 2f;

    [Tooltip("움직임으로 감지할 최소 거리입니다. (손떨림으로 인한 오작동 방지)")]
    public float movementThreshold = 0.005f;

    [Header("연결")]
    [Tooltip("실제로 회전시킬 손잡이 파트(Part_7)입니다.")]
    public Transform handleToRotate;

    private PlayerFishingController _playerFishingController;
    private XRBaseInteractor _interactor = null;
    private bool _isGrabbed = false;
    private Vector3 _lastPosition; // 이전 프레임의 컨트롤러 위치

    void Start()
    {
        _playerFishingController = GetComponentInParent<PlayerFishingController>();
    }

    void Update()
    {
        // 릴이 잡혀있고, 컨트롤러가 할당되어 있다면
        if (_isGrabbed && _interactor != null)
        {
            // 이전 프레임과 현재 프레임의 컨트롤러 위치 차이를 계산합니다.
            float distanceMoved = Vector3.Distance(_interactor.transform.position, _lastPosition);

            // 계산된 이동 거리가 설정한 역치(threshold)보다 크면 '움직인 것'으로 간주합니다.
            if (distanceMoved > movementThreshold)
            {
                // 1. 시각적 회전: 고정된 속도로 손잡이를 회전시킵니다.
                if (handleToRotate != null)
                {
                    // Time.deltaTime을 곱해줘서 프레임 속도와 관계없이 일정한 속도로 회전하게 합니다.
                    handleToRotate.Rotate(0, 0, fixedRotationSpeed * Time.deltaTime, Space.Self);
                }

                // 2. 기능적 릴링: 고정된 속도로 찌를 감아들입니다.
                if (_playerFishingController != null)
                {
                    // PlayerFishingController의 ReelIn은 이동 '거리'를 받으므로, 속도에 시간을 곱해 전달합니다.
                    _playerFishingController.ReelIn(fixedReelInSpeed * Time.deltaTime);
                }
            }

            // 다음 프레임 계산을 위해 현재 위치를 저장합니다.
            _lastPosition = _interactor.transform.position;
        }
    }

    public void OnGrab(SelectEnterEventArgs args)
    {
        _isGrabbed = true;
        _interactor = args.interactorObject.transform.GetComponent<XRBaseInteractor>();
        if (_interactor != null)
        {
            // 처음 잡았을 때의 위치를 초기 값으로 저장하여, 잡자마자 움직이는 것을 방지합니다.
            _lastPosition = _interactor.transform.position;
        }
        Debug.Log("릴을 잡았습니다.");
    }

    public void OnRelease(SelectExitEventArgs args)
    {
        _isGrabbed = false;
        _interactor = null;
        Debug.Log("릴을 놓았습니다.");
    }
}