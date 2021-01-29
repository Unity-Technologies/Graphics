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
            VeryLow = 64,
            Low = 128,
            Medium = 256,
            High = 512,
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

        [Tooltip("Enable/Disable the volumetric clouds effect.")]
        public BoolParameter enable = new BoolParameter(false);

        // The size of the cloud dome in kilometers around the center of the world
        [Tooltip("Radius of the earth")]
        public ClampedFloatParameter earthRadiusMultiplier = new ClampedFloatParameter(0.5f, 0.025f, 1.0f);

        [Tooltip("Tiling (x,y) and offset (z,w) of the cloud map")]
        public Vector4Parameter cloudTiling = new Vector4Parameter(new Vector4(1.0f, 1.0f, 0.0f, 0.0f));

        [Tooltip("Altitude in meters of the lowest cloud")]
        public MinFloatParameter lowestCloudAltitude = new MinFloatParameter(1500f, 0.01f);

        [Tooltip("Thickness of the cloud volume")]
        public MinFloatParameter cloudThickness = new MinFloatParameter(6500.0f, 100.0f);

        [Tooltip("Number of camera->cloud steps")]
        public ClampedIntParameter numPrimarySteps = new ClampedIntParameter(48, 16, 512);

        [Tooltip("Number of cloud-> light steps")]
        public ClampedIntParameter numLightSteps = new ClampedIntParameter(8, 6, 32);

        [Tooltip("Cloud map (Coverage, Rain, Type)")]
        public TextureParameter cloudMap = new TextureParameter(null);

        [Tooltip("Cloud Control Mode")]
        public CloudControlParameter cloudControl = new CloudControlParameter(CloudControl.Simple);

        [Tooltip("Cloud Preset Mode")]
        public CloudPresetsParameter cloudPresets = new CloudPresetsParameter(CloudPresets.Cloudy);

        [Tooltip("Cloud type/height map")]
        public TextureParameter cloudLut = new TextureParameter(null);

        [Tooltip("Direction of the scattering. 0.0 is backward 1.0 is forward")]
        public ClampedFloatParameter scatteringDirection = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        [Tooltip("Tint of the cloud scattering")]
        public ColorParameter scatteringTint = new ColorParameter(new Color(0.0f, 0.0f, 0.0f, 1.0f));

        [Tooltip("Intensity of the back scattering function")]
        public ClampedFloatParameter powderEffectIntensity = new ClampedFloatParameter(0.8f, 0.0f, 1.0f);

        [Tooltip("Intensity of the multi-scattering")]
        public ClampedFloatParameter multiScattering = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        [Tooltip("Global density multiplier")]
        public ClampedFloatParameter densityMultiplier = new ClampedFloatParameter(0.25f, 0.0f, 1.0f);

        [Tooltip("Shape factor")]
        public ClampedFloatParameter shapeFactor = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        [Tooltip("Erosion factor")]
        public ClampedFloatParameter erosionFactor = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        [Tooltip("Value to control the intensity of the ambient light probe when lighting the clouds")]
        public ClampedFloatParameter ambientLightProbeDimmer = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        [Tooltip("Global wind speed in kilometers per hour.")]
        public MinFloatParameter globalWindSpeed = new MinFloatParameter(50.0f, 0.0f);

        [Tooltip("Rotation of the wind in degrees.")]
        public ClampedFloatParameter windRotation = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);

        [Tooltip("Multiplier to the speed of the cloud map.")]
        public ClampedFloatParameter cloudMapWindSpeedMultiplier = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        [Tooltip("Multiplier to the speed of the larger cloud shapes.")]
        public ClampedFloatParameter shapeWindSpeedMultiplier = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        [Tooltip("Multiplier to the speed of the erosion cloud shapes.")]
        public ClampedFloatParameter erosionWindSpeedMultiplier = new ClampedFloatParameter(0.25f, 0.0f, 1.0f);

        [Tooltip("Global temporal accumulation factor.")]
        public ClampedFloatParameter temporalAccumulationFactor = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        [Tooltip("Enable/Disable the volumetric clouds shadow. This will override the cookie of your directional light and the cloud layer shadow (if active).")]
        public BoolParameter shadow = new BoolParameter(false);

        [Tooltip("Controls the resolution of the volumetric clouds shadow map.")]
        public CloudShadowResolutionParameter shadowResolution = new CloudShadowResolutionParameter(CloudShadowResolution.Medium);

        [Tooltip("Shift applied to compute the volumetric clouds shadow.")]
        public FloatParameter shadowPlaneOffset = new FloatParameter(0.0f);

        [Tooltip("Controls the size of the volumetric clouds shadow in meters.")]
        public MinFloatParameter shadowSize = new MinFloatParameter(16000.0f, 1000.0f);

        [Tooltip("Controls the intensity of the volumetric clouds shadow.")]
        public ClampedFloatParameter shadowIntensity = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        [Tooltip("Controls value that is used to fallback on when the volumetric clouds shadow is not define.")]
        public ClampedFloatParameter shadowFallbackValue = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        public VolumetricClouds()
        {
            displayName = "Volumetric Clouds (Preview)";
        }
    }
}
