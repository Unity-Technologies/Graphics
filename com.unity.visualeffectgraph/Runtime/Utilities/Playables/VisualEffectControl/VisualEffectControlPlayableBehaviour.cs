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
        private readonly Func<VFXEventAttribute, int, bool> m_HasFunc;
        private readonly Action<VFXEventAttribute, int, T> m_ApplyFunc;

        protected EventAttributeValue(Func<VFXEventAttribute, int, bool> hasFunc, Action<VFXEventAttribute, int, T> applyFunc)
        {
            m_HasFunc = hasFunc;
            m_ApplyFunc = applyFunc;
        }

        public sealed override bool ApplyToVFX(VFXEventAttribute eventAttribute)
        {
            if (!m_HasFunc(eventAttribute, id))
                return false;
            m_ApplyFunc(eventAttribute, id, value);;
            return true;
        }

        public T value;
    }

    [Serializable]
    class EventAttributeFloat : EventAttributeValue<float>
    {
        public EventAttributeFloat() : base((e, id) => e.HasFloat(id), (e, id, value) => e.SetFloat(id, value)) { }
    }

    [Serializable]
    class EventAttributeVector2 : EventAttributeValue<Vector2>
    {
        public EventAttributeVector2() : base((e, id) => e.HasVector2(id), (e, id, value) => e.SetVector2(id, value)) { }
    }

    [Serializable]
    class EventAttributeVector3 : EventAttributeValue<Vector3>
    {
        public EventAttributeVector3() : base((e, id) => e.HasVector3(id), (e, id, value) => e.SetVector3(id, value)) { }
    }
    [Serializable]
    class EventAttributeColor : EventAttributeVector3 {}
    [Serializable]
    class EventAttributeVector4 : EventAttributeValue<Vector4>
    {
        public EventAttributeVector4() : base((e, id) => e.HasVector4(id), (e, id, value) => e.SetVector4(id, value)) { }
    }
    [Serializable]
    class EventAttributeInt : EventAttributeValue<int>
    {
        public EventAttributeInt() : base((e, id) => e.HasInt(id), (e, id, value) => e.SetInt(id, value)) { }
    }
    [Serializable]
    class EventAttributeUInt : EventAttributeValue<uint>
    {
        public EventAttributeUInt() : base((e, id) => e.HasUint(id), (e, id, value) => e.SetUint(id, value)) { }
    }
    [Serializable]
    class EventAttributeBool : EventAttributeValue<bool>
    {
        public EventAttributeBool() : base((e, id) => e.HasBool(id), (e, id, value) => e.SetBool(id, value)) { }
    }

    static class VFXTimeSpaceHelper
    {
        public static IEnumerable<VisualEffectPlayableSerializedEvent> GetEventNormalizedSpace(PlayableTimeSpace space, VisualEffectControlPlayableBehaviour source)
        {
            return GetEventNormalizedSpace(space, source.events, source.clipStart, source.clipEnd);
        }

        private static IEnumerable<VisualEffectPlayableSerializedEvent> CollectClipEvents(VisualEffectControlClip source)
        {
            if (source.clipEvents != null)
            {
                foreach (var clip in source.clipEvents)
                {
                    var eventEnter = (VisualEffectPlayableSerializedEvent)clip.enter;
                    var eventExit = (VisualEffectPlayableSerializedEvent)clip.exit;
                    eventEnter.editorColor = eventExit.editorColor = clip.editorColor;

                    yield return eventEnter;
                    yield return eventExit;
                }
            }
        }

        public static IEnumerable<VisualEffectPlayableSerializedEvent> GetEventNormalizedSpace(PlayableTimeSpace space, VisualEffectControlClip source, bool clipEvents)
        {
            IEnumerable<VisualEffectPlayableSerializedEvent> sourceEvents;
            if (clipEvents)
                sourceEvents = CollectClipEvents(source);
            else
                sourceEvents = source.singleEvents;
            return GetEventNormalizedSpace(space, sourceEvents, source.clipStart, source.clipEnd);
        }

        private static IEnumerable<VisualEffectPlayableSerializedEvent> GetEventNormalizedSpace(PlayableTimeSpace space, IEnumerable<VisualEffectPlayableSerializedEvent> events, double clipStart, double clipEnd)
        {
            foreach (var itEvent in events)
            {
                var copy = itEvent;
                copy.timeSpace = space;
                copy.time = GetTimeInSpace(itEvent.timeSpace, itEvent.time, space, clipStart, clipEnd);
                yield return copy;
            }
        }

        public static double GetTimeInSpace(PlayableTimeSpace srcSpace, double srcTime, PlayableTimeSpace dstSpace, double clipStart, double clipEnd)
        {
            if (srcSpace == dstSpace)
                return srcTime;

            if (dstSpace == PlayableTimeSpace.AfterClipStart)
            {
                switch (srcSpace)
                {
                    case PlayableTimeSpace.BeforeClipEnd:
                        return clipEnd - srcTime - clipStart;
                    case PlayableTimeSpace.Percentage:
                        return (clipEnd - clipStart) * (srcTime / 100.0);
                    case PlayableTimeSpace.Absolute:
                        return srcTime - clipStart;
                }
            }
            else if (dstSpace == PlayableTimeSpace.BeforeClipEnd)
            {
                switch (srcSpace)
                {
                    case PlayableTimeSpace.AfterClipStart:
                        return clipEnd - srcTime - clipStart;
                    case PlayableTimeSpace.Percentage:
                        return clipEnd - clipStart - (clipEnd - clipStart) * (srcTime / 100.0);
                    case PlayableTimeSpace.Absolute:
                        return clipEnd - srcTime;
                }
            }
            else if (dstSpace == PlayableTimeSpace.Percentage)
            {
                switch (srcSpace)
                {
                    case PlayableTimeSpace.AfterClipStart:
                        return 100.0 * (srcTime) / (clipEnd - clipStart);
                    case PlayableTimeSpace.BeforeClipEnd:
                        return 100.0 * (clipEnd - srcTime - clipStart) / (clipEnd - clipStart);
                    case PlayableTimeSpace.Absolute:
                        return 100.0 * (srcTime - clipStart) / (clipEnd - clipStart);
                }
            }
            else if (dstSpace == PlayableTimeSpace.Absolute)
            {
                switch (srcSpace)
                {
                    case PlayableTimeSpace.AfterClipStart:
                        return clipStart + srcTime;
                    case PlayableTimeSpace.BeforeClipEnd:
                        return clipEnd - srcTime;
                    case PlayableTimeSpace.Percentage:
                        return clipStart + (clipEnd - clipStart) * (srcTime / 100.0);
                }
            }

            //Other conversion
            throw new NotImplementedException(srcSpace + " to " + dstSpace);
        }
    }

    enum PlayableTimeSpace
    {
        AfterClipStart,
        BeforeClipEnd,
        Percentage,
        Absolute
    }

    [Serializable]
    struct VisualEffectPlayableSerializedEvent
    {
        public Color editorColor;
        public double time;
        public PlayableTimeSpace timeSpace;
        public ExposedProperty name;
        public EventAttributes eventAttributes;
    }

    [Serializable]
    struct VisualEffectPlayableSerializedEventNoColor
    {
        public double time;
        public PlayableTimeSpace timeSpace;
        public ExposedProperty name;
        public EventAttributes eventAttributes;

        public static implicit operator VisualEffectPlayableSerializedEvent(VisualEffectPlayableSerializedEventNoColor evt)
        {
            return new VisualEffectPlayableSerializedEvent()
            {
                time = evt.time,
                timeSpace = evt.timeSpace,
                name = evt.name,
                eventAttributes = evt.eventAttributes
            };
        }
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
