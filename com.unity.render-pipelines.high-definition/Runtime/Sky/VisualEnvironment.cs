using System;
using System.Diagnostics;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Visual Environment Volume Component.
    /// This component setups the sky used for rendering as well as the way ambient probe should be computed.
    /// </summary>
    [Serializable, VolumeComponentMenu("Visual Environment")]
    public sealed class VisualEnvironment : VolumeComponent
    {
        /// <summary>Type of sky that should be used for rendering.</summary>
        public IntParameter skyType = new IntParameter(0);
        /// <summary>Defines the way the ambient probe should be computed.</summary>
        public SkyAmbientModeParameter skyAmbientMode = new SkyAmbientModeParameter(SkyAmbientMode.Static);

        // Deprecated, kept for migration
        [SerializeField]
        internal FogTypeParameter fogType = new FogTypeParameter(FogType.None);
    }

    /// <summary>
    /// Informative enumeration containing SkyUniqeIDs already used by HDRP.
    /// When users write their own sky type, they can use any ID not present in this enumeration or in their project.
    /// </summary>
    public enum SkyType
    {
        /// <summary>HDRI Sky Unique ID.</summary>
        HDRI = 1,
        /// <summary>Procedural Sky Unique ID.</summary>
        Procedural = 2,
        /// <summary>Gradient Sky Unique ID.</summary>
        Gradient = 3,
        /// <summary>Physically Based Sky Unique ID.</summary>
        PhysicallyBased = 4,
    }

    /// <summary>
    /// Sky Ambient Mode.
    /// </summary>
    public enum SkyAmbientMode
    {
        /// <summary>HDRP will use the static lighting sky setup in the lighting panel to compute the global ambient probe.</summary>
        Static,
        /// <summary>HDRP will use the current sky used for lighting (either the one setup in the Visual Environment component or the Sky Lighting Override) to compute the global ambient probe.</summary>
        Dynamic,
    }

    /// <summary>
    /// Sky Ambient Mode volume parameter.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class SkyAmbientModeParameter : VolumeParameter<SkyAmbientMode>
    {
        /// <summary>
        /// Sky Ambient Mode volume parameter constructor.
        /// </summary>
        /// <param name="value">Sky Ambient Mode parameter.</param>
        /// <param name="overrideState">Initial override value.</param>
        public SkyAmbientModeParameter(SkyAmbientMode value, bool overrideState = false)
            : base(value, overrideState) { }
    }
}
