using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;

public class BusLevelSetupManager : MonoBehaviour
{
    public static BusLevelSetupManager Instance;

    [Header("UI Panels")]
    public GameObject menuCanvas;
    public GameObject mainMenuPanel;
    public GameObject customSettingsPanel;
    public GameObject loadingScreenPanel;
    public GameObject gameOverPanel;

    [Header("UI Elements")]
    public Slider populationSlider;
    public TextMeshProUGUI populationText;
    public Slider timeSlider;
    public TextMeshProUGUI timeText;

    [Header("Managers")]
    public Bus_CrowdManager crowdManager;
    public Bus_MissionManager missionManager;

    [Header("Environment")]
    public GameObject busEnviroment;
    public GameObject crowdObject;
    public GameObject HUDCanvas;

    [Header("Player Settings")]
    public Transform playerTransform;
    public Transform startPoint;
    public Transform endPoint;

    // Settings
    private int selectedAudienceSize;
    private float selectedTime;

    private int targetInteractions;

    void Awake() { Instance = this; }

    void Start()
    {
        if (busEnviroment) busEnviroment.SetActive(false);
        ShowMainMenu();
        UpdateSliderTexts();
    }

    public void SelectEasy()
    {
        StartGameSequence(5, 1, 2f);
    }

    public void SelectMedium()
    {
        StartGameSequence(12, 3, 3.5f);
    }

    public void SelectHard()
    {
        StartGameSequence(25, 5, 5f);
    }


    public void SelectCustom()
    {
        mainMenuPanel.SetActive(false);
        customSettingsPanel.SetActive(true);
    }

    public void StartCustomGame()
    {
        int pop = (int)populationSlider.value;
        float time = timeSlider.value;
        int interactions = 5;

        StartGameSequence(pop,interactions, time);
    }


    void StartGameSequence(int audienceSize,int interactions, float time)
    {
        selectedAudienceSize = audienceSize;
        selectedTime = time;
        targetInteractions = interactions;


        StartCoroutine(LoadingRoutine());
    }

    IEnumerator LoadingRoutine()
    {
        mainMenuPanel.SetActive(false);
        customSettingsPanel.SetActive(false);
        loadingScreenPanel.SetActive(true);

        yield return new WaitForSeconds(0.5f);

        // 1. Activate Stage
        if (busEnviroment != null) busEnviroment.SetActive(true);
        if (crowdObject != null) crowdObject.SetActive(true);

        // 2. Setup Audience (No NavMesh, just toggling objects)
        Debug.Log($"Seating {selectedAudienceSize} audience members...");
        crowdManager.SetupAudience(selectedAudienceSize);

        // 3. Setup Mission
        if (missionManager != null)
        {
            Debug.Log($"Mission: {targetInteractions} interactions in {selectedTime} minutes.");
            missionManager.GenerateBusMission(targetInteractions, selectedTime);
        }
        yield return new WaitForSeconds(1.5f);

        loadingScreenPanel.SetActive(false);
        menuCanvas.SetActive(false);

        TeleportPlayerToStart();
        if (HUDCanvas != null) HUDCanvas.SetActive(true);
    }

    public void TriggerLevelComplete()
    {
        menuCanvas.SetActive(true);
        gameOverPanel.SetActive(true);
        TeleportPlayerToEnd();
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
    public void ContinueToNextLevelFromGameOver()
    {
        SceneManager.LoadScene("Level_Restaurant");
    }

    // --- UI HELPERS ---
    public void BackToMain() { customSettingsPanel.SetActive(false); mainMenuPanel.SetActive(true); }
    public void UpdateSliderTexts()
    {
        populationText.text = "Audience: " + populationSlider.value;
        timeText.text = "Duration: " + timeSlider.value + " min";
    }
    public void BackToMainMenuFromGameOver() { SceneManager.LoadScene("MainMenu"); }
    public void ShowMainMenu()
    {
        mainMenuPanel.SetActive(true);
        customSettingsPanel.SetActive(false);
        loadingScreenPanel.SetActive(false);
        gameOverPanel.SetActive(false);
    }
}
