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
using OC;


namespace OpenAI
{
    public class ChatGPT : MonoBehaviour
    {
        public static ChatGPT instance { get; set; }
        public TextToSpeech textToSpeech;
        private OpenAIApi openai = new OpenAIApi("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");
        public List<DialogueLine> dialogue = new List<DialogueLine>();
        [HideInInspector]
        public string episodeJSON = null;
        public List<AudioClip> audioClips = new List<AudioClip>();
        public VoiceScriptableObject voiceFemale;
        public VoiceScriptableObject voiceMale;
        [HideInInspector]
        public bool isFirstTime = true;
        public bool isEpisodeGenerated = false;
        public AudioSource audioSource = null;
        public string[] characterActions;
        private List<string> remainingActions;
        private void Start()
        {
            characterActions = new string[] { "sitOnChair1", "sitOnChair2", "lean1" , "lean2"};
            remainingActions = new List<string>(characterActions);
            if (instance == null) instance = this;
               

            // button.onClick.AddListener(GenerateEpisodePlot);
            if(isFirstTime)
                GenerateEpisodePlot();

            textToSpeech.AllRequestsProcessed += OnAllAudioClipsGenerated;

        }

        private async void GenerateEpisode(string episodePlot, int plotTotalTokens)
        {
            // List of prompts [NOT USING RIGHT NOW]
            List<string> prompts = new List<string>()
            {
                "Write a short dialogue with wordplay and double meanings that create funny misunderstandings.",
                "Create a short conversation with comical exaggerations and they engaging in playful banter.",
                "Write a short dialogue where they engage in a battle of wits, using witty comebacks and quick retorts.",
                "Create a short conversation where they use ironic statements and humorously misinterpret each other's words.",
                "Write a short dialogue with humorous self-deprecating humor where they poke fun at their own quirks.",
                "Create a short conversation where they exchange humorously teasing banter, using cultural references and inside jokes.",
                "Write a short dialogue where they use absurd logic and nonsensical arguments to make each other laugh.",
                "Create a short conversation where they share humorous stories.",  
                "Create a short conversation where they engage in a lighthearted debate, using humorously flawed reasoning and amusing counterarguments.",
                "Write a short dialogue where Tomas and Laura engage in a philosophical debate about the meaning of life, using humor and wit.",
                "Create a short conversation where Tomas and Laura discuss their most embarrassing moments, leading to laughter and deeper understanding of each other.",
                "Create a short conversation where Tomas and Laura discuss their dreams and aspirations, using metaphors and analogies in a humorous way.",
                "Write a short dialogue where Tomas and Laura debate about whether a glass is half full or half empty, using clever arguments and funny anecdotes.",
                "Create a short conversation where Tomas and Laura discuss the paradoxes of time travel, leading to humorous confusion and interesting theories.",
                "Write a short dialogue where Tomas and Laura engage in a playful argument about who is the better cook, using funny insults and exaggerated claims.",
                "Create a short conversation where Tomas and Laura discuss their interpretations of a famous piece of art, leading to deep insights and humorous misunderstandings."
            };

            // Randomly select a prompt from the list
            string randomPrompt = prompts[UnityEngine.Random.Range(0, prompts.Count)];

            List<ChatMessage> messages = new List<ChatMessage>()
            {
                new ChatMessage() { Role = "system", Content = "You are a comedy dialogue writer that only writes dialogues, and always return only with json, format " + System.IO.File.ReadAllText(@"getJSON_GPTprompt.txt")},
               // new ChatMessage() { Role = "system", Content = "IMPORTANT: You must write character spoken text in Lithuanian language."},
                //new ChatMessage() { Role = "system", Content = "VERY IMPORTANT: If no character action from the list fits the context of dialogue, make it 'noAction'. If no action fits at all? Adjust the dialogue so something fits. Action name are self explanatory!  "},
                new ChatMessage() { Role = "user", Content = "Write a short comedy dialogue between ONLY 2 characters Tomas and Laura for this plot - " + episodePlot + ". IMPORTANT: No alcohol, drugs or violence in the dialogue." +
                " Ensure the output is in JSON format according to " + System.IO.File.ReadAllText(@"getJSON_GPTprompt.txt") + " Respond back only with json code." }
            };



        // Complete the instruction
            var completionResponse = await openai.CreateChatCompletion(new CreateChatCompletionRequest()
            {
                Model = "gpt-3.5-turbo-0301",
                Temperature = 0.01f,
                Messages = messages
            });

            if (completionResponse.Choices != null && completionResponse.Choices.Count > 0)
            {
                var message = completionResponse.Choices[0].Message;
                message.Content = message.Content.Trim();

                messages.Add(message);
                Debug.Log(message.Content);
                Debug.Log("[DIALOGUE] Prompt Tokens: " + completionResponse.Usage.PromptTokens);
                Debug.Log("[DIALOGUE] Completion Tokens: " + completionResponse.Usage.CompletionTokens);
                Debug.Log("[DIALOGUE] Total Tokens: " + completionResponse.Usage.TotalTokens);
                Debug.Log("[TOTAL] Tokens: " + (int.Parse(completionResponse.Usage.TotalTokens) + plotTotalTokens));
                Debug.LogWarning(completionResponse.Choices[0].FinishReason);
                episodeJSON = message.Content;

                isEpisodeGenerated = true;

                if (isFirstTime)
                {

                    GenerateAudioList();
                    isFirstTime = false;

                }
            }
            else
            {
                Debug.LogWarning("No text was generated from this prompt.");
            }

            // button.enabled = true;

        }



