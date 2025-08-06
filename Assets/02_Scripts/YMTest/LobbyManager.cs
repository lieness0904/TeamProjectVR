using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;
using System.Linq;
using TMPro;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour, INetworkRunnerCallbacks
{
    public static LobbyManager Instance;

    [Header("Player Prefab")]
    [SerializeField] private NetworkObject playerPrefab;

    private Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();
    public NetworkRunner runner;

    private List<SessionInfo> currentSessions = new();

    private void Awake()
    {
        if(Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    private async void Start()
    {
        runner = gameObject.AddComponent<NetworkRunner>();
        runner.AddCallbacks(this);

        await runner.JoinSessionLobby(SessionLobby.ClientServer); // 기본 홈 세션 시작
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
        currentSessions = sessionList;
        Debug.Log($"세션 갱신됨: {currentSessions.Count}개");
    }

    /// <summary>
    /// 세션 시작 (없으면 생성, 있으면 참가)
    /// </summary>
    public async Task TryJoinOrCreate(string sessionName)
    {
        // 로비 연결 대기
        while (!runner.IsCloudReady)
        {
            Debug.Log("로비 연결 대기 중...");
            await Task.Yield();
        }

        var targetSession = currentSessions.FirstOrDefault(s => s.Name == sessionName);

        // 기존 SceneManager 제거 후 재생성
        var sceneManager = GetComponent<NetworkSceneManagerDefault>();
        if (sceneManager == null)
            sceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>();

        if (targetSession != null)
        {
            Debug.Log($"세션 {sessionName} 참가");
            await runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Client,
                SessionName = sessionName,
                SceneManager = sceneManager
            });
        }
        else
        {
            Debug.Log($"세션 {sessionName} 없음, 호스트 생성");

            // sessionName을 씬 이름으로 변환
            string sceneName = "HomeScene"; // 예시: Jeju_Home이면 HomeScene
            int sceneIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/01_Scenes/{sceneName}.unity");
            if (sceneIndex < 0)
            {
                Debug.LogError($"{sceneName} 씬이 빌드 세팅에 없음!");
                return;
            }

            await runner.StartGame(new StartGameArgs
            {
                GameMode = GameMode.Host,
                SessionName = sessionName,
                SceneManager = sceneManager,
                Scene = SceneRef.FromIndex(sceneIndex)
            });
        }
    }

    /// <summary>
    /// 씬 이동 및 세션 전환
    /// </summary>
    public async Task MoveToScene(string sceneType)
    {
        if (!runner.IsServer) return;

        string sessionName = $"Jeju_{sceneType}";
        Debug.Log($"[LobbyManager] {sceneType} 씬으로 이동 시도 (세션: {sessionName})");

        // 클라이언트 전부에게 씬 이동 명령 보내기
        RPC_MoveAllClients(sceneType);

        // 호스트 이동 처리
        await runner.Shutdown(false);
        runner = gameObject.AddComponent<NetworkRunner>();
        runner.AddCallbacks(this);

        int sceneIndex = SceneUtility.GetBuildIndexByScenePath($"Assets/01_Scenes/{sceneType}.unity");
        await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Host,
            SessionName = sessionName,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>(),
            Scene = SceneRef.FromIndex(sceneIndex)
        });
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_MoveAllClients(string sceneType)
    {
        if (runner.IsServer) return; // 서버는 따로 처리하니까 무시
        _ = MoveClientToScene(sceneType);
    }
    private async Task MoveClientToScene(string sceneType)
    {
        string sessionName = $"Jeju_{sceneType}";

        await runner.Shutdown(true);
        runner = gameObject.AddComponent<NetworkRunner>();
        runner.AddCallbacks(this);

        await runner.StartGame(new StartGameArgs
        {
            GameMode = GameMode.Client,
            SessionName = sessionName,
            SceneManager = gameObject.AddComponent<NetworkSceneManagerDefault>()
        });
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"[OnPlayerJoined] Player {player.PlayerId} joined. IsServer={runner.IsServer}, Local={runner.LocalPlayer}");

        if (runner.IsServer && !spawnedCharacters.ContainsKey(player))
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

        Debug.Log($"[SpawnPlayerForServer] {player} 스폰 시도");
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
            runner.Despawn(networkObject);
            spawnedCharacters.Remove(player);
            Debug.Log($"[OnPlayerLeft] {player} removed");
        }

        // 플레이어가 나가고 방에 아무도 없으면 방 폭파
        if (runner.IsServer && !runner.ActivePlayers.Any())
        {
            Debug.Log("[LobbyManager] 방에 플레이어 없음 → 세션 종료");
            runner.Shutdown();
        }
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
        if (!runner.IsServer) return;

        Debug.Log("[OnSceneLoadDone] 서버가 씬 로드를 완료했습니다. 플레이어 스폰 시작");

        foreach (var player in runner.ActivePlayers)
        {
            if (!spawnedCharacters.ContainsKey(player))
            {
                SpawnPlayerForServer(runner, player);
            }
        }
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {

        Debug.Log("[OnSceneLoadStart] 씬 로드 시작");

        // 현재 플레이어만 despawn (전체 despawn X)
        if (spawnedCharacters.TryGetValue(runner.LocalPlayer, out var myPlayer))
        {
            runner.Despawn(myPlayer);
            spawnedCharacters.Remove(runner.LocalPlayer);
        }
    }

    public async void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
        Debug.Log("[LobbyManager] 호스트 마이그레이션 발생 → 새로운 호스트로 승계");

        // 기존 플레이어 오브젝트 정리
        foreach (var obj in spawnedCharacters.Values)
        {
            if (obj != null)
                runner.Despawn(obj);
        }
        spawnedCharacters.Clear();

        // 새로운 호스트로 세션 재시작
        await runner.StartGame(new StartGameArgs
        {
            GameMode = hostMigrationToken.GameMode,
            SessionName = runner.SessionInfo.Name,
            SceneManager = runner.GetComponent<NetworkSceneManagerDefault>(),
            HostMigrationToken = hostMigrationToken // 핵심 부분
        });

        // 호스트 승계 후 플레이어 재스폰
        foreach (var player in runner.ActivePlayers)
        {
            if (!spawnedCharacters.ContainsKey(player))
            {
                SpawnPlayerForServer(runner, player);
            }
        }

        Debug.Log("[LobbyManager] 호스트 승계 완료");
    }

    #region 사용하지 않는 콜백들
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    #endregion
}