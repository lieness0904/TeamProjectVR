using UnityEngine;
using UnityEngine.InputSystem;

public class CastingController : MonoBehaviour
{
    [Header("입력 액션")]
    public InputActionReference castAction;

    [Header("캐스팅 설정")]
    public float castPowerMultiplier = 1f; // 회전 값을 사용하므로, 힘의 단위를 완전히 바꿔야 합니다. 50 정도로 시작해 보세요.

    // 마지막 프레임의 회전 값과 각속도를 저장할 변수
    private Quaternion lastRotation;
    private float lastAngularSpeed;

    private void OnEnable()
    {
        castAction.action.Enable();
        if (castAction.action.actionMap != null)
        {
            castAction.action.actionMap.Enable();
        }
        castAction.action.started += OnCastStarted;
        castAction.action.canceled += OnCastCanceled;
    }

    private void OnDisable()
    {
        if (castAction.action.actionMap != null)
        {
            castAction.action.actionMap.Disable();
        }
        castAction.action.started -= OnCastStarted;
        castAction.action.canceled -= OnCastCanceled;
    }

    // 버튼을 누르는 순간 호출
    private void OnCastStarted(InputAction.CallbackContext context)
    {
        if (FishingManager.instance.fishingRodObject.activeInHierarchy)
        {
            Debug.Log("캐스팅 준비! 손목 스냅으로 던지세요.");
            // 현재 회전 값을 초기 회전 값으로 기록
            lastRotation = transform.rotation;
        }
    }

    // 버튼을 떼는 순간 호출
    private void OnCastCanceled(InputAction.CallbackContext context)
    {
        if (FishingManager.instance.fishingRodObject.activeInHierarchy)
        {
            // 낚싯대 끝의 정면 방향을 가져옴
            Vector3 castDirection = FishingManager.instance.rodTip.forward;
            // 마지막 순간의 회전 속력에 비례하여 힘을 계산
            Vector3 castVelocity = castDirection * lastAngularSpeed * castPowerMultiplier;

            Debug.Log($"캐스팅! 마지막 순간 각속도: {lastAngularSpeed}, 적용된 힘: {castVelocity}");

            // FishingManager에게 캐스팅 명령
            FishingManager.instance.CastBobber(castVelocity);
        }
    }

    // 매 프레임마다 호출되어 마지막 순간의 회전 속도를 계속 갱신합니다.
    void Update()
    {
        // 매 프레임마다 호출되어 마지막 순간의 회전 속도를 계속 갱신합니다.
        if (Time.deltaTime > 0)
        {
            Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(lastRotation);
            deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);

            float angularSpeed = angle / Time.deltaTime;

            // --- 이 부분을 수정합니다 ---
            // 최대 속도를 300에서 30 정도로 대폭 줄여서 힘을 제어합니다.
            lastAngularSpeed = Mathf.Clamp(angularSpeed, 0, 20);

            lastRotation = transform.rotation;
        }
    }
}