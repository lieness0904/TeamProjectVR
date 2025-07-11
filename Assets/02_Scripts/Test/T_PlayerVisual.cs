using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Fusion;
using Unity.XR.CoreUtils;

public class T_PlayerVisual : NetworkBehaviour
{
    public GameObject character;
    public Animator animator;

    [Networked] public Vector3 syncedPosition { get; set; }
    [Networked] public Quaternion syncedRotation { get; set; }
    [Networked] private Vector2 blendParam { get; set; }

    private Transform head; // XR Origin HMD

    private Vector3 lastHeadPosition;

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            var rig = GetComponentInChildren<RiggingManager>();
            StartCoroutine(WaitForRigHMD(rig));
        }
        character.SetActive(true);
    }

    private IEnumerator WaitForRigHMD(RiggingManager rig)
    {
        if (rig == null)
        {
            Debug.LogError("RiggingManager is null!");
            yield break;
        }
        while (rig.hmd == null)
        {
            yield return null;
        }
        head = rig.hmd;

        lastHeadPosition = head.position;
    }

    private void Update()
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
