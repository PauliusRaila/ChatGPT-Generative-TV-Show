using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CognitiveServicesTTS;
using System;
using System.Threading.Tasks;

public class UIManager : MonoBehaviour {

    public SpeechManager speech;
    public InputField input;
    public InputField pitch;
    public Toggle useSDK;
    public Dropdown voicelist;
    public GameObject shape;

    public static UIManager instance {get; set;}


    private void Start()
    {

        if(instance == null)
                instance = this;

        //pitch.text = "0";

        List<string> voices = new List<string>();
      //  foreach (VoiceName voice in Enum.GetValues(typeof(VoiceName)))
       // {
       //     voices.Add(voice.ToString());
       // }
       // voicelist.AddOptions(voices);
       // voicelist.value = (int)VoiceName.ltLTOnaNeural;
    }

    // The spinning cube is only used to verify that speech synthesis doesn't introduce
    // game loop blocking code.
    public void Update()
    {
        if (shape != null)
            shape.transform.Rotate(Vector3.up, 1);
    }



    /// <summary>
    /// Speech synthesis can be called via REST API or Speech Service SDK plugin for Unity
    /// </summary>
   

    public void ClearText()
    {
        input.text = "";
    }
}
