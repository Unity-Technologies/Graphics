#if VFX_HAS_TIMELINE
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UnityEngine.VFX
{
    [Serializable]
    public class VisualEffectControlPlayableAsset : PlayableAsset, ITimelineClipAsset
    {
        public ClipCaps clipCaps
        {
            //TODOPAUL: If speed available, enable here
            get { return ClipCaps.None; }
        }

        public double clipStart { get; set; }
        public double clipEnd { get; set; }

        [NotKeyable]
        public bool scrubbing = true;
        [NotKeyable]
        public uint startSeed;

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
