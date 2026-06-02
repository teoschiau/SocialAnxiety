using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityPdfViewer; 

public class Presentation_LevelSetupManager : MonoBehaviour
{
    public static Presentation_LevelSetupManager Instance;

    [Header("UI Panels")]
    public GameObject menuCanvas;
    public GameObject mainMenuPanel;
    public GameObject customSettingsPanel;
    public GameObject loadingScreenPanel;
    public GameObject gameOverPanel;

    [Header("UI Elements")]
    public Slider populationSlider; 
    public TextMeshProUGUI populationText;
	public TMP_Dropdown pdfDropdown;

    [Header("Managers")]
    public Presentation_CrowdManager crowdManager;
    public Presentation_MissionManager missionManager;
    
    [Header("Environment")]
    public GameObject stageEnvironment;
    public GameObject crowdObject;
    public GameObject HUDCanvas; 
    public Transform playerTransform; 
    public Transform startPoint;
	public Transform endPoint;

    // Components
    private PdfViewerUI pdfViewerUI;

    // Settings
    private int selectedAudienceSize;
    private string selectedPdfFile; 

	private readonly string[] availablePdfs = new string[] 
    { 
        "easy.pdf",   // Index 0 in Dropdown
        "medium.pdf", // Index 1 in Dropdown
        "hard.pdf"    // Index 2 in Dropdown
    };

    void Awake() { Instance = this; }

    void Start()
    {
        pdfViewerUI = GetComponent<PdfViewerUI>();
        
        if(stageEnvironment) stageEnvironment.SetActive(false);
        ShowMainMenu();
        UpdateSliderTexts(); 
    }

    // --- PRESETS ---

    public void SelectEasy()
    {
        // Easy: Small Crowd, Easy PDF
        StartGameSequence(5, "easy.pdf"); 
    }

    public void SelectMedium()
    {
        // Medium: Medium Crowd, Medium PDF
        StartGameSequence(20, "medium.pdf"); 
    }

    public void SelectHard()
    {
        // Hard: Big Crowd, Hard PDF
        StartGameSequence(50, "hard.pdf"); 
    }

    // --- CUSTOM ---

    public void SelectCustom()
    {
        mainMenuPanel.SetActive(false);
        customSettingsPanel.SetActive(true);
    }

    public void StartCustomGame()
    {
        int pop = (int)populationSlider.value;
		int index = pdfDropdown.value; 
        
        // Safety check to ensure index is valid
        string fileToLoad = "medium.pdf"; // Default fallback
        if (index >= 0 && index < availablePdfs.Length)
        {
            fileToLoad = availablePdfs[index];
        }

        StartGameSequence(pop, fileToLoad);
    }

    // --- LOGIC ---

    void StartGameSequence(int audienceSize, string pdfFileName)
    {
        selectedAudienceSize = audienceSize;
        selectedPdfFile = pdfFileName;

        StartCoroutine(LoadingRoutine());
    }

    IEnumerator LoadingRoutine()
    {
        mainMenuPanel.SetActive(false);
        customSettingsPanel.SetActive(false);
        loadingScreenPanel.SetActive(true);

        yield return new WaitForSeconds(0.5f);

        // 1. Activate Environment
        if (stageEnvironment != null) stageEnvironment.SetActive(true);
        if (crowdObject != null) crowdObject.SetActive(true);

        // 2. Setup Audience
        crowdManager.SetupAudience(selectedAudienceSize);
        
        // 3. Load the Specific PDF
        if (pdfViewerUI != null)
        {
            Debug.Log($"Loading Document: {selectedPdfFile}");
            pdfViewerUI.LoadPDF(selectedPdfFile); // Load the file
            yield return null; // Wait a frame for load
            pdfViewerUI.GoToPage(0); // Reset to start
        }

        // 4. Get total pages
        int totalPages = 1; 
        if(pdfViewerUI != null) totalPages = pdfViewerUI.navigator.TotalPages; 

        // 5. Setup Mission (Just passes Total Pages now)
        missionManager.GenerateSlideMission(totalPages);

		if (PresentationAI.Instance != null)
    	{
        	PresentationAI.Instance.StartPresentation();
    	}
        
        yield return new WaitForSeconds(1.5f); 

        loadingScreenPanel.SetActive(false);
        menuCanvas.SetActive(false);
        
        TeleportPlayerToStart();
        
        if (HUDCanvas != null) HUDCanvas.SetActive(true);
    }
    
    // --- CALLED BY MISSION MANAGER ---
    
    public void FlipToNextPage()
    {
        if(pdfViewerUI != null)
        {
            pdfViewerUI.NextPage(); 
        }
    }

    public void TriggerLevelComplete() 
    {
       	menuCanvas.SetActive(true);
		gameOverPanel.SetActive(true);
		TeleportPlayerToEnd();
    }

    // --- UTILS ---

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

    public void BackToMain() { customSettingsPanel.SetActive(false); mainMenuPanel.SetActive(true); }
    
    public void UpdateSliderTexts() {
        populationText.text = "Audience: " + populationSlider.value;
    }
    
    public void BackToMainMenuFromGameOver() { SceneManager.LoadScene("MainMenu"); }
    
    public void ShowMainMenu() {
        mainMenuPanel.SetActive(true);
        customSettingsPanel.SetActive(false);
        loadingScreenPanel.SetActive(false);
        gameOverPanel.SetActive(false);
    }
}