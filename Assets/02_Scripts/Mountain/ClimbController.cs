using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ClimbController : MonoBehaviour
{
    private ClimbProvider climbProvider;
    public CharacterController characterController;

    private LocomotionPhase lastPhase = LocomotionPhase.Idle;

    private void Start()
    {
        var components = GetComponents<MonoBehaviour>();
        foreach (var c in components)
        {
            if (c.GetType().FullName == "UnityEngine.XR.Interaction.Toolkit.ClimbProvider")
            {
                climbProvider = c as ClimbProvider;
                Debug.Log("[ClimbController] ClimbProvider 강제 캐스팅 성공!");
            }
        }
    }

    void Update()
    {
        if (climbProvider == null || characterController == null)
            return;

        bool isClimbing = climbProvider.locomotionPhase == LocomotionPhase.Started ||
                          climbProvider.locomotionPhase == LocomotionPhase.Moving;

        if (isClimbing && characterController.enabled)
        {
            characterController.enabled = false;
            //Debug.Log("[ClimbController] 클라이밍 시작 - CharacterController 비활성화");
        }
        else if (!isClimbing && !characterController.enabled)
        {
            characterController.enabled = true;
            //Debug.Log("[ClimbController] 클라이밍 종료 - CharacterController 활성화");
        }

        // 상태 변화 추적
        if (climbProvider.locomotionPhase != lastPhase)
        {
            Debug.Log($"[ClimbController] 상태 변화: {lastPhase} → {climbProvider.locomotionPhase}");
            lastPhase = climbProvider.locomotionPhase;
        }
    }
}