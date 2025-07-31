using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Fusion;
using System.Linq;
using System.Collections;

public class ChatPanelManager : MonoBehaviour
{
    [Header("Root Panels")]
    [SerializeField] private GameObject chatPanel;
    [SerializeField] private GameObject friendsPanelRoot;
    [SerializeField] private GameObject rankingPanelRoot;

    [Header("Friend Prefab")]
    [SerializeField] private GameObject friendSlotPrefab;
    [SerializeField] private Transform friendListParent;

    [Header("Button Setup")]
    [SerializeField] private Button friendsTabButton;
    [SerializeField] private Button rankingTabButton;

    private List<string> onlineFriends = new();

    [SerializeField] private RankingManager rankingManager;

    void Start()
    {
        chatPanel.SetActive(false);

        friendsTabButton.onClick.AddListener(() => ShowTab(true));
        rankingTabButton.onClick.AddListener(() => ShowTab(false));
    }

    public void OpenChatPanel()
    {
        chatPanel.SetActive(true);
        StartCoroutine(InitializeChatPanel());
    }

    private IEnumerator InitializeChatPanel()
    {
        yield return null; // 1프레임 대기 (UI와 Runner 동기화 대기)

        LayoutRebuilder.ForceRebuildLayoutImmediate(chatPanel.GetComponent<RectTransform>());

        RefreshFriendList();
        rankingManager.LoadRankings();

        ShowTab(true); // 디폴트: 접속 패널
    }

    private void ShowTab(bool showFriends)
    {
        friendsPanelRoot.SetActive(showFriends);
        rankingPanelRoot.SetActive(!showFriends);

        if (showFriends)
        {
            RefreshFriendList();
        }
        else
        {
            rankingManager.LoadRankings();
        }
    }

    private void RefreshFriendList()
    {
        onlineFriends.Clear();

        foreach (Transform child in friendListParent)
        {
            Destroy(child.gameObject);
        }

        var runner = FusionManager.Instance.Runner;
        if (runner == null)
        {
            Debug.LogWarning("FusionManager에서 NetworkRunner를 찾지 못함");
            return;
        }

        // 본인부터 표시
        var myPlayer = FindObjectsOfType<PlayerData>()
            .FirstOrDefault(p => p.Object.HasInputAuthority);
        if (myPlayer != null)
        {
            AddFriendSlot(myPlayer.UserId, myPlayer.Points);
        }

        // 나머지 플레이어 표시
        foreach (var playerRef in runner.ActivePlayers)
        {
            var playerObj = runner.GetPlayerObject(playerRef);
            if (playerObj == null) continue;

            var playerData = playerObj.GetComponent<PlayerData>();
            if (playerData != null && !string.IsNullOrEmpty(playerData.UserId) && !onlineFriends.Contains(playerData.UserId))
            {
                AddFriendSlot(playerData.UserId, playerData.Points);
            }
        }
    }
    private void AddFriendSlot(string userId, int points)
    {
        onlineFriends.Add(userId);
        GameObject slot = Instantiate(friendSlotPrefab, friendListParent);
        FriendSlotUI ui = slot.GetComponent<FriendSlotUI>();
        if (ui != null)
        {
            ui.Setup(userId, points, () => StartVoiceCall(userId), () => TeleportToPlayer(userId));
        }
    }

    private void StartVoiceCall(string playerId)
    {
        Debug.Log($"[VoiceCall] {playerId}에게 통화 요청");
        // TODO: Photon Voice 로직 추가
    }

    private void TeleportToPlayer(string targetUserId)
    {
        StartCoroutine(TeleportAfterDelay(targetUserId));
    }

    private IEnumerator TeleportAfterDelay(string targetUserId)
    {
        yield return new WaitForSeconds(1f);

        var myPlayer = FindObjectsOfType<PlayerData>()
            .FirstOrDefault(p => p.Object.HasInputAuthority);

        if (myPlayer == null)
        {
            Debug.LogWarning("내 PlayerData를 찾을 수 없음");
            yield break;
        }

        var targetPlayer = FindObjectsOfType<PlayerData>()
            .FirstOrDefault(p => p.UserId == targetUserId);

        if (targetPlayer == null)
        {
            Debug.LogWarning($"타겟 플레이어 '{targetUserId}'를 찾을 수 없음");
            yield break;
        }

        Vector3 targetPos = targetPlayer.transform.position + new Vector3(0, 0, 1.5f);
        myPlayer.TeleportTo(targetPos);
    }
}


