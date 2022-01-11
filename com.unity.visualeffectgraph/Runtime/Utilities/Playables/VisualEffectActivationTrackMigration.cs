#if VFX_HAS_TIMELINE && UNITY_EDITOR
using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using UnityEngine.VFX;

namespace UnityEditor.VFX.Migration
{
    static class ActivationToControlTrack
    {
        static EventAttribute MigrateEventStateToAttributes(VisualEffectActivationBehaviour.EventState eventState)
        {
            var name = (string)eventState.attribute;
            switch (eventState.type)
            {
                case VisualEffectActivationBehaviour.AttributeType.Float:
                    return new EventAttributeFloat()
                    {
                        id = name,
                        value = eventState.values[0]
                    };
                case VisualEffectActivationBehaviour.AttributeType.Float2:
                    return new EventAttributeVector2()
                    {
                        id = name,
                        value = new Vector2(eventState.values[0], eventState.values[1])
                    };
                case VisualEffectActivationBehaviour.AttributeType.Float3:
                    if (name == "color")
                    {
                        return new EventAttributeColor()
                        {
                            id = name,
                            value = new Vector3(eventState.values[0], eventState.values[1], eventState.values[2])
                        };
                    }
                    return new EventAttributeVector3()
                    {
                        id = name,
                        value = new Vector3(eventState.values[0], eventState.values[1], eventState.values[2])
                    };
                case VisualEffectActivationBehaviour.AttributeType.Float4:
                    return new EventAttributeVector4()
                    {
                        id = name,
                        value = new Vector4(eventState.values[0], eventState.values[1], eventState.values[2], eventState.values[3])
                    };
                case VisualEffectActivationBehaviour.AttributeType.Int32:
                    return new EventAttributeInt()
                    {
                        id = name,
                        value = (int)eventState.values[0]
                    };
                case VisualEffectActivationBehaviour.AttributeType.Uint32:
                    return new EventAttributeUInt()
                    {
                        id = name,
                        value = (uint)eventState.values[0]
                    };
                case VisualEffectActivationBehaviour.AttributeType.Boolean:
                    return new EventAttributeBool()
                    {
                        id = name,
                        value = eventState.values[0] != 0.0f
                    };
            }
            return null;
        }

        static EventAttributes MigrateEventStateToAttributes(VisualEffectActivationBehaviour.EventState[] eventStates)
        {
            EventAttributes eventAttributes = new EventAttributes();
            if (eventStates == null)
                return eventAttributes;

            if (eventStates.Length == 0)
                return eventAttributes;

            eventAttributes.content = eventStates.Select(o => MigrateEventStateToAttributes(o)).ToArray();
            return eventAttributes;
        }

        public static void SanitizeActivationToControl(VisualEffectControlTrack invalidTrack)
        {
            var toDeleteClip = new List<TimelineClip>();
            foreach (var oldClip in invalidTrack.GetClips())
            {
                if (oldClip.asset is VisualEffectControlClip)
                    continue; //Already sanitized

                //The previous implementation wasn't reinit the VFX
                invalidTrack.reinit = VisualEffectControlTrack.ReinitMode.None;

                var newClip = invalidTrack.CreateClip<VisualEffectControlClip>();
                newClip.start = oldClip.start;
                newClip.duration = oldClip.duration;

                var newAsset = newClip.asset as VisualEffectControlClip;
                var oldAsset = oldClip.asset as VisualEffectActivationClip;

                newAsset.clipStart = oldClip.start;
                newAsset.clipEnd = oldClip.end;

                //Equivalent of the previous VisualEffectActivationClip behavior, no scrubbing, no reinit, only activation
                newAsset.prewarm.enable = false;
                newAsset.reinit = VisualEffectControlClip.ReinitMode.None;
                newAsset.scrubbing = false;

                if (oldAsset == null)
                    throw new NullReferenceException("Unable to read VisualEffectActivationClip");

                if (oldAsset.activationBehavior == null)
                    throw new NullReferenceException("Unable to read VisualEffectActivationBehaviour");

                newAsset.clipEvents = new List<VisualEffectControlClip.ClipEvent>()
                {
                    new VisualEffectControlClip.ClipEvent()
                    {
                        editorColor = VisualEffectControlClip.ClipEvent.defaultEditorColor,
                        enter = new VisualEffectPlayableSerializedEventNoColor()
                        {
                            name = (string)oldAsset.activationBehavior.onClipEnter,
                            time = 0.0,
                            timeSpace = PlayableTimeSpace.AfterClipStart,
                            eventAttributes = MigrateEventStateToAttributes(oldAsset.activationBehavior.clipEnterEventAttributes)
                        },

                        exit = new VisualEffectPlayableSerializedEventNoColor()
                        {
                            name = (string)oldAsset.activationBehavior.onClipExit,
                            time = 0.0,
                            timeSpace = PlayableTimeSpace.BeforeClipEnd,
                            eventAttributes = MigrateEventStateToAttributes(oldAsset.activationBehavior.clipExitEventAttributes)
                        }
                    }
                };
                toDeleteClip.Add(oldClip);
            }

            foreach (var deprecatedClip in toDeleteClip)
            {
                invalidTrack.timelineAsset.DeleteClip(deprecatedClip);
            }
        }
    }
}
#endif
