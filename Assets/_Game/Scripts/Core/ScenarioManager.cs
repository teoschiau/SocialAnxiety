using UnityEngine;

public class ScenarioManager : MonoBehaviour
{
    // Singleton pattern: This allows any script to find the "Current Scenario" easily
    public static ScenarioManager Instance;

    [Header("Scenario Settings")]
    [Tooltip("Is the 'Anxiety Dial' turned up?")]
    public bool highAnxietyMode = false;

    [Header("AI Prompts")]
    [TextArea(5, 10)]
    public string easyModePrompt = "You are a helpful and kind character. Speak briefly.";

    [TextArea(5, 10)]
    public string hardModePrompt = "You are a rude and difficult character. Speak briefly.";

    private void Awake()
    {
        // Simple Singleton setup
        if (Instance == null) 
        {
            Instance = this;
        }
        else 
        {
            Destroy(gameObject); // Ensures only one manager exists per scene
        }
    }

    public string GetCurrentSystemPrompt()
    {
        return highAnxietyMode ? hardModePrompt : easyModePrompt;
    }
}