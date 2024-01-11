using System;
using GoogleTextToSpeech.Scripts.Data;
using TMPro;
using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using SimpleJSON;
using CognitiveServicesTTS;
using static System.Net.Mime.MediaTypeNames;
using Unity.VisualScripting;
using UnityEngine.TextCore.Text;
using GoogleTextToSpeech.Scripts; // Add this line
using GoogleTextToSpeech.Scripts.Data;


namespace GoogleTextToSpeech.Scripts.Example
{
    public class TextToSpeechExample : MonoBehaviour
    {
        [SerializeField] private VoiceScriptableObject voice;
        [SerializeField] private TextToSpeech textToSpeech;
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private TextMeshProUGUI inputField;
        public List<AudioClip> audioClips = new List<AudioClip>();

        private Action<AudioClip> _audioClipReceived;
        private Action<BadRequestData> _errorReceived;
        
        public void PressBtn()
        {
            _errorReceived += ErrorReceived;
            _audioClipReceived += AudioClipReceived;
            textToSpeech.GetSpeechAudioFromGoogle(inputField.text, voice, _audioClipReceived, _errorReceived);
            
        }

        private void ErrorReceived(BadRequestData badRequestData)
        {
            Debug.Log($"Error {badRequestData.error.code} : {badRequestData.error.message}");
        }

        private void AudioClipReceived(AudioClip clip)
        {
            audioSource.Stop();
            audioSource.clip = clip;
            audioSource.Play();

            audioClips.Add(clip);
        }
    }
}
