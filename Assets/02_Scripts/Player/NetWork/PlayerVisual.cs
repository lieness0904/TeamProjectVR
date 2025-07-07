using System.Collections;
using System.Collections.Generic;
using Fusion;
using UnityEngine;

public class PlayerVisual : NetworkBehaviour
{
    [Header("VR용 XR리그")]
    public GameObject xrOrigin; // 내 화면에서만 켜짐

    [Header("공통 시각 요소")]
    public GameObject characterModel; // 손 포함 몸 전체

    [Header("내 손만 보이게 제어할 대상 (CharacterModel 내부의 손들")]
    public GameObject[] handsOnly; // 내 화면에서 보여야 하는 손 오브젝트

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            // 내 플레이어일 때
            xrOrigin.SetActive(true);            // 내 XR 리그 활성화
            characterModel.SetActive(true);      // 모델 활성화
            SetBodyPartsVisibleExceptHands(false);
        }
        else
        {
            // 상대방 플레이어일 때
            xrOrigin.SetActive(false);           // 상대 XR 리그 끔
            characterModel.SetActive(true);      // 전체 모델 보여줌
            SetBodyPartsVisibleExceptHands(true);
        }
    }
    private void SetBodyPartsVisibleExceptHands(bool bodyVisible)
    {
        // 상대방이면 전체 보여주고, 내 화면이면 손만 보여줌
        foreach (Transform child in characterModel.transform)
        {
            bool isHand = false;
            foreach (var hand in handsOnly)
            {
                if (child.gameObject == hand)
                {
                    isHand = true;
                    break;
                }
            }

            child.gameObject.SetActive(isHand || bodyVisible);
        }
    }
}
