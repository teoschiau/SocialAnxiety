using UnityEngine;

[CreateAssetMenu(fileName = "APISettings", menuName = "AI/APISettings")]
public class APISettings : ScriptableObject
{
    [Header("OpenAI Settings")]
    [Tooltip("Your OpenAI Secret Key starting with sk-...")]
    public string openAIKey;
    public string openAIModel = "gpt-4o-mini"; // Cheaper and faster for testing

    [Header("ElevenLabs Settings (Optional for later)")]
    public string elevenLabsKey;
}