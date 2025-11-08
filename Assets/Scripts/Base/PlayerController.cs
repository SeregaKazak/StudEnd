// 08.11.2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

// 08.11.2025 AI-Tag
// This was created with the help of Assistant, a Unity Artificial Intelligence product.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5.0f;
    public float crouchSpeed = 2.5f;
    public float crouchHeight = 1.0f;

    [Header("Jump / Gravity")]
    public float jumpForce = 7.0f;
    public float gravity = 25.81f; // использую более высокий, чем 9.81, для более "живой" физики CharacterController
    public float fallTriggerVelocity = -2f; // порог, после которого считаем, что персонаж в падении

    [Header("Animator damping")]
    public float speedDampTime = 0.12f;
    public float directionDampTime = 0.12f;

    [SerializeField] private GameObject mainChar; // ссылка на объект с Animator
    [SerializeField] private Transform cameraTransform; // ссылка на камеру (для camera-relative движения)

    private CharacterController controller;
    private Animator animator;

    private float originalHeight;
    private bool isCrouching = false;

    private float verticalVelocity = 0f;
    private bool wasGrounded = true;

    private float currentDirection = 0f; // храним текущее направление, чтобы не сбрасывать резко

    private void Start()
    {
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.LogError("CharacterController не найден на объекте.");
        }

        if (mainChar != null)
        {
            animator = mainChar.GetComponent<Animator>();
            if (animator == null)
                Debug.LogError("Animator не найден на mainChar.");
        }
        else
        {
            Debug.LogError("MainChar GameObject не назначен в скрипте PlayerController.");
        }

        originalHeight = controller != null ? controller.height : 1f;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        // Проверяем, не приостановлена ли игра
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsGamePaused)
            return;

        // ----- INPUT -----
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 inputVector = new Vector3(horizontalInput, 0f, verticalInput); // raw input в локальных осях

        // ----- CAMERA-RELATIVE MOVEMENT -----
        Vector3 worldMove = Vector3.zero;
        if (cameraTransform != null)
        {
            Vector3 camForward = cameraTransform.forward;
            Vector3 camRight = cameraTransform.right;
            camForward.y = 0f;
            camRight.y = 0f;
            camForward.Normalize();
            camRight.Normalize();

            worldMove = camForward * verticalInput + camRight * horizontalInput;
        }
        else
        {
            // если камеры нет, используем локальные оси мира
            worldMove = new Vector3(horizontalInput, 0f, verticalInput);
        }

        // скорость по горизонтали (мировой вектор)
        float horizontalSpeed = worldMove.magnitude;
        float normalizedSpeed = Mathf.Clamp01(horizontalSpeed); // 0..1 для BlendTree

        // ----- DIRECTION (угол относительно forward камеры) -----
        float direction = currentDirection;
        if (horizontalSpeed > 0.01f)
        {
            Vector3 referenceForward;
            if (cameraTransform != null)
            {
                referenceForward = cameraTransform.forward;
                referenceForward.y = 0f;
                referenceForward.Normalize();
            }
            else
            {
                referenceForward = Vector3.forward;
            }

            // SignedAngle даёт -180..180, где 0 = вперед по referenceForward
            direction = Vector3.SignedAngle(referenceForward, worldMove.normalized, Vector3.up);
            currentDirection = direction; // сохраняем новое направление
        }
        else
        {
            // при остановке можно плавно возвращать направление к 0, чтобы BlendTree централизовал idle
            currentDirection = Mathf.Lerp(currentDirection, 0f, 10f * Time.deltaTime);
            direction = currentDirection;
        }

        // ----- АНІМАЦИОННЫЕ ПАРАМЕТРЫ -----
        if (animator != null)
        {
            animator.SetFloat("Speed", normalizedSpeed, speedDampTime, Time.deltaTime);
            animator.SetFloat("Direction", direction, directionDampTime, Time.deltaTime);
            animator.SetFloat("VerticalVelocity", verticalVelocity);
        }

        // ----- GRAVITY & JUMP LOGIC -----
        bool isGrounded = controller.isGrounded;

        // если только что приземлились
        if (!wasGrounded && isGrounded)
        {
            // приземление
            if (animator != null)
            {
                animator.ResetTrigger("Falling");
                animator.SetTrigger("Land");
                animator.SetBool("IsGrounded", true);
                animator.SetBool("IsJumping", false);
            }

            // небольшая сила вниз, чтобы контроллер оставался "прижат"
            verticalVelocity = -0.5f;
        }

        // если на земле
        if (isGrounded)
        {
            // allow jump
            if (Input.GetButtonDown("Jump"))
            {
                verticalVelocity = jumpForce;
                if (animator != null)
                {
                    animator.SetBool("IsGrounded", false);
                    animator.SetBool("IsJumping", true);
                    animator.SetTrigger("JumpStart"); // переход в JumpBegin
                }
            }
            else
            {
                if (animator != null)
                {
                    animator.SetBool("IsJumping", false);
                    animator.SetBool("IsGrounded", true);
                }

                // если не прыгаем и на земле — небольшое прижатие
                if (verticalVelocity < 0f)
                    verticalVelocity = -0.5f;
            }
        }
        else // не на земле — в воздухе
        {
            // применяем гравитацию
            verticalVelocity -= gravity * Time.deltaTime;

            if (animator != null)
            {
                animator.SetBool("IsGrounded", false);

                // если сильно падаем — триггер падения
                if (verticalVelocity < fallTriggerVelocity)
                {
                    animator.SetTrigger("Falling");
                }
            }
        }

        // ----- MOVEMENT APPLY -----
        // Горизонтальное движение: нормализуем worldMove, умножаем на текущую скорость (walk / crouch)
        float currentMoveSpeed = isCrouching ? crouchSpeed : moveSpeed;
        Vector3 horizontalMovement = Vector3.zero;
        if (worldMove.sqrMagnitude > 0.0001f)
            horizontalMovement = worldMove.normalized * currentMoveSpeed;

        // итоговый вектор движения с вертикальной скоростью
        Vector3 finalMove = horizontalMovement;
        finalMove.y = verticalVelocity;

        // перемещаем контроллер
        controller.Move(finalMove * Time.deltaTime);

        // ----- CROUCH -----
        HandleCrouch();

        // сохраняем текущее grounded состояние для следующего кадра
        wasGrounded = isGrounded;
    }

    private void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.LeftControl))
        {
            isCrouching = true;
            if (controller != null) controller.height = crouchHeight;
        }
        else if (Input.GetKeyUp(KeyCode.LeftControl))
        {
            isCrouching = false;
            if (controller != null) controller.height = originalHeight;
        }
    }
}
