#if VFX_HAS_TIMELINE
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
#if UNITY_EDITOR
using UnityEditor.VFX.Migration;
#endif

namespace UnityEngine.VFX
{
    [TrackColor(0.5990566f, 0.9038978f, 1f)]
    [TrackClipType(typeof(VisualEffectControlClip))]
    [TrackBindingType(typeof(VisualEffect))]
    class VisualEffectControlTrack : TrackAsset
    {
        //0: Initial
        //1: VisualEffectActivationTrack which contains VisualEffectActivationClip => VisualEffectControlTrack with VisualEffectControlClip
        const int kCurrentVersion = 1;
        [SerializeField, HideInInspector]
        int m_VFXVersion;

        public enum ReinitMode
        {
            None,
            OnBindingEnable,
            OnBindingDisable,
            OnBindingEnableOrDisable
        }

        [SerializeField, NotKeyable]
        public ReinitMode reinit = ReinitMode.OnBindingEnableOrDisable;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_VFXVersion != kCurrentVersion)
            {
                try
                {
                    ActivationToControlTrack.SanitizeActivationToControl(this);
                    m_VFXVersion = kCurrentVersion;
                }
                catch (System.Exception e)
                {
                    //Workaround, we will retry until it's possible
                    Debug.LogFormat("Fail to update controlTrack\n{0}", e.ToString());
                }
            }
        }

        protected override void OnBeforeTrackSerialize()
        {
            base.OnBeforeTrackSerialize();

            if (GetClips().All(x => x.asset is VisualEffectControlClip))
            {
                m_VFXVersion = kCurrentVersion;
            }
        }
#endif

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            foreach (var clip in GetClips())
            {
                if (clip.asset is VisualEffectControlClip customClip)
                {
                    customClip.clipStart = clip.start;
                    customClip.clipEnd = clip.end;
                }
#if UNITY_EDITOR
                else
                {
                    Debug.LogErrorFormat("Unexpected clip type : {0} in timeline '{1}'", clip, UnityEditor.AssetDatabase.GetAssetPath(timelineAsset));
                }
#endif
            }

            var mixer = ScriptPlayable<VisualEffectControlTrackMixerBehaviour>.Create(graph, inputCount);
            var behaviour = mixer.GetBehaviour();
            var reinitBinding = reinit == ReinitMode.OnBindingEnable || reinit == ReinitMode.OnBindingEnableOrDisable;
            var reinitUnbinding = reinit == ReinitMode.OnBindingDisable || reinit == ReinitMode.OnBindingEnableOrDisable;
            behaviour.Init(this, reinitBinding, reinitUnbinding);
            return mixer;
        }

        public override void GatherProperties(PlayableDirector director, IPropertyCollector driver)
        {
            if (director.GetGenericBinding(this) is VisualEffect)
            {
                base.GatherProperties(director, driver);
            }
        }
    }
}
#endif
