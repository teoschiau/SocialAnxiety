using UnityEngine;
using TMPro;

public class Presentation_MissionManager : MonoBehaviour
{
    public static Presentation_MissionManager Instance;

    [Header("UI")]
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI objectiveText;

    // Internal State
    private float currentTime;
    private bool gameStarted = false;
    private bool levelComplete = false;

    // Slide Logic
    private int sentencesOnCurrentSlide = 0;
    private int currentSlideIndex = 0; // 0-based
    private int totalSlides = 0;

    void Awake() { Instance = this; }

    void Update()
    {
        if (!gameStarted || levelComplete) return;

        currentTime += Time.deltaTime;
        UpdateTimerUI();
    }

void OnDisable()
{
    if (VoiceInputManager.Instance != null) 
    {
        VoiceInputManager.Instance.StopPresentationListening();
    }
}

    public void GenerateSlideMission(int totalSlideCount)
    {
        totalSlides = totalSlideCount;
        
        // Reset State
        currentSlideIndex = 0;
        sentencesOnCurrentSlide = 0;
        currentTime = 0;
        
        levelComplete = false;
        gameStarted = true;
        
        UpdateObjectivesUI();
    }

    // Call this from VoiceInputManager when Whisper returns text!
    public void RegisterSpokenSentence()
    {
        if (!gameStarted || levelComplete) return;
        
        sentencesOnCurrentSlide++;
        
        // Logic: "At least one sentence" -> Advance immediately
        // if (sentencesOnCurrentSlide >= 1)
        // {
        //     AdvanceToNextSlide();
        // }
        
        UpdateObjectivesUI();
    }

    public void AdvanceToNextSlide()
    {
		if (!gameStarted || levelComplete) return;
		
		if (sentencesOnCurrentSlide >= 1)
		{
		
        // Check if we just finished the LAST slide
        if (currentSlideIndex >= totalSlides - 1)
        {
            Victory();
        }
        else
        {
            // Move to next slide
            currentSlideIndex++;
            sentencesOnCurrentSlide = 0; 
            
            // Tell the Setup Manager to physically flip the PDF page
            Presentation_LevelSetupManager.Instance.FlipToNextPage();
            
            Debug.Log($"Advanced to Slide {currentSlideIndex + 1}");
        }
		}
    }

    void Victory()
    {
        gameStarted = false;
        levelComplete = true;
        
        // Update text one last time to show full completion
        objectiveText.text = $"Successfully hold the presentation (at least 1 sentence per slide): <color=green>Complete ({totalSlides}/{totalSlides})</color>";
        
        // Notify the Setup Manager
        Presentation_LevelSetupManager.Instance.TriggerLevelComplete();
    }

    void UpdateTimerUI()
    {
        float curM = Mathf.FloorToInt(currentTime / 60);
        float curS = Mathf.FloorToInt(currentTime % 60);
        timerText.text = $"Time: {curM:00}:{curS:00}";
    }

    void UpdateObjectivesUI()
    {
        // "Successfully hold the presentation (at least 1 sentence per slide): Slide 1/x"
        objectiveText.text = $"Successfully hold the presentation (at least 1 sentence per slide): <color=yellow>Slide {currentSlideIndex + 1}/{totalSlides}</color>";
    }
}