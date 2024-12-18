using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Presets for the <see cref="FilmGrain"/> effect.
    /// </summary>
    public enum FilmGrainLookup
    {
        /// <summary>
        /// Thin grain preset.
        /// </summary>
        Thin1,

        /// <summary>
        /// Thin grain preset.
        /// </summary>
        Thin2,

        /// <summary>
        /// Medium grain preset.
        /// </summary>
        Medium1,

        /// <summary>
        /// Medium grain preset.
        /// </summary>
        Medium2,

        /// <summary>
        /// Medium grain preset.
        /// </summary>
        Medium3,

        /// <summary>
        /// Medium grain preset.
        /// </summary>
        Medium4,

        /// <summary>
        /// Medium grain preset.
        /// </summary>
        Medium5,

        /// <summary>
        /// Medium grain preset.
        /// </summary>
        Medium6,

        /// <summary>
        /// Large grain preset.
        /// </summary>
        Large01,

        /// <summary>
        /// Large grain preset.
        /// </summary>
        Large02,

        /// <summary>
        /// Custom grain preset.
        /// </summary>
        /// <seealso cref="FilmGrain.texture"/>
        Custom
    }

    /// <summary>
    /// A volume component that holds settings for the Film Grain effect.
    /// </summary>
    /// <remarks>
    /// You can add <see cref="VolumeComponent"/> to a <see cref="VolumeProfile"/> in the Editor to apply a Film Grain post-processing effect.
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
    ///     private FilmGrain m_VolumeComponent;
    ///
    ///     [Serializable]
    ///     private struct VolumeSettings
    ///     {
    ///         public bool active;
    ///         public FilmGrainLookupParameter type;
    ///         public ClampedFloatParameter intensity;
    ///         public ClampedFloatParameter response;
    ///         public NoInterpTextureParameter texture;
    ///
    ///         public void SetVolumeComponentSettings(ref FilmGrain volumeComponent)
    ///         {
    ///             volumeComponent.active = active;
    ///             volumeComponent.type = type;
    ///             volumeComponent.intensity = intensity;
    ///             volumeComponent.response = response;
    ///             volumeComponent.texture = texture;
    ///         }
    ///
    ///         public void GetVolumeComponentSettings(ref FilmGrain volumeComponent)
    ///         {
    ///             active = volumeComponent.active;
    ///             type = volumeComponent.type;
    ///             intensity = volumeComponent.intensity;
    ///             response = volumeComponent.response;
    ///             texture = volumeComponent.texture;
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
    ///     private static bool GetVolumeComponent(in VolumeProfile volumeProfile, ref FilmGrain volumeComponent)
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
    ///         volumeProfile.TryGet(out FilmGrain component);
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
    /// <seealso cref="FilmGrainLookupParameter"/>
    /// <seealso cref="ClampedFloatParameter"/>
    /// <seealso cref="NoInterpTextureParameter"/>
    [Serializable, VolumeComponentMenu("Post-processing/Film Grain")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [URPHelpURL("Post-Processing-Film-Grain")]
    public sealed class FilmGrain : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// The type of grain to use. You can select a preset or provide your own texture by selecting Custom.
        /// </summary>
        [Tooltip("The type of grain to use. You can select a preset or provide your own texture by selecting Custom.")]
        public FilmGrainLookupParameter type = new FilmGrainLookupParameter(FilmGrainLookup.Thin1);

        /// <summary>
        /// Use this to set the strength of the Film Grain effect.
        /// </summary>
        [Tooltip("Use the slider to set the strength of the Film Grain effect.")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0f, 0f, 1f);

        /// <summary>
        /// Controls the noisiness response curve based on scene luminance. Higher values mean less noise in light areas.
        /// </summary>
        [Tooltip("Controls the noisiness response curve based on scene luminance. Higher values mean less noise in light areas.")]
        public ClampedFloatParameter response = new ClampedFloatParameter(0.8f, 0f, 1f);

        /// <summary>
        /// A tileable texture to use for the grain. The neutral value is 0.5 where no grain is applied
        /// </summary>
        [Tooltip("A tileable texture to use for the grain. The neutral value is 0.5 where no grain is applied.")]
        public NoInterpTextureParameter texture = new NoInterpTextureParameter(null);

        /// <summary>
        /// Tells if the post process needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive() => intensity.value > 0f && (type.value != FilmGrainLookup.Custom || texture.value != null);

        /// <summary>
        /// Tells if the post process can run the effect on-tile or if it needs a full pass.
        /// </summary>
        /// <returns><c>true</c> if it can run on-tile, <c>false</c> otherwise.</returns>
        [Obsolete("Unused #from(2023.1)", false)]
        public bool IsTileCompatible() => true;
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="FilmGrainLookup"/> value.
    /// </summary>
    [Serializable]
    public sealed class FilmGrainLookupParameter : VolumeParameter<FilmGrainLookup>
    {
        /// <summary>
        /// Creates a new <see cref="FilmGrainLookupParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public FilmGrainLookupParameter(FilmGrainLookup value, bool overrideState = false) : base(value, overrideState) { }
    }
}
