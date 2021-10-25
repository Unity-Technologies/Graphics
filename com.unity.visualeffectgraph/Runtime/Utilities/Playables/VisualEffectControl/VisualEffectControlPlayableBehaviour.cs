#if VFX_HAS_TIMELINE
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.VFX.Utility;

namespace UnityEngine.VFX
{
    [Serializable]
    public struct EventAttributes //Using encapsulated structure to ease CustomPropertyDrawer
    {
        [SerializeReference]
        public EventAttribute[] content;
    }

    [Serializable]
    public abstract class EventAttribute
    {
        public ExposedProperty id;
        public abstract void ApplyToVFX(VFXEventAttribute eventAttribute);
    }

    [Serializable]
    public abstract class EventAttributeValue<T> : EventAttribute
    {
        public T value;
    }

    [Serializable]
    public class EventAttributeFloat : EventAttributeValue<float>
    {
        public sealed override void ApplyToVFX(VFXEventAttribute eventAttribute)
        {
            eventAttribute.SetFloat(id, value);
        }
    }
    [Serializable]
    public class EventAttributeVector2 : EventAttributeValue<Vector2>
    {
        public sealed override void ApplyToVFX(VFXEventAttribute eventAttribute)
        {
            eventAttribute.SetVector2(id, value);
        }
    }
    [Serializable]
    public class EventAttributeVector3 : EventAttributeValue<Vector3>
    {
        public sealed override void ApplyToVFX(VFXEventAttribute eventAttribute)
        {
            eventAttribute.SetVector3(id, value);
        }
    }
    [Serializable]
    public class EventAttributeColor : EventAttributeVector3 {}
    [Serializable]
    public class EventAttributeVector4 : EventAttributeValue<Vector4>
    {
        public sealed override void ApplyToVFX(VFXEventAttribute eventAttribute)
        {
            eventAttribute.SetVector4(id, value);
        }
    }
    [Serializable]
    public class EventAttributeInt : EventAttributeValue<int>
    {
        public sealed override void ApplyToVFX(VFXEventAttribute eventAttribute)
        {
            eventAttribute.SetInt(id, value);
        }
    }
    [Serializable]
    public class EventAttributeUInt : EventAttributeValue<uint>
    {
        public sealed override void ApplyToVFX(VFXEventAttribute eventAttribute)
        {
            eventAttribute.SetUint(id, value);
        }
    }
    [Serializable]
    public class EventAttributeBool : EventAttributeValue<bool>
    {
        public sealed override void ApplyToVFX(VFXEventAttribute eventAttribute)
        {
            eventAttribute.SetBool(id, value);
        }
    }

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
                copy.time = GetTimeInSpace(itEvent.timeSpace, itEvent.time, space, clipStart, clipEnd);
                yield return copy;
            }
        }

        public static double GetTimeInSpace(TimeSpace srcSpace, double srcTime, TimeSpace dstSpace, double clipStart, double clipEnd)
        {
            if (srcSpace == dstSpace)
                return srcTime;

            if (dstSpace == TimeSpace.AfterClipStart)
            {
                switch (srcSpace)
                {
                    case TimeSpace.BeforeClipEnd:
                        return clipEnd - srcTime - clipStart;
                    case TimeSpace.Percentage:
                        return (clipEnd - clipStart) * (srcTime / 100.0);
                    case TimeSpace.Absolute:
                        return srcTime - clipStart;
                }
            }
            else if (dstSpace == TimeSpace.BeforeClipEnd)
            {
                switch (srcSpace)
                {
                    case TimeSpace.AfterClipStart:
                        return clipEnd - srcTime - clipStart;
                    case TimeSpace.Percentage:
                        //TODOPAUL: Can be simplified
                        return clipEnd - clipStart - (clipEnd - clipStart) * (srcTime / 100.0);
                    case TimeSpace.Absolute:
                        return clipEnd - srcTime;
                }
            }
            else if (dstSpace == TimeSpace.Percentage)
            {
                switch (srcSpace)
                {
                    case TimeSpace.AfterClipStart:
                        return 100.0 * (srcTime) / (clipEnd - clipStart);
                    case TimeSpace.BeforeClipEnd:
                        return 100.0 * (clipEnd - srcTime - clipStart) / (clipEnd - clipStart);
                    case TimeSpace.Absolute:
                        return 100.0 * (srcTime - clipStart) / (clipEnd - clipStart);
                }
            }
            else if (dstSpace == TimeSpace.Absolute)
            {
                switch (srcSpace)
                {
                    case TimeSpace.AfterClipStart:
                        return clipStart + srcTime;
                    case TimeSpace.BeforeClipEnd:
                        return clipEnd - srcTime;
                    case TimeSpace.Percentage:
                        return clipStart + (clipEnd - clipStart) * (srcTime / 100.0);
                }
            }

            //Other conversion
            throw new NotImplementedException(srcSpace + " to " + dstSpace);
        }

        public enum TimeSpace
        {
            AfterClipStart,
            BeforeClipEnd,
            Percentage,
            Absolute
        }

        public double time;
        public TimeSpace timeSpace;
        public ExposedProperty name;
        public EventAttributes eventAttributes;
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
