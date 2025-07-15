using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;

public class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Player Prefab")]
    [SerializeField] private NetworkObject playerPrefab;

    [Header("Spawn Settings")]
    [SerializeField] private List<Transform> spawnPoints = new List<Transform>();
    // 새로 추가된 부분: 스폰 시 적용할 회전 값 (오일러 각도)
    [SerializeField] private Vector3 spawnRotation = Vector3.zero;
    private int nextSpawnPointIndex = 0;

    // 현재 세션에 있는 플레이어들의 정보를 저장하는 딕셔너리
    private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    void Start()
    {
        // 씬이 시작되면 자동으로 게임 세션에 참여를 시도합니다.
        StartGame();
    }

    private async void StartGame()
    {
        // NetworkRunner 컴포넌트를 이 게임 오브젝트에 추가합니다.
        var runner = gameObject.AddComponent<NetworkRunner>();

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
        // 새로운 플레이어가 세션에 참여했을 때 호출됩니다.
        // 호스트(서버 역할)만 플레이어를 스폰할 권한이 있습니다.
        if (runner.IsServer)
        {
            Debug.Log($"OnPlayerJoined: Player {player.PlayerId} joined. Spawning character.");

            Transform spawnPoint = spawnPoints[nextSpawnPointIndex];

            // 다음 플레이어를 위해 인덱스를 1 증가시킵니다.
            nextSpawnPointIndex = (nextSpawnPointIndex + 1) % spawnPoints.Count;

            // --- [수정된 부분] ---
            // 인스펙터에서 설정한 Vector3 회전 값을 Quaternion으로 변환합니다.
            Quaternion rotation = Quaternion.Euler(spawnRotation);

            // 플레이어 프리팹을 스폰할 때, 스폰 포인트의 위치와 우리가 지정한 회전 값을 사용합니다.
            NetworkObject networkPlayerObject = runner.Spawn(playerPrefab, spawnPoint.position, rotation, player);
            // --------------------

            // 스폰된 플레이어 정보를 딕셔너리에 추가하여 관리합니다.
            spawnedCharacters.Add(player, networkPlayerObject);
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
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    #endregion
}