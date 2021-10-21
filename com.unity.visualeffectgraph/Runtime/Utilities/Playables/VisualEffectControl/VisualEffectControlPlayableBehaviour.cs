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

        private static IEnumerable<VisualEffectPlayableSerializedEvent> CollectClipEvents(VisualEffectControlPlayableAsset source)
        {
            if (source.clipEvents != null)
            {
                foreach (var clip in source.clipEvents)
                {
                    yield return clip.enter;
                    yield return clip.exit;
                }
            }
        }

        public static IEnumerable<VisualEffectPlayableSerializedEvent> GetEventNormalizedSpace(TimeSpace space, VisualEffectControlPlayableAsset source, bool clipEvents)
        {
            IEnumerable<VisualEffectPlayableSerializedEvent> sourceEvents;
            if (clipEvents)
                sourceEvents = CollectClipEvents(source);
            else
                sourceEvents = source.singleEvents;
            return GetEventNormalizedSpace(space, sourceEvents, source.clipStart, source.clipEnd);
        }

        private static IEnumerable<VisualEffectPlayableSerializedEvent> GetEventNormalizedSpace(TimeSpace space, IEnumerable<VisualEffectPlayableSerializedEvent> events, double clipStart, double clipEnd)
        {
            foreach (var itEvent in events)
            {
                var copy = itEvent;
                copy.timeSpace = space;
                copy.time = GetTimeInSpace(space, itEvent, clipStart, clipEnd);
                yield return copy;
            }
        }

        private static double GetTimeInSpace(TimeSpace space, VisualEffectPlayableSerializedEvent source, double clipStart, double clipEnd)
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
                }
            }
            else if (space == TimeSpace.AfterClipStart)
            {
                switch (source.timeSpace)
                {
                    case TimeSpace.BeforeClipEnd:
                        return clipEnd - source.time - clipStart;
                    case TimeSpace.Absolute:
                        return source.time - clipStart;
                }
            }

            //Other conversion
            throw new NotImplementedException();
        }

        public enum TimeSpace
        {
            AfterClipStart,
            BeforeClipEnd,
            Absolute
            //... TODOPAUL Add Percentage between Start/End
        }

        public TimeSpace timeSpace;
        public double time;
        public string name;
        //TODOPAUL payload of attribute
    }

    public class VisualEffectControlPlayableBehaviour : PlayableBehaviour
    {
        public double clipStart { get; set; }
        public double clipEnd { get; set; }
        public bool scrubbing { get; set; }
        public uint startSeed { get; set; }

        public VisualEffectPlayableSerializedEvent[] events { get; set; }
        public uint clipEventsCount { get; set; }
    }
}
#endif
