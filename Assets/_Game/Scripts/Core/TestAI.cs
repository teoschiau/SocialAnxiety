using UnityEngine;

public class TestAI : MonoBehaviour
{
    void Start()
    {
        Debug.Log("Sending test message to OpenAI...");
        OpenAIService.Instance.SendToChatGPT("Hello! Say 'System Online' if you can hear me.", 
            (response) => Debug.Log($"<color=green>AI Responded:</color> {response}"),
            (error) => Debug.LogError(error)
        );
    }
}