using UnityEngine;
using UnityEngine.XR;

public class PlayerMovementDetector : MonoBehaviour
{
    [SerializeField] private float moveThreshold = 0.05f;
    [SerializeField] private float allowedDuration = 0.2f;

    private float movementTimer;
    private InputDevice hmd, leftHand, rightHand;
    private DolhareubangWatcher dolhareubang;

    // 멀티플레이일 경우 여기서 "내 것" 판정 로직을 구현
    // Fusion: bool IsLocal => TryGetComponent<NetworkObject>(out var no) && no.HasInputAuthority;
    // PUN2 : bool IsLocal => TryGetComponent<PhotonView>(out var pv) && pv.IsMine;
    // 여기서는 싱글/로컬 가정
    private bool IsLocal => true;

    private void OnEnable()
    {
        // 로컬 플레이어만 매니저에 등록
        if (IsLocal && FarmGameManager.Instance != null)
            FarmGameManager.Instance.RegisterLocalPlayer(this);
    }

    private void OnDisable()
    {
        if (IsLocal && FarmGameManager.Instance != null)
            FarmGameManager.Instance.UnregisterLocalPlayer(this);
    }

    private void Start()
    {
        hmd = InputDevices.GetDeviceAtXRNode(XRNode.Head);
        leftHand = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
        rightHand = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);
    }

    public void SetWatcher(DolhareubangWatcher watcher)
    {
        dolhareubang = watcher;
    }

    private void Update()
    {
        var gm = FarmGameManager.Instance;
        if (gm == null || !gm.IsGameStarted || dolhareubang == null || !dolhareubang.IsWatching)
        {
            movementTimer = 0f;
            return;
        }

        bool moved = IsMoving(hmd) || IsMoving(leftHand) || IsMoving(rightHand);
        movementTimer = moved ? movementTimer + Time.deltaTime : 0f;

        if (movementTimer > allowedDuration)
        {
            TriggerCaught();
            movementTimer = 0f;
        }
    }

    private bool IsMoving(InputDevice device)
    {
        return device.TryGetFeatureValue(CommonUsages.deviceVelocity, out Vector3 v)
               && v.magnitude > moveThreshold;
    }

    private void TriggerCaught()
    {
        Debug.Log("들켰다! 귤을 뺏깁니다!");
        // TODO: 패널티 처리
    }
}
