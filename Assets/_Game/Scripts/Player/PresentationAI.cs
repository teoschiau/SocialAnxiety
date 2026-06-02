using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Text;

public class PresentationAI : MonoBehaviour
{
    public static PresentationAI Instance;

    [Header("Configuration")]
    public Presentation_CrowdManager crowdManager; 
    public float silenceTimeout = 10f; 

    // REMOVED: public AudioClip[] speakLouderClips; 

    // Internal Memory
    private StringBuilder fullPresentationTranscript = new StringBuilder();
    private float lastTimeSpoke;
    private bool isPresenting = false;
    private bool isGeneratingHeckle = false; // Prevent spamming API calls

    // Available voices for random audience members
    private readonly string[] audienceVoices = new string[] { "echo", "fable", "onyx" };

    void Awake() { Instance = this; }

    public void StartPresentation()
    {
        isPresenting = true;
        isGeneratingHeckle = false;
        fullPresentationTranscript.Clear();
        lastTimeSpoke = Time.time;
        
        if (VoiceInputManager.Instance != null)
            VoiceInputManager.Instance.StartPresentationListening();
        
        StartCoroutine(MonitorSilence());
    }

    public void ReceivePresenterText(string text, float volume)
    {
        Debug.Log($"Presenter said: {text} (vol: {volume})");
        if (!isPresenting) return;

        lastTimeSpoke = Time.time; // Reset silence timer
        fullPresentationTranscript.AppendLine(text);

        // Logic: Volume too low?
        if (volume < 0.01f) 
        {
            // We pass a specific context so the AI knows WHY it is yelling
            TriggerAIHeckle("The presenter is mumbling and speaking too quietly. Yell at them to speak up.");
        }
        else
        {
            // Only count mission progress if volume was okay
            if (Presentation_MissionManager.Instance != null)
                Presentation_MissionManager.Instance.RegisterSpokenSentence();

			AskQuestionBasedOnHistory();
        }
    }

    // --- NEW: AI GENERATED HECKLES ---

    void TriggerAIHeckle(string instructionForAI)
    {
        // Don't queue up multiple heckles at once, wait for the first one to finish
        if (isGeneratingHeckle) return; 
        isGeneratingHeckle = true;

        // 1. Pick a random audience member
        GameObject heckler = crowdManager.GetRandomActiveMember();
        if (heckler == null) 
        {
            isGeneratingHeckle = false;
            return;
        }

        Debug.Log("Generating AI Heckle...");

        // 2. Build Prompt
        string prompt = $"You are a rude audience member at a presentation.\n" +
                        $"Context: {instructionForAI}\n" +
                        $"Action: Generate a VERY SHORT (max 5 words) shout/complaint.\n" +
                        $"Do not be polite. Do not include 'Audience:' prefix.";

        // 3. Send to ChatGPT
        OpenAIService.Instance.SendToChatGPT(prompt, (heckleText) => 
        {
            Debug.Log($"Audience wants to say: {heckleText}");

            // 4. Send to TTS
            // Pick a random voice to make the crowd feel diverse
            string randomVoice = audienceVoices[Random.Range(0, audienceVoices.Length)];

            OpenAIService.Instance.GetTextToSpeech(heckleText, randomVoice, (audioClip) => 
            {
                // 5. Play Audio
                if (heckler != null)
                {
                    AudioSource src = heckler.GetComponent<AudioSource>();
                    if (src == null) src = heckler.AddComponent<AudioSource>();
                    
                    src.spatialBlend = 1.0f; // Make it 3D sound so we know who yelled
                    src.clip = audioClip;
                    src.Play();
                }
                
                // Allow new heckles now
                StartCoroutine(ResetHeckleCooldown());

            }, (e) => { Debug.LogError(e); isGeneratingHeckle = false; });

        }, (e) => { Debug.LogError(e); isGeneratingHeckle = false; });
    }
    
    // Tiny delay to prevent back-to-back overlaps
    IEnumerator ResetHeckleCooldown()
    {
        yield return new WaitForSeconds(10.0f); 
        isGeneratingHeckle = false;
    }

    // --- QUESTIONS ---

    public void AskQuestionBasedOnHistory()
    {
        if (fullPresentationTranscript.Length < 10) return; 
		if (fullPresentationTranscript.Length % 10 != 0) return; // Throttle: only every 10 sentences

        GameObject questioner = crowdManager.GetRandomActiveMember();
        if (questioner == null) return;

        string prompt = $"You are an audience member listening to: \n" +
                        $"'{fullPresentationTranscript.ToString()}'\n\n" +
                        $"Ask a single, short, relevant, and slightly challenging question.";

        OpenAIService.Instance.SendToChatGPT(prompt, (questionText) => 
        {
            string voice = "onyx"; 
            OpenAIService.Instance.GetTextToSpeech(questionText, voice, (audioClip) => 
            {
                AudioSource src = questioner.GetComponent<AudioSource>();
                if (src)
                {
                    src.clip = audioClip;
                    src.Play();
                    Debug.Log($"Audience Question: {questionText}");
                }
            }, (e) => Debug.LogError(e));

        }, (e) => Debug.LogError(e));
    }

    IEnumerator MonitorSilence()
    {
        while (isPresenting)
        {
            if (Time.time > lastTimeSpoke + silenceTimeout)
            {
                // Trigger specific "Silence" complaint
                TriggerAIHeckle("The presenter has stopped talking and it is awkward. Yell at them to continue.");
                
                lastTimeSpoke = Time.time + 5f; // Add buffer
            }
            yield return new WaitForSeconds(1f);
        }
    }
}