#if VFX_HAS_TIMELINE
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

namespace UnityEngine.VFX
{
    [Serializable]
    public struct VisualEffectPlayableSerializedEvent
    {
        public static double GetAbsoluteTime(VisualEffectPlayableSerializedEvent current, VisualEffectControlPlayableBehaviour parent)
        {
            return GetAbsoluteTime(current, parent.clipStart, parent.clipEnd, GetPlayTime(parent.events));
        }

        public static double GetAbsoluteTime(VisualEffectPlayableSerializedEvent current, VisualEffectControlPlayableAsset parent)
        {
            return GetAbsoluteTime(current, parent.clipStart, parent.clipEnd, GetPlayTime(parent.events));
        }

        private static double GetPlayTime(IEnumerable<VisualEffectPlayableSerializedEvent> events)
        {
            if (events != null)
            {
                var itEvent = events.FirstOrDefault(o => o.type == VisualEffectPlayableSerializedEvent.Type.Play);
                if (itEvent.timeSpace != TimeSpace.AfterClipStart)
                    throw new NotImplementedException();
                return itEvent.time;
            }
            return 0.0;
        }

        private static double GetAbsoluteTime(VisualEffectPlayableSerializedEvent current, double clipStart, double clipEnd, double clipPlay)
        {
            switch (current.timeSpace)
            {
                case VisualEffectPlayableSerializedEvent.TimeSpace.AfterClipStart:
                    return clipStart + current.time;
                case VisualEffectPlayableSerializedEvent.TimeSpace.BeforeClipEnd:
                    return clipEnd - current.time;
                case VisualEffectPlayableSerializedEvent.TimeSpace.AfterPlay:
                    return clipStart + clipPlay + current.time;
            }
            throw new NotImplementedException();
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
