using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace Tantawowa.TimelineEvents
{
    [System.Serializable]
    public class TimelineEventClip : PlayableAsset, ITimelineClipAsset
    {
        public TimelineEventBehaviour template = new TimelineEventBehaviour();
        public GameObject TrackTargetObject { get; set; }
        [SerializeField] private double m_Start;
        public ClipCaps clipCaps => ClipCaps.None;

        public double start
        {
            get { return m_Start; }
            set { m_Start = value; }
        }

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<TimelineEventBehaviour>.Create(graph, template);
            TimelineEventBehaviour clone = playable.GetBehaviour();
            clone.HandlerKey = template.HandlerKey;
            clone.ArgValue = template.ArgValue;
            clone.TargetObject = TrackTargetObject; // Set the TargetObject from the TimelineEventClip
            return playable;
        }


    }
}