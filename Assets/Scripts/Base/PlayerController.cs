using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5.0f;
    public float jumpForce = 4.0f;
    public float crouchHeight = 1.0f;
    public float crouchSpeed = 2.5f;

    [Header("Animator damping")]
    public float speedDampTime = 0.12f;
    public float directionDampTime = 0.12f;

    [Header("Jump / Gravity")]
    public float gravity = 15.0f; // гравитация (меньше стандартной для более плавного падения)
    public float fallTriggerVelocity = -2f; // порог, после которого считаем, что персонаж в падении

    [SerializeField] private GameObject mainChar; // ссылка на объект с Animator

    private CharacterController controller;
    private Animator animator;
    private float verticalVelocity;
    private float originalHeight;
    private bool isCrouching = false;
    private bool wasGrounded = true;
    private float currentDirection = 0f; // храним текущее направление, чтобы не сбрасывать резко

    private IPlayerInputService inputService;
    private IGameStateService gameStateService;

    private void Start()
    {
        CacheServices();

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
        if (IsGamePaused())
            return;

        Vector2 moveAxis = GetMoveAxis();
        float horizontalInput = moveAxis.x;
        float verticalInput = moveAxis.y;

        Vector3 moveDirection = transform.forward * verticalInput + transform.right * horizontalInput;

        // Вычисляем скорость для аниматора
        Vector3 inputVector = new Vector3(horizontalInput, 0f, verticalInput);
        float horizontalSpeed = inputVector.magnitude;
        float normalizedSpeed = Mathf.Clamp01(horizontalSpeed); // 0..1 для BlendTree

        // Вычисляем направление (угол относительно forward персонажа)
        float direction = currentDirection;
        if (horizontalSpeed > 0.01f)
        {
            // Вычисляем угол между forward персонажа и направлением движения
            Vector3 worldMove = moveDirection;
            worldMove.y = 0f;
            worldMove.Normalize();
            
            direction = Vector3.SignedAngle(transform.forward, worldMove, Vector3.up);
            currentDirection = direction; // сохраняем новое направление
        }
        else
        {
            // при остановке плавно возвращаем направление к 0, чтобы BlendTree централизовал idle
            currentDirection = Mathf.Lerp(currentDirection, 0f, 10f * Time.deltaTime);
            direction = currentDirection;
        }

        // ----- АНИМАЦИОННЫЕ ПАРАМЕТРЫ -----
        if (animator != null)
        {
            animator.SetFloat("Speed", normalizedSpeed, speedDampTime, Time.deltaTime);
            animator.SetFloat("Direction", direction, directionDampTime, Time.deltaTime);
            animator.SetFloat("VerticalVelocity", verticalVelocity);
        }

        // Обработка прыжка
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

            verticalVelocity = -0.5f; // Íåáîëüøàÿ îòðèöàòåëüíàÿ ñêîðîñòü äëÿ ïðèæàòèÿ ê çåìëå
        }
        else if (isGrounded)
        {
            bool jumpPressed = inputService != null ? inputService.JumpPressedThisFrame : Input.GetButtonDown("Jump");
            if (jumpPressed)
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

                verticalVelocity = -0.5f; // Íåáîëüøàÿ îòðèöàòåëüíàÿ ñêîðîñòü äëÿ ïðèæàòèÿ ê çåìëå
            }
        }
        else
        {
            verticalVelocity -= gravity * Time.deltaTime; // Ãðàâèòàöèÿ

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

        moveDirection.y = verticalVelocity;

        // Обработка приседания
        HandleCrouch();

        // Ïðèìåíåíèå äâèæåíèÿ
        float currentSpeed = isCrouching ? crouchSpeed : moveSpeed;
        controller.Move(moveDirection * currentSpeed * Time.deltaTime);

        // сохраняем текущее grounded состояние для следующего кадра
        wasGrounded = isGrounded;
    }

    private void OnEnable()
    {
        CacheServices();
    }

    private void HandleCrouch()
    {
        bool crouchPressed = inputService != null ? inputService.ConsumeCrouchPressed() : Input.GetKeyDown(KeyCode.LeftControl);
        bool crouchReleased = inputService != null ? inputService.ConsumeCrouchReleased() : Input.GetKeyUp(KeyCode.LeftControl);

        if (crouchPressed)
        {
            isCrouching = true;
            controller.height = crouchHeight;
        }
        else if (crouchReleased)
        {
            isCrouching = false;
            controller.height = originalHeight;
        }
    }

    private Vector2 GetMoveAxis()
    {
        if (inputService != null)
        {
            return inputService.MoveAxis;
        }

        return new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
    }

    private bool IsGamePaused()
    {
        if (gameStateService != null)
            return gameStateService.IsGamePaused;

        return GameStateManager.Instance != null && GameStateManager.Instance.IsGamePaused;
    }

    private void CacheServices()
    {
        if (inputService == null)
        {
            if (!GameServiceLocator.TryGet(out inputService))
            {
                inputService = FindObjectOfType<PlayerInputService>();
                if (inputService != null)
                {
                    GameServiceLocator.Register<IPlayerInputService>(inputService);
                }
            }
        }

        if (gameStateService == null)
        {
            if (!GameServiceLocator.TryGet(out gameStateService))
            {
                gameStateService = FindObjectOfType<GameStateManager>();
                if (gameStateService != null)
                {
                    GameServiceLocator.Register<IGameStateService>(gameStateService);
                }
            }
        }
    }
}