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
        // ※ 주의: 이 방식은 씬에 XR Origin이 하나만 있고, 항상 활성화 상태일 때만 안정적으로 동작합니다.
        localXROrigin = FindAnyObjectByType<XROrigin>();
        if (localXROrigin == null)
        {
            Debug.LogWarning("T_PlayerVisual: 씬에서 XROrigin을 찾을 수 없습니다.", this);
        }

        // 이 오브젝트의 제어 권한을 내가 가지고 있는지(로컬 플레이어인지) 확인합니다.
        if (Object.HasInputAuthority)
        {
            // "이것은 내 캐릭터입니다."

            // 1. XR Origin을 활성화하여 1인칭 시점으로 세상을 봅니다.
            if (localXROrigin != null)
            {
                localXROrigin.gameObject.SetActive(true);
            }

            // 2. 내 아바타는 내 시야를 가리므로 비활성화합니다.
            if (characterVisual != null)
            {
                characterVisual.SetActive(false);
            }

            // 3. 속도 계산을 위한 초기 위치를 기록합니다.
            _lastPosition = (localXROrigin != null && localXROrigin.Camera != null) ? localXROrigin.Camera.transform.position : transform.position;
        }
        else
        {
            // "이것은 다른 사람의 캐릭터입니다."

            // 1. 다른 사람의 아바타를 봐야 하므로 활성화합니다.
            if (characterVisual != null)
            {
                characterVisual.SetActive(true);
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        // --- 물리 업데이트 주기마다 호출되는 Fusion의 핵심 업데이트 함수입니다. ---
        // 입력 처리, 상태 동기화 등 중요한 로직이 여기서 처리됩니다.

        // 이 오브젝트의 제어 권한을 가진 클라이언트에서만 아래 로직을 실행합니다.
        // 즉, 내 캐릭터만 내 XR 장비의 움직임을 따라가도록 합니다.
        if (Object.HasInputAuthority)
        {
            if (localXROrigin != null && localXROrigin.Camera != null)
            {
                // NetworkObject(Player 프리팹의 루트)의 위치를 내 머리(HMD) 위치의 X, Z값에 맞춰줍니다.
                // Y값은 그대로 두어, 공중에 뜨거나 땅에 파고드는 것을 방지합니다.
                transform.position = new Vector3(
                    localXROrigin.Camera.transform.position.x,
                    transform.position.y,
                    localXROrigin.Camera.transform.position.z
                );

                // NetworkObject의 Y축 회전 값을 내 머리(HMD)의 Y축 회전에 맞춰줍니다.
                transform.rotation = Quaternion.Euler(0, localXROrigin.Camera.transform.rotation.eulerAngles.y, 0);
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
                // 현재 머리 위치를 기준으로 속도를 계산합니다.
                Vector3 currentPosition = localXROrigin.Camera.transform.position;
                Vector3 velocity = (currentPosition - _lastPosition) / Time.deltaTime;

                // 월드 좌표계 기준 속도를 캐릭터의 로컬 좌표계 기준으로 변환합니다. (앞/뒤/좌/우 움직임)
                Vector3 localVelocity = transform.InverseTransformDirection(velocity);

                // 계산된 값을 [Networked] 속성인 _blendParam에 저장합니다.
                // 이 값은 자동으로 다른 클라이언트들에게 동기화됩니다.
                _blendParam = new Vector2(localVelocity.x, localVelocity.z);

                // 다음 계산을 위해 현재 위치를 저장합니다.
                _lastPosition = currentPosition;
            }
        }

        // 모든 클라이언트(나 자신 포함)에서 동기화된 _blendParam 값을 애니메이터에 적용합니다.
        // 이를 통해 다른 사람의 아바타가 움직이는 것처럼 보입니다.
        if (animator != null)
        {
            animator.SetFloat("MoveX", _blendParam.x);
            animator.SetFloat("MoveY", _blendParam.y);
        }
    }
}