using System;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.Internal
{
    /// <summary>
    /// Renders a shadow map for the main Light.
    /// </summary>
    public class MainLightShadowCasterPass : ScriptableRenderPass
    {
        // Internal
        internal RTHandle m_MainLightShadowmapTexture;

        // Private
        private int renderTargetWidth;
        private int renderTargetHeight;
        private int m_ShadowCasterCascadesCount;
        private bool m_CreateEmptyShadowmap;
        private bool m_SetKeywordForEmptyShadowmap;
        private bool m_EmptyShadowmapNeedsClear;
        private float m_CascadeBorder;
        private float m_MaxShadowDistanceSq;
        private PassData m_PassData;
        private RTHandle m_EmptyMainLightShadowmapTexture;
        private RenderTextureDescriptor m_MainLightShadowDescriptor;
        private readonly Vector4[] m_CascadeSplitDistances;
        private readonly Matrix4x4[] m_MainLightShadowMatrices;
        private readonly ProfilingSampler m_ProfilingSetupSampler = new ("Setup Main Shadowmap");
        private readonly ShadowSliceData[] m_CascadeSlices;

        // Constants and Statics
        private const int k_EmptyShadowMapDimensions = 1;
        private const int k_MaxCascades = 4;
        private const int k_ShadowmapBufferBits = 16;
        private const string k_MainLightShadowMapTextureName = "_MainLightShadowmapTexture";
        private const string k_EmptyMainLightShadowMapTextureName = "_EmptyMainLightShadowmapTexture";
        private static Vector4 s_EmptyShadowParams = new (0f, 0f, 1f, 0f);
        private static readonly Vector4 s_EmptyShadowmapSize = new (k_EmptyShadowMapDimensions, 1f / k_EmptyShadowMapDimensions, k_EmptyShadowMapDimensions, k_EmptyShadowMapDimensions);

        // Classes
        private static class MainLightShadowConstantBuffer
        {
            public static readonly int _WorldToShadow = Shader.PropertyToID("_MainLightWorldToShadow");
            public static readonly int _ShadowParams = Shader.PropertyToID("_MainLightShadowParams");
            public static readonly int _CascadeShadowSplitSpheres0 = Shader.PropertyToID("_CascadeShadowSplitSpheres0");
            public static readonly int _CascadeShadowSplitSpheres1 = Shader.PropertyToID("_CascadeShadowSplitSpheres1");
            public static readonly int _CascadeShadowSplitSpheres2 = Shader.PropertyToID("_CascadeShadowSplitSpheres2");
            public static readonly int _CascadeShadowSplitSpheres3 = Shader.PropertyToID("_CascadeShadowSplitSpheres3");
            public static readonly int _CascadeShadowSplitSphereRadii = Shader.PropertyToID("_CascadeShadowSplitSphereRadii");
            public static readonly int _ShadowOffset0 = Shader.PropertyToID("_MainLightShadowOffset0");
            public static readonly int _ShadowOffset1 = Shader.PropertyToID("_MainLightShadowOffset1");
            public static readonly int _ShadowmapSize = Shader.PropertyToID("_MainLightShadowmapSize");
            public static readonly int _MainLightShadowmapID = Shader.PropertyToID(k_MainLightShadowMapTextureName);
        }

        private class PassData
        {
            internal bool emptyShadowmap;
            internal bool setKeywordForEmptyShadowmap;
            internal UniversalRenderingData renderingData;
            internal UniversalCameraData cameraData;
            internal UniversalLightData lightData;
            internal UniversalShadowData shadowData;
            internal MainLightShadowCasterPass pass;
            internal TextureHandle shadowmapTexture;
            internal readonly RendererList[] shadowRendererLists = new RendererList[k_MaxCascades];
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

            m_PassData = new PassData();
            m_MainLightShadowMatrices = new Matrix4x4[k_MaxCascades + 1];
            m_CascadeSlices = new ShadowSliceData[k_MaxCascades];
            m_CascadeSplitDistances = new Vector4[k_MaxCascades];

            m_EmptyShadowmapNeedsClear = true;
        }

        /// <summary>
        /// Cleans up resources used by the pass.
        /// </summary>
        public void Dispose()
        {
            m_MainLightShadowmapTexture?.Release();
            m_EmptyMainLightShadowmapTexture?.Release();
        }

        /// <summary>
        /// Sets up the pass.
        /// </summary>
        /// <param name="renderingData"></param>
        /// <returns>True if the pass should be enqueued, otherwise false.</returns>
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

        /// <summary>
        /// Sets up the pass.
        /// </summary>
        /// <param name="renderingData">Data containing rendering settings.</param>
        /// <param name="cameraData">Data containing camera settings.</param>
        /// <param name="lightData">Data containing light settings.</param>
        /// <param name="shadowData">Data containing shadow settings.</param>
        /// <returns>True if the pass should be enqueued, otherwise false.</returns>
        /// <seealso cref="RenderingData"/>
        public bool Setup(UniversalRenderingData renderingData, UniversalCameraData cameraData, UniversalLightData lightData, UniversalShadowData shadowData)
        {
            bool shadowsEnabled = shadowData.mainLightShadowsEnabled;
            bool shadowsSupported = shadowData.supportsMainLightShadows;

#if UNITY_EDITOR
            if (CoreUtils.IsSceneLightingDisabled(cameraData.camera))
                return false;
#endif

            using var profScope = new ProfilingScope(m_ProfilingSetupSampler);

            bool stripShadowsOffVariants = cameraData.renderer.stripShadowsOffVariants;

            Clear();
            int shadowLightIndex = lightData.mainLightIndex;
            if (shadowLightIndex == -1 || (cameraData.camera.targetTexture != null && cameraData.camera.targetTexture.format == RenderTextureFormat.Depth))
            {
                if (shadowsEnabled)
                    return SetupForEmptyRendering(stripShadowsOffVariants, shadowsEnabled, null, cameraData, shadowData);
                else
                    return false;
            }

            VisibleLight shadowLight = lightData.visibleLights[shadowLightIndex];
            Light light = shadowLight.light;
            if (shadowsSupported && light.shadows == LightShadows.None)
                return SetupForEmptyRendering(stripShadowsOffVariants, shadowsEnabled, light, cameraData, shadowData);

            if (!shadowsEnabled)
            {
                // If (realtime) shadows are disabled, but the light casts baked shadows, we need to do empty rendering to setup the _MainLightShadowParams uniform,
                // which is also used when sampling baked shadows. This allows for using baked shadows even when realtime shadows are completely disabled.
                if (light.shadows != LightShadows.None &&
                    light.bakingOutput.isBaked &&
                    light.bakingOutput.mixedLightingMode != MixedLightingMode.IndirectOnly &&
                    light.bakingOutput.lightmapBakeType == LightmapBakeType.Mixed)
                {
                    return SetupForEmptyRendering(stripShadowsOffVariants, shadowsEnabled, light, cameraData, shadowData);
                }

                return false;
            }

            if (!shadowsSupported)
                return SetupForEmptyRendering(stripShadowsOffVariants, shadowsEnabled, null, cameraData, shadowData);

            if (shadowLight.lightType != LightType.Directional)
            {
                Debug.LogWarning("Only directional lights are supported as main light.");
            }

            if (!renderingData.cullResults.GetShadowCasterBounds(shadowLightIndex, out Bounds _))
                return SetupForEmptyRendering(stripShadowsOffVariants, shadowsEnabled, light, cameraData, shadowData);

            m_ShadowCasterCascadesCount = shadowData.mainLightShadowCascadesCount;
            renderTargetWidth = shadowData.mainLightRenderTargetWidth;
            renderTargetHeight = shadowData.mainLightRenderTargetHeight;

            ref readonly URPLightShadowCullingInfos shadowCullingInfos = ref shadowData.visibleLightsShadowCullingInfos.UnsafeElementAt(shadowLightIndex);

            for (int cascadeIndex = 0; cascadeIndex < m_ShadowCasterCascadesCount; ++cascadeIndex)
            {
                ref readonly ShadowSliceData sliceData = ref shadowCullingInfos.slices.UnsafeElementAt(cascadeIndex);
                m_CascadeSplitDistances[cascadeIndex] = sliceData.splitData.cullingSphere;
                m_CascadeSlices[cascadeIndex] = sliceData;

                if (!shadowCullingInfos.IsSliceValid(cascadeIndex))
                    return SetupForEmptyRendering(stripShadowsOffVariants, shadowsEnabled, light, cameraData, shadowData);
            }

            UpdateTextureDescriptorIfNeeded();

            m_MaxShadowDistanceSq = cameraData.maxShadowDistance * cameraData.maxShadowDistance;
            m_CascadeBorder = shadowData.mainLightShadowCascadeBorder;
            m_CreateEmptyShadowmap = false;
            useNativeRenderPass = true;

            return true;
        }

        private void UpdateTextureDescriptorIfNeeded()
        {
            if (   m_MainLightShadowDescriptor.width != renderTargetWidth
                || m_MainLightShadowDescriptor.height != renderTargetHeight
                || m_MainLightShadowDescriptor.depthBufferBits != k_ShadowmapBufferBits
                || m_MainLightShadowDescriptor.colorFormat != RenderTextureFormat.Shadowmap)
            {
                m_MainLightShadowDescriptor = new RenderTextureDescriptor(renderTargetWidth, renderTargetHeight, RenderTextureFormat.Shadowmap, k_ShadowmapBufferBits);
            }
        }

        bool SetupForEmptyRendering(bool stripShadowsOffVariants, bool shadowsEnabled, Light light, UniversalCameraData cameraData, UniversalShadowData shadowData)
        {
            if (!stripShadowsOffVariants)
                return false;

            m_CreateEmptyShadowmap = true;
            useNativeRenderPass = false;

            m_SetKeywordForEmptyShadowmap = shadowsEnabled;

            // Even though there are not real-time shadows, the light might be using shadowmasks,
            // which is why we need to update the shadow parameters, for example so shadow strength can be used.

            if (light == null)
            {
                s_EmptyShadowParams = new Vector4(0, 0, 1, 0);
            }
            else
            {
                bool supportsSoftShadows = shadowData.supportsSoftShadows;
                float maxShadowDistanceSq = cameraData.maxShadowDistance;
                float mainLightShadowCascadeBorder = shadowData.mainLightShadowCascadeBorder;

                bool softShadows = light.shadows == LightShadows.Soft && supportsSoftShadows;
                float softShadowsProp = ShadowUtils.SoftShadowQualityToShaderProperty(light, softShadows);
                ShadowUtils.GetScaleAndBiasForLinearDistanceFade(maxShadowDistanceSq, mainLightShadowCascadeBorder, out float shadowFadeScale, out float shadowFadeBias);
                s_EmptyShadowParams =  new Vector4(light.shadowStrength, softShadowsProp, shadowFadeScale, shadowFadeBias);
            }

            return true;
        }

        /// <inheritdoc />
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Configure(CommandBuffer cmd, RenderTextureDescriptor cameraTextureDescriptor)
        {
            // Disable obsolete warning for internal usage
            #pragma warning disable CS0618

            if (m_CreateEmptyShadowmap)
            {
                // Required for scene view camera(URP renderer not initialized)
                if (ShadowUtils.ShadowRTReAllocateIfNeeded(ref m_EmptyMainLightShadowmapTexture, k_EmptyShadowMapDimensions, k_EmptyShadowMapDimensions, k_ShadowmapBufferBits, name: k_EmptyMainLightShadowMapTextureName))
                    m_EmptyShadowmapNeedsClear = true;

                if (!m_EmptyShadowmapNeedsClear)
                    return;

                ConfigureTarget(m_EmptyMainLightShadowmapTexture);
                m_EmptyShadowmapNeedsClear = false;
            }
            else
            {
                ShadowUtils.ShadowRTReAllocateIfNeeded(ref m_MainLightShadowmapTexture, renderTargetWidth, renderTargetHeight, k_ShadowmapBufferBits, name: k_MainLightShadowMapTextureName);
                ConfigureTarget(m_MainLightShadowmapTexture);
            }

            ConfigureClear(ClearFlag.All, Color.black);

            #pragma warning restore CS0618
        }

        /// <inheritdoc/>
        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ContextContainer frameData = renderingData.frameData;
            UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            UniversalShadowData shadowData = frameData.Get<UniversalShadowData>();

            RasterCommandBuffer rasterCommandBuffer = CommandBufferHelpers.GetRasterCommandBuffer(renderingData.commandBuffer);
            if (m_CreateEmptyShadowmap)
            {
                if (m_SetKeywordForEmptyShadowmap)
                    rasterCommandBuffer.EnableKeyword(ShaderGlobalKeywords.MainLightShadows);
                SetShadowParamsForEmptyShadowmap(rasterCommandBuffer);
                universalRenderingData.commandBuffer.SetGlobalTexture(MainLightShadowConstantBuffer._MainLightShadowmapID, m_EmptyMainLightShadowmapTexture.nameID);
                return;
            }

            InitPassData(ref m_PassData, universalRenderingData, cameraData, lightData, shadowData);
            InitRendererLists(ref m_PassData, context, default(RenderGraph), false);

            RenderMainLightCascadeShadowmap(rasterCommandBuffer, ref m_PassData, false);
            universalRenderingData.commandBuffer.SetGlobalTexture(MainLightShadowConstantBuffer._MainLightShadowmapID, m_MainLightShadowmapTexture.nameID);
        }

        void Clear()
        {
            for (int i = 0; i < m_MainLightShadowMatrices.Length; ++i)
                m_MainLightShadowMatrices[i] = Matrix4x4.identity;

            for (int i = 0; i < m_CascadeSplitDistances.Length; ++i)
                m_CascadeSplitDistances[i] = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);

            for (int i = 0; i < m_CascadeSlices.Length; ++i)
                m_CascadeSlices[i].Clear();
        }

        internal static void SetShadowParamsForEmptyShadowmap(RasterCommandBuffer rasterCommandBuffer)
        {
            rasterCommandBuffer.SetGlobalVector(MainLightShadowConstantBuffer._ShadowmapSize, s_EmptyShadowmapSize);
            rasterCommandBuffer.SetGlobalVector(MainLightShadowConstantBuffer._ShadowParams, s_EmptyShadowParams);
        }

        void RenderMainLightCascadeShadowmap(RasterCommandBuffer cmd, ref PassData data, bool isRenderGraph)
        {
            var lightData = data.lightData;

            int shadowLightIndex = lightData.mainLightIndex;
            if (shadowLightIndex == -1)
                return;

            VisibleLight shadowLight = lightData.visibleLights[shadowLightIndex];

            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.MainLightShadow)))
            {
                // Need to start by setting the Camera position and worldToCamera Matrix as that is not set for passes executed before normal rendering
                ShadowUtils.SetCameraPosition(cmd, data.cameraData.worldSpaceCameraPos);

                // For non-RG, need set the worldToCamera Matrix as that is not set for passes executed before normal rendering,
                // otherwise shadows will behave incorrectly when Scene and Game windows are open at the same time (UUM-63267).
                if (!isRenderGraph)
                    ShadowUtils.SetWorldToCameraAndCameraToWorldMatrices(cmd, data.cameraData.GetViewMatrix());

                for (int cascadeIndex = 0; cascadeIndex < m_ShadowCasterCascadesCount; ++cascadeIndex)
                {
                    Vector4 shadowBias = ShadowUtils.GetShadowBias(ref shadowLight, shadowLightIndex, data.shadowData, m_CascadeSlices[cascadeIndex].projectionMatrix, m_CascadeSlices[cascadeIndex].resolution);
                    ShadowUtils.SetupShadowCasterConstantBuffer(cmd, ref shadowLight, shadowBias);
                    cmd.SetKeyword(ShaderGlobalKeywords.CastingPunctualLightShadow, false);
                    RendererList shadowRendererList = isRenderGraph? data.shadowRendererListsHandle[cascadeIndex] : data.shadowRendererLists[cascadeIndex];
                    ShadowUtils.RenderShadowSlice(cmd, ref m_CascadeSlices[cascadeIndex], ref shadowRendererList, m_CascadeSlices[cascadeIndex].projectionMatrix, m_CascadeSlices[cascadeIndex].viewMatrix);
                }

                data.shadowData.isKeywordSoftShadowsEnabled = shadowLight.light.shadows == LightShadows.Soft && data.shadowData.supportsSoftShadows;
                cmd.SetKeyword(ShaderGlobalKeywords.MainLightShadows, data.shadowData.mainLightShadowCascadesCount == 1);
                cmd.SetKeyword(ShaderGlobalKeywords.MainLightShadowCascades, data.shadowData.mainLightShadowCascadesCount > 1);
                ShadowUtils.SetSoftShadowQualityShaderKeywords(cmd, data.shadowData);

                SetupMainLightShadowReceiverConstants(cmd, ref shadowLight, data.shadowData);
            }
        }

        void SetupMainLightShadowReceiverConstants(RasterCommandBuffer cmd, ref VisibleLight shadowLight, UniversalShadowData shadowData)
        {
            Light light = shadowLight.light;
            bool softShadows = shadowLight.light.shadows == LightShadows.Soft && shadowData.supportsSoftShadows;

            int cascadeCount = m_ShadowCasterCascadesCount;
            for (int i = 0; i < cascadeCount; ++i)
                m_MainLightShadowMatrices[i] = m_CascadeSlices[i].shadowTransform;

            // We setup and additional a no-op WorldToShadow matrix in the last index
            // because the ComputeCascadeIndex function in Shadows.hlsl can return an index
            // out of bounds. (position not inside any cascade) and we want to avoid branching
            Matrix4x4 noOpShadowMatrix = Matrix4x4.zero;
            noOpShadowMatrix.m22 = (SystemInfo.usesReversedZBuffer) ? 1.0f : 0.0f;
            for (int i = cascadeCount; i <= k_MaxCascades; ++i)
                m_MainLightShadowMatrices[i] = noOpShadowMatrix;

            float invShadowAtlasWidth = 1.0f / renderTargetWidth;
            float invShadowAtlasHeight = 1.0f / renderTargetHeight;
            float invHalfShadowAtlasWidth = 0.5f * invShadowAtlasWidth;
            float invHalfShadowAtlasHeight = 0.5f * invShadowAtlasHeight;
            float softShadowsProp = ShadowUtils.SoftShadowQualityToShaderProperty(light, softShadows);

            ShadowUtils.GetScaleAndBiasForLinearDistanceFade(m_MaxShadowDistanceSq, m_CascadeBorder, out float shadowFadeScale, out float shadowFadeBias);

            cmd.SetGlobalMatrixArray(MainLightShadowConstantBuffer._WorldToShadow, m_MainLightShadowMatrices);
            cmd.SetGlobalVector(MainLightShadowConstantBuffer._ShadowParams,
                new Vector4(light.shadowStrength, softShadowsProp, shadowFadeScale, shadowFadeBias));

            if (m_ShadowCasterCascadesCount > 1)
            {
                cmd.SetGlobalVector(MainLightShadowConstantBuffer._CascadeShadowSplitSpheres0,
                    m_CascadeSplitDistances[0]);
                cmd.SetGlobalVector(MainLightShadowConstantBuffer._CascadeShadowSplitSpheres1,
                    m_CascadeSplitDistances[1]);
                cmd.SetGlobalVector(MainLightShadowConstantBuffer._CascadeShadowSplitSpheres2,
                    m_CascadeSplitDistances[2]);
                cmd.SetGlobalVector(MainLightShadowConstantBuffer._CascadeShadowSplitSpheres3,
                    m_CascadeSplitDistances[3]);
                cmd.SetGlobalVector(MainLightShadowConstantBuffer._CascadeShadowSplitSphereRadii, new Vector4(
                    m_CascadeSplitDistances[0].w * m_CascadeSplitDistances[0].w,
                    m_CascadeSplitDistances[1].w * m_CascadeSplitDistances[1].w,
                    m_CascadeSplitDistances[2].w * m_CascadeSplitDistances[2].w,
                    m_CascadeSplitDistances[3].w * m_CascadeSplitDistances[3].w));
            }

            // Inside shader soft shadows are controlled through global keyword.
            // If any additional light has soft shadows it will force soft shadows on main light too.
            // As it is not trivial finding out which additional light has soft shadows, we will pass main light properties if soft shadows are supported.
            // This workaround will be removed once we will support soft shadows per light.
            if (shadowData.supportsSoftShadows)
            {
                cmd.SetGlobalVector(MainLightShadowConstantBuffer._ShadowOffset0,
                    new Vector4(-invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight,
                        invHalfShadowAtlasWidth, -invHalfShadowAtlasHeight));
                cmd.SetGlobalVector(MainLightShadowConstantBuffer._ShadowOffset1,
                    new Vector4(-invHalfShadowAtlasWidth, invHalfShadowAtlasHeight,
                        invHalfShadowAtlasWidth, invHalfShadowAtlasHeight));

                cmd.SetGlobalVector(MainLightShadowConstantBuffer._ShadowmapSize, new Vector4(invShadowAtlasWidth,
                    invShadowAtlasHeight,
                    renderTargetWidth, renderTargetHeight));
            }
        }

        private void InitPassData(
            ref PassData passData,
            UniversalRenderingData renderingData,
            UniversalCameraData cameraData,
            UniversalLightData lightData,
            UniversalShadowData shadowData)
        {
            passData.pass = this;
            passData.emptyShadowmap = m_CreateEmptyShadowmap;
            passData.setKeywordForEmptyShadowmap = m_SetKeywordForEmptyShadowmap;
            passData.renderingData = renderingData;
            passData.cameraData = cameraData;
            passData.lightData = lightData;
            passData.shadowData = shadowData;
        }

        private void InitRendererLists(ref PassData passData, ScriptableRenderContext context, RenderGraph renderGraph, bool useRenderGraph)
        {
            int shadowLightIndex = passData.lightData.mainLightIndex;
            if (!m_CreateEmptyShadowmap && shadowLightIndex != -1)
            {
                ShadowDrawingSettings settings = new (passData.renderingData.cullResults, shadowLightIndex) {
                    useRenderingLayerMaskTest = UniversalRenderPipeline.asset.useRenderingLayers
                };

                for (int cascadeIndex = 0; cascadeIndex < m_ShadowCasterCascadesCount; ++cascadeIndex)
                {
                    if (useRenderGraph)
                        passData.shadowRendererListsHandle[cascadeIndex] = renderGraph.CreateShadowRendererList(ref settings);
                    else
                        passData.shadowRendererLists[cascadeIndex] = context.CreateShadowRendererList(ref settings);
                }
            }
        }

        internal TextureHandle Render(RenderGraph graph, ContextContainer frameData)
        {
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            UniversalShadowData shadowData = frameData.Get<UniversalShadowData>();

            TextureHandle shadowTexture;

            using (var builder = graph.AddRasterRenderPass<PassData>(passName, out var passData, profilingSampler))
            {
                InitPassData(ref passData, renderingData, cameraData, lightData, shadowData);
                InitRendererLists(ref passData, default(ScriptableRenderContext), graph, true);

                if (!m_CreateEmptyShadowmap)
                {
                    for (int cascadeIndex = 0; cascadeIndex < m_ShadowCasterCascadesCount; ++cascadeIndex)
                    {
                        builder.UseRendererList(passData.shadowRendererListsHandle[cascadeIndex]);
                    }

                    shadowTexture = UniversalRenderer.CreateRenderGraphTexture(graph, m_MainLightShadowDescriptor, k_MainLightShadowMapTextureName, true, ShadowUtils.m_ForceShadowPointSampling ? FilterMode.Point : FilterMode.Bilinear);
                    builder.SetRenderAttachmentDepth(shadowTexture, AccessFlags.Write);
                }
                else
                {
                    shadowTexture = graph.defaultResources.defaultShadowTexture;
                }

                builder.AllowGlobalStateModification(true);

                if (shadowTexture.IsValid())
                    builder.SetGlobalTextureAfterPass(shadowTexture, MainLightShadowConstantBuffer._MainLightShadowmapID);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                {
                    RasterCommandBuffer rasterCommandBuffer = context.cmd;
                    if (!data.emptyShadowmap)
                    {
                        data.pass.RenderMainLightCascadeShadowmap(rasterCommandBuffer, ref data, true);
                    }
                    else
                    {
                        if (data.setKeywordForEmptyShadowmap)
                            rasterCommandBuffer.EnableKeyword(ShaderGlobalKeywords.MainLightShadows);
                        SetShadowParamsForEmptyShadowmap(rasterCommandBuffer);
                    }
                });
            }

            return shadowTexture;
        }
    };
}
