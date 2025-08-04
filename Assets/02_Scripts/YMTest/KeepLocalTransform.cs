using UnityEngine;

/// <summary>
/// 이 스크립트는 지정된 타겟의 로컬 Transform(위치, 회전, 크기)을
/// 게임 시작 시의 값으로 매 프레임 고정시킵니다.
/// 부모 오브젝트의 움직임이나 회전에 자식이 영향을 받지 않게 하고 싶을 때 사용합니다.
/// </summary>
public class KeepLocalTransform : MonoBehaviour
{
    [Header("고정할 대상")]
    [Tooltip("로컬 Transform을 고정시킬 자식 오브젝트를 지정하세요.")]
    public Transform targetToKeep;

    // 초기 로컬 Transform 값을 저장할 변수들
    private Vector3 initialLocalPosition;
    private Quaternion initialLocalRotation;
    private Vector3 initialLocalScale;

    // 게임이 시작될 때 한 번 호출됩니다.
    void Awake()
    {
        // 타겟이 지정되었는지 확인합니다.
        if (targetToKeep == null)
        {
            Debug.LogError("고정할 타겟(TargetToKeep)이 지정되지 않았습니다!", this.gameObject);
            // 타겟이 없으면 스크립트를 비활성화하여 에러를 방지합니다.
            this.enabled = false;
            return;
        }

        // 타겟의 초기 로컬 Transform 값을 저장합니다.
        initialLocalPosition = targetToKeep.localPosition;
        initialLocalRotation = targetToKeep.localRotation;
        initialLocalScale = targetToKeep.localScale;
    }

    // 모든 Update 함수가 실행된 후에 매 프레임 호출됩니다.
    // 부모의 움직임이 적용된 후 값을 덮어쓰기 위해 LateUpdate를 사용합니다.
    void LateUpdate()
    {
        // 타겟이 없다면 아무것도 하지 않습니다.
        if (targetToKeep == null) return;

        // 매 프레임, 저장해둔 초기 로컬 값으로 강제 복원합니다.
        targetToKeep.localPosition = initialLocalPosition;
        targetToKeep.localRotation = initialLocalRotation;
        targetToKeep.localScale = initialLocalScale;
    }
}