using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class OpenAIService : MonoBehaviour
{
    [SerializeField] private APISettings apiSettings;

    // Singleton pattern so we can call this from anywhere
    public static OpenAIService Instance;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void SendToChatGPT(string userMessage, Action<string> onSuccess, Action<string> onError)
    {
        StartCoroutine(PostRequest(userMessage, onSuccess, onError));
    }
    
    public void SendAudioToWhisper(byte[] audioData, Action<string> onSuccess, Action<string> onError)
    {
        StartCoroutine(PostAudioRequest(audioData, onSuccess, onError));
    }

    private IEnumerator PostAudioRequest(byte[] audioData, Action<string> callback, Action<string> errorCallback)
    {
        string url = "https://api.openai.com/v1/audio/transcriptions";

        WWWForm form = new WWWForm();
        form.AddBinaryData("file", audioData, "recording.wav", "audio/wav");
        form.AddField("model", "whisper-1");

        using (UnityWebRequest request = UnityWebRequest.Post(url, form))
        {
            request.SetRequestHeader("Authorization", "Bearer " + apiSettings.openAIKey);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                errorCallback?.Invoke($"Whisper Error: {request.error}\n{request.downloadHandler.text}");
            }
            else
            {
                var response = JsonConvert.DeserializeObject<WhisperResponse>(request.downloadHandler.text);
                callback?.Invoke(response.text);
            }
        }
    }

    private class WhisperResponse
    {
        public string text;
    }

    private IEnumerator PostRequest(string message, Action<string> callback, Action<string> errorCallback)
    {
        string url = "https://api.openai.com/v1/chat/completions";

        // 1. Create the Request Body (JSON)
        var messageData = new
        {
            model = apiSettings.openAIModel,
            messages = new[]
            {
                new { role = "system", content = "You are a helpful assistant in a VR game." },
                new { role = "user", content = message }
            }
        };

        string jsonData = JsonConvert.SerializeObject(messageData);

        // 2. Setup the Web Request
        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            
            // Headers
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiSettings.openAIKey);

            // 3. Send
            yield return request.SendWebRequest();

            // 4. Handle Response
            if (request.result != UnityWebRequest.Result.Success)
            {
                errorCallback?.Invoke($"Error: {request.error}\nResponse: {request.downloadHandler.text}");
            }
            else
            {
                // Parse the JSON response to get just the text
                var responseRoot = JsonConvert.DeserializeObject<OpenAIResponse>(request.downloadHandler.text);
                string aiText = responseRoot.choices[0].message.content;
                callback?.Invoke(aiText);
            }
        }
    }

    // minimal classes to map the JSON response
    private class OpenAIResponse
    {
        public Choice[] choices;
    }
    private class Choice
    {
        public Message message;
    }
    private class Message
    {
        public string content;
    }
    
    public void GetTextToSpeech(string textToSpeak, string voice, Action<AudioClip> onSuccess, Action<string> onError)
    {
        StartCoroutine(PostTTSRequest(textToSpeak, voice, onSuccess, onError));
    }
    
    private IEnumerator PostTTSRequest(string text, string voice, Action<AudioClip> callback, Action<string> errorCallback) {
        string url = "https://api.openai.com/v1/audio/speech";

        var data = new
        {
            model = "tts-1",       // Standard model (fast)
            input = text,
            voice = voice
        };

        string jsonData = JsonConvert.SerializeObject(data);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            
            // This handler automatically converts the MP3 data to an AudioClip
            request.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.MPEG);

            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiSettings.openAIKey);

            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                errorCallback?.Invoke($"TTS Error: {request.error}");
            }
            else
            {
                AudioClip clip = DownloadHandlerAudioClip.GetContent(request);
                callback?.Invoke(clip);
            }
        }
    }
}