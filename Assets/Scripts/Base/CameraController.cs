using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float sensitivity = 2.0f;
    public float maxYAngle = 80.0f;

    public float rotationX = 0.0f;

    private IPlayerInputService inputService;
    private IGameStateService gameStateService;

    void Awake()
    {
        CacheServices();
    }

    void OnEnable()
    {
        CacheServices();
    }

    // Update is called once per frame
    void Update()
    {
        // Проверяем, не приостановлена ли игра
        if (IsGamePaused())
            return;

        Vector2 look = inputService != null
            ? inputService.LookDelta
            : new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));

        float mouseX = look.x;
        float mouseY = look.y;

        transform.parent.Rotate(Vector3.up * mouseX * sensitivity);

        rotationX -= mouseY * sensitivity;
        rotationX = Mathf.Clamp(rotationX, -maxYAngle, maxYAngle);
        transform.localRotation = Quaternion.Euler(rotationX, transform.localEulerAngles.y, 0.0f);

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

    private bool IsGamePaused()
    {
        if (gameStateService != null)
            return gameStateService.IsGamePaused;

        return GameStateManager.Instance != null && GameStateManager.Instance.IsGamePaused;
    }

}