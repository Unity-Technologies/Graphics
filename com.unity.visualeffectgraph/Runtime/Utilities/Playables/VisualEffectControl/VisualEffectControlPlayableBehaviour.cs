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
    struct EventAttributes
    {
        [SerializeReference]
        public EventAttribute[] content;
    }

    [Serializable]
    abstract class EventAttribute
    {
        public ExposedProperty id;
        public abstract bool ApplyToVFX(VFXEventAttribute eventAttribute);
    }

    [Serializable]
    abstract class EventAttributeValue<T> : EventAttribute
    {
        public T value;
    }

    [Serializable]
    class EventAttributeFloat : EventAttributeValue<float>
    {
        public sealed override bool ApplyToVFX(VFXEventAttribute eventAttribute)
        {
            if (!eventAttribute.HasFloat(id))
                return false;
            eventAttribute.SetFloat(id, value);
            return true;
        }
    }
    [Serializable]
    class EventAttributeVector2 : EventAttributeValue<Vector2>
    {
        public sealed override bool ApplyToVFX(VFXEventAttribute eventAttribute)
        {
            if (!eventAttribute.HasVector2(id))
                return false;
            eventAttribute.SetVector2(id, value);
            return true;
        }
    }
    [Serializable]
    class EventAttributeVector3 : EventAttributeValue<Vector3>
    {
        public sealed override bool ApplyToVFX(VFXEventAttribute eventAttribute)
        {
            if (!eventAttribute.HasVector3(id))
                return false;
            eventAttribute.SetVector3(id, value);
            return true;
        }
    }
    [Serializable]
    class EventAttributeColor : EventAttributeVector3 {}
    [Serializable]
    class EventAttributeVector4 : EventAttributeValue<Vector4>
    {
        public sealed override bool ApplyToVFX(VFXEventAttribute eventAttribute)
        {
            if (!eventAttribute.HasVector4(id))
                return false;
            eventAttribute.SetVector4(id, value);
            return true;
        }
    }
    [Serializable]
    class EventAttributeInt : EventAttributeValue<int>
    {
        public sealed override bool ApplyToVFX(VFXEventAttribute eventAttribute)
        {
            if (!eventAttribute.HasInt(id))
                return false;
            eventAttribute.SetInt(id, value);
            return true;
        }
    }
    [Serializable]
    class EventAttributeUInt : EventAttributeValue<uint>
    {
        public sealed override bool ApplyToVFX(VFXEventAttribute eventAttribute)
        {
            if (!eventAttribute.HasUint(id))
                return false;
            eventAttribute.SetUint(id, value);
            return true;
        }
    }
    [Serializable]
    class EventAttributeBool : EventAttributeValue<bool>
    {
        public sealed override bool ApplyToVFX(VFXEventAttribute eventAttribute)
        {
            if (!eventAttribute.HasBool(id))
                return false;
            eventAttribute.SetBool(id, value);
            return true;
        }
    }

    static class VFXTimeSpaceHelper
    {
        public static IEnumerable<VisualEffectPlayableSerializedEvent> GetEventNormalizedSpace(VisualEffectPlayableSerializedEvent.TimeSpace space, VisualEffectControlPlayableBehaviour source)
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

        public static IEnumerable<VisualEffectPlayableSerializedEvent> GetEventNormalizedSpace(VisualEffectPlayableSerializedEvent.TimeSpace space, VisualEffectControlPlayableAsset source, bool clipEvents)
        {
            IEnumerable<VisualEffectPlayableSerializedEvent> sourceEvents;
            if (clipEvents)
                sourceEvents = CollectClipEvents(source);
            else
                sourceEvents = source.singleEvents;
            return GetEventNormalizedSpace(space, sourceEvents, source.clipStart, source.clipEnd);
        }

        private static IEnumerable<VisualEffectPlayableSerializedEvent> GetEventNormalizedSpace(VisualEffectPlayableSerializedEvent.TimeSpace space, IEnumerable<VisualEffectPlayableSerializedEvent> events, double clipStart, double clipEnd)
        {
            foreach (var itEvent in events)
            {
                var copy = itEvent;
                copy.timeSpace = space;
                copy.time = GetTimeInSpace(itEvent.timeSpace, itEvent.time, space, clipStart, clipEnd);
                yield return copy;
            }
        }

        public static double GetTimeInSpace(VisualEffectPlayableSerializedEvent.TimeSpace srcSpace, double srcTime, VisualEffectPlayableSerializedEvent.TimeSpace dstSpace, double clipStart, double clipEnd)
        {
            if (srcSpace == dstSpace)
                return srcTime;

            if (dstSpace == VisualEffectPlayableSerializedEvent.TimeSpace.AfterClipStart)
            {
                switch (srcSpace)
                {
                    case VisualEffectPlayableSerializedEvent.TimeSpace.BeforeClipEnd:
                        return clipEnd - srcTime - clipStart;
                    case VisualEffectPlayableSerializedEvent.TimeSpace.Percentage:
                        return (clipEnd - clipStart) * (srcTime / 100.0);
                    case VisualEffectPlayableSerializedEvent.TimeSpace.Absolute:
                        return srcTime - clipStart;
                }
            }
            else if (dstSpace == VisualEffectPlayableSerializedEvent.TimeSpace.BeforeClipEnd)
            {
                switch (srcSpace)
                {
                    case VisualEffectPlayableSerializedEvent.TimeSpace.AfterClipStart:
                        return clipEnd - srcTime - clipStart;
                    case VisualEffectPlayableSerializedEvent.TimeSpace.Percentage:
                        return clipEnd - clipStart - (clipEnd - clipStart) * (srcTime / 100.0);
                    case VisualEffectPlayableSerializedEvent.TimeSpace.Absolute:
                        return clipEnd - srcTime;
                }
            }
            else if (dstSpace == VisualEffectPlayableSerializedEvent.TimeSpace.Percentage)
            {
                switch (srcSpace)
                {
                    case VisualEffectPlayableSerializedEvent.TimeSpace.AfterClipStart:
                        return 100.0 * (srcTime) / (clipEnd - clipStart);
                    case VisualEffectPlayableSerializedEvent.TimeSpace.BeforeClipEnd:
                        return 100.0 * (clipEnd - srcTime - clipStart) / (clipEnd - clipStart);
                    case VisualEffectPlayableSerializedEvent.TimeSpace.Absolute:
                        return 100.0 * (srcTime - clipStart) / (clipEnd - clipStart);
                }
            }
            else if (dstSpace == VisualEffectPlayableSerializedEvent.TimeSpace.Absolute)
            {
                switch (srcSpace)
                {
                    case VisualEffectPlayableSerializedEvent.TimeSpace.AfterClipStart:
                        return clipStart + srcTime;
                    case VisualEffectPlayableSerializedEvent.TimeSpace.BeforeClipEnd:
                        return clipEnd - srcTime;
                    case VisualEffectPlayableSerializedEvent.TimeSpace.Percentage:
                        return clipStart + (clipEnd - clipStart) * (srcTime / 100.0);
                }
            }

            //Other conversion
            throw new NotImplementedException(srcSpace + " to " + dstSpace);
        }
    }

    [Serializable]
    struct VisualEffectPlayableSerializedEvent
    {
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

    class VisualEffectControlPlayableBehaviour : PlayableBehaviour
    {
        public double clipStart { get; set; }
        public double clipEnd { get; set; }
        public bool scrubbing { get; set; }
        public bool reinitEnter { get; set; }
        public bool reinitExit { get; set; }
        public uint startSeed { get; set; }

        public VisualEffectPlayableSerializedEvent[] events { get; set; }
        public uint clipEventsCount { get; set; }

        public uint prewarmStepCount { get; set; }
        public float prewarmDeltaTime { get; set; }
        public ExposedProperty prewarmEvent { get; set; }
    }
}
#endif
