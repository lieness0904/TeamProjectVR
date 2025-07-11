using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Unity.XR.CoreUtils;

public class T_PlayerVisual : NetworkBehaviour
{
    public GameObject character;
    public Animator animator;

    private Vector3 lastPosition;
    private Transform head;

    [Networked] private Vector2 blendParam { get; set; }
    [Networked] private Quaternion charRotation { get; set; }

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            var rig = GetComponent<RiggingManager>();
            StartCoroutine(WaitForRigHMD(rig));
        }

        character.SetActive(true);

        lastPosition = head != null ? head.position : transform.position;
    }
    private IEnumerator WaitForRigHMD(RiggingManager rig)
    {
        while (rig.hmd == null) yield return null;  // 🔁 RiggingManager가 생성될 때까지 대기

        head = rig.hmd;
        lastPosition = head.position;
    }

    private void Update()
    {
        if (!Object.HasInputAuthority || head == null)
        {
            // 회전 적용은 모든 플레이어가 받아야 하므로 계속 실행
            character.transform.rotation = charRotation;

            // 애니메이션 파라미터도 모두에게 적용
            animator.SetFloat("MoveX", blendParam.x);
            animator.SetFloat("MoveY", blendParam.y);
            return;
        }

        // 외형 위치를 XR Origin의 head 기준으로 이동
        character.transform.position = new Vector3(
            head.position.x,
            character.transform.position.y,
            head.position.z
        );

        // 회전 동기화
        Quaternion rotation = Quaternion.Euler(0, head.rotation.eulerAngles.y, 0);
        charRotation = rotation;

        // 속도 계산
        Vector3 velocity = (head.position - lastPosition) / Time.deltaTime;
        Vector3 localVelocity = character.transform.InverseTransformDirection(velocity);
        blendParam = new Vector2(localVelocity.x, localVelocity.z);

        lastPosition = head.position;

        // 회전 적용 (모든 플레이어에 적용됨)
        character.transform.rotation = charRotation;

        // 애니메이션 파라미터 적용 (모든 플레이어에 적용됨)
        animator.SetFloat("MoveX", blendParam.x);
        animator.SetFloat("MoveY", blendParam.y);
    }
}
