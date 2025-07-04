using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using TMPro;

public class LobbyNetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    private NetworkRunner _runner;

    [Header("Network Prefabs")]
    public NetworkObject playerPrefab;

    [Header("UI Elements")]
    public TextMeshProUGUI playerCountText;
    public GameObject playerListContent;
    public GameObject playerListItemPrefab;

    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    private const int MAX_PLAYERS_PER_ROOM = 8;

    void Awake()
    {
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        ConnectToLobby();
    }

    void Update()
    {
        // �� ������ UI�� ������Ʈ�Ͽ� �̸� ���� ���� �ǽð����� �ݿ��մϴ�.
        UpdatePlayerListUI();
    }

    async void ConnectToLobby()
    {
        if (_runner != null && _runner.IsRunning) return;

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.AddCallbacks(this);

        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = "�Խ�Ʈ �Ͽ콺",
        });
    }

    public void JoinFishingContent()
    {
        JoinContentScene("Fishing", "TestFishingScene");
    }

    public async void QuitGame()
    {
        Debug.Log("������ �����մϴ�...");
        if (_runner != null && _runner.IsRunning)
        {
            await _runner.Shutdown();
        }
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    public async void JoinContentScene(string contentName, string sceneName)
    {
        int sceneIndex = SceneUtility.GetBuildIndexByScenePath("Assets/Scenes/" + sceneName + ".unity");
        if (sceneIndex < 0)
        {
            Debug.LogError($"'{sceneName}' ���� ã�� �� �����ϴ�. File > Build Settings�� ���� �߰��ߴ���, ��ΰ� �ùٸ��� Ȯ���ϼ���.");
            return;
        }

        if (_runner != null)
        {
            await _runner.Shutdown();
        }

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.AddCallbacks(this);
        gameObject.AddComponent<NetworkSceneManagerDefault>();

        int channelNumber = 1;
        while (true)
        {
            string sessionName = $"{contentName}_{channelNumber}";
            var result = await _runner.StartGame(new StartGameArgs()
            {
                GameMode = GameMode.AutoHostOrClient,
                SessionName = sessionName,
                PlayerCount = MAX_PLAYERS_PER_ROOM,
                Scene = SceneRef.FromIndex(sceneIndex)
            });

            if (result.Ok)
            {
                Debug.Log($"���������� {sessionName}�� �����߽��ϴ�.");
                break;
            }
            else
            {
                Debug.LogWarning($"{sessionName}�� ������ �� ���ų� �� á�� �� �ֽ��ϴ�. ���� ä���� �õ��մϴ�. ����: {result.ShutdownReason}");
                channelNumber++;
                if (channelNumber > 100)
                {
                    Debug.LogError("������ �� �ִ� ä���� ã�� ���߽��ϴ�.");
                    break;
                }
            }
        }
    }

    // --- ������� �ٽ� ���� �κ� ---
    private void UpdatePlayerListUI()
    {
        if (_runner == null || !_runner.IsRunning)
        {
            if (playerCountText != null) playerCountText.text = "���� ���� ����";
            if (playerListContent != null)
            {
                foreach (Transform child in playerListContent.transform) Destroy(child.gameObject);
            }
            return;
        }

        if (playerCountText != null)
        {
            playerCountText.text = $"'{_runner.SessionInfo.Name}' ���� �ο�: {_runner.ActivePlayers.Count()} �� �Դϴ�.";
        }

        // --- UI ������Ʈ ���� ���� ---
        // 1. ���� ����� ��� �����մϴ�.
        if (playerListContent == null || playerListItemPrefab == null) return;
        foreach (Transform child in playerListContent.transform)
        {
            Destroy(child.gameObject);
        }

        // 2. ���� ���� �ִ� ��� PlayerNetworkData ��ü�� ã�Ƽ� ����� �ٽ� ����ϴ�.
        // �� ����� Ŭ���̾�Ʈ/���� ��ο��� �����ϰ� �����մϴ�.
        foreach (var player in FindObjectsOfType<PlayerNetworkData>())
        {
            GameObject item = Instantiate(playerListItemPrefab, playerListContent.transform);
            string playerName = player.PlayerName.Value;

            if (string.IsNullOrEmpty(playerName))
            {
                playerName = "���� ��...";
            }

            item.GetComponent<TextMeshProUGUI>().text = playerName;
        }
    }
    // --- ������� �ٽ� ���� �κ� ---


    // --- INetworkRunnerCallbacks ---

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"OnPlayerJoined: �÷��̾� {player.PlayerId}�� ����. ���� ����: {runner.SessionInfo.Name}");

        if (runner.IsServer)
        {
            if (playerPrefab != null)
            {
                NetworkObject networkPlayerObject = runner.Spawn(playerPrefab, Vector3.zero, Quaternion.identity, player);
                _spawnedCharacters.Add(player, networkPlayerObject);
            }
            else
            {
                Debug.LogError("Player Prefab�� �Ҵ���� �ʾҽ��ϴ�!");
            }
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"OnPlayerLeft: �÷��̾� {player.PlayerId}�� ����. ���� ����: {runner.SessionInfo.Name}");

        if (_spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            if (runner.IsServer)
            {
                runner.Despawn(networkObject);
            }
            _spawnedCharacters.Remove(player);
        }
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"OnShutdown: NetworkRunner�� ����Ǿ����ϴ�. ����: {shutdownReason}");
        _spawnedCharacters.Clear();
    }

    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}