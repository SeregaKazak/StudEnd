using UnityEngine;
using StarterAssets;

/// <summary>
/// Контроллер анимации игрока. Управляет только анимацией на основе данных из FirstPersonController.
/// Движение обрабатывается FirstPersonController.
/// </summary>
public class PlayerAnimationController : MonoBehaviour
{
    [Header("Animator Settings")]
    [Tooltip("Ссылка на объект с Animator компонентом")]
    [SerializeField] private GameObject mainChar;
    
    [Header("Animator Damping")]
    [Tooltip("Время сглаживания для параметра Speed")]
    public float speedDampTime = 0.12f;
    [Tooltip("Время сглаживания для параметра Direction")]
    public float directionDampTime = 0.12f;

    [Header("Jump / Gravity")]
    [Tooltip("Порог вертикальной скорости, после которого считаем, что персонаж в падении")]
    public float fallTriggerVelocity = -2f;

    private Animator animator;
    private CharacterController controller;
    private StarterAssetsInputs input;
    private FirstPersonController firstPersonController;

    private bool wasGrounded = true;
    private bool wasJumpPressed = false; // отслеживаем предыдущее состояние прыжка
    private float currentDirection = 0f; // храним текущее направление, чтобы не сбрасывать резко

    private IGameStateService gameStateService;

    private void Start()
    {
        CacheServices();

        // Получаем компоненты
        controller = GetComponent<CharacterController>();
        if (controller == null)
        {
            Debug.LogError("CharacterController не найден на объекте.");
        }

        input = GetComponent<StarterAssetsInputs>();
        if (input == null)
        {
            Debug.LogError("StarterAssetsInputs не найден на объекте.");
        }

        firstPersonController = GetComponent<FirstPersonController>();
        if (firstPersonController == null)
        {
            Debug.LogError("FirstPersonController не найден на объекте.");
        }

        // Получаем Animator
        if (mainChar != null)
        {
            animator = mainChar.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Animator не найден на mainChar.");
            }
        }
        else
        {
            Debug.LogError("MainChar GameObject не назначен в скрипте PlayerAnimationController.");
        }
    }

    private void Update()
    {
        // Проверяем, не приостановлена ли игра
        if (IsGamePaused())
            return;

        if (animator == null || input == null || controller == null)
            return;

        // Получаем данные о движении из StarterAssetsInputs
        Vector2 moveInput = input.move;
        float horizontalInput = moveInput.x;
        float verticalInput = moveInput.y;

        // Вычисляем скорость для аниматора
        Vector3 inputVector = new Vector3(horizontalInput, 0f, verticalInput);
        float horizontalSpeed = inputVector.magnitude;
        float normalizedSpeed = Mathf.Clamp01(horizontalSpeed); // 0..1 для BlendTree

        // Вычисляем направление (угол относительно forward персонажа)
        float direction = currentDirection;
        if (horizontalSpeed > 0.01f)
        {
            // Вычисляем угол между forward персонажа и направлением движения
            Vector3 moveDirection = transform.forward * verticalInput + transform.right * horizontalInput;
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

        // Устанавливаем параметры движения в аниматоре
        animator.SetFloat("Speed", normalizedSpeed, speedDampTime, Time.deltaTime);
        animator.SetFloat("Direction", direction, directionDampTime, Time.deltaTime);

        // Получаем вертикальную скорость из CharacterController
        float verticalVelocity = controller.velocity.y;
        animator.SetFloat("VerticalVelocity", verticalVelocity);

        // Обработка состояния земли и прыжков
        bool isGrounded = controller.isGrounded;

        // Если только что приземлились
        if (!wasGrounded && isGrounded)
        {
            // Приземление
            animator.ResetTrigger("Falling");
            animator.SetTrigger("Land");
            animator.SetBool("IsGrounded", true);
            animator.SetBool("IsJumping", false);
        }
        else if (isGrounded)
        {
            // Проверяем, был ли нажат прыжок (только в момент нажатия, а не удержания)
            bool jumpPressed = input.jump && !wasJumpPressed;
            if (jumpPressed)
            {
                animator.SetBool("IsGrounded", false);
                animator.SetBool("IsJumping", true);
                animator.SetTrigger("JumpStart"); // переход в JumpBegin
            }
            else
            {
                animator.SetBool("IsJumping", false);
                animator.SetBool("IsGrounded", true);
            }
        }
        else
        {
            // В воздухе
            animator.SetBool("IsGrounded", false);

            // Если сильно падаем — триггер падения
            if (verticalVelocity < fallTriggerVelocity)
            {
                animator.SetTrigger("Falling");
            }
        }

        // Сохраняем текущее grounded состояние для следующего кадра
        wasGrounded = isGrounded;
        wasJumpPressed = input.jump;
    }

    private void OnEnable()
    {
        CacheServices();
    }

    private bool IsGamePaused()
    {
        if (gameStateService != null)
            return gameStateService.IsGamePaused;

        return GameStateManager.Instance != null && GameStateManager.Instance.IsGamePaused;
    }

    private void CacheServices()
    {
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

