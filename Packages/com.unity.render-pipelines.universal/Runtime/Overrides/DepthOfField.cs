using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Focusing modes for the depth of field effect.
    /// </summary>
    public enum DepthOfFieldMode
    {
        /// <summary>
        /// Disables depth of field.
        /// </summary>
        Off,

        /// <summary>
        /// Use this for faster but non physical depth of field.
        /// </summary>
        Gaussian, // Non physical, fast, small radius, far blur only

        /// <summary>
        /// Use this for a more realistic but slower depth of field.
        /// </summary>
        Bokeh
    }

    /// <summary>
    /// A volume component that holds settings for the Depth Of Field effect.
    /// </summary>
    /// <remarks>
    /// You can add <see cref="VolumeComponent"/> to a <see cref="VolumeProfile"/> in the Editor to apply a Depth Of Field post-processing effect.
    /// </remarks>
    /// <example>
    /// <para>This sample code shows how settings can be retrieved and modified in runtime:</para>
    /// <code>
    /// using System;
    /// using UnityEngine;
    /// using UnityEngine.Rendering;
    /// using UnityEngine.Rendering.Universal;
    ///
    /// public class ModifyVolumeComponent : MonoBehaviour
    /// {
    ///     [SerializeField] VolumeProfile volumeProfile;
    ///     [SerializeField] VolumeSettings volumeSettings;
    ///
    ///     private bool m_HasRetrievedVolumeComponent;
    ///     private DepthOfField m_VolumeComponent;
    ///
    ///     [Serializable]
    ///     private struct VolumeSettings
    ///     {
    ///         public bool active;
    ///         public DepthOfFieldModeParameter mode;
    ///         public MinFloatParameter gaussianStart;
    ///         public MinFloatParameter gaussianEnd;
    ///         public ClampedFloatParameter gaussianMaxRadius;
    ///         public BoolParameter highQualitySampling;
    ///         public MinFloatParameter focusDistance;
    ///         public ClampedFloatParameter aperture;
    ///         public ClampedFloatParameter focalLength;
    ///         public ClampedIntParameter bladeCount;
    ///         public ClampedFloatParameter bladeCurvature;
    ///         public ClampedFloatParameter bladeRotation;
    ///
    ///
    ///         public void SetVolumeComponentSettings(ref DepthOfField volumeComponent)
    ///         {
    ///             volumeComponent.active = active;
    ///             volumeComponent.mode = mode;
    ///             volumeComponent.gaussianStart = gaussianStart;
    ///             volumeComponent.gaussianEnd = gaussianEnd;
    ///             volumeComponent.gaussianMaxRadius = gaussianMaxRadius;
    ///             volumeComponent.highQualitySampling = highQualitySampling;
    ///             volumeComponent.focusDistance = focusDistance;
    ///             volumeComponent.aperture = aperture;
    ///             volumeComponent.focalLength = focalLength;
    ///             volumeComponent.bladeCount = bladeCount;
    ///             volumeComponent.bladeCurvature = bladeCurvature;
    ///             volumeComponent.bladeRotation = bladeRotation;
    ///         }
    ///
    ///         public void GetVolumeComponentSettings(ref DepthOfField volumeComponent)
    ///         {
    ///             active = volumeComponent.active;
    ///             mode = volumeComponent.mode;
    ///             gaussianStart = volumeComponent.gaussianStart;
    ///             gaussianEnd = volumeComponent.gaussianEnd;
    ///             gaussianMaxRadius = volumeComponent.gaussianMaxRadius;
    ///             highQualitySampling = volumeComponent.highQualitySampling;
    ///             focusDistance = volumeComponent.focusDistance;
    ///             aperture = volumeComponent.aperture;
    ///             focalLength = volumeComponent.focalLength;
    ///             bladeCount = volumeComponent.bladeCount;
    ///             bladeCurvature = volumeComponent.bladeCurvature;
    ///             bladeRotation = volumeComponent.bladeRotation;
    ///         }
    ///     }
    ///
    ///     private void Start()
    ///     {
    ///         m_HasRetrievedVolumeComponent = GetVolumeComponent(in volumeProfile, ref m_VolumeComponent);
    ///         if (m_HasRetrievedVolumeComponent)
    ///             volumeSettings.GetVolumeComponentSettings(ref m_VolumeComponent);
    ///     }
    ///
    ///     private void Update()
    ///     {
    ///         if (!m_HasRetrievedVolumeComponent)
    ///             return;
    ///
    ///         volumeSettings.SetVolumeComponentSettings(ref m_VolumeComponent);
    ///     }
    ///
    ///     private static bool GetVolumeComponent(in VolumeProfile volumeProfile, ref DepthOfField volumeComponent)
    ///     {
    ///         if (volumeComponent != null)
    ///             return true;
    ///
    ///         if (volumeProfile == null)
    ///         {
    ///             Debug.LogError("ModifyVolumeComponent.GetVolumeComponent():\nvolumeProfile has not been assigned.");
    ///             return false;
    ///         }
    ///
    ///         volumeProfile.TryGet(out DepthOfField component);
    ///         if (component == null)
    ///         {
    ///             Debug.LogError($"ModifyVolumeComponent.GetVolumeComponent():\nMissing component in the \"{volumeProfile.name}\" VolumeProfile ");
    ///             return false;
    ///         }
    ///
    ///         volumeComponent = component;
    ///         return true;
    ///     }
    /// }
    /// </code>
    /// </example>
    /// <seealso cref="VolumeProfile"/>
    /// <seealso cref="VolumeComponent"/>
    /// <seealso cref="IPostProcessComponent"/>
    /// <seealso cref="VolumeParameter{T}"/>
    /// <seealso cref="DepthOfFieldModeParameter"/>
    /// <seealso cref="MinFloatParameter"/>
    /// <seealso cref="ClampedFloatParameter"/>
    /// <seealso cref="BoolParameter"/>
    /// <seealso cref="ClampedIntParameter"/>
    [Serializable, VolumeComponentMenu("Post-processing/Depth Of Field")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [URPHelpURL("post-processing-depth-of-field")]
    public sealed class DepthOfField : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Use this to select Focusing modes for the depth of field effect.
        /// </summary>
        [Tooltip("Use \"Gaussian\" for a faster but non physical depth of field; \"Bokeh\" for a more realistic but slower depth of field.")]
        public DepthOfFieldModeParameter mode = new DepthOfFieldModeParameter(DepthOfFieldMode.Off);

        /// <summary>
        /// The distance at which the blurring will start.
        /// </summary>
        [Tooltip("The distance at which the blurring will start.")]
        public MinFloatParameter gaussianStart = new MinFloatParameter(10f, 0f);

        /// <summary>
        /// The distance at which the blurring will reach its maximum radius.
        /// </summary>
        [Tooltip("The distance at which the blurring will reach its maximum radius.")]
        public MinFloatParameter gaussianEnd = new MinFloatParameter(30f, 0f);

        /// <summary>
        /// The maximum radius of the gaussian blur. Values above 1 may show under-sampling artifacts.
        /// </summary>
        [Tooltip("The maximum radius of the gaussian blur. Values above 1 may show under-sampling artifacts.")]
        public ClampedFloatParameter gaussianMaxRadius = new ClampedFloatParameter(1f, 0.5f, 1.5f);

        /// <summary>
        /// Use higher quality sampling to reduce flickering and improve the overall blur smoothness.
        /// </summary>
        [Tooltip("Use higher quality sampling to reduce flickering and improve the overall blur smoothness.")]
        public BoolParameter highQualitySampling = new BoolParameter(false);

        /// <summary>
        /// The distance to the point of focus.
        /// </summary>
        [Tooltip("The distance to the point of focus.")]
        public MinFloatParameter focusDistance = new MinFloatParameter(10f, 0.1f);

        /// <summary>
        /// The ratio of aperture (known as f-stop or f-number). The smaller the value is, the shallower the depth of field is.
        /// </summary>
        [Tooltip("The ratio of aperture (known as f-stop or f-number). The smaller the value is, the shallower the depth of field is.")]
        public ClampedFloatParameter aperture = new ClampedFloatParameter(5.6f, 1f, 32f);

        /// <summary>
        /// The distance between the lens and the film. The larger the value is, the shallower the depth of field is.
        /// </summary>
        [Tooltip("The distance between the lens and the film. The larger the value is, the shallower the depth of field is.")]
        public ClampedFloatParameter focalLength = new ClampedFloatParameter(50f, 1f, 300f);

        /// <summary>
        /// The number of aperture blades.
        /// </summary>
        [Tooltip("The number of aperture blades.")]
        public ClampedIntParameter bladeCount = new ClampedIntParameter(5, 3, 9);

        /// <summary>
        /// The curvature of aperture blades. The smaller the value is, the more visible aperture blades are. A value of 1 will make the bokeh perfectly circular.
        /// </summary>
        [Tooltip("The curvature of aperture blades. The smaller the value is, the more visible aperture blades are. A value of 1 will make the bokeh perfectly circular.")]
        public ClampedFloatParameter bladeCurvature = new ClampedFloatParameter(1f, 0f, 1f);

        /// <summary>
        /// The rotation of aperture blades in degrees.
        /// </summary>
        [Tooltip("The rotation of aperture blades in degrees.")]
        public ClampedFloatParameter bladeRotation = new ClampedFloatParameter(0f, -180f, 180f);

        /// <summary>
        /// Tells if the post process needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive()
        {
            if (mode.value == DepthOfFieldMode.Off || SystemInfo.graphicsShaderLevel < 35)
                return false;

            return mode.value != DepthOfFieldMode.Gaussian || SystemInfo.supportedRenderTargetCount > 1;
        }

        /// <summary>
        /// Tells if the post process can run the effect on-tile or if it needs a full pass.
        /// </summary>
        /// <returns><c>true</c> if it can run on-tile, <c>false</c> otherwise.</returns>
        [Obsolete("Unused #from(2023.1)", false)]
        public bool IsTileCompatible() => false;
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="DepthOfFieldMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class DepthOfFieldModeParameter : VolumeParameter<DepthOfFieldMode>
    {
        /// <summary>
        /// Creates a new <see cref="DepthOfFieldModeParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public DepthOfFieldModeParameter(DepthOfFieldMode value, bool overrideState = false) : base(value, overrideState) { }
    }
}
