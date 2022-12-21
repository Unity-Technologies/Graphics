using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>Light Layers.</summary>
    [Flags]
    public enum LightLayerEnum
    {
        /// <summary>The light will no affect any object.</summary>
        Nothing = 0,   // Custom name for "Nothing" option
        /// <summary>Light Layer 0.</summary>
        LightLayerDefault = 1 << 0,
        /// <summary>Light Layer 1.</summary>
        LightLayer1 = 1 << 1,
        /// <summary>Light Layer 2.</summary>
        LightLayer2 = 1 << 2,
        /// <summary>Light Layer 3.</summary>
        LightLayer3 = 1 << 3,
        /// <summary>Light Layer 4.</summary>
        LightLayer4 = 1 << 4,
        /// <summary>Light Layer 5.</summary>
        LightLayer5 = 1 << 5,
        /// <summary>Light Layer 6.</summary>
        LightLayer6 = 1 << 6,
        /// <summary>Light Layer 7.</summary>
        LightLayer7 = 1 << 7,
        /// <summary>Everything.</summary>
        Everything = 0xFF, // Custom name for "Everything" option
    }

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
    /// Class containing various additional light data used by URP.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Light))]
    [URPHelpURL("universal-additional-light-data")]
    public class UniversalAdditionalLightData : MonoBehaviour, ISerializationCallbackReceiver, IAdditionalData
    {
        // Version 0 means serialized data before the version field.
        [SerializeField] int m_Version = 3;
        internal int version
        {
            get => m_Version;
        }

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

        /// <summary>
        /// The minimum shadow resolution for additional lights.
        /// </summary>
        public static readonly int AdditionalLightsShadowMinimumResolution = 128;

        [Tooltip("Controls if light shadow resolution uses pipeline settings.")]
        [SerializeField] int m_AdditionalLightsShadowResolutionTier = AdditionalLightsShadowDefaultResolutionTier;

        /// <summary>
        /// Returns the selected shadow resolution tier.
        /// </summary>
        public int additionalLightsShadowResolutionTier
        {
            get { return m_AdditionalLightsShadowResolutionTier; }
        }

        // The layer(s) this light belongs too.
        [Obsolete("This is obsolete, please use m_RenderingLayerMask instead.", false)]
        [SerializeField] LightLayerEnum m_LightLayerMask = LightLayerEnum.LightLayerDefault;

        /// <summary>
        /// The layer(s) this light belongs to.
        /// </summary>
        [Obsolete("This is obsolete, please use renderingLayerMask instead.", false)]
        public LightLayerEnum lightLayerMask
        {
            get { return m_LightLayerMask; }
            set { m_LightLayerMask = value; }
        }

        [SerializeField] uint m_RenderingLayers = 1;

        /// <summary>
        /// Specifies which rendering layers this light will affect.
        /// </summary>
        public uint renderingLayers
        {
            get { return m_RenderingLayers; }
            set { m_RenderingLayers = value; }
        }

        [SerializeField] bool m_CustomShadowLayers = false;

        /// <summary>
        /// Indicates whether shadows need custom layers.
        /// If not, then it uses the same settings as lightLayerMask.
        /// </summary>
        public bool customShadowLayers
        {
            get { return m_CustomShadowLayers; }
            set { m_CustomShadowLayers = value; }
        }

        // The layer(s) used for shadow casting.
        [SerializeField] LightLayerEnum m_ShadowLayerMask = LightLayerEnum.LightLayerDefault;

        /// <summary>
        /// The layer(s) for shadow.
        /// </summary>
        [Obsolete("This is obsolete, please use shadowRenderingLayerMask instead.", false)]
        public LightLayerEnum shadowLayerMask
        {
            get { return m_ShadowLayerMask; }
            set { m_ShadowLayerMask = value; }
        }

        [SerializeField] uint m_ShadowRenderingLayers = 1;
        /// <summary>
        /// Specifies which rendering layers this light shadows will affect.
        /// </summary>
        public uint shadowRenderingLayers
        {
            get { return m_ShadowRenderingLayers; }
            set { m_ShadowRenderingLayers = value; }
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
        [SerializeField] private SoftShadowQuality m_SoftShadowQuality = SoftShadowQuality.UsePipelineSettings;

        /// <inheritdoc/>
        public void OnBeforeSerialize()
        {
        }

        /// <inheritdoc/>
        public void OnAfterDeserialize()
        {
            if (m_Version < 2)
            {
#pragma warning disable 618 // Obsolete warning
                m_RenderingLayers = (uint)m_LightLayerMask;
                m_ShadowRenderingLayers = (uint)m_ShadowLayerMask;
#pragma warning restore 618 // Obsolete warning
                m_Version = 2;
            }

            if (m_Version < 3)
            {
                // SoftShadowQuality.UsePipelineSettings added at index 0. Bump existing serialized values by 1. e.g. Low(0) -> Low(1).
                m_SoftShadowQuality = (SoftShadowQuality)(Math.Clamp((int)m_SoftShadowQuality + 1, 0, (int)SoftShadowQuality.High));
                m_Version = 3;
            }
        }
    }
}
