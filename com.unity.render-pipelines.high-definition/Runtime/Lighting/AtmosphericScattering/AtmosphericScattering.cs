using System;
using System.Diagnostics;

namespace UnityEngine.Rendering.HighDefinition
{
    // Deprecated, kept for migration
    abstract class AtmosphericScattering : VolumeComponent
    {
        // Fog Color
        public FogColorParameter colorMode = new FogColorParameter(FogColorMode.SkyColor);
        [Tooltip("Specifies the constant color of the fog.")]
        public ColorParameter color = new ColorParameter(Color.grey, hdr: true, showAlpha: false, showEyeDropper: true);
        [Tooltip("Specifies the tint of the fog.")]
        public ColorParameter tint = new ColorParameter(Color.white, hdr: true, showAlpha: false, showEyeDropper: true);
        [Tooltip("Controls the overall density of the fog. Acts as a global multiplier.")]
        public ClampedFloatParameter density = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        [Tooltip("Sets the maximum fog distance HDRP uses when it shades the skybox or the Far Clipping Plane of the Camera.")]
        public MinFloatParameter maxFogDistance = new MinFloatParameter(5000.0f, 0.0f);
        [Tooltip("Controls the maximum mip map HDRP uses for mip fog (0 is the lowest mip and 1 is the highest mip).")]
        public ClampedFloatParameter mipFogMaxMip = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        [Tooltip("Sets the distance at which HDRP uses the minimum mip image of the blurred sky texture as the fog color.")]
        public MinFloatParameter mipFogNear = new MinFloatParameter(0.0f, 0.0f);
        [Tooltip("Sets the distance at which HDRP uses the maximum mip image of the blurred sky texture as the fog color.")]
        public MinFloatParameter mipFogFar = new MinFloatParameter(1000.0f, 0.0f);

        internal abstract void PushShaderParameters(HDCamera hdCamera, CommandBuffer cmd);
    }

    // Deprecated, kept for migration
    internal enum FogType
    {
        None = 0,
        Exponential = 2,
        Volumetric = 3,
    }
}
