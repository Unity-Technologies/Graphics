using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the ambient occlusion.
    /// </summary>
    [Serializable, VolumeComponentMenu("Lighting/Volumetric Clouds")]
    public sealed class VolumetricClouds : VolumeComponent
    {
        [Tooltip("Enable/Disable the volumetric clouds effect.")]
        public BoolParameter enable = new BoolParameter(false);

        // The size of the cloud dome in kilometers around the center of the world
        [Tooltip("The size of the cloud dome in kilometers around the center of the world")]
        public ClampedFloatParameter cloudDomeSize = new ClampedFloatParameter(32.0f, 16.0f, 128.0f);

        [Tooltip("Tiling (x,y) and offset (z,w) of the cloud map")]
        public Vector4Parameter cloudTiling = new Vector4Parameter(new Vector4(1.0f, 1.0f, 0.0f, 0.0f));

        [Tooltip("Altitude in meters of the lowest cloud")]
        public ClampedFloatParameter lowestCloudAltitude = new ClampedFloatParameter(1500f, 600f, 1500f);

        [Tooltip("Altitude in meters of the highest cloud")]
        public ClampedFloatParameter highestCloudAltitude = new ClampedFloatParameter(8000, 1500f, 8000f);

        [Tooltip("Number of camera->cloud steps")]
        public ClampedIntParameter numPrimarySteps = new ClampedIntParameter(48, 8, 256);

        [Tooltip("Number of cloud-> light steps")]
        public ClampedIntParameter numLightSteps = new ClampedIntParameter(6, 4, 32);

        [Tooltip("Cloud map (Coverage, Rain, Type)")]
        public TextureParameter cloudMap = new TextureParameter(null);

        [Tooltip("Cloud type/height map")]
        public TextureParameter cloudLut = new TextureParameter(null);

        [Tooltip("Direction of the scattering. 0.0 is backward 1.0 is forward")]
        public ClampedFloatParameter scatteringDirection = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        [Tooltip("Intensity of the back scattering function")]
        public ClampedFloatParameter powderEffectIntensity = new ClampedFloatParameter(0.8f, 0.0f, 1.0f);

        [Tooltip("Intensity of the multi-scattering")]
        public ClampedFloatParameter multiScattering = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        [Tooltip("Global density multiplier")]
        public ClampedFloatParameter densityMultiplier = new ClampedFloatParameter(0.25f, 0.0f, 1.0f);

        [Tooltip("Density amplifier for larger cloud types")]
        public ClampedFloatParameter densityAmplifier = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        [Tooltip("Erosion factor")]
        public ClampedFloatParameter erosionFactor = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        [Tooltip("Value to control the intensity of the ambient light probe when lighting the clouds")]
        public ClampedFloatParameter ambientLightProbeDimmer = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        [Tooltip("Global wind speed in kilometers per hour.")]
        public MinFloatParameter globalWindSpeed = new MinFloatParameter(50.0f, 0.0f);

        [Tooltip("Rotation of the wind in degrees.")]
        public ClampedFloatParameter windRotation = new ClampedFloatParameter(0.0f, 0.0f, 360.0f);

        [Tooltip("Relative wind speed of the large cloud shapes.")]
        public ClampedFloatParameter largeCloudsWindSpeed = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        [Tooltip("Relative wind speed of the medium cloud shapes.")]
        public ClampedFloatParameter mediumCloudsWindSpeed = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        [Tooltip("Relative wind speed of the local cloud shapes.")]
        public ClampedFloatParameter smallCloudsWindSpeed = new ClampedFloatParameter(0.25f, 0.0f, 1.0f);

        [Tooltip("Global temporal accumulation factor.")]
        public ClampedFloatParameter temporalAccumulationFactor = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        public VolumetricClouds()
        {
            displayName = "Volumetric Clouds (Preview)";
        }
    }
}
