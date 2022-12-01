using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the ambient occlusion.
    /// </summary>
    [Serializable, VolumeComponentMenu("Sky/Volumetric Clouds")]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [HDRPHelpURL("Override-Volumetric-Clouds")]
    public sealed partial class VolumetricClouds : VolumeComponent
    {
        /// <summary>
        /// Control mode for the volumetric clouds.
        /// </summary>
        public enum CloudControl
        {
            /// <summary>Control the volumetric clouds with a set of presets and very few parameters.</summary>
            Simple,
            /// <summary>Control the volumetric clouds by specifing the cloud types and densities.</summary>
            Advanced,
            /// <summary>Control volumetric clouds by providing your own cloud map and properties LUT.</summary>
            Manual
        }

        /// <summary>
        /// A <see cref="VolumeParameter"/> that holds a <see cref="CloudControl"/> value.
        /// </summary>
        [Serializable]
        public sealed class CloudControlParameter : VolumeParameter<CloudControl>
        {
            /// <summary>
            /// Creates a new <see cref="CloudControlParameter"/> instance.
            /// </summary>
            /// <param name="value">The initial value to store in the parameter.</param>
            /// <param name="overrideState">The initial override state for the parameter.</param>
            public CloudControlParameter(CloudControl value, bool overrideState = false) : base(value, overrideState) { }
        }

        /// <summary>
        /// Controls the quality level for the simple mode.
        /// </summary>
        public enum CloudSimpleMode
        {
            /// <summary>Control the volumetric clouds with a set of presets and very few parameters (performance mode).</summary>
            Performance,
            /// <summary>Control the volumetric clouds with a set of presets and very few parameters (quality mode).</summary>
            Quality
        }

        /// <summary>
        /// A <see cref="VolumeParameter"/> that holds a <see cref="CloudSimpleMode"/> value.
        /// </summary>
        [Serializable]
        public sealed class CloudSimpleModeParameter : VolumeParameter<CloudSimpleMode>
        {
            /// <summary>
            /// Creates a new <see cref="CloudSimpleModeParameter"/> instance.
            /// </summary>
            /// <param name="value">The initial value to store in the parameter.</param>
            /// <param name="overrideState">The initial override state for the parameter.</param>
            public CloudSimpleModeParameter(CloudSimpleMode value, bool overrideState = false) : base(value, overrideState) { }
        }

        /// <summary>
        /// The set of available presets for the simple cloud control mode.
        /// </summary>
        public enum CloudPresets
        {
            /// <summary>Smaller clouds that are spread apart.</summary>
            Sparse,
            /// <summary>Medium-sized clouds that partially cover the sky.</summary>
            Cloudy,
            /// <summary>A light layer of cloud that covers the entire sky. Some areas are less dense and let more light through, whereas other areas are more dense and appear darker.</summary>
            Overcast,
            /// <summary>Large dark clouds that cover most of the sky.</summary>
            Stormy,
            /// <summary>Exposes properties that control the shape of the clouds.</summary>
            Custom
        }

        /// <summary>
        /// A <see cref="VolumeParameter"/> that holds a <see cref="CloudControl"/> value.
        /// </summary>
        [Serializable]
        public sealed class CloudPresetsParameter : VolumeParameter<CloudPresets>
        {
            /// <summary>
            /// Creates a new <see cref="CloudPresetsParameter"/> instance.
            /// </summary>
            /// <param name="value">The initial value to store in the parameter.</param>
            /// <param name="overrideState">The initial override state for the parameter.</param>
            public CloudPresetsParameter(CloudPresets value, bool overrideState = false) : base(value, overrideState) { }
        }

        /// <summary>
        /// Resolution of the volumetric clouds shadow.
        /// </summary>
        public enum CloudShadowResolution
        {
            /// <summary>The volumetric clouds shadow will be 64x64.</summary>
            VeryLow64 = 64,
            /// <summary>The volumetric clouds shadow will be 128x128.</summary>
            Low128 = 128,
            /// <summary>The volumetric clouds shadow will be 256x256.</summary>
            Medium256 = 256,
            /// <summary>The volumetric clouds shadow will be 512x512.</summary>
            High512 = 512,
            /// <summary>The volumetric clouds shadow will be 1024x1024.</summary>
            Ultra1024 = 1024,
        }

        /// <summary> </summary>
        public const int CloudShadowResolutionCount = 5;

        /// <summary>
        /// A <see cref="VolumeParameter"/> that holds a <see cref="CloudControl"/> value.
        /// </summary>
        [Serializable]
        public sealed class CloudShadowResolutionParameter : VolumeParameter<CloudShadowResolution>
        {
            /// <summary>
            /// Creates a new <see cref="CloudShadowResolutionParameter"/> instance.
            /// </summary>
            /// <param name="value">The initial value to store in the parameter.</param>
            /// <param name="overrideState">The initial override state for the parameter.</param>
            public CloudShadowResolutionParameter(CloudShadowResolution value, bool overrideState = false) : base(value, overrideState) { }
        }

        /// <summary>
        /// Resolution of the volumetric clouds map.
        /// </summary>
        public enum CloudMapResolution
        {
            /// <summary>The volumetric clouds map will be 32x32.</summary>
            Low32x32 = 32,
            /// <summary>The volumetric clouds map will be 64x64.</summary>
            Medium64x64 = 64,
            /// <summary>The volumetric clouds map will be 128x128.</summary>
            High128x128 = 128,
            /// <summary>The volumetric clouds map will be 256x256.</summary>
            Ultra256x256 = 256
        }

        /// <summary>
        /// A <see cref="VolumeParameter"/> that holds a <see cref="CloudMapResolution"/> value.
        /// </summary>
        [Serializable]
        public sealed class CloudMapResolutionParameter : VolumeParameter<CloudMapResolution>
        {
            /// <summary>
            /// Creates a new <see cref="CloudMapResolutionParameter"/> instance.
            /// </summary>
            /// <param name="value">The initial value to store in the parameter.</param>
            /// <param name="overrideState">The initial override state for the parameter.</param>
            public CloudMapResolutionParameter(CloudMapResolution value, bool overrideState = false) : base(value, overrideState) { }
        }

        /// <summary>
        /// Controls the erosion noise used for the clouds.
        /// </summary>
        public enum CloudErosionNoise
        {
            /// <summary>The erosion noise will be a 32x32x32 worley texture.</summary>
            Worley32,
            /// <summary>The erosion noise will be a 32x32x32 perlin texture.</summary>
            Perlin32,
        }

        /// <summary>
        /// A <see cref="VolumeParameter"/> that holds a <see cref="CloudErosionNoise"/> value.
        /// </summary>
        [Serializable]
        public sealed class CloudErosionNoiseParameter : VolumeParameter<CloudErosionNoise>
        {
            /// <summary>
            /// Creates a new <see cref="CloudErosionNoiseParameter"/> instance.
            /// </summary>
            /// <param name="value">The initial value to store in the parameter.</param>
            /// <param name="overrideState">The initial override state for the parameter.</param>
            public CloudErosionNoiseParameter(CloudErosionNoise value, bool overrideState = false) : base(value, overrideState) { }
        }

        /// <summary>
        /// The set mode in which the clouds fade in when close to the camera
        /// </summary>
        public enum CloudFadeInMode
        {
            /// <summary>The fade in parameters are automatically evaluated.</summary>
            Automatic,
            /// <summary>The fade in parameters are to be defined by the user.</summary>
            Manual
        }

        /// <summary>
        /// A <see cref="VolumeParameter"/> that holds a <see cref="CloudControl"/> value.
        /// </summary>
        [Serializable]
        public sealed class CloudFadeInModeParameter : VolumeParameter<CloudFadeInMode>
        {
            /// <summary>
            /// Creates a new <see cref="CloudFadeInModeParameter"/> instance.
            /// </summary>
            /// <param name="value">The initial value to store in the parameter.</param>
            /// <param name="overrideState">The initial override state for the parameter.</param>
            public CloudFadeInModeParameter(CloudFadeInMode value, bool overrideState = false) : base(value, overrideState) { }
        }

        /// <summary>
        /// Enable/Disable the volumetric clouds effect.
        /// </summary>
        [Tooltip("Enable/Disable the volumetric clouds effect.")]
        public BoolParameter enable = new BoolParameter(false, BoolParameter.DisplayType.EnumPopup);

        /// <summary>
        /// When enabled, clouds are part of the scene and you can interact with them. This means you can move around and inside the clouds, they can appear between the Camera and other GameObjects, and the Camera's clipping planes affect the clouds. When disabled, the clouds are part of the skybox. This means the clouds and their shadows appear relative to the Camera and always appear behind geometry.
        /// </summary>
        [Tooltip("When enabled, clouds are part of the scene and you can interact with them. This means you can move around and inside the clouds, they can appear between the Camera and other GameObjects, and the Camera's clipping planes affect the clouds. When disabled, the clouds are part of the skybox. This means the clouds and their shadows appear relative to the Camera and always appear behind geometry.")]
        public BoolParameter localClouds = new BoolParameter(false);

        /// <summary>
        /// Controls the curvature of the cloud volume which defines the distance at which the clouds intersect with the horizon.
        /// </summary>
        [Tooltip("Controls the curvature of the cloud volume which defines the distance at which the clouds intersect with the horizon.")]
        public ClampedFloatParameter earthCurvature = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        /// <summary>
        /// Tiling (x,y) of the cloud map.
        /// </summary>
        [Tooltip("Tiling (x,y) of the cloud map.")]
        public Vector2Parameter cloudTiling = new Vector2Parameter(new Vector2(1.0f, 1.0f));

        /// <summary>
        /// Offset (x,y) of the cloud map.
        /// </summary>
        [Tooltip("Offset (x,y) of the cloud map.")]
        public Vector2Parameter cloudOffset = new Vector2Parameter(new Vector2(0.0f, 0.0f));

        /// <summary>
        /// Controls the altitude of the bottom of the volumetric clouds volume in meters.
        /// </summary>
        [Tooltip("Controls the altitude of the bottom of the volumetric clouds volume in meters.")]
        public MinFloatParameter bottomAltitude = new MinFloatParameter(1200.0f, 0.01f);

        /// <summary>
        /// Controls the size of the volumetric clouds volume in meters.
        /// </summary>
        [Tooltip("Controls the size of the volumetric clouds volume in meters.")]
        public MinFloatParameter altitudeRange = new MinFloatParameter(2000.0f, 100.0f);

        /// <summary>
        /// Controls the mode in which the clouds fade in when close to the camera's near plane.
        /// </summary>
        [Tooltip("Controls the mode in which the clouds fade in when close to the camera's near plane.")]
        public CloudFadeInModeParameter fadeInMode = new CloudFadeInModeParameter(CloudFadeInMode.Automatic);

        /// <summary>
        /// Controls the minimal distance at which clouds start appearing.
        /// </summary>
        [Tooltip("Controls the minimal distance at which clouds start appearing.")]
        public MinFloatParameter fadeInStart = new MinFloatParameter(0.0f, 0.0f);

        /// <summary>
        /// Controls the distance that it takes for the clouds to reach their complete density.
        /// </summary>
        [Tooltip("Controls the distance that it takes for the clouds to reach their complete density.")]
        public MinFloatParameter fadeInDistance = new MinFloatParameter(0.0f, 0.0f);

        /// <summary>
        /// Controls the number of steps when evaluating the clouds' transmittance. A higher value may lead to a lower noise level and longer view distance, but at a higher cost.
        /// </summary>
        [Tooltip("Controls the number of steps when evaluating the clouds' transmittance. A higher value may lead to a lower noise level and longer view distance, but at a higher cost.")]
        public ClampedIntParameter numPrimarySteps = new ClampedIntParameter(64, 32, 1024);

        /// <summary>
        /// Controls the number of steps when evaluating the clouds' lighting. A higher value will lead to smoother lighting and improved self-shadowing, but at a higher cost.
        /// </summary>
        [Tooltip("Controls the number of steps when evaluating the clouds' lighting. A higher value will lead to smoother lighting and improved self-shadowing, but at a higher cost.")]
        public ClampedIntParameter numLightSteps = new ClampedIntParameter(6, 1, 32);

        /// <summary>
        /// Specifies the cloud map - Coverage (R), Rain (G), Type (B).
        /// </summary>
        [Tooltip("Specifies the cloud map - Coverage (R), Rain (G), Type (B).")]
        public TextureParameter cloudMap = new TextureParameter(null, TextureDimension.Tex2D);

        /// <summary>
        /// Specifies the lookup table for the clouds - Profile Coverage (R), Erosion (G), Ambient Occlusion (B).
        /// </summary>
        [Tooltip("Specifies the lookup table for the clouds - Profile Coverage (R), Erosion (G), Ambient Occlusion (B).")]
        public TextureParameter cloudLut = new TextureParameter(null, TextureDimension.Tex2D);

        /// <summary>
        /// Specifies the cloud control Mode: Simple, Advanced or Manual.
        /// </summary>
        [Tooltip("Specifies the cloud control Mode: Simple, Advanced or Manual.")]
        public CloudControlParameter cloudControl = new CloudControlParameter(CloudControl.Simple);

        /// <summary>
        /// Specifies the quality mode used to render the volumetric clouds.
        /// </summary>
        public CloudSimpleModeParameter cloudSimpleMode = new CloudSimpleModeParameter(CloudSimpleMode.Performance);

        /// <summary>
        /// Specifies the weather preset in Simple mode.
        /// </summary>
        public CloudPresets cloudPreset
        {
            get
            {
                return m_CloudPreset.value;
            }
            set
            {
                m_CloudPreset.value = value;
                ApplyCurrentCloudPreset();
            }
        }
        [SerializeField, FormerlySerializedAs("cloudPreset")]
        private CloudPresetsParameter m_CloudPreset = new CloudPresetsParameter(CloudPresets.Cloudy);

        /// <summary>
        /// Specifies the lower cloud layer distribution in the advanced mode.
        /// </summary>
        [Tooltip("Specifies the lower cloud layer distribution in the advanced mode.")]
        public TextureParameter cumulusMap = new TextureParameter(null, TextureDimension.Tex2D);

        /// <summary>
        /// Overrides the coverage of the lower cloud layer specified in the cumulus map in the advanced mode.
        /// </summary>
        [Tooltip("Overrides the coverage of the lower cloud layer specified in the cumulus map in the advanced mode.")]
        public ClampedFloatParameter cumulusMapMultiplier = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        /// <summary>
        /// Specifies the higher cloud layer distribution in the advanced mode.
        /// </summary>
        [Tooltip("Specifies the higher cloud layer distribution in the advanced mode.")]
        public TextureParameter altoStratusMap = new TextureParameter(null, TextureDimension.Tex2D);

        /// <summary>
        /// Overrides the coverage of the higher cloud layer specified in the alto stratus map in the advanced mode.
        /// </summary>
        [Tooltip("Overrides the coverage of the higher cloud layer specified in the alto stratus map in the advanced mode.")]
        public ClampedFloatParameter altoStratusMapMultiplier = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        /// <summary>
        /// Specifies the anvil shaped clouds distribution in the advanced mode.
        /// </summary>
        [Tooltip("Specifies the anvil shaped clouds distribution in the advanced mode.")]
        public TextureParameter cumulonimbusMap = new TextureParameter(null, TextureDimension.Tex2D);

        /// <summary>
        /// Overrides the coverage of the anvil shaped clouds specified in the cumulonimbus map in the advanced mode.
        /// </summary>
        [Tooltip("Overrides the coverage of the anvil shaped clouds specified in the cumulonimbus map in the advanced mode.")]
        public ClampedFloatParameter cumulonimbusMapMultiplier = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        /// <summary>
        /// Specifies the rain distribution in the advanced mode.
        /// </summary>
        [Tooltip("Specifies the rain distribution in the advanced mode.")]
        public TextureParameter rainMap = new TextureParameter(null, TextureDimension.Tex2D);

        /// <summary>
        /// Specifies the internal texture resolution used for the cloud map in the advanced mode. A lower value will lead to higher performance, but less precise cloud type transitions.
        /// </summary>
        [Tooltip("Specifies the internal texture resolution used for the cloud map in the advanced mode. A lower value will lead to higher performance, but less precise cloud type transitions.")]
        public CloudMapResolutionParameter cloudMapResolution = new CloudMapResolutionParameter(CloudMapResolution.Medium64x64);

        /// <summary>
        /// Controls the density (Y axis) of the volumetric clouds as a function of the height (X Axis) inside the cloud volume.
        /// </summary>
        [Tooltip("Controls the density (Y axis) of the volumetric clouds as a function of the height (X Axis) inside the cloud volume.")]
        public AnimationCurveParameter densityCurve = new AnimationCurveParameter(new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.15f, 1.0f), new Keyframe(1.0f, 0.1f)), false);

        /// <summary>
        /// Controls the erosion (Y axis) of the volumetric clouds as a function of the height (X Axis) inside the cloud volume.
        /// </summary>
        [Tooltip("Controls the erosion (Y axis) of the volumetric clouds as a function of the height (X Axis) inside the cloud volume.")]
        public AnimationCurveParameter erosionCurve = new AnimationCurveParameter(new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.1f, 0.9f), new Keyframe(1.0f, 1.0f)), false);

        /// <summary>
        /// Controls the ambient occlusion (Y axis) of the volumetric clouds as a function of the height (X Axis) inside the cloud volume.
        /// </summary>
        [Tooltip("Controls the ambient occlusion (Y axis) of the volumetric clouds as a function of the height (X Axis) inside the cloud volume.")]
        public AnimationCurveParameter ambientOcclusionCurve = new AnimationCurveParameter(new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.25f, 0.4f), new Keyframe(1.0f, 0.0f)), false);

        /// <summary>
        /// Specifies the tint of the cloud scattering color.
        /// </summary>
        [Tooltip("Specifies the tint of the cloud scattering color.")]
        public ColorParameter scatteringTint = new ColorParameter(new Color(0.0f, 0.0f, 0.0f, 1.0f));

        /// <summary>
        /// Controls the amount of local scattering in the clouds. A higher value may produce a more powdery or diffused aspect.
        /// </summary>
        [Tooltip("Controls the amount of local scattering in the clouds. A higher value may produce a more powdery or diffused aspect.")]
        [AdditionalProperty]
        public ClampedFloatParameter powderEffectIntensity = new ClampedFloatParameter(0.25f, 0.0f, 1.0f);

        /// <summary>
        /// Controls the amount of multi-scattering inside the cloud.
        /// </summary>
        [Tooltip("Controls the amount of multi-scattering inside the cloud.")]
        [AdditionalProperty]
        public ClampedFloatParameter multiScattering = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        /// <summary>
        /// Controls the global density of the cloud volume.
        /// </summary>
        [Tooltip("Controls the global density of the cloud volume.")]
        public ClampedFloatParameter densityMultiplier = new ClampedFloatParameter(0.4f, 0.0f, 1.0f);

        /// <summary>
        /// Controls the larger noise passing through the cloud coverage. A higher value will yield less cloud coverage and smaller clouds.
        /// </summary>
        [Tooltip("Controls the larger noise passing through the cloud coverage. A higher value will yield less cloud coverage and smaller clouds.")]
        public ClampedFloatParameter shapeFactor = new ClampedFloatParameter(0.9f, 0.0f, 1.0f);

        /// <summary>
        /// Controls the size of the larger noise passing through the cloud coverage.
        /// </summary>
        [Tooltip("Controls the size of the larger noise passing through the cloud coverage.")]
        public MinFloatParameter shapeScale = new MinFloatParameter(5.0f, 0.1f);

        /// <summary>
        /// Controls the world space offset applied when evaluating the larger noise passing through the cloud coverage.
        /// </summary>
        [Tooltip("Controls the world space offset applied when evaluating the larger noise passing through the cloud coverage.")]
        public Vector3Parameter shapeOffset = new Vector3Parameter(Vector3.zero);

        /// <summary>
        /// Controls the smaller noise on the edge of the clouds. A higher value will erode clouds more significantly.
        /// </summary>
        [Tooltip("Controls the smaller noise on the edge of the clouds. A higher value will erode clouds more significantly.")]
        public ClampedFloatParameter erosionFactor = new ClampedFloatParameter(0.8f, 0.0f, 1.0f);

        /// <summary>
        /// Controls the size of the smaller noise passing through the cloud coverage.
        /// </summary>
        [Tooltip("Controls the size of the smaller noise passing through the cloud coverage.")]
        public MinFloatParameter erosionScale = new MinFloatParameter(107.0f, 1.0f);

        /// <summary>
        /// Controls the type of noise used to generate the smaller noise passing through the cloud coverage.
        /// </summary>
        [Tooltip("Controls the type of noise used to generate the smaller noise passing through the cloud coverage.")]
        [AdditionalProperty]
        public CloudErosionNoiseParameter erosionNoiseType = new CloudErosionNoiseParameter(CloudErosionNoise.Perlin32);

        /// <summary>
        /// When enabled, an additional noise should be evaluated for the clouds in the advanced and manual modes. This increases signficantly the cost of the volumetric clouds.
        /// </summary>
        [Tooltip("When enabled, an additional noise should be evaluated for the clouds in the advanced and manual modes. This increases signficantly the cost of the volumetric clouds.")]
        public BoolParameter microErosion = new BoolParameter(false);

        /// <summary>
        /// Controls the smallest noise on the edge of the clouds. A higher value will erode clouds more.
        /// </summary>
        [Tooltip("Controls the smallest noise on the edge of the clouds. A higher value will erode clouds more.")]
        public ClampedFloatParameter microErosionFactor = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        /// <summary>
        /// Controls the size of the smaller noise passing through the cloud coverage.
        /// </summary>
        [Tooltip("Controls the size of the smaller noise passing through the cloud coverage.")]
        public MinFloatParameter microErosionScale = new MinFloatParameter(200.0f, 0.1f);

        /// <summary>
        /// Controls the influence of the light probes on the cloud volume. A lower value will suppress the ambient light and produce darker clouds overall.
        /// </summary>
        [Tooltip("Controls the influence of the light probes on the cloud volume. A lower value will suppress the ambient light and produce darker clouds overall.")]
        public ClampedFloatParameter ambientLightProbeDimmer = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        /// <summary>
        /// Controls the influence of the sun light on the cloud volume. A lower value will suppress the sun light and produce darker clouds overall.
        /// </summary>
        [Tooltip("Controls the influence of the sun light on the cloud volume. A lower value will suppress the sun light and produce darker clouds overall.")]
        public ClampedFloatParameter sunLightDimmer = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        /// <summary>
        /// Controls how much Erosion Factor is taken into account when computing ambient occlusion. The Erosion Factor parameter is editable in the custom preset, Advanced and Manual Modes.
        /// </summary>
        [Tooltip("Controls how much Erosion Factor is taken into account when computing ambient occlusion. The Erosion Factor parameter is editable in the custom preset, Advanced and Manual Modes.")]
        [AdditionalProperty]
        public ClampedFloatParameter erosionOcclusion = new ClampedFloatParameter(0.1f, 0.0f, 1.0f);

        /// <summary>
        /// Sets the global horizontal wind speed in kilometers per hour. This value can be relative to the Global Wind Speed defined in the Visual Environment.
        /// </summary>
        [Tooltip("Sets the global horizontal wind speed in kilometers per hour.\nThis value can be relative to the Global Wind Speed defined in the Visual Environment.")]
        public WindSpeedParameter globalWindSpeed = new WindSpeedParameter();

        /// <summary>
        /// Controls the orientation of the wind relative to the X world vector. This value can be relative to the Global Wind Orientation defined in the Visual Environment.
        /// </summary>
        [Tooltip("Controls the orientation of the wind relative to the X world vector.\nThis value can be relative to the Global Wind Orientation defined in the Visual Environment.")]
        public WindOrientationParameter orientation = new WindOrientationParameter();

        /// <summary>
        /// Controls the intensity of the wind-based altitude distortion of the clouds.
        /// </summary>
        [AdditionalProperty]
        [Tooltip("Controls the intensity of the wind-based altitude distortion of the clouds.")]
        public ClampedFloatParameter altitudeDistortion = new ClampedFloatParameter(0.25f, -1.0f, 1.0f);

        /// <summary>
        /// Controls the multiplier to the speed of the cloud map.
        /// </summary>
        [Tooltip("Controls the multiplier to the speed of the cloud map.")]
        [AdditionalProperty]
        public ClampedFloatParameter cloudMapSpeedMultiplier = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        /// <summary>
        /// Controls the multiplier to the speed of the larger cloud shapes.
        /// </summary>
        [Tooltip("Controls the multiplier to the speed of the larger cloud shapes.")]
        [AdditionalProperty]
        public ClampedFloatParameter shapeSpeedMultiplier = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        /// <summary>
        /// Controls the multiplier to the speed of the erosion cloud shapes.
        /// </summary>
        [Tooltip("Controls the multiplier to the speed of the erosion cloud shapes.")]
        [AdditionalProperty]
        public ClampedFloatParameter erosionSpeedMultiplier = new ClampedFloatParameter(0.25f, 0.0f, 1.0f);

        /// <summary>
        /// Controls the vertical wind speed of the larger cloud shapes.
        /// </summary>
        [Tooltip("Controls the vertical wind speed of the larger cloud shapes.")]
        [AdditionalProperty]
        public FloatParameter verticalShapeWindSpeed = new FloatParameter(0.0f);

        /// <summary>
        /// Controls the vertical wind speed of the erosion cloud shapes.
        /// </summary>
        [Tooltip("Controls the vertical wind speed of the erosion cloud shapes.")]
        [AdditionalProperty]
        public FloatParameter verticalErosionWindSpeed = new FloatParameter(0.0f);

        /// <summary>
        /// Temporal accumulation increases the visual quality of clouds by decreasing the noise. A higher value will give you better quality but can create ghosting.
        /// </summary>
        [Tooltip("Temporal accumulation increases the visual quality of clouds by decreasing the noise. A higher value will give you better quality but can create ghosting.")]
        public ClampedFloatParameter temporalAccumulationFactor = new ClampedFloatParameter(0.95f, 0.0f, 1.0f);

        /// <summary>
        /// Enable/Disable the volumetric clouds ghosting reduction. When enabled, reduces significantly the ghosting of the volumetric clouds, but may introduce some flickering at lower temporal accumulation factors.
        /// </summary>
        [Tooltip("Enable/Disable the volumetric clouds ghosting reduction. When enabled, reduces significantly the ghosting of the volumetric clouds, but may introduce some flickering at lower temporal accumulation factors.")]
        public BoolParameter ghostingReduction = new BoolParameter(false);

        /// <summary>
        /// Specifies the strength of the perceptual blending for the volumetric clouds. This value should be treated as flag and only be set to 0.0 or 1.0.
        /// </summary>
        [Tooltip("Specifies the strength of the perceptual blending for the volumetric clouds. This value should be treated as flag and only be set to 0.0 or 1.0.")]
        public ClampedFloatParameter perceptualBlending = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        /// <summary>
        /// Enable/Disable the volumetric clouds shadow. This will override the cookie of your directional light and the cloud layer shadow (if active).
        /// </summary>
        [Tooltip("Enable/Disable the volumetric clouds shadow. This will override the cookie of your directional light and the cloud layer shadow (if active).")]
        public BoolParameter shadows = new BoolParameter(false);

        /// <summary>
        /// Specifies the resolution of the volumetric clouds shadow map.
        /// </summary>
        [Tooltip("Specifies the resolution of the volumetric clouds shadow map.")]
        public CloudShadowResolutionParameter shadowResolution = new CloudShadowResolutionParameter(CloudShadowResolution.Medium256);

        /// <summary>
        /// Controls the vertical offset applied to compute the volumetric clouds shadow in meters. To have accurate results, enter the average height at which the volumetric clouds shadow is received.
        /// </summary>
        [Tooltip("Controls the vertical offset applied to compute the volumetric clouds shadow in meters. To have accurate results, enter the average height at which the volumetric clouds shadow is received.")]
        public FloatParameter shadowPlaneHeightOffset = new FloatParameter(0.0f);

        /// <summary>
        /// Sets the size of the area covered by shadow around the camera.
        /// </summary>
        [Tooltip("Sets the size of the area covered by shadow around the camera.")]
        [AdditionalProperty]
        public MinFloatParameter shadowDistance = new MinFloatParameter(8000.0f, 1000.0f);

        /// <summary>
        /// Controls the opacity of the volumetric clouds shadow.
        /// </summary>
        [Tooltip("Controls the opacity of the volumetric clouds shadow.")]
        [AdditionalProperty]
        public ClampedFloatParameter shadowOpacity = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        /// <summary>
        /// Controls the shadow opacity when outside the area covered by the volumetric clouds shadow.
        /// </summary>
        [Tooltip("Controls the shadow opacity when outside the area covered by the volumetric clouds shadow.")]
        [AdditionalProperty]
        public ClampedFloatParameter shadowOpacityFallback = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        void ApplyCurrentCloudPreset()
        {
            // Apply the currently set preset
            bool microDetails = cloudSimpleMode == CloudSimpleMode.Quality;
            switch (cloudPreset)
            {
                case VolumetricClouds.CloudPresets.Sparse:
                {
                    densityMultiplier.value = 0.4f;
                    if (microDetails)
                    {
                        shapeFactor.value = 0.925f;
                        shapeScale.value = 5.0f;
                        erosionFactor.value = 0.85f;
                        erosionScale.value = 75.0f;
                        microErosionFactor.value = 0.65f;
                        microErosionScale.value = 300.0f;
                    }
                    else
                    {
                        shapeFactor.value = 0.95f;
                        shapeScale.value = 5.0f;
                        erosionFactor.value = 0.8f;
                        erosionScale.value = 107.0f;
                    }

                    // Curves
                    densityCurve.value = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.05f, 1.0f), new Keyframe(0.75f, 1.0f), new Keyframe(1.0f, 0.0f));
                    erosionCurve.value = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.1f, 0.9f), new Keyframe(1.0f, 1.0f));
                    ambientOcclusionCurve.value = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.25f, 0.5f), new Keyframe(1.0f, 0.0f));

                    // Layer properties
                    bottomAltitude.value = 3000.0f;
                    altitudeRange.value = 1000.0f;
                }
                break;
                case VolumetricClouds.CloudPresets.Cloudy:
                {
                    densityMultiplier.value = 0.4f;

                    if (microDetails)
                    {
                        shapeFactor.value = 0.875f;
                        shapeScale.value = 5.0f;
                        erosionFactor.value = 0.9f;
                        erosionScale.value = 75.0f;
                        microErosionFactor.value = 0.65f;
                        microErosionScale.value = 300.0f;
                    }
                    else
                    {
                        shapeFactor.value = 0.9f;
                        shapeScale.value = 5.0f;
                        erosionFactor.value = 0.8f;
                        erosionScale.value = 107.0f;
                    }

                    // Curves
                    densityCurve.value = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.15f, 1.0f), new Keyframe(1.0f, 0.1f));
                    erosionCurve.value = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.1f, 0.9f), new Keyframe(1.0f, 1.0f));
                    ambientOcclusionCurve.value = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.25f, 0.4f), new Keyframe(1.0f, 0.0f));

                    // Layer properties
                    bottomAltitude.value = 1200.0f;
                    altitudeRange.value = 2000.0f;
                }
                break;
                case VolumetricClouds.CloudPresets.Overcast:
                {
                    densityMultiplier.value = 0.3f;

                    if (microDetails)
                    {
                        shapeFactor.value = 0.45f;
                        shapeScale.value = 5.0f;
                        erosionFactor.value = 0.7f;
                        erosionScale.value = 75.0f;
                        microErosionFactor.value = 0.5f;
                        microErosionScale.value = 300.0f;
                    }
                    else
                    {
                        shapeFactor.value = 0.5f;
                        shapeScale.value = 5.0f;
                        erosionFactor.value = 0.5f;
                        erosionScale.value = 107.0f;
                    }

                    // Curves
                    densityCurve.value = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.05f, 1.0f), new Keyframe(0.9f, 0.0f), new Keyframe(1.0f, 0.0f));
                    erosionCurve.value = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.1f, 0.9f), new Keyframe(1.0f, 1.0f));
                    ambientOcclusionCurve.value = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(1.0f, 0.0f));

                    // Layer properties
                    bottomAltitude.value = 1500.0f;
                    altitudeRange.value = 2500.0f;
                }
                break;
                case VolumetricClouds.CloudPresets.Stormy:
                {
                    densityMultiplier.value = 0.35f;

                    if (microDetails)
                    {
                        shapeFactor.value = 0.825f;
                        shapeScale.value = 5.0f;
                        erosionFactor.value = 0.9f;
                        erosionScale.value = 75.0f;
                        microErosionFactor.value = 0.6f;
                        microErosionScale.value = 300.0f;
                    }
                    else
                    {
                        shapeFactor.value = 0.85f;
                        shapeScale.value = 5.0f;
                        erosionFactor.value = 0.75f;
                        erosionScale.value = 107.0f;
                    }

                    // Curves
                    densityCurve.value = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.037f, 1.0f), new Keyframe(0.6f, 1.0f), new Keyframe(1.0f, 0.0f));
                    erosionCurve.value = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.05f, 0.8f), new Keyframe(0.2438f, 0.9498f), new Keyframe(0.5f, 1.0f), new Keyframe(0.93f, 0.9268f), new Keyframe(1.0f, 1.0f));
                    ambientOcclusionCurve.value = new AnimationCurve(new Keyframe(0f, 0f), new Keyframe(0.1f, 0.4f), new Keyframe(1.0f, 0.0f));

                    // Layer properties
                    bottomAltitude.value = 1000.0f;
                    altitudeRange.value = 5000.0f;
                }
                break;
                default:
                    break;
            }
        }

        VolumetricClouds()
        {
            displayName = "Volumetric Clouds";
        }
    }
}
