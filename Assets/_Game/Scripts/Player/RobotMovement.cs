using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class RobotMovement : MonoBehaviour
{
    [Header("Components")]
    public Transform playerTransform;
    public NavMeshAgent navAgent;
    private IWalker walkerScript;
    
    // Reference to the brain
    private RobotConversation robotConversation;

    [Header("Bump Settings")]
    public float prepareDistance = 4.0f;
    public float triggerDistance = 1.5f;
    public float bumpCooldown = 5.0f;

    [Header("Anti-Glitch Settings")]
    public float minHeightY = 1.0f;
    public float maxSpeed = 0.9f;

    // Internal State
    private AudioClip cachedReaction;
    private bool isDownloading = false;
    private float lastBumpTime = -10f;
    private bool isMovementLocked = false;

    void Start()
    {
        // Auto-grab references
        robotConversation = GetComponent<RobotConversation>();
        walkerScript = GetComponent<IWalker>();
        // REMOVED: myRb = GetComponent<Rigidbody>();
        
        if (walkerScript == null) Debug.LogWarning($"{name} has no IWalker component!");
        
        if (playerTransform == null && GameObject.FindGameObjectWithTag("Player"))
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;
            
        // Ensure the agent respects the speed limit initially
        if (navAgent != null) navAgent.speed = maxSpeed; 
    }

    void Update()
    {
        EnforcePhysicsConstraints();

        if (playerTransform == null) return;

        // If conversation has locked us, do not process bump logic or movement
        if (isMovementLocked) return;

        float distance = Vector3.Distance(transform.position, playerTransform.position);

        // Pre-load logic
        if (distance < prepareDistance && distance > triggerDistance && cachedReaction == null && !isDownloading)
        {
            StartCoroutine(PreloadReaction());
        }

        // Bump logic
        if (distance <= triggerDistance)
        {
            TriggerBumpInteraction();
        }
    }

    public void SetMovementLocked(bool locked)
    {
        isMovementLocked = locked;
        
        if (navAgent != null)
        {
            navAgent.isStopped = locked;
            if (locked) navAgent.velocity = Vector3.zero;
        }

        if (walkerScript != null)
        {
            walkerScript.SetMovementEnabled(!locked);
        }
    }

    // --- UPDATED: No Rigidbody needed ---
    void EnforcePhysicsConstraints()
    {
        // 1. Height Constraint (Direct Transform modification)
        if (transform.position.y < minHeightY)
        {
            Vector3 fixedPos = transform.position;
            fixedPos.y = minHeightY;
            transform.position = fixedPos;
        }

        // 2. Speed Constraint (NavMeshAgent modification)
        // Instead of clamping Rigidbody velocity, we clamp the Agent's velocity
        if (navAgent != null && navAgent.isActiveAndEnabled)
        {
            // If the agent is moving faster than maxSpeed, clamp it
            if (navAgent.velocity.magnitude > maxSpeed)
            {
                navAgent.velocity = navAgent.velocity.normalized * maxSpeed;
            }
            
            // Redundancy: Ensure the agent's settings match the limit
            // (This prevents the pathfinder from trying to accelerate beyond this)
            if (navAgent.speed > maxSpeed) 
            {
                navAgent.speed = maxSpeed;
            }
        }
    }

    void TriggerBumpInteraction()
    {
        if (robotConversation != null && robotConversation.IsInConversation) return;
        
        if (Time.time < lastBumpTime + bumpCooldown) return;
        lastBumpTime = Time.time;

        StartCoroutine(PerformReactionSequence());
    }

    IEnumerator PerformReactionSequence()
    {
        if (walkerScript != null) walkerScript.SetMovementEnabled(false);

        if (cachedReaction != null && robotConversation != null && robotConversation.myAudioSource != null)
        {
            robotConversation.myAudioSource.clip = cachedReaction;
            robotConversation.myAudioSource.Play();
            cachedReaction = null;
        }

        yield return new WaitForSeconds(4.0f);

        if (robotConversation != null && !robotConversation.IsInConversation && walkerScript != null)
        {
            walkerScript.SetMovementEnabled(true);
        }
    }

    IEnumerator PreloadReaction()
    {
        isDownloading = true;
        if (VoiceInputManager.Instance != null && robotConversation != null)
        {
            string prompt = "Generate a short, natural reaction as if someone just bumped into you clumsily.";
            
            VoiceInputManager.Instance.GenerateAudioForCache(
                prompt, 
                robotConversation.myPersona, 
                robotConversation.NpcVoice, 
                (clip) => 
                {
                    if (this != null) { cachedReaction = clip; isDownloading = false; }
                }
            );
        }
        else 
        { 
            isDownloading = false; 
        }
        yield return null;
    }
}