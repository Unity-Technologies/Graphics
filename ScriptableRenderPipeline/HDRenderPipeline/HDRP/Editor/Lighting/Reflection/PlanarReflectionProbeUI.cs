using System;
using UnityEditor.AnimatedValues;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class PlanarReflectionProbeUI : BaseUI<SerializedPlanarReflectionProbe>
    {
        const int k_AnimBoolFields = 4;
        static readonly int k_ReflectionProbeModeModeCount = Enum.GetValues(typeof(ReflectionProbeMode)).Length;
        static readonly int k_AnimBoolTotal = k_AnimBoolFields + k_ReflectionProbeModeModeCount;

        public InfluenceVolumeUI influenceVolume = new InfluenceVolumeUI();
        public FrameSettingsUI frameSettings = new FrameSettingsUI();
        public ReflectionProxyVolumeComponentUI reflectionProxyVolume = new ReflectionProxyVolumeComponentUI();

        public AnimBool isSectionExpandedInfluenceSettings { get { return m_AnimBools[k_ReflectionProbeModeModeCount]; } }
        public AnimBool isSectionExpandedCaptureSettings { get { return m_AnimBools[k_ReflectionProbeModeModeCount + 1]; } }

        public AnimBool isSectionExpandedCaptureMirrorSettings { get { return m_AnimBools[k_ReflectionProbeModeModeCount + 2]; } }
        public AnimBool isSectionExpandedCaptureStaticSettings { get { return m_AnimBools[k_ReflectionProbeModeModeCount + 3]; } }

        public bool showCaptureHandles { get; set; }

        public PlanarReflectionProbeUI()
            : base(k_AnimBoolTotal)
        {
            
        }

        public AnimBool IsSectionExpandedReflectionProbeMode(ReflectionProbeMode mode)
        {
            return m_AnimBools[(int)mode];
        }

        public override void Reset(SerializedPlanarReflectionProbe data, UnityAction repaint)
        {
            reflectionProxyVolume.Reset(data.reflectionProxyVolume, repaint);
            frameSettings.Reset(data.frameSettings, repaint);
            influenceVolume.Reset(data.influenceVolume, repaint);
            base.Reset(data, repaint);
        }

        public override void Update()
        {
            for (var i = 0; i < k_ReflectionProbeModeModeCount; i++)
                m_AnimBools[i].target = i == data.mode.intValue;

            isSectionExpandedCaptureMirrorSettings.target = data.isMirrored;
            isSectionExpandedCaptureStaticSettings.target = !data.isMirrored;

            reflectionProxyVolume.Update();
            frameSettings.Update();
            influenceVolume.Update();
            base.Update();
        }
    }
}
