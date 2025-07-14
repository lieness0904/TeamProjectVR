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
    private Animator animator;

    private Vector2 moveInput;
    private float verticalVelocity = 0f;
    private float gravity = -9.81f;

    private void Start()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    private void Update()
    {
        if (!HasInputAuthority) return;

        // 이동 입력
        float x = 0;
        float y = 0;

        if (Keyboard.current.wKey.isPressed) y += 1;
        if (Keyboard.current.sKey.isPressed) y -= 1;
        if (Keyboard.current.aKey.isPressed) x -= 1;
        if (Keyboard.current.dKey.isPressed) x += 1;

        moveInput = new Vector2(x, y).normalized;
        Debug.Log($"MoveInput: {moveInput}"); 

        // 실제 이동 벡터 (월드 기준)
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

        Vector3 velocity = move * moveSpeed;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);

        // 애니메이션 파라미터 전달
        animator.SetFloat("MoveX", moveInput.x);
        animator.SetFloat("MoveY", moveInput.y);
    }
}
