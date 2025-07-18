using UnityEngine;
using UnityEngine.InputSystem;

public class ClimbHand : MonoBehaviour
{
    public InputActionProperty gripAction;
    public LayerMask climbableLayer;
    public float grabRadius = 0.05f;

    public bool IsGrabbing { get; private set; }
    public Vector3 CurrentPosition => transform.position;

    void Update()
    {
        float gripValue = gripAction.action.ReadValue<float>();
        bool gripPressed = gripValue > 0.5f;
        bool nearClimb = Physics.CheckSphere(transform.position, grabRadius, climbableLayer);

        IsGrabbing = gripPressed && nearClimb;
    }
}