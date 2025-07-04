using Fusion;
using TMPro;
using UnityEngine;

public class PlayerNetworkData : NetworkBehaviour
{
    [Networked]
    public NetworkString<_16> PlayerName { get; set; }

    [Header("UI")]
    public TextMeshProUGUI nameText;

    // 이 객체가 네트워크상에 스폰되었을 때 Fusion에 의해 자동으로 호출됩니다.
    public override void Spawned()
    {
        // Object.HasInputAuthority는 이 오브젝트의 주인이 자기 자신인지를 확인합니다.
        // 즉, 이 코드는 각 클라이언트의 자기 자신 캐릭터에서만 실행됩니다.
        if (Object.HasInputAuthority)
        {
            // --- 아래 세 줄의 Debug.Log 추가 ---
            Debug.Log("[PlayerNetworkData] 내 캐릭터가 스폰되었습니다. ID를 설정합니다.");
            string id = PlayerDataManager.Instance.UserID;
            Debug.Log($"[PlayerNetworkData] PlayerDataManager에서 가져온 UserID: '{id}'");
            // ---------------------------------
            PlayerName = id;
        }

        // 스폰될 때 현재 이름으로 UI를 한번 업데이트합니다.
        UpdateNameUI();
    }

    public override void Render()
    {
        // Render에서 UI를 계속 업데이트하여 이름 변경이 실시간으로 보이게 합니다.
        UpdateNameUI();
    }

    private void UpdateNameUI()
    {
        if (nameText != null && nameText.text != PlayerName.Value)
        {
            nameText.text = PlayerName.Value;
        }
    }
}