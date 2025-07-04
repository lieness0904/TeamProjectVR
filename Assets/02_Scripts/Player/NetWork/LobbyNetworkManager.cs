using UnityEngine;
using Fusion;
using Fusion.Sockets;
using System.Collections.Generic;
using System;

// MonoBehaviour와 함께 INetworkRunnerCallbacks 인터페이스를 상속받습니다.
public class LobbyNetworkManager : MonoBehaviour, INetworkRunnerCallbacks
{
    // 네트워크 연결을 관리하는 핵심 컴포넌트
    private NetworkRunner _runner;

    // 게임(로비 씬)이 시작될 때 자동으로 호출되는 함수
    void Start()
    {
        // 서버에 접속을 시도하는 함수를 호출합니다.
        ConnectToPhotonServer();
    }

    // 비동기적으로 포톤 서버에 접속을 시작하는 함수
    async void ConnectToPhotonServer()
    {
        // NetworkRunner가 아직 없으면 새로 생성합니다.
        if (_runner == null)
        {
            // 이 게임오브젝트에 NetworkRunner 컴포넌트를 추가하여 인스턴스를 만듭니다.
            _runner = gameObject.AddComponent<NetworkRunner>();
        }

        // 씬 전환을 관리할 기본 씬 매니저를 추가합니다.
        gameObject.AddComponent<NetworkSceneManagerDefault>();

        // StartGame 메소드를 호출하여 서버 접속을 시작합니다.
        var result = await _runner.StartGame(new StartGameArgs()
        {
            // 게임 모드: AutoHostOrClient는 방이 없으면 새로 만들고(Host), 있으면 참가(Client)하는 편리한 모드입니다.
            GameMode = GameMode.AutoHostOrClient,

            // 세션 이름(방 이름): 같은 이름을 가진 플레이어들끼리 만나게 됩니다.
            SessionName = "VRSmartCityLobby",

            // 콜백 리스너: 이 스크립트에서 발생하는 네트워크 이벤트를 감지하도록 설정합니다.
            
        });

        // 접속 시도 결과에 따라 로그를 출력합니다.
        if (result.Ok)
        {
            Debug.Log("서버 접속 시작 성공!");
        }
        else
        {
            Debug.LogError($"서버 접속 실패: {result.ShutdownReason}");
        }
    }

    // --- INetworkRunnerCallbacks 인터페이스 필수 구현 메소드들 ---

    public void OnConnectedToServer(NetworkRunner runner)
    {
        Debug.Log("OnConnectedToServer: 서버에 성공적으로 연결되었습니다.");
    }

    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"OnPlayerJoined: 플레이어 {player.PlayerId}가 입장했습니다.");
    }

    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        Debug.Log($"OnPlayerLeft: 플레이어 {player.PlayerId}가 퇴장했습니다.");
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
        Debug.LogError($"OnConnectFailed: 서버 연결에 실패했습니다. 이유: {reason}");
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
        Debug.Log($"OnDisconnectedFromServer: 서버와 연결이 끊겼습니다. 이유: {reason}");
    }

    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
        Debug.Log($"OnShutdown: NetworkRunner가 종료되었습니다. 이유: {shutdownReason}");
    }

    // -- 이하 빈 내용으로 구현해야 하는 메소드들 --

    public void OnInput(NetworkRunner runner, NetworkInput input) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
}