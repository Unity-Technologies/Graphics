using System;
using UnityEditor.AnimatedValues;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class PlanarReflectionProbeUI : BaseUI<SerializedPlanarReflectionProbe>
    {
        const int k_AnimBoolFields = 2;
        static readonly int k_ReflectionProbeModeModeCount = Enum.GetValues(typeof(ReflectionProbeMode)).Length;
        static readonly int k_AnimBoolTotal = k_AnimBoolFields + k_ReflectionProbeModeModeCount;

        public InfluenceVolumeUI influenceVolume = new InfluenceVolumeUI();
        public FrameSettingsUI frameSettings = new FrameSettingsUI();
        public ProxyVolumeComponentUI proxyVolume = new ProxyVolumeComponentUI();

        public AnimBool isSectionExpandedInfluenceSettings { get { return m_AnimBools[k_ReflectionProbeModeModeCount]; } }
        public AnimBool isSectionExpandedCaptureSettings { get { return m_AnimBools[k_ReflectionProbeModeModeCount + 1]; } }

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
            proxyVolume.Reset(data.proxyVolume, repaint);
            frameSettings.Reset(data.frameSettings, repaint);
            influenceVolume.Reset(data.influenceVolume, repaint);
            base.Reset(data, repaint);
        }

        public override void Update()
        {
            for (var i = 0; i < k_ReflectionProbeModeModeCount; i++)
                m_AnimBools[i].target = i == data.mode.intValue;

            proxyVolume.Update();
            frameSettings.Update();
            influenceVolume.Update();
            base.Update();
        }
    }
}
