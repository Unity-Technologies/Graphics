using System;
using System.Diagnostics;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Fog Volume Component.
    /// </summary>
    [Serializable, VolumeComponentMenu("Fog/Fog")]
    public class Fog : VolumeComponent
    {
        /// <summary>Enable fog.</summary>
        [Tooltip("Enables the fog.")]
        public BoolParameter         enabled = new BoolParameter(false);

        /// <summary>Fog color mode.</summary>
        public FogColorParameter     colorMode = new FogColorParameter(FogColorMode.SkyColor);
        /// <summary>Fog color.</summary>
        [Tooltip("Specifies the constant color of the fog.")]
        public ColorParameter        color = new ColorParameter(Color.grey, hdr: true, showAlpha: false, showEyeDropper: true);
        /// <summary>Specifies the tint of the fog when using Sky Color.</summary>
        [Tooltip("Specifies the tint of the fog.")]
        public ColorParameter        tint = new ColorParameter(Color.white, hdr: true, showAlpha: false, showEyeDropper: true);
        /// <summary>Maximum fog distance.</summary>
        [Tooltip("Sets the maximum fog distance HDRP uses when it shades the skybox or the Far Clipping Plane of the Camera.")]
        public MinFloatParameter     maxFogDistance = new MinFloatParameter(5000.0f, 0.0f);
        /// <summary>Controls the maximum mip map HDRP uses for mip fog (0 is the lowest mip and 1 is the highest mip).</summary>
        [Tooltip("Controls the maximum mip map HDRP uses for mip fog (0 is the lowest mip and 1 is the highest mip).")]
        public ClampedFloatParameter mipFogMaxMip = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        /// <summary>Sets the distance at which HDRP uses the minimum mip image of the blurred sky texture as the fog color.</summary>
        [Tooltip("Sets the distance at which HDRP uses the minimum mip image of the blurred sky texture as the fog color.")]
        public MinFloatParameter     mipFogNear = new MinFloatParameter(0.0f, 0.0f);
        /// <summary>Sets the distance at which HDRP uses the maximum mip image of the blurred sky texture as the fog color.</summary>
        [Tooltip("Sets the distance at which HDRP uses the maximum mip image of the blurred sky texture as the fog color.")]
        public MinFloatParameter     mipFogFar = new MinFloatParameter(1000.0f, 0.0f);

        // Height Fog
        /// <summary>Height fog base height.</summary>
        public FloatParameter baseHeight = new FloatParameter(0.0f);
        /// <summary>Height fog maximum height.</summary>
        public FloatParameter maximumHeight = new FloatParameter(50.0f);

        // Common Fog Parameters (Exponential/Volumetric)
        /// <summary>Fog albedo.</summary>
        public ColorParameter albedo = new ColorParameter(Color.white);
        /// <summary>Fog mean free path.</summary>
        public MinFloatParameter meanFreePath = new MinFloatParameter(400.0f, 1.0f);

        // Optional Volumetric Fog
        /// <summary>Enable volumetric fog.</summary>
        public BoolParameter enableVolumetricFog = new BoolParameter(false);
        /// <summary>Volumetric fog anisotropy.</summary>
        public ClampedFloatParameter anisotropy = new ClampedFloatParameter(0.0f, -1.0f, 1.0f);
        /// <summary>Multiplier for ambient probe contribution.</summary>
        public ClampedFloatParameter globalLightProbeDimmer = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        /// <summary>Sets the distance (in meters) from the Camera's Near Clipping Plane to the back of the Camera's volumetric lighting buffer.</summary>
        [Tooltip("Sets the distance (in meters) from the Camera's Near Clipping Plane to the back of the Camera's volumetric lighting buffer.")]
        public MinFloatParameter depthExtent = new MinFloatParameter(64.0f, 0.1f);
        /// <summary>Controls the distribution of slices along the Camera's focal axis. 0 is exponential distribution and 1 is linear distribution.</summary>
        [Tooltip("Controls the distribution of slices along the Camera's focal axis. 0 is exponential distribution and 1 is linear distribution.")]
        public ClampedFloatParameter sliceDistributionUniformity = new ClampedFloatParameter(0.75f, 0, 1);

        /// <summary>Applies a blur to smoothen the volumetric lighting output.</summary>
        [Tooltip("Applies a blur to smoothen the volumetric lighting output.")]
        public BoolParameter filter = new BoolParameter(false);

        internal static bool IsFogEnabled(HDCamera hdCamera)
        {
            return hdCamera.frameSettings.IsEnabled(FrameSettingsField.AtmosphericScattering) && hdCamera.volumeStack.GetComponent<Fog>().enabled.value;
        }

        internal static bool IsVolumetricFogEnabled(HDCamera hdCamera)
        {
            var fog = hdCamera.volumeStack.GetComponent<Fog>();

            bool a = fog.enableVolumetricFog.value;
            bool b = hdCamera.frameSettings.IsEnabled(FrameSettingsField.Volumetrics);
            bool c = CoreUtils.IsSceneViewFogEnabled(hdCamera.camera);

            return a && b && c;
        }

        internal static bool IsPBRFogEnabled(HDCamera hdCamera)
        {
            var visualEnv = hdCamera.volumeStack.GetComponent<VisualEnvironment>();
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

        static void UpdateShaderVariablesGlobalCBNeutralParameters(ConstantBuffer<ShaderVariablesGlobal> cb)
        {
            cb.data._FogEnabled = 0;
            cb.data._EnableVolumetricFog = 0;
            cb.data._HeightFogBaseScattering = Vector3.zero;
            cb.data._HeightFogBaseExtinction = 0.0f;
            cb.data._HeightFogExponents = Vector2.one;
            cb.data._HeightFogBaseHeight = 0.0f;
            cb.data._GlobalFogAnisotropy = 0.0f;
        }

        internal static void UpdateShaderVariablesGlobalCB(ConstantBuffer<ShaderVariablesGlobal> cb, HDCamera hdCamera)
        {
            // TODO Handle user override
            var fogSettings = hdCamera.volumeStack.GetComponent<Fog>();

            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.AtmosphericScattering) || !fogSettings.enabled.value)
            {
                UpdateShaderVariablesGlobalCBNeutralParameters(cb);
            }
            else
            {
                fogSettings.UpdateShaderVariablesGlobalCBFogParameters(cb, hdCamera);
            }
        }

        void UpdateShaderVariablesGlobalCBFogParameters(ConstantBuffer<ShaderVariablesGlobal> cb, HDCamera hdCamera)
        {
            cb.data._FogEnabled = 1;
            cb.data._MaxFogDistance = maxFogDistance.value;

            // Fog Color
            Color fogColor = (colorMode.value == FogColorMode.ConstantColor) ? color.value : tint.value;
            cb.data._FogColorMode = (float)colorMode.value;
            cb.data._FogColor = new Color(fogColor.r, fogColor.g, fogColor.b, 0.0f);
            cb.data._MipFogParameters  = new Vector4(mipFogNear.value, mipFogFar.value, mipFogMaxMip.value, 0.0f);

            DensityVolumeArtistParameters param = new DensityVolumeArtistParameters(albedo.value, meanFreePath.value, anisotropy.value);
            DensityVolumeEngineData data = param.ConvertToEngineData();

            cb.data._HeightFogBaseScattering = data.scattering;
            cb.data._HeightFogBaseExtinction = data.extinction;

            float crBaseHeight = baseHeight.value;

            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                crBaseHeight -= hdCamera.camera.transform.position.y;
            }

            float layerDepth = Mathf.Max(0.01f, maximumHeight.value - baseHeight.value);
            float H = ScaleHeightFromLayerDepth(layerDepth);
            cb.data._HeightFogExponents = new Vector2(1.0f / H, H);
            cb.data._HeightFogBaseHeight = crBaseHeight;

            bool enableVolumetrics = enableVolumetricFog.value && hdCamera.frameSettings.IsEnabled(FrameSettingsField.Volumetrics);
            cb.data._GlobalFogAnisotropy = anisotropy.value;
            cb.data._EnableVolumetricFog = enableVolumetrics ? 1 : 0;

            cb.data._PBRFogEnabled = IsPBRFogEnabled(hdCamera) ? 1 : 0;
        }
    }

    /// <summary>
    /// Fog Color Mode.
    /// </summary>
    [GenerateHLSL]
    public enum FogColorMode
    {
        /// <summary>Fog is a constant color.</summary>
        ConstantColor,
        /// <summary>Fog uses the current sky to determine its color.</summary>
        SkyColor,
    }

    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    sealed class FogTypeParameter : VolumeParameter<FogType>
    {
        public FogTypeParameter(FogType value, bool overrideState = false)
            : base(value, overrideState) { }
    }

    /// <summary>
    /// Fog Color parameter.
    /// </summary>
    [Serializable, DebuggerDisplay(k_DebuggerDisplay)]
    public sealed class FogColorParameter : VolumeParameter<FogColorMode>
    {
        /// <summary>
        /// Fog Color Parameter constructor.
        /// </summary>
        /// <param name="value">Fog Color Parameter.</param>
        /// <param name="overrideState">Initial override state.</param>
        public FogColorParameter(FogColorMode value, bool overrideState = false)
            : base(value, overrideState) { }
    }
}
