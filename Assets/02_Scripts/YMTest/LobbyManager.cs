using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using UnityEngine.SceneManagement;
using System.Linq;
using TMPro;
using System.Collections;
using UnityEditor;

public class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    private static LobbyManager Instance;

    [Header("Player Prefab")]
    [SerializeField] private NetworkObject playerPrefab;

    private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
    private NetworkRunner runner;

    [SerializeField] private GameObject loadingUIObject;
    private LoadingScreenController lsc;

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (loadingUIObject != null)
            lsc = loadingUIObject.GetComponent<LoadingScreenController>();
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

            SessionName = "Jeju_Lobby11111",
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()

        });
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[OnPlayerJoined] Player {player.PlayerId} joined. IsServer={runner.IsServer}, Local={runner.LocalPlayer}");

        if (runner.IsServer)
        {
            SpawnPlayerForServer(runner, player);
        }

        if (player == runner.LocalPlayer)
        {
            StartCoroutine(WaitForPlayerObject(runner, player));
        }
    }

    private void SpawnPlayerForServer(NetworkRunner runner, PlayerRef player)
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
        runner.SetPlayerObject(player, playerObj);
    }

    private IEnumerator WaitForPlayerObject(NetworkRunner runner, PlayerRef player)
    {
        NetworkObject playerObj = null;
        while ((playerObj = runner.GetPlayerObject(player)) == null)
            yield return null;

        var text = playerObj.GetComponentsInChildren<TextMeshProUGUI>(true)
                            .FirstOrDefault(t => t.name == "PlayerPointText");

        if (text != null)
        {
            PlayerPointManager.Instance.SetPointText(text);
        }
        else
        {
            Debug.LogWarning("PlayerPointText를 찾을 수 없음");
        }

        PlayerDataManager.Instance.StartCoroutine(PlayerDataManager.Instance.WaitAndApplyInventory());
        GameManager.Instance.VoiceManager.ConnectToVoiceRoom(runner.SessionInfo.Name);
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
        if (lsc != null)
            lsc.HideLoading();

        if (!runner.IsServer) return;

        foreach (var player in runner.ActivePlayers)
        {
            if (!spawnedCharacters.ContainsKey(player))
            {
                Vector3 basePos = Vector3.zero;
                var spawnPoints = GameObject.FindGameObjectsWithTag("SpawnPoint");
                if (spawnPoints != null && spawnPoints.Length > 0)
                {
                    var spawnObj = spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];
                    basePos = spawnObj.transform.position;
                }

                float offsetX = UnityEngine.Random.Range(-0.5f, 0.5f);
                float offsetZ = UnityEngine.Random.Range(-0.5f, 0.5f);
                Vector3 spawnPos = basePos + new Vector3(offsetX, 0f, offsetZ);

                var playerObj = runner.Spawn(playerPrefab, spawnPos, Quaternion.identity, player);
                spawnedCharacters[player] = playerObj;
                runner.SetPlayerObject(player, playerObj);
            }
        }
    }
    
    public void OnSceneLoadStart(NetworkRunner runner)
    {
        if (lsc != null)
            lsc.ShowLoading();
        spawnedCharacters.Clear();
    }
    public void OnConnectedToServer(NetworkRunner runner)
    {
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