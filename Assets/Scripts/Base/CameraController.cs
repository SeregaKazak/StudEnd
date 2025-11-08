using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float sensitivity = 2.0f;
    public float maxYAngle = 80.0f;

    public float rotationX = 0.0f;

    // Update is called once per frame
    void Update()
    {
        // Проверяем, не приостановлена ли игра
        if (GameStateManager.Instance != null && GameStateManager.Instance.IsGamePaused)
            return;
            
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        transform.parent.Rotate(Vector3.up * mouseX * sensitivity);

        rotationX -= mouseY * sensitivity;
        rotationX = Mathf.Clamp(rotationX, -maxYAngle, maxYAngle);
        transform.localRotation = Quaternion.Euler(rotationX, transform.localEulerAngles.y, 0.0f);

    }

}