        // High temperature prompt to create unique plot for episode.
        // Then with GenerateEpisode() low temperature prompt we generate episode dialogue.
        // Low temperature listens more to system role.
        public async void GenerateEpisodePlot()
        {
            isEpisodeGenerated = false;
            dialogue = new List<DialogueLine>();
            dialogue  = new List<OpenAI.ChatGPT.DialogueLine>();
            audioClips = new List<AudioClip>();
            episodeJSON = null;
         
            remainingActions = new List<string>(characterActions);


            List<string> prompts = new List<string>()
            {
                "Write a short plot for a comedy situation between Tomas and Laura, focusing on a dialogue-driven scenario involving wordplay and double meanings that create funny misunderstandings.",
                "Write a short plot for a comedy situation between Tomas and Laura, focusing on a dialogue-driven scenario with comical exaggerations and characters engaging in playful banter.",
                "Write a short plot for a comedy situation between Tomas and Laura, focusing on a dialogue-driven scenario where characters engage in a battle of wits, using witty comebacks and quick retorts.",
                "Write a short plot for a comedy situation between Tomas and Laura, focusing on a dialogue-driven scenario where characters use ironic statements and humorously misinterpret each other's words.",
                "Write a short plot for a comedy situation between Tomas and Laura, focusing on a dialogue-driven scenario with humorous self-deprecating humor where characters poke fun at their own quirks.",
                "Write a short plot for a comedy situation between Tomas and Laura, focusing on a dialogue-driven scenario where characters use absurd logic and nonsensical arguments to make each other laugh.",
                "Write a short plot for a comedy situation between Tomas and Laura, focusing on a dialogue-driven scenario where characters share humorous stories, each trying to outdo the other with a funnier tale.",
                "Write a short plot for a comedy situation between Tomas and Laura, focusing on a dialogue-driven scenario where characters engage in a lighthearted debate, using humorously flawed reasoning and amusing counterarguments.",
                

            };

            string selectedPrompt = prompts[UnityEngine.Random.Range(0, prompts.Count)];
            Debug.Log(selectedPrompt);
               List<ChatMessage> messages = new List<ChatMessage>()
            {
                new ChatMessage() { Role = "system", Content = "You are a comedy writer that writes short tv comedy episode plots, you hate writing dialogue and you dont write dialogue."},
                new ChatMessage() { Role = "system", Content = "IMPORTANT: You must not write dialogue! Just the plot, small description!"},
                new ChatMessage() { Role = "user", Content = selectedPrompt + "VERY IMPORTANT: Do not write dialogue! Make the plot very short. IMPORTANT: All action must happen in their apartment and be dialogue driven, no physical action. IMPORTANT: Alcohol must not be included! Respond back only with short plot! " }
            };


            var completionResponse = await openai.CreateChatCompletion(new CreateChatCompletionRequest()
            {

                Model = "gpt-3.5-turbo-0301",
                Temperature = 1.2f,
                MaxTokens = 200,
                Messages = messages
                
            });

            if (completionResponse.Choices != null && completionResponse.Choices.Count > 0)
            {

                var message = completionResponse.Choices[0].Message;
                message.Content = message.Content.Trim();

                messages.Add(message);
                Debug.Log(message.Content);
                Debug.Log("[PLOT] Total Tokens: " + completionResponse.Usage.TotalTokens);
                Debug.LogWarning(completionResponse.Choices[0].FinishReason);

                GenerateEpisode(message.Content, int.Parse(completionResponse.Usage.TotalTokens));

            }
            else
            {

                Debug.LogWarning("No text was generated from this prompt.");

            }
        } 







