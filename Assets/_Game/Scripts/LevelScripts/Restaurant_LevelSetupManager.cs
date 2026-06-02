using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class Restaurant_LevelSetupManager : MonoBehaviour
{
    public static Restaurant_LevelSetupManager Instance;

    [Header("UI Panels")]
    public GameObject menuCanvas;
    public GameObject mainMenuPanel;
    public GameObject customSettingsPanel;
    public GameObject loadingScreenPanel;
    public GameObject gameOverPanel;

    [Header("UI HUD (In Game)")]
    public GameObject HUDCanvas; 

    [Header("Custom UI Elements")]
    public Slider populationSlider; 
    public TextMeshProUGUI populationText;
    public Slider timeSlider;       
    public TextMeshProUGUI timeText;

    [Header("Managers")]
    public Restaurant_CrowdManager crowdManager; 
    public Restaurant_MissionManager missionManager; 

    [Header("Setup Scena")]
    public GameObject restaurantEnvironment; 
    public GameObject crowdObject;
    public Transform playerTransform; 
    public Transform playerChair;
    private int selectedAudienceSize;
    private float selectedTime;
    private int selectedDifficulty;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
       if(restaurantEnvironment) restaurantEnvironment.SetActive(false);
       if(HUDCanvas) HUDCanvas.SetActive(false);
       
       ShowMainMenu();
       UpdateSliderTexts(); 
    }
    
    public void SelectEasy() 
    { 
        // 1 Minute, Easy Persona
        StartGameSequence(8, 1.0f, 1); 
    }

    public void SelectMedium() 
    { 
        // 2 Minutes, Medium Persona
        StartGameSequence(18, 2.0f, 2); 
    }

    public void SelectHard() 
    { 
        // 3 Minutes, Hard Persona
        StartGameSequence(33, 3.0f, 3); 
    }
    
    public void SelectCustom() { 
        mainMenuPanel.SetActive(false); 
        customSettingsPanel.SetActive(true); 
    }
    
   public void StartCustomGame()
    {
        int pop = (int)populationSlider.value;
        float time = timeSlider.value;
        
        int diff = 2; 

        StartGameSequence(pop, time, diff);
    }
    public void TriggerLevelComplete()
    {
        Debug.Log("Robot signaled victory! Updating Mission Manager...");
        if(missionManager != null)
        {
            missionManager.OnWaiterConvinced();
        }
    }
    public void ShowGameOver()
    {
        StartCoroutine(ShowWinScreen());
    }

    IEnumerator ShowWinScreen()
    {
        yield return new WaitForSeconds(0.30f);
        crowdManager.friendObject.SetActive(false);
        crowdManager.waiterObject.SetActive(false);
        crowdObject.SetActive(false);
        restaurantEnvironment.SetActive(false);
        HUDCanvas.SetActive(false);
        menuCanvas.SetActive(true);
        gameOverPanel.SetActive(true);
    }

    void StartGameSequence(int crowdSize, float time, int difficulty)
    {
        selectedAudienceSize = crowdSize;
        selectedTime = time;
        selectedDifficulty = difficulty;
        
        StartCoroutine(LoadingRoutine());
    }

    IEnumerator LoadingRoutine()
    {
        mainMenuPanel.SetActive(false);
        customSettingsPanel.SetActive(false);
        loadingScreenPanel.SetActive(true);

        yield return new WaitForSeconds(0.5f);

        if (restaurantEnvironment != null) restaurantEnvironment.SetActive(true);
        if (crowdObject != null) crowdObject.SetActive(true);
        
        if (crowdManager != null) 
            crowdManager.SetupScene(selectedDifficulty, selectedAudienceSize > 5); // Simple logic for crowded bool

        if (missionManager != null)
            missionManager.StartMission(selectedTime);

        yield return new WaitForSeconds(1.5f); 

        loadingScreenPanel.SetActive(false);
        menuCanvas.SetActive(false);
        
        TeleportPlayerToChair();

        if (HUDCanvas != null) HUDCanvas.SetActive(true);
    }

    void TeleportPlayerToChair()
    {
        if (playerTransform != null && playerChair != null)
        {
            CharacterController cc = playerTransform.GetComponent<CharacterController>();
            if (cc != null) cc.enabled = false;
            playerTransform.position = playerChair.position;
            playerTransform.rotation = playerChair.rotation;
            if (cc != null) cc.enabled = true;
        }
    }
    
    public void BackToMain() 
    { 
        customSettingsPanel.SetActive(false); 
        mainMenuPanel.SetActive(true); 
    }
    
    public void UpdateSliderTexts() 
    {
        if(populationText) populationText.text = "Audience: " + populationSlider.value;
        if(timeText) timeText.text = "Duration: " + timeSlider.value + " min";
    }
    
    public void BackToMainMenuFromGameOver() 
    { 
        SceneManager.LoadScene("MainMenu"); 
    }
    public void ContinueToNextLevelFromGameOver() 
    {
        SceneManager.LoadScene("Level_Presentation");
    }
    public void ShowMainMenu() 
    {
        mainMenuPanel.SetActive(true);
        customSettingsPanel.SetActive(false);
        loadingScreenPanel.SetActive(false);
        gameOverPanel.SetActive(false);
    }
}