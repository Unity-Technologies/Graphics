using System;
using UnityEngine.Serialization;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// Determine how the component of the motion vectors coming from the camera are clamped.
    /// </summary>
    public enum CameraClampMode
    {
        /// <summary>
        /// Camera derived motion vectors are not treated differently.
        /// </summary>
        None = 0,

        /// <summary>
        /// Clamp camera rotation separately.
        /// </summary>
        Rotation = 1,

        /// <summary>
        /// Clamp camera rotation separately.
        /// </summary>
        Translation = 2,

        /// <summary>
        /// Clamp camera rotation and translation separately.
        /// </summary>
        SeparateTranslationAndRotation = 3,

        /// <summary>
        /// Clamp the motion vectors coming from the camera entirely (translation and rotation included).
        /// </summary>
        FullCameraMotionVector = 4,
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="CameraClampMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class CameraClampModeParameter : VolumeParameter<CameraClampMode>
    {
        /// <summary>
        /// Creates a new <see cref="CameraClampModeParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public CameraClampModeParameter(CameraClampMode value, bool overrideState = false) : base(value, overrideState) { }
    }

    /// <summary>
    /// A volume component that holds settings for the Motion Blur effect.
    /// </summary>
    [Serializable, VolumeComponentMenu("Post-processing/Motion Blur")]
    [SupportedOnRenderPipeline(typeof(HDRenderPipelineAsset))]
    [HDRPHelpURL("Post-Processing-Motion-Blur")]
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
        /// If toggled on camera rotation can be clamped differently.
        /// </summary>

        [Header("Camera Velocity")]
        [AdditionalProperty]
        [Tooltip("If toggled off, the motion caused by the camera is not considered when doing motion blur.")]
        public BoolParameter cameraMotionBlur = new BoolParameter(true);


        /// <summary>
        /// Determine how the component of the motion vectors coming from the camera is clamped. It is important to remember that clamping the camera component separately, velocities relative to camera might change too (e.g. an object parented to a camera
        /// when the camera moves might not have a 0 motion vector anymore).
        /// </summary>
        [AdditionalProperty]
        [Tooltip("Determine if and how the component of the motion vectors coming from the camera is clamped in a special fashion.")]
        public CameraClampModeParameter specialCameraClampMode = new CameraClampModeParameter(CameraClampMode.None);

        /// <summary>
        /// Sets the maximum length, as a fraction of the screen's full resolution, that the motion vectors resulting from Camera movement can have. Only valid if specialCameraClampMode is set to CameraClampMode.FullCameraMotionVector.
        /// </summary>
        [AdditionalProperty]
        [Tooltip("Sets the maximum length, as a fraction of the screen's full resolution, that the motion vectors resulting from Camera can have.")]
        public ClampedFloatParameter cameraVelocityClamp = new ClampedFloatParameter(0.05f, 0.0f, 0.3f);

        /// <summary>
        /// Sets the maximum length, as a fraction of the screen's full resolution, that the motion vectors resulting from Camera can have. Only valid if specialCameraClampMode is set to CameraClampMode.Translation or CameraClampMode.SeparateTranslationAndRotation.
        /// </summary>
        [AdditionalProperty]
        [Tooltip("Sets the maximum length, as a fraction of the screen's full resolution, that the motion vectors resulting from Camera can have.")]
        public ClampedFloatParameter cameraTranslationVelocityClamp = new ClampedFloatParameter(0.05f, 0.0f, 0.3f);

        /// <summary>
        /// Sets the maximum length, as a fraction of the screen's full resolution, that the motion vectors resulting from Camera rotation can have. Only valid if specialCameraClampMode is set to CameraClampMode.Rotation or CameraClampMode.SeparateTranslationAndRotation.
        /// </summary>
        [AdditionalProperty]
        [Tooltip("Sets the maximum length, as a fraction of the screen's full resolution, that the motion vectors resulting from Camera rotation can have.")]
        public ClampedFloatParameter cameraRotationVelocityClamp = new ClampedFloatParameter(0.03f, 0.0f, 0.3f);

        /// <summary>
        /// Value used for the depth based weighting of samples. Tweak if unwanted leak of background onto foreground or viceversa is detected.
        /// </summary>
        [AdditionalProperty]
        [Tooltip("Value used for the depth based weighting of samples. Tweak if unwanted leak of background onto foreground or viceversa is detected.")]
        public ClampedFloatParameter depthComparisonExtent = new ClampedFloatParameter(1.0f, 0.0f, 20.0f);

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
