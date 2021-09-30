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
        // Creates a runtime instance of the track, represented by a PlayableBehaviour.
        // The runtime instance performs mixing on the timeline clips.
        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            return ScriptPlayable<VisualEffectControlTrackMixerBehaviour>.Create(graph, inputCount);
        }

        // Invoked by the timeline editor to put properties into preview mode. This permits the timeline
        // to temporarily change fields for the purpose of previewing in EditMode.
        public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            VisualEffect trackBinding = director.GetGenericBinding(this) as VisualEffect;
            if (trackBinding == null)
                return;

            // The field names are the name of the backing serializable field. These can be found from the class source,
            // or from the unity scene file that contains an object of that type.
            //driver.AddFromName<TMP_Text>(trackBinding.gameObject, "m_text");
            //driver.AddFromName<TMP_Text>(trackBinding.gameObject, "m_fontSize");
            //driver.AddFromName<TMP_Text>(trackBinding.gameObject, "m_fontColor");

            base.GatherProperties(director, driver);
        }
    }
}
#endif
