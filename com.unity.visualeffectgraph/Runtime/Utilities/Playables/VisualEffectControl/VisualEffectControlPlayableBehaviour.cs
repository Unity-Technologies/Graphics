#if VFX_HAS_TIMELINE
using System;
using UnityEngine;
using UnityEngine.Playables;

namespace UnityEngine.VFX
{
    [Serializable]
    public struct VisualEffectPlayableSerializedEvent
    {
        public static double GetAbsoluteTime(VisualEffectPlayableSerializedEvent current, VisualEffectControlPlayableBehaviour parent)
        {
            return GetAbsoluteTime(current, parent.clipStart, parent.clipEnd);
        }

        public static double GetAbsoluteTime(VisualEffectPlayableSerializedEvent current, VisualEffectControlPlayableAsset parent)
        {
            return GetAbsoluteTime(current, parent.clipStart, parent.clipEnd);
        }

        private static double GetAbsoluteTime(VisualEffectPlayableSerializedEvent current, double clipStart, double clipEnd)
        {
            switch (current.timeSpace)
            {
                case VisualEffectPlayableSerializedEvent.TimeSpace.AfterClipStart:
                    return clipStart + current.time;
                case VisualEffectPlayableSerializedEvent.TimeSpace.BeforeClipEnd:
                    return clipEnd - current.time;
                default:
                    throw new System.Exception("TODOPAUL");
            }
        }

        public enum Type
        {
            Custom,
            Play,
            Stop,
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
