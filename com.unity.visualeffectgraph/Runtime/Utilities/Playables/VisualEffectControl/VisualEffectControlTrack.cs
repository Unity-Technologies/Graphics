#if VFX_HAS_TIMELINE
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UnityEngine.VFX
{
    [TrackColor(1.0f, 0.0f, 1.0f)]
    [TrackClipType(typeof(VisualEffectControlPlayableAsset))]
    [TrackBindingType(typeof(VisualEffect))]
    public class VisualEffectControlTrack : TrackAsset
    {
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            foreach (var clip in GetClips())
            {
                var customClip = clip.asset as VisualEffectControlPlayableAsset;
                if (customClip != null)
                {
                    if (VisualEffectControlPlayableAsset.useBlending_WIP)
                    {
                        if (clip.easeInDuration != 0.0)
                        {
#if UNITY_EDITOR
                            //TODOPAUL : Doesn't update the timeline view
                            UnityEditor.EditorUtility.SetDirty(clip.GetParentTrack());
#endif
                            clip.easeInDuration = 0.0;
                        }
                    }

                    customClip.clipStart = clip.start;
                    customClip.clipEnd = clip.end;
                    var easeIn = clip.hasBlendIn ? clip.blendInDuration : clip.easeInDuration;
                    var easeOut = clip.hasBlendOut ? clip.blendOutDuration : clip.easeOutDuration;

                    customClip.SetDefaultEvent(easeIn, easeOut);
                }
            }

            return ScriptPlayable<VisualEffectControlTrackMixerBehaviour>.Create(graph, inputCount);
        }

        public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            VisualEffect trackBinding = director.GetGenericBinding(this) as VisualEffect;
            if (trackBinding == null)
                return;

            // The field names are the name of the backing serializable field. These can be found from the class source,
            // or from the unity scene file that contains an object of that type.
            //TODOPAUL: Check if needed
            //driver.AddFromName<TMP_Text>(trackBinding.gameObject, "m_text");
            //driver.AddFromName<TMP_Text>(trackBinding.gameObject, "m_fontSize");
            //driver.AddFromName<TMP_Text>(trackBinding.gameObject, "m_fontColor");

            base.GatherProperties(director, driver);
        }
    }
}
#endif
