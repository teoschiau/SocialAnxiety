using UnityEngine;

public class ParentHeadAI : MonoBehaviour
{
[Header("Setup")]
    public Transform player;                 // Drag VR Camera here
    public Transform headBone;               // Drag the NECK or HEAD bone here
    public Transform bodyTransform;          // Drag the ROBOT ROOT or CHEST here

    [Header("Settings")]
    public bool invertForward = false;       // Check if he looks away from you
    public float lookSpeed = 5f;             // Lower is smoother
    public float maxLookAngle = 85f;         // Neck limit (degrees)
    
    [Header("Corrections")]
    // If head is tilted wrong, change these (e.g. X=90 or Y=180)
    public Vector3 rotationOffset = Vector3.zero; 

    void Start()
    {
        // Auto-find body (Assumes this script is on the root)
        if (bodyTransform == null) bodyTransform = transform;

        // Auto-find player
        if (player == null)
        {
            GameObject p = GameObject.FindGameObjectWithTag("Player");
            if (p) player = p.transform;
        }
    }

    // LateUpdate runs AFTER the walking animation, ensuring the head overrides the animation
    void FixedUpdate()
    {
        if (player == null || headBone == null) return;

        UpdateHeadTracking();
    }

    void UpdateHeadTracking()
    {
        // 1. Get direction to player
        Vector3 directionToPlayer = player.position - headBone.position;

        // 2. Convert to Body's Local Space
        // This calculates the angle relative to where the chest is facing
        Vector3 localDir = bodyTransform.InverseTransformDirection(directionToPlayer);

        if (invertForward) localDir = -localDir;

        // 3. Calculate Angles
        // Yaw = Left/Right (Atan2 of X and Z)
        float yaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
        
        // Pitch = Up/Down (Atan2 of Y and Distance)
        float horizontalDistance = new Vector2(localDir.x, localDir.z).magnitude;
        float pitch = Mathf.Atan2(localDir.y, horizontalDistance) * Mathf.Rad2Deg;

        // 4. The "Owl Check" (Don't break neck)
        if (Mathf.Abs(yaw) > maxLookAngle)
        {
            // Player is behind -> Look forward (0,0)
            RotateHead(0, 0);
        }
        else
        {
            // Player is in front -> Look at player
            RotateHead(pitch, yaw);
        }
    }

    void RotateHead(float targetPitch, float targetYaw)
    {
        // Clamp Up/Down so he doesn't look at the sky too hard
        targetPitch = Mathf.Clamp(targetPitch, -50f, 50f);

        // Create Rotation
        // Most bones rotate Yaw on Y and Pitch on X. 
        // We add the offset at the end.
        Quaternion targetRot = Quaternion.Euler(targetPitch, targetYaw, 0);
        
        // Apply Offset (Important for models that are imported sideways)
        Quaternion offsetRot = Quaternion.Euler(rotationOffset);
        
        Quaternion finalRot = offsetRot * targetRot;

        // Apply Smoothly
        headBone.localRotation = Quaternion.Slerp(
            headBone.localRotation, 
            finalRot, 
            Time.deltaTime * lookSpeed
        );
    }
}
