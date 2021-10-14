#if VFX_HAS_TIMELINE
using System;
using UnityEngine;
using UnityEngine.Playables;

namespace UnityEngine.VFX
{
    [Serializable]
    public struct VisualEffectPlayableSerializedEvent
    {
        public enum Type
        {
            Play,
            Stop,
            Custom
        }

        public enum TimeSpace
        {
            AfterClipStart,
            BeforeClipEnd,
            AfterPlay,
            //...
        }

        public Type type;
        public TimeSpace timeSpace;
        public double time;
        public string name;
        //TODOPAUL payload of attribute
    }

    [Serializable]
    public class VisualEffectControlPlayableBehaviour : PlayableBehaviour
    {
        public double clipStart { get; set; }
        public double clipEnd { get; set; }

        public VisualEffectPlayableSerializedEvent[] events { get; set; }
    }
}
#endif
