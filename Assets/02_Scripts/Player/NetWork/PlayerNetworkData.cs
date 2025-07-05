using Fusion;
using TMPro;
using UnityEngine;
using Photon.Voice.Unity; // --- [1] 보이스 네임스페이스 추가 ---

public class PlayerNetworkData : NetworkBehaviour
{
    [Networked]
    public NetworkString<_16> PlayerName { get; set; }

    [Header("UI")]
    public TextMeshProUGUI nameText;

    // --- [2] Recorder 컴포넌트를 담을 변수 추가 ---
    private Recorder voiceRecorder;

    // --- [3] Awake 함수 추가 ---
    // 네트워크에 스폰되기 전, 자기 자신의 컴포넌트를 미리 찾아둡니다.
    private void Awake()
    {
        voiceRecorder = GetComponent<Recorder>();
    }

    // 이 객체가 네트워크상에 스폰되었을 때 Fusion에 의해 자동으로 호출됩니다.
    public override void Spawned()
    {
        // Object.HasInputAuthority는 이 오브젝트의 주인이 자기 자신인지를 확인합니다.
        // 즉, 이 코드는 각 클라이언트의 자기 자신 캐릭터에서만 실행됩니다.
        if (Object.HasInputAuthority)
        {
            Debug.Log("[PlayerNetworkData] 내 캐릭터가 스폰되었습니다. ID를 설정합니다.");
            string id = PlayerDataManager.Instance.UserID;
            Debug.Log($"[PlayerNetworkData] PlayerDataManager에서 가져온 UserID: '{id}'");

            PlayerName = id;

            // --- [4] 내 캐릭터의 Recorder만 활성화 ---
            if (voiceRecorder != null)
            {
                voiceRecorder.enabled = true;
                Debug.Log("[PlayerNetworkData] 보이스 레코더를 활성화했습니다.");
            }
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