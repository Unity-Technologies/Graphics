using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    [Serializable, VolumeComponentMenu("Post-processing/Motion Blur")]
    public sealed class MotionBlur : VolumeComponentWithQuality, IPostProcessComponent
    {
        [Tooltip("Sets the intensity of the motion blur effect. Acts as a multiplier for velocities.")]
        public MinFloatParameter intensity = new MinFloatParameter(0.0f, 0.0f);
        [Tooltip("Controls the maximum velocity, in pixels, that HDRP allows for all sources of motion blur except Camera rotation.")]
        public ClampedFloatParameter maximumVelocity = new ClampedFloatParameter(200.0f, 0.0f, 1500.0f);
        [Tooltip("Controls the minimum velocity, in pixels, that a GameObject must have to contribute to the motion blur effect.")]
        public ClampedFloatParameter minimumVelocity = new ClampedFloatParameter(2.0f, 0.0f, 64.0f);
        [Tooltip("Sets the maximum length, as a fraction of the screen's full resolution, that the velocity resulting from Camera rotation can have.")]
        public ClampedFloatParameter cameraRotationVelocityClamp = new ClampedFloatParameter(0.03f, 0.0f, 0.2f);

        [Tooltip("Value used for the depth based weighting of samples. Tweak if unwanted leak of background onto foreground or viceversa is detected.")]
        public ClampedFloatParameter depthComparisonExtent = new ClampedFloatParameter(1.0f, 0.0f, 20.0f);

        public int sampleCount
        {
            get
            {
                if (!UsesQualitySettings())
                {
                    return m_SampleCount.value;
                }
                else
                {
                    int qualityLevel = (int)quality.levelAndOverride.level;
                    return GetPostProcessingQualitySettings().MotionBlurSampleCount[qualityLevel];
                }
            }
            set { m_SampleCount.value = value; }
        }

        [Tooltip("Sets the maximum number of sample points that HDRP uses to compute motion blur.")]
        [SerializeField, FormerlySerializedAs("sampleCount")]
        MinIntParameter m_SampleCount = new MinIntParameter(8, 2);

        public bool IsActive()
        {
            return intensity.value > 0.0f;
        }
    }
}
