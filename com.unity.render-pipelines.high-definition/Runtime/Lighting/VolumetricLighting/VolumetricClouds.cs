using System;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the ambient occlusion.
    /// </summary>
    [Serializable, VolumeComponentMenu("Lighting/Volumetric Clouds")]
    public sealed class VolumetricClouds : VolumeComponent
    {
        public BoolParameter enable = new BoolParameter(false);
        public ClampedFloatParameter cloudDomeSize = new ClampedFloatParameter(10.0f, 0.1f, 32.0f);
        public ClampedFloatParameter lowestCloudAltitude = new ClampedFloatParameter(1500f, 600f, 1500f);
        public ClampedFloatParameter highestCloudAltitude = new ClampedFloatParameter(4000f, 1500f, 8000f);
        public ClampedIntParameter numPrimarySteps = new ClampedIntParameter(16, 8, 512);
        public ClampedIntParameter numLightSteps = new ClampedIntParameter(10, 4, 64);
        public TextureParameter cloudMap = new TextureParameter(null);
        public TextureParameter cloudLut = new TextureParameter(null);
        public ColorParameter scatteringTint = new ColorParameter(new Color(1.0f, 1.0f, 1.0f));
        public ClampedFloatParameter eccentricityF = new ClampedFloatParameter(0.8f, -1.0f, 1.0f);
        public ClampedFloatParameter eccentricityB = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        public ClampedFloatParameter phaseFunctionBlend = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        public ClampedFloatParameter powderEffectIntensity = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        public ClampedFloatParameter multiScattering = new ClampedFloatParameter(0.5f, 0.0f, 1.0f);
        public ClampedFloatParameter densityMultiplier = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        public ClampedFloatParameter globalLightProbeDimmer = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        public ClampedFloatParameter windRotation = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);

        public VolumetricClouds()
        {
            displayName = "Volumetric Clouds (Preview)";
        }
    }
}
