using UnityEngine;
using UnityEngine.XR;

public class PlayerMovementDetector : MonoBehaviour
{
    [SerializeField] private float moveThreshold = 0.05f;
    [SerializeField] private float allowedDuration = 0.2f;

    private float movementTimer = 0f;

    private InputDevice hmd;
    private InputDevice leftHand;
    private InputDevice rightHand;

    private DolhareubangWatcher dolhareubang;

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
        if (!FarmGameManager.Instance.IsGameStarted || dolhareubang == null || !dolhareubang.IsWatching)
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
        return device.TryGetFeatureValue(CommonUsages.deviceVelocity, out Vector3 v) && v.magnitude > moveThreshold;
    }

    private void TriggerCaught()
    {
        Debug.Log("들켰다! 귤을 뺏깁니다!");
        // TODO: 패널티 처리
    }
}
