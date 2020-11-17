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
        public ClampedFloatParameter highestCloudAltitude = new ClampedFloatParameter(4000f, 1500f, 4000f);
        public ClampedIntParameter numPrimarySteps = new ClampedIntParameter(16, 8, 64);
        public ClampedIntParameter numLightSteps = new ClampedIntParameter(10, 4, 32);
        public TextureParameter cloudMap = new TextureParameter(null);
        public TextureParameter cloudLut = new TextureParameter(null);
        public ColorParameter scatteringTint = new ColorParameter(new Color(1.0f, 1.0f, 1.0f));
        public ClampedFloatParameter eccentricity = new ClampedFloatParameter(0.0f, -1.0f, 1.0f);
        public ClampedFloatParameter silverIntensity = new ClampedFloatParameter(0.0f, 0.0f, 1.0f);
        public ClampedFloatParameter silverSpread = new ClampedFloatParameter(0.0f, 0.0f, 2.0f);
        public ClampedFloatParameter globalLightProbeDimmer = new ClampedFloatParameter(1.0f, 0.0f, 1.0f);
        public VolumetricClouds()
        {
            displayName = "Volumetric Clouds (Preview)";
        }
    }
}
