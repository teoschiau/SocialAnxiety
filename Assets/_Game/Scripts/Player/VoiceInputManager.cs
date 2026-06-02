using UnityEngine;
using System;
using System.IO;
using System.Text; 
using System.Collections;

public enum InputMode { Conversation, Presentation }

public class VoiceInputManager : MonoBehaviour
{
    public static VoiceInputManager Instance; 
    public InputMode currentMode = InputMode.Conversation;
    
    [Header("Continuous Settings")]
    public float chunkTime = 5.0f; // How often we send audio (seconds)
    public int micFrequency = 16000; // Lower freq usually fine for Whisper & faster
    
    [Tooltip("Minimum volume (0.0 to 1.0) required to send to AI. 0.01 is usually a whisper.")]
    public float voiceDetectionThreshold = 0.0075f; 
    
    // Internal State
    // CHANGED: Now references the Conversation script, not the old Controller
    private RobotConversation activeNPC; 
    
    private AudioClip micBuffer; // Rolling buffer
    private string micDevice;
    private bool isListening = false;
    private Coroutine listeningCoroutine;
    
    // Memory 
    private string npcVoice = "alloy"; // Default voice for TTS
    private StringBuilder conversationHistory = new StringBuilder();

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (Microphone.devices.Length > 0) micDevice = Microphone.devices[0];
    }
    
    public bool IsInConversation()
    {
        return activeNPC != null;
    }

    // --- 1. INTERACTION BUTTON (Toggle) ---
    
    // CHANGED: Parameter type updated
    public void InteractWithRobot(RobotConversation robot)
    {
        // If talking to THIS robot -> End it
        if (activeNPC == robot)
        {
            EndCurrentConversation();
        }
        // If talking to NO ONE (or someone else) -> Start
        else 
        {
            StartConversation(robot);
        }
    }

    // --- 2. CONVERSATION MANAGEMENT ---

    private void StartConversation(RobotConversation robot)
    {
        if (activeNPC != null && activeNPC != robot) EndCurrentConversation();

        activeNPC = robot;
        
        // CHANGED: Accessing property NpcVoice instead of method getNpcVoice()
        npcVoice = activeNPC.NpcVoice;
        
        // Reset State
        conversationHistory.Clear();

        activeNPC.StartConversation();
        
        // Start the Loop
        StartContinuousListening();
    }

    public void EndCurrentConversation()
    {
        if (activeNPC == null) return;
        
        StopContinuousListening();
        
        // Tell the robot (if it didn't tell us first)
        RobotConversation tempRef = activeNPC;
        activeNPC = null; 
        
        // Ensure mic stops
        if (Microphone.IsRecording(micDevice)) Microphone.End(micDevice);
        
        // CHANGED: If the robot is still in "conversation mode", tell it to stop
        if(tempRef.IsInConversation)
        {
             tempRef.EndConversation();
        }

        Debug.Log($"Conversation ended with {tempRef.name}");
    }

    // --- 3. CONTINUOUS LISTENING LOOP ---

    private void StartContinuousListening()
    {
        if (isListening || micDevice == null) return;
        isListening = true;

        // Start Mic (Looping clip, 20s buffer is enough since we slice every 5s)
        micBuffer = Microphone.Start(micDevice, true, 20, micFrequency); 
        
        listeningCoroutine = StartCoroutine(ListeningRoutine());
    }

    private void StopContinuousListening()
    {
        isListening = false;
        if (listeningCoroutine != null) StopCoroutine(listeningCoroutine);
        Microphone.End(micDevice);
    }

    IEnumerator ListeningRoutine()
    {
        Debug.Log("Continuous Listening Started...");

        while (isListening)
        {
            yield return new WaitForSeconds(chunkTime);

            int samplesNeeded = (int)(chunkTime * micFrequency);
            int currentPos = Microphone.GetPosition(micDevice);
            
            float[] samples = new float[samplesNeeded];
            
            int startPos = currentPos - samplesNeeded;
            if(startPos < 0) startPos = 0; 

            if (micBuffer.GetData(samples, startPos)) 
            {
                ProcessAudioChunk(samples);
            }
        }
    }

// --- PRESENTATION CONTROL ---

public void StartPresentationListening()
{
    Debug.Log("Starting Presentation Mic...");
    currentMode = InputMode.Presentation;
    StartContinuousListening(); // This is the missing link!
}

