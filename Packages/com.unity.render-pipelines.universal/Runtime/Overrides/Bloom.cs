using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// This controls the size of the bloom texture.
    /// </summary>
    public enum BloomDownscaleMode
    {
        /// <summary>
        /// Use this to select half size as the starting resolution.
        /// </summary>
        Half,

        /// <summary>
        /// Use this to select quarter size as the starting resolution.
        /// </summary>
        Quarter,
    }

    /// <summary>
    /// This controls the filtering method of the bloom texture blur.
    /// </summary>
    public enum BloomFilterMode
    {
        /// <summary>
        /// Gaussian blur on downsample and bilinear/bicubic on upsample. Best quality.
        /// </summary>
        [Tooltip("Best quality.")]
        Gaussian,

        /// <summary>
        /// Dual blur combines Kawase like blur with downsampling and upsampling. It is faster than Gaussian but has worse quality. Dual can be faster than Kawase at high resolutions.
        /// </summary>
        [Tooltip("Balanced quality and speed.")]
        Dual,

        /// <summary>
        /// Kawase blur uses a fixed size texture. It can be faster at lower resolutions while saving small amount of memory.
        /// </summary>
        [Tooltip("Lowest quality. Fastest at low resolutions. Saves memory.")]
        Kawase
    }

    /// <summary>
    /// A volume component that holds settings for the Bloom effect.
    /// </summary>
    /// <remarks>
    /// You can add <see cref="VolumeComponent"/> to a <see cref="VolumeProfile"/> in the Editor to apply a Bloom post-processing effect.
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
    ///     private Bloom m_VolumeComponent;
    ///
    ///     [Serializable]
    ///     private struct VolumeSettings
    ///     {
    ///         public bool active;
    ///         public MinFloatParameter threshold;
    ///         public MinFloatParameter intensity;
    ///         public ClampedFloatParameter scatter;
    ///         public MinFloatParameter clamp;
    ///         public ColorParameter tint;
    ///         public BoolParameter highQualityFiltering;
    ///         public DownscaleParameter downscale;
    ///         public ClampedIntParameter maxIterations;
    ///         public TextureParameter dirtTexture;
    ///         public MinFloatParameter dirtIntensity;
    ///
    ///         public void SetVolumeComponentSettings(ref Bloom volumeComponent)
    ///         {
    ///             volumeComponent.active = active;
    ///             volumeComponent.threshold = threshold;
    ///             volumeComponent.intensity = intensity;
    ///             volumeComponent.scatter = scatter;
    ///             volumeComponent.clamp = clamp;
    ///             volumeComponent.tint = tint;
    ///             volumeComponent.highQualityFiltering = highQualityFiltering;
    ///             volumeComponent.downscale = downscale;
    ///             volumeComponent.maxIterations = maxIterations;
    ///             volumeComponent.dirtTexture = dirtTexture;
    ///             volumeComponent.dirtIntensity = dirtIntensity;
    ///         }
    ///
    ///         public void GetVolumeComponentSettings(ref Bloom volumeComponent)
    ///         {
    ///             active = volumeComponent.active;
    ///             threshold = volumeComponent.threshold;
    ///             intensity = volumeComponent.intensity;
    ///             scatter = volumeComponent.scatter;
    ///             clamp = volumeComponent.clamp;
    ///             tint = volumeComponent.tint;
    ///             highQualityFiltering = volumeComponent.highQualityFiltering;
    ///             downscale = volumeComponent.downscale;
    ///             maxIterations = volumeComponent.maxIterations;
    ///             dirtTexture = volumeComponent.dirtTexture;
    ///             dirtIntensity = volumeComponent.dirtIntensity;
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
    ///     private static bool GetVolumeComponent(in VolumeProfile volumeProfile, ref Bloom volumeComponent)
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
    ///         volumeProfile.TryGet(out Bloom component);
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
    /// <seealso cref="MinFloatParameter"/>
    /// <seealso cref="ClampedFloatParameter"/>
    /// <seealso cref="ColorParameter"/>
    /// <seealso cref="BoolParameter"/>
    /// <seealso cref="DownscaleParameter"/>
    /// <seealso cref="BloomFilterModeParameter"/>
    /// <seealso cref="ClampedIntParameter"/>
    /// <seealso cref="TextureParameter"/>
    [Serializable, VolumeComponentMenu("Post-processing/Bloom")]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [URPHelpURL("post-processing-bloom")]
    public sealed partial class Bloom : VolumeComponent, IPostProcessComponent
    {
        /// <summary>
        /// Set the level of brightness to filter out pixels under this level.
        /// This value is expressed in gamma-space.
        /// A value above 0 will disregard energy conservation rules.
        /// </summary>
        [Header("Bloom")]
        [Tooltip("Filters out pixels under this level of brightness. Value is in gamma-space.")]
        public MinFloatParameter threshold = new MinFloatParameter(0.9f, 0f);

        /// <summary>
        /// Controls the strength of the bloom filter.
        /// </summary>
        [Tooltip("Strength of the bloom filter.")]
        public MinFloatParameter intensity = new MinFloatParameter(0f, 0f);

        /// <summary>
        /// Controls the extent of the veiling effect.
        /// </summary>
        [Tooltip("Set the radius of the bloom effect.")]
        public ClampedFloatParameter scatter = new ClampedFloatParameter(0.7f, 0f, 1f);

        /// <summary>
        /// Set the maximum intensity that Unity uses to calculate Bloom.
        /// If pixels in your Scene are more intense than this, URP renders them at their current intensity, but uses this intensity value for the purposes of Bloom calculations.
        /// </summary>
        [Tooltip("Set the maximum intensity that Unity uses to calculate Bloom. If pixels in your Scene are more intense than this, URP renders them at their current intensity, but uses this intensity value for the purposes of Bloom calculations.")]
        public MinFloatParameter clamp = new MinFloatParameter(65472f, 0f);

        /// <summary>
        /// Specifies the tint of the bloom filter.
        /// </summary>
        [Tooltip("Use the color picker to select a color for the Bloom effect to tint to.")]
        public ColorParameter tint = new ColorParameter(Color.white, false, false, true);

        /// <summary>
        /// Controls whether to use bicubic sampling instead of bilinear sampling for the upsampling passes.
        /// This is slightly more expensive but helps getting smoother visuals.
        /// </summary>
        [Tooltip("Use bicubic sampling instead of bilinear sampling for the upsampling passes. This is slightly more expensive but helps getting smoother visuals.")]
        public BoolParameter highQualityFiltering = new BoolParameter(false);

        /// <summary>
        /// Controls the filtering method used for the bloom downsampling and upsampling passes.
        /// </summary>
        [Tooltip("Set the filtering algorithm for the Bloom effect.")]
        public BloomFilterModeParameter filter = new BloomFilterModeParameter(BloomFilterMode.Gaussian);

        /// <summary>
        /// Controls the starting resolution that this effect begins processing.
        /// </summary>
        [Tooltip("The starting resolution that this effect begins processing."), AdditionalProperty]
        public DownscaleParameter downscale = new DownscaleParameter(BloomDownscaleMode.Half);

        /// <summary>
        /// Controls the maximum number of iterations in the effect processing sequence.
        /// </summary>
        [Tooltip("The maximum number of iterations in the effect processing sequence."), AdditionalProperty]
        public ClampedIntParameter maxIterations = new ClampedIntParameter(6, 2, 8);

        /// <summary>
        /// Specifies a Texture to add smudges or dust to the bloom effect.
        /// </summary>
        [Header("Lens Dirt")]
        [Tooltip("Dirtiness texture to add smudges or dust to the bloom effect.")]
        public TextureParameter dirtTexture = new TextureParameter(null);

        /// <summary>
        /// Controls the strength of the lens dirt.
        /// </summary>
        [Tooltip("Amount of dirtiness.")]
        public MinFloatParameter dirtIntensity = new MinFloatParameter(0f, 0f);

        /// <summary>
        /// Tells if the post process needs to be rendered or not.
        /// </summary>
        /// <returns><c>true</c> if the effect should be rendered, <c>false</c> otherwise.</returns>
        public bool IsActive() => intensity.value > 0f;

        /// <summary>
        /// Tells if the post process can run the effect on-tile or if it needs a full pass.
        /// </summary>
        /// <returns><c>true</c> if it can run on-tile, <c>false</c> otherwise.</returns>
        [Obsolete("Unused. #from(2023.1)")]
        public bool IsTileCompatible() => false;
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="BloomDownscaleMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class DownscaleParameter : VolumeParameter<BloomDownscaleMode>
    {
        /// <summary>
        /// Creates a new <see cref="DownscaleParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public DownscaleParameter(BloomDownscaleMode value, bool overrideState = false) : base(value, overrideState) { }
    }

    /// <summary>
    /// A <see cref="VolumeParameter"/> that holds a <see cref="BloomFilterMode"/> value.
    /// </summary>
    [Serializable]
    public sealed class BloomFilterModeParameter : VolumeParameter<BloomFilterMode>
    {
        /// <summary>
        /// Creates a new <see cref="BloomFilterModeParameter"/> instance.
        /// </summary>
        /// <param name="value">The initial value to store in the parameter.</param>
        /// <param name="overrideState">The initial override state for the parameter.</param>
        public BloomFilterModeParameter(BloomFilterMode value, bool overrideState = false) : base(value, overrideState) { }
    }
}
