#if VFX_HAS_TIMELINE
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UnityEngine.VFX
{
    // Represents the serialized data for a clip on the TextTrack
    [Serializable]
    public class VisualEffectControlPlayableAsset : PlayableAsset, ITimelineClipAsset
    {
        //[NoFoldOut]
        [NotKeyable] // NotKeyable used to prevent Timeline from making fields available for animation.
        public VisualEffectControlPlayableBehaviour template = new VisualEffectControlPlayableBehaviour();

        public static bool useBlending_WIP
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.EditorPrefs.GetBool("VFX.MixerUseBlending_TEMP_TO_BE_REMOVED", true);
#else
                return true;
#endif
            }
        }

        public ClipCaps clipCaps
        {
            get { return useBlending_WIP ? ClipCaps.Blending : ClipCaps.None; }
        }

        public double clipStart { get; set; }
        public double clipEnd { get; set; }

        public void SetDefaultEvent(double playAfterClipStart, double stopBeforeClipEnd)
        {
            //if (events == null) //TEMP TODPAUL
                events = new List<VisualEffectPlayableSerializedEvent>();

            if (!events.Any(o => o.type == VisualEffectPlayableSerializedEvent.Type.Play))
            {
                events.Add(new VisualEffectPlayableSerializedEvent()
                {
                    name = VisualEffectAsset.PlayEventName,
                    time = playAfterClipStart,
                    timeSpace = VisualEffectPlayableSerializedEvent.TimeSpace.AfterClipStart,
                    type = VisualEffectPlayableSerializedEvent.Type.Play
                });
            }

            if (!events.Any(o => o.type == VisualEffectPlayableSerializedEvent.Type.Stop))
            {
                events.Add(new VisualEffectPlayableSerializedEvent()
                {
                    name = VisualEffectAsset.StopEventName,
                    time = stopBeforeClipEnd,
                    timeSpace = VisualEffectPlayableSerializedEvent.TimeSpace.BeforeClipEnd,
                    type = VisualEffectPlayableSerializedEvent.Type.Stop
                });
            }
        }

        [NotKeyable]
        public List<VisualEffectPlayableSerializedEvent> events;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<VisualEffectControlPlayableBehaviour>.Create(graph, template);
            var behaviour = playable.GetBehaviour();
            behaviour.clipStart = clipStart;
            behaviour.clipEnd = clipEnd;
            behaviour.events = events.ToArray();
            return playable;
        }
    }
}
#endif
