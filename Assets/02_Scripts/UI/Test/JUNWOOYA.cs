using System;
using System.Collections.Generic;
using Fusion;
using Fusion.Sockets;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.XR;

    //                                              +_+   준우의 공부 기록   +_+
public class JUNWOOYA : NetworkBehaviour, INetworkRunnerCallbacks
{


    // 게임에 참여할 플레이어 프리팹 
    public NetworkPrefabRef playerPrefab;

    // 게임에 참여하고 있는 플레이어들을 기억할 Dictionary
    Dictionary<PlayerRef, NetworkObject> spawnedCharacters = new Dictionary<PlayerRef, NetworkObject>();

    // OnPlayerJoined - 사용자가 게임 세션에 입장하는지 알려주는 콜백 함수 
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {
        if (runner.IsServer)
        {
            // 랜덤한 위치 구하기
            Vector3 spawnPosition = UnityEngine.Random.insideUnitSphere * 5;
            spawnPosition.y = 0;

            // 게임에 참여한 플레이어를 위한 캐릭터 스폰
            NetworkObject networkPlayerObject = runner.Spawn(playerPrefab, spawnPosition, Quaternion.identity, player);

            // 참가자 목록에 추가
            spawnedCharacters.Add(player, networkPlayerObject);
        }
    }
    // OnPlayerLeft - 플레이어가 방을 나가면 삭제처리
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {
        if (spawnedCharacters.TryGetValue(player, out NetworkObject networkObject))
        {
            runner.Despawn(networkObject);
            spawnedCharacters.Remove(player);
        }
    }
    // 사용자의 입력을 매프레임 수집하는 함수
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        // NetworkInput에 전달될 데이터
        NetworkInputData data = new NetworkInputData();

        // 왼쪽 컨트롤러 썸스틱 값 얻어오기
        InputDevice leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        if (leftHand.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 axis))
        {
            // 방향 벡터로 변환
            data.direction = new Vector3(axis.x, 0, axis.y); // x = 좌우, y = 앞뒤
        }
        else
        {
            data.direction = Vector3.zero;  
        }

        // 데이터 전달
        input.Set(data);
    }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }

    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {
    }



    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {
    }

    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {
    }

    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {
    }

    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {
    }

    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {
    }

    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {
    }

    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {
    }


    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {
    }

    public void OnConnectedToServer(NetworkRunner runner)
    {
    }

    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {
    }

    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {
    }

    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {
    }

    public void OnSceneLoadDone(NetworkRunner runner)
    {
    }

    public void OnSceneLoadStart(NetworkRunner runner)
    {
    }
}

// 사용자의 입력을 받아 방향데이터를 담아주도록 처리 *INetworkInput은 값이 싸기떄문에 struct(값타입)으로 구현하도록 되어있음, 참조타입(class)는 힙에 할당되고 GC비용 발생
public struct NetworkInputData : INetworkInput
{
    public Vector3 direction;
}


// 네트워크 위치 및 회전 동기화
public class PlyerMove : NetworkBehaviour
{
    public float moveSpeed = 3.0f;
    public float rotSpeed = 180.0f;

    public override void Spawned()
    {
        // 네트워크 상에서 스폰이 완료됐을때 호출되는 이벤트 콜백함수 Spanwed()
    }

    public override void FixedUpdateNetwork()
    {
        // Fusion Tick 고정 간격
        // 상태 계산 및 동기화 (위치, 입력 등)
        // 서버/클라이언트 공통 동작
        // 동작 주기 : 고정 주기 실행 (Tick 단위)
        // 주 용도 : 입력 처리, 이동 계산, 동기화 로직
        // 수정 가능 데이터 : NetworkObject의 상태
        // 보간 사용 여부 : 계산 대상 (원본 데이터)
    }

    public override void Render()
    {
        // Fusion Tick 고정 간격
        // 클라이언트 시각화 전용 (보간, 애니메이션 등)
        // 클라이언트 전용 처리
        // 동작 주기 : 매 프레임 실행 (Unity Update()와 유사)
        // 주 용도 : 위치 보간, 애니메이션 연출, 이펙트 표시 등 
        // 수정 가능 데이터 : 읽기 전용(변형 금지)
        // 보간 사용 여부 : 보간된 결과 반영
    }
}