using System;
using GoogleTextToSpeech.Scripts.Data;
using UnityEngine;
using Input = GoogleTextToSpeech.Scripts.Data.Input;
using System.Collections.Generic;

namespace GoogleTextToSpeech.Scripts
{
    public class TextToSpeech : MonoBehaviour
    {
        [SerializeField] private string apiKey;

        private Action<string> _actionRequestReceived;
        private Action<BadRequestData> _errorReceived;
        private Action<AudioClip> _audioClipReceived;

        private RequestService _requestService;
        private AudioConverter _audioConverter;
        public Action AllRequestsProcessed;
        private Queue<Action> _requestQueue = new Queue<Action>();
        private bool _isProcessing = false;

        private int totalRequests = 0;
        private int completedRequests = 0;

  public void GetSpeechAudioFromGoogle(string textToConvert, VoiceScriptableObject voice, Action<AudioClip> audioClipReceived,  Action<BadRequestData> errorReceived)
{

    totalRequests++;
    Debug.Log("GetSpeechAudioFromGoogle called with text: " + textToConvert);
    _audioConverter = gameObject.AddComponent<AudioConverter>();
    _actionRequestReceived = (requestData => RequestReceived(requestData, audioClipReceived));
    _requestQueue.Enqueue(() => ProcessRequest(textToConvert, voice, _actionRequestReceived, errorReceived));

    Debug.Log("Request enqueued. Queue count: " + _requestQueue.Count);

    if (!_isProcessing)
    {
        Debug.Log("Starting to process the queue");
        _isProcessing = true;
        ProcessNextRequest();
    }

         
       
}

private void ProcessRequest(string textToConvert, VoiceScriptableObject voice, Action<string> actionRequestReceived, Action<BadRequestData> errorReceived)
{
    if (_requestService == null)
        _requestService = gameObject.AddComponent<RequestService>();

    if (_audioConverter == null)
        _audioConverter = gameObject.AddComponent<AudioConverter>();

    var dataToSend = new DataToSend
    {
        input =
            new Input()
            {
                text = textToConvert
            },
        voice =
            new Voice()
            {
                languageCode = voice.languageCode,
                name = voice.name
            },
        audioConfig =
            new AudioConfig()
            {
                audioEncoding = "MP3",
                pitch = voice.pitch,
                speakingRate = voice.speed
            }
    };

    RequestService.SendDataToGoogle("https://texttospeech.googleapis.com/v1/text:synthesize", dataToSend,
        apiKey, actionRequestReceived, errorReceived);

        
}

private void ProcessNextRequest()
{
    if (_requestQueue.Count > 0)
    {
        var nextRequest = _requestQueue.Dequeue();
        nextRequest();
    }
    else if (completedRequests >= totalRequests)
    {
        _isProcessing = false;
        AllRequestsProcessed?.Invoke();
    }
}

private void RequestReceived(string requestData, Action<AudioClip> audioClipReceived)
{
    completedRequests++;
    Debug.Log("RequestReceived called with data: " + requestData);
    var audioData = JsonUtility.FromJson<AudioData>(requestData);
    AudioConverter.SaveTextToMp3(audioData);
    _audioConverter.LoadClipFromMp3((audioClip) => {
    // This code will be executed after the audio clip is loaded
    // You can use the loaded audioClip here
        OpenAI.ChatGPT.instance.audioClips.Add(audioClip);
        Debug.Log("Audio clip loaded");
        ProcessNextRequest();
    });

    // Process the next request in the queue
    
}


public void ResetRequests()
{
    totalRequests = 0;
    completedRequests = 0;
}
    }
}