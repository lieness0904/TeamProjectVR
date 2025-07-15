using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static LobbyManager Instance { get; private set; }

    [Header("Player Prefab")]
    [SerializeField] private NetworkObject playerPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    // 새로 추가된 부분: 스폰 시 적용할 회전 값 (오일러 각도)
    [SerializeField] private Vector3 spawnRotation = Vector3.zero;
    private int nextSpawnPointIndex = 0;

    // 현재 세션에 있는 플레이어들의 정보를 저장하는 딕셔너리
    private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    private NetworkObject spawnedPlayer;
    private NetworkRunner runner;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        // 씬이 시작되면 자동으로 게임 세션에 참여를 시도합니다.
        StartGame();
    }

    private async void StartGame()
    {
        // NetworkRunner 컴포넌트를 이 게임 오브젝트에 추가합니다.
        runner = gameObject.AddComponent<NetworkRunner>();

        // 이 스크립트가 네트워크 이벤트를 받을 수 있도록 콜백으로 등록합니다.
        runner.AddCallbacks(this);

        // 게임 시작을 위한 설정
        await runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient, // 자동으로 호스트 또는 클라이언트가 됨
            SessionName = "Jeju_Lobby",           // 접속할 세션(방)의 이름
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>() // 씬 관리자
        });
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            SpawnPlayer(player);
        }
    }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        // 플레이어가 세션을 떠났을 때 호출됩니다.
        if (spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            if (runner.IsServer)
            {
                runner.Despawn(networkObject);
            }
            spawnedCharacters.Remove(player);
            Debug.Log($"OnPlayerLeft: Player {player.PlayerId} left. Despawned character.");
        }
    }
    private void SpawnPlayer(PlayerRef player)
    {
        if (!runner.IsServer) return;
        if (spawnedCharacters.ContainsKey(player)) return;

        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("No spawn points available!");
            return;
        }

        Transform spawnPoint = spawnPoints[nextSpawnPointIndex];
        nextSpawnPointIndex = (nextSpawnPointIndex + 1) % spawnPoints.Count;

        Quaternion rotation = Quaternion.Euler(spawnRotation);

        NetworkObject playerObj = runner.Spawn(playerPrefab, spawnPoint.position, rotation, player);
        spawnedCharacters.Add(player, playerObj);

        Debug.Log($"[Fusion] Spawned player {player.PlayerId} at index {nextSpawnPointIndex}");
    }
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("[Fusion] 씬 로딩 완료");

        // 씬 바뀌면 스폰포인트 갱신 필수
        RefreshSpawnPoints();

        // 서버일 경우, 씬 전환 직후에도 스폰 재확인
        if (runner.IsServer)
        {
            foreach (var player in runner.ActivePlayers)
            {
                if (!spawnedCharacters.ContainsKey(player))
                {
                    SpawnPlayer(player);
                }
            }
        }

        var phoneUI = GameObject.Find("PhoneUI");
        if (phoneUI != null)
            phoneUI.SetActive(false);
    }
    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("[Fusion] 씬 로딩 시작");
    }
    private void RefreshSpawnPoints()
    {
        spawnPoints.Clear();

        string groupName = $"SpawnPointGroup_{SceneManager.GetActiveScene().name}";
        GameObject group = GameObject.Find(groupName);

        if (group == null)
        {
            Debug.LogWarning($"[LobbyManager] Spawn group '{groupName}' not found.");
            return;
        }

        foreach (Transform child in group.transform)
        {
            spawnPoints.Add(child);
        }

        nextSpawnPointIndex = 0;
    }

    #region 사용하지 않는 콜백들
    // 이 스크립트에서는 사용하지 않지만, INetworkRunnerCallbacks 인터페이스를 위해 필요한 빈 함수들입니다.
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    #endregion


}