using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Contains extension methods for Light class.
    /// </summary>
    public static class LightExtensions
    {
        /// <summary>
        /// Universal Render Pipeline exposes additional light data in a separate component.
        /// This method returns the additional data component for the given light or create one if it doesn't exist yet.
        /// </summary>
        /// <param name="light"></param>
        /// <returns>The <c>UniversalAdditionalLightData</c> for this light.</returns>
        /// <see cref="UniversalAdditionalLightData"/>
        public static UniversalAdditionalLightData GetUniversalAdditionalLightData(this Light light)
        {
            var gameObject = light.gameObject;
            bool componentExists = gameObject.TryGetComponent<UniversalAdditionalLightData>(out var lightData);
            if (!componentExists)
                lightData = gameObject.AddComponent<UniversalAdditionalLightData>();

            return lightData;
        }
    }

    /// <summary>
    /// The PCSS sample count preset of a light. Higher presets reduce noise at a GPU cost.
    /// </summary>
    internal enum PCSSSampleQuality
    {
        Low,
        Medium,
        High,
        Custom,
    }

    /// <summary>
    /// Class containing various additional light data used by URP.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    [URPHelpURL("urp/universal-additional-light-data")]
    public partial class UniversalAdditionalLightData : MonoBehaviour, ISerializationCallbackReceiver, IAdditionalData
    {
        [Tooltip("Controls if light Shadow Bias parameters use pipeline settings.")]
        [SerializeField] bool m_UsePipelineSettings = true;

        /// <summary>
        /// Controls if light Shadow Bias parameters use pipeline settings or not.
        /// </summary>
        public bool usePipelineSettings
        {
            get { return m_UsePipelineSettings; }
            set { m_UsePipelineSettings = value; }
        }

        /// <summary>
        /// Value used to indicate custom shadow resolution tier for additional lights.
        /// </summary>
        public static readonly int AdditionalLightsShadowResolutionTierCustom = -1;

        /// <summary>
        /// Value used to indicate low shadow resolution tier for additional lights.
        /// </summary>
        public static readonly int AdditionalLightsShadowResolutionTierLow = 0;

        /// <summary>
        /// Value used to indicate medium shadow resolution tier for additional lights.
        /// </summary>
        public static readonly int AdditionalLightsShadowResolutionTierMedium = 1;

        /// <summary>
        /// Value used to indicate high shadow resolution tier for additional lights.
        /// </summary>
        public static readonly int AdditionalLightsShadowResolutionTierHigh = 2;

        /// <summary>
        /// The default shadow resolution tier for additional lights.
        /// </summary>
        public static readonly int AdditionalLightsShadowDefaultResolutionTier = AdditionalLightsShadowResolutionTierHigh;

        /// <summary>
        /// The default custom shadow resolution for additional lights.
        /// </summary>
        public static readonly int AdditionalLightsShadowDefaultCustomResolution = 128;

        [NonSerialized] private Light m_Light;

        /// <summary>
        /// Returns the cached light component associated with the game object that owns this light data.
        /// </summary>
        internal Light light
        {
            get
            {
                if (!m_Light)
                    TryGetComponent(out m_Light);
                return m_Light;
            }
        }

        /// <summary>
        /// The minimum shadow resolution for additional lights.
        /// </summary>
        public static readonly int AdditionalLightsShadowMinimumResolution = 128;

        [Tooltip("Controls if light shadow resolution uses pipeline settings.")]
        [SerializeField] int m_AdditionalLightsShadowResolutionTier = AdditionalLightsShadowDefaultResolutionTier;

        /// <summary>
        /// Gets or sets the selected shadow resolution tier.
        /// Can only be modified during Play mode.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown when attempting to set the value outside of play mode.</exception>
        public int additionalLightsShadowResolutionTier
        {
            get { return m_AdditionalLightsShadowResolutionTier; }
            set
            {
                if (!Application.isPlaying)
                    throw new InvalidOperationException("Cannot modify additionalLightsShadowResolutionTier outside of play mode.");
                m_AdditionalLightsShadowResolutionTier = value;
            }
        }

        [SerializeField] bool m_CustomShadowLayers = false;

        /// <summary>
        /// Indicates whether shadows need custom layers.
        /// If not, then it uses the same settings as lightLayerMask.
        /// </summary>
        public bool customShadowLayers
        {
            get
            {
                return m_CustomShadowLayers;
            }
            set
            {
                if (m_CustomShadowLayers != value)
                {
                    m_CustomShadowLayers = value;
                    SyncLightAndShadowLayers();
                }
            }
        }

        /// <summary>
        /// Controls the size of the cookie mask currently assigned to the light.
        /// </summary>
        [Tooltip("Controls the size of the cookie mask currently assigned to the light.")]
        public Vector2 lightCookieSize
        {
            get => m_LightCookieSize;
            set => m_LightCookieSize = value;
        }
        [SerializeField] Vector2 m_LightCookieSize = Vector2.one;

        /// <summary>
        /// Controls the offset of the cookie mask currently assigned to the light.
        /// </summary>
        [Tooltip("Controls the offset of the cookie mask currently assigned to the light.")]
        public Vector2 lightCookieOffset
        {
            get => m_LightCookieOffset;
            set => m_LightCookieOffset = value;
        }
        [SerializeField] Vector2 m_LightCookieOffset = Vector2.zero;

        /// <summary>
        /// Light soft shadow filtering quality.
        /// </summary>
        [Tooltip("Controls the filtering quality of soft shadows. Higher quality has lower performance.")]
        public SoftShadowQuality softShadowQuality
        {
            get => m_SoftShadowQuality;
            set => m_SoftShadowQuality = value;
        }
        [SerializeField] SoftShadowQuality m_SoftShadowQuality = SoftShadowQuality.UsePipelineSettings;

        // PCSS defaults, also used for a light without this component
        internal const float k_DefaultSoftShadowAngularDiameter = 0.5f;
        internal const float k_DefaultSoftShadowScale = 1.0f;
        internal const int k_DefaultSoftShadowBlockerSampleCount = 24;
        internal const int k_DefaultSoftShadowFilterSampleCount = 16;
        internal const float k_DefaultSoftShadowMaxPenumbraSize = 0.56f;
        internal const float k_DefaultSoftShadowMaxSamplingDistance = 0.5f;
        internal const float k_DefaultSoftShadowMinFilterSizeTexels = 1.5f;
        internal const float k_DefaultSoftShadowMinFilterMaxAngularDiameter = 10.0f;
        internal const float k_DefaultSoftShadowBlockerSearchAngularDiameter = 12.0f;
        internal const float k_DefaultSoftShadowBlockerSamplingClumpExponent = 2.0f;
        internal const float k_DefaultSoftShadowMinFilterSize = 0.1f;

        // PCSS sample counts of the Low and High presets. Medium uses the defaults above.
        internal const int k_LowSoftShadowBlockerSampleCount = 12;
        internal const int k_LowSoftShadowFilterSampleCount = 8;
        internal const int k_HighSoftShadowBlockerSampleCount = 32;
        internal const int k_HighSoftShadowFilterSampleCount = 24;

        /// <summary>
        /// The angular diameter in degrees of the main directional light, which sets how wide its PCSS penumbra grows.
        /// Spot and point lights use <see cref="Light.shapeRadius"/> instead.
        /// </summary>
        [Tooltip("Controls the angular diameter of the light in degrees, which sets how wide the PCSS penumbra grows with distance. The sun is roughly 0.5 degrees.")]
        internal float softShadowAngularDiameter
        {
            get => m_SoftShadowAngularDiameter;
            set => m_SoftShadowAngularDiameter = Mathf.Max(value, 0.0f);
        }
        [SerializeField, Min(0f)] float m_SoftShadowAngularDiameter = k_DefaultSoftShadowAngularDiameter;

        /// <summary>
        /// The multiplier applied to the PCSS penumbra size of this light, on top of its angular diameter or shape radius.
        /// </summary>
        [Tooltip("Controls the multiplier applied to the PCSS penumbra size, so shadows can be softened without changing the size of the light.")]
        internal float softShadowScale
        {
            get => m_SoftShadowScale;
            set => m_SoftShadowScale = Mathf.Max(value, 0.0f);
        }
        [SerializeField, Min(0f)] float m_SoftShadowScale = k_DefaultSoftShadowScale;

        /// <summary>
        /// The PCSS sample count preset of this light.
        /// The presets use the default tuning values, and <see cref="PCSSSampleQuality.Custom"/> uses the sample counts and tuning values set on this light.
        /// </summary>
        [Tooltip("Controls how many shadow map samples PCSS takes per pixel. Higher quality reduces noise but has lower performance. Lower settings rely on temporal anti-aliasing to hide noise. Select Custom to set the sample counts and tuning values yourself.")]
        internal PCSSSampleQuality softShadowPCSSSampleQuality
        {
            get => m_SoftShadowPCSSSampleQuality;
            set => m_SoftShadowPCSSSampleQuality = value;
        }
        [SerializeField] PCSSSampleQuality m_SoftShadowPCSSSampleQuality = PCSSSampleQuality.Medium;

        /// <summary>
        /// The number of samples the PCSS blocker search takes, clamped to 1 to 64.
        /// </summary>
        [Tooltip("Controls the number of samples the PCSS blocker search takes. Higher values reduce noise but have lower performance.")]
        internal int softShadowBlockerSampleCount
        {
            get => m_SoftShadowBlockerSampleCount;
            set => m_SoftShadowBlockerSampleCount = Mathf.Clamp(value, 1, ShadowUtils.k_PCSSMaxSampleCount);
        }
        [SerializeField, Range(1, ShadowUtils.k_PCSSMaxSampleCount)] int m_SoftShadowBlockerSampleCount = k_DefaultSoftShadowBlockerSampleCount;

        /// <summary>
        /// The number of samples the PCSS filtering phase takes, clamped to 1 to 64.
        /// </summary>
        [Tooltip("Controls the number of samples the PCSS filtering phase takes. Higher values reduce noise but have lower performance.")]
        internal int softShadowFilterSampleCount
        {
            get => m_SoftShadowFilterSampleCount;
            set => m_SoftShadowFilterSampleCount = Mathf.Clamp(value, 1, ShadowUtils.k_PCSSMaxSampleCount);
        }
        [SerializeField, Range(1, ShadowUtils.k_PCSSMaxSampleCount)] int m_SoftShadowFilterSampleCount = k_DefaultSoftShadowFilterSampleCount;

        /// <summary>
        /// The world-space upper bound on the PCSS penumbra size of the main directional light.
        /// </summary>
        [Tooltip("Controls the world-space upper bound on the PCSS penumbra size. This acts as a safety clamp.")]
        internal float softShadowMaxPenumbraSize
        {
            get => m_SoftShadowMaxPenumbraSize;
            set => m_SoftShadowMaxPenumbraSize = Mathf.Max(value, 0.0f);
        }
        [SerializeField, Min(0f)] float m_SoftShadowMaxPenumbraSize = k_DefaultSoftShadowMaxPenumbraSize;

        /// <summary>
        /// The world-space distance, at most, that the PCSS filter of the main directional light moves the depth each sample compares against towards the light.
        /// The sample positions on the shadow map do not change.
        /// </summary>
        [Tooltip("Controls the world-space distance, at most, that PCSS moves the depth each sample compares against towards the light. This does not move the samples across the shadow map. Useful for reducing light bleeding.")]
        internal float softShadowMaxSamplingDistance
        {
            get => m_SoftShadowMaxSamplingDistance;
            set => m_SoftShadowMaxSamplingDistance = Mathf.Max(value, 0.0f);
        }
        [SerializeField, Min(0f)] float m_SoftShadowMaxSamplingDistance = k_DefaultSoftShadowMaxSamplingDistance;

        /// <summary>
        /// The smallest PCSS filter radius of the main directional light, in shadow map texels.
        /// </summary>
        [Tooltip("Controls the smallest PCSS filter radius in shadow map texels. Useful for hiding flickering at shadow contact points.")]
        internal float softShadowMinFilterSizeTexels
        {
            get => m_SoftShadowMinFilterSizeTexels;
            set => m_SoftShadowMinFilterSizeTexels = Mathf.Max(value, 0.0f);
        }
        [SerializeField, Min(0f)] float m_SoftShadowMinFilterSizeTexels = k_DefaultSoftShadowMinFilterSizeTexels;

        /// <summary>
        /// The angular diameter in degrees the minimum PCSS filter cone of the main directional light widens to.
        /// Values below <see cref="softShadowBlockerSearchAngularDiameter"/>, or below the scaled angular diameter of the light, are raised to the larger of the two.
        /// </summary>
        [Tooltip("Controls the angle in degrees the minimum PCSS filter cone widens to. Values below Blocker Search Angle, or below Angular Diameter multiplied by Softness Scale, are raised to the larger of the two.")]
        internal float softShadowMinFilterMaxAngularDiameter
        {
            get => m_SoftShadowMinFilterMaxAngularDiameter;
            set => m_SoftShadowMinFilterMaxAngularDiameter = Mathf.Max(value, 0.0f);
        }
        [SerializeField, Min(0f)] float m_SoftShadowMinFilterMaxAngularDiameter = k_DefaultSoftShadowMinFilterMaxAngularDiameter;

        /// <summary>
        /// The angular diameter in degrees of the PCSS blocker search cone of the main directional light.
        /// Values below the scaled angular diameter of the light are raised to it.
        /// </summary>
        [Tooltip("Controls the angle in degrees of the PCSS blocker search cone. Wider angles reduce light bleeding. Values below Angular Diameter multiplied by Softness Scale are raised to it.")]
        internal float softShadowBlockerSearchAngularDiameter
        {
            get => m_SoftShadowBlockerSearchAngularDiameter;
            set => m_SoftShadowBlockerSearchAngularDiameter = Mathf.Max(value, 0.0f);
        }
        [SerializeField, Min(0f)] float m_SoftShadowBlockerSearchAngularDiameter = k_DefaultSoftShadowBlockerSearchAngularDiameter;

        /// <summary>
        /// How strongly the PCSS blocker search samples of the main directional light clump towards the center of the search cone.
        /// </summary>
        [Tooltip("Controls how strongly the PCSS blocker search samples clump towards the center of the search cone.")]
        internal float softShadowBlockerSamplingClumpExponent
        {
            get => m_SoftShadowBlockerSamplingClumpExponent;
            set => m_SoftShadowBlockerSamplingClumpExponent = Mathf.Max(value, 0.0f);
        }
        [SerializeField, Min(0f)] float m_SoftShadowBlockerSamplingClumpExponent = k_DefaultSoftShadowBlockerSamplingClumpExponent;

        /// <summary>
        /// The smallest PCSS filter radius of a spot or point light.
        /// The range 0 to 1 maps to 0 to about 4 shadow map texels.
        /// </summary>
        [Tooltip("Controls the smallest PCSS filter radius. The range 0 to 1 maps to 0 to about 4 shadow map texels. Useful for hiding flickering at shadow contact points.")]
        internal float softShadowMinFilterSize
        {
            get => m_SoftShadowMinFilterSize;
            set => m_SoftShadowMinFilterSize = Mathf.Clamp01(value);
        }
        [SerializeField, Range(0f, 1f)] float m_SoftShadowMinFilterSize = k_DefaultSoftShadowMinFilterSize;

#if VOLUMETRIC_FOG
        /// <summary>
        /// When enabled, this light interacts with volumetric fog.
        /// </summary>
        public bool affectsVolumetricFog
        {
            get => m_AffectsVolumetricFog;
            set => m_AffectsVolumetricFog = value;
        }
        [SerializeField] bool m_AffectsVolumetricFog = true;

        /// <summary>
        /// Controls the intensity of the scattered volumetric lighting.
        /// </summary>
        public float volumetricMultiplier
        {
            get => m_VolumetricMultiplier;
            set => m_VolumetricMultiplier = Mathf.Clamp(value, 0.0f, 16.0f);
        }
        [Range(0.0f, 16.0f), SerializeField] float m_VolumetricMultiplier = 1.0f;

        /// <summary>
        /// Dims the volumetric shadows this light casts. Set to 0 to skip shadow sampling in volumetric fog.
        /// </summary>
        public float volumetricShadowDimmer
        {
            get => m_VolumetricShadowDimmer;
            set => m_VolumetricShadowDimmer = Mathf.Clamp01(value);
        }
        [Range(0.0f, 1.0f), SerializeField] float m_VolumetricShadowDimmer = 1.0f;
#endif

        [SerializeField] RenderingLayerMask m_RenderingLayersMask = RenderingLayerMask.defaultRenderingLayerMask;

        /// <summary>
        /// Specifies which rendering layers this light will affect.
        /// </summary>
        public RenderingLayerMask renderingLayers
        {
            get => m_RenderingLayersMask;
            set
            {
                if (m_RenderingLayersMask == value) return;
                m_RenderingLayersMask = value;
                SyncLightAndShadowLayers();
            }
        }
        
        [SerializeField] RenderingLayerMask m_ShadowRenderingLayersMask = RenderingLayerMask.defaultRenderingLayerMask;
        
        /// <summary>
        /// Specifies which rendering layers this light shadows will affect.
        /// </summary>
        public RenderingLayerMask shadowRenderingLayers
        {
            get => m_ShadowRenderingLayersMask;
            set
            {
                if (value == m_ShadowRenderingLayersMask) return;
                m_ShadowRenderingLayersMask = value;
                SyncLightAndShadowLayers();
            }
        }

        internal void SyncLightAndShadowLayers()
        {
            if (light)
                light.renderingLayerMask = m_CustomShadowLayers ? m_ShadowRenderingLayersMask : m_RenderingLayersMask;
        }

        enum Version
        {
            Initial = 0,
            RenderingLayers = 2,
            SoftShadowQuality = 3,
            RenderingLayersMask = 4,
            
            Count
        }
        
        [SerializeField] Version m_Version = Version.Count;

        // This piece of code is needed because some objects could have been created before existence of Version enum
        /// <summary>OnBeforeSerialize needed to handle migration before the versioning system was in place.</summary>
        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            if (m_Version == Version.Count) // serializing a newly created object
                m_Version = Version.Count - 1; // mark as up to date
        }

        /// <summary>OnAfterDeserialize needed to handle migration before the versioning system was in place.</summary>
        void ISerializationCallbackReceiver.OnAfterDeserialize()
        {
            if (m_Version == Version.Count) // deserializing and object without version
                m_Version = Version.Initial; // reset to run the migration
            
            if (m_Version < Version.RenderingLayers)
            {
#pragma warning disable 618 // Obsolete warning
                m_RenderingLayers = (uint)m_LightLayerMask;
                m_ShadowRenderingLayers = (uint)m_ShadowLayerMask;
#pragma warning restore 618 // Obsolete warning
                m_Version = Version.RenderingLayers;
            }

            if (m_Version < Version.SoftShadowQuality)
            {
                // SoftShadowQuality.UsePipelineSettings added at index 0. Bump existing serialized values by 1. e.g. Low(0) -> Low(1).
                m_SoftShadowQuality = (SoftShadowQuality)(Math.Clamp((int)m_SoftShadowQuality + 1, 0, (int)SoftShadowQuality.High));
                m_Version = Version.SoftShadowQuality;
            }
            
            if (m_Version <  Version.RenderingLayersMask)
            {
#pragma warning disable 618 // Obsolete warning
                m_RenderingLayersMask = m_RenderingLayers;
                m_ShadowRenderingLayersMask = m_ShadowRenderingLayers;
#pragma warning restore 618 // Obsolete warning
                m_Version = Version.RenderingLayersMask;
            }
        }
    }
    
    
}
