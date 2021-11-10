#if VFX_HAS_TIMELINE
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

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

        public bool IsUpToDate()
        {
            return m_VFXVersion == kCurrentVersion;
        }

        protected override void OnBeforeTrackSerialize()
        {
            base.OnBeforeTrackSerialize();

            bool allClipAreControl = true;
            foreach (var clip in GetClips())
            {
                if (!(clip.asset is VisualEffectControlClip))
                {
                    allClipAreControl = false;
                    break;
                }
            }

            if (allClipAreControl)
            {
                m_VFXVersion = kCurrentVersion;
            }
        }

#if UNITY_EDITOR
        public VisualEffectControlTrackMixerBehaviour lastCreatedMixer { get; private set; }
#endif

        public override Playable CreateTrackMixer(PlayableGraph graph, GameObject go, int inputCount)
        {
            foreach (var clip in GetClips())
            {
                var customClip = clip.asset as VisualEffectControlClip;
                if (customClip != null)
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
            var reinitBinding = false;
            var reinitUnbinding = false;
            switch (reinit)
            {
                case ReinitMode.None:
                    reinitBinding = false;
                    reinitUnbinding = false;
                    break;
                case ReinitMode.OnBindingDisable:
                    reinitBinding = false;
                    reinitUnbinding = true;
                    break;
                case ReinitMode.OnBindingEnable:
                    reinitBinding = true;
                    reinitUnbinding = false;
                    break;
                case ReinitMode.OnBindingEnableOrDisable:
                    reinitBinding = true;
                    reinitUnbinding = true;
                    break;
            }
            behaviour.Init(reinitBinding, reinitUnbinding);

#if UNITY_EDITOR
            lastCreatedMixer = behaviour;
#endif
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
