using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using System.Linq;
using TMPro;

public class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    [Header("Player Prefab")]
    [SerializeField] private NetworkObject playerPrefab;

    private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
    private NetworkRunner runner;

    private void Start()
    {
        StartGame();
    }

    private async void StartGame()
    {
        // DontDestroyOnLoad는 Awake에서 호출하는 것이 더 안정적입니다.
        if (transform.parent == null)
        {
            DontDestroyOnLoad(this.gameObject);
        }

        runner = gameObject.AddComponent<NetworkRunner>();
        runner.AddCallbacks(this);

        await runner.StartGame(new StartGameArgs()
        {
            GameMode = GameMode.AutoHostOrClient,
            SessionName = "Jeju_Lobby111",
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (!runner.IsServer) return;

        Vector3 basePos = Vector3.zero;
        var spawnObj = GameObject.FindWithTag("SpawnPoint");
        if (spawnObj != null)
            basePos = spawnObj.transform.position;

        float offsetX = UnityEngine.Random.Range(-1.5f, 1.5f);
        float offsetZ = UnityEngine.Random.Range(-1.5f, 1.5f);
        Vector3 spawnPos = basePos + new Vector3(offsetX, 0f, offsetZ);

        var playerObj = runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
        spawnedCharacters[player] = playerObj;

        runner.SetPlayerObject(player, playerObj);

        if (player == runner.LocalPlayer)
        {
            runner.SetPlayerObject(player, playerObj);
        }

        // 플레이어 포인트 연결
        if (player == runner.LocalPlayer)
        {
            var text = playerObj.GetComponentsInChildren<TextMeshProUGUI>(true)
                                .FirstOrDefault(t => t.name == "PlayerPointText");

            if (text != null)
            {
                PlayerPointManager.Instance.SetPointText(text);
                Debug.Log("PlayerPointText 연결 성공");
            }
            else
            {
                Debug.LogWarning("PlayerPointText를 찾을 수 없음");
            }

            PlayerDataManager.Instance.StartCoroutine(PlayerDataManager.Instance.WaitAndApplyInventory());
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

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (!runner.IsServer) return;

        foreach (var player in runner.ActivePlayers)
        {
            if (!spawnedCharacters.ContainsKey(player))
            {
                Vector3 basePos = Vector3.zero;
                var spawnObj = GameObject.FindWithTag("SpawnPoint");
                if (spawnObj != null)
                    basePos = spawnObj.transform.position;

                float offsetX = UnityEngine.Random.Range(-1.5f, 1.5f);
                float offsetZ = UnityEngine.Random.Range(-1.5f, 1.5f);
                Vector3 spawnPos = basePos + new Vector3(offsetX, 0f, offsetZ);

                var playerObj = runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
                spawnedCharacters[player] = playerObj;

                // --- [핵심 수정] ---
                // 씬 로드 후 스폰된 플레이어 역시 공식 객체로 등록합니다.
                runner.SetPlayerObject(player, playerObj);

                if (player == runner.LocalPlayer)
                {
                    runner.SetPlayerObject(player, playerObj); 
                }
            }
        }
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
        var keys = spawnedCharacters.Keys.ToList();
        foreach (var player in keys)
        {
            var obj = spawnedCharacters[player];
            if (obj != null)
            {
                runner.Despawn(obj);
            }
        }
        spawnedCharacters.Clear();
    }
    public void OnConnectedToServer(NetworkRunner runner)
    {
        GameManager.Instance.VoiceManager.ConnectToVoiceRoom(runner.SessionInfo.Name);
    }

    #region 사용하지 않는 콜백들
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
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