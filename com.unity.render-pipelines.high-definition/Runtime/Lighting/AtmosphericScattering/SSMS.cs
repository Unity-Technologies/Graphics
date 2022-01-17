using System;
using System.Diagnostics;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// SSMS Volume Component.
    /// </summary>
    [Serializable, VolumeComponentMenuForRenderPipeline("SSMS", typeof(HDRenderPipeline))]
    [HDRPHelpURLAttribute("Override-SSMS")]
    public class SSMS : VolumeComponentWithQuality
    {
        /// <summary>Enable fog.</summary>
        [Tooltip("Enables the SSMS.")]
        public BoolParameter enabled = new BoolParameter(false);

        /// <summary>SSMS color.</summary>
        // [Tooltip("Specifies the constant color of the fog.")]
        // public ColorParameter color = new ColorParameter(Color.grey, hdr: true, showAlpha: false, showEyeDropper: true);

        public MinFloatParameter maxBlurRadius = new MinFloatParameter(5000.0f, 0.0f);
        /// <summary>Controls the maximum mip map HDRP uses for mip fog (0 is the lowest mip and 1 is the highest mip).</summary>

        public BoolParameter antiFlicker = new BoolParameter(false);
    }
}
