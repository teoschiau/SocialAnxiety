using UnityEngine;
using System.Collections;

public class Restaurant_WaiterController : MonoBehaviour
{
    [Header("Identity")]
    [TextArea] public string myPersona = "You are a waiter.";
    public AudioSource myAudioSource; 

    // Internal State
    public bool IsInConversation { get; private set; } = false;
    public string NpcVoice { get; private set; } = "onyx";
    
    // Logica de misiune (Copiat din RobotConversation)
    private int currentConversationDepth = 0;
    private bool hasCompletedMission = false; 
    
    private readonly string[] availableVoices = new string[] { "alloy", "echo", "fable", "onyx", "nova", "shimmer" };
    private Transform playerTransform;
    private Animator animator;

    void Start()
    {
        if (myAudioSource == null) myAudioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();
        
        // Gasim jucatorul (Tag Player obligatoriu!)
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj != null) playerTransform = playerObj.transform;

        NpcVoice = availableVoices[Random.Range(0, availableVoices.Length)];
    }

    void Update()
    {
        if (playerTransform != null) RotateTowardsPlayer();
    }

    // --- FUNCTIA CARE FACE LEGATURA CU MANAGERUL ---
    public void SetDifficultyPersona(int difficulty)
    {
        // Resetam progresul la inceputul rundei
        currentConversationDepth = 0;
        hasCompletedMission = false;

        // Convertim INT (dificultate) in STRING (Persona)
        switch (difficulty)
        {
            case 1: // Easy
                myPersona = "You are a polite waiter. You apologize for the mistake immediately.";
                break;
            case 2: // Medium
                myPersona = "You are a busy waiter. You think the customer is wrong but will double check.";
                break;
            case 3: // Hard
                myPersona = "You are an arrogant waiter. You never make mistakes. The customer is wrong.";
                break;
            default:
                myPersona = "You are a waiter.";
                break;
        }
        Debug.Log($"Waiter difficulty set to {difficulty}. Persona updated.");
    }

    // --- LOGICA DE CONVERSATIE ---

    public void RegisterReply(string replyText)
    {
        if(animator) animator.SetTrigger("Talk");

        if (replyText.Contains("Goodbye"))
        {
            EndConversation();
            return;
        }

        // Creștem progresul (Misiunea e direct aici, nu in alt manager)
        currentConversationDepth++;
        Debug.Log($"Conversation Depth: {currentConversationDepth}/3");

        // Conditia de Victorie: 3 Replici schimbate
        if (currentConversationDepth >= 3 && !hasCompletedMission)
        {
            hasCompletedMission = true;
            
            // Anuntam LevelManager ca am terminat
            if (Restaurant_LevelSetupManager.Instance != null)
            {
                Restaurant_LevelSetupManager.Instance.TriggerLevelComplete();
            }
        }
    }

    public void StartConversation()
    {
        IsInConversation = true;
    }

    public void EndConversation()
    {
        IsInConversation = false;
        if (VoiceInputManager.Instance != null) VoiceInputManager.Instance.EndCurrentConversation();
    }

    void RotateTowardsPlayer()
    {
        Vector3 direction = (playerTransform.position - transform.position).normalized;
        direction.y = 0;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 5f);
        }
    }
}