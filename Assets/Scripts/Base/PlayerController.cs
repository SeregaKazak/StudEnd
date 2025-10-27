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
        // Обработка движения
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 moveDirection = transform.forward * verticalInput + transform.right * horizontalInput;

        // Обработка прыжка
        if (controller.isGrounded)
        {
            verticalVelocity = -0.5f; // Небольшая отрицательная скорость для прижатия к земле

            if (Input.GetButtonDown("Jump"))
            {
                verticalVelocity = jumpForce;
            }
        }
        else
        {
            verticalVelocity -= 25.81f * Time.deltaTime; // Гравитация
        }

        moveDirection.y = verticalVelocity;

        // Обработка приседания
        HandleCrouch();

        // Применение движения
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
