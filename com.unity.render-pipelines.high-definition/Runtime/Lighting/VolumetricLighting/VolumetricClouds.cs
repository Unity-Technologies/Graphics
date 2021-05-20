using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the ambient occlusion.
    /// </summary>
    [Serializable, VolumeComponentMenu("Sky/Volumetric Clouds")]
    [HDRPHelpURLAttribute("Override-Volumetric-Clouds")]
    public sealed class VolumetricClouds : VolumeComponent
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
            public CloudControlParameter(CloudControl value, bool overrideState = false) : base(value, overrideState) {}
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
            public CloudPresetsParameter(CloudPresets value, bool overrideState = false) : base(value, overrideState) {}
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
        }

        /// <summary> </summary>
        public const int CloudShadowResolutionCount = 4;

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
            public CloudShadowResolutionParameter(CloudShadowResolution value, bool overrideState = false) : base(value, overrideState) {}
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
            public CloudMapResolutionParameter(CloudMapResolution value, bool overrideState = false) : base(value, overrideState) {}
        }

        /// <summary>
        /// Enable/Disable the volumetric clouds effect.
        /// </summary>
        [Tooltip("Enable/Disable the volumetric clouds effect.")]
        public BoolParameter enable = new BoolParameter(false);

        /// <summary>
        /// When enabled, clouds are part of the scene and you can interact with them. This means for example, you can move around the clouds, clouds can appear between the Camera and other GameObjects, and the Camera's clipping planes affects the clouds. When disabled, the clouds are part of the skybox. This mean the clouds and their shadows appear relative to the Camera and always appear behind geometry.
        /// </summary>
        [Tooltip("When enabled, clouds are part of the scene and you can interact with them. This means for example, you can move around the clouds, clouds can appear between the Camera and other GameObjects, and the Camera's clipping planes affects the clouds. When disabled, the clouds are part of the skybox. This mean the clouds and their shadows appear relative to the Camera and always appear behind geometry.")]
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
        public MinFloatParameter lowestCloudAltitude = new MinFloatParameter(1000, 0.01f);

        /// <summary>
        /// Controls the thickness of the volumetric clouds volume in meters.
        /// </summary>
        [Tooltip("Controls the thickness of the volumetric clouds volume in meters.")]
        public MinFloatParameter cloudThickness = new MinFloatParameter(6000.0f, 100.0f);

        /// <summary>
        /// Controls the number of steps when evaluating the clouds' transmittance.
        /// </summary>
        [Tooltip("Controls the number of steps when evaluating the clouds' transmittance.")]
        public ClampedIntParameter numPrimarySteps = new ClampedIntParameter(48, 16, 512);

        /// <summary>
        /// Controls the number of steps when evaluating the clouds' lighting.
        /// </summary>
        [Tooltip("Controls the number of steps when evaluating the clouds' lighting.")]
        public ClampedIntParameter numLightSteps = new ClampedIntParameter(8, 6, 32);

        /// <summary>
        /// Specifies the cloud map - Coverage (R), Rain (G), Type (B).
        /// </summary>
        [Tooltip("Specifies the cloud map - Coverage (R), Rain (G), Type (B).")]
        public TextureParameter cloudMap = new TextureParameter(null);

        /// <summary>
        /// Specifies the lookup table for the clouds - Profile Coverage (R), Erosion (G), Ambient Occlusion (B).
        /// </summary>
        [Tooltip("Specifies the lookup table for the clouds - Profile Coverage (R), Erosion (G), Ambient Occlusion (B).")]
        public TextureParameter cloudLut = new TextureParameter(null);

        /// <summary>
        /// Specifies the cloud control Mode: Simple, Advanced or Manual.
        /// </summary>
        [Tooltip("Specifies the cloud control Mode: Simple, Advanced or Manual.")]
        public CloudControlParameter cloudControl = new CloudControlParameter(CloudControl.Simple);

        /// <summary>
        /// Specifies the weather preset in Simple mode.
        /// </summary>
        [Tooltip("Specifies the weather preset in Simple mode.")]
        public CloudPresetsParameter cloudPreset = new CloudPresetsParameter(CloudPresets.Cloudy);

        /// <summary>
        /// Specifies the lower cloud layer distribution in the advanced mode.
        /// </summary>
        [Tooltip("Specifies the lower cloud layer distribution in the advanced mode.")]
        public TextureParameter cumulusMap = new TextureParameter(null);

        /// <summary>
        /// Overrides the coverage of the lower cloud layer specified in the cumulus map in the advanced mode.
        /// </summary>
        [Tooltip("Overrides the coverage of the lower cloud layer specified in the cumulus map in the advanced mode.")]
        public ClampedFloatParameter cumulusMapMultiplier = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        /// <summary>
        /// Specifies the higher cloud layer distribution in the advanced mode.
        /// </summary>
        [Tooltip("Specifies the higher cloud layer distribution in the advanced mode.")]
        public TextureParameter altoStratusMap = new TextureParameter(null);

        /// <summary>
        /// Overrides the coverage of the higher cloud layer specified in the alto stratus map in the advanced mode.
        /// </summary>
        [Tooltip("Overrides the coverage of the higher cloud layer specified in the alto stratus map in the advanced mode.")]
        public ClampedFloatParameter altoStratusMapMultiplier = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        /// <summary>
        /// Specifies the anvil shaped clouds distribution in the advanced mode.
        /// </summary>
        [Tooltip("Specifies the anvil shaped clouds distribution in the advanced mode.")]
        public TextureParameter cumulonimbusMap = new TextureParameter(null);

        /// <summary>
        /// Overrides the coverage of the anvil shaped clouds specified in the cumulonimbus map in the advanced mode.
        /// </summary>
        [Tooltip("Overrides the coverage of the anvil shaped clouds specified in the cumulonimbus map in the advanced mode.")]
        public ClampedFloatParameter cumulonimbusMapMultiplier = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        /// <summary>
        /// Specifies the rain distribution in the advanced mode.
        /// </summary>
        [Tooltip("Specifies the rain distribution in the advanced mode.")]
        public TextureParameter rainMap = new TextureParameter(null);

        /// <summary>
        /// Specifies the internal texture resolution used for the cloud map in the advanced mode. A lower value will lead to higher performance, but less precise cloud type transitions.
        /// </summary>
        [Tooltip("Specifies the internal texture resolution used for the cloud map in the advanced mode. A lower value will lead to higher performance, but less precise cloud type transitions.")]
        public CloudMapResolutionParameter cloudMapResolution = new CloudMapResolutionParameter(CloudMapResolution.Medium64x64);

        /// <summary>
        /// Specifies the tint of the cloud scattering color.
        /// </summary>
        [Tooltip("Specifies the tint of the cloud scattering color.")]
        public ColorParameter scatteringTint = new ColorParameter(new Color(0.0f, 0.0f, 0.0f, 1.0f));

        /// <summary>
        /// Controls the amount of local scattering in the clouds. A value of 1 may provide a more powdery aspect.
        /// </summary>
        [Tooltip("Controls the amount of local scattering in the clouds. A value of 1 may provide a more powdery aspect.")]
        public ClampedFloatParameter powderEffectIntensity = new ClampedFloatParameter(0.8f, 0.0f, 1.0f);

        /// <summary>
        /// Controls the amount of multi-scattering inside the cloud.
        /// </summary>
        [Tooltip("Controls the amount of multi-scattering inside the cloud.")]
        public ClampedFloatParameter multiScattering = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        /// <summary>
        /// Controls the global density of the cloud volume.
        /// </summary>
        [Tooltip("Controls the global density of the cloud volume.")]
        public ClampedFloatParameter densityMultiplier = new ClampedFloatParameter(0.25f, 0.0f, 1.0f);

        /// <summary>
        /// Controls the larger noise passing through the cloud coverage. A higher value will yield less cloud coverage and smaller clouds.
        /// </summary>
        [Tooltip("Controls the larger noise passing through the cloud coverage. A higher value will yield less cloud coverage and smaller clouds.")]
        public ClampedFloatParameter shapeFactor = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        /// <summary>
        /// Controls the size of the larger noise passing through the cloud coverage.
        /// </summary>
        [Tooltip("Controls the size of the larger noise passing through the cloud coverage.")]
        public ClampedFloatParameter shapeScale = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        /// <summary>
        /// Controls the smaller noise on the edge of the clouds. A higher value will erode clouds more significantly.
        /// </summary>
        [Tooltip("Controls the smaller noise on the edge of the clouds. A higher value will erode clouds more significantly.")]
        public ClampedFloatParameter erosionFactor = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        /// <summary>
        /// Controls the size of the smaller noise passing through the cloud coverage.
        /// </summary>
        [Tooltip("Controls the size of the smaller noise passing through the cloud coverage.")]
        public ClampedFloatParameter erosionScale = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        /// <summary>
        /// Controls the influence of the light probes on the cloud volume. A lower value will suppress the ambient light and produce darker clouds overall.
        /// </summary>
        [Tooltip("Controls the influence of the light probes on the cloud volume. A lower value will suppress the ambient light and produce darker clouds overall.")]
        public ClampedFloatParameter ambientLightProbeDimmer = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        /// <summary>
        /// Sets the global wind speed in kilometers per hour.
        /// </summary>
        [Tooltip("Sets the global wind speed in kilometers per hour.")]
        public MinFloatParameter globalWindSpeed = new MinFloatParameter(100.0f, 0.0f);

        /// <summary>
        /// Controls the orientation of the wind relative to the X world vector.
        /// </summary>
        [Tooltip("Controls the orientation of the wind relative to the X world vector.")]
        [AdditionalProperty]
        public ClampedFloatParameter orientation = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);

        /// <summary>
        /// Controls the multiplier to the speed of the cloud map.
        /// </summary>
        [Tooltip("Controls the multiplier to the speed of the cloud map.")]
        [AdditionalProperty]
        public ClampedFloatParameter cloudMapSpeedMultiplier = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        /// <summary>
        /// Controls the multiplier to the speed of the larger cloud shapes.
        /// </summary>
        [Tooltip("Controls the multiplier to the speed of the larger cloud shapes.")]
        [AdditionalProperty]
        public ClampedFloatParameter shapeSpeedMultiplier = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        /// <summary>
        /// Controls the multiplier to the speed of the erosion cloud shapes.
        /// </summary>
        [Tooltip("Controls the multiplier to the speed of the erosion cloud shapes.")]
        [AdditionalProperty]
        public ClampedFloatParameter erosionSpeedMultiplier = new ClampedFloatParameter(0.25f, 0.0f, 1.0f);

        /// <summary>
        /// Temporal accumulation increases the visual quality of clouds by decreasing the noise. A higher value will give you better quality but can create ghosting.
        /// </summary>
        [Tooltip("Temporal accumulation increases the visual quality of clouds by decreasing the noise. A higher value will give you better quality but can create ghosting.")]
        public ClampedFloatParameter temporalAccumulationFactor = new ClampedFloatParameter(0.95f, 0.0f, 1.0f);

        /// <summary>
        /// Enable/Disable the volumetric clouds shadow. This will override the cookie of your directional light and the cloud layer shadow (if active).
        /// </summary>
        [Tooltip("Enable/Disable the volumetric clouds shadow. This will override the cookie of your directional light and the cloud layer shadow (if active).")]
        public BoolParameter shadows = new BoolParameter(false);

        /// <summary>
        /// Enable/Disable the volumetric clouds shadow. This will override the cookie of your directional light and the cloud layer shadow (if active).
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
        public ClampedFloatParameter shadowOpacity = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        /// <summary>
        /// Controls the shadow opacity when outside the area covered by the volumetric clouds shadow.
        /// </summary>
        [Tooltip("Controls the shadow opacity when outside the area covered by the volumetric clouds shadow.")]
        [AdditionalProperty]
        public ClampedFloatParameter shadowOpacityFallback = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        VolumetricClouds()
        {
            displayName = "Volumetric Clouds (Preview)";
        }
    }
}
