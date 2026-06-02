using UnityEngine;
using UnityEngine.InputSystem;

public class NPCInteractor : MonoBehaviour
{
    [Header("Input")]
    public InputActionProperty interactButton;

    [Header("Settings")]
    public float interactionRange = 3.0f;
    public LayerMask npcLayer;
    
    void OnEnable()
    {
        interactButton.action.Enable();
    }

    void OnDisable()
    {
        interactButton.action.Disable();
    }

    void Update()
    {
        if (interactButton.action.WasPressedThisFrame())
        {
            Debug.Log("Button PRESSED!");
            HandleInput();
        }
    }

    void HandleInput()
    {
        // 1. Try to find a robot nearby
        // CHANGED: We now look for RobotConversation, not RobotController
        RobotConversation nearbyRobot = FindNearbyRobot();

        if (nearbyRobot != null)
        {
            Debug.Log($"Input targeted: {nearbyRobot.name}");
            
            // NOTE: You may need to update VoiceInputManager to accept 'RobotConversation' 
            // instead of 'RobotController' in its method signature.
            VoiceInputManager.Instance.InteractWithRobot(nearbyRobot);
        }
        else
        {
            // 2. No robot nearby? 
            if (VoiceInputManager.Instance.IsInConversation())
            {
                Debug.Log("No target, ending current conversation.");
                VoiceInputManager.Instance.EndCurrentConversation();
            }
        }
    }

    // CHANGED: Return type is now RobotConversation
    RobotConversation FindNearbyRobot()
    {
        Collider[] hits = Physics.OverlapSphere(transform.position, interactionRange, npcLayer);
        RobotConversation closestRobot = null;
        float closestDist = float.MaxValue;

        foreach (var hit in hits)
        {
            // Search in parent (common) or children for the NEW script
            RobotConversation robot = hit.GetComponentInParent<RobotConversation>();
            if (robot == null) robot = hit.GetComponentInChildren<RobotConversation>();

            if (robot != null)
            {
                Debug.Log($"Found robot: {robot.name}");
                float dist = Vector3.Distance(transform.position, robot.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestRobot = robot;
                }
            }
        }
        return closestRobot;
    }
}