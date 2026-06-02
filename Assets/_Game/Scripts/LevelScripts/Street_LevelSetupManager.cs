using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class Street_LevelSetupManager : MonoBehaviour
{
    public static Street_LevelSetupManager Instance;

    [Header("UI Panels")]
    public GameObject menuCanvas;
    public GameObject mainMenuPanel;
    public GameObject customSettingsPanel;
    public GameObject loadingScreenPanel;
    public GameObject gameOverPanel;

    [Header("Custom UI Elements")]
    public Slider populationSlider;
    public TextMeshProUGUI populationText;
    public Slider timeSlider;
    public TextMeshProUGUI timeText;

    [Header("External Managers")]
    public Street_CrowdManager crowdManager;
    public Street_MissionManager missionManager;
    
    [Header("Inactive Level Objects")]
    public GameObject cityscape; 
    public GameObject HUDCanvas; 

    [Header("Player Settings")]
    public Transform playerTransform; 
    public Transform startPoint;      
    public Transform endPoint;       

    // Internal Settings
    private int selectedPopulation;
    private float selectedTime;
    
    // New Internal Goal Settings
    private int targetInteractions;
    private float targetDistance;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
       cityscape.SetActive(false);
       ShowMainMenu();
       UpdateSliderTexts(); 
    }

    // --- PRESETS ---

    public void SelectEasy()
    {
        // 10 People, 5 Mins -> 2 Interactions, 300m Walk
        StartGameSequence(10, 5, 2, 300f); 
    }

    public void SelectMedium()
    {
        // 30 People, 10 Mins -> 5 Interactions, 600m Walk
        StartGameSequence(30, 10, 5, 600f); 
    }

    public void SelectHard()
    {
        // 60 People, 15 Mins -> 12 Interactions, 1000m Walk
        StartGameSequence(60, 15, 12, 1000f); 
    }

    // --- CUSTOM MODE ---

    public void SelectCustom()
    {
        mainMenuPanel.SetActive(false);
        customSettingsPanel.SetActive(true);
    }

    public void StartCustomGame()
    {
        int pop = (int)populationSlider.value;
        float time = timeSlider.value;

        // --- LOGIC: AUTO-CALCULATE GOALS BASED ON TIME ---
        // Example: 1 Interaction per minute, 25 meters per minute
        int calcInteractions = Mathf.Max(1, Mathf.FloorToInt(time * 0.9f)); 
        float calcDistance = time * 60.0f; 

        StartGameSequence(pop, time, calcInteractions, calcDistance);
    }

    // --- UI HELPERS ---

    public void BackToMain()
    {
        customSettingsPanel.SetActive(false);
        mainMenuPanel.SetActive(true);
    }

    public void UpdateSliderTexts()
    {
        populationText.text = "Population: " + populationSlider.value;
        timeText.text = "Time: " + timeSlider.value + " min";
    }
    
    public void ContinueToNextLevelFromGameOver() 
    {
       SceneManager.LoadScene("Level_Bus");
    }

    public void BackToMainMenuFromGameOver()
    {
       SceneManager.LoadScene("MainMenu");
    }

    // --- LOADING SEQUENCE ---
    
    // Updated signature to accept specific goals
    void StartGameSequence(int pop, float time, int interactions, float distance)
    {
        selectedPopulation = pop;
        selectedTime = time;
        targetInteractions = interactions;
        targetDistance = distance;

        StartCoroutine(LoadingRoutine());
    }

    public void TriggerLevelComplete() {
       menuCanvas.SetActive(true);
       gameOverPanel.SetActive(true);
       TeleportPlayerToEnd();
    }

    IEnumerator LoadingRoutine()
    {
        mainMenuPanel.SetActive(false);
        customSettingsPanel.SetActive(false);
        loadingScreenPanel.SetActive(true);

        yield return new WaitForSeconds(0.5f);

        // 1. Crowd
        Debug.Log($"Spawning {selectedPopulation} NPCs...");
        crowdManager.GenerateCrowd(selectedPopulation);
        
        // 2. Missions (Now passing specific targets)
        Debug.Log($"Setting Goals: {targetInteractions} Interactions, {targetDistance}m Walk.");
        missionManager.GenerateMissions(targetInteractions, targetDistance, selectedTime);
        
        // 3. Level Activation
        if (cityscape != null) cityscape.SetActive(true);

        yield return new WaitForSeconds(2.0f); 

        loadingScreenPanel.SetActive(false);
        menuCanvas.SetActive(false);
        
        TeleportPlayerToStart();
        
        if (HUDCanvas != null) HUDCanvas.SetActive(true);
    }

    void TeleportPlayerToStart()
    {
        if (playerTransform != null && startPoint != null)
        {
            CharacterController cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            playerTransform.position = startPoint.position;
            playerTransform.rotation = startPoint.rotation;
            if (cc != null) cc.enabled = true;
        }
    }

    void TeleportPlayerToEnd()
    {
        if (playerTransform != null && endPoint != null)
        {
            CharacterController cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            playerTransform.position = endPoint.position;
            playerTransform.rotation = endPoint.rotation;
            if (cc != null) cc.enabled = true;
        }
    }

    void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        customSettingsPanel.SetActive(false);
        loadingScreenPanel.SetActive(false);
        gameOverPanel.SetActive(false);
    }
}