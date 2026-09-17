using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Options for how the shadow-map depth offset is applied.
    /// </summary>
    public enum ShadowDepthBiasMode
    {
        /// <summary>
        /// The legacy behavior: the depth bias value pushes shadow-caster geometry away from the light by a fixed, shadow-map-texel-scaled amount, and a fixed slope-scale depth bias of 2.5 is applied on top.
        /// </summary>
        [InspectorName("Depth Bias (Legacy)")]
        Legacy,

        /// <summary>
        /// The depth bias value drives the rasterizer's slope-scaled depth bias, interpreted as a normalized [0..1] multiplier: surfaces at a grazing angle to the light receive a proportionally larger depth offset.
        /// </summary>
        [InspectorName("Slope-Scale Depth Bias")]
        SlopeScale,
    }

    /// <summary>
    /// Options for where the shadow normal bias is applied.
    /// </summary>
    public enum ShadowNormalBiasMode
    {
        /// <summary>
        /// The normal bias value shrinks shadow-caster geometry along its normals while rendering the shadow map. Point lights ignore the normal bias in this mode.
        /// </summary>
        [InspectorName("Shadow Caster")]
        Caster,

        /// <summary>
        /// The normal bias value offsets the shadow-map sampling position along the surface normal of the shadow receiver. Shaders must pass the surface normal to the shadow sampling functions for the bias to apply.
        /// </summary>
        [InspectorName("Shadow Receiver")]
        Receiver,
    }

    /// <summary>
    /// Project-wide shadow bias settings for URP.
    /// </summary>
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "Shadows", Order = 22)]
    public class URPShadowBiasSettings : IRenderPipelineGraphicsSettings
    {
        [SerializeField, HideInInspector] private int m_Version = 1;

        int IRenderPipelineGraphicsSettings.version => m_Version;
        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        [SerializeField, Tooltip("Selects how the shadow-map depth offset is applied. Slope-Scale Depth Bias drives the rasterizer's slope-scaled depth bias with the bias value as a normalized [0..1] multiplier. Depth Bias (Legacy) pushes shadow casters away from the light by a fixed texel-scaled amount and also applies a fixed slope-scale bias of 2.5. Bias values need re-tuning when switching modes.")]
        private ShadowDepthBiasMode m_DepthBiasMode = ShadowDepthBiasMode.SlopeScale;

        [SerializeField, Tooltip("Selects where the shadow normal bias is applied. Shadow Receiver offsets the shadow-map sampling position along the receiver's surface normal, which works for every light type but requires shaders to pass the normal to the shadow sampling functions. Shadow Caster shrinks shadow-caster geometry along its normals while rendering the shadow map, matching the legacy behavior.")]
        private ShadowNormalBiasMode m_NormalBiasMode = ShadowNormalBiasMode.Receiver;

        /// <summary>
        /// Selects how the shadow-map depth offset is applied. See <see cref="ShadowDepthBiasMode"/>.
        /// </summary>
        public ShadowDepthBiasMode depthBiasMode
        {
            get => m_DepthBiasMode;
            set
            {
#if UNITY_EDITOR
                this.SetValueAndNotify(ref m_DepthBiasMode, value, nameof(m_DepthBiasMode));
#else
                m_DepthBiasMode = value;
#endif
            }
        }

        /// <summary>
        /// Selects where the shadow normal bias is applied. See <see cref="ShadowNormalBiasMode"/>.
        /// </summary>
        public ShadowNormalBiasMode normalBiasMode
        {
            get => m_NormalBiasMode;
            set
            {
#if UNITY_EDITOR
                this.SetValueAndNotify(ref m_NormalBiasMode, value, nameof(m_NormalBiasMode));
#else
                m_NormalBiasMode = value;
#endif
            }
        }
    }
}
