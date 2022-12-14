using System;
using System.Diagnostics;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Fog Volume Component.
    /// </summary>
    [Serializable, VolumeComponentMenuForRenderPipeline("Fog", typeof(HDRenderPipeline))]
    [HDRPHelpURLAttribute("Override-Fog")]
    public class Fog : VolumeComponentWithQuality
    {
        /// <summary>Enable fog.</summary>
        [Tooltip("Enables the fog.")]
        public BoolParameter enabled = new BoolParameter(false);

        /// <summary>Fog color mode.</summary>
        public FogColorParameter colorMode = new FogColorParameter(FogColorMode.SkyColor);
        /// <summary>Fog color.</summary>
        [Tooltip("Specifies the constant color of the fog.")]
        public ColorParameter color = new ColorParameter(Color.grey, hdr: true, showAlpha: false, showEyeDropper: true);
        /// <summary>Specifies the tint of the fog when using Sky Color.</summary>
        [Tooltip("Specifies the tint of the fog.")]
        public ColorParameter tint = new ColorParameter(Color.white, hdr: true, showAlpha: false, showEyeDropper: true);
        /// <summary>Maximum fog distance.</summary>
        [Tooltip("Sets the maximum fog distance HDRP uses when it shades the skybox or the Far Clipping Plane of the Camera.")]
        public MinFloatParameter maxFogDistance = new MinFloatParameter(5000.0f, 0.0f);
        /// <summary>Controls the maximum mip map HDRP uses for mip fog (0 is the lowest mip and 1 is the highest mip).</summary>
        [AdditionalProperty]
        [Tooltip("Controls the maximum mip map HDRP uses for mip fog (0 is the lowest mip and 1 is the highest mip).")]
        public ClampedFloatParameter mipFogMaxMip = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        /// <summary>Sets the distance at which HDRP uses the minimum mip image of the blurred sky texture as the fog color.</summary>
        [AdditionalProperty]
        [Tooltip("Sets the distance at which HDRP uses the minimum mip image of the blurred sky texture as the fog color.")]
        public MinFloatParameter mipFogNear = new MinFloatParameter(0.0f, 0.0f);
        /// <summary>Sets the distance at which HDRP uses the maximum mip image of the blurred sky texture as the fog color.</summary>
        [AdditionalProperty]
        [Tooltip("Sets the distance at which HDRP uses the maximum mip image of the blurred sky texture as the fog color.")]
        public MinFloatParameter mipFogFar = new MinFloatParameter(1000.0f, 0.0f);

        // Height Fog
        /// <summary>Height fog base height.</summary>
        public FloatParameter baseHeight = new FloatParameter(0.0f);
        /// <summary>Height fog maximum height.</summary>
        public FloatParameter maximumHeight = new FloatParameter(50.0f);
        /// <summary>Fog mean free path.</summary>
        [DisplayInfo(name = "Fog Attenuation Distance")]
        public MinFloatParameter meanFreePath = new MinFloatParameter(400.0f, 1.0f);

        // Optional Volumetric Fog
        /// <summary>Enable volumetric fog.</summary>
        [DisplayInfo(name = "Volumetric Fog")]
        public BoolParameter enableVolumetricFog = new BoolParameter(false);
        // Common Fog Parameters (Exponential/Volumetric)
        /// <summary>Stores the fog albedo. This defines the color of the fog.</summary>
        public ColorParameter albedo = new ColorParameter(Color.white);
        /// <summary>Multiplier for ambient probe contribution.</summary>
        [DisplayInfo(name = "Ambient Light Probe Dimmer")]
        public ClampedFloatParameter globalLightProbeDimmer = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        /// <summary>Sets the distance (in meters) from the Camera's Near Clipping Plane to the back of the Camera's volumetric lighting buffer. The lower the distance is, the higher the fog quality is.</summary>
        public MinFloatParameter depthExtent = new MinFloatParameter(64.0f, 0.1f);
        /// <summary>Controls which denoising technique to use for the volumetric effect.</summary>
        /// <remarks>Reprojection mode is effective for static lighting but can lead to severe ghosting artifacts with highly dynamic lighting. Gaussian mode is effective with dynamic lighting. You can also use both modes together which produces high-quality results, but increases the resource intensity of processing the effect.</remarks>
        [Tooltip("Specifies the denoising technique to use for the volumetric effect.")]
        public FogDenoisingModeParameter denoisingMode = new FogDenoisingModeParameter(FogDenoisingMode.Gaussian);

        // Advanced parameters
        /// <summary>Controls the angular distribution of scattered light. 0 is isotropic, 1 is forward scattering, and -1 is backward scattering.</summary>
        [AdditionalProperty]
        public ClampedFloatParameter anisotropy = new ClampedFloatParameter(0.0f, -1.0f, 1.0f);
        /// <summary>Controls the distribution of slices along the Camera's focal axis. 0 is exponential distribution and 1 is linear distribution.</summary>
        [AdditionalProperty]
        [Tooltip("Controls the distribution of slices along the Camera's focal axis. 0 is exponential distribution and 1 is linear distribution.")]
        public ClampedFloatParameter sliceDistributionUniformity = new ClampedFloatParameter(0.75f, 0, 1);

        // Limit parameters for the fog quality
        internal const float minFogScreenResolutionPercentage = (1.0f / 16.0f) * 100;
        internal const float optimalFogScreenResolutionPercentage = (1.0f / 8.0f) * 100;
        internal const float maxFogScreenResolutionPercentage = 0.5f * 100;
        internal const int maxFogSliceCount = 512;

        /// <summary>Controls which method to use to control the performance and quality of the volumetric fog.</summary>
        /// <remarks>Balance mode allows you to use a performance-oriented approach to define the quality of the volumetric fog. Manual mode gives you access to the internal set of properties which directly control the effect.</remarks>
        public FogControl fogControlMode
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_FogControlMode.value;
                else
                    return GetLightingQualitySettings().Fog_ControlMode[(int)quality.value];
            }
            set { m_FogControlMode.value = value; }
        }
        [AdditionalProperty]
        [SerializeField, FormerlySerializedAs("fogControlMode")]
        [Tooltip("Specifies which method to use to control the performance and quality of the volumetric fog.")]
        private FogControlParameter m_FogControlMode = new FogControlParameter(FogControl.Balance);

        /// <summary>Stores the resolution of the volumetric buffer (3D texture) along the x-axis and y-axis relative to the resolution of the screen.</summary>
        [AdditionalProperty]
        [Tooltip("Controls the resolution of the volumetric buffer (3D texture) along the x-axis and y-axis relative to the resolution of the screen.")]
        public ClampedFloatParameter screenResolutionPercentage = new ClampedFloatParameter(optimalFogScreenResolutionPercentage, minFogScreenResolutionPercentage, maxFogScreenResolutionPercentage);
        /// <summary>Number of slices of the volumetric buffer (3D texture) along the camera's focal axis.</summary>
        [AdditionalProperty]
        [Tooltip("Controls the number of slices to use the volumetric buffer (3D texture) along the camera's focal axis.")]
        public ClampedIntParameter volumeSliceCount = new ClampedIntParameter(64, 1, maxFogSliceCount);

        /// <summary>Defines the performance to quality ratio of the volumetric fog. A value of 0 being the least resource-intensive and a value of 1 being the highest quality.</summary>
        /// <remarks>Try to minimize this value to find a compromise between quality and performance. </remarks>
        public float volumetricFogBudget
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_VolumetricFogBudget.value;
                else
                    return GetLightingQualitySettings().Fog_Budget[(int)quality.value];
            }
            set { m_VolumetricFogBudget.value = value; }
        }
        [AdditionalProperty]
        [SerializeField, FormerlySerializedAs("volumetricFogBudget")]
        [Tooltip("Controls the performance to quality ratio of the volumetric fog. A value of 0 being the least resource-intensive and a value of 1 being the highest quality.")]
        private ClampedFloatParameter m_VolumetricFogBudget = new ClampedFloatParameter(0.25f, 0.0f, 1.0f);

        /// <summary>Controls how Unity shares resources between Screen (XY) and Depth (Z) resolutions.</summary>
        /// <remarks>A value of 0 means Unity allocates all of the resources to the XY resolution, which reduces aliasing, but increases noise. A value of 1 means Unity allocates all of the resources to the Z resolution, which reduces noise, but increases aliasing. This property allows for linear interpolation between the two configurations.</remarks>
        public float resolutionDepthRatio
        {
            get
            {
                if (!UsesQualitySettings())
                    return m_ResolutionDepthRatio.value;
                else
                    return GetLightingQualitySettings().Fog_DepthRatio[(int)quality.value];
            }
            set { m_ResolutionDepthRatio.value = value; }
        }

        /// <summary>Controls how Unity shares resources between Screen (XY) and Depth (Z) resolutions.</summary>
        [AdditionalProperty]
        [SerializeField, FormerlySerializedAs("resolutionDepthRatio")]
        [Tooltip("Controls how Unity shares resources between Screen (x-axis and y-axis) and Depth (z-axis) resolutions.")]
        public ClampedFloatParameter m_ResolutionDepthRatio = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        /// <summary>Indicates whether Unity includes or excludes non-directional light types when it evaluates the volumetric fog. Including non-directional lights increases the resource intensity of the effect.</summary>
        [AdditionalProperty]
        [Tooltip("When enabled, HDRP only includes directional Lights when it evaluates volumetric fog.")]
        public BoolParameter directionalLightsOnly = new BoolParameter(false);

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
            bool d = fog.enabled.value;

            return a && b && c && d;
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

        static void UpdateShaderVariablesGlobalCBNeutralParameters(ref ShaderVariablesGlobal cb)
        {
            cb._FogEnabled = 0;
            cb._EnableVolumetricFog = 0;
            cb._HeightFogBaseScattering = Vector3.zero;
            cb._HeightFogBaseExtinction = 0.0f;
            cb._HeightFogExponents = Vector2.one;
            cb._HeightFogBaseHeight = 0.0f;
            cb._GlobalFogAnisotropy = 0.0f;
        }

        internal static void UpdateShaderVariablesGlobalCB(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            // TODO Handle user override
            var fogSettings = hdCamera.volumeStack.GetComponent<Fog>();

            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.AtmosphericScattering) || !fogSettings.enabled.value)
            {
                UpdateShaderVariablesGlobalCBNeutralParameters(ref cb);
            }
            else
            {
                fogSettings.UpdateShaderVariablesGlobalCBFogParameters(ref cb, hdCamera);
            }
        }

        void UpdateShaderVariablesGlobalCBFogParameters(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            bool enableVolumetrics = enableVolumetricFog.value && hdCamera.frameSettings.IsEnabled(FrameSettingsField.Volumetrics);

            cb._FogEnabled = 1;
            cb._PBRFogEnabled = IsPBRFogEnabled(hdCamera) ? 1 : 0;
            cb._EnableVolumetricFog = enableVolumetrics ? 1 : 0;
            cb._MaxFogDistance = maxFogDistance.value;

            Color fogColor = (colorMode.value == FogColorMode.ConstantColor) ? color.value : tint.value;
            cb._FogColorMode = (float)colorMode.value;
            cb._FogColor = new Color(fogColor.r, fogColor.g, fogColor.b, 0.0f);
            cb._MipFogParameters = new Vector4(mipFogNear.value, mipFogFar.value, mipFogMaxMip.value, 0.0f);

            LocalVolumetricFogArtistParameters param = new LocalVolumetricFogArtistParameters(albedo.value, meanFreePath.value, anisotropy.value);
            LocalVolumetricFogEngineData data = param.ConvertToEngineData();

            // When volumetric fog is disabled, we don't want its color to affect the heightfog. So we pass neutral values here.
            cb._HeightFogBaseScattering = enableVolumetrics ? data.scattering : Vector4.one * data.extinction;
            cb._HeightFogBaseExtinction = data.extinction;

            float crBaseHeight = baseHeight.value;

            if (ShaderConfig.s_CameraRelativeRendering != 0)
            {
                crBaseHeight -= hdCamera.camera.transform.position.y;
            }

            float layerDepth = Mathf.Max(0.01f, maximumHeight.value - baseHeight.value);
            float H = ScaleHeightFromLayerDepth(layerDepth);
            cb._HeightFogExponents = new Vector2(1.0f / H, H);
            cb._HeightFogBaseHeight = crBaseHeight;
            cb._GlobalFogAnisotropy = anisotropy.value;
            cb._VolumetricFilteringEnabled = ((int)denoisingMode.value & (int)FogDenoisingMode.Gaussian) != 0 ? 1 : 0;
            cb._FogDirectionalOnly = directionalLightsOnly.value ? 1 : 0;
        }
    }

    /// <summary>
    /// Fog Color Mode.
    /// </summary>
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

    /// <summary>
    /// Options that control the quality and resource intensity of the volumetric fog.
    /// </summary>
    public enum FogControl
    {
        /// <summary>
        /// Use this mode if you want to change the fog control properties based on a higher abstraction level centered around performance.
        /// </summary>
        Balance,

        /// <summary>
        /// Use this mode if you want to have direct access to the internal properties that control volumetric fog.
        /// </summary>
        Manual
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="ExposureMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class FogControlParameter : VolumeParameter<FogControl>
    {
        /// <summary>
        /// Creates a new <see cref="FogControlParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public FogControlParameter(FogControl value, bool overrideState = false) : base(value, overrideState) { }
    }

    /// <summary>
    /// Options that control which denoising algorithms Unity should use on the volumetric fog signal.
    /// </summary>
    public enum FogDenoisingMode
    {
        /// <summary>
        /// Use this mode to not filter the volumetric fog.
        /// </summary>
        None = 0,
        /// <summary>
        /// Use this mode to reproject data from previous frames to denoise the signal. This is effective for static lighting, but it can lead to severe ghosting artifacts for highly dynamic lighting.
        /// </summary>
        Reprojection = 1 << 0,
        /// <summary>
        /// Use this mode to reduce the aliasing patterns that can appear on the volumetric fog.
        /// </summary>
        Gaussian = 1 << 1,
        /// <summary>
        /// Use this mode to use both Reprojection and Gaussian filtering techniques. This produces high visual quality, but significantly increases the resource intensity of the effect.
        /// </summary>
        Both = Reprojection | Gaussian
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="FogDenoisingMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class FogDenoisingModeParameter : VolumeParameter<FogDenoisingMode>
    {
        /// <summary>
        /// Creates a new <see cref="FogDenoisingModeParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public FogDenoisingModeParameter(FogDenoisingMode value, bool overrideState = false) : base(value, overrideState) { }
    }
}
