using UnityEngine;
using UnityEngine.SceneManagement;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Threading.Tasks;
using TMPro;
using Unity.XR.CoreUtils; // XROrigin을 위한 네임스페이스

public class T_NetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static T_NetworkManager Instance { get; private set; }

    private static bool _hasConnectedOnce = false;

    private NetworkRunner _runner;

    [Header("Network Prefabs")]
    public NetworkObject playerPrefab; // T_PlayerVisual과 NetworkTransform이 붙은 플레이어 프리팹

    [Header("스폰 높이")]
    public float spawnY;
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

        if (FindAnyObjectByType<XROrigin>() == null)
        {
            Debug.LogWarning("T_NetworkManager: 씬에 XROrigin이 없습니다. VR 환경이 제대로 설정되지 않았을 수 있습니다.");
        }
    }

    void Start()
    {
        ConnectToLobby();
    }

    void Update()
    {
        // UpdatePlayerListUI(); // FixedUpdateNetwork나 OnPlayerJoined/Left에서 호출하는 것이 더 효율적
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

        Debug.Log("Connecting to Lobby...");
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

        if (_runner != null && _runner.IsRunning)
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

        foreach (var playerRef in _runner.ActivePlayers)
        {
            GameObject listItem = Instantiate(playerListItemPrefab, playerListContent.transform);
            TextMeshProUGUI playerText = listItem.GetComponentInChildren<TextMeshProUGUI>();
            if (playerText != null)
            {
                playerText.text = $"플레이어 {playerRef.PlayerId}";
            }
        }
    }


    // --- INetworkRunnerCallbacks ---

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"OnPlayerJoined: 플레이어 {player.PlayerId}가 입장. 현재 세션: {runner.SessionInfo.Name}");
        UpdatePlayerListUI();

        if (runner.IsServer)
        {
            if (playerPrefab != null)
            {
                Vector3 spawnPosition = UnityEngine.Random.insideUnitSphere * 3;
                spawnPosition.y = spawnY;

                NetworkObject networkPlayerObject = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);
                _spawnedCharacters.Add(player, networkPlayerObject);

                Debug.Log($"플레이어 {player.PlayerId}를 위치 {spawnPosition}에 스폰했습니다.");
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
        UpdatePlayerListUI();

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
        UpdatePlayerListUI();
    }


    public void OnConnectedToServer(NetworkRunner runner) { Debug.Log("Connected to server."); }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { Debug.Log($"Disconnected from server: {reason}"); }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { Debug.LogError($"Connect failed: {reason}"); }

    // 이 함수가 핵심입니다!
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // 로컬 플레이어(이 게임을 실행하고 있는 나 자신)의 캐릭터가 스폰되었는지 확인합니다.
        if (_spawnedCharacters.TryGetValue(runner.LocalPlayer, out NetworkObject playerObject) && playerObject != null)
        {
            // 캐릭터에서 T_PlayerController 스크립트를 찾습니다.
            T_PlayerController playerController = playerObject.GetComponent<T_PlayerController>();
            if (playerController != null)
            {
                // T_PlayerController로부터 현재 컨트롤러 입력 값을 받아와서
                // Fusion의 네트워크 입력(input)에 담아줍니다.
                input.Set(playerController.GetNetworkInput());
            }
        }
    }

    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { Debug.Log("Scene Load Done."); UpdatePlayerListUI(); }
    public void OnSceneLoadStart(NetworkRunner runner) { Debug.Log("Scene Load Start."); }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}