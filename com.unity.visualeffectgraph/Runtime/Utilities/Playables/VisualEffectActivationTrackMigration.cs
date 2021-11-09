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
    class ActivationToControlTrack : AssetPostprocessor
    {
        static IEnumerable<VisualEffectControlTrack> GetOutOfDateControlTrack(TimelineAsset timeline)
        {
            foreach (var track in timeline.GetOutputTracks())
            {
                if (track is VisualEffectControlTrack)
                {
                    var vfxTrack = track as VisualEffectControlTrack;
                    var hasOldClip = false;
                    foreach (var clip in vfxTrack.GetClips())
                    {
                        if (clip.asset is VisualEffectActivationClip)
                        {
                            hasOldClip = true;
                            break;
                        }
                    }

                    if (hasOldClip)
                        yield return vfxTrack;
                }
            }
        }

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

        static void SanitizeActivationToControl(TimelineAsset timelineAsset, VisualEffectControlTrack[] invalidTracks)
        {
            var toDeleteClip = new List<TimelineClip>();

            foreach (var invalidTrack in invalidTracks)
            {
                foreach (var oldClip in invalidTrack.GetClips())
                {
                    if (oldClip.asset is VisualEffectControlPlayableAsset)
                        continue; //Already sanitized

                    var newClip = invalidTrack.CreateClip<VisualEffectControlPlayableAsset>();
                    newClip.start = oldClip.start;
                    newClip.duration = oldClip.duration;

                    var newAsset = newClip.asset as VisualEffectControlPlayableAsset;
                    var oldAsset = oldClip.asset as VisualEffectActivationClip;

                    newAsset.clipStart = oldClip.start;
                    newAsset.clipEnd = oldClip.end;

                    //Equivalent of the previous VisualEffectActivationClip behavior, no scrubbing, no reinit, only activation
                    newAsset.prewarm.enable = false;
                    newAsset.reinit = VisualEffectControlPlayableAsset.ReinitMode.None;
                    newAsset.scrubbing = false;

                    newAsset.clipEvents = new List<VisualEffectControlPlayableAsset.ClipEvent>()
                    {
                        new VisualEffectControlPlayableAsset.ClipEvent()
                        {
                            enter = new VisualEffectPlayableSerializedEvent()
                            {
                                name = (string)oldAsset.activationBehavior.onClipEnter,
                                time = 0.0,
                                timeSpace = VisualEffectPlayableSerializedEvent.TimeSpace.AfterClipStart,
                                eventAttributes = MigrateEventStateToAttributes(oldAsset.activationBehavior.clipEnterEventAttributes)
                            },

                            exit = new VisualEffectPlayableSerializedEvent()
                            {
                                name = (string)oldAsset.activationBehavior.onClipExit,
                                time = 0.0,
                                timeSpace = VisualEffectPlayableSerializedEvent.TimeSpace.BeforeClipEnd,
                                eventAttributes = MigrateEventStateToAttributes(oldAsset.activationBehavior.clipExitEventAttributes)
                            }
                        }
                    };
                    newAsset.singleEvents = new List<VisualEffectPlayableSerializedEvent>();
                    toDeleteClip.Add(oldClip);
                }
            }

            foreach (var deprecatedClip in toDeleteClip)
            {
                timelineAsset.DeleteClip(deprecatedClip);
            }
        }

        static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            foreach (var str in importedAssets)
            {
                try
                {
                    if (str.EndsWith(".playable", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(str);
                        if (timeline != null)
                        {
                            var activationTracks = GetOutOfDateControlTrack(timeline);
                            if (activationTracks.Any())
                            {
                                SanitizeActivationToControl(timeline, activationTracks.ToArray());
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogErrorFormat("Failed to migrate VisualEffectActivationTrack: {0}\n{1}", str, e);
                }
            }
        }
    }
}
#endif
