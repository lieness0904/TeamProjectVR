using System.Collections;
using System.Collections.Generic;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ClimbCtrl : MonoBehaviour
{
    [Header("Required (비우면 자동 탐색)")]
    [SerializeField] private XROrigin xrOrigin;           // NetworkPlayer 자식
    [SerializeField] private ClimbProvider climbProvider; // Locomotion System/Climb

    [Header("Optional (자동 탐색 가능)")]
    [SerializeField] private ContinuousMoveProviderBase[] moveProviders;
    [SerializeField] private Behaviour networkCharacterController; // Fusion NCC 등(없으면 비움)

    private CharacterController cc;
    private Vector3 prevRigPos;
    private bool climbing;
    private bool[] prevMoveEnabled;
    private bool prevNccEnabled;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!xrOrigin) xrOrigin = GetComponentInChildren<XROrigin>(true);
        if (!climbProvider) climbProvider = GetComponentInChildren<ClimbProvider>(true);

        if (moveProviders == null || moveProviders.Length == 0)
            moveProviders = GetComponentsInChildren<ContinuousMoveProviderBase>(true);

        if (!networkCharacterController)
        {
            networkCharacterController =
                GetComponent("NetworkCharacterController") as Behaviour ??
                GetComponent("NetworkCharacterControllerPrototype") as Behaviour;
        }
    }

    void OnEnable()
    {
        // 즉시 시도 + 한 프레임 뒤 재시도(스폰 타이밍 대비)
        TryBindAndSubscribe();
        if (climbProvider == null) StartCoroutine(BindNextFrame());
    }

    IEnumerator BindNextFrame()
    {
        yield return null; // 1프레임 대기
        TryBindAndSubscribe();
    }

    void TryBindAndSubscribe()
    {
        if (!xrOrigin) xrOrigin = GetComponentInChildren<XROrigin>(true);
        if (!climbProvider) climbProvider = GetComponentInChildren<ClimbProvider>(true);

        if (climbProvider != null)
        {
            // 중복 구독 방지 후 구독
            //climbProvider.beginLocomotion.RemoveListener(OnBeginClimb);
            //climbProvider.endLocomotion.RemoveListener(OnEndClimb);
            //climbProvider.beginLocomotion.AddListener(OnBeginClimb);
            //climbProvider.endLocomotion.AddListener(OnEndClimb);
            prevRigPos = xrOrigin ? xrOrigin.transform.position : Vector3.zero;

            Debug.Log($"[ClimbCtrl] ClimbProvider 바인딩 완료: {climbProvider.name}");
        }
        else
        {
            Debug.LogWarning("[ClimbCtrl] ClimbProvider를 찾지 못했습니다. 경로를 확인하세요. (Locomotion System/Climb)");
        }
    }

    void OnDisable()
    {
        if (climbProvider != null)
        {
            //climbProvider.beginLocomotion.RemoveListener(OnBeginClimb);
            //climbProvider.endLocomotion.RemoveListener(OnEndClimb);
        }
        if (climbing) OnEndClimb(); // 안전 복구
    }

    void LateUpdate()
    {
        if (!climbing || xrOrigin == null || cc == null) return;

        var rigPos = xrOrigin.transform.position;
        var delta = rigPos - prevRigPos;
        prevRigPos = rigPos;

        if (delta.sqrMagnitude > 0f)
        {
            // 루트 실제 이동(충돌/경사 포함)
            cc.Move(delta);
            // 리그 되돌림(이중 이동 방지)
            xrOrigin.transform.position -= delta;
        }
    }

    // ---- 이벤트 핸들러 ----
    void OnBeginClimb(LocomotionSystem _)
    {
        if (xrOrigin == null || cc == null) return;

        climbing = true;
        prevRigPos = xrOrigin.transform.position;

        if (moveProviders != null && moveProviders.Length > 0)
        {
            prevMoveEnabled = new bool[moveProviders.Length];
            for (int i = 0; i < moveProviders.Length; i++)
            {
                if (!moveProviders[i]) continue;
                prevMoveEnabled[i] = moveProviders[i].enabled;
                moveProviders[i].enabled = false;
            }
        }
        if (networkCharacterController)
        {
            prevNccEnabled = networkCharacterController.enabled;
            networkCharacterController.enabled = false;
        }

        Debug.Log("[ClimbCtrl] BeginClimb");
    }

    void OnEndClimb(LocomotionSystem _ = null)
    {
        climbing = false;

        if (moveProviders != null && prevMoveEnabled != null)
        {
            for (int i = 0; i < moveProviders.Length; i++)
                if (moveProviders[i]) moveProviders[i].enabled = prevMoveEnabled[i];
        }
        if (networkCharacterController)
            networkCharacterController.enabled = prevNccEnabled;

        Debug.Log("[ClimbCtrl] EndClimb");
    }
}