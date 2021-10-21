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

        [NotKeyable]
        public bool scrubbing = true;
        [NotKeyable]
        public uint startSeed;

        public void SetDefaultEvent(double playAfterClipStart, double stopBeforeClipEnd)
        {
            if (!useBlending_WIP)
                return;

            var previousEvent = events == null ? new List<VisualEffectPlayableSerializedEvent>() : events.ToList();
            events = new List<VisualEffectPlayableSerializedEvent>();

            int indexOfStart = -1;
            int indexOfStop = -1;

            for (int i = 0; i < previousEvent.Count; ++i)
            {
                if (indexOfStart == -1 && previousEvent[i].type == VisualEffectPlayableSerializedEvent.Type.Play)
                    indexOfStart = i;
                if (indexOfStop == -1 && previousEvent[i].type == VisualEffectPlayableSerializedEvent.Type.Stop)
                    indexOfStop = i;
                if (indexOfStop != -1 && indexOfStart != -1)
                    break;
            }

            //Copy Play
            {
                var startName = indexOfStart == -1 ? VisualEffectAsset.PlayEventName : previousEvent[indexOfStart].name;
                var startTime = playAfterClipStart;
                events.Add(new VisualEffectPlayableSerializedEvent()
                {
                    name = startName,
                    time = startTime,
                    timeSpace = VisualEffectPlayableSerializedEvent.TimeSpace.AfterClipStart,
                    type = VisualEffectPlayableSerializedEvent.Type.Play
                });
            }

            //Copy Stop
            {
                var stopName = indexOfStop == -1 ? VisualEffectAsset.StopEventName : previousEvent[indexOfStop].name;
                var stopTime = stopBeforeClipEnd;
                events.Add(new VisualEffectPlayableSerializedEvent()
                {
                    name = stopName,
                    time = stopTime,
                    timeSpace = VisualEffectPlayableSerializedEvent.TimeSpace.BeforeClipEnd,
                    type = VisualEffectPlayableSerializedEvent.Type.Stop
                });
            }

            if (indexOfStop != -1)
                previousEvent.RemoveAt(indexOfStop);

            if (indexOfStart != -1)
                previousEvent.RemoveAt(indexOfStart);

            //Take the rest
            var other = previousEvent.Select(o =>
            {
                return new VisualEffectPlayableSerializedEvent()
                {
                    name = o.name,
                    time = o.time,
                    timeSpace = o.timeSpace,
                    type = VisualEffectPlayableSerializedEvent.Type.Custom
                };
            }).ToArray();
            events.AddRange(other);
        }

        [NotKeyable]
        public List<VisualEffectPlayableSerializedEvent> events = new List<VisualEffectPlayableSerializedEvent>()
        {
            new VisualEffectPlayableSerializedEvent()
            {
                name = VisualEffectAsset.PlayEventName,
                time = 0.0,
                timeSpace = VisualEffectPlayableSerializedEvent.TimeSpace.AfterClipStart,
                type = VisualEffectPlayableSerializedEvent.Type.Play
            },
            new VisualEffectPlayableSerializedEvent()
            {
                name = VisualEffectAsset.StopEventName,
                time = 0.0,
                timeSpace = VisualEffectPlayableSerializedEvent.TimeSpace.BeforeClipEnd,
                type = VisualEffectPlayableSerializedEvent.Type.Stop
            },
        };

        public override Playable CreatePlayable(PlayableGraph graph, GameObject owner)
        {
            var playable = ScriptPlayable<VisualEffectControlPlayableBehaviour>.Create(graph);
            var behaviour = playable.GetBehaviour();
            behaviour.clipStart = clipStart;
            behaviour.clipEnd = clipEnd;
            behaviour.scrubbing = scrubbing;
            behaviour.startSeed = startSeed;
            behaviour.events = events == null ? new VisualEffectPlayableSerializedEvent[] { } : events.ToArray();
            return playable;
        }
    }
}
#endif
