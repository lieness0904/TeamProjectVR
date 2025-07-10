using Fusion;
using UnityEngine;

public class PlayerVisual : NetworkBehaviour
{
    [Header("필수 연결 요소")]
    public GameObject xrOrigin; // 내 화면에서만 켜질 XR Origin

    [Header("시각적 모델")]
    public GameObject fullBodyVisuals; // 몸 전체 모델 (상대방에게 보일 부분)
    public GameObject handsOnlyVisuals; // 1인칭 손 모델 (나에게만 보일 부분)

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            // --- 내가 조종하는 캐릭터일 때 ---
            // 내 VR 화면을 켜고, 나에게 보일 몸통도 켠다.
            // 1인칭 전용 손은 이제 사용하지 않으므로 끈다.
            if (xrOrigin != null) xrOrigin.SetActive(true);
            if (fullBodyVisuals != null) fullBodyVisuals.SetActive(true);
            if (handsOnlyVisuals != null) handsOnlyVisuals.SetActive(false);
        }
        else
        {
            // --- 상대방이 조종하는 캐릭터일 때 (이전과 동일) ---
            // 상대방의 VR 화면은 끄고, 나에게 보여야 할 몸통은 켠다.
            if (xrOrigin != null) xrOrigin.SetActive(false);
            if (fullBodyVisuals != null) fullBodyVisuals.SetActive(true);
            if (handsOnlyVisuals != null) handsOnlyVisuals.SetActive(false);
        }
    }
}