       public void GenerateAudioList()
       {
            if(!isEpisodeGenerated && !isFirstTime){

                Invoke("GenerateAudioList", 3);
                Debug.LogWarning("Episode not generated, Invoking again in 3.");
                return;
            }
         //
        // Parse the JSON string into a JSONNode object
    
       
        JSONNode jsonNode = JSON.Parse(episodeJSON);

        Debug.Log("Dialogues: " + jsonNode["dialogue"].AsArray.Count);
        // Iterate through the dialogue array and access values in each dialogue object
        foreach (JSONNode dialog in jsonNode["dialogue"].AsArray)
        {
            string speakerName = dialog["speaker"];
            string spokenText = dialog["spokenText"];
            bool laughTrack = bool.Parse(dialog["laughTrack"]);
            int index = dialogue.Count;  // Get the current index


            //string action = "sitOnChair1"; //dialog["action"];
            string action = GetUniqueAction();
            Debug.Log(action);

            dialogue.Add(new DialogueLine(index, speakerName, spokenText, laughTrack, action));  // Add the DialogueLine with index
            
            if(speakerName == "Tomas")
                textToSpeech.GetSpeechAudioFromGoogle(spokenText, voiceMale, AudioClipReceived, ErrorReceived);
            else if (speakerName == "Laura")
                textToSpeech.GetSpeechAudioFromGoogle(spokenText, voiceFemale, AudioClipReceived, ErrorReceived);

            Debug.Log(speakerName + ": " + spokenText);
        }
    }

        private void ErrorReceived(BadRequestData badRequestData)
        {
            Debug.Log($"Error {badRequestData.error.code} : {badRequestData.error.message}");
                 
        }

        private void AudioClipReceived(AudioClip clip)
        {
           // audioClips.Add(clip);

       
        }

private string GetUniqueAction()
{
    if (remainingActions.Count == 0)
    {
        // If all actions have been used, reset the list
        Debug.Log("No actions left!");
        return "";
        //remainingActions = new List<string>(characterActions);
    }

    // Select a random action
    int index = UnityEngine.Random.Range(0, remainingActions.Count);
    string selectedAction = remainingActions[index];

    // Remove the selected action from the list
    remainingActions.Remove(selectedAction);

    return selectedAction;
}


     private void OnAllAudioClipsGenerated()
    {

        audioSource.clip = null;
        audioSource.loop = false;
        OverCloud.timeOfDay.play = false;
        audioSource.Stop();
        TimelineManager.instance.CreateTimeline();
        // Do something after all audio clips have been generated
    }

   


    public class DialogueLine
    {
        public string SpeakerName { get; set; }
        public string SpokenText { get; set; }
        public bool LaughTrack { get; set; }
        public int Index { get; set; }  // New property to hold the index

        public string Action;

        public DialogueLine(int index, string speakerName, string spokenText, bool laughTrack, string action)
        {
            Index = index;
            SpeakerName = speakerName;
            SpokenText = spokenText;
            LaughTrack = laughTrack;
            Action = action;
        }
    }

    } 
}