using Fusion;
using UnityEngine;

public class PlayerVisual : NetworkBehaviour
{
    [Header("필수 연결 요소")]
    public GameObject xrOrigin;

    [Header("시각적 모델")]
    public GameObject fullBodyVisuals;
    public GameObject handsOnlyVisuals;

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            // --- 내가 조종하는 캐릭터 (Local Player) ---

            // 내 VR 컨트롤러와 카메라는 반드시 활성화해야 합니다.
            if (xrOrigin != null) xrOrigin.SetActive(true);

            // 1인칭 시점에서도 내 몸이 보여야 하므로 활성화합니다.
            if (fullBodyVisuals != null) fullBodyVisuals.SetActive(true);

            // 1인칭 전용 손은 사용하지 않으므로 비활성화합니다.
            if (handsOnlyVisuals != null) handsOnlyVisuals.SetActive(false);
        }
        else
        {
            // --- 상대방이 조종하는 캐릭터 (Remote Player) ---

            // 상대방의 VR 컨트롤러와 카메라는 비활성화해야 합니다.
            if (xrOrigin != null) xrOrigin.SetActive(false);

            // 상대방의 몸은 보여야 하므로 활성화합니다.
            if (fullBodyVisuals != null) fullBodyVisuals.SetActive(true);

            // 상대방의 1인칭 전용 손은 당연히 비활성화합니다.
            if (handsOnlyVisuals != null) handsOnlyVisuals.SetActive(false);
        }
    }
}