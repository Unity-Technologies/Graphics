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

        // public MinFloatParameter maxBlurRadius = new MinFloatParameter(5000.0f, 0.0f);
        [HideInInspector]
        public MinIntParameter blurLevel = new MinIntParameter(4, 1);
        /// <summary>Controls the maximum mip map HDRP uses for mip fog (0 is the lowest mip and 1 is the highest mip).</summary>
        public FloatParameter densityScaling = new FloatParameter(1);
        public FloatParameter depthScaling = new FloatParameter(0);
        public MinFloatParameter densityThreshold = new MinFloatParameter(0, 0);
        public MinFloatParameter densityPower = new MinFloatParameter(20, 0);
        public MinFloatParameter blurRadius = new MinFloatParameter(8, 0);
        [HideInInspector]
        public MinFloatParameter softKnee = new MinFloatParameter(1, 0);
        [HideInInspector]
        public MinFloatParameter scatteringStartDistance = new MinFloatParameter(0, 0);

        public Vector4 densityCurve
        {
            get
            {
                float knee = densityThreshold.value * softKnee.value + 1e-5f;
                return new Vector4(densityThreshold.value - knee, knee * 2, 0.25f / knee, 0);
            }
        }
        [HideInInspector]
        public BoolParameter antiFlicker = new BoolParameter(false);
    }
}