public void StopPresentationListening()
{
    Debug.Log("Stopping Presentation Mic...");
    StopContinuousListening();
    currentMode = InputMode.Conversation; // Reset default
}

    // --- 4. PROCESSING LOGIC ---
    
    private float GetAverageVolume(float[] samples)
    {
        float sum = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            sum += samples[i] * samples[i]; 
        }
        return Mathf.Sqrt(sum / samples.Length); 
    }

    private void ProcessAudioChunk(float[] samples)
    {
        float avgVolume = GetAverageVolume(samples);
        
        // Debug log to help you find the right number
        // Debug.Log($"Microphone Level: {avgVolume}"); 

        if (avgVolume < voiceDetectionThreshold)
        {
            Debug.Log($"Ignored (Too Quiet: {avgVolume:F4})");
            return; 
        }

        byte[] wavData = EncodeSamplesAsWAV(samples, micFrequency, 1);

        OpenAIService.Instance.SendAudioToWhisper(wavData, (userText) => 
        {
            // Filter: If Whisper hears nothing/hallucinates simple noise
            if (string.IsNullOrWhiteSpace(userText) || userText.Length < 2) return; 

            Debug.Log($"Buffer Heard: {userText}");
            
            conversationHistory.AppendLine($"User: {userText}");
            
            if (currentMode == InputMode.Presentation)
            {
                // Just send the text to the Audience Manager
                if (PresentationAI.Instance != null)
    			{
        			PresentationAI.Instance.ReceivePresenterText(userText, GetAverageVolume(samples));
    			}
    			else
    			{
       				Debug.LogWarning("InputMode is Presentation, but PresentationAI instance is null! Switching back to Conversation.");
        			currentMode = InputMode.Conversation;
    			}
    			return;
            }

            // Prompt Construction
            string fullPrompt = $"{activeNPC.myPersona}\n\n" +
                                $"[HISTORY]\n{conversationHistory.ToString()}\n\n" +
                                $"[INSTRUCTION]\n" +
                                $"1. Reply to the User's last line naturally.\n" +
                                $"2. Do NOT start your line with 'You:', 'Me:', 'AI:', or 'Robot:'. Just write the spoken text.\n" +
                                $"3. Do NOT repeat what the user said.\n" +
                                $"4. If input is just noise, reply EXACTLY: [SILENCE]\n" +
                                $"5. If user says bye, include: Goodbye, Player";

            OpenAIService.Instance.SendToChatGPT(fullPrompt, (aiResponse) => 
            {
                // Debug.Log($"AI Generated: {aiResponse}");
                if (aiResponse.Contains("[SILENCE]")) 
                {
                    return; 
                }

                Debug.Log($"AI Reply: {aiResponse}");
                conversationHistory.AppendLine($"You: {aiResponse}");

                // Register logic (Mission check & End check)
                if (activeNPC != null) activeNPC.RegisterReply(aiResponse);

                // Speak 
                if (activeNPC != null)
                {
                    OpenAIService.Instance.GetTextToSpeech(aiResponse, npcVoice, (npcAudio) => 
                    {
                        if (activeNPC != null && activeNPC.myAudioSource != null)
                        {
                            activeNPC.myAudioSource.clip = npcAudio;
                            activeNPC.myAudioSource.Play();
                        }
                    }, 
                    (e) => Debug.LogError(e));
                }
            }, 
            (e) => Debug.LogError(e));
        }, 
        (e) => Debug.LogError("Whisper Err: " + e));
    }
    
    public void GenerateAudioForCache(string prompt, string persona, string voice, Action<AudioClip> onAudioReady)
    {
        string fullPrompt = $"{persona}\n\n[INSTRUCTION]: {prompt}. No actions/gestures in text.";
        
        OpenAIService.Instance.SendToChatGPT(fullPrompt, (aiText) => 
        {
            OpenAIService.Instance.GetTextToSpeech(aiText, voice, onAudioReady, (e) => Debug.LogError(e));
        }, (e) => Debug.LogError(e));
    }

    private byte[] EncodeSamplesAsWAV(float[] data, int frequency, int channels)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            BinaryWriter writer = new BinaryWriter(stream);
            short[] intData = new short[data.Length];
            for (int i = 0; i < data.Length; i++) intData[i] = (short)(data[i] * 32767);
            
            Byte[] bytesData = new Byte[intData.Length * 2];
            Buffer.BlockCopy(intData, 0, bytesData, 0, bytesData.Length);

            writer.Write(Encoding.UTF8.GetBytes("RIFF"));
            writer.Write(36 + bytesData.Length);
            writer.Write(Encoding.UTF8.GetBytes("WAVE"));
            writer.Write(Encoding.UTF8.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((ushort)1);
            writer.Write((ushort)channels);
            writer.Write(frequency);
            writer.Write(frequency * channels * 2);
            writer.Write((ushort)(channels * 2));
            writer.Write((ushort)16);
            writer.Write(Encoding.UTF8.GetBytes("data"));
            writer.Write(bytesData.Length);
            writer.Write(bytesData);
            return stream.ToArray();
        }
    }
}