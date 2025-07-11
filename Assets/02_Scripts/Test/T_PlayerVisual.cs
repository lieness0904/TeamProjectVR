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
        // 필수: 인스펙터에 꼭 할당돼있어야 함!
        if (xrOrigin == null)
        {
            xrOrigin = transform.Find("XR Origin (Action-based)")?.gameObject;
            if (xrOrigin == null)
                Debug.LogError("XR Origin (Action-based) 오브젝트가 Player 하위에 없습니다!");
        }
        if (character == null)
        {
            character = transform.Find("MaleCharacter")?.gameObject;
            if (character == null)
                Debug.LogError("MaleCharacter 오브젝트가 Player 하위에 없습니다!");
        }
        if (animator == null)
        {
            animator = character.GetComponentInChildren<Animator>();
            if (animator == null)
                Debug.LogError("Animator가 MaleCharacter(자식)에 없습니다!");
        }

        // HMD(Main Camera) 참조
        head = xrOrigin.transform.Find("Camera Offset/Main Camera");
        if (head == null)
            Debug.LogError("Camera Offset/Main Camera(HMD)를 XR Origin 하위에서 못 찾음!");

        if (Object.HasInputAuthority)
        {
            xrOrigin.SetActive(true); // 내 XR Origin만 켜기
            lastHeadPosition = head.position;
        }
        else
        {
            xrOrigin.SetActive(false); // 남의 XR Origin은 반드시 꺼버려야 함
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
