using UnityEngine;
using UnityEngine.InputSystem;

public class CastingHandler : MonoBehaviour
{
    [Header("입력 액션")]
    [Tooltip("캐스팅에 사용할 입력 액션 (예: 오른손 Trigger 또는 Grip)")]
    public InputActionReference castAction;

    [Header("캐스팅 설정")]
    [Tooltip("휘두르는 힘을 증폭시킬 배율. 20~50 사이에서 시작해보세요.")]
    public float castPowerMultiplier = 30f;

    // --- [핵심 변경] ---
    // PlayerFishingController가 이 값을 읽어갈 수 있도록 public으로 변경합니다.
    // { get; private set; }은 다른 스크립트에서 이 값을 읽을 수는 있지만, 바꿀 수는 없게 만듭니다.
    public Vector3 LastCastVelocity { get; private set; }

    private Quaternion _lastRotation;
    private float _lastAngularSpeed;
    private bool _isPreparingToCast = false;

    void Start()
    {
        // PlayerFishingController 참조가 더 이상 필요 없으므로 해당 코드를 삭제합니다.

        if (castAction != null && castAction.action != null)
        {
            castAction.action.Enable();
            castAction.action.started += OnCastStarted;
            castAction.action.canceled += OnCastCanceled;
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
        // 낚싯대가 활성화된 상태인지 확인하는 로직은 PlayerFishingController가 담당하게 됩니다.
        // 여기서는 일단 캐스팅 준비 상태로만 만듭니다.
        _isPreparingToCast = true;
    }

    private void OnCastCanceled(InputAction.CallbackContext context)
    {
        if (_isPreparingToCast)
        {
            _isPreparingToCast = false;

            Vector3 castDirection = transform.forward;
            Vector3 castVelocity = castDirection * _lastAngularSpeed * castPowerMultiplier;

            // --- [핵심 변경] ---
            // 함수를 호출하는 대신, 계산된 최종 속도 값을 공개 변수에 저장합니다.
            LastCastVelocity = castVelocity;

            Debug.Log($"[CastingHandler] Cast velocity calculated: {LastCastVelocity}");
        }
    }

    void Update()
    {
        if (_isPreparingToCast)
        {
            if (Time.deltaTime > 0)
            {
                Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(_lastRotation);
                deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

                float angularSpeed = angle / Time.deltaTime;

                _lastAngularSpeed = Mathf.Clamp(angularSpeed, 0, 300);
            }
        }
        _lastRotation = transform.rotation;
    }
}