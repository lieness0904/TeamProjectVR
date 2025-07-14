using UnityEngine;
using Fusion;
using UnityEngine.InputSystem;
using Fusion.Sockets;

public class PlayerInputHandler : NetworkBehaviour, INetworkRunnerCallbacks
{
    [Header("Player Components")]
    // 이 필드에 플레이어의 시점(HMD)이 될 카메라의 Transform을 할당해야 합니다.
    public Transform cameraTransform;

    [Header("XR Controller Input Action")]
    [SerializeField] private InputActionProperty moveAction;

    // 필요한 컴포넌트 참조
    private NetworkCharacterController _ncc;

    void Awake()
    {
        // NetworkCharacterController 컴포넌트의 참조를 미리 가져옵니다.
        _ncc = GetComponent<NetworkCharacterController>();
    }

    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            Runner.AddCallbacks(this);
            Debug.Log("PlayerInputHandler Spawned and callbacks added for local player.");
        }
    }

    // --- [새로 추가된 함수] ---
    // 퓨전의 물리 시뮬레이션 틱마다 호출됩니다. 모든 네트워크 로직은 여기서 처리해야 합니다.
    public override void FixedUpdateNetwork()
    {
        // 입력 권한이 있는 플레이어의 입력을 받아와서 처리합니다.
        if (GetInput(out NetworkInputData data))
        {
            // 카메라의 정면/오른쪽 방향을 기준으로 이동 방향을 계산합니다.
            Vector3 moveDirection = cameraTransform.forward * data.movementInput.y + cameraTransform.right * data.movementInput.x;

            // y축(수직)으로는 움직이지 않도록 고정합니다.
            moveDirection.y = 0;

            // NetworkCharacterController의 Move 함수를 호출하여 캐릭터를 움직입니다.
            _ncc.Move(moveDirection.normalized);
        }
    }
    // -------------------------

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        if (!Object.HasInputAuthority)
        {
            return;
        }

        Vector2 moveValue = moveAction.action.ReadValue<Vector2>();

        NetworkInputData data = new NetworkInputData
        {
            movementInput = moveValue
        };

        input.Set(data);
    }

    #region 사용하지 않는 콜백들
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnSessionListUpdated(NetworkRunner runner, System.Collections.Generic.List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, System.Collections.Generic.Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, System.ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    #endregion
}