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
    [SerializeField] private Vector3 spawnRotation = Vector3.zero;

    private int nextSpawnPointIndex = 0;
    private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
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

    private void Start()
    {
        StartGame();
    }

    private async void StartGame()
    {
        runner = gameObject.AddComponent<NetworkRunner>();
        runner.AddCallbacks(this);

        await runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = "Jeju_Lobby",
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
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

        RefreshSpawnPoints();

        if (runner.IsServer)
        {
            foreach (var player in runner.ActivePlayers)
            {
                SpawnPlayer(player);
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
    public void OnSceneLoadDone(NetworkRunner runner)
    {
        Debug.Log("[Fusion] 씬 로딩 완료");
        if (spawnedCharacters.ContainsKey(runner.LocalPlayer)) return;
        var phoneUI = GameObject.Find("PhoneUI");
        if (phoneUI != null)
            phoneUI.SetActive(false);
    }
    public void OnSceneLoadStart(NetworkRunner runner)
    {
        Debug.Log("[Fusion] 씬 로딩 시작");
    }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    #endregion
}