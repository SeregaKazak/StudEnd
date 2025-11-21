/// <summary>
/// Абстракция управления игровым состоянием (пауза, курсор, время).
/// Позволяет подменять реализацию и соблюдать DIP.
/// </summary>
public interface IGameStateService
{
    bool IsGamePaused { get; }

    void PauseGame();
    void ResumeGame();
    void TogglePause();
}


