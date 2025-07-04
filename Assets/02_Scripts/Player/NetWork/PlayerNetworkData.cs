using Fusion;
using TMPro;
using UnityEngine;

public class PlayerNetworkData : NetworkBehaviour
{
    [Networked]
    public NetworkString<_16> PlayerName { get; set; }

    [Header("UI")]
    public TextMeshProUGUI nameText;

    // �� ��ü�� ��Ʈ��ũ�� �����Ǿ��� �� Fusion�� ���� �ڵ����� ȣ��˴ϴ�.
    public override void Spawned()
    {
        // Object.HasInputAuthority�� �� ������Ʈ�� ������ �ڱ� �ڽ������� Ȯ���մϴ�.
        // ��, �� �ڵ�� �� Ŭ���̾�Ʈ�� �ڱ� �ڽ� ĳ���Ϳ����� ����˴ϴ�.
        if (Object.HasInputAuthority)
        {
            // --- �Ʒ� �� ���� Debug.Log �߰� ---
            Debug.Log("[PlayerNetworkData] �� ĳ���Ͱ� �����Ǿ����ϴ�. ID�� �����մϴ�.");
            string id = PlayerDataManager.Instance.UserID;
            Debug.Log($"[PlayerNetworkData] PlayerDataManager���� ������ UserID: '{id}'");
            // ---------------------------------
            PlayerName = id;
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