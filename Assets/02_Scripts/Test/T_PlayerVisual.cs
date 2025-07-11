using UnityEngine;
using Fusion;
using Unity.XR.CoreUtils;
using System.Linq;

public class T_PlayerVisual : NetworkBehaviour
{
    // [Header("로컬 플레이어 XR Rig 참조")]
    // 씬에 단 하나 존재하는 XR Origin을 담을 변수
    private XROrigin localXROrigin;

    [Header("아바타 관련")]
    // 네트워크로 동기화될 플레이어의 시각적 아바타
    public GameObject characterVisual;
    // 아바타의 애니메이터
    public Animator animator;

    // [Header("네트워크 동기화 속성")]
    // 애니메이션 동기화를 위한 이동 값 (X, Y)
    [Networked] private Vector2 _blendParam { get; set; }

    // 마지막 위치 기록 (속도 계산용)
    private Vector3 _lastPosition;


    public override void Spawned()
    {
        // --- 이 NetworkObject가 스폰되었을 때 호출됩니다. ---

        // 씬에 있는 XR Origin을 찾습니다.
        localXROrigin = FindAnyObjectByType<XROrigin>();
        if (localXROrigin == null)
        {
            Debug.LogWarning("T_PlayerVisual: 씬에서 XROrigin을 찾을 수 없습니다.", this);
        }

        // 이 오브젝트의 제어 권한을 내가 가지고 있는지(로컬 플레이어인지) 확인합니다.
        if (Object.HasInputAuthority)
        {
            // "이것은 내 캐릭터입니다."
            if (localXROrigin != null)
            {
                localXROrigin.gameObject.SetActive(true);
            }

            if (characterVisual != null)
            {
                characterVisual.SetActive(false);
            }

            _lastPosition = (localXROrigin != null && localXROrigin.Camera != null) ? localXROrigin.Camera.transform.position : transform.position;
        }
        else
        {
            // "이것은 다른 사람의 캐릭터입니다."
            if (characterVisual != null)
            {
                characterVisual.SetActive(true);
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        // --- 물리 업데이트 주기마다 호출되는 Fusion의 핵심 업데이트 함수입니다. ---

        if (Object.HasInputAuthority)
        {
            if (localXROrigin != null && localXROrigin.Camera != null)
            {
                // 1. 부모(Player) 오브젝트를 HMD의 월드 위치와 Y축 회전에 맞춰 이동시킵니다.
                transform.position = new Vector3(
                    localXROrigin.Camera.transform.position.x,
                    transform.position.y,
                    localXROrigin.Camera.transform.position.z
                );

                transform.rotation = Quaternion.Euler(0, localXROrigin.Camera.transform.rotation.eulerAngles.y, 0);

                // 2. (핵심 수정!) 자식(XR Origin)의 로컬 위치를 0으로 리셋합니다.
                // 이 코드가 부모의 움직임이 자식에게 누적되는 것을 막아 피드백 루프를 끊어줍니다.
                localXROrigin.transform.localPosition = Vector3.zero;
            }
        }
    }

    public override void Render()
    {
        // --- 화면이 렌더링되기 직전 매 프레임 호출됩니다. 애니메이션처럼 시각적인 처리에 적합합니다. ---

        // 내 캐릭터인 경우에만 속도를 계산하고 애니메이션 파라미터를 네트워크로 보냅니다.
        if (Object.HasInputAuthority)
        {
            if (localXROrigin != null && localXROrigin.Camera != null)
            {
                Vector3 currentPosition = localXROrigin.Camera.transform.position;
                Vector3 velocity = (currentPosition - _lastPosition) / Time.deltaTime;
                Vector3 localVelocity = transform.InverseTransformDirection(velocity);
                _blendParam = new Vector2(localVelocity.x, localVelocity.z);
                _lastPosition = currentPosition;
            }
        }

        // 모든 클라이언트(나 자신 포함)에서 동기화된 _blendParam 값을 애니메이터에 적용합니다.
        if (animator != null)
        {
            animator.SetFloat("MoveX", _blendParam.x);
            animator.SetFloat("MoveY", _blendParam.y);
        }
    }
}