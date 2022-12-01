#if VFX_HAS_TIMELINE
using System;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
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

#if UNITY_EDITOR
        private VisualEffect m_CurrentVisualEffect;
#endif

        public bool IsUpToDate()
        {
            return m_VFXVersion == kCurrentVersion;
        }

        protected override void OnBeforeTrackSerialize()
        {
            base.OnBeforeTrackSerialize();

            if (GetClips().All(x => x.asset is VisualEffectControlClip))
            {
                m_VFXVersion = kCurrentVersion;
            }
        }

#if UNITY_EDITOR
        protected override void OnCreateClip(TimelineClip clip)
        {
            base.OnCreateClip(clip);
            if (clip.asset is VisualEffectControlClip vfxClip)
            {
                vfxClip.clipStart = clip.start;
                vfxClip.clipEnd = clip.end;

                if (m_CurrentVisualEffect != null)
                {
                    //Copy Seed settings
                    vfxClip.startSeed = m_CurrentVisualEffect.startSeed;

                    //Copy Prewarm settings
                    vfxClip.prewarm.eventName = m_CurrentVisualEffect.initialEventName;
                    if (m_CurrentVisualEffect.visualEffectAsset != null)
                    {
                        using var resourceObject = new SerializedObject(m_CurrentVisualEffect.visualEffectAsset);
                        var prewarmDeltaTime = resourceObject.FindProperty("m_Infos.m_PreWarmDeltaTime");
                        var prewarmStepCount = resourceObject.FindProperty("m_Infos.m_PreWarmStepCount");
                        if (prewarmDeltaTime != null && prewarmStepCount != null && prewarmStepCount.uintValue != 0u)
                        {
                            vfxClip.prewarm.stepCount = prewarmStepCount.uintValue;
                            vfxClip.prewarm.deltaTime = prewarmDeltaTime.floatValue;
                        }
                    }
                }
            }
            else
            {
                throw new InvalidOperationException("Unexpected clip added : " + clip.asset);
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
                    Debug.LogErrorFormat("Unexpected clip type : {0} in timeline '{1}'. Please reimport this playable.", clip, UnityEditor.AssetDatabase.GetAssetPath(timelineAsset));
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
#if UNITY_EDITOR
            m_CurrentVisualEffect = null;
#endif

            if (director.GetGenericBinding(this) is VisualEffect vfx)
            {
#if UNITY_EDITOR
                m_CurrentVisualEffect = vfx;
#endif
                base.GatherProperties(director, driver);
            }
        }
    }
}
#endif
