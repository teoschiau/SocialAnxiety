using UnityEngine;

public class SmoothHUD : MonoBehaviour
{
    [Header("Settings")]
    public Transform cameraTransform; // Drag your Main Camera here
    public float distance = 2.0f;     // How far in front?
    public float smoothSpeed = 5.0f;  // How fast it catches up

    void LateUpdate()
    {
        if (cameraTransform == null) return;

        // 1. Calculate where the HUD *should* be
        Vector3 targetPosition = cameraTransform.position + (cameraTransform.forward * distance);
        Quaternion targetRotation = cameraTransform.rotation;

        // 2. Smoothly move there (Lerp)
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * smoothSpeed);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * smoothSpeed);
    }
}