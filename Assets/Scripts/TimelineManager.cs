using System;
using System.Linq;
using System.Reflection;
using Tantawowa.Extensions;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using Random = UnityEngine.Random;
using OpenAI;
using Cinemachine;
using System.Collections.Generic;
using Tantawowa.TimelineEvents;
using UnityEngine.Audio;
using UnityEngine.Animations;
using UnityEditor.Animations;
using System.Collections;
using Invector.vCharacterController.AI;

public class TimelineManager : MonoBehaviour
{
    public static TimelineManager instance { get; set; }
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private AudioSource mainAudioSource;
    [SerializeField] private AudioSource laughAudioSource;
    [SerializeField] private List<AudioClip> laughTracks;
    [SerializeField] private float minDelay = 0.1f;
    [SerializeField] private float maxDelay = 0.4f;
    private Dictionary<CinemachineVirtualCamera, Transform> defaultCamTransforms;
    private List<CinemachineVirtualCamera> virtualCameras;
    public static event Action OnEpisodeEnded;
    private void Start()
{
    if (instance == null)
        instance = this;

    virtualCameras = new List<CinemachineVirtualCamera>(FindObjectsOfType<CinemachineVirtualCamera>());

    defaultCamTransforms = new Dictionary<CinemachineVirtualCamera, Transform>();

    CinemachineCore cmCore = CinemachineCore.Instance;
    for (int i = 0; i < cmCore.VirtualCameraCount; i++)
    {
        CinemachineVirtualCamera cam = cmCore.GetVirtualCamera(i) as CinemachineVirtualCamera;
        if (cam != null)
        {
            Transform defaultTransform = new GameObject("DefaultTransform").transform;
            defaultTransform.SetParent(transform);
            defaultTransform.position = cam.transform.position;
            defaultTransform.rotation = cam.transform.rotation;
            defaultCamTransforms.Add(cam, defaultTransform);
        }
    }

    
}

  

    public void FocusOnCharacter(string characterName, Action onSuccess = null)
    {
        GameObject character = GameObject.Find(characterName); //Might need later to do something with invector
        if (character == null)
        {
            Debug.LogWarning($"Character '{characterName}' not found.");
            return;
        }

        Transform headTransform = FindChildByName(character.transform, "Head");
        if (headTransform == null)
        {
            Debug.LogWarning($"Head GameObject not found in '{characterName}' hierarchy.");
            return;
        }

        List<CinemachineVirtualCamera> visibleCameras = new List<CinemachineVirtualCamera>();
        Collider[] colliders = headTransform.GetComponentsInChildren<BoxCollider>();//character.GetComponentsInChildren<Collider>();

        if (colliders.Length == 0)
        {
            Debug.LogWarning($"Character '{characterName}' has no colliders.");
            return;
        }

        CinemachineCore cmCore = CinemachineCore.Instance;
        Camera tempCamera = new GameObject("TempCamera").AddComponent<Camera>();
        tempCamera.transform.SetParent(transform);
        
        CinemachineVirtualCamera currentCam = null;

        for (int i = 0; i < cmCore.VirtualCameraCount; i++)
        {
            CinemachineVirtualCamera cam = cmCore.GetVirtualCamera(i) as CinemachineVirtualCamera;
            
            if (cam == null || cam.tag == "nochange") continue;

            if(cam.Priority == 2) { currentCam = cam; continue;}

            cam.Priority = 0;
            cam.LookAt = colliders[0].transform;
            cam.transform.position = defaultCamTransforms[cam].position;
            cam.transform.rotation = defaultCamTransforms[cam].rotation;

            Cinemachine.CameraState state = cam.State;
            tempCamera.fieldOfView = state.Lens.FieldOfView;
            tempCamera.aspect = state.Lens.Aspect;
            tempCamera.nearClipPlane = state.Lens.NearClipPlane;
            tempCamera.farClipPlane = state.Lens.FarClipPlane;
            tempCamera.transform.position = state.CorrectedPosition;
            tempCamera.transform.rotation = state.CorrectedOrientation;

         foreach (Collider collider in colliders)
{                          
    // Make the temporary camera look at the collider
    tempCamera.transform.LookAt(collider.transform);

    // Create a ray from the camera to the collider
    Ray ray = new Ray(tempCamera.transform.position, collider.transform.position - tempCamera.transform.position);
    RaycastHit hit;

    // Perform the sphere cast
    float sphereRadius = 0.5f; // Adjust this value as needed
    if (Physics.SphereCast(ray, sphereRadius, out hit))
    {
        // If the sphere cast hit the target collider, add the camera to the list
        if (hit.collider == collider)
        {
            visibleCameras.Add(cam);
            break;
        }

        Debug.Log(hit.collider.name);
    }
}
        }

        Destroy(tempCamera.gameObject);

        if (visibleCameras.Count > 0)
        {
            if(currentCam != null){
                currentCam.Priority = 0;
                currentCam = null;
            }
            

            CinemachineVirtualCamera selectedCamera = visibleCameras[UnityEngine.Random.Range(0, visibleCameras.Count)];
            selectedCamera.Priority = 2;
            selectedCamera.LookAt = headTransform; 

            if (onSuccess != null)
            {
                onSuccess.Invoke();
            }
        }
        else
        {
            Debug.LogWarning($"Character '{characterName}' is not visible by any virtual cameras.");

            if(currentCam == null)
            {
                cmCore.GetVirtualCamera(3).LookAt = headTransform;
            }
            else
                currentCam.LookAt = headTransform;
            
        }
    }

    public void FocusOnCharacter(string characterName)
    {
        FocusOnCharacter(characterName, null);
        
    }

    public void CreateTimeline()
    {
        // Create a new timeline asset
        TimelineAsset timeline = ScriptableObject.CreateInstance<TimelineAsset>();
        
        // Get the dialogue lines from the ChatGPT script
         List<OpenAI.ChatGPT.DialogueLine> dialogueLines = ChatGPT.instance.dialogue;



        // Initialize the current time
        double currentTime = 0;

        // Create an audio track for dialogue and laugh
        AudioTrack dialogueAudioTrack = timeline.CreateTrack<AudioTrack>(null, "dialogueTrack");
        AudioTrack laughAudioTrack = timeline.CreateTrack<AudioTrack>(null, "laughTrack");

        // Create a custom event track
        TimelineEventTrack focusTrack = timeline.CreateTrack<TimelineEventTrack>(null, "cameraFocusTrack");
        
        TimelineEventTrack talkTrack = timeline.CreateTrack<TimelineEventTrack>(null, "characterTalkTrack");
        TimelineEventTrack actionTrack = timeline.CreateTrack<TimelineEventTrack>(null, "characterActionTrack");
        playableDirector.SetGenericBinding(laughAudioTrack, laughAudioSource);
        playableDirector.SetGenericBinding(dialogueAudioTrack, mainAudioSource);

        float actionInterval = UnityEngine.Random.Range(5, 15);
        float timeSinceLastAction = 0;
        // Iterate through the dialogue lines
        for (int i = 0; i < dialogueLines.Count; i++)
        {
             Debug.Log($"Iteration {i}. Dialogue lines: {dialogueLines.Count}, Audio clips: {ChatGPT.instance.audioClips.Count}, Laugh tracks: {laughTracks.Count}");
            OpenAI.ChatGPT.DialogueLine line = dialogueLines[i];
            AudioClip dialogueClip = ChatGPT.instance.audioClips[i];

            // Add the AudioClip to the dialogue audio track
            TimelineClip audioClip = dialogueAudioTrack.CreateClip(dialogueClip);
            audioClip.start = currentTime;
            currentTime += audioClip.duration;
            timeSinceLastAction += (float)audioClip.duration;

            ////////////////////////////////////////////////////////////////////////////////////////////


            // Create a default TimelineClip with TimelineEventClip asset and add it to the event track
            TimelineClip eventTimelineClip = focusTrack.CreateDefaultClip();
            // Set the target object for the TimelineEventClip

            eventTimelineClip.start = currentTime - audioClip.duration;

            // Get the TimelineEventClip asset from the TimelineClip
            TimelineEventClip eventClip = eventTimelineClip.asset as TimelineEventClip;
            eventClip.template.IsMethodWithParam = true;
            // Set the handler key for the event clip
            eventClip.template.HandlerKey = "TimelineManager.FocusOnCharacter";

            eventClip.TrackTargetObject = this.gameObject;
            // Set the argument for the custom event based on the current dialogue line
            eventClip.template.ArgValue = line.SpeakerName;
            
            // Add a random delay between the clips
            currentTime += Random.Range(minDelay, maxDelay);


            ////////////////////////////////////////////////////////////////////////////////////////////


            // Create a default TimelineClip with TimelineEventClip asset and add it to the event track
            TimelineClip talkEventTimelineClip = talkTrack.CreateDefaultClip();
            // Set the target object for the TimelineEventClip
            talkEventTimelineClip.start = currentTime - audioClip.duration;

            // Get the TimelineEventClip asset from the TimelineClip
            TimelineEventClip talkEventClip = talkEventTimelineClip.asset as TimelineEventClip;
            talkEventClip.template.IsMethodWithParam = true;
            // Set the handler key for the event clip
            talkEventClip.template.HandlerKey = "TimelineManager.OnTalkAnimationEvent";


            talkEventClip.TrackTargetObject = this.gameObject;
            // Set the argument for the custom event based on the current dialogue line
            talkEventClip.template.ArgValue = line.SpeakerName + "," + audioClip.duration.ToString();

            ////////////////////////////////////////////////////////////////////////////////////////////

             if (timeSinceLastAction >= actionInterval)
             {
                // It's time to add a new action
              

                 // Create and configure the action event clip...
                // Create a default TimelineClip with TimelineEventClip asset and add it to the event track
                TimelineClip characterActionTimelineClip = actionTrack.CreateDefaultClip();
                // Set the target object for the TimelineEventClip
                characterActionTimelineClip.start = currentTime - audioClip.duration;

                // Get the TimelineEventClip asset from the TimelineClip
                TimelineEventClip actionEventClip = characterActionTimelineClip.asset as TimelineEventClip;
                actionEventClip.template.IsMethodWithParam = true;
                // Set the handler key for the event clip
                actionEventClip.template.HandlerKey = "TimelineManager.characterDoAction";

           
                actionEventClip.TrackTargetObject = this.gameObject;
                // Set the argument for the custom event based on the current dialogue line
                actionEventClip.template.ArgValue = line.SpeakerName + "," + line.Action;
                // Reset the timers
                actionInterval = UnityEngine.Random.Range(5, 15);
                timeSinceLastAction = 0;

                
            }
           



            /////////////////////////////////////////////////////////////////////////////////////////////

            if (line.LaughTrack)
            {
                
                AudioClip laughClip = laughTracks[Random.Range(0, laughTracks.Count)];

                // Add the AudioClip to the laugh audio track
                TimelineClip laughAudioClip = laughAudioTrack.CreateClip(laughClip);
               
                laughAudioClip.start = currentTime;
                currentTime += laughAudioClip.duration;
            }






        }

        // Bind the FocusOnCharacterTrack to the current GameObject (assuming it has the TimelineManager script)
       // foreach (var trackAsset in timeline.GetOutputTracks())
       // {
       //     if(trackAsset.name != "Focus On Actor" || trackAsset.name != "Trigger Talk")
       //         playableDirector.SetGenericBinding(trackAsset.outputs.GetEnumerator().Current.sourceObject, mainAudioSource);
       // }

        playableDirector.playableAsset = timeline;
        // Focus on the first speaker
        if (ChatGPT.instance.dialogue.Count > 0)
        {
            FocusOnCharacter(ChatGPT.instance.dialogue[0].SpeakerName);
           
        }

        // ADD THE END
        // Create a default TimelineClip with TimelineEventClip asset and add it to the event track
        TimelineClip endEventTimelineClip = talkTrack.CreateDefaultClip();
        // Set the target object for the TimelineEventClip
        endEventTimelineClip.start = currentTime + 1;

        // Get the TimelineEventClip asset from the TimelineClip
        TimelineEventClip endEventClip = endEventTimelineClip.asset as TimelineEventClip;
        // Set the handler key for the event clip
        endEventClip.template.HandlerKey = "TimelineManager.EpisodeFinished";
        endEventClip.TrackTargetObject = this.gameObject;

        // Play the timeline
        playableDirector.Play();

        ChatGPT.instance.GenerateEpisodePlot();
       
    }

