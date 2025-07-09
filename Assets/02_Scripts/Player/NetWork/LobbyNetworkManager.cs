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
    public static LobbyNetworkManager Instance { get; private set; }

    private static bool _hasConnectedOnce = false;

    private NetworkRunner _runner;

    [Header("Network Prefabs")]
    public NetworkObject playerPrefab;
    public NetworkObject xrOriginPrefab;

    [Header("스폰 위치")]

    public Transform[] spawnPoints;
    // -------------------------


    [Header("UI Elements")]
    public TextMeshProUGUI playerCountText;
    public GameObject playerListContent;
    public GameObject playerListItemPrefab;

    private readonly Dictionary<PlayerRef, NetworkObject> _spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    private const int MAX_PLAYERS_PER_ROOM = 8;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        ConnectToLobby();
    }

    void Update()
    {
        UpdatePlayerListUI();
    }

    async void ConnectToLobby()
    {
        if (_hasConnectedOnce)
        {
            return;
        }
        _hasConnectedOnce = true;

        if (_runner != null && _runner.IsRunning) return;

        _runner = gameObject.AddComponent<NetworkRunner>();
        _runner.AddCallbacks(this);

        await _runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = "게스트 하우스",
        });
    }

    public void JoinFishingContent()
    {
        JoinContentScene("Fishing", "TestFishingScene");
    }

    public async void QuitGame()
    {
        Debug.Log("게임을 종료합니다...");
        if (_runner != null && _runner.IsRunning)
        {
            await _runner.Shutdown();
        }
#if UNITY_EDITOR
        _hasConnectedOnce = false;
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
            Debug.LogError($"'{sceneName}' 씬을 찾을 수 없습니다. File > Build Settings에 씬을 추가했는지, 경로가 올바른지 확인하세요.");
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
                Debug.Log($"성공적으로 {sessionName}에 접속했습니다.");
                break;
            }
            else
            {
                Debug.LogWarning($"{sessionName}에 접속할 수 없거나 꽉 찼을 수 있습니다. 다음 채널을 시도합니다. 이유: {result.ShutdownReason}");
                channelNumber++;
                if (channelNumber > 100)
                {
                    Debug.LogError("접속할 수 있는 채널을 찾지 못했습니다.");
                    break;
                }
            }
        }
    }

    private void UpdatePlayerListUI()
    {
        if (_runner == null || !_runner.IsRunning)
        {
            if (playerCountText != null) playerCountText.text = "접속 정보 없음";
            if (playerListContent != null)
            {
                foreach (Transform child in playerListContent.transform) Destroy(child.gameObject);
            }
            return;
        }

        if (playerCountText != null)
        {
            playerCountText.text = $"'{_runner.SessionInfo.Name}' 현재 인원: {_runner.ActivePlayers.Count()} 명 입니다.";
        }

        if (playerListContent == null || playerListItemPrefab == null) return;

        foreach (Transform child in playerListContent.transform)
        {
            Destroy(child.gameObject);
        }

        foreach (PlayerRef playerRef in _runner.ActivePlayers)
        {
            if (_runner.TryGetPlayerObject(playerRef, out NetworkObject playerObject) && playerObject != null)
            {
                PlayerNetworkData networkData = playerObject.GetComponent<PlayerNetworkData>();
                if (networkData != null)
                {
                    GameObject item = Instantiate(playerListItemPrefab, playerListContent.transform);
                    string playerName = networkData.PlayerName.Value;

                    if (string.IsNullOrEmpty(playerName))
                    {
                        playerName = "연결 중...";
                    }
                    item.GetComponent<TextMeshProUGUI>().text = playerName;
                }
            }
        }
    }


    // --- INetworkRunnerCallbacks ---

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"OnPlayerJoined: 플레이어 {player.PlayerId}가 입장. 현재 세션: {runner.SessionInfo.Name}");

        if (runner.IsServer)
        {
            if (playerPrefab != null)
            {
                // 플레이어 ID 기반으로 스폰 위치 선택 (Index가 범위 내에 있을 경우만)
                int spawnIndex = player.PlayerId % spawnPoints.Length;

                Vector3 spawnPosition = Vector3.zero;
                Quaternion spawnRotation = Quaternion.identity;

                if (spawnPoints != null && spawnPoints.Length > 0 && spawnIndex < spawnPoints.Length && spawnPoints[spawnIndex] != null)
                {
                    spawnPosition = spawnPoints[spawnIndex].position;
                    spawnRotation = spawnPoints[spawnIndex].rotation;
                }
                else
                {
                    Debug.LogWarning("스폰 포인트가 설정되지 않았거나 유효하지 않습니다. 기본 위치 (0,0,0)에 스폰합니다.");
                }

                NetworkObject networkPlayerObject = runner.Spawn(playerPrefab, spawnPosition, spawnRotation, player);
                _spawnedCharacters.Add(player, networkPlayerObject);
            }
            else
            {
                Debug.LogError("Player Prefab이 할당되지 않았습니다!");
            }
        }
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"OnPlayerLeft: 플레이어 {player.PlayerId}가 퇴장. 현재 세션: {runner.SessionInfo.Name}");

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
        Debug.Log($"OnShutdown: NetworkRunner가 종료되었습니다. 이유: {shutdownReason}");
        _spawnedCharacters.Clear();
    }


    public void OnConnectedToServer(NetworkRunner runner)
    {
        if (runner.IsRunning && runner.LocalPlayer != null)
        {
            runner.TryGetPlayerObject(runner.LocalPlayer, out NetworkObject playerObj);

            if (playerObj != null && playerObj.HasInputAuthority)
            {
                NetworkObject player = Instantiate(playerPrefab);

                var rigManager = playerPrefab.GetComponent<RiggingManager>();
                rigManager.headIK = playerObj.transform.Find("HeadIK");
                rigManager.leftHandIK = playerObj.transform.Find("LeftHandIK");
                rigManager.rightHandIK = playerObj.transform.Find("RightHandIK");

                rigManager.transform.SetParent(playerObj.transform);
            }
        }
    }
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