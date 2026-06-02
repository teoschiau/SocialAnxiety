using UnityEngine;
using System.Collections;

public class RobotConversation : MonoBehaviour
{
    [Header("Identity")]
    [TextArea] public string myPersona = "You are random person walking down the street.";
    public AudioSource myAudioSource; 

    [Header("Interaction Settings")]
    [Range(0, 100)] public int chatProbability = 25;
    public bool invertYaxis = false;

    public bool IsInConversation { get; private set; } = false;
    public string NpcVoice { get; private set; } = "alloy";
    
    private int currentConversationDepth = 0;
    private bool hasContributedToMission = false;
    private readonly string[] availableVoices = new string[] { "alloy", "echo", "fable", "onyx", "nova", "shimmer" };

    private RobotMovement robotMovement;
    private Transform playerTransform;

    void Start()
    {
        robotMovement = GetComponent<RobotMovement>();
        if (myAudioSource == null) myAudioSource = GetComponent<AudioSource>();
        
        if (GameObject.FindGameObjectWithTag("Player"))
            playerTransform = GameObject.FindGameObjectWithTag("Player").transform;

        NpcVoice = availableVoices[Random.Range(0, availableVoices.Length)];
    }

    void Update()
    {
        if (IsInConversation && playerTransform != null)
        {
            RotateTowardsPlayer();
        }
    }

    public void RequestConversation()
    {
        if (IsInConversation) return;

        int roll = Random.Range(0, 100);
        if (roll < chatProbability)
        {
            StartConversation();
        }
        else
        {
            RefuseConversation();
        }
    }

    public void StartConversation()
    {
        Debug.Log($"{name}: Hello! I am listening.");
        currentConversationDepth = 0;
        IsInConversation = true;

        if (robotMovement != null) robotMovement.SetMovementLocked(true);
    }

    public void EndConversation()
    {
        Debug.Log($"{name}: Ending conversation.");
        if (VoiceInputManager.Instance != null)
        {
            VoiceInputManager.Instance.EndCurrentConversation();
        }
        
        currentConversationDepth = 0;
        IsInConversation = false;

        if (robotMovement != null) robotMovement.SetMovementLocked(false);
    }

    void RefuseConversation()
    {
        Debug.Log("Conversation Refused.");
    }

    public void RegisterReply(string replyText)
    {
        if (replyText.Contains("Goodbye, Player"))
        {
            EndConversation();
            return;
        }

        currentConversationDepth++;
        Debug.Log($"{name} conversation depth: {currentConversationDepth}/3");

        if (currentConversationDepth >= 3 && !hasContributedToMission)
        {
            hasContributedToMission = true;
            if (Street_MissionManager.Instance != null) 
            {
                Street_MissionManager.Instance.RegisterInteraction();
                Debug.Log("Mission Point Gained!");
            }
            if (Restaurant_LevelSetupManager.Instance != null)
            {
                Debug.Log("Mission POin Gained! Congrats!");
                Restaurant_LevelSetupManager.Instance.TriggerLevelComplete();
            }
            if (Bus_MissionManager.Instance != null)
            {
                Bus_MissionManager.Instance.RegisterInteraction();
                Debug.Log("Mission Point Gained!");
            }
        }
    }

    void RotateTowardsPlayer()
    {
        Vector3 direction;

        if (invertYaxis)
        {
            // Standard Unity "LookAt" behavior
            // Vector points FROM robot TO player
            // Use this if your model faces +Z (Forward)
            direction = (playerTransform.position - transform.position).normalized;
        }
        else
        {
            // Your Original Logic ("Backwards" LookAt)
            // Vector points FROM player TO robot
            // Use this if your model faces -Z (Backwards)
            direction = (transform.position - playerTransform.position).normalized;
        }

        direction.y = 0;

        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }
}