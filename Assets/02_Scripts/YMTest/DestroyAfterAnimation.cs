using UnityEngine;

// 이 스크립트는 Animator가 있는 게임 오브젝트에 추가되어야 합니다.
[RequireComponent(typeof(Animator))]
public class DestroyAfterAnimation : MonoBehaviour
{
    void Start()
    {
        // 이 오브젝트에 붙어있는 Animator 컴포넌트를 가져옵니다.
        Animator animator = GetComponent<Animator>();

        // 현재 Animator의 첫 번째 레이어에 있는 상태 정보(애니메이션 클립 등)를 가져옵니다.
        AnimatorStateInfo stateInfo = animator.GetCurrentAnimatorStateInfo(0);

        // 애니메이션 클립의 길이를 가져옵니다.
        float animationLength = stateInfo.length;

        // 애니메이션 길이만큼의 시간이 지난 후에 이 게임 오브젝트를 파괴하도록 예약합니다.
        Destroy(gameObject, animationLength);
    }
}