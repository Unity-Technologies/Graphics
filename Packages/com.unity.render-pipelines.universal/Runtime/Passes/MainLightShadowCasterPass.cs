using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using Unity.Scripting.LifecycleManagement;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Renders a shadow map for the main Light.
    /// </summary>
    public partial class MainLightShadowCasterPass : ScriptableRenderPass
    {
        // Internal
        internal RTHandle m_MainLightShadowmapTexture;

        // Private
        private GraphicsFormat m_ShadowmapDepthStencilFormat;
        private int m_RenderTargetWidth;
        private int m_RenderTargetHeight;
        private int m_ShadowCasterCascadesCount;
        private ShadowPassMode m_ShadowPassMode;
        private bool m_SetKeywordForEmptyShadowmap;
        private float m_CascadeBorder;
        private float m_MaxShadowDistanceSq;
        private RenderTextureDescriptor m_MainLightShadowDescriptor;
        private readonly Vector4[] m_CascadeSplitDistances;
        [NoAutoStaticsCleanup] private static readonly ProfilingSampler s_ProfilingSetupSampler = new("Setup Main Shadowmap");
        [NoAutoStaticsCleanup] private static readonly ProfilingSampler s_SetKeywordsSampler = new("Set Main Light Shadow Keywords");
        private readonly ShadowSliceData[] m_CascadeSlices;

        // Constants and Statics
        private const int k_EmptyShadowMapDimensions = 1;
        private const int k_MaxCascades = MainLightShadowMatrices.count - 1;
        private const string k_MainLightShadowMapTextureName = "_MainLightShadowmapTexture";
        private static readonly Vector4 k_DefaultEmptyShadowParams = new Vector4(0f, 0f, 1f, 0f);
        private static Vector4 s_EmptyShadowParams = k_DefaultEmptyShadowParams;

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void ResetStaticsOnLoad()
        {
            s_EmptyShadowParams = k_DefaultEmptyShadowParams;
        }
#endif

        private static readonly Vector4 s_EmptyShadowmapSize = new(k_EmptyShadowMapDimensions, 1f / k_EmptyShadowMapDimensions, k_EmptyShadowMapDimensions, k_EmptyShadowMapDimensions);

        // Fills the cascade slots a frame does not use, plus the trailing slot ComputeCascadeIndex returns for a pixel
        // beyond every cascade. Only depends on the graphics API, so it is built once rather than per camera.
        private static readonly Matrix4x4 s_NoOpShadowMatrix = CreateNoOpShadowMatrix();

        static Matrix4x4 CreateNoOpShadowMatrix()
        {
            Matrix4x4 matrix = Matrix4x4.zero;
            matrix.m22 = SystemInfo.usesReversedZBuffer ? 1.0f : 0.0f;
            return matrix;
        }

        // Classes
        private static class MainLightShadowConstantBuffer
        {
            public static readonly int _MainLightShadowmapID = Shader.PropertyToID(k_MainLightShadowMapTextureName);
            public static readonly int _ShadowParams = Shader.PropertyToID("_MainLightShadowParams");
        }

        private class PassData
        {
            internal ShadowPassMode mode;
            internal UniversalRenderingData renderingData;
            internal UniversalCameraData cameraData;
            internal UniversalLightData lightData;
            internal UniversalShadowData shadowData;
            internal MainLightShadowCasterPass pass;
            internal TextureHandle shadowmapTexture;
            internal readonly RendererListHandle[] shadowRendererListsHandle = new RendererListHandle[k_MaxCascades];
        }

        /// <summary>
        /// Creates a new <c>MainLightShadowCasterPass</c> instance.
        /// </summary>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <seealso cref="RenderPassEvent"/>
        public MainLightShadowCasterPass(RenderPassEvent evt)
        {
            profilingSampler = new ProfilingSampler("Draw Main Light Shadowmap");
            renderPassEvent = evt;

            m_CascadeSlices = new ShadowSliceData[k_MaxCascades];
            m_CascadeSplitDistances = new Vector4[k_MaxCascades];
        }

        /// <summary>
        /// Cleans up resources used by the pass.
        /// </summary>
        public void Dispose()
        {
            m_MainLightShadowmapTexture?.Release();
        }

        /// <summary>
        /// Sets up the pass.
        /// </summary>
        /// <param name="renderingData"></param>
        /// <returns>True if the pass will render real shadow geometry, otherwise false.</returns>
        /// <seealso cref="RenderingData"/>
        public bool Setup(ref RenderingData renderingData)
        {
            ContextContainer frameData = renderingData.frameData;
            UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            UniversalShadowData shadowData = frameData.Get<UniversalShadowData>();
            return Setup(universalRenderingData, cameraData, lightData, shadowData);
        }

        // Returns true if the pass will render shadow casters this frame. False means the shadow map is reused
        // from a previous pass (cached) or no real shadows are drawn (empty keyword-only pass).
        static bool ShouldRenderShadowGeometry(UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData, UniversalShadowData shadowData)
        {
            // Reusing a cached shadow map: no geometry is rendered, so no camera re-setup is needed afterwards.
            // NOTE: This check must be done first - shadow casters are not culled for cached passes, so the culling
            // infos read further down are empty for them.
            if (shadowData.useCachedShadowMap)
                return false;

            if (!shadowData.mainLightShadowsEnabled || !shadowData.supportsMainLightShadows)
                return false;

#if UNITY_EDITOR
            if (CoreUtils.IsSceneLightingDisabled(cameraData.camera))
                return false;
#endif

            int shadowLightIndex = lightData.mainLightIndex;
            if (shadowLightIndex == -1)
                return false;

            VisibleLight shadowLight = lightData.visibleLights[shadowLightIndex];
            if (shadowLight.light.shadows == LightShadows.None)
                return false;

            if (!renderingData.cullResults.GetShadowCasterBounds(shadowLightIndex, out Bounds _))
                return false;

            ref readonly URPLightShadowCullingInfos shadowCullingInfos = ref shadowData.visibleLightsShadowCullingInfos.UnsafeElementAt(shadowLightIndex);
            for (int cascadeIndex = 0; cascadeIndex < shadowData.mainLightShadowCascadesCount; ++cascadeIndex)
            {
                if (!shadowCullingInfos.IsSliceValid(cascadeIndex))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Sets up the pass to render the main light shadowmap, or an empty shadowmap when no real
        /// shadows are drawn this frame. RecordRenderGraph() is always safe to call afterwards.
        /// </summary>
        /// <param name="renderingData">Data containing rendering settings.</param>
        /// <param name="cameraData">Data containing camera settings.</param>
        /// <param name="lightData">Data containing light settings.</param>
        /// <param name="shadowData">Data containing shadow settings.</param>
        /// <returns>True if the pass will render real shadow geometry, otherwise false.</returns>
        /// <seealso cref="RenderingData"/>
        public bool Setup(UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData, UniversalShadowData shadowData)
        {
            return Setup(renderingData, cameraData, lightData, shadowData, false);
        }

        /// <summary>
        /// Sets up the pass to render the main light shadowmap, or an empty shadowmap when no real
        /// shadows are drawn this frame. RecordRenderGraph() is always safe to call afterwards.
        /// </summary>
        /// <param name="renderingData">Data containing rendering settings.</param>
        /// <param name="cameraData">Data containing camera settings.</param>
        /// <param name="lightData">Data containing light settings.</param>
        /// <param name="shadowData">Data containing shadow settings.</param>
        /// <param name="stencilBuffer">Whether to allocate a stencil buffer for the shadowmap textures.</param>
        /// <returns>True if the pass will render real shadow geometry, otherwise false.</returns>
        /// <seealso cref="RenderingData"/>
        public bool Setup(UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData, UniversalShadowData shadowData, bool stencilBuffer)
        {
            return Setup(renderingData, cameraData, lightData, shadowData, stencilBuffer, true);
        }

        // Internal variant of Setup that allows setting shadowsEnabledByCamera == false, which forces the keyword-only
        // empty path (shadows disabled at camera-level, e.g. cullingMask == 0).
        internal bool Setup(UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData, UniversalShadowData shadowData, bool stencilBuffer, bool shadowsEnabledByCamera)
        {
            using var profScope = new ProfilingScope(s_ProfilingSetupSampler);

            m_ShadowmapDepthStencilFormat = ShadowUtils.GetShadowmapDepthStencilFormat(stencilBuffer);

            bool isOffscreenDepthTexture = cameraData.camera.targetTexture != null && cameraData.camera.targetTexture.format == RenderTextureFormat.Depth;
            bool willRenderShadowGeometry = shadowsEnabledByCamera && !isOffscreenDepthTexture && ShouldRenderShadowGeometry(renderingData, cameraData, lightData, shadowData);

            if (willRenderShadowGeometry)
            {
                SetupForShadowmapRendering(cameraData, lightData, shadowData);
            }
            else if (shadowsEnabledByCamera && shadowData.useCachedShadowMap && !isOffscreenDepthTexture)
            {
                // The pass that rendered the atlas left behind all needed member state (matrices, cascades,
                // descriptor), so we only update the mode (note that using empty mode doesn't count as reuse).
                if (m_ShadowPassMode == ShadowPassMode.DrawGeometry)
                    m_ShadowPassMode = ShadowPassMode.ReuseCachedAtlas;
            }
            else
            {
                SetupForEmptyRendering(cameraData, GetMainLight(lightData), shadowData);
            }

            return willRenderShadowGeometry;
        }

        // Configures the pass to render real shadow geometry into the shadow atlas
        void SetupForShadowmapRendering(UniversalCameraData cameraData, UniversalLightData lightData, UniversalShadowData shadowData)
        {
            Clear();

            int shadowLightIndex = lightData.mainLightIndex;
            VisibleLight shadowLight = lightData.visibleLights[shadowLightIndex];
            if (shadowLight.lightType != LightType.Directional)
                Debug.LogWarning("Only directional lights are supported as main light.");

            m_ShadowCasterCascadesCount = shadowData.mainLightShadowCascadesCount;
            m_RenderTargetWidth = shadowData.mainLightRenderTargetWidth;
            m_RenderTargetHeight = shadowData.mainLightRenderTargetHeight;

            ref readonly URPLightShadowCullingInfos shadowCullingInfos = ref shadowData.visibleLightsShadowCullingInfos.UnsafeElementAt(shadowLightIndex);
            for (int cascadeIndex = 0; cascadeIndex < m_ShadowCasterCascadesCount; ++cascadeIndex)
            {
                ref readonly ShadowSliceData sliceData = ref shadowCullingInfos.slices.UnsafeElementAt(cascadeIndex);
                m_CascadeSplitDistances[cascadeIndex] = sliceData.splitData.cullingSphere;
                m_CascadeSlices[cascadeIndex] = sliceData;
            }

            UpdateTextureDescriptorIfNeeded();
            m_MaxShadowDistanceSq = cameraData.maxShadowDistance * cameraData.maxShadowDistance;
            m_CascadeBorder = shadowData.mainLightShadowCascadeBorder;
            m_ShadowPassMode = ShadowPassMode.DrawGeometry;
        }

        static bool MainLightHasMixedShadows(Light mainLight)
        {
            return mainLight != null
                && mainLight.shadows != LightShadows.None
                && mainLight.bakingOutput.isBaked
                && mainLight.bakingOutput.mixedLightingMode != MixedLightingMode.IndirectOnly
                && mainLight.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed;
        }

        // Setups the pass to bind an empty shadowmap and only set shadow keywords and params,
        // without rendering any geometry. Used whenever no real shadows are drawn this frame.
        void SetupForEmptyRendering(UniversalCameraData cameraData, Light mainLight, UniversalShadowData shadowData)
        {
#if UNITY_EDITOR
            // SceneView "Lighting off" mode must also disable baked/mixed ones, so it's treated as if no mainLight exists.
            if (CoreUtils.IsSceneLightingDisabled(cameraData.camera))
                mainLight = null;
#endif

            m_ShadowPassMode = ShadowPassMode.Empty;

            bool stripShadowsOffVariants = cameraData.renderer.stripShadowsOffVariants;
            m_SetKeywordForEmptyShadowmap = ShadowUtils.ShouldEnableKeywordForEmptyShadowmap(stripShadowsOffVariants, shadowData.mainLightShadowsEnabled);
            bool computeShadowParams = mainLight != null
                                       && ShadowUtils.ShouldComputeEmptyShadowmapParams(stripShadowsOffVariants, shadowData.mainLightShadowsEnabled, MainLightHasMixedShadows(mainLight));

            s_EmptyShadowParams = computeShadowParams
                ? ComputeShadowParamsForEmptyRendering(mainLight, cameraData, shadowData)
                : k_DefaultEmptyShadowParams;
        }

        static Light GetMainLight(UniversalLightData lightData)
        {
            int shadowLightIndex = lightData.mainLightIndex;
            return shadowLightIndex != -1 ? lightData.visibleLights[shadowLightIndex].light : null;
        }

        static Vector4 ComputeShadowParamsForEmptyRendering(Light light, UniversalCameraData cameraData, UniversalShadowData shadowData)
        {
            bool softShadows = light.shadows == LightShadows.Soft && shadowData.supportsSoftShadows;
            ShadowUtils.GetMainLightShadowParams(light, softShadows, out float softShadowsProp, out float shadowStrength);
            ShadowUtils.GetScaleAndBiasForLinearDistanceFade(cameraData.maxShadowDistance, shadowData.mainLightShadowCascadeBorder, out float shadowFadeScale, out float shadowFadeBias);
            return new Vector4(shadowStrength, softShadowsProp, shadowFadeScale, shadowFadeBias);
        }

        void UpdateTextureDescriptorIfNeeded()
        {
            if (   m_MainLightShadowDescriptor.width != m_RenderTargetWidth
                || m_MainLightShadowDescriptor.height != m_RenderTargetHeight
                || m_MainLightShadowDescriptor.depthStencilFormat != m_ShadowmapDepthStencilFormat
                || m_MainLightShadowDescriptor.colorFormat != RenderTextureFormat.Shadowmap)
            {
                m_MainLightShadowDescriptor = new RenderTextureDescriptor(m_RenderTargetWidth, m_RenderTargetHeight, GraphicsFormat.None, m_ShadowmapDepthStencilFormat, Texture.GenerateAllMips)
                {
                    shadowSamplingMode = ShadowSamplingMode.CompareDepths
                };
            }
        }

        void Clear()
        {
            for (int i = 0; i < m_CascadeSplitDistances.Length; ++i)
                m_CascadeSplitDistances[i] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

            for (int i = 0; i < m_CascadeSlices.Length; ++i)
                m_CascadeSlices[i].Clear();
        }

        internal static void SetShadowParamsForEmptyShadowmap(IBaseCommandBuffer cmd)
        {
            cmd.SetGlobalVector(MainLightShadowConstantBuffer._ShadowParams, s_EmptyShadowParams);
        }

        // Binds the default (1x1) shadowmap as the global main light shadow texture. Called from the renderer's
        // frame init pass so that a valid texture is always bound, even when this pass doesn't record.
        internal static void SetDefaultShadowmapGlobalTexture(IBaseRenderGraphBuilder builder, TextureHandle defaultShadowTexture)
        {
            builder.SetGlobalTextureAfterPass(defaultShadowTexture, MainLightShadowConstantBuffer._MainLightShadowmapID);
        }

        // Applies the state of the Empty mode (keyword and shadow params).
        // NOTE: Must be called only after Setup() has been called, to ensure m_ShadowPassMode and m_SetKeywordForEmptyShadowmap
        // have been set. For example it's valid to call from within a RenderFunc of a RenderGraph pass.
        // Called from InitRenderGraphFrame; for non-empty modes, the keywords and params are set in the shadow pass itself.
        internal void ApplyEmptyShadowmapGlobals(IBaseCommandBuffer cmd)
        {
            if (m_ShadowPassMode != ShadowPassMode.Empty)
                return;

            if (m_SetKeywordForEmptyShadowmap)
                cmd.EnableKeyword(ShaderGlobalKeywords.MainLightShadows);

            SetShadowParamsForEmptyShadowmap(cmd);
        }

        internal void UpdateGlobalShaderVariables(GlobalShaderVariablesUploader vars, UniversalLightData lightData, UniversalShadowData shadowData)
        {
            if (m_ShadowPassMode == ShadowPassMode.Empty)
            {
                vars._MainLightShadowmapSize = s_EmptyShadowmapSize;
                return;
            }

            int shadowLightIndex = lightData.mainLightIndex;
            if (shadowLightIndex == -1)
                return;

            VisibleLight shadowLight = lightData.visibleLights[shadowLightIndex];
            FillMainLightShadowReceiverVars(ref shadowLight, shadowData, vars);
        }

        void FillMainLightShadowReceiverVars(ref VisibleLight shadowLight, UniversalShadowData shadowData, GlobalShaderVariablesUploader vars)
        {
            MainLightShadowMatrices shadowMatrices = default;

            int cascadeCount = m_ShadowCasterCascadesCount;
            for (int i = 0; i < cascadeCount; ++i)
                shadowMatrices[i] = m_CascadeSlices[i].shadowTransform;

            for (int i = cascadeCount; i <= k_MaxCascades; ++i)
                shadowMatrices[i] = s_NoOpShadowMatrix;

            vars.SetMainLightWorldToShadow(in shadowMatrices);

            // The spheres are only read by ComputeCascadeIndex, which single-cascade shaders never call.
            if (m_ShadowCasterCascadesCount > 1)
            {
                vars._CascadeShadowSplitSpheres0 = m_CascadeSplitDistances[0];
                vars._CascadeShadowSplitSpheres1 = m_CascadeSplitDistances[1];
                vars._CascadeShadowSplitSpheres2 = m_CascadeSplitDistances[2];
                vars._CascadeShadowSplitSpheres3 = m_CascadeSplitDistances[3];
                vars._CascadeShadowSplitSphereRadii = new Vector4(
                    m_CascadeSplitDistances[0].w * m_CascadeSplitDistances[0].w,
                    m_CascadeSplitDistances[1].w * m_CascadeSplitDistances[1].w,
                    m_CascadeSplitDistances[2].w * m_CascadeSplitDistances[2].w,
                    m_CascadeSplitDistances[3].w * m_CascadeSplitDistances[3].w);
            }

            // The offsets and size only feed the soft shadow filters.
            if (shadowData.supportsSoftShadows)
            {
                float invShadowAtlasWidth = 1.0f / m_RenderTargetWidth;
                float invShadowAtlasHeight = 1.0f / m_RenderTargetHeight;
                float invHalfShadowAtlasWidth = 0.5f * invShadowAtlasWidth;
                float invHalfShadowAtlasHeight = 0.5f * invShadowAtlasHeight;

                vars._MainLightShadowOffset0 = new Vector4(-invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight,
                    invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight);
                vars._MainLightShadowOffset1 = new Vector4(-invHalfShadowAtlasWidth, invHalfShadowAtlasHeight,
                    invHalfShadowAtlasWidth, invHalfShadowAtlasHeight);
                vars._MainLightShadowmapSize = new Vector4(invShadowAtlasWidth, invShadowAtlasHeight,
                    m_RenderTargetWidth, m_RenderTargetHeight);
            }
        }

        // Re-broadcasts the main light shadow keywords/constants from persisted state without
        // rendering geometry. Used by the cached pass that reuses a previously rendered atlas.
        void SetMainLightShadowGlobals(RasterCommandBuffer cmd, ref PassData data)
        {
            int shadowLightIndex = data.lightData.mainLightIndex;
            Debug.Assert(shadowLightIndex != -1, "Cached shadow pass has no main light, but the pass that rendered the atlas did. This should not happen.");
            if (shadowLightIndex == -1)
                return;

            VisibleLight shadowLight = data.lightData.visibleLights[shadowLightIndex];
            SetShadowGlobalKeywordsAndConstants(cmd, ref shadowLight, data.shadowData);
        }

        void RenderMainLightCascadeShadowmap(RasterCommandBuffer cmd, ref PassData data)
        {
            var lightData = data.lightData;

            int shadowLightIndex = lightData.mainLightIndex;
            VisibleLight shadowLight = lightData.visibleLights[shadowLightIndex];

            using (new ProfilingScope(cmd, URPProfilingSamplers.MainLightShadow, shadowLight.light))
            {
                // Need to start by setting the Camera position and worldToCamera Matrix as that is not set for passes executed before normal rendering
                ShadowUtils.SetCameraPosition(cmd, data.cameraData.worldSpaceCameraPos);

                float slopeScaleDepthBias = ShadowUtils.GetSlopeScaleDepthBias(ref shadowLight, data.shadowData, shadowLightIndex);

                for (int cascadeIndex = 0; cascadeIndex < m_ShadowCasterCascadesCount; ++cascadeIndex)
                {
                    Vector4 shadowBias = ShadowUtils.GetShadowBias(ref shadowLight, shadowLightIndex, data.shadowData, m_CascadeSlices[cascadeIndex].projectionMatrix, m_CascadeSlices[cascadeIndex].resolution);
                    ShadowUtils.SetupShadowCasterConstantBuffer(cmd, ref shadowLight, shadowBias);
                    cmd.SetKeyword(ShaderGlobalKeywords.CastingPunctualLightShadow, false);
                    RendererList shadowRendererList = data.shadowRendererListsHandle[cascadeIndex];
                    ShadowUtils.RenderShadowSlice(cmd, ref m_CascadeSlices[cascadeIndex], ref shadowRendererList, m_CascadeSlices[cascadeIndex].projectionMatrix, m_CascadeSlices[cascadeIndex].viewMatrix, slopeScaleDepthBias);
                }
                SetShadowGlobalKeywordsAndConstants(cmd, ref shadowLight, data.shadowData);
            }
        }

        internal void SetShadowGlobalKeywordsAndConstants(RasterCommandBuffer cmd, ref VisibleLight shadowLight, UniversalShadowData shadowData)
        {
            shadowData.isKeywordSoftShadowsEnabled = shadowLight.light.shadows == LightShadows.Soft && shadowData.supportsSoftShadows;
            cmd.SetKeyword(ShaderGlobalKeywords.MainLightShadows, shadowData.mainLightShadowCascadesCount == 1);
            cmd.SetKeyword(ShaderGlobalKeywords.MainLightShadowCascades, shadowData.mainLightShadowCascadesCount > 1);
            ShadowUtils.SetSoftShadowQualityShaderKeywords(cmd, shadowData);

            Light light = shadowLight.light;
            bool softShadows = light.shadows == LightShadows.Soft && shadowData.supportsSoftShadows;
            ShadowUtils.GetMainLightShadowParams(light, softShadows, out float softShadowsProp, out float shadowStrength);
            ShadowUtils.GetScaleAndBiasForLinearDistanceFade(m_MaxShadowDistanceSq, m_CascadeBorder, out float shadowFadeScale, out float shadowFadeBias);
            cmd.SetGlobalVector(MainLightShadowConstantBuffer._ShadowParams,
                new Vector4(shadowStrength, softShadowsProp, shadowFadeScale, shadowFadeBias));
        }

        private void InitPassData(
            ref PassData passData,
            UniversalRenderingData renderingData,
            UniversalCameraData cameraData,
            UniversalLightData lightData,
            UniversalShadowData shadowData)
        {
            passData.pass = this;
            passData.mode = m_ShadowPassMode;
            passData.renderingData = renderingData;
            passData.cameraData = cameraData;
            passData.lightData = lightData;
            passData.shadowData = shadowData;
        }

        private void InitRendererLists(ref PassData passData, RenderGraph renderGraph)
        {
            Debug.Assert(m_ShadowPassMode == ShadowPassMode.DrawGeometry, "Renderer lists should only be used in DrawGeometry mode.");

            int shadowLightIndex = passData.lightData.mainLightIndex;
            ShadowDrawingSettings settings = new (passData.renderingData.cullResults, shadowLightIndex) {
                useRenderingLayerMaskTest = UniversalRenderPipeline.asset.useRenderingLayers,
                sortShadowcastersByRenderQueue = GraphicsFormatUtility.IsStencilFormat(m_ShadowmapDepthStencilFormat),
            };

            for (int cascadeIndex = 0; cascadeIndex < m_ShadowCasterCascadesCount; ++cascadeIndex)
            {
                passData.shadowRendererListsHandle[cascadeIndex] = renderGraph.CreateShadowRendererList(ref settings);
            }
        }

        // Allocates the shadow atlas for the frame. When shadow map caching is active, a persistent RTHandle is imported
        // so the texture can survive the RenderGraph frame - otherwise a transient RenderGraph texture is created.
        TextureHandle GetOrCreateMainShadowsTextureHandle(RenderGraph renderGraph, UniversalShadowData shadowData)
        {
            if (shadowData.shadowMapCachingEnabled)
            {
                RenderingUtils.ReAllocateHandleIfNeeded(ref m_MainLightShadowmapTexture,
                               m_MainLightShadowDescriptor,
                               ShadowUtils.m_ForceShadowPointSampling ? FilterMode.Point : FilterMode.Bilinear,
                               TextureWrapMode.Repeat, 1, 0, k_MainLightShadowMapTextureName);

                ImportResourceParams importParams = new ImportResourceParams();
                importParams.clearOnFirstUse = !shadowData.useCachedShadowMap;
                importParams.clearColor = Color.black;
                importParams.discardOnLastUse = shadowData.useCachedShadowMap;
                return renderGraph.ImportTexture(m_MainLightShadowmapTexture, importParams);
            }

            return UniversalRenderer.CreateRenderGraphTexture(
                renderGraph, m_MainLightShadowDescriptor, k_MainLightShadowMapTextureName, true,
                ShadowUtils.m_ForceShadowPointSampling ? FilterMode.Point : FilterMode.Bilinear);
        }

        // Standard RecordRenderGraph override. Must be preceded by a call to Setup(), which decides the ShadowPassMode
        // for the pass. The pass is recorded for the DrawGeometry and ReuseCachedAtlas modes, while the Empty mode is
        // handled by the renderer's frame init pass (see ApplyEmptyShadowmapGlobals) to avoid overhead from recording a
        // pass that only sets globals. The shadow texture is always exposed through UniversalResourceData, so downstream
        // passes always see a valid handle.
        /// <inheritdoc/>
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            UniversalShadowData shadowData = frameData.Get<UniversalShadowData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            if (m_ShadowPassMode == ShadowPassMode.Empty)
            {
                resourceData.mainShadowsTexture = renderGraph.defaultResources.defaultShadowTexture;
                return;
            }

            bool drawsGeometry = m_ShadowPassMode == ShadowPassMode.DrawGeometry;

            // Allocate the shadow texture and expose it through resourceData so downstream passes can
            // reference it even before this pass executes.
            resourceData.mainShadowsTexture = GetOrCreateMainShadowsTextureHandle(renderGraph, shadowData);

            string passLabel = drawsGeometry ? passName : s_SetKeywordsSampler.name;
            ProfilingSampler sampler = drawsGeometry ? profilingSampler : s_SetKeywordsSampler;

            using (var builder = renderGraph.AddRasterRenderPass<PassData>(passLabel, out var passData, sampler))
            {
                InitPassData(ref passData, renderingData, cameraData, lightData, shadowData);

                if (m_ShadowPassMode == ShadowPassMode.DrawGeometry)
                {
                    InitRendererLists(ref passData, renderGraph);
                    for (int cascadeIndex = 0; cascadeIndex < m_ShadowCasterCascadesCount; ++cascadeIndex)
                        builder.UseRendererList(passData.shadowRendererListsHandle[cascadeIndex]);
                    builder.SetRenderAttachmentDepth(resourceData.mainShadowsTexture, AccessFlags.ReadWrite);
                }

                builder.AllowGlobalStateModification(true);
                builder.SetGlobalTextureAfterPass(resourceData.mainShadowsTexture, MainLightShadowConstantBuffer._MainLightShadowmapID);
                builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                {
                    RasterCommandBuffer cmd = context.cmd;
                    switch (data.mode)
                    {
                        case ShadowPassMode.DrawGeometry:
                            data.pass.RenderMainLightCascadeShadowmap(cmd, ref data);
                            break;
                        case ShadowPassMode.ReuseCachedAtlas:
                            data.pass.SetMainLightShadowGlobals(cmd, ref data);
                            break;
                        default:
                            Debug.Assert(false, $"Unhandled {nameof(ShadowPassMode)}: {data.mode}");
                            break;
                    }
                });
            }
        }
    };
}
