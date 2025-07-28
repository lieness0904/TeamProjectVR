using UnityEngine;
using System.Collections.Generic;
using Fusion; // PlayerFishingController를 찾기 위해 추가

public class RodBendingController : MonoBehaviour
{
    [Header("뼈대 설정")]
    [Tooltip("낚싯대의 뼈대(Bone)들을 순서대로(루트->끝) 넣어주세요.")]
    public List<Transform> bones;

    [Header("휘어짐 설정")]
    [Tooltip("휘어지는 강도를 조절합니다.")]
    public float bendMultiplier = 1.0f;

    [Tooltip("휘어졌다가 펴지는 속도를 조절합니다.")]
    public float stiffness = 10.0f;

    // --- private 변수들 ---
    private PlayerFishingController _playerFishingController;
    private Transform _target; // 찌(Bobber)의 Transform
    private List<Quaternion> _initialBoneRotations; // 뼈들의 초기 회전값

    void Start()
    {
        // 이 스크립트가 붙어있는 오브젝트의 부모들 중에서 PlayerFishingController를 찾습니다.
        // 이 로직이 작동하려면, 이 스크립트가 붙은 Rod_Bones 오브젝트가 Player의 자식으로 있어야 합니다.
        _playerFishingController = GetComponentInParent<PlayerFishingController>();
        if (_playerFishingController == null)
        {
            Debug.LogError("RodBendingController가 PlayerFishingController를 찾지 못했습니다!", this.gameObject);
            this.enabled = false;
            return;
        }

        // 낚싯대가 펴져있을 때의 초기 회전값을 저장해둡니다.
        _initialBoneRotations = new List<Quaternion>();
        foreach (var bone in bones)
        {
            _initialBoneRotations.Add(bone.localRotation);
        }
    }

    // 물리 계산, 캐릭터 이동 등이 모두 끝난 후에 호출되어 시각적인 떨림을 방지합니다.
    void LateUpdate()
    {
        // PlayerFishingController에서 현재 찌(Bobber) 정보를 가져옵니다.
        NetworkObject currentBobber = _playerFishingController.CurrentBobber;
        _target = (currentBobber != null) ? currentBobber.transform : null;

        // 뼈대를 업데이트합니다.
        UpdateBones();
    }

    void UpdateBones()
    {
        if (bones == null || bones.Count == 0) return;

        // 찌가 있으면 그쪽으로 휘고, 없으면 원래대로 펴집니다.
        if (_target != null)
        {
            // 찌가 있을 때 (휘어지는 로직)
            for (int i = 0; i < bones.Count; i++)
            {
                Transform bone = bones[i];
                // 뼈에서 찌를 향하는 방향을 계산합니다.
                Vector3 targetDirection = _target.position - bone.position;

                // 뼈의 원래 Up 방향(Y축)이 타겟 방향을 바라보도록 목표 회전값을 계산합니다.
                // 낚싯대의 기본 축이 다를 경우 Vector3.up을 Vector3.forward 등으로 바꿔야 할 수 있습니다.
                Quaternion targetRotation = Quaternion.FromToRotation(transform.up, targetDirection);

                // Slerp를 사용해 부드럽게 회전시킵니다.
                bone.localRotation = Quaternion.Slerp(
                    bone.localRotation,
                    _initialBoneRotations[i] * targetRotation, // 초기 회전값에 목표 회전값을 곱해줍니다.
                    Time.deltaTime * stiffness * bendMultiplier
                );
            }
        }
        else
        {
            // 찌가 없을 때 (원래대로 펴지는 로직)
            for (int i = 0; i < bones.Count; i++)
            {
                // 저장해둔 초기 회전값으로 부드럽게 되돌립니다.
                bones[i].localRotation = Quaternion.Slerp(bones[i].localRotation, _initialBoneRotations[i], Time.deltaTime * stiffness);
            }
        }
    }
}