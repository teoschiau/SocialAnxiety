using UnityEngine;

public class RobotHeadAI : MonoBehaviour
{
    [Header("Setup")]
    public Transform player;                 // Drag your VR Camera here
    public ConfigurableJoint headJoint;      // Drag the Neck Joint here
    public Transform bodyTransform;          // Drag the Torso/Chest object here

    [Header("Settings")]
    public bool invertForward = false;       // Check this if he looks backwards
    public float lookSpeed = 10f;            // How fast the head tracks
    public float maxLookAngle = 85f;         // Max limit (cannot look behind shoulder)
    
    [Header("Calibration")]
    // If he looks slightly up/down/left/right by default, tweak these
    public Vector3 rotationOffset = Vector3.zero; 

    void Start()
    {
        // Auto-find body if empty
        if (bodyTransform == null) bodyTransform = transform;

        // Auto-find player
        if (player == null && GameObject.FindGameObjectWithTag("Player"))
            player = GameObject.FindGameObjectWithTag("Player").transform;

        // CRITICAL: Setup the Joint Drive automatically so it actually moves
        SetupJointPhysics();
    }

    void SetupJointPhysics()
    {
        // Unlock rotation so the script can control it
        headJoint.rotationDriveMode = RotationDriveMode.Slerp;
        
        // Lock angular X/Z in physics, we control them via TargetRotation
        // We actually want them 'Limited' or 'Locked' but driven by Slerp.
        // For pure script control, standard setup is fine.
        
        // Set Muscle Strength (Spring)
        JointDrive drive = new JointDrive();
        drive.positionSpring = 1000f; // Muscle strength
        drive.positionDamper = 50f;   // Resistance/Smoothing
        drive.maximumForce = 3.402823e+38f; // Infinite force allowed
        
        headJoint.slerpDrive = drive;
    }

    void FixedUpdate()
    {
        if (player == null) return;

        UpdateHeadTracking();
    }

    void UpdateHeadTracking()
    {
        // 1. Get vector from Head to Player
        Vector3 directionToPlayer = player.position - headJoint.transform.position;

        // 2. Convert this world vector into the Body's LOCAL space
        // This tells us: "Is the player to the left/right OF THE BODY?"
        Vector3 localDir = bodyTransform.InverseTransformDirection(directionToPlayer);

        // Handle the "Backwards Model" issue
        if (invertForward) localDir = -localDir;

        // 3. Calculate Angles from the local vector
        // YAW (Left/Right) is Atan2 of X and Z
        float yaw = Mathf.Atan2(localDir.x, localDir.z) * Mathf.Rad2Deg;
        
        // PITCH (Up/Down) is Atan2 of Y and Distance(X,Z)
        float horizontalDistance = new Vector2(localDir.x, localDir.z).magnitude;
        float pitch = Mathf.Atan2(localDir.y, horizontalDistance) * Mathf.Rad2Deg;

        // 4. THE "BEHIND" CHECK
        // If the player is outside the max angle (e.g. > 85 degrees left or right),
        // we force the target to be 0 (Forward).
        if (Mathf.Abs(yaw) > maxLookAngle)
        {
            // Smoothly return to center
            RotateHead(0, 0); 
        }
        else
        {
            // Look at player
            RotateHead(pitch, yaw);
        }
    }

    void RotateHead(float targetPitch, float targetYaw)
    {
        // Clamp pitch so he doesn't break his neck looking straight up
        targetPitch = Mathf.Clamp(targetPitch, -60f, 60f);

        // Create the rotation Quaternion
        // NOTE: ConfigurableJoint targetRotation is often inverted.
        // We use Euler(Pitch, -Yaw, 0) or (-Pitch, -Yaw, 0) depending on the joint setup.
        // Standard Unity joints usually need NEGATIVE Yaw to turn Right.
        
        Quaternion desiredRotation = Quaternion.Euler(
            -targetPitch + rotationOffset.x, 
            -targetYaw + rotationOffset.y, 
            rotationOffset.z
        );

        // Smoothly Interpolate for natural movement
        headJoint.targetRotation = Quaternion.Slerp(
            headJoint.targetRotation, 
            desiredRotation, 
            Time.fixedDeltaTime * lookSpeed
        );
    }
}