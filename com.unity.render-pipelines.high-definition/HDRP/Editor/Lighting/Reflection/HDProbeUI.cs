using System;
using UnityEditor.AnimatedValues;
using UnityEngine.Events;
using UnityEngine.Experimental.Rendering.HDPipeline;
using UnityEngine.Rendering;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class HDProbeUI : BaseUI<SerializedHDProbe>
    {
        const int k_AnimBoolFields = 6;
        static readonly int k_ReflectionProbeModeModeCount = Enum.GetValues(typeof(ReflectionProbeMode)).Length;
        static readonly int k_AnimBoolTotal = k_AnimBoolFields + k_ReflectionProbeModeModeCount;

        public InfluenceVolumeUI influenceVolume = new InfluenceVolumeUI();
        public FrameSettingsUI frameSettings = new FrameSettingsUI();
        public ReflectionProxyVolumeComponentUI reflectionProxyVolume = new ReflectionProxyVolumeComponentUI();

        public AnimBool isSectionExpandedInfluenceSettings { get { return m_AnimBools[k_ReflectionProbeModeModeCount]; } }
        public AnimBool isSectionExpandedCaptureSettings { get { return m_AnimBools[k_ReflectionProbeModeModeCount + 1]; } }

        public AnimBool isSectionExpandedCaptureMirrorSettings { get { return m_AnimBools[k_ReflectionProbeModeModeCount + 2]; } }
        public AnimBool isSectionExpandedCaptureStaticSettings { get { return m_AnimBools[k_ReflectionProbeModeModeCount + 3]; } }
        public AnimBool isSectionExpendedProxyVolume { get { return m_AnimBools[k_ReflectionProbeModeModeCount + 4]; } }
        public AnimBool isSectionExpendedAdditionalSettings { get { return m_AnimBools[k_ReflectionProbeModeModeCount + 5]; } }

        public bool showCaptureHandles { get; set; }

        internal HDProbeUI CreateFor(HDProbe p)
        {
            if (p is PlanarReflectionProbe)
                return new PlanarReflectionProbeUI();
            else
                return new HDProbeUI();
        }

        public HDProbeUI()
            : base(k_AnimBoolTotal)
        {
            isSectionExpandedInfluenceSettings.value = true;
            isSectionExpandedCaptureSettings.value = true;
            isSectionExpendedProxyVolume.value = true;
            isSectionExpendedAdditionalSettings.value = false;
        }

        public AnimBool IsSectionExpandedReflectionProbeMode(ReflectionProbeMode mode)
        {
            return m_AnimBools[(int)mode];
        }

        public override void Reset(SerializedHDProbe data, UnityAction repaint)
        {
            //reflectionProxyVolume.Reset(data.proxyVolumeComponent, repaint);
            frameSettings.Reset(data.frameSettings, repaint);
            influenceVolume.Reset(data.influenceVolume, repaint);
            base.Reset(data, repaint);
        }

        public override void Update()
        {
            for (var i = 0; i < k_ReflectionProbeModeModeCount; i++)
                m_AnimBools[i].target = i == data.mode.intValue;

            //reflectionProxyVolume.Update();
            frameSettings.Update();
            influenceVolume.Update();
            base.Update();
        }
    }
}
