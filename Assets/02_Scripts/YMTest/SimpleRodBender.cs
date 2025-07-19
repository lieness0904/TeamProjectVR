using UnityEngine;
using Fusion;

public class SimpleRodBender : MonoBehaviour
{
    [Header("연결 대상")]
    [Tooltip("낚싯대처럼 보이게 할 Line Renderer")]
    public LineRenderer rodRenderer;

    [Tooltip("낚싯대 휘어짐이 시작되는 지점 (손잡이 끝)")]
    public Transform rodBase;

    [Tooltip("낚싯대 끝. 이 오브젝트가 찌를 따라 움직입니다.")]
    public Transform rodTip;

    [Header("휘어짐 설정")]
    [Tooltip("라인을 얼마나 부드럽게 만들지 결정합니다. (10~30 추천)")]
    public int segments = 20;

    [Tooltip("휘어지는 정도를 조절합니다. 클수록 많이 휩니다. (0.1 ~ 2.0 추천)")]
    public float bendiness = 0.5f;

    [Tooltip("낚싯대 끝이 찌를 따라가는 속도입니다.")]
    public float stiffness = 10f;

    // --- Private 변수 ---
    private PlayerFishingController _playerFishingController;
    private float _rodLength; // 낚싯대의 초기 길이

    void Start()
    {
        // 이 스크립트가 정상 작동하려면, 부모 오브젝트 중에 PlayerFishingController가 있어야 합니다.
        _playerFishingController = GetComponentInParent<PlayerFishingController>();
        if (_playerFishingController == null)
        {
            Debug.LogError("SimpleRodBender가 PlayerFishingController를 찾지 못했습니다!", this.gameObject);
            this.enabled = false;
            return;
        }

        // Line Renderer의 점 개수를 설정합니다.
        rodRenderer.positionCount = segments;
        // 낚싯대의 원래 길이를 저장합니다.
        _rodLength = Vector3.Distance(rodBase.position, rodTip.position);
    }

    void LateUpdate()
    {
        // 1. 찌(타겟)의 위치를 가져옵니다.
        Transform target = _playerFishingController.CurrentBobber?.transform;

        Vector3 targetPosition;
        if (target != null)
        {
            // 찌가 있으면 그 위치가 타겟입니다.
            targetPosition = target.position;
        }
        else
        {
            // 찌가 없으면 낚싯대가 원래 방향으로 곧게 펴진 상태의 끝점이 타겟입니다.
            targetPosition = rodBase.position + transform.forward * _rodLength;
        }

        // 2. 낚싯대 끝(rodTip)이 타겟 위치를 부드럽게 따라가도록 위치를 업데이트합니다.
        rodTip.position = Vector3.Lerp(rodTip.position, targetPosition, Time.deltaTime * stiffness);

        // 3. 베지어 커브를 사용해 Line Renderer의 점들을 계산하고 그려줍니다.
        Vector3 controlPoint = (rodBase.position + rodTip.position) / 2f;
        // 낚싯대가 중력 방향(아래)으로 자연스럽게 휘도록 컨트롤 포인트를 살짝 내려줍니다.
        controlPoint -= transform.up * Vector3.Distance(rodBase.position, rodTip.position) * bendiness;

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);
            // 2차 베지어 커브 공식
            Vector3 position = (1 - t) * (1 - t) * rodBase.position
                             + 2 * (1 - t) * t * controlPoint
                             + t * t * rodTip.position;
            rodRenderer.SetPosition(i, position);
        }
    }
}