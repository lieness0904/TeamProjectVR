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
    // --- [수정] --- 아래 playerPrefab에 NetworkTransform 컴포넌트를 꼭 추가해주세요!
    public NetworkObject playerPrefab;

    [Header("스폰 위치")]
    public Transform[] spawnPoints;

    [Header("UI Elements")]
    public TextMeshProUGUI playerCountText;
    public GameObject playerListContent;
    public GameObject playerListItemPrefab; // 플레이어 목록에 표시될 UI 프리팹

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
        // --- [수정] --- 로비 씬에서만 ConnectToLobby를 호출하도록 변경
        if (SceneManager.GetActiveScene().name == "Lobby")
        {
            ConnectToLobby();
        }
    }

    // --- [수정] --- Update에서 매번 UI를 갱신하는 로직 삭제 (성능 저하의 원인)
    // void Update()
    // {
    //     UpdatePlayerListUI();
    // }

    async void ConnectToLobby()
    {
        if (_hasConnectedOnce)
        {
            // 이미 연결된 Runner가 있다면 UI만 업데이트
            if (_runner != null && _runner.IsRunning)
            {
                UpdatePlayerListUI();
            }
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
            // --- [수정] --- 호스트 마이그레이션 기능 활성화
            EnableClientSessionCreation = true
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
                Scene = SceneRef.FromIndex(sceneIndex),
                // --- [수정] --- 콘텐츠 씬에서도 호스트 마이그레이션 기능 활성화
                EnableClientSessionCreation = true
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

    // --- [수정] --- UI 업데이트 로직 수정 및 최적화
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

        // 기존 목록 모두 삭제
        foreach (Transform child in playerListContent.transform)
        {
            Destroy(child.gameObject);
        }


        // --- [추가] --- 현재 접속한 플레이어 목록을 기반으로 UI 아이템 생성
        foreach (PlayerRef player in _runner.ActivePlayers)
        {
            GameObject item = Instantiate(playerListItemPrefab, playerListContent.transform);

            // playerListItemPrefab에 TextMeshProUGUI 컴포넌트가 있다고 가정
            TextMeshProUGUI playerNameText = item.GetComponentInChildren<TextMeshProUGUI>();
            if (playerNameText != null)
            {
                string id = $"Player {player.PlayerId}";
                if (player == _runner.LocalPlayer) id += " (나)";
                

                playerNameText.text = id;
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

        // --- [수정] --- 플레이어 입장 시 UI 업데이트
        UpdatePlayerListUI();
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

        // --- [수정] --- 플레이어 퇴장 시 UI 업데이트
        UpdatePlayerListUI();
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"OnShutdown: NetworkRunner가 종료되었습니다. 이유: {shutdownReason}");
        _spawnedCharacters.Clear();

        // --- [수정] --- 로비로 돌아가도록 씬 전환 로직 추가 (호스트가 나가서 종료됐을 경우 등)
        SceneManager.LoadScene("Lobby");
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("OnConnectedToServer: 서버에 연결되었습니다.");
    }

    // --- [수정] --- 호스트 마이그레이션 콜백 추가
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        Debug.Log("OnHostMigration: 호스트 마이그레이션이 시작되었습니다.");
        // 필요 시 여기에 호스트 마이그레이션 관련 로직 추가 (예: UI 표시)
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}