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
