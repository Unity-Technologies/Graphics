using System;
using System.Diagnostics;

namespace UnityEngine.Rendering.HighDefinition
{
    // Keep this class first in the file. Otherwise it seems that the script type is not registered properly.
    [Serializable, VolumeComponentMenu("Visual Environment")]
    public sealed class VisualEnvironment : VolumeComponent
    {
        public IntParameter skyType = new IntParameter(0);
        public SkyAmbientModeParameter skyAmbientMode = new SkyAmbientModeParameter(SkyAmbientMode.Static);

        // Deprecated, kept for migration
        public FogTypeParameter fogType = new FogTypeParameter(FogType.None);
    }

    // This enum is just here to centralize UniqueID values for skies provided with HDRP
    public enum SkyType
    {
        HDRI = 1,
        Procedural = 2,
        Gradient = 3,
        PhysicallyBased = 4,
    }

    public enum SkyAmbientMode
    {
        Static,
        Dynamic,
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class SkyAmbientModeParameter : VolumeParameter<SkyAmbientMode>
    {
        public SkyAmbientModeParameter(SkyAmbientMode value, bool overrideState = false)
            : base(value, overrideState) { }
    }
}
