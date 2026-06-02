using UnityEngine;
using TMPro;

public class Bus_MissionManager : MonoBehaviour
{
    public static Bus_MissionManager Instance;

    [Header("UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI objectiveText;

    // Internal State
    private float currentTime;
    private float requiredTime;
    private bool gameStarted = false;
    private bool levelComplete = false;

    // Mission Goals
    private int targetInteractions = 0;
    private int currentInteractions = 0;
    private bool hasReachedExit = false;

    void Awake() { Instance = this; }

    void Update()
    {
        if (!gameStarted || levelComplete) return;

        currentTime += Time.deltaTime;
        UpdateTimerUI();
        CheckVictory();
    }

    public void GenerateBusMission(int interactions, float minutes)
    {
        currentInteractions = 0;
        targetInteractions = interactions;

        hasReachedExit = false;
        levelComplete = false;
        currentTime = 0;
        requiredTime = minutes * 60; // Minutes to Seconds

        gameStarted = true;
        UpdateObjectivesUI();
    }

    public void RegisterInteraction()
    {
        if (!gameStarted || levelComplete) return;

        currentInteractions++;
        UpdateObjectivesUI();
        CheckVictory();
    }

    public void RegisterReachedExit()
    {
        if (!gameStarted || levelComplete) return;

        hasReachedExit = true;
        UpdateObjectivesUI();
        CheckVictory();
    }

    void CheckVictory()
    {
        bool interactionsDone = currentInteractions >= targetInteractions;
        bool exitDone = hasReachedExit;
        bool timeDone = currentTime >= requiredTime;

        if (interactionsDone && exitDone && timeDone)
        {
            EndLevel();
        }
    }

    void EndLevel()
    {
        gameStarted = false;
        levelComplete = true;
        objectiveText.text = "<color=green>GOALS REACHED: YOU CAN EXIT NOW!</color>";
        BusLevelSetupManager.Instance.TriggerLevelComplete();
    }

    void UpdateTimerUI()
    {
        float curM = Mathf.FloorToInt(currentTime / 60);
        float curS = Mathf.FloorToInt(currentTime % 60);
        float reqM = Mathf.FloorToInt(requiredTime / 60);
        float reqS = Mathf.FloorToInt(requiredTime % 60);

        string color = currentTime >= requiredTime ? "<color=green>" : "<color=white>";
        timerText.text = $"{color}Time: {curM:00}:{curS:00} / {reqM:00}:{reqS:00}</color>";
    }

    void UpdateObjectivesUI()
    {
        string intColor = currentInteractions >= targetInteractions ? "<color=green>" : "<color=white>";
        string exitColor = hasReachedExit ? "<color=green>" : "<color=white>";

        objectiveText.text = $"<b>MISSION GOALS:</b>\n" +
                             $"{intColor}- Interact with People: {currentInteractions}/{targetInteractions}</color>\n" +
                             $"{exitColor}- Move to the middle exit door</color>";
    }
}