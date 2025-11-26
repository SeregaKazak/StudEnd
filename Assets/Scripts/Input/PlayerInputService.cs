using UnityEngine;

/// <summary>
/// Центральный сервис обработки ввода. Соблюдает GRASP Controller, перенаправляя события
/// другим подсистемам через GameServiceLocator.
/// </summary>
public class PlayerInputService : MonoBehaviour, IPlayerInputService
{
    public Vector2 MoveAxis { get; private set; }
    public Vector2 LookDelta { get; private set; }
    public bool JumpPressedThisFrame => jumpPressed;
    public bool IsCrouchHeld => Input.GetKey(KeyCode.LeftControl);

    private bool jumpPressed;
    private bool toggleInventoryPressed;
    private bool interactPressed;
    private bool grabTogglePressed;
    private bool crouchPressed;
    private bool crouchReleased;

    private void Awake()
    {
        GameServiceLocator.Register<IPlayerInputService>(this);
    }

    private void OnDestroy()
    {
        GameServiceLocator.Unregister<IPlayerInputService>();
    }

    private void Update()
    {
        MoveAxis = new Vector2(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"));
        LookDelta = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        jumpPressed |= Input.GetButtonDown("Jump");

        if (Input.GetKeyDown(KeyCode.I))
            toggleInventoryPressed = true;

        if (Input.GetKeyDown(KeyCode.E))
            interactPressed = true;

        if (Input.GetKeyDown(KeyCode.F))
            grabTogglePressed = true;

        if (Input.GetKeyDown(KeyCode.LeftControl))
            crouchPressed = true;

        if (Input.GetKeyUp(KeyCode.LeftControl))
            crouchReleased = true;
    }

    private void LateUpdate()
    {
        jumpPressed = false;
        toggleInventoryPressed = false;
        interactPressed = false;
        grabTogglePressed = false;
        crouchPressed = false;
        crouchReleased = false;
    }

    public bool ConsumeToggleInventory()
    {
        bool result = toggleInventoryPressed;
        toggleInventoryPressed = false;
        return result;
    }

    public bool ConsumeInteract()
    {
        bool result = interactPressed;
        interactPressed = false;
        return result;
    }

    public bool ConsumeGrabToggle()
    {
        bool result = grabTogglePressed;
        grabTogglePressed = false;
        return result;
    }

    public bool ConsumeCrouchPressed()
    {
        bool result = crouchPressed;
        crouchPressed = false;
        return result;
    }

    public bool ConsumeCrouchReleased()
    {
        bool result = crouchReleased;
        crouchReleased = false;
        return result;
    }
}


