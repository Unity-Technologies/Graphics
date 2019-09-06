using System;
using System.Diagnostics;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable, VolumeComponentMenu("Fog/Fog")]
    public class Fog : VolumeComponent
    {
        // Fog Color
        static readonly int m_ColorModeParam = Shader.PropertyToID("_FogColorMode");
        static readonly int m_FogColorParam = Shader.PropertyToID("_FogColor");
        static readonly int m_MipFogParam = Shader.PropertyToID("_MipFogParameters");

        [Tooltip("Enables the fog.")]
        public BoolParameter         enabled = new BoolParameter(false);

        // Fog Color
        public FogColorParameter     colorMode = new FogColorParameter(FogColorMode.SkyColor);
        [Tooltip("Specifies the constant color of the fog.")]
        public ColorParameter        color = new ColorParameter(Color.grey, hdr: true, showAlpha: false, showEyeDropper: true);
        [Tooltip("Sets the maximum fog distance HDRP uses when it shades the skybox or the Far Clipping Plane of the Camera.")]
        public MinFloatParameter     maxFogDistance = new MinFloatParameter(5000.0f, 0.0f);
        [Tooltip("Controls the maximum mip map HDRP uses for mip fog (0 is the lowest mip and 1 is the highest mip).")]
        public ClampedFloatParameter mipFogMaxMip = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        [Tooltip("Sets the distance at which HDRP uses the minimum mip image of the blurred sky texture as the fog color.")]
        public MinFloatParameter     mipFogNear = new MinFloatParameter(0.0f, 0.0f);
        [Tooltip("Sets the distance at which HDRP uses the maximum mip image of the blurred sky texture as the fog color.")]
        public MinFloatParameter     mipFogFar = new MinFloatParameter(1000.0f, 0.0f);

        // Height Fog
        public FloatParameter baseHeight = new FloatParameter(0.0f);
        public FloatParameter maximumHeight = new FloatParameter(50.0f);

        // Common Fog Parameters (Exponential/Volumetric)
        public ColorParameter albedo = new ColorParameter(Color.white);
        public MinFloatParameter meanFreePath = new MinFloatParameter(400.0f, 1.0f);

        // Optional Volumetric Fog
        public BoolParameter enableVolumetricFog = new BoolParameter(false);
        public ClampedFloatParameter anisotropy = new ClampedFloatParameter(0.0f, -1.0f, 1.0f);
        public ClampedFloatParameter globalLightProbeDimmer = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        [Tooltip("Sets the distance (in meters) from the Camera's Near Clipping Plane to the back of the Camera's volumetric lighting buffer.")]
        public MinFloatParameter depthExtent = new MinFloatParameter(64.0f, 0.1f);
        [Tooltip("Controls the distribution of slices along the Camera's focal axis. 0 is exponential distribution and 1 is linear distribution.")]
        public ClampedFloatParameter sliceDistributionUniformity = new ClampedFloatParameter(0.75f, 0, 1);

        public static bool IsFogEnabled(HDCamera hdCamera)
        {
            return hdCamera.frameSettings.IsEnabled(FrameSettingsField.AtmosphericScattering) && VolumeManager.instance.stack.GetComponent<Fog>().enabled.value;
        }

        public static bool IsVolumetricLightingEnabled(HDCamera hdCamera)
        {
            return hdCamera.frameSettings.IsEnabled(FrameSettingsField.Volumetrics) && VolumeManager.instance.stack.GetComponent<Fog>().enableVolumetricFog.value;
        }

        public static bool IsPBRFogEnabled(HDCamera hdCamera)
        {
            var visualEnv = VolumeManager.instance.stack.GetComponent<VisualEnvironment>();
            // For now PBR fog (coming from the PBR sky) is disabled until we improve it
            return false;
            //return (visualEnv.skyType.value == (int)SkyType.PhysicallyBased) && hdCamera.frameSettings.IsEnabled(FrameSettingsField.AtmosphericScattering);
        }


        static float ScaleHeightFromLayerDepth(float d)
        {
            // Exp[-d / H] = 0.001
            // -d / H = Log[0.001]
            // H = d / -Log[0.001]
            return d * 0.144765f;
        }

        public static void PushNeutralShaderParameters(CommandBuffer cmd)
        {
            cmd.SetGlobalInt(HDShaderIDs._FogEnabled, 0);
            cmd.SetGlobalInt(HDShaderIDs._EnableVolumetricFog, 0);
            cmd.SetGlobalVector(HDShaderIDs._HeightFogBaseScattering, Vector3.zero);
            cmd.SetGlobalFloat(HDShaderIDs._HeightFogBaseExtinction, 0.0f);
            cmd.SetGlobalVector(HDShaderIDs._HeightFogExponents, Vector2.one);
            cmd.SetGlobalFloat(HDShaderIDs._HeightFogBaseHeight, 0.0f);
            cmd.SetGlobalFloat(HDShaderIDs._GlobalFogAnisotropy, 0.0f);
        }

        public static void PushFogShaderParameters(HDCamera hdCamera, CommandBuffer cmd)
        {
            // TODO Handle user override
            var fogSettings = VolumeManager.instance.stack.GetComponent<Fog>();

            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.AtmosphericScattering) || !fogSettings.enabled.value)
            {
                PushNeutralShaderParameters(cmd);
                return;
            }

            fogSettings.PushShaderParameters(hdCamera, cmd);

            cmd.SetGlobalInt(HDShaderIDs._PBRFogEnabled, IsPBRFogEnabled(hdCamera) ? 1 : 0);
        }

        //internal abstract void PushShaderParameters(HDCamera hdCamera, CommandBuffer cmd);
        public virtual void PushShaderParameters(HDCamera hdCamera, CommandBuffer cmd)
        {
            cmd.SetGlobalInt(HDShaderIDs._FogEnabled, 1);
            cmd.SetGlobalFloat(HDShaderIDs._MaxFogDistance, maxFogDistance.value);

            // Fog Color
            cmd.SetGlobalFloat(m_ColorModeParam, (float)colorMode.value);
            cmd.SetGlobalColor(m_FogColorParam, new Color(color.value.r, color.value.g, color.value.b, 0.0f));
            cmd.SetGlobalVector(m_MipFogParam, new Vector4(mipFogNear.value, mipFogFar.value, mipFogMaxMip.value, 0.0f));

            DensityVolumeArtistParameters param = new DensityVolumeArtistParameters(albedo.value, meanFreePath.value, anisotropy.value);
            DensityVolumeEngineData data = param.ConvertToEngineData();

            cmd.SetGlobalVector(HDShaderIDs._HeightFogBaseScattering, data.scattering);
            cmd.SetGlobalFloat(HDShaderIDs._HeightFogBaseExtinction, data.extinction);

            float crBaseHeight = baseHeight.value;

            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                crBaseHeight -= hdCamera.camera.transform.position.y;
            }

            float layerDepth = Mathf.Max(0.01f, maximumHeight.value - baseHeight.value);
            float H = ScaleHeightFromLayerDepth(layerDepth);
            cmd.SetGlobalVector(HDShaderIDs._HeightFogExponents, new Vector2(1.0f / H, H));
            cmd.SetGlobalFloat(HDShaderIDs._HeightFogBaseHeight, crBaseHeight);

            bool enableVolumetrics = enableVolumetricFog.value && hdCamera.frameSettings.IsEnabled(FrameSettingsField.Volumetrics);
            cmd.SetGlobalFloat(HDShaderIDs._GlobalFogAnisotropy, anisotropy.value);
            cmd.SetGlobalInt(HDShaderIDs._EnableVolumetricFog, enableVolumetrics ? 1 : 0);
        }
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
            : base(value, overrideState) { }
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class FogColorParameter : VolumeParameter<FogColorMode>
    {
        public FogColorParameter(FogColorMode value, bool overrideState = false)
            : base(value, overrideState) { }
    }
}
