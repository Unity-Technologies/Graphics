using System;
using System.Diagnostics;

namespace UnityEngine.Rendering.Universal
{
    [Serializable, VolumeComponentMenu("Visual Environment")]
    public sealed class VisualEnvironment : VolumeComponent
    {
        public IntParameter skyType = new IntParameter(0);
        public SkyAmbientModeParameter skyAmbientMode = new SkyAmbientModeParameter(SkyAmbientMode.Static); // TODO Unused
    }

    public enum SkyAmbientMode
    {
        Static,
        Dynamic,
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class SkyAmbientModeParameter : VolumeParameter<SkyAmbientMode>
    {
        public SkyAmbientModeParameter(SkyAmbientMode mode, bool overrideState = false)
            : base(mode, overrideState) { }
    }
}
