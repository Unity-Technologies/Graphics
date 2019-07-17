using System;
using System.Diagnostics;

namespace UnityEngine.Rendering.HighDefinition
{
    // Keep this class first in the file. Otherwise it seems that the script type is not registered properly.
    public abstract class AtmosphericScattering : VolumeComponent
    {
        // Fog Color
        static readonly int m_ColorModeParam = Shader.PropertyToID("_FogColorMode");
        static readonly int m_FogColorDensityParam = Shader.PropertyToID("_FogColorDensity");
        static readonly int m_MipFogParam = Shader.PropertyToID("_MipFogParameters");

        // Fog Color
        public FogColorParameter     colorMode = new FogColorParameter(FogColorMode.SkyColor);
        [Tooltip("Specifies the constant color of the fog.")]
        public ColorParameter        color = new ColorParameter(Color.grey, hdr: true, showAlpha: false, showEyeDropper: true);
        [Tooltip("Controls the overall density of the fog. Acts as a global multiplier.")]
        public ClampedFloatParameter density = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        [Tooltip("Sets the maximum fog distance HDRP uses when it shades the skybox or the Far Clipping Plane of the Camera.")]
        public MinFloatParameter     maxFogDistance = new MinFloatParameter(5000.0f, 0.0f);
        [Tooltip("Controls the maximum mip map HDRP uses for mip fog (0 is the lowest mip and 1 is the highest mip).")]
        public ClampedFloatParameter mipFogMaxMip = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        [Tooltip("Sets the distance at which HDRP uses the minimum mip image of the blurred sky texture as the fog color.")]
        public MinFloatParameter     mipFogNear = new MinFloatParameter(0.0f, 0.0f);
        [Tooltip("Sets the distance at which HDRP uses the maximum mip image of the blurred sky texture as the fog color.")]
        public MinFloatParameter     mipFogFar = new MinFloatParameter(1000.0f, 0.0f);

        internal abstract void PushShaderParameters(HDCamera hdCamera, CommandBuffer cmd);

        internal void PushShaderParametersCommon(HDCamera hdCamera, CommandBuffer cmd, FogType type)
        {
            Debug.Assert(hdCamera.frameSettings.IsEnabled(FrameSettingsField.AtmosphericScattering));

            int physicallyBasedSkyAtmosphereFlag = 0;

            var visualEnvironment = VolumeManager.instance.stack.GetComponent<VisualEnvironment>();
            Debug.Assert(visualEnvironment != null);

            // The PBR sky contributes to atmospheric scattering.
            physicallyBasedSkyAtmosphereFlag = visualEnvironment.skyType.value == (int)SkyType.PhysicallyBased ? 128 : 0;

            cmd.SetGlobalInt(HDShaderIDs._AtmosphericScatteringType, physicallyBasedSkyAtmosphereFlag | (int)type);
            cmd.SetGlobalFloat(HDShaderIDs._MaxFogDistance, maxFogDistance.value);

            // Fog Color
            cmd.SetGlobalFloat(m_ColorModeParam, (float)colorMode.value);
            cmd.SetGlobalColor(m_FogColorDensityParam, new Color(color.value.r, color.value.g, color.value.b, density.value));
            cmd.SetGlobalVector(m_MipFogParam, new Vector4(mipFogNear.value, mipFogFar.value, mipFogMaxMip.value, 0.0f));
        }
    }

    [GenerateHLSL]
    public enum FogType // 7 bits max, 8th bit is for the PBR sky atmosphere flag
    {
        None,
        Linear,
        Exponential,
        Volumetric,
    }

    [GenerateHLSL]
    public enum FogColorMode
    {
        ConstantColor,
        SkyColor,
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class FogTypeParameter : VolumeParameter<FogType>
    {
        public FogTypeParameter(FogType value, bool overrideState = false)
            : base(value, overrideState) {}
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class FogColorParameter : VolumeParameter<FogColorMode>
    {
        public FogColorParameter(FogColorMode value, bool overrideState = false)
            : base(value, overrideState) {}
    }
}
