using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    public float jumpForce = 7.0f;
    public float crouchHeight = 1.0f;
    public float crouchSpeed = 2.5f;

    private CharacterController controller;
    private float verticalVelocity;
    private float originalHeight;
    private bool isCrouching = false;

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        originalHeight = controller.height;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // Проверяем, не приостановлена ли игра
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsGamePaused)
            return;
            
        //   ��������� ��������
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 moveDirection = transform.forward * verticalInput + transform.right * horizontalInput;

        // ��������� ������
        if (controller.isGrounded)
        {
            verticalVelocity = -0.5f; // ��������� ������������� �������� ��� �������� � �����

            if (Input.GetButtonDown("Jump"))
            {
                verticalVelocity = jumpForce;
            }
        }
        else
        {
            verticalVelocity -= 25.81f * Time.deltaTime; // ����������
        }

        moveDirection.y = verticalVelocity;

        // ��������� ����������
        HandleCrouch();

        // ���������� ��������
        float currentSpeed = isCrouching ? crouchSpeed : moveSpeed;
        controller.Move(moveDirection * currentSpeed * Time.deltaTime);
    }

    private void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCrouching = true;
            controller.height = crouchHeight;
        }
        else if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            isCrouching = false;
            controller.height = originalHeight;
        }
    }
}
