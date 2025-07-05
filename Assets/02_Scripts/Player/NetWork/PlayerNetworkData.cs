using Fusion;
using TMPro;
using UnityEngine;
using Photon.Voice.Unity; // --- [1] ���̽� ���ӽ����̽� �߰� ---

public class PlayerNetworkData : NetworkBehaviour
{
    [Networked]
    public NetworkString<_16> PlayerName { get; set; }

    [Header("UI")]
    public TextMeshProUGUI nameText;

    // --- [2] Recorder ������Ʈ�� ���� ���� �߰� ---
    private Recorder voiceRecorder;

    // --- [3] Awake �Լ� �߰� ---
    // ��Ʈ��ũ�� �����Ǳ� ��, �ڱ� �ڽ��� ������Ʈ�� �̸� ã�ƵӴϴ�.
    private void Awake()
    {
        voiceRecorder = GetComponent<Recorder>();
    }

    // �� ��ü�� ��Ʈ��ũ�� �����Ǿ��� �� Fusion�� ���� �ڵ����� ȣ��˴ϴ�.
    public override void Spawned()
    {
        // Object.HasInputAuthority�� �� ������Ʈ�� ������ �ڱ� �ڽ������� Ȯ���մϴ�.
        // ��, �� �ڵ�� �� Ŭ���̾�Ʈ�� �ڱ� �ڽ� ĳ���Ϳ����� ����˴ϴ�.
        if (Object.HasInputAuthority)
        {
            Debug.Log("[PlayerNetworkData] �� ĳ���Ͱ� �����Ǿ����ϴ�. ID�� �����մϴ�.");
            string id = PlayerDataManager.Instance.UserID;
            Debug.Log($"[PlayerNetworkData] PlayerDataManager���� ������ UserID: '{id}'");

            PlayerName = id;

            // --- [4] �� ĳ������ Recorder�� Ȱ��ȭ ---
            if (voiceRecorder != null)
            {
                voiceRecorder.enabled = true;
                Debug.Log("[PlayerNetworkData] ���̽� ���ڴ��� Ȱ��ȭ�߽��ϴ�.");
            }
        }

        // ������ �� ���� �̸����� UI�� �ѹ� ������Ʈ�մϴ�.
        UpdateNameUI();
    }

    public override void Render()
    {
        // Render���� UI�� ��� ������Ʈ�Ͽ� �̸� ������ �ǽð����� ���̰� �մϴ�.
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