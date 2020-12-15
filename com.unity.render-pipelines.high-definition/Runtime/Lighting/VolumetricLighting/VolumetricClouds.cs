using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the ambient occlusion.
    /// </summary>
    [Serializable, VolumeComponentMenu("Lighting/Volumetric Clouds")]
    public sealed class VolumetricClouds : VolumeComponent
    {
        // Enable/Disable the volumetric clouds effect
        public BoolParameter enable = new BoolParameter(false);

        // The size of the cloud dome in kilometers around the center of the world
        public ClampedFloatParameter cloudDomeSize = new ClampedFloatParameter(32.0f, 16.0f, 64.0f);

        // Lower cloud altitude above the camera
        public ClampedFloatParameter lowestCloudAltitude = new ClampedFloatParameter(1500f, 600f, 1500f);

        // Highest cloud altitude above the camera
        public ClampedFloatParameter highestCloudAltitude = new ClampedFloatParameter(8000, 1500f, 8000f);

        // Number of camera->cloud steps
        public ClampedIntParameter numPrimarySteps = new ClampedIntParameter(48, 8, 256);

        // Number of cloud-> light steps
        public ClampedIntParameter numLightSteps = new ClampedIntParameter(6, 4, 32);

        // Cloud map (Coverage, Rain, Type)
        public TextureParameter cloudMap = new TextureParameter(null);

        // Cloud type/height map
        public TextureParameter cloudLut = new TextureParameter(null);

        // Blend between forward and back scattering
        public ClampedFloatParameter scatteringDirection = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        // Intensity of the back scattering function
        public ClampedFloatParameter powderEffectIntensity = new ClampedFloatParameter(0.8f, 0.0f, 1.0f);

        // The intensity of the multi scattering
        public ClampedFloatParameter multiScattering = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        // Global density multiplier
        public ClampedFloatParameter densityMultiplier = new ClampedFloatParameter(0.25f, 0.0f, 1.0f);

        // Density amplifier for the bigger clouds
        public ClampedFloatParameter densityAmplifier = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        // Erosion factor intensity
        public ClampedFloatParameter erosionFactor = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        // Value to control the intensity of the ambient light probe when lighting the clouds
        public ClampedFloatParameter ambientLightProbeDimmer = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        // The global wind speed
        public ClampedFloatParameter globalWindSpeed = new ClampedFloatParameter(0.25f, 0.0f, 1.0f);

        // The  rotation angle of the wind in the horizontal place
        public ClampedFloatParameter windRotation = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        // Large cloud wind speed
        public ClampedFloatParameter largeCloudsWindSpeed = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        // Medium cloud wind speed
        public ClampedFloatParameter mediumCloudsWindSpeed = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);

        // Small cloud wind speed
        public ClampedFloatParameter smallCloudsWindSpeed = new ClampedFloatParameter(0.25f, 0.0f, 1.0f);

        // Global accumulation factor
        public ClampedFloatParameter temporalAccumulationFactor = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        public VolumetricClouds()
        {
            displayName = "Volumetric Clouds (Preview)";
        }
    }
}
