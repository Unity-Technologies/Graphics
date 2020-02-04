using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        Material m_DepthResolveMaterial;
        // Need to cache to avoid alloc of arrays...
        GBufferOutput m_GBufferOutput;
        DBufferOutput m_DBufferOutput;

        HDUtils.PackedMipChainInfo m_DepthBufferMipChainInfo;

        Vector2Int ComputeDepthBufferMipChainSize(Vector2Int screenSize)
        {
            m_DepthBufferMipChainInfo.ComputePackedMipChainInfo(screenSize);
            return m_DepthBufferMipChainInfo.textureSize;
        }

        void InitializePrepass(HDRenderPipelineAsset hdAsset)
        {
            m_DepthResolveMaterial = CoreUtils.CreateEngineMaterial(asset.renderPipelineResources.shaders.depthValuesPS);

            m_GBufferOutput = new GBufferOutput();
            m_GBufferOutput.mrt = new RenderGraphResource[RenderGraph.kMaxMRTCount];

            m_DBufferOutput = new DBufferOutput();
            m_DBufferOutput.mrt = new RenderGraphResource[(int)Decal.DBufferMaterial.Count];

            m_DepthBufferMipChainInfo = new HDUtils.PackedMipChainInfo();
            m_DepthBufferMipChainInfo.Allocate();
        }

        void CleanupPrepass()
        {
            CoreUtils.Destroy(m_DepthResolveMaterial);
        }

        bool NeedClearGBuffer()
        {
            // TODO: Add an option to force clear
            return m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled();
        }

        HDUtils.PackedMipChainInfo GetDepthBufferMipChainInfo()
        {
            return m_DepthBufferMipChainInfo;
        }


        struct PrepassOutput
        {
            // Buffers that may be output by the prepass.
            // They will be MSAA depending on the frame settings
            public RenderGraphMutableResource   depthBuffer;
            public RenderGraphMutableResource   depthAsColor;
            public RenderGraphMutableResource   normalBuffer;
            public RenderGraphMutableResource   motionVectorsBuffer;

            // GBuffer output. Will also contain a reference to the normal buffer (as it is shared between deferred and forward objects)
            public GBufferOutput                gbuffer;

            public DBufferOutput                dbuffer;

            // Additional buffers only for MSAA
            public RenderGraphMutableResource   depthValuesMSAA;

            // Resolved buffers for MSAA. When MSAA is off, they will be the same reference as the buffers above.
            public RenderGraphMutableResource   resolvedDepthBuffer;
            public RenderGraphMutableResource   resolvedNormalBuffer;
            public RenderGraphMutableResource   resolvedMotionVectorsBuffer;

            // Copy of the resolved depth buffer with mip chain
            public RenderGraphMutableResource   depthPyramidTexture;

            public RenderGraphResource          stencilBuffer;
        }

        RenderGraphMutableResource CreateDepthBuffer(RenderGraph renderGraph, bool msaa)
        {
            TextureDesc depthDesc = new TextureDesc(Vector2.one, true, true)
                { depthBufferBits = DepthBits.Depth32, bindTextureMS = msaa, enableMSAA = msaa, clearBuffer = true, name = msaa ? "CameraDepthStencilMSAA" : "CameraDepthStencil" };

            return renderGraph.CreateTexture(depthDesc);
        }

        RenderGraphMutableResource CreateNormalBuffer(RenderGraph renderGraph, bool msaa)
        {
            TextureDesc normalDesc = new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, clearBuffer = NeedClearGBuffer(), clearColor = Color.black, bindTextureMS = msaa, enableMSAA = msaa, enableRandomWrite = !msaa, name = msaa ? "NormalBufferMSAA" : "NormalBuffer" };
            return renderGraph.CreateTexture(normalDesc, msaa ? HDShaderIDs._NormalTextureMS : HDShaderIDs._NormalBufferTexture);
        }

        RenderGraphMutableResource CreateMotionVectorBuffer(RenderGraph renderGraph, bool msaa, bool clear)
        {
            TextureDesc motionVectorDesc = new TextureDesc(Vector2.one, true, true)
                { colorFormat = Builtin.GetMotionVectorFormat(), bindTextureMS = msaa, enableMSAA = msaa, clearBuffer = clear, clearColor = Color.clear, name = msaa ? "Motion Vectors MSAA" : "Motion Vectors" };
            return renderGraph.CreateTexture(motionVectorDesc, HDShaderIDs._CameraMotionVectorsTexture);
        }

        PrepassOutput RenderPrepass(RenderGraph renderGraph, RenderGraphMutableResource sssBuffer, CullingResults cullingResults, HDCamera hdCamera)
        {
            m_IsDepthBufferCopyValid = false;

            var result = new PrepassOutput();
            result.gbuffer = m_GBufferOutput;
            result.dbuffer = m_DBufferOutput;

            bool msaa = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA);
            bool clearMotionVectors = hdCamera.camera.cameraType == CameraType.SceneView && !hdCamera.animateMaterials;


            // TODO: See how to clean this. Some buffers are created outside, some inside functions...
            result.motionVectorsBuffer = CreateMotionVectorBuffer(renderGraph, msaa, clearMotionVectors);
            result.depthBuffer = CreateDepthBuffer(renderGraph, msaa);

            RenderXROcclusionMeshes(renderGraph, hdCamera, result.depthBuffer);

            using (new XRSinglePassScope(renderGraph, hdCamera))
            {
                // TODO RENDERGRAPH
                //// Bind the custom color/depth before the first custom pass
                //if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.CustomPass))
                //{
                //    if (m_CustomPassColorBuffer.IsValueCreated)
                //        cmd.SetGlobalTexture(HDShaderIDs._CustomColorTexture, m_CustomPassColorBuffer.Value);
                //    if (m_CustomPassDepthBuffer.IsValueCreated)
                //        cmd.SetGlobalTexture(HDShaderIDs._CustomDepthTexture, m_CustomPassDepthBuffer.Value);
                //}
                //RenderCustomPass(renderContext, cmd, hdCamera, customPassCullingResults, CustomPassInjectionPoint.BeforeRendering);

                bool renderMotionVectorAfterGBuffer = RenderDepthPrepass(renderGraph, cullingResults, hdCamera, ref result);

                if (!renderMotionVectorAfterGBuffer)
                {
                    // If objects motion vectors are enabled, this will render the objects with motion vector into the target buffers (in addition to the depth)
                    // Note: An object with motion vector must not be render in the prepass otherwise we can have motion vector write that should have been rejected
                    RenderObjectsMotionVectors(renderGraph, cullingResults, hdCamera, result);
                }

                // TODO RENDERGRAPH
                //PreRenderSky(hdCamera, cmd);

                // At this point in forward all objects have been rendered to the prepass (depth/normal/motion vectors) so we can resolve them
                ResolvePrepassBuffers(renderGraph, hdCamera, ref result);

                RenderDBuffer(renderGraph, hdCamera, ref result, cullingResults);

                RenderGBuffer(renderGraph, sssBuffer, ref result, cullingResults, hdCamera);

                DecalNormalPatch(renderGraph, hdCamera, ref result);

                // TODO RENDERGRAPH
                //// After Depth and Normals/roughness including decals
                //RenderCustomPass(renderContext, cmd, hdCamera, customPassCullingResults, CustomPassInjectionPoint.AfterOpaqueDepthAndNormal);

                // In both forward and deferred, everything opaque should have been rendered at this point so we can safely copy the depth buffer for later processing.
                GenerateDepthPyramid(renderGraph, hdCamera, ref result);

                // TODO RENDERGRAPH
                //// Send all the geometry graphics buffer to client systems if required (must be done after the pyramid and before the transparent depth pre-pass)
                //SendGeometryGraphicsBuffers(cmd, hdCamera);

                if (renderMotionVectorAfterGBuffer)
                {
                    // See the call RenderObjectsMotionVectors() above and comment
                    RenderObjectsMotionVectors(renderGraph, cullingResults, hdCamera, result);
                }

                RenderCameraMotionVectors(renderGraph, hdCamera, result.depthPyramidTexture, result.resolvedMotionVectorsBuffer);

                // TODO RENDERGRAPH
                //RenderTransparencyOverdraw(cullingResults, hdCamera, renderContext, cmd);

                ResolveStencilBufferIfNeeded(renderGraph, hdCamera, ref result);
            }

            return result;
        }

        class DepthPrepassData
        {
            public FrameSettings frameSettings;
            public bool msaaEnabled;
            public bool hasDepthOnlyPrepass;
            public bool renderRayTracingPrepass;

            public RenderGraphMutableResource depthBuffer;
            public RenderGraphMutableResource depthAsColorBuffer;
            public RenderGraphMutableResource normalBuffer;

            public RenderGraphResource rendererListMRT;
            public RenderGraphResource rendererListDepthOnly;

            public RenderGraphResource renderListRayTracingOpaque;
            public RenderGraphResource renderListRayTracingTransparent;
        }

        // RenderDepthPrepass render both opaque and opaque alpha tested based on engine configuration.
        // Lit Forward only: We always render all materials
        // Lit Deferred: We always render depth prepass for alpha tested (optimization), other deferred material are render based on engine configuration.
        // Forward opaque with deferred renderer (DepthForwardOnly pass): We always render all materials
        // True is returned if motion vector must be rendered after GBuffer pass
        bool RenderDepthPrepass(RenderGraph renderGraph, CullingResults cull, HDCamera hdCamera, ref PrepassOutput output)
        {
            var depthPrepassParameters = PrepareDepthPrepass(cull, hdCamera);

            bool msaa = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA);

            using (var builder = renderGraph.AddRenderPass<DepthPrepassData>(depthPrepassParameters.passName, out var passData, ProfilingSampler.Get(depthPrepassParameters.profilingId)))
            {
                passData.frameSettings = hdCamera.frameSettings;
                passData.msaaEnabled = msaa;
                passData.hasDepthOnlyPrepass = depthPrepassParameters.hasDepthOnlyPass;
                passData.renderRayTracingPrepass = depthPrepassParameters.renderRayTracingPrepass;

                passData.depthBuffer = builder.UseDepthBuffer(output.depthBuffer, DepthAccess.ReadWrite);
                passData.normalBuffer = builder.WriteTexture(CreateNormalBuffer(renderGraph, msaa));
                // This texture must be used because reading directly from an MSAA Depth buffer is way to expensive.
                // The solution that we went for is writing the depth in an additional color buffer (10x cheaper to solve on ps4)
                if (msaa)
                {
                    passData.depthAsColorBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                        { colorFormat = GraphicsFormat.R32_SFloat, clearBuffer = true, clearColor = Color.black, bindTextureMS = true, enableMSAA = true, name = "DepthAsColorMSAA" }, HDShaderIDs._DepthTextureMS));
                }

                if (passData.hasDepthOnlyPrepass)
                {
                    passData.rendererListDepthOnly = builder.UseRendererList(renderGraph.CreateRendererList(depthPrepassParameters.depthOnlyRendererListDesc));
                }

                passData.rendererListMRT = builder.UseRendererList(renderGraph.CreateRendererList(depthPrepassParameters.mrtRendererListDesc));

                if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing))
                {
                    passData.renderListRayTracingOpaque = builder.UseRendererList(renderGraph.CreateRendererList(depthPrepassParameters.rayTracingOpaqueRLDesc));
                    passData.renderListRayTracingTransparent = builder.UseRendererList(renderGraph.CreateRendererList(depthPrepassParameters.rayTracingTransparentRLDesc));
                }

                output.depthBuffer = passData.depthBuffer;
                output.depthAsColor = passData.depthAsColorBuffer;
                output.normalBuffer = passData.normalBuffer;

                builder.SetRenderFunc(
                (DepthPrepassData data, RenderGraphContext context) =>
                {
                    var mrt = context.renderGraphPool.GetTempArray<RenderTargetIdentifier>(data.msaaEnabled ? 2 : 1);
                    mrt[0] = context.resources.GetTexture(data.normalBuffer);
                    if (data.msaaEnabled)
                        mrt[1] = context.resources.GetTexture(data.depthAsColorBuffer);

                    bool useRayTracing = passData.frameSettings.IsEnabled(FrameSettingsField.RayTracing);

                    RenderDepthPrepass(context.renderContext, context.cmd, data.frameSettings
                                    , mrt
                                    , context.resources.GetTexture(data.depthBuffer)
                                    , data.hasDepthOnlyPrepass ? context.resources.GetRendererList(data.rendererListDepthOnly) : RendererList.nullRendererList
                                    , context.resources.GetRendererList(data.rendererListMRT)
                                    , data.hasDepthOnlyPrepass
                                    , useRayTracing ? context.resources.GetRendererList(data.renderListRayTracingOpaque) : new RendererList()
                                    , useRayTracing ? context.resources.GetRendererList(data.renderListRayTracingTransparent) : new RendererList()
                                    , data.renderRayTracingPrepass
                                    );
                });
            }

            return depthPrepassParameters.shouldRenderMotionVectorAfterGBuffer;
        }

        class ObjectMotionVectorsPassData
        {
            public FrameSettings                frameSettings;
            public RenderGraphMutableResource   depthBuffer;
            public RenderGraphMutableResource   motionVectorsBuffer;
            public RenderGraphMutableResource   normalBuffer;
            public RenderGraphMutableResource   depthAsColorMSAABuffer;
            public RenderGraphResource          rendererList;
        }

        void RenderObjectsMotionVectors(RenderGraph renderGraph, CullingResults cull, HDCamera hdCamera, in PrepassOutput output)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.ObjectMotionVectors))
                return;

            using (var builder = renderGraph.AddRenderPass<ObjectMotionVectorsPassData>("Objects Motion Vectors Rendering", out var passData, ProfilingSampler.Get(HDProfileId.ObjectsMotionVector)))
            {
                // These flags are still required in SRP or the engine won't compute previous model matrices...
                // If the flag hasn't been set yet on this camera, motion vectors will skip a frame.
                hdCamera.camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;

                bool msaa = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA);

                passData.frameSettings = hdCamera.frameSettings;
                passData.depthBuffer = builder.UseDepthBuffer(output.depthBuffer, DepthAccess.ReadWrite);
                passData.motionVectorsBuffer = builder.UseColorBuffer(output.motionVectorsBuffer, 0);
                passData.normalBuffer = builder.UseColorBuffer(output.normalBuffer, 1);
                if (msaa)
                    passData.depthAsColorMSAABuffer = builder.UseColorBuffer(output.depthAsColor, 2);

                passData.rendererList = builder.UseRendererList(
                    renderGraph.CreateRendererList(CreateOpaqueRendererListDesc(cull, hdCamera.camera, HDShaderPassNames.s_MotionVectorsName, PerObjectData.MotionVectors)));

                builder.SetRenderFunc(
                (ObjectMotionVectorsPassData data, RenderGraphContext context) =>
                {
                    DrawOpaqueRendererList(context, data.frameSettings, context.resources.GetRendererList(data.rendererList));
                });
            }
        }

        class GBufferPassData
        {
            public FrameSettings                frameSettings;
            public RenderGraphResource          rendererList;
            public RenderGraphMutableResource[] gbufferRT = new RenderGraphMutableResource[RenderGraph.kMaxMRTCount];
            public RenderGraphMutableResource   depthBuffer;
        }

        struct GBufferOutput
        {
            public RenderGraphResource[] mrt;
            public int gBufferCount;
        }

        void SetupGBufferTargets(RenderGraph renderGraph, HDCamera hdCamera, GBufferPassData passData, RenderGraphMutableResource sssBuffer, ref PrepassOutput prepassOutput, FrameSettings frameSettings, RenderGraphBuilder builder)
        {
            bool clearGBuffer = NeedClearGBuffer();
            bool lightLayers = frameSettings.IsEnabled(FrameSettingsField.LightLayers);
            bool shadowMasks = frameSettings.IsEnabled(FrameSettingsField.Shadowmask);

            passData.depthBuffer = builder.UseDepthBuffer(prepassOutput.depthBuffer, DepthAccess.ReadWrite);
            passData.gbufferRT[0] = builder.UseColorBuffer(sssBuffer, 0);
            passData.gbufferRT[1] = builder.UseColorBuffer(prepassOutput.normalBuffer, 1);
            // If we are in deferred mode and the SSR is enabled, we need to make sure that the second gbuffer is cleared given that we are using that information for clear coat selection
            bool clearGBuffer2 = clearGBuffer || hdCamera.frameSettings.IsEnabled(FrameSettingsField.SSR);
            passData.gbufferRT[2] = builder.UseColorBuffer(renderGraph.CreateTexture(
                new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, clearBuffer = clearGBuffer2, clearColor = Color.clear, name = "GBuffer2" }, HDShaderIDs._GBufferTexture[2]), 2);
            passData.gbufferRT[3] = builder.UseColorBuffer(renderGraph.CreateTexture(
                new TextureDesc(Vector2.one, true, true) { colorFormat = Builtin.GetLightingBufferFormat(), clearBuffer = clearGBuffer, clearColor = Color.clear, name = "GBuffer3" }, HDShaderIDs._GBufferTexture[3]), 3);

            int currentIndex = 4;
            if (lightLayers)
            {
                passData.gbufferRT[currentIndex] = builder.UseColorBuffer(renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, clearBuffer = clearGBuffer, clearColor = Color.clear, name = "LightLayers" }, HDShaderIDs._LightLayersTexture), currentIndex);
                currentIndex++;
            }
            if (shadowMasks)
            {
                passData.gbufferRT[currentIndex] = builder.UseColorBuffer(renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one, true, true) { colorFormat = Builtin.GetShadowMaskBufferFormat(), clearBuffer = clearGBuffer, clearColor = Color.clear, name = "ShadowMasks" }, HDShaderIDs._ShadowMaskTexture), currentIndex);
                currentIndex++;
            }

            prepassOutput.gbuffer.gBufferCount = currentIndex;
            for (int i = 0; i < currentIndex; ++i)
            {
                prepassOutput.gbuffer.mrt[i] = passData.gbufferRT[i];
            }
        }

        // RenderGBuffer do the gbuffer pass. This is only called with deferred. If we use a depth prepass, then the depth prepass will perform the alpha testing for opaque alpha tested and we don't need to do it anymore
        // during Gbuffer pass. This is handled in the shader and the depth test (equal and no depth write) is done here.
        void RenderGBuffer(RenderGraph renderGraph, RenderGraphMutableResource sssBuffer, ref PrepassOutput prepassOutput, CullingResults cull, HDCamera hdCamera)
        {
            if (hdCamera.frameSettings.litShaderMode != LitShaderMode.Deferred)
            {
                prepassOutput.gbuffer.gBufferCount = 0;
                return;
            }

            using (var builder = renderGraph.AddRenderPass<GBufferPassData>("GBuffer", out var passData, ProfilingSampler.Get(HDProfileId.GBuffer)))
            {
                FrameSettings frameSettings = hdCamera.frameSettings;

                passData.frameSettings = frameSettings;
                SetupGBufferTargets(renderGraph, hdCamera, passData, sssBuffer, ref prepassOutput, frameSettings, builder);
                passData.rendererList = builder.UseRendererList(
                    renderGraph.CreateRendererList(CreateOpaqueRendererListDesc(cull, hdCamera.camera, HDShaderPassNames.s_GBufferName, m_CurrentRendererConfigurationBakedLighting)));

                ReadDBuffer(prepassOutput.dbuffer, builder);

                builder.SetRenderFunc(
                (GBufferPassData data, RenderGraphContext context) =>
                {
                    DrawOpaqueRendererList(context, data.frameSettings, context.resources.GetRendererList(data.rendererList));
                });
            }
        }

        class ResolvePrepassData
        {
            public RenderGraphMutableResource   depthBuffer;
            public RenderGraphMutableResource   depthValuesBuffer;
            public RenderGraphMutableResource   normalBuffer;
            public RenderGraphMutableResource   motionVectorsBuffer;
            public RenderGraphResource          depthAsColorBufferMSAA;
            public RenderGraphResource          normalBufferMSAA;
            public RenderGraphResource          motionVectorBufferMSAA;
            public Material                     depthResolveMaterial;
            public int                          depthResolvePassIndex;
        }

        void ResolvePrepassBuffers(RenderGraph renderGraph, HDCamera hdCamera, ref PrepassOutput output)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA))
            {
                output.resolvedNormalBuffer = output.normalBuffer;
                output.resolvedDepthBuffer = output.depthBuffer;
                output.resolvedMotionVectorsBuffer = output.motionVectorsBuffer;

                return;
            }

            using (var builder = renderGraph.AddRenderPass<ResolvePrepassData>("Resolve Prepass MSAA", out var passData))
            {
                // This texture stores a set of depth values that are required for evaluating a bunch of effects in MSAA mode (R = Samples Max Depth, G = Samples Min Depth, G =  Samples Average Depth)
                RenderGraphMutableResource depthValuesBuffer = renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R32G32B32A32_SFloat, name = "DepthValuesBuffer" });

                passData.depthResolveMaterial = m_DepthResolveMaterial;
                passData.depthResolvePassIndex = SampleCountToPassIndex(m_MSAASamples);

                passData.depthBuffer = builder.UseDepthBuffer(CreateDepthBuffer(renderGraph, false), DepthAccess.Write);
                passData.depthValuesBuffer = builder.UseColorBuffer(depthValuesBuffer, 0);
                passData.normalBuffer = builder.UseColorBuffer(CreateNormalBuffer(renderGraph, false), 1);
                passData.motionVectorsBuffer = builder.UseColorBuffer(CreateMotionVectorBuffer(renderGraph, false, false), 2);

                passData.normalBufferMSAA = builder.ReadTexture(output.normalBuffer);
                passData.depthAsColorBufferMSAA = builder.ReadTexture(output.depthAsColor);
                passData.motionVectorBufferMSAA = builder.ReadTexture(output.motionVectorsBuffer);

                output.resolvedNormalBuffer = passData.normalBuffer;
                output.resolvedDepthBuffer = passData.depthBuffer;
                output.resolvedMotionVectorsBuffer = passData.motionVectorsBuffer;
                output.depthValuesMSAA = passData.depthValuesBuffer;

                builder.SetRenderFunc(
                (ResolvePrepassData data, RenderGraphContext context) =>
                {
                    context.cmd.DrawProcedural(Matrix4x4.identity, data.depthResolveMaterial, data.depthResolvePassIndex, MeshTopology.Triangles, 3, 1);
                });
            }
        }

        class CopyDepthPassData
        {
            public RenderGraphResource          inputDepth;
            public RenderGraphMutableResource   outputDepth;
            public GPUCopy                      GPUCopy;
            public int                          width;
            public int                          height;
        }

        void CopyDepthBufferIfNeeded(RenderGraph renderGraph, HDCamera hdCamera, ref PrepassOutput output)
        {
            if (!m_IsDepthBufferCopyValid)
            {
                using (var builder = renderGraph.AddRenderPass<CopyDepthPassData>("Copy depth buffer", out var passData, ProfilingSampler.Get(HDProfileId.CopyDepthBuffer)))
                {
                    passData.inputDepth = builder.ReadTexture(output.resolvedDepthBuffer);
                    passData.outputDepth = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(ComputeDepthBufferMipChainSize, true, true)
                        { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "CameraDepthBufferMipChain" }, HDShaderIDs._CameraDepthTexture));
                    passData.GPUCopy = m_GPUCopy;
                    passData.width = hdCamera.actualWidth;
                    passData.height = hdCamera.actualHeight;

                    output.depthPyramidTexture = passData.outputDepth;

                    builder.SetRenderFunc(
                    (CopyDepthPassData data, RenderGraphContext context) =>
                    {
                        RenderGraphResourceRegistry resources = context.resources;
                        // TODO: maybe we don't actually need the top MIP level?
                        // That way we could avoid making the copy, and build the MIP hierarchy directly.
                        // The downside is that our SSR tracing accuracy would decrease a little bit.
                        // But since we never render SSR at full resolution, this may be acceptable.

                        // TODO: reading the depth buffer with a compute shader will cause it to decompress in place.
                        // On console, to preserve the depth test performance, we must NOT decompress the 'm_CameraDepthStencilBuffer' in place.
                        // We should call decompressDepthSurfaceToCopy() and decompress it to 'm_CameraDepthBufferMipChain'.
                        data.GPUCopy.SampleCopyChannel_xyzw2x(context.cmd, resources.GetTexture(data.inputDepth), resources.GetTexture(data.outputDepth), new RectInt(0, 0, data.width, data.height));
                    });
                }

                m_IsDepthBufferCopyValid = true;
            }
        }

        class ResolveStencilPassData
        {
            public RenderGraphResource inputDepth;
            public RenderGraphMutableResource resolvedStencil;
            public ComputeBuffer coarseStencilBuffer;
        }

        void ResolveStencilBufferIfNeeded(RenderGraph renderGraph, HDCamera hdCamera, ref PrepassOutput output)
        {
            using (var builder = renderGraph.AddRenderPass<ResolveStencilPassData>("Resolve Stencil", out var passData, ProfilingSampler.Get(HDProfileId.ResolveStencilBuffer)))
            {
                passData.inputDepth = output.depthBuffer;
                passData.coarseStencilBuffer = m_SharedRTManager.GetCoarseStencilBuffer();
                passData.resolvedStencil = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R8G8_UInt, enableRandomWrite = true, name = "StencilBufferResolved" }));
                builder.SetRenderFunc(
                (ResolveStencilPassData data, RenderGraphContext context) =>
                {
                    var res = context.resources;
                    BuildCoarseStencilAndResolveIfNeeded(hdCamera,
                        res.GetTexture(data.inputDepth),
                        res.GetTexture(data.resolvedStencil),
                        data.coarseStencilBuffer,
                        context.cmd);
                }
                );
                bool isMSAAEnabled = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA);

                if (isMSAAEnabled)
                {
                    output.stencilBuffer = passData.resolvedStencil;
                }
                else
                {
                    output.stencilBuffer = output.depthBuffer;
                }
            }
        }

        class RenderDBufferPassData
        {
            public RenderGraphMutableResource[] mrt = new RenderGraphMutableResource[Decal.GetMaterialDBufferCount()];
            public int                          dBufferCount;
            public RenderGraphResource          meshDecalsRendererList;
            public RenderGraphMutableResource   depthStencilBuffer;
        }

        struct DBufferOutput
        {
            public RenderGraphResource[]    mrt;
            public int                      dBufferCount;
        }

        class DBufferNormalPatchData
        {
            public DBufferNormalPatchParameters parameters;
            public RenderGraphResource depthStencilBuffer;
            public RenderGraphMutableResource normalBuffer;
        }

        void SetupDBufferTargets(RenderGraph renderGraph, RenderDBufferPassData passData, bool use4RTs, ref PrepassOutput output, RenderGraphBuilder builder)
        {
            GraphicsFormat[] rtFormat;
            Decal.GetMaterialDBufferDescription(out rtFormat);
            passData.dBufferCount = use4RTs ? 4 : 3;

            for (int dbufferIndex = 0; dbufferIndex < passData.dBufferCount; ++dbufferIndex)
            {
                passData.mrt[dbufferIndex] = builder.UseColorBuffer(renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one, true, true) { colorFormat = rtFormat[dbufferIndex], name = string.Format("DBuffer{0}", dbufferIndex) }, HDShaderIDs._DBufferTexture[dbufferIndex]), dbufferIndex);
            }

            passData.depthStencilBuffer = builder.UseDepthBuffer(output.resolvedDepthBuffer, DepthAccess.Write);

            output.dbuffer.dBufferCount = passData.dBufferCount;
            for (int i = 0; i < passData.dBufferCount; ++i)
            {
                output.dbuffer.mrt[i] = passData.mrt[i];
            }
        }

        static void ReadDBuffer(DBufferOutput dBufferOutput, RenderGraphBuilder builder)
        {
            // Will bind automatically so no need to track an explicit reference after ReadTexture is called (DBuffer targets have a ShaderTagId associated with them)
            for (int i = 0; i < dBufferOutput.dBufferCount; ++i)
                builder.ReadTexture(dBufferOutput.mrt[i]);

        }

        void RenderDBuffer(RenderGraph renderGraph, HDCamera hdCamera, ref PrepassOutput output, CullingResults cullingResults)
        {
            bool use4RTs = m_Asset.currentPlatformRenderPipelineSettings.decalSettings.perChannelMask;

            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals))
            {
                // Return all black textures for default values.
                var blackTexture = renderGraph.ImportTexture(TextureXR.GetBlackTexture());
                output.dbuffer.dBufferCount = use4RTs ? 4 : 3;
                for (int i = 0; i < output.dbuffer.dBufferCount; ++i)
                    output.dbuffer.mrt[i] = blackTexture;
                return;
            }

            // We need to copy depth buffer texture if we want to bind it at this stage
            CopyDepthBufferIfNeeded(renderGraph, hdCamera, ref output);

            using (var builder = renderGraph.AddRenderPass<RenderDBufferPassData>("DBufferRender", out var passData, ProfilingSampler.Get(HDProfileId.DBufferRender)))
            {
                passData.meshDecalsRendererList = builder.UseRendererList(renderGraph.CreateRendererList(PrepareMeshDecalsRendererList(cullingResults, hdCamera, use4RTs)));
                SetupDBufferTargets(renderGraph, passData, use4RTs, ref output, builder);

                builder.SetRenderFunc(
                (RenderDBufferPassData data, RenderGraphContext context) =>
                {
                    RenderGraphResourceRegistry resources = context.resources;

                    RenderTargetIdentifier[] rti = context.renderGraphPool.GetTempArray<RenderTargetIdentifier>(data.dBufferCount);
                    RTHandle[] rt = context.renderGraphPool.GetTempArray<RTHandle>(data.dBufferCount);

                    // TODO : Remove once we remove old renderer
                    // This way we can directly use the UseColorBuffer API and set clear color directly at resource creation and not in the RenderDBuffer shared function.
                    for (int i = 0; i < data.dBufferCount; ++i)
                    {
                        rt[i] = resources.GetTexture(data.mrt[i]);
                        rti[i] = rt[i];
                    }

                    RenderDBuffer(data.dBufferCount == 4,
                                    rti,
                                    rt,
                                    resources.GetTexture(data.depthStencilBuffer),
                                    m_DbufferManager.propertyMaskBuffer,
                                    m_DbufferManager.clearPropertyMaskBufferShader,
                                    m_DbufferManager.clearPropertyMaskBufferKernel,
                                    m_DbufferManager.propertyMaskBufferSize,
                                    resources.GetRendererList(data.meshDecalsRendererList),
                                    context.renderContext,
                                    context.cmd);
                });
            }
        }

        void DecalNormalPatch(RenderGraph renderGraph, HDCamera hdCamera, ref PrepassOutput output)
        {
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals) &&
                !hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA)) // MSAA not supported
            {
                using (var builder = renderGraph.AddRenderPass<DBufferNormalPatchData>("DBuffer Normal (forward)", out var passData, ProfilingSampler.Get(HDProfileId.DBufferNormal)))
                {
                    passData.parameters = PrepareDBufferNormalPatchParameters(hdCamera);
                    ReadDBuffer(output.dbuffer, builder);

                    passData.normalBuffer = builder.WriteTexture(output.resolvedNormalBuffer);
                    passData.depthStencilBuffer = builder.ReadTexture(output.resolvedDepthBuffer);

                    builder.SetRenderFunc(
                    (DBufferNormalPatchData data, RenderGraphContext context) =>
                    {
                        DecalNormalPatch(hdCamera, context.cmd, context.renderContext);
                    });
                }
            }
        }

        class GenerateDepthPyramidPassData
        {
            public RenderGraphMutableResource   depthTexture;
            public HDUtils.PackedMipChainInfo   mipInfo;
            public MipGenerator                 mipGenerator;
        }

        void GenerateDepthPyramid(RenderGraph renderGraph, HDCamera hdCamera, ref PrepassOutput output)
        {
            // If the depth buffer hasn't been already copied by the decal pass, then we do the copy here.
            CopyDepthBufferIfNeeded(renderGraph, hdCamera, ref output);

            using (var builder = renderGraph.AddRenderPass<GenerateDepthPyramidPassData>("Generate Depth Buffer MIP Chain", out var passData, ProfilingSampler.Get(HDProfileId.DepthPyramid)))
            {
                passData.depthTexture = builder.WriteTexture(output.depthPyramidTexture);
                passData.mipInfo = GetDepthBufferMipChainInfo();
                passData.mipGenerator = m_MipGenerator;

                builder.SetRenderFunc(
                (GenerateDepthPyramidPassData data, RenderGraphContext context) =>
                {
                    data.mipGenerator.RenderMinDepthPyramid(context.cmd, context.resources.GetTexture(data.depthTexture), data.mipInfo);
                });

                output.depthPyramidTexture = passData.depthTexture;
            }

            // TODO: This is currently useless as the depth pyramid is an atlas.
            // The mip choice is directly resolved in the full screen debug pass.
            // Re-enable this when/if depth texture becomes an actual texture with mip again.
            //PushFullScreenDebugTextureMip(renderGraph, output.depthPyramidTexture, GetDepthBufferMipChainInfo().mipLevelCount, renderGraph.rtHandleProperties.rtHandleScale, FullScreenDebugMode.DepthPyramid);
        }

        class CameraMotionVectorsPassData
        {
            public Material cameraMotionVectorsMaterial;
            public RenderGraphMutableResource motionVectorsBuffer;
            public RenderGraphResource depthTexture;
        }

        void RenderCameraMotionVectors(RenderGraph renderGraph, HDCamera hdCamera, RenderGraphResource depthTexture, RenderGraphMutableResource motionVectorsBuffer)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.MotionVectors))
                return;

            using (var builder = renderGraph.AddRenderPass<CameraMotionVectorsPassData>("Camera Motion Vectors Rendering", out var passData, ProfilingSampler.Get(HDProfileId.CameraMotionVectors)))
            {
                // These flags are still required in SRP or the engine won't compute previous model matrices...
                // If the flag hasn't been set yet on this camera, motion vectors will skip a frame.
                hdCamera.camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;

                passData.cameraMotionVectorsMaterial = m_CameraMotionVectorsMaterial;
                passData.depthTexture = builder.ReadTexture(depthTexture);
                passData.motionVectorsBuffer = builder.WriteTexture(motionVectorsBuffer);

                builder.SetRenderFunc(
                (CameraMotionVectorsPassData data, RenderGraphContext context) =>
                {
                    var res = context.resources;
                    HDUtils.DrawFullScreen(context.cmd, data.cameraMotionVectorsMaterial, res.GetTexture(data.motionVectorsBuffer));
                });
            }
        }
    }
}
