using OC;
using OpenAI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartGapScene : MonoBehaviour
{ 
    
     public static StartGapScene instance { get; set; }
    public List<string> episodeSceneNames = new List<string>();
    private Queue<string> episodeScenes;
    public AudioClip backgroundMusic; // Add this line to store the background music
    public AudioSource audioSource; // Add this line to handle playing the background music

    private void Start()
    {
          if (instance == null)
                instance = this;


        episodeScenes = new Queue<string>(episodeSceneNames);
        LoadRandomScene();

        if(backgroundMusic == null)
            Debug.LogWarning("No background music assigned!");
        else
            audioSource.clip = backgroundMusic;

        
        audioSource.loop = true;
        audioSource.Play();
    }

    private void OnEnable()
    {
        TimelineManager.OnEpisodeEnded += OnEpisodeEnded;
    }

    private void OnDisable()
    {
        TimelineManager.OnEpisodeEnded -= OnEpisodeEnded;
    }

    private void LoadRandomScene()
    {
        if (episodeScenes.Count == 0)
        {
            Debug.Log("No extra episodes. Repeating the last scene.");
            episodeScenes = new Queue<string>(episodeSceneNames); // Re-add the scene names
        }

        int randomIndex = Random.Range(0, episodeScenes.Count);
        string randomSceneName = episodeScenes.Dequeue();


        StartCoroutine(LoadSceneAsync(randomSceneName));

     
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        while (!asyncOperation.isDone)
        {
            yield return null;
        }
        // Stop the background music when an episode starts
      
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(sceneName));
       

    }

    private void OnEpisodeEnded()
    {
        Debug.Log("Episode Ended");

        // Unload the current scene
        SceneManager.UnloadSceneAsync(SceneManager.GetActiveScene());

        OverCloud.timeOfDay.play = true;
        audioSource.clip = backgroundMusic;
        audioSource.loop = true;
        audioSource.Play();

        // Load a random scene from the list
        LoadRandomScene();
       // SpeechManager.instance.audioClips.Clear();
       // SpeechManager.instance.completedAudioClips = 0;


        ChatGPT.instance.GenerateAudioList();
    
    }


}

