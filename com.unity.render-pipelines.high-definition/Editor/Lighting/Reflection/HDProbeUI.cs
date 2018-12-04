using System;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEditor.Experimental.Rendering.HDPipeline
{
    partial class HDProbeUI : IUpdateable<SerializedHDProbe>
    {
        [Flags]
        public enum Flag
        {
            None = 0,
            SectionExpandedInfluence = 1 << 0,
            SectionExpandedCapture = 1 << 1,
            SectionExpandedProjection = 1 << 2,
            SectionExpandedCustom = 1 << 3,
            SectionExpandedProbeModeBaked = 1 << 4,
            SectionExpandedProbeModeCustom = 1 << 5,
            SectionExpandedProbeModeRealtime = 1 << 6,
            SectionExpandedInfluenceShapeSphere = 1 << 7,
            SectionExpandedInfluenceShapeBox = 1 << 8,
            FrameSettingsOverriden = 1 << 9,
        }

        EditorPrefBoolFlags<Flag> m_FlagStorage = new EditorPrefBoolFlags<Flag>("HDProbeUI");

        public ReflectionProxyVolumeComponentUI reflectionProxyVolume = new ReflectionProxyVolumeComponentUI();
        public ProbeSettingsUI probeSettings = new ProbeSettingsUI();

        public bool HasFlag(Flag v) => m_FlagStorage.HasFlag(v);
        public bool SetFlag(Flag f, bool v)
        {
            m_FlagStorage.SetFlag(f, v);
            return v;
        }

        public void SetModeTarget(int value)
        {
            SetFlag(Flag.SectionExpandedProbeModeBaked, value == (int)ProbeSettings.Mode.Baked);
            SetFlag(Flag.SectionExpandedProbeModeCustom, value == (int)ProbeSettings.Mode.Custom);
            SetFlag(Flag.SectionExpandedProbeModeRealtime, value == (int)ProbeSettings.Mode.Realtime);
        }

        public void Update(SerializedHDProbe probe)
        {
            probeSettings.Reset(probe.probeSettings, null);
            probeSettings.Update();
            SetModeTarget(probe.probeSettings.mode.hasMultipleDifferentValues ? -1 : probe.probeSettings.mode.intValue);
        }
    }
}
