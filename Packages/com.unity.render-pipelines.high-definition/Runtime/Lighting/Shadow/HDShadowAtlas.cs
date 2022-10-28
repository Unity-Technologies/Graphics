using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    abstract class HDShadowAtlas
    {
        internal struct HDShadowAtlasInitParameters
        {
            internal HDRenderPipelineRuntimeResources renderPipelineResources;
            internal RenderGraph renderGraph;
            internal bool useSharedTexture;
            internal int width;
            internal int height;
            internal int atlasShaderID;
            internal int maxShadowRequests;
            internal string name;

            internal Material clearMaterial;
            internal HDShadowInitParameters initParams;
            internal BlurAlgorithm blurAlgorithm;
            internal FilterMode filterMode;
            internal DepthBits depthBufferBits;
            internal RenderTextureFormat format;
            internal ConstantBuffer<ShaderVariablesGlobal> cb;

            internal HDShadowAtlasInitParameters(HDRenderPipelineRuntimeResources renderPipelineResources, RenderGraph renderGraph, bool useSharedTexture, int width, int height, int atlasShaderID,
                                                 Material clearMaterial, int maxShadowRequests, HDShadowInitParameters initParams, ConstantBuffer<ShaderVariablesGlobal> cb)
            {
                this.renderPipelineResources = renderPipelineResources;
                this.renderGraph = renderGraph;
                this.useSharedTexture = useSharedTexture;
                this.width = width;
                this.height = height;
                this.atlasShaderID = atlasShaderID;
                this.clearMaterial = clearMaterial;
                this.maxShadowRequests = maxShadowRequests;
                this.initParams = initParams;
                this.blurAlgorithm = BlurAlgorithm.None;
                this.filterMode = FilterMode.Bilinear;
                this.depthBufferBits = DepthBits.Depth16;
                this.format = RenderTextureFormat.Shadowmap;
                this.name = "";

                this.cb = cb;
            }
        }

        public enum BlurAlgorithm
        {
            None,
            EVSM, // exponential variance shadow maps
            IM // Improved Moment shadow maps
        }

        protected List<HDShadowRequest> m_ShadowRequests = new List<HDShadowRequest>();

        public int width { get; private set; }
        public int height { get; private set; }

        Material m_ClearMaterial;
        LightingDebugSettings m_LightingDebugSettings;
        FilterMode m_FilterMode;
        DepthBits m_DepthBufferBits;
        RenderTextureFormat m_Format;
        string m_Name;
        string m_MomentName;
        string m_MomentCopyName;
        string m_IntermediateSummedAreaName;
        string m_SummedAreaName;
        int m_AtlasShaderID;
        HDRenderPipelineRuntimeResources m_RenderPipelineResources;

        // Moment shadow data
        BlurAlgorithm m_BlurAlgorithm;

        // This is only a reference that is hold by the atlas, but its lifetime is responsibility of the shadow manager.
        ConstantBuffer<ShaderVariablesGlobal> m_GlobalConstantBuffer;

        // This must be true for atlas that contain cached data (effectively this
        // drives what to do with mixed cached shadow map -> if true we filter with only static
        // if false we filter only for dynamic)
        protected bool m_IsACacheForShadows;

        // In case of using shared persistent render graph textures.
        bool m_UseSharedTexture;
        protected TextureHandle m_Output;
        protected TextureHandle m_ShadowMapOutput;

        public TextureDesc GetShadowMapTextureDesc()
        {
            return new TextureDesc(width, height)
            { filterMode = m_FilterMode, depthBufferBits = m_DepthBufferBits, isShadowMap = true, name = m_Name };
        }

        public HDShadowAtlas() { }

        public virtual void InitAtlas(HDShadowAtlasInitParameters initParams)
        {
            this.width = initParams.width;
            this.height = initParams.height;
            m_FilterMode = initParams.filterMode;
            m_DepthBufferBits = initParams.depthBufferBits;
            m_Format = initParams.format;
            m_Name = initParams.name;
            // With render graph, textures are "allocated" every frame so we need to prepare strings beforehand.
            m_MomentName = m_Name + "Moment";
            m_MomentCopyName = m_Name + "MomentCopy";
            m_IntermediateSummedAreaName = m_Name + "IntermediateSummedArea";
            m_SummedAreaName = m_Name + "SummedAreaFinal";
            m_AtlasShaderID = initParams.atlasShaderID;
            m_ClearMaterial = initParams.clearMaterial;
            m_BlurAlgorithm = initParams.blurAlgorithm;
            m_RenderPipelineResources = initParams.renderPipelineResources;
            m_IsACacheForShadows = false;

            m_GlobalConstantBuffer = initParams.cb;

            InitializeRenderGraphOutput(initParams.renderGraph, initParams.useSharedTexture);
        }

        public HDShadowAtlas(HDShadowAtlasInitParameters initParams)
        {
            InitAtlas(initParams);
        }

        TextureDesc GetMomentAtlasDesc(string name)
        {
            return new TextureDesc(width / 2, height / 2)
            { colorFormat = GraphicsFormat.R32G32_SFloat, useMipMap = true, autoGenerateMips = false, name = name, enableRandomWrite = true };
        }

        TextureDesc GetImprovedMomentAtlasDesc()
        {
            return new TextureDesc(width, height)
            { colorFormat = GraphicsFormat.R32G32B32A32_SFloat, name = m_MomentName, enableRandomWrite = true, clearColor = Color.black };
        }

        TextureDesc GetAtlasDesc()
        {
            switch (m_BlurAlgorithm)
            {
                case (BlurAlgorithm.None):
                    return GetShadowMapTextureDesc();
                case BlurAlgorithm.EVSM:
                    return GetMomentAtlasDesc(m_MomentName);
                case BlurAlgorithm.IM:
                    return GetImprovedMomentAtlasDesc();
            }

            return default;
        }

        public void UpdateSize(Vector2Int size)
        {
            width = size.x;
            height = size.y;
        }

        internal void AddShadowRequest(HDShadowRequest shadowRequest)
        {
            m_ShadowRequests.Add(shadowRequest);
        }

        public void UpdateDebugSettings(LightingDebugSettings lightingDebugSettings)
        {
            m_LightingDebugSettings = lightingDebugSettings;
        }

        public void InvalidateOutputIfNeeded()
        {
            // Since we now store the output TextureHandle (because we only want to create the texture once depending on the control flow and because of shared textures),
            // we need to be careful not to keep a "valid" handle when it's not a shared resource.
            // Indeed, if for example we don't render with the atlas for a few frames, this handle will "look" valid (with a valid index internally) but its index will not match any valid resource.
            // To avoid that, we invalidate it explicitly at the start of every frame if it's not a shared resource.
            if (!m_UseSharedTexture)
            {
                m_Output = TextureHandle.nullHandle;
            }
        }

        public TextureHandle GetOutputTexture(RenderGraph renderGraph)
        {
            if (m_UseSharedTexture)
            {
                Debug.Assert(m_Output.IsValid());

                return m_Output; // Should always be valid.
            }
            else
            {
                renderGraph.CreateTextureIfInvalid(GetAtlasDesc(), ref m_Output);
                return m_Output;
            }
        }

        public TextureHandle GetShadowMapDepthTexture(RenderGraph renderGraph)
        {
            if (m_BlurAlgorithm == BlurAlgorithm.None)
                return GetOutputTexture(renderGraph);

            // We use the actual shadow map as intermediate target
            renderGraph.CreateTextureIfInvalid(GetShadowMapTextureDesc(), ref m_ShadowMapOutput);
            return m_ShadowMapOutput;
        }

        protected void InitializeRenderGraphOutput(RenderGraph renderGraph, bool useSharedTexture)
        {
            // First release if not needed anymore.
            if (m_UseSharedTexture)
            {
                Debug.Assert(useSharedTexture, "Shadow atlas can't go from shared to non-shared texture");
            }

            m_UseSharedTexture = useSharedTexture;
            // Else it's created on the fly like a regular render graph texture.
            // Also when using shared texture (for static shadows) we want to manage lifetime manually. Otherwise this would break static shadow caching.
            if (m_UseSharedTexture)
                m_Output = renderGraph.CreateSharedTexture(GetAtlasDesc(), explicitRelease: true);
        }

        internal void CleanupRenderGraphOutput(RenderGraph renderGraph)
        {
            if (m_UseSharedTexture && renderGraph != null && m_Output.IsValid())
            {
                renderGraph.ReleaseSharedTexture(m_Output);
                m_UseSharedTexture = false;
                m_Output = TextureHandle.nullHandle;
            }
        }

        public bool HasBlurredEVSM()
        {
            return (m_BlurAlgorithm == BlurAlgorithm.EVSM);
        }

        // This is a 9 tap filter, a gaussian with std. dev of 3. This standard deviation with this amount of taps probably cuts
        // the tail of the gaussian a bit too much, and it is a very fat curve, but it seems to work fine for our use case.
        static readonly Vector4[] evsmBlurWeights =
        {
            new Vector4(0.1531703f, 0.1448929f, 0.1226492f, 0.0929025f),
            new Vector4(0.06297021f, 0.0f, 0.0f, 0.0f),
        };

        class RenderShadowMapsPassData
        {
            public TextureHandle atlasTexture;

            public ShaderVariablesGlobal globalCBData;
            public ConstantBuffer<ShaderVariablesGlobal> globalCB;
            public ShadowDrawingSettings shadowDrawSettings;
            public List<HDShadowRequest> shadowRequests;
            public Material clearMaterial;
            public bool debugClearAtlas;
            public bool isRenderingOnACache;
        }

        internal TextureHandle RenderShadowMaps(RenderGraph renderGraph, CullingResults cullResults, in ShaderVariablesGlobal globalCBData, FrameSettings frameSettings, string shadowPassName)
        {
            using (var builder = renderGraph.AddRenderPass<RenderShadowMapsPassData>("Render Shadow Maps", out var passData, ProfilingSampler.Get(HDProfileId.RenderShadowMaps)))
            {
                passData.globalCBData = globalCBData;
                passData.globalCB = m_GlobalConstantBuffer;
                passData.shadowRequests = m_ShadowRequests;
                passData.clearMaterial = m_ClearMaterial;
                passData.debugClearAtlas = m_LightingDebugSettings.clearShadowAtlas;
                passData.shadowDrawSettings = new ShadowDrawingSettings(cullResults, 0);
                passData.shadowDrawSettings.useRenderingLayerMaskTest = frameSettings.IsEnabled(FrameSettingsField.LightLayers);
                passData.isRenderingOnACache = m_IsACacheForShadows;

                // Only in case of regular shadow map do we render directly in the output texture of the atlas.
                if (m_BlurAlgorithm == BlurAlgorithm.EVSM || m_BlurAlgorithm == BlurAlgorithm.IM)
                    passData.atlasTexture = builder.WriteTexture(GetShadowMapDepthTexture(renderGraph));
                else
                    passData.atlasTexture = builder.WriteTexture(GetOutputTexture(renderGraph));

                builder.SetRenderFunc(
                    (RenderShadowMapsPassData data, RenderGraphContext ctx) =>
                    {
                        ctx.cmd.SetRenderTarget(data.atlasTexture, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);

                        // Clear the whole atlas to avoid garbage outside of current request when viewing it.
                        if (data.debugClearAtlas)
                            CoreUtils.DrawFullScreen(ctx.cmd, data.clearMaterial, null, 0);

                        foreach (var shadowRequest in data.shadowRequests)
                        {
                            bool shouldSkipRequest = shadowRequest.shadowMapType != ShadowMapType.CascadedDirectional ? !shadowRequest.shouldRenderCachedComponent && data.isRenderingOnACache :
                                shadowRequest.shouldUseCachedShadowData;

                            if (shouldSkipRequest)
                                continue;

                            bool mixedInDynamicAtlas = false;
#if UNITY_2021_1_OR_NEWER
                            if (shadowRequest.isMixedCached)
                            {
                                mixedInDynamicAtlas = !data.isRenderingOnACache;
                                data.shadowDrawSettings.objectsFilter = mixedInDynamicAtlas ? ShadowObjectsFilter.DynamicOnly : ShadowObjectsFilter.StaticOnly;
                            }
                            else
                            {
                                data.shadowDrawSettings.objectsFilter = ShadowObjectsFilter.AllObjects;
                            }
#endif

                            ctx.cmd.SetGlobalDepthBias(1.0f, shadowRequest.slopeBias);
                            ctx.cmd.SetViewport(data.isRenderingOnACache ? shadowRequest.cachedAtlasViewport : shadowRequest.dynamicAtlasViewport);

                            ctx.cmd.SetGlobalFloat(HDShaderIDs._ZClip, shadowRequest.zClip ? 1.0f : 0.0f);

                            if (!mixedInDynamicAtlas)
                                CoreUtils.DrawFullScreen(ctx.cmd, data.clearMaterial, null, 0);

                            data.shadowDrawSettings.lightIndex = shadowRequest.lightIndex;
                            data.shadowDrawSettings.splitData = shadowRequest.splitData;

                            // Setup matrices for shadow rendering:
                            Matrix4x4 viewProjection = shadowRequest.deviceProjectionYFlip * shadowRequest.view;
                            data.globalCBData._ViewMatrix = shadowRequest.view;
                            data.globalCBData._InvViewMatrix = shadowRequest.view.inverse;
                            data.globalCBData._ProjMatrix = shadowRequest.deviceProjectionYFlip;
                            data.globalCBData._InvProjMatrix = shadowRequest.deviceProjectionYFlip.inverse;
                            data.globalCBData._ViewProjMatrix = viewProjection;
                            data.globalCBData._InvViewProjMatrix = viewProjection.inverse;
                            data.globalCBData._SlopeScaleDepthBias = -shadowRequest.slopeBias;
                            data.globalCBData._GlobalMipBias = 0.0f;
                            data.globalCBData._GlobalMipBiasPow2 = 1.0f;

                            data.globalCB.PushGlobal(ctx.cmd, data.globalCBData, HDShaderIDs._ShaderVariablesGlobal);

                            ctx.cmd.SetGlobalVectorArray(HDShaderIDs._ShadowFrustumPlanes, shadowRequest.frustumPlanes);

                            // TODO: remove this execute when DrawShadows will use a CommandBuffer
                            ctx.renderContext.ExecuteCommandBuffer(ctx.cmd);
                            ctx.cmd.Clear();

                            ctx.renderContext.DrawShadows(ref data.shadowDrawSettings);
                        }
                        ctx.cmd.SetGlobalFloat(HDShaderIDs._ZClip, 1.0f);   // Re-enable zclip globally
                        ctx.cmd.SetGlobalDepthBias(0.0f, 0.0f);             // Reset depth bias.
                    });

                m_ShadowMapOutput = passData.atlasTexture;
                return passData.atlasTexture;
            }
        }

        class EVSMBlurMomentsPassData
        {
            public TextureHandle atlasTexture;
            public TextureHandle momentAtlasTexture1;
            public TextureHandle momentAtlasTexture2;

            public ComputeShader evsmShadowBlurMomentsCS;
            public List<HDShadowRequest> shadowRequests;
            public bool isRenderingOnACache;
        }

        unsafe TextureHandle EVSMBlurMoments(RenderGraph renderGraph, TextureHandle inputAtlas)
        {
            using (var builder = renderGraph.AddRenderPass<EVSMBlurMomentsPassData>("EVSM Blur Moments", out var passData, ProfilingSampler.Get(HDProfileId.RenderEVSMShadowMaps)))
            {
                passData.evsmShadowBlurMomentsCS = m_RenderPipelineResources.shaders.evsmBlurCS;
                passData.shadowRequests = m_ShadowRequests;
                passData.isRenderingOnACache = m_IsACacheForShadows;
                passData.atlasTexture = builder.ReadTexture(inputAtlas);
                passData.momentAtlasTexture1 = builder.WriteTexture(GetOutputTexture(renderGraph));
                passData.momentAtlasTexture2 = builder.WriteTexture(renderGraph.CreateTexture(GetMomentAtlasDesc(m_MomentCopyName)));

                builder.SetRenderFunc(
                    (EVSMBlurMomentsPassData data, RenderGraphContext ctx) =>
                    {
                        ComputeShader shadowBlurMomentsCS = data.evsmShadowBlurMomentsCS;
                        RTHandle[] momentAtlasRenderTextures = ctx.renderGraphPool.GetTempArray<RTHandle>(2);
                        momentAtlasRenderTextures[0] = data.momentAtlasTexture1;
                        momentAtlasRenderTextures[1] = data.momentAtlasTexture2;

                        int generateAndBlurMomentsKernel = shadowBlurMomentsCS.FindKernel("ConvertAndBlur");
                        int blurMomentsKernel = shadowBlurMomentsCS.FindKernel("Blur");
                        int copyMomentsKernel = shadowBlurMomentsCS.FindKernel("CopyMoments");

                        RTHandle atlasRenderTexture = data.atlasTexture;

                        ctx.cmd.SetComputeTextureParam(shadowBlurMomentsCS, generateAndBlurMomentsKernel, HDShaderIDs._DepthTexture, atlasRenderTexture);
                        ctx.cmd.SetComputeVectorArrayParam(shadowBlurMomentsCS, HDShaderIDs._BlurWeightsStorage, evsmBlurWeights);

                        // We need to store in which of the two moment texture a request will have its last version stored in for a final patch up at the end.
                        var finalAtlasTexture = stackalloc int[data.shadowRequests.Count];

                        int requestIdx = 0;
                        foreach (var shadowRequest in data.shadowRequests)
                        {
                            bool shouldSkipRequest = shadowRequest.shadowMapType != ShadowMapType.CascadedDirectional ? !shadowRequest.shouldRenderCachedComponent && data.isRenderingOnACache :
                                shadowRequest.shouldUseCachedShadowData;

                            if (shouldSkipRequest)
                                continue;

                            var viewport = data.isRenderingOnACache ? shadowRequest.cachedAtlasViewport : shadowRequest.dynamicAtlasViewport;

                            using (new ProfilingScope(ctx.cmd, ProfilingSampler.Get(HDProfileId.RenderEVSMShadowMapsBlur)))
                            {
                                int downsampledWidth = Mathf.CeilToInt(viewport.width * 0.5f);
                                int downsampledHeight = Mathf.CeilToInt(viewport.height * 0.5f);

                                Vector2 DstRectOffset = new Vector2(viewport.min.x * 0.5f, viewport.min.y * 0.5f);

                                ctx.cmd.SetComputeTextureParam(shadowBlurMomentsCS, generateAndBlurMomentsKernel, HDShaderIDs._OutputTexture, momentAtlasRenderTextures[0]);
                                ctx.cmd.SetComputeVectorParam(shadowBlurMomentsCS, HDShaderIDs._SrcRect, new Vector4(viewport.min.x, viewport.min.y, viewport.width, viewport.height));
                                ctx.cmd.SetComputeVectorParam(shadowBlurMomentsCS, HDShaderIDs._DstRect, new Vector4(DstRectOffset.x, DstRectOffset.y, 1.0f / atlasRenderTexture.rt.width, 1.0f / atlasRenderTexture.rt.height));
                                ctx.cmd.SetComputeFloatParam(shadowBlurMomentsCS, HDShaderIDs._EVSMExponent, shadowRequest.evsmParams.x);

                                int dispatchSizeX = ((int)downsampledWidth + 7) / 8;
                                int dispatchSizeY = ((int)downsampledHeight + 7) / 8;

                                ctx.cmd.DispatchCompute(shadowBlurMomentsCS, generateAndBlurMomentsKernel, dispatchSizeX, dispatchSizeY, 1);

                                int currentAtlasMomentSurface = 0;

                                RTHandle GetMomentRT() { return momentAtlasRenderTextures[currentAtlasMomentSurface]; }
                                RTHandle GetMomentRTCopy() { return momentAtlasRenderTextures[(currentAtlasMomentSurface + 1) & 1]; }

                                ctx.cmd.SetComputeVectorParam(shadowBlurMomentsCS, HDShaderIDs._SrcRect, new Vector4(DstRectOffset.x, DstRectOffset.y, downsampledWidth, downsampledHeight));
                                for (int i = 0; i < shadowRequest.evsmParams.w; ++i)
                                {
                                    currentAtlasMomentSurface = (currentAtlasMomentSurface + 1) & 1;
                                    ctx.cmd.SetComputeTextureParam(shadowBlurMomentsCS, blurMomentsKernel, HDShaderIDs._InputTexture, GetMomentRTCopy());
                                    ctx.cmd.SetComputeTextureParam(shadowBlurMomentsCS, blurMomentsKernel, HDShaderIDs._OutputTexture, GetMomentRT());

                                    ctx.cmd.DispatchCompute(shadowBlurMomentsCS, blurMomentsKernel, dispatchSizeX, dispatchSizeY, 1);
                                }

                                finalAtlasTexture[requestIdx++] = currentAtlasMomentSurface;
                            }
                        }

                        // We patch up the atlas with the requests that, due to different count of blur passes, remained in the copy
                        for (int i = 0; i < data.shadowRequests.Count; ++i)
                        {
                            if (finalAtlasTexture[i] != 0)
                            {
                                using (new ProfilingScope(ctx.cmd, ProfilingSampler.Get(HDProfileId.RenderEVSMShadowMapsCopyToAtlas)))
                                {
                                    var shadowRequest = data.shadowRequests[i];
                                    var viewport = data.isRenderingOnACache ? shadowRequest.cachedAtlasViewport : shadowRequest.dynamicAtlasViewport;
                                    int downsampledWidth = Mathf.CeilToInt(viewport.width * 0.5f);
                                    int downsampledHeight = Mathf.CeilToInt(viewport.height * 0.5f);

                                    ctx.cmd.SetComputeVectorParam(shadowBlurMomentsCS, HDShaderIDs._SrcRect, new Vector4(viewport.min.x * 0.5f, viewport.min.y * 0.5f, downsampledWidth, downsampledHeight));
                                    ctx.cmd.SetComputeTextureParam(shadowBlurMomentsCS, copyMomentsKernel, HDShaderIDs._InputTexture, momentAtlasRenderTextures[1]);
                                    ctx.cmd.SetComputeTextureParam(shadowBlurMomentsCS, copyMomentsKernel, HDShaderIDs._OutputTexture, momentAtlasRenderTextures[0]);

                                    int dispatchSizeX = ((int)downsampledWidth + 7) / 8;
                                    int dispatchSizeY = ((int)downsampledHeight + 7) / 8;

                                    ctx.cmd.DispatchCompute(shadowBlurMomentsCS, copyMomentsKernel, dispatchSizeX, dispatchSizeY, 1);
                                }
                            }
                        }
                    });

                return passData.momentAtlasTexture1;
            }
        }

        class IMBlurMomentPassData
        {
            public TextureHandle atlasTexture;
            public TextureHandle momentAtlasTexture;
            public TextureHandle intermediateSummedAreaTexture;
            public TextureHandle summedAreaTexture;

            public List<HDShadowRequest> shadowRequests;
            public ComputeShader imShadowBlurMomentsCS;
            public bool isRenderingOnACache;
        }

        TextureHandle IMBlurMoment(RenderGraph renderGraph, TextureHandle atlasTexture)
        {
            using (var builder = renderGraph.AddRenderPass<IMBlurMomentPassData>("EVSM Blur Moments", out var passData, ProfilingSampler.Get(HDProfileId.RenderMomentShadowMaps)))
            {
                passData.shadowRequests = m_ShadowRequests;
                passData.isRenderingOnACache = m_IsACacheForShadows;
                passData.imShadowBlurMomentsCS = m_RenderPipelineResources.shaders.momentShadowsCS;
                passData.atlasTexture = builder.ReadTexture(atlasTexture);
                passData.momentAtlasTexture = builder.WriteTexture(GetOutputTexture(renderGraph));
                passData.intermediateSummedAreaTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(width, height)
                { colorFormat = GraphicsFormat.R32G32B32A32_SInt, name = m_IntermediateSummedAreaName, enableRandomWrite = true, clearBuffer = true, clearColor = Color.black }));
                passData.summedAreaTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(width, height)
                { colorFormat = GraphicsFormat.R32G32B32A32_SInt, name = m_SummedAreaName, enableRandomWrite = true, clearColor = Color.black }));

                builder.SetRenderFunc(
                    (IMBlurMomentPassData data, RenderGraphContext ctx) =>
                    {
                        // If the target kernel is not available
                        ComputeShader momentCS = data.imShadowBlurMomentsCS;
                        if (momentCS == null) return;

                        int computeMomentKernel = momentCS.FindKernel("ComputeMomentShadows");
                        int summedAreaHorizontalKernel = momentCS.FindKernel("MomentSummedAreaTableHorizontal");
                        int summedAreaVerticalKernel = momentCS.FindKernel("MomentSummedAreaTableVertical");

                        RTHandle atlas = data.atlasTexture;
                        RTHandle atlasMoment = data.momentAtlasTexture;
                        RTHandle intermediateSummedAreaTexture = data.intermediateSummedAreaTexture;
                        RTHandle summedAreaTexture = data.summedAreaTexture;

                        // Alright, so the thing here is that for every sub-shadow map of the atlas, we need to generate the moment shadow map
                        foreach (var shadowRequest in data.shadowRequests)
                        {
                            // Let's bind the resources of this
                            ctx.cmd.SetComputeTextureParam(momentCS, computeMomentKernel, HDShaderIDs._ShadowmapAtlas, atlas);
                            ctx.cmd.SetComputeTextureParam(momentCS, computeMomentKernel, HDShaderIDs._MomentShadowAtlas, atlasMoment);
                            ctx.cmd.SetComputeVectorParam(momentCS, HDShaderIDs._MomentShadowmapSlotST, new Vector4(shadowRequest.dynamicAtlasViewport.width, shadowRequest.dynamicAtlasViewport.height, shadowRequest.dynamicAtlasViewport.min.x, shadowRequest.dynamicAtlasViewport.min.y));

                            // First of all we need to compute the moments
                            int numTilesX = Math.Max((int)shadowRequest.dynamicAtlasViewport.width / 8, 1);
                            int numTilesY = Math.Max((int)shadowRequest.dynamicAtlasViewport.height / 8, 1);
                            ctx.cmd.DispatchCompute(momentCS, computeMomentKernel, numTilesX, numTilesY, 1);

                            // Do the horizontal pass of the summed area table
                            ctx.cmd.SetComputeTextureParam(momentCS, summedAreaHorizontalKernel, HDShaderIDs._SummedAreaTableInputFloat, atlasMoment);
                            ctx.cmd.SetComputeTextureParam(momentCS, summedAreaHorizontalKernel, HDShaderIDs._SummedAreaTableOutputInt, intermediateSummedAreaTexture);
                            ctx.cmd.SetComputeFloatParam(momentCS, HDShaderIDs._IMSKernelSize, shadowRequest.kernelSize);
                            ctx.cmd.SetComputeVectorParam(momentCS, HDShaderIDs._MomentShadowmapSize, new Vector2((float)atlasMoment.referenceSize.x, (float)atlasMoment.referenceSize.y));

                            int numLines = Math.Max((int)shadowRequest.dynamicAtlasViewport.width / 64, 1);
                            ctx.cmd.DispatchCompute(momentCS, summedAreaHorizontalKernel, numLines, 1, 1);

                            // Do the horizontal pass of the summed area table
                            ctx.cmd.SetComputeTextureParam(momentCS, summedAreaVerticalKernel, HDShaderIDs._SummedAreaTableInputInt, intermediateSummedAreaTexture);
                            ctx.cmd.SetComputeTextureParam(momentCS, summedAreaVerticalKernel, HDShaderIDs._SummedAreaTableOutputInt, summedAreaTexture);
                            ctx.cmd.SetComputeVectorParam(momentCS, HDShaderIDs._MomentShadowmapSize, new Vector2((float)atlasMoment.referenceSize.x, (float)atlasMoment.referenceSize.y));
                            ctx.cmd.SetComputeFloatParam(momentCS, HDShaderIDs._IMSKernelSize, shadowRequest.kernelSize);

                            int numColumns = Math.Max((int)shadowRequest.dynamicAtlasViewport.height / 64, 1);
                            ctx.cmd.DispatchCompute(momentCS, summedAreaVerticalKernel, numColumns, 1, 1);

                            // Push the global texture
                            ctx.cmd.SetGlobalTexture(HDShaderIDs._SummedAreaTableInputInt, summedAreaTexture);
                        }
                    });

                return passData.momentAtlasTexture;
            }
        }

        internal TextureHandle BlurShadows(RenderGraph renderGraph)
        {
            if (m_ShadowRequests.Count == 0)
            {
                return renderGraph.defaultResources.whiteTexture;
            }

            if (m_BlurAlgorithm == BlurAlgorithm.EVSM)
            {
                return EVSMBlurMoments(renderGraph, m_ShadowMapOutput);
            }
            else if (m_BlurAlgorithm == BlurAlgorithm.IM)
            {
                return IMBlurMoment(renderGraph, m_ShadowMapOutput);
            }
            else // Regular shadow maps.
            {
                return m_ShadowMapOutput;
            }
        }

        internal TextureHandle RenderShadows(RenderGraph renderGraph, CullingResults cullResults, in ShaderVariablesGlobal globalCB, FrameSettings frameSettings, string shadowPassName)
        {
            if (m_ShadowRequests.Count == 0)
            {
                return renderGraph.defaultResources.defaultShadowTexture;
            }

            RenderShadowMaps(renderGraph, cullResults, globalCB, frameSettings, shadowPassName);

            if (m_BlurAlgorithm == BlurAlgorithm.EVSM)
            {
                return EVSMBlurMoments(renderGraph, m_ShadowMapOutput);
            }
            else if (m_BlurAlgorithm == BlurAlgorithm.IM)
            {
                return IMBlurMoment(renderGraph, m_ShadowMapOutput);
            }
            else // Regular shadow maps.
            {
                return m_ShadowMapOutput;
            }
        }

        public virtual void DisplayAtlas(RTHandle atlasTexture, CommandBuffer cmd, Material debugMaterial, Rect atlasViewport, float screenX, float screenY, float screenSizeX, float screenSizeY, float minValue, float maxValue, MaterialPropertyBlock mpb, float scaleFactor = 1)
        {
            if (atlasTexture == null)
                return;

            Vector4 validRange = new Vector4(minValue, 1.0f / (maxValue - minValue));
            float rWidth = 1.0f / width;
            float rHeight = 1.0f / height;
            Vector4 scaleBias = Vector4.Scale(new Vector4(rWidth, rHeight, rWidth, rHeight), new Vector4(atlasViewport.width, atlasViewport.height, atlasViewport.x, atlasViewport.y));

            mpb.SetTexture("_AtlasTexture", atlasTexture);
            mpb.SetVector("_TextureScaleBias", scaleBias);
            mpb.SetVector("_ValidRange", validRange);
            mpb.SetFloat("_RcpGlobalScaleFactor", scaleFactor);
            cmd.SetViewport(new Rect(screenX, screenY, screenSizeX, screenSizeY));
            cmd.DrawProcedural(Matrix4x4.identity, debugMaterial, debugMaterial.FindPass("RegularShadow"), MeshTopology.Triangles, 3, 1, mpb);
        }

        public virtual void Clear()
        {
            m_ShadowRequests.Clear();
        }

        public void Release(RenderGraph renderGraph)
        {
            CleanupRenderGraphOutput(renderGraph);
        }
    }
}
