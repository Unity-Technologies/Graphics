using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A volume component that holds settings for the Motion Blur effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/Motion Blur")]
    public sealed class MotionBlur : VolumeComponentWithQuality, IPostProcessComponent
    {
        /// <summary>
        /// Sets the intensity of the motion blur effect. Acts as a multiplier for velocities.
        /// </summary>
        [Tooltip("Sets the intensity of the motion blur effect. Acts as a multiplier for velocities.")]
        public MinFloatParameter intensity = new MinFloatParameter(0.0f, 0.0f);

        /// <summary>
        /// Controls the maximum velocity, in pixels, that HDRP allows for all sources of motion blur except Camera rotation.
        /// </summary>
        [Tooltip("Controls the maximum velocity, in pixels, that HDRP allows for all sources of motion blur except Camera rotation.")]
        public ClampedFloatParameter maximumVelocity = new ClampedFloatParameter(200.0f, 0.0f, 1500.0f);

        /// <summary>
        /// Controls the minimum velocity, in pixels, that a GameObject must have to contribute to the motion blur effect.
        /// </summary>
        [Tooltip("Controls the minimum velocity, in pixels, that a GameObject must have to contribute to the motion blur effect.")]
        public ClampedFloatParameter minimumVelocity = new ClampedFloatParameter(2.0f, 0.0f, 64.0f);

        /// <summary>
        /// Sets the maximum length, as a fraction of the screen's full resolution, that the velocity resulting from Camera rotation can have.
        /// </summary>
        [Tooltip("Sets the maximum length, as a fraction of the screen's full resolution, that the velocity resulting from Camera rotation can have.")]
        public ClampedFloatParameter cameraRotationVelocityClamp = new ClampedFloatParameter(0.03f, 0.0f, 0.2f);

        /// <summary>
        /// Value used for the depth based weighting of samples. Tweak if unwanted leak of background onto foreground or viceversa is detected.
        /// </summary>
        [Tooltip("Value used for the depth based weighting of samples. Tweak if unwanted leak of background onto foreground or viceversa is detected.")]
        public ClampedFloatParameter depthComparisonExtent = new ClampedFloatParameter(1.0f, 0.0f, 20.0f);

        /// <summary>
        /// If toggled off, the motion caused by the camera is not considered when doing motion blur.
        /// </summary>
        [Tooltip("If toggled off, the motion caused by the camera is not considered when doing motion blur.")]
        public BoolParameter cameraMotionBlur = new BoolParameter(true);

        /// <summary>
        /// Sets the maximum number of sample points that HDRP uses to compute motion blur.
        /// </summary>
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

        /// <summary>
        /// Tells if the effect needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive()
        {
            return intensity.value > 0.0f;
        }
    }
}
