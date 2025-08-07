using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class PlayerClimbingController : MonoBehaviour
{
    private ClimbProvider climbProvider;

    [SerializeField] private Transform xrOrigin;         // XR Origin (ClimbProvider가 움직이는 대상)
    [SerializeField] private Transform networkPlayer;    // NetworkPlayer 루트
    private Vector3 lastOriginPosition;

    void Start()
    {
        climbProvider = GetComponent<ClimbProvider>();

        if (climbProvider == null)
        {
            Debug.LogError("[PlayerClimbingController] ClimbProvider가 없습니다.");
            return;
        }

        // AssignClimbProviderToAllHandles();

        if (xrOrigin != null)
        {
            lastOriginPosition = xrOrigin.position;
        }
    }

   // void AssignClimbProviderToAllHandles()
   // {
   //     ClimbInteractable[] handles = FindObjectsOfType<ClimbInteractable>();
   //
   //     foreach (var handle in handles)
   //     {
   //         handle.climbProvider = climbProvider;
   //         Debug.Log($"[PlayerClimbingController] 핸들 '{handle.name}'에 ClimbProvider 할당 완료");
   //     }
   // }
   //
   // void LateUpdate()
   // {
   //     if (xrOrigin == null || networkPlayer == null)
   //         return;
   //
   //     if (climbProvider.locomotionPhase == LocomotionPhase.Started ||
   //         climbProvider.locomotionPhase == LocomotionPhase.Moving)
   //     {
   //         // XR Origin이 이동한 벡터를 구한다
   //         Vector3 delta = xrOrigin.position - lastOriginPosition;
   //
   //         if (delta.sqrMagnitude > 0.0001f)
   //         {
   //             // NetworkPlayer를 이동시키고
   //             networkPlayer.position += delta;
   //
   //             // XR Origin은 원래 로컬 위치로 되돌린다
   //             xrOrigin.position -= delta;
   //
   //             Debug.Log($"[PlayerClimbingController] NetworkPlayer 이동: {delta}, 현재 위치: {networkPlayer.position}");
   //         }
   //     }
   //
   //     lastOriginPosition = xrOrigin.position;
   // }
}