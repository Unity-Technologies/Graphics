#if UNITY_EDITOR
using System;
using ShaderKeywordFilter = UnityEditor.ShaderKeywordFilter;

namespace UnityEngine.Rendering.Universal
{
    // This partial class is used for Shader Keyword Prefiltering
    // It's an editor only file and used when making builds to determine what keywords can
    // be removed early in the Shader Processing stage based on the settings in each URP Asset
    public partial class UniversalRenderPipelineAsset
    {
        private enum PrefilteringMode
        {
            Remove,    // Removes the keyword
            Select,    // Keeps the keyword
            SelectOnly // Selects the keyword and removes others
        }

        // Defaults for renderer features that are not dependent on other settings.
        // These are the filter rules if no such renderer features are present.
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ShaderKeywordStrings.ScreenSpaceOcclusion)]

        // TODO: decal settings needs some rework before we can filter DBufferMRT/DecalNormalBlend.
        // Atm the setup depends on the technique but settings are present for both at the same time.
        //[ShaderKeywordFilter.RemoveIf(true, keywordNames: new string[] {ShaderKeywordStrings.DBufferMRT1, ShaderKeywordStrings.DBufferMRT2, ShaderKeywordStrings.DBufferMRT3})]
        //[ShaderKeywordFilter.RemoveIf(true, keywordNames: new string[] {ShaderKeywordStrings.DecalNormalBlendLow, ShaderKeywordStrings.DecalNormalBlendMedium, ShaderKeywordStrings.DecalNormalBlendHigh})]
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ShaderKeywordStrings.DecalLayers)]
        private const bool k_RendererFeatureDefaults = true;

        // Platform specific filtering overrides
        [ShaderKeywordFilter.ApplyRulesIfGraphicsAPI(GraphicsDeviceType.OpenGLES3, GraphicsDeviceType.OpenGLCore)]
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ShaderKeywordStrings.WriteRenderingLayers)]
        private const bool k_CommonGLDefaults = true;

        // Foveated Rendering
        #if ENABLE_VR && ENABLE_XR_MODULE
        [ShaderKeywordFilter.ApplyRulesIfNotGraphicsAPI(GraphicsDeviceType.PlayStation5NGGC)]
        #endif
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: ShaderKeywordStrings.FoveatedRenderingNonUniformRaster)]
        private const bool k_PrefilterFoveatedRenderingNonUniformRaster = true;

        // XR Specific keywords
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: new string[] { ShaderKeywordStrings.DisableTexture2DXArray, ShaderKeywordStrings.BlitSingleSlice, ShaderKeywordStrings.XROcclusionMeshCombined})]
        [SerializeField] private bool m_PrefilterXRKeywords = true;

        // Forward+
        [ShaderKeywordFilter.RemoveIf(PrefilteringMode.Remove,     keywordNames: ShaderKeywordStrings.ForwardPlus)]
        [ShaderKeywordFilter.SelectIf(PrefilteringMode.Select,     keywordNames: new string[] { "", ShaderKeywordStrings.ForwardPlus })]
        [ShaderKeywordFilter.SelectIf(PrefilteringMode.SelectOnly, keywordNames: ShaderKeywordStrings.ForwardPlus)]
        [SerializeField] private PrefilteringMode m_PrefilterForwardPlus = PrefilteringMode.Remove;

        // Deferred Rendering
        [ShaderKeywordFilter.RemoveIf(PrefilteringMode.Remove, keywordNames: new string[] {    ShaderKeywordStrings._DEFERRED_FIRST_LIGHT, ShaderKeywordStrings._DEFERRED_MAIN_LIGHT, ShaderKeywordStrings._DEFERRED_MIXED_LIGHTING, ShaderKeywordStrings._GBUFFER_NORMALS_OCT})]
        [SerializeField] private PrefilteringMode m_PrefilterDeferredRendering = PrefilteringMode.Remove;

        // Rendering Debugger
        [ShaderKeywordFilter.RemoveIf(true, keywordNames: new string[] { ShaderKeywordStrings.DEBUG_DISPLAY })]
        [SerializeField] private bool m_PrefilterDebugKeywords = true;

        // Filters out WriteRenderingLayers if nothing requires the feature
        // TODO: Implement a different filter triggers for different passes (i.e. per-pass filter attributes)
        //[ShaderKeywordFilter.RemoveIf(true, keywordNames: ShaderKeywordStrings.WriteRenderingLayers)]
        //[SerializeField] private bool m_PrefilterWriteRenderingLayers = true;

        internal void SetupShaderKeywordPrefiltering(bool isDevelopmentBuild, bool usesXR)
        {
            SetupGlobalPrefiltering(isDevelopmentBuild, usesXR);
            SetupRendererPrefiltering();
        }

        private void SetupGlobalPrefiltering(bool isDevelopmentBuild, bool usesXR)
        {
            if (isDevelopmentBuild)
            {
                UniversalRenderPipelineGlobalSettings globalSettings = UniversalRenderPipelineGlobalSettings.instance;
                m_PrefilterDebugKeywords = globalSettings == null || globalSettings.stripDebugVariants;
            }
            else
            {
                m_PrefilterDebugKeywords = true;
            }

            m_PrefilterXRKeywords = !usesXR;
        }

        private void SetupRendererPrefiltering()
        {
            // Gather the rendering modes from the Renderers inside the URP Asset
            bool hasForwardPlus = false;
            bool onlyForwardPlus = true;
            bool hasDeferred = false;
            bool onlyDeferred = true;
            bool usesRenderingLayers = false;
            for (int i = 0; i < m_RendererDataList.Length; i++)
            {
                UniversalRendererData universalRendererData = m_RendererDataList[i] as UniversalRendererData;
                if (universalRendererData == null)
                    continue;

                switch (universalRendererData.renderingMode)
                {
                    case RenderingMode.Forward:
                        onlyDeferred = false;
                        onlyForwardPlus = false;
                        break;
                    case RenderingMode.ForwardPlus:
                        onlyDeferred = false;
                        hasForwardPlus = true;
                        break;
                    case RenderingMode.Deferred:
                        hasDeferred = true;
                        onlyForwardPlus = false;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (usesRenderingLayers)
                    continue;

                // To stop prefiltering of Rendering Layers we only need:
                // 1) A renderer feature requiring the feature
                // 2) Deferred Renderer + with Rendering Layers feature enabled in the URP Asset
                if (useRenderingLayers && universalRendererData.renderingMode == RenderingMode.Deferred)
                    usesRenderingLayers = true;
                else
                    usesRenderingLayers = RenderingLayerUtils.RequireRenderingLayers(universalRendererData);
            }

            // Set up the filtering settings
            if (onlyForwardPlus)
            {
                m_PrefilterForwardPlus       = PrefilteringMode.SelectOnly;
                m_PrefilterDeferredRendering = PrefilteringMode.Remove;
            }
            else if (onlyDeferred)
            {
                m_PrefilterForwardPlus       = PrefilteringMode.Remove;
                m_PrefilterDeferredRendering = PrefilteringMode.SelectOnly;
            }
            else
            {
                m_PrefilterForwardPlus       = hasForwardPlus ? PrefilteringMode.Select : PrefilteringMode.Remove;
                m_PrefilterDeferredRendering = hasDeferred ? PrefilteringMode.Select : PrefilteringMode.Remove;
            }

            //m_PrefilterWriteRenderingLayers = !usesRenderingLayers;
        }
    }
}
#endif
