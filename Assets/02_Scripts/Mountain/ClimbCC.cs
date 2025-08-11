using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

public class ClimbCC : MonoBehaviour
{
    [Header("Auto (leave empty)")]
    [SerializeField] private ClimbProvider climbProvider;
    [SerializeField] private CharacterController cc;
    [SerializeField] private Behaviour networkCharacterController; // Fusion NCC
    [SerializeField] private ContinuousMoveProviderBase[] moveProviders;

    [Header("Options")]
    [SerializeField] private float graceTime = 0.12f;     // 손 바꿔잡기 유예
    [SerializeField] private int fallKickFrames = 4;      // 떨어뜨리기 킥 프레임 수
    [SerializeField] private float extraDownSpeed = 2.0f; // 초기 하강 보정 속도
    [SerializeField] private bool logDebug = false;

    private int grabbingCount;
    private bool subscribed;

    void Awake()
    {
        if (!climbProvider) climbProvider = FindObjectOfType<ClimbProvider>(true);
        if (!cc) cc = GetComponentInParent<CharacterController>();
        if (!networkCharacterController)
            networkCharacterController = GetComponentInParent<Behaviour>(); // Fusion NCC를 수동 연결 권장

        if (moveProviders == null || moveProviders.Length == 0)
            moveProviders = GetComponentsInParent<ContinuousMoveProviderBase>(true);
    }

    void OnEnable() => Subscribe(true);
    void OnDisable() => Subscribe(false);

    void Subscribe(bool on)
    {
        if (!climbProvider || climbProvider.climbInteractors == null) return;

        if (on && !subscribed)
        {
            foreach (var it in climbProvider.climbInteractors)
            {
                it.selectEntered.AddListener(OnGrab);
                it.selectExited.AddListener(OnRelease);
            }
            subscribed = true;
        }
        else if (!on && subscribed)
        {
            foreach (var it in climbProvider.climbInteractors)
            {
                it.selectEntered.RemoveListener(OnGrab);
                it.selectExited.RemoveListener(OnRelease);
            }
            subscribed = false;
        }
    }

    void OnGrab(SelectEnterEventArgs _)
    {
        grabbingCount++;
        if (logDebug) Debug.Log($"[ClimbFall] Grab -> {grabbingCount}");
        // 클라임 시 이동/중력 담당 끄기
        SetMoveEnabled(false);
    }

    void OnRelease(SelectExitEventArgs _)
    {
        grabbingCount = Mathf.Max(0, grabbingCount - 1);
        if (logDebug) Debug.Log($"[ClimbFall] Release -> {grabbingCount}");
        if (grabbingCount == 0) StartCoroutine(CoFallAfterGrace());
    }

    IEnumerator CoFallAfterGrace()
    {
        // 빠른 그립 전환 유예
        yield return new WaitForSeconds(graceTime);
        if (grabbingCount > 0) yield break;

        // 이동/중력 다시 켜기
        SetMoveEnabled(true);

        // CharacterController가 벽/피처에 달라붙어 있으면 한두 프레임 아래로 '킥'해서 떨어뜨린다
        if (cc)
        {
            for (int i = 0; i < fallKickFrames; i++)
            {
                // 기본 중력 + 약간의 추가 하강
                Vector3 down = Physics.gravity * Time.deltaTime + Vector3.down * extraDownSpeed * Time.deltaTime;
                cc.Move(down);
                yield return null; // 한 프레임씩
            }
        }

        if (logDebug) Debug.Log("[ClimbFall] Both hands released -> Falling");
    }

    void SetMoveEnabled(bool enabled)
    {
        // XR 연속이동(있다면) 켜고/끄고 + 중력 사용
        if (moveProviders != null)
        {
            foreach (var mv in moveProviders)
            {
                if (!mv) continue;
                mv.enabled = enabled;
                mv.useGravity = enabled;
                // 즉시 낙하가 약하면 에디터에서 Gravity Application Mode를 'Immediately'로 설정
            }
        }

        // Fusion NetworkCharacterController(있다면) 켜기/끄기
        if (networkCharacterController)
            networkCharacterController.enabled = enabled;
    }
}