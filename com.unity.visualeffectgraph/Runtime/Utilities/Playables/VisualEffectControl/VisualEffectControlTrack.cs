#if VFX_HAS_TIMELINE
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace UnityEngine.VFX
{
    [TrackColor(0.5990566f, 0.9038978f, 1f)]
    [TrackClipType(typeof(VisualEffectControlPlayableAsset))]
    [TrackBindingType(typeof(VisualEffect))]
    class VisualEffectControlTrack : TrackAsset
    {
        //Only for debug purpose
        public VisualEffectControlTrackMixerBehaviour lastCreatedMixer { get; private set; }

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            foreach (var clip in GetClips())
            {
                var customClip = clip.asset as VisualEffectControlPlayableAsset;
                if (customClip != null)
                {
                    customClip.clipStart = clip.start;
                    customClip.clipEnd = clip.end;
                }
            }

            var mixer = ScriptPlayable<VisualEffectControlTrackMixerBehaviour>.Create(graph, inputCount);
            lastCreatedMixer = mixer.GetBehaviour();
            return mixer;
        }

        public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            VisualEffect trackBinding = director.GetGenericBinding(this) as VisualEffect;
            if (trackBinding == null)
                return;
            base.GatherProperties(director, driver);
        }
    }
}
#endif