    public void OnTalkAnimationEvent(string args)
    {

        string[] splitArgs = args.Split(',');
        string characterName = splitArgs[0];
        float duration = float.Parse(splitArgs[1]);

        GameObject character = GameObject.Find(characterName);
        if (character != null)
        {
            Animator animator = character.GetComponent<Animator>();
            if (animator != null)
            {
                int triggerHash = Animator.StringToHash("talkBool");
                animator.SetBool(triggerHash, true);
                StartCoroutine(ResetTriggerAfterDuration(animator, triggerHash, duration));
            }
        }
    }

    public void EpisodeFinished()
    {
        // Code to execute when an episode finishes
        OnEpisodeEnded?.Invoke();
 
    }

    public void characterDoAction(string args)
    {
        string[] splitArgs = args.Split(',');
        string characterName = splitArgs[0];
        string actionName = splitArgs[1];


        //We find character gameobject.
        GameObject character = GameObject.Find(characterName);
        //Make character move to action, after he gets there, he starts the action.
        character.GetComponent<vAIMoveToPosition>().MoveTo(actionName);
        Debug.Log(character.name + " " + actionName);
        
    }

    private IEnumerator ResetTriggerAfterDuration(Animator animator, int triggerHash, float duration)
    {
        yield return new WaitForSeconds(duration);
        animator.SetBool(triggerHash, false);
    }

    private Transform FindChildByName(Transform parent, string name)
    {
        if (parent.name == name) return parent;

        foreach (Transform child in parent)
            
        {
           // GetComponent<Animator>().PlayInFixedTime()
            Transform result = FindChildByName(child, name);
            if (result != null) return result;
        }

        return null;
    }
}
