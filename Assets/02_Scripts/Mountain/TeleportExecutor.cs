using System.Collections;
using System.Reflection;
using UnityEngine;
using Fusion;

public class TeleportExecutor : NetworkBehaviour
{
    [Header("Optional cached components (비워두면 자동 탐색)")]
    [SerializeField] private CharacterController cc;
    [SerializeField] private Rigidbody rb;

    // NCC/NT는 버전마다 타입명이 달라질 수 있어 Component로 받아 리플렉션으로 Teleport 호출
    private Component ncc; // NetworkCharacterController 또는 Prototype
    private Component nt;  // NetworkTransform 또는 파생

    private void Awake()
    {
        if (!cc) cc = GetComponent<CharacterController>();
        if (!rb) rb = GetComponent<Rigidbody>();

        // NCC(둘 중 하나가 있을 수 있음)
        ncc = GetComponent("NetworkCharacterController")
           ?? GetComponent("NetworkCharacterControllerPrototype");

        // NetworkTransform(프로젝트/버전에 따라 이름 다름)
        nt = GetComponent("NetworkTransform")
           ?? GetComponent("NetworkTransformReliable");
    }

    /// <summary>
    /// 버튼 쪽에서 호출(로컬 → 서버)
    /// </summary>
    public void RequestTeleport(Vector3 pos, Quaternion rot)
    {
        RPC_RequestTeleport(pos, rot);
    }

    /// <summary>
    /// 입력 권한 → 상태 권한 : 서버/호스트에서 최종 텔레포트 수행
    /// </summary>
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_RequestTeleport(Vector3 pos, Quaternion rot)
    {
        Runner.StartCoroutine(DoTeleport(pos, rot)); // 권한 측 실제 이동
        RPC_ConfirmTeleport(pos, rot);               // 입력 권한에도 즉시 반영(체감 지연 최소화)
    }

    /// <summary>
    /// 상태 권한 → 입력 권한 : 시각 동기용
    /// </summary>
    [Rpc(RpcSources.StateAuthority, RpcTargets.InputAuthority)]
    private void RPC_ConfirmTeleport(Vector3 pos, Quaternion rot)
    {
        // 이 객체가 상태 권한이 아닌(=클라) 경우에만 로컬 스냅 실시
        if (!Object.HasStateAuthority)
            Runner.StartCoroutine(DoTeleport(pos, rot));
    }

    /// <summary>
    /// 내장 Teleport API가 있으면 우선 사용, 없으면 안전 스냅
    /// </summary>
    private IEnumerator DoTeleport(Vector3 pos, Quaternion rot)
    {
        // 1) Fusion 내장 API 시도(NCC → NT 순서)
        if (TryCallBuiltInTeleport(ncc, pos, rot) || TryCallBuiltInTeleport(nt, pos, rot))
            yield break;

        // 2) 내장 API가 없으면: 한 틱 동안 이동/보간 계층 잠시 중지 후 스냅
        var nccB = ncc as UnityEngine.Behaviour;
        var ntB = nt as UnityEngine.Behaviour;

        bool hadRb = rb != null;
        bool prevKinematic = false;

        if (nccB) nccB.enabled = false;
        if (ntB) ntB.enabled = false;

        if (hadRb)
        {
            prevKinematic = rb.isKinematic;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.isKinematic = true;
        }

        if (cc) cc.enabled = false;

        transform.SetPositionAndRotation(pos, rot);

        // 보간/물리 안정화를 위해 한 틱 대기
        yield return new WaitForFixedUpdate();

        if (cc) cc.enabled = true;
        if (hadRb) rb.isKinematic = prevKinematic;
        if (ntB) ntB.enabled = true;
        if (nccB) nccB.enabled = true;
    }

    /// <summary>
    /// 주어진 컴포넌트에 Teleport/TeleportTo/TeleportToPosition 메서드가 있으면 호출
    /// </summary>
    private bool TryCallBuiltInTeleport(Component comp, Vector3 pos, Quaternion rot)
    {
        if (!comp) return false;

        var t = comp.GetType();
        MethodInfo m =
            t.GetMethod("Teleport", new[] { typeof(Vector3), typeof(Quaternion) }) ??
            t.GetMethod("Teleport", new[] { typeof(Vector3) }) ??
            t.GetMethod("TeleportTo", new[] { typeof(Vector3), typeof(Quaternion) }) ??
            t.GetMethod("TeleportTo", new[] { typeof(Vector3) }) ??
            t.GetMethod("TeleportToPosition", new[] { typeof(Vector3), typeof(Quaternion) }) ??
            t.GetMethod("TeleportToPosition", new[] { typeof(Vector3) });

        if (m == null) return false;

        var ps = m.GetParameters();
        if (ps.Length == 2) m.Invoke(comp, new object[] { pos, rot });
        else m.Invoke(comp, new object[] { pos });
        return true;
    }
}