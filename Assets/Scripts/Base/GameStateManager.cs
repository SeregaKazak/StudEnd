using UnityEngine;

public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance { get; private set; }
    
    public bool IsGamePaused { get; private set; } = false;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    public void PauseGame()
    {
        IsGamePaused = true;
        Time.timeScale = 0f; // Останавливаем время игры
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    
    public void ResumeGame()
    {
        IsGamePaused = false;
        Time.timeScale = 1f; // Восстанавливаем нормальное время
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }
    
    public void TogglePause()
    {
        if (IsGamePaused)
            ResumeGame();
        else
            PauseGame();
    }
}
