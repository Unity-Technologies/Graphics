using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the ambient occlusion.
    /// </summary>
    [Serializable, VolumeComponentMenu("Sky/Volumetric Clouds")]
    public sealed class VolumetricClouds : VolumeComponent
    {
        public enum CloudControl
        {
            Simple,
            Advanced,
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

        public enum CloudPresets
        {
            Sparse,
            Cloudy,
            Overcast,
            StormClouds,
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

        public enum CloudShadowResolution
        {
            VeryLow64 = 64,
            Low128 = 128,
            Medium256 = 256,
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

        public enum CloudMapResolution
        {
            Low32x32 = 32,
            Medium64x64 = 64,
            High128x128 = 128,
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

        [Tooltip("Enable/Disable the volumetric clouds effect.")]
        public BoolParameter enable = new BoolParameter(false);

        // The size of the cloud dome in kilometers around the center of the world
        [Tooltip("Controls the curvature of the cloud volume which defines the distance at which the clouds intersect with the horizon.")]
        public ClampedFloatParameter earthCurvature = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        [Tooltip("Tiling (x,y) of the cloud map")]
        public Vector2Parameter cloudTiling = new Vector2Parameter(new Vector2(1.0f, 1.0f));

        [Tooltip("Offset (x,y) of the cloud map")]
        public Vector2Parameter cloudOffset = new Vector2Parameter(new Vector2(0.0f, 0.0f));

        [Tooltip("Altitude of the lowest cloud in meters.")]
        public MinFloatParameter lowestCloudAltitude = new MinFloatParameter(500, 0.01f);

        [Tooltip("Controls the thickness of the volumetric clouds volume in meters.")]
        public MinFloatParameter cloudThickness = new MinFloatParameter(3000.0f, 100.0f);

        [Tooltip("Controls the number of steps when evaluating the clouds' transmittance.")]
        public ClampedIntParameter numPrimarySteps = new ClampedIntParameter(48, 16, 512);

        [Tooltip("Controls the number of steps when evaluating the clouds' lighting.")]
        public ClampedIntParameter numLightSteps = new ClampedIntParameter(8, 6, 32);

        [Tooltip("Specifies the cloud map - Coverage (R), Rain (G), Type (B).")]
        public TextureParameter cloudMap = new TextureParameter(null);

        [Tooltip("Specifies the lookup table for the clouds - Profile Coverage (R), Erosion (G), Ambient Occlusion (B).")]
        public TextureParameter cloudLut = new TextureParameter(null);

        [Tooltip("Specifies the cloud control Mode: Simple, Advanced or Manual.")]
        public CloudControlParameter cloudControl = new CloudControlParameter(CloudControl.Simple);

        [Tooltip("Specifies the weather preset in Simple mode.")]
        public CloudPresetsParameter cloudPreset = new CloudPresetsParameter(CloudPresets.Cloudy);

        [Tooltip("Controls the lower cloud layer distribution in the advanced mode.")]
        public TextureParameter cumulusMap = new TextureParameter(null);

        [Tooltip("Overrides the coverage of the lower cloud layer specified in the cumulus map in the advanced mode.")]
        public ClampedFloatParameter cumulusMapMultiplier = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        [Tooltip("Control the higher cloud layer distribution in the advanced mode.")]
        public TextureParameter altoStratusMap = new TextureParameter(null);

        [Tooltip("Overrides the coverage of the higher cloud layer specified in the alto stratus map in the advanced mode.")]
        public ClampedFloatParameter altoStratusMapMultiplier = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        [Tooltip("Control the anvil shaped clouds distribution in the advanced mode.")]
        public TextureParameter cumulonimbusMap = new TextureParameter(null);

        [Tooltip("Overrides the coverage of the anvil shaped clouds specified in the cumulonimbus map in the advanced mode.")]
        public ClampedFloatParameter cumulonimbusMapMultiplier = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        [Tooltip("Control the rain distribution in the advanced mode.")]
        public TextureParameter rainMap = new TextureParameter(null);

        [Tooltip("Controls the internal texture resolution used for the cloud map in the advanced mode. A lower value will lead to higher performance, but less precise cloud type transitions.")]
        public CloudMapResolutionParameter cloudMapResolution = new CloudMapResolutionParameter(CloudMapResolution.Medium64x64);

        [Tooltip("Direction of the scattering. 0.0 is backward 1.0 is forward.")]
        public ClampedFloatParameter scatteringDirection = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        [Tooltip("Specifies the tint of the cloud scattering color.")]
        public ColorParameter scatteringTint = new ColorParameter(new Color(0.0f, 0.0f, 0.0f, 1.0f));

        [Tooltip("Controls the amount of local scattering in the clouds. A value of 1 may provide a more powdery aspect.")]
        public ClampedFloatParameter powderEffectIntensity = new ClampedFloatParameter(0.8f, 0.0f, 1.0f);

        [Tooltip("Controls the amount of multi-scattering inside the cloud.")]
        public ClampedFloatParameter multiScattering = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        [Tooltip("Controls the global density of the cloud volume.")]
        public ClampedFloatParameter densityMultiplier = new ClampedFloatParameter(0.25f, 0.0f, 1.0f);

        [Tooltip("Controls the larger noise passing through the cloud coverage. A higher value will yield less cloud coverage and smaller clouds.")]
        public ClampedFloatParameter shapeFactor = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        [Tooltip("Controls the size of the larger noise passing through the cloud coverage.")]
        public ClampedFloatParameter shapeScale = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        [Tooltip("Controls the smaller noise on the edge of the clouds. A higher value will erode clouds more significantly.")]
        public ClampedFloatParameter erosionFactor = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        [Tooltip("Controls the size of the smaller noise passing through the cloud coverage.")]
        public ClampedFloatParameter erosionScale = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        [Tooltip("Controls the influence of the light probes on the cloud volume. A lower value will suppress the ambient light and produce darker clouds overall.")]
        public ClampedFloatParameter ambientLightProbeDimmer = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        [Tooltip("Sets the global wind speed in kilometers per hour.")]
        public MinFloatParameter globalWindSpeed = new MinFloatParameter(50.0f, 0.0f);

        [Tooltip("Controls the orientation of the wind relative to the X world vector.")]
        public ClampedFloatParameter orientation = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);

        [Tooltip("Multiplier to the speed of the cloud map.")]
        public ClampedFloatParameter cloudMapSpeedMultiplier = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        [Tooltip("Controls the multiplier to the speed of the larger cloud shapes.")]
        public ClampedFloatParameter shapeSpeedMultiplier = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        [Tooltip("Controls the multiplier to the speed of the erosion cloud shapes.")]
        public ClampedFloatParameter erosionSpeedMultiplier = new ClampedFloatParameter(0.25f, 0.0f, 1.0f);

        [Tooltip("Temporal accumulation increases the visual quality of clouds by decreasing the noise. A higher value will give you better quality but can create ghosting.")]
        public ClampedFloatParameter temporalAccumulationFactor = new ClampedFloatParameter(0.95f, 0.0f, 1.0f);

        [Tooltip("Enable/Disable the volumetric clouds shadow. This will override the cookie of your directional light and the cloud layer shadow (if active).")]
        public BoolParameter shadows = new BoolParameter(false);

        [Tooltip("Specifies the resolution of the volumetric clouds shadow map.")]
        public CloudShadowResolutionParameter shadowResolution = new CloudShadowResolutionParameter(CloudShadowResolution.Medium256);

        [Tooltip("Vertical offset applied to compute the volumetric clouds shadow.")]
        public FloatParameter shadowPlaneHeightOffset = new FloatParameter(0.0f);

        [Tooltip("Sets the size of the area covered by shadow around the camera.")]
        public MinFloatParameter shadowDistance = new MinFloatParameter(8000.0f, 1000.0f);

        [Tooltip("Controls the opacity of the volumetric clouds shadow.")]
        public ClampedFloatParameter shadowOpacity = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        [Tooltip("Controls the shadow opacity when outside the area covered by the volumetric clouds shadow.")]
        public ClampedFloatParameter shadowOpacityFallback = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);

        public VolumetricClouds()
        {
            displayName = "Volumetric Clouds (Preview)";
        }
    }
}
