using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class Street_MissionManager : MonoBehaviour
{
    public static Street_MissionManager Instance;

    [Header("UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI objectiveText;
    
    [Header("Tracking")]
    public Transform playerTransform;

    // Internal State
    private float currentTime;
    private float requiredTime;
    private bool gameStarted = false;
    private bool levelComplete = false;
    private Vector3 lastPlayerPos;

    // Mission Goals
    private int targetInteractions = 0;
    private int currentInteractions = 0;
    
    private float targetDistance = 0; 
    private float currentDistance = 0;

    void Awake() { Instance = this; }

    void Update()
    {
        if (!gameStarted || levelComplete) return;

        // 1. Timer
        currentTime += Time.deltaTime;
        UpdateTimerUI();

        // 2. Distance Logic
        if (targetDistance > 0 && playerTransform != null)
        {
            float dist = Vector3.Distance(playerTransform.position, lastPlayerPos);
            // Filter extremely small moves (jitter) or teleport jumps (>5.0f)
            if (dist > 0.01f && dist < 5.0f) 
            {
                currentDistance += dist;
            }
            lastPlayerPos = playerTransform.position;
        }

        CheckVictory();
        UpdateObjectivesUI();
    }

    // UPDATED: Now takes explicit goals
    public void GenerateMissions(int interactions, float distance, float minutes)
    {
        // Reset Progress
        currentInteractions = 0;
        currentDistance = 0;
        
        // Set Targets
        targetInteractions = interactions;
        targetDistance = distance;
        
        levelComplete = false;
        currentTime = 0;
        requiredTime = minutes * 60; // Minutes to Seconds
        
        if (playerTransform) lastPlayerPos = playerTransform.position;

        gameStarted = true;
        UpdateObjectivesUI();
    }
    
    public void RegisterInteraction()
    {
        if (!gameStarted) return;
        
        currentInteractions++;
        
        UpdateObjectivesUI();
        CheckVictory();
    }

    void CheckVictory()
    {
        bool interactionsDone = (targetInteractions == 0) || (currentInteractions >= targetInteractions);
        bool walkDone = (targetDistance == 0) || (currentDistance >= targetDistance);
        bool timeDone = currentTime >= requiredTime;

        if (interactionsDone && walkDone && timeDone)
        {
            EndGame();
        }
    }

    void EndGame()
    {
        gameStarted = false;
        levelComplete = true;
        objectiveText.text = "<color=green>LEVEL COMPLETE!</color>";
        
        Street_LevelSetupManager.Instance.TriggerLevelComplete();
    }

    void UpdateTimerUI()
    {
        float curM = Mathf.FloorToInt(currentTime / 60);
        float curS = Mathf.FloorToInt(currentTime % 60);
        
        float reqM = Mathf.FloorToInt(requiredTime / 60);
        float reqS = Mathf.FloorToInt(requiredTime % 60);

        string color = currentTime >= requiredTime ? "<color=green>" : "<color=white>";
        timerText.text = $"{color}{curM:00}:{curS:00} / {reqM:00}:{reqS:00}</color>";
    }

    void UpdateObjectivesUI()
    {
        string display = "<b>MISSION GOALS:</b>\n";

        // Interactions Status
        if (targetInteractions > 0)
        {
            string color = currentInteractions >= targetInteractions ? "<color=green>" : "<color=white>";
            display += $"{color}- Interact with People: {currentInteractions}/{targetInteractions}</color>\n";
        }

        // Distance Status
        if (targetDistance > 0)
        {
            string color = currentDistance >= targetDistance ? "<color=green>" : "<color=white>";
            display += $"{color}- Walk Distance: {currentDistance:F0}m / {targetDistance}m</color>\n";
        }

        objectiveText.text = display;
    }
}