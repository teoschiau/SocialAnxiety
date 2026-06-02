using UnityEngine;
using TMPro;

public class Restaurant_MissionManager : MonoBehaviour
{
    public static Restaurant_MissionManager Instance;

    [Header("UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI objectiveText; 
    
    private float currentTime;
    private float requiredTime;
    private bool gameStarted = false;
    private bool levelComplete = false;
    
    private bool isWaiterConvinced = false;

    void Awake() { Instance = this; }

    void Update()
    {
        if (!gameStarted || levelComplete) return;

        currentTime += Time.deltaTime;
        UpdateTimerUI();

        CheckVictory();
    }
    
    public void StartMission(float minutes)
    {
        isWaiterConvinced = false;
        levelComplete = false;
        currentTime = 0;
        requiredTime = minutes * 60;
        gameStarted = true;
        
        UpdateObjectivesUI();
        UpdateTimerUI();
    }
    
    public void OnWaiterConvinced()
    {
        if (isWaiterConvinced) return;
        
        isWaiterConvinced = true;
        UpdateObjectivesUI();
    }

    void CheckVictory()
    {
        bool timeDone = currentTime >= requiredTime;

        if (isWaiterConvinced && timeDone)
        {
            EndGame();
        }
    }

    void EndGame()
    {
        gameStarted = false;
        levelComplete = true;
        
        objectiveText.text = "<color=green>MISSION ACCOMPLISHED!\nShift over.</color>";
        timerText.text = $"<color=green>Final Time: {FormatTime(currentTime)}</color>";
        
        if (Restaurant_LevelSetupManager.Instance != null)
        {
            Restaurant_LevelSetupManager.Instance.ShowGameOver();
        }
    }

    void UpdateTimerUI()
    {
        string color = currentTime >= requiredTime ? "<color=green>" : "<color=white>";
        timerText.text = $"{color}Time: {FormatTime(currentTime)} / {FormatTime(requiredTime)}</color>";
    }

    void UpdateObjectivesUI()
    {
        if (isWaiterConvinced)
        {
            objectiveText.text = "<color=green><b>SUCCESS:</b> Waiter convinced.\nWait for the order...</color>";
        }
        else
        {
            objectiveText.text = "<b>GOAL:</b> Convince the waiter that your order is wrong.";
        }
    }

    string FormatTime(float timeInSeconds)
    {
        float m = Mathf.FloorToInt(timeInSeconds / 60);
        float s = Mathf.FloorToInt(timeInSeconds % 60);
        return string.Format("{0:00}:{1:00}", m, s);
    }
}