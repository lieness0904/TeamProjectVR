using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Unity.XR.CoreUtils;

public class T_PlayerVisual : NetworkBehaviour
{
    public GameObject character;
    public Animator animator;
    public GameObject xrOrigin;

    [Networked] public Vector3 syncedPosition { get; set; }
    [Networked] public Quaternion syncedRotation { get; set; }
    [Networked] private Vector2 blendParam { get; set; }

    private Transform head; // XR Origin HMD

    private Vector3 lastHeadPosition;



    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            // XR Origin이 하위 오브젝트에 이미 존재한다고 가정
            head = xrOrigin.transform.Find("Camera Offset/Main Camera");
            if (head == null)
            {
                Debug.LogError("Main Camera(HMD) not found in XR Origin!");
            }
            lastHeadPosition = head.position;
        }
        else
        {
            // 프록시에서는 XR Origin을 사용하지 않음
            xrOrigin.SetActive(false); // 남의 XR Origin은 꺼버림 (필수!)
        }
        character.SetActive(true);
        Debug.Log($"[Player.Spawned] Object: {this.name}, HasInputAuthority: {Object.HasInputAuthority}, InputAuthority: {Object.InputAuthority}, IsProxy: {Object.IsProxy}");
    }

    public override void FixedUpdateNetwork()
    {
        if (Object.HasInputAuthority && head != null)
        {
            // XR Origin 기준으로 외형 좌표/회전 동기화 값에 기록 (네트워크 전파)
            syncedPosition = new Vector3(head.position.x, character.transform.position.y, head.position.z);
            syncedRotation = Quaternion.Euler(0, head.rotation.eulerAngles.y, 0);

            // 외형도 즉시 내 XR Origin 따라감
            character.transform.position = syncedPosition;
            character.transform.rotation = syncedRotation;

            // 애니메이션 계산도 여기서
            Vector3 velocity = (head.position - lastHeadPosition) / Time.deltaTime;
            Vector3 localVel = character.transform.InverseTransformDirection(velocity);
            blendParam = new Vector2(localVel.x, localVel.z);

            lastHeadPosition = head.position;
        }
        else
        {
            // 남들은 네트워크 동기화 값만 따라감
            character.transform.position = syncedPosition;
            character.transform.rotation = syncedRotation;
        }

        // 애니메이션 동기화
        animator.SetFloat("MoveX", blendParam.x);
        animator.SetFloat("MoveY", blendParam.y);
    }
}
