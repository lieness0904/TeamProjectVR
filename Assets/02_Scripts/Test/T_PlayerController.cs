using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(NetworkObject))]
public class T_PlayerController : NetworkBehaviour
{
    public float moveSpeed = 5.0f;
    public float jumpForce = 5.0f;
    private CharacterController characterController;
    private Vector2 moveInput;
    private float verticalVelocity = 0f;
    private float gravity = -9.81f;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
    }

    private void Update()
    {
        if (!HasInputAuthority) return;

        // 이동 입력
        if (Keyboard.current.wKey.isPressed) moveInput.y = 1;
        else if (Keyboard.current.sKey.isPressed) moveInput.y = -1;
        else moveInput.y = 0;

        if (Keyboard.current.aKey.isPressed) moveInput.x = -1;
        else if (Keyboard.current.dKey.isPressed) moveInput.x = 1;
        else moveInput.x = 0;

        Vector3 move = new Vector3(moveInput.x, 0, moveInput.y);

        // 점프 입력
        if (characterController.isGrounded)
        {
            verticalVelocity = -1f; // 바닥에 있을 때 약간 눌러주는 용도
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                verticalVelocity = jumpForce;
            }
        }
        else
        {
            verticalVelocity += gravity * Time.deltaTime;
        }

        move.y = verticalVelocity;
        characterController.Move(move * Time.deltaTime * moveSpeed);
    }
}
