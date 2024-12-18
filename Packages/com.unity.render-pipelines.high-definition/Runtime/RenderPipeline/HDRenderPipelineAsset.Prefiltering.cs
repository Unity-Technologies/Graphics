#if UNITY_EDITOR
using ShaderKeywordFilter = UnityEditor.ShaderKeywordFilter;

namespace UnityEngine.Rendering.HighDefinition
{
    // This partial class is used for Shader Keyword Prefiltering
    // It's an editor only file and used when making builds to determine what keywords can
    // be removed early in the Shader Processing stage based on the settings in each HDRP Asset
    public partial class HDRenderPipelineAsset
    {
        // Use legacy lightmaps (GPU resident drawer)
        [ShaderKeywordFilter.SelectOrRemove(true, keywordNames: "USE_LEGACY_LIGHTMAPS")]
        [SerializeField] private bool m_PrefilterUseLegacyLightmaps = false;

        [ShaderKeywordFilter.RemoveIf(false,  keywordNames: "LIGHTMAP_BICUBIC_SAMPLING")]
        [ShaderKeywordFilter.SelectIf(true, keywordNames: "LIGHTMAP_BICUBIC_SAMPLING")]
        [SerializeField] private bool m_PrefilterUseLightmapBicubicSampling = false;

        internal struct ShaderPrefilteringData
        {
            public bool useLegacyLightmaps;
            public bool useBicubicLightmapSampling;
        }

        internal void UpdateShaderKeywordPrefiltering(ref ShaderPrefilteringData prefilteringData)
        {
            m_PrefilterUseLegacyLightmaps = prefilteringData.useLegacyLightmaps;
            m_PrefilterUseLightmapBicubicSampling = prefilteringData.useBicubicLightmapSampling;
        }
    }
}

#endif
