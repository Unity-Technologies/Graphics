#if VFX_HAS_TIMELINE
using System;
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
        public double easeIn { get; set; }
        public double easeOut { get; set; }

        [NotKeyable]
        VisualEffectPlayableSerializedEvent[] events;

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<VisualEffectControlPlayableBehaviour>.Create(graph, template);
            var behaviour = playable.GetBehaviour();

            behaviour.clipStart = clipStart;
            behaviour.clipEnd = clipEnd;

            //Transmission here, will be move TODOPAUL
            behaviour.events = new VisualEffectPlayableSerializedEvent[]
            {
                new VisualEffectPlayableSerializedEvent
                {
                    name = VisualEffectAsset.PlayEventName,
                    time = easeIn,
                    timeSpace = VisualEffectPlayableSerializedEvent.TimeSpace.AfterClipStart
                },
                new VisualEffectPlayableSerializedEvent
                {
                    name = VisualEffectAsset.StopEventName,
                    time = easeOut,
                    timeSpace = VisualEffectPlayableSerializedEvent.TimeSpace.BeforeClipEnd
                }
            };
            return playable;
        }
    }
}
#endif
