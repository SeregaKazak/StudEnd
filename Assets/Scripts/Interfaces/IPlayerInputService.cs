using UnityEngine;

/// <summary>
/// Сервис ввода игрока. Инкапсулирует обращение к Unity Input и предоставляет
/// события/состояния для других компонентов.
/// </summary>
public interface IPlayerInputService
{
    Vector2 MoveAxis { get; }
    Vector2 LookDelta { get; }
    bool JumpPressedThisFrame { get; }
    bool IsCrouchHeld { get; }

    bool ConsumeToggleInventory();
    bool ConsumeInteract();
    bool ConsumeGrabToggle();
    bool ConsumeCrouchPressed();
    bool ConsumeCrouchReleased();
}


