using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Unity.XR.CoreUtils;

public class T_PlayerVisual : NetworkBehaviour
{
    public GameObject xrOrigin;
    public GameObject character;
    public Animator animator;

    private Vector3 lastPosition;

    [Networked] private Vector2 blendParam { get; set; }
    [Networked] private Quaternion charRotation { get; set; }

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            xrOrigin.SetActive(true);
            character.SetActive(true);
        }
        else
        {
            xrOrigin.SetActive(false);
            character.SetActive(true);
        }

        // 초기 위치 기록
        var head = xrOrigin.GetComponent<XROrigin>().Camera.transform;
        lastPosition = head.position;
    }
    private void Update()
    {
        var head = xrOrigin.GetComponent<XROrigin>().Camera.transform;
        if (Object.HasInputAuthority)
        {
            // 외형 위치를 XR Origin의 head 기준으로 이동
            character.transform.position = new Vector3(
                head.position.x,
                character.transform.position.y,
                head.position.z
            );

            // 회전 동기화
            Quaternion rotation = Quaternion.Euler(0, head.rotation.eulerAngles.y, 0);
            charRotation = rotation;

            // 속도 계산도 head 기준 
            Vector3 velocity = (head.position - lastPosition) / Time.deltaTime;
            Vector3 localVelocity = character.transform.InverseTransformDirection(velocity);
            blendParam = new Vector2(localVelocity.x, localVelocity.z);

            lastPosition = head.position;
        }

        // 회전 적용
        character.transform.rotation = charRotation;

        // 3. 애니메이션 파라미터는 모두 적용 (본인 + 타인)
        animator.SetFloat("MoveX", blendParam.x);
        animator.SetFloat("MoveY", blendParam.y);
    }
}
