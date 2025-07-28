using UnityEngine;
using UnityEngine.InputSystem;

public class CastingHandler : MonoBehaviour
{
    [Header("입력 액션")]
    [Tooltip("캐스팅에 사용할 입력 액션 (예: 오른손 Trigger 또는 Grip)")]
    public InputActionReference castAction;

    [Header("참조 트랜스폼")]
    [Tooltip("플레이어의 시선 기준이 될 HMD(메인 카메라)의 Transform을 연결해주세요.")]
    public Transform hmdTransform; // 플레이어의 시선(HMD) 방향

    [Header("캐스팅 설정")]
    [Tooltip("휘두르는 힘을 증폭시킬 배율. 20~50 사이에서 시작해보세요.")]
    public float castPowerMultiplier = 30f;

    [Tooltip("스냅 캐스팅 시 적용될 최소 속도입니다. 0이 아닌 작은 값으로 설정하세요.")]
    [SerializeField] private float minCastSpeed = 5f;

    // 다른 스크립트가 최종 속도 값을 읽어갈 수 있도록 public으로 유지합니다.
    public Vector3 LastCastVelocity { get; private set; }

    // --- Private 변수 ---
    private Quaternion _lastRotation;
    private float _lastAngularSpeed;
    private bool _isPreparingToCast = false;

    void Start()
    {
        if (castAction != null && castAction.action != null)
        {
            castAction.action.Enable();
            castAction.action.started += OnCastStarted;
            castAction.action.canceled += OnCastCanceled;
        }

        // hmdTransform이 할당되지 않았을 경우, 자동으로 메인 카메라를 찾으려고 시도합니다.
        if (hmdTransform == null)
        {
            if (Camera.main != null)
            {
                hmdTransform = Camera.main.transform;
                Debug.LogWarning("HMD Transform이 자동으로 메인 카메라로 설정되었습니다. 정확한 HMD(Center-Eye Anchor 등)를 직접 할당하는 것을 권장합니다.");
            }
            else
            {
                Debug.LogError("HMD Transform이 할당되지 않았고, 메인 카메라도 찾을 수 없습니다! 캐스팅 방향이 올바르게 작동하지 않습니다.");
                this.enabled = false; // 스크립트 비활성화
            }
        }
    }

    private void OnDestroy()
    {
        if (castAction != null && castAction.action != null)
        {
            castAction.action.started -= OnCastStarted;
            castAction.action.canceled -= OnCastCanceled;
        }
    }

    private void OnCastStarted(InputAction.CallbackContext context)
    {
        _isPreparingToCast = true;
    }

    private void OnCastCanceled(InputAction.CallbackContext context)
    {
        if (!_isPreparingToCast) return;

        _isPreparingToCast = false;

        // --- [핵심 로직] ---

        // 1. 수평 방향: HMD 기준 정면 방향만 사용 (좌우 흔들림 제거)
        Vector3 horizontalDirection = hmdTransform.forward;
        horizontalDirection.y = 0;
        horizontalDirection.Normalize();

        // 2. 수직 방향: 컨트롤러가 위를 향하는 정도를 반영
        float verticalComponent = Vector3.Dot(transform.forward, Vector3.up);

        // 3. 최종 방향 조합: 수평 방향 + 수직 성분
        Vector3 finalDirection = horizontalDirection + Vector3.up * verticalComponent;

        // 비정상적인 값 방지
        if (finalDirection.sqrMagnitude < 0.01f)
        {
            finalDirection = hmdTransform.forward;
        }
        finalDirection.Normalize();

        // 4. 힘(속도) 계산
        float finalSpeed = _lastAngularSpeed;
        if (finalSpeed < 1f)
        {
            finalSpeed = minCastSpeed;
        }

        // 5. 최종 속도 계산 및 저장
        LastCastVelocity = finalDirection * finalSpeed * castPowerMultiplier;

        Debug.Log($"[CastingHandler] Cast Executed. Final Direction: {finalDirection}, Velocity: {LastCastVelocity}");
    }

    void Update()
    {
        if (_isPreparingToCast)
        {
            if (Time.deltaTime > 0)
            {
                // 회전 속도 계산
                Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(_lastRotation);
                deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

                float angularSpeed = angle / Time.deltaTime;
                _lastAngularSpeed = Mathf.Clamp(angularSpeed, 0, 300);
            }
        }
        _lastRotation = transform.rotation;
    }
}
