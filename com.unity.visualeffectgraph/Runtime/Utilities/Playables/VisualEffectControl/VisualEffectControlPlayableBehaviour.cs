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

        public static IEnumerable<VisualEffectPlayableSerializedEvent> GetEventNormalizedSpace(TimeSpace space, VisualEffectControlPlayableBehaviour source)
        {
            return GetEventNormalizedSpace(space, source.events, source.clipStart, source.clipEnd);
        }

        public static IEnumerable<VisualEffectPlayableSerializedEvent> GetEventNormalizedSpace(TimeSpace space, VisualEffectControlPlayableAsset source)
        {
            return GetEventNormalizedSpace(space, source.events, source.clipStart, source.clipEnd);
        }

        private static IEnumerable<VisualEffectPlayableSerializedEvent> GetEventNormalizedSpace(TimeSpace space, IEnumerable<VisualEffectPlayableSerializedEvent> events, double clipStart, double clipEnd)
        {
            var playTimeRef = GetPlayTime(events);
            return GetEventNormalizedSpace(space, events, clipStart, clipEnd, playTimeRef);
        }

        private static IEnumerable<VisualEffectPlayableSerializedEvent> GetEventNormalizedSpace(TimeSpace space, IEnumerable<VisualEffectPlayableSerializedEvent> events, double clipStart, double clipEnd, double playTimeRef)
        {
            foreach (var itEvent in events)
            {
                var copy = itEvent;
                copy.timeSpace = space;
                copy.time = GetTimeInSpace(space, itEvent, clipStart, clipEnd, playTimeRef);
                yield return copy;
            }
        }

        private static double GetTimeInSpace(TimeSpace space, VisualEffectPlayableSerializedEvent source, double clipStart, double clipEnd, double clipPlay)
        {
            if (source.timeSpace == space)
                return source.time;

            if (space == TimeSpace.Absolute)
            {
                switch (source.timeSpace)
                {
                    case TimeSpace.AfterClipStart:
                        return clipStart + source.time;
                    case TimeSpace.BeforeClipEnd:
                        return clipEnd - source.time;
                    case TimeSpace.AfterPlay:
                        return clipStart + clipPlay + source.time;
                }
            }
            else if (space == TimeSpace.AfterClipStart)
            {
                switch (source.timeSpace)
                {
                    case TimeSpace.BeforeClipEnd:
                        return clipEnd - source.time - clipStart;
                    case TimeSpace.AfterPlay:
                        return clipPlay + source.time;
                    case TimeSpace.Absolute:
                        return source.time - clipStart;
                }
            }

            //Other conversion
            throw new NotImplementedException();
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
            Absolute
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
