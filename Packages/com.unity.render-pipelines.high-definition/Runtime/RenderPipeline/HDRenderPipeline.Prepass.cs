using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        Material m_MSAAResolveMaterial, m_MSAAResolveMaterialDepthOnly;
        Material m_CameraMotionVectorsMaterial;
        Material m_DecalNormalBufferMaterial;
        Material m_DownsampleDepthMaterialLoad;
        Material m_DownsampleDepthMaterialGather;
        Material[] m_ComputeThicknessOpaqueMaterial;
        Material[] m_ComputeThicknessTransparentMaterial;
        uint[] m_ComputeThicknessReindexMapData = new uint[HDComputeThickness.computeThicknessMaxLayer];
        uint[] m_ComputeThicknessLayerMask = new uint[HDComputeThickness.computeThicknessMaxLayer];
        bool[] m_ComputeThicknessReindexSolver = new bool[HDComputeThickness.computeThicknessMaxLayer];
        ShaderTagId[] m_ComputeThicknessShaderTags = { HDShaderPassNames.s_ForwardName, HDShaderPassNames.s_ForwardOnlyName, HDShaderPassNames.s_SRPDefaultUnlitName };
        GraphicsBuffer m_ComputeThicknessReindexMap;

        // Need to cache to avoid alloc of arrays...
        GBufferOutput m_GBufferOutput;
        DBufferOutput m_DBufferOutput;

        GPUCopy m_GPUCopy;

        const int m_MaxXRViewsCount = 4;

        const int kIntelVendorId = 0x8086;

        void InitializePrepass(HDRenderPipelineAsset hdAsset)
        {
            m_MSAAResolveMaterial = CoreUtils.CreateEngineMaterial(runtimeShaders.depthValuesPS);
            m_MSAAResolveMaterialDepthOnly = CoreUtils.CreateEngineMaterial(runtimeShaders.depthValuesPS);
            m_MSAAResolveMaterialDepthOnly.EnableKeyword("_DEPTH_ONLY");
            m_CameraMotionVectorsMaterial = CoreUtils.CreateEngineMaterial(runtimeShaders.cameraMotionVectorsPS);
            m_DecalNormalBufferMaterial = CoreUtils.CreateEngineMaterial(runtimeShaders.decalNormalBufferPS);
            m_DownsampleDepthMaterialLoad = CoreUtils.CreateEngineMaterial(runtimeShaders.downsampleDepthPS);
            m_DownsampleDepthMaterialGather = CoreUtils.CreateEngineMaterial(runtimeShaders.downsampleDepthPS);
            m_DownsampleDepthMaterialGather.EnableKeyword("GATHER_DOWNSAMPLE");
            m_ComputeThicknessOpaqueMaterial = new Material[m_MaxXRViewsCount];
            m_ComputeThicknessTransparentMaterial = new Material[m_MaxXRViewsCount];
            for (int viewId = 0; viewId < m_MaxXRViewsCount; ++viewId)
            {
                m_ComputeThicknessOpaqueMaterial[viewId] = CoreUtils.CreateEngineMaterial(runtimeShaders.ComputeThicknessPS);
                m_ComputeThicknessOpaqueMaterial[viewId].SetInt(HDShaderIDs._ViewId, viewId);
                m_ComputeThicknessTransparentMaterial[viewId] = CoreUtils.CreateEngineMaterial(runtimeShaders.ComputeThicknessPS);
                m_ComputeThicknessTransparentMaterial[viewId].SetInt(HDShaderIDs._ViewId, viewId);
            }
            m_ComputeThicknessReindexMap = new GraphicsBuffer(GraphicsBuffer.Target.Structured, (int)HDComputeThickness.computeThicknessMaxLayer, sizeof(uint));

            m_GBufferOutput = new GBufferOutput();
            m_GBufferOutput.mrt = new TextureHandle[RenderGraph.kMaxMRTCount];

            m_DBufferOutput = new DBufferOutput();
            m_DBufferOutput.mrt = new TextureHandle[(int)Decal.DBufferMaterial.Count];

            m_GPUCopy = new GPUCopy(runtimeShaders.copyChannelCS);
        }

        void CleanupPrepass()
        {
            CoreUtils.Destroy(m_MSAAResolveMaterial);
            CoreUtils.Destroy(m_MSAAResolveMaterialDepthOnly);
            CoreUtils.Destroy(m_CameraMotionVectorsMaterial);
            CoreUtils.Destroy(m_DecalNormalBufferMaterial);
            CoreUtils.Destroy(m_DownsampleDepthMaterialLoad);
            CoreUtils.Destroy(m_DownsampleDepthMaterialGather);
            m_ComputeThicknessReindexMap.Dispose();
            for (int viewId = 0; viewId < m_MaxXRViewsCount; ++viewId)
            {
                CoreUtils.Destroy(m_ComputeThicknessOpaqueMaterial[viewId]);
                CoreUtils.Destroy(m_ComputeThicknessTransparentMaterial[viewId]);
            }
        }

        bool NeedClearGBuffer(HDCamera hdCamera)
        {
            return m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled() || hdCamera.frameSettings.IsEnabled(FrameSettingsField.ClearGBuffers);
        }

        internal struct PrepassOutput
        {
            // Buffers that may be output by the prepass.
            // They will be MSAA depending on the frame settings
            public TextureHandle depthBuffer;
            public TextureHandle depthAsColor;
            public TextureHandle normalBuffer;
            public TextureHandle motionVectorsBuffer;
            // This one contains 16 bits vertex normal and 8 bits decal layers in RGB
            // It also contains light layers in last eight bits, so it can be part of gbuffer
            public TextureHandle renderingLayersBuffer;

            // GBuffer output. Will also contain a reference to the normal buffer (as it is shared between deferred and forward objects)
            public GBufferOutput gbuffer;

            public DBufferOutput dbuffer;

            // Additional buffers only for MSAA
            public TextureHandle depthValuesMSAA;

            // Resolved buffers for MSAA. When MSAA is off, they will be the same reference as the buffers above.
            public TextureHandle resolvedDepthBuffer;
            public TextureHandle resolvedNormalBuffer;
            public TextureHandle resolvedMotionVectorsBuffer;

            // Copy of the resolved depth buffer with mip chain
            public TextureHandle depthPyramidTexture;
            // Depth buffer used for low res transparents.
            public TextureHandle downsampledDepthBuffer;

            public TextureHandle stencilBuffer;
            public BufferHandle coarseStencilBuffer;

            // Output by the water system to mark underwater pixels (during transparent prepass)
            public BufferHandle waterLine;

            public TextureHandle shadingRateImage;
        }

        TextureHandle CreateDepthBuffer(RenderGraph renderGraph, bool clear, MSAASamples msaaSamples, string name = null, bool disableFallback = true)
        {
            bool msaa = msaaSamples != MSAASamples.None;
#if UNITY_2020_2_OR_NEWER
            FastMemoryDesc fastMemDesc;
            fastMemDesc.inFastMemory = true;
            fastMemDesc.residencyFraction = 1.0f;
            fastMemDesc.flags = FastMemoryFlags.SpillTop;
#endif

            TextureDesc depthDesc = new TextureDesc(Vector2.one, true, true)
            {
                format = CoreUtils.GetDefaultDepthStencilFormat(),
                bindTextureMS = msaa,
                msaaSamples = msaaSamples,
                clearBuffer = clear,
                name = name ?? (msaa ? "CameraDepthStencilMSAA" : "CameraDepthStencil"),
                disableFallBackToImportedTexture = disableFallback,
                fallBackToBlackTexture = true,
#if UNITY_2020_2_OR_NEWER
                fastMemoryDesc = fastMemDesc,
#endif
            };

            return renderGraph.CreateTexture(depthDesc);
        }

        TextureHandle CreateNormalBuffer(RenderGraph renderGraph, HDCamera hdCamera, MSAASamples msaaSamples)
        {
            bool msaa = msaaSamples != MSAASamples.None;
#if UNITY_2020_2_OR_NEWER
            FastMemoryDesc fastMemDesc;
            fastMemDesc.inFastMemory = true;
            fastMemDesc.residencyFraction = 1.0f;
            fastMemDesc.flags = FastMemoryFlags.SpillTop;
#endif

            TextureDesc normalDesc = new TextureDesc(Vector2.one, true, true)
            {
                format = GraphicsFormat.R8G8B8A8_UNorm,
                clearBuffer = NeedClearGBuffer(hdCamera),
                clearColor = Color.black,
                bindTextureMS = msaa,
                msaaSamples = msaaSamples,
                enableRandomWrite = !msaa,
                name = msaa ? "NormalBufferMSAA" : "NormalBuffer",
                fallBackToBlackTexture = true,
#if UNITY_2020_2_OR_NEWER
                fastMemoryDesc = fastMemDesc,
#endif
            };
            return renderGraph.CreateTexture(normalDesc);
        }

        TextureHandle CreateRenderingLayersBuffer(RenderGraph renderGraph, MSAASamples msaaSamples, bool decalLayers)
        {
            bool msaa = msaaSamples != MSAASamples.None;
            bool enableRandomWrite = decalLayers && !msaa;
            var format = decalLayers ? GraphicsFormat.R8G8B8A8_UNorm : GraphicsFormat.R8G8_UNorm;
            TextureDesc decalDesc = new TextureDesc(Vector2.one, true, true)
            { format = format, clearBuffer = true, clearColor = Color.clear, bindTextureMS = false, msaaSamples = msaaSamples, enableRandomWrite = enableRandomWrite, name = msaa ? "RenderingLayersBufferMSAA" : "RenderingLayersBuffer" };
            return renderGraph.CreateTexture(decalDesc);
        }

        TextureHandle CreateDepthAsColorBuffer(RenderGraph renderGraph, MSAASamples msaaSamples)
        {
            return renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            { format = GraphicsFormat.R32_SFloat, clearBuffer = true, clearColor = Color.black, bindTextureMS = true, msaaSamples = msaaSamples, name = "DepthAsColorMSAA" });
        }

        TextureHandle CreateMotionVectorBuffer(RenderGraph renderGraph, bool clear, MSAASamples msaaSamples)
        {
            bool msaa = msaaSamples != MSAASamples.None;
            TextureDesc motionVectorDesc = new TextureDesc(Vector2.one, true, true)
            { format = Builtin.GetMotionVectorFormat(), bindTextureMS = msaa, msaaSamples = msaaSamples, clearBuffer = clear, clearColor = Color.clear, name = msaa ? "Motion Vectors MSAA" : "Motion Vectors" };
            return renderGraph.CreateTexture(motionVectorDesc);
        }

        void BindMotionVectorPassColorBuffers(in RenderGraphBuilder builder, in PrepassOutput prepassOutput, HDCamera hdCamera)
        {
            bool msaa = hdCamera.msaaSamples != MSAASamples.None;
            bool outputLayerMask = hdCamera.frameSettings.IsEnabled(FrameSettingsField.DecalLayers) ||
                hdCamera.frameSettings.IsEnabled(FrameSettingsField.RenderingLayerMaskBuffer);

            int index = 0;
            if (msaa)
                builder.UseColorBuffer(prepassOutput.depthAsColor, index++);
            builder.UseColorBuffer(prepassOutput.motionVectorsBuffer, index++);
            if (outputLayerMask)
                builder.UseColorBuffer(prepassOutput.renderingLayersBuffer, index++);
            builder.UseColorBuffer(prepassOutput.normalBuffer, index++);
        }

        enum OccluderPass
        {
            None,
            DepthPrepass,
            GBuffer
        }

        OccluderPass GetOccluderPass(HDCamera hdCamera)
        {
            bool useGPUOcclusionCulling = GPUResidentDrawer.IsInstanceOcclusionCullingEnabled()
                                          && hdCamera.camera.cameraType is CameraType.Game or CameraType.SceneView or CameraType.Preview;
            if (!useGPUOcclusionCulling)
                return OccluderPass.None;

            bool fullDepthPrepass = (hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred && hdCamera.frameSettings.IsEnabled(FrameSettingsField.DepthPrepassWithDeferredRendering))
                || hdCamera.frameSettings.litShaderMode == LitShaderMode.Forward;
            if (fullDepthPrepass || m_Asset.currentPlatformRenderPipelineSettings.gpuResidentDrawerSettings.useDepthPrepassForOccluders)
                return OccluderPass.DepthPrepass;

            return OccluderPass.GBuffer;
        }

        void UpdateInstanceOccluders(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthTexture)
        {
            bool isSinglePassXR = hdCamera.xr.enabled && hdCamera.xr.singlePassEnabled;
            var occluderParams = new OccluderParameters(hdCamera.camera.GetInstanceID())
            {
                subviewCount = isSinglePassXR ? 2 : 1,
                depthTexture = depthTexture,
                depthSize = new Vector2Int(hdCamera.actualWidth, hdCamera.actualHeight),
                depthIsArray = TextureXR.useTexArray,
            };
            Span<OccluderSubviewUpdate> occluderSubviewUpdates = stackalloc OccluderSubviewUpdate[occluderParams.subviewCount];
            for (int subviewIndex = 0; subviewIndex < occluderParams.subviewCount; ++subviewIndex)
            {
                occluderSubviewUpdates[subviewIndex] = new OccluderSubviewUpdate(subviewIndex)
                {
                    depthSliceIndex = subviewIndex,
                    viewMatrix = hdCamera.m_XRViewConstants[subviewIndex].viewMatrix,
                    invViewMatrix = hdCamera.m_XRViewConstants[subviewIndex].invViewMatrix,
                    gpuProjMatrix = hdCamera.m_XRViewConstants[subviewIndex].projMatrix,
                    viewOffsetWorldSpace = hdCamera.m_XRViewConstants[subviewIndex].worldSpaceCameraPos,
                };
            }
            GPUResidentDrawer.UpdateInstanceOccluders(renderGraph, occluderParams, occluderSubviewUpdates);
        }

        void InstanceOcclusionTest(RenderGraph renderGraph, HDCamera hdCamera, OcclusionTest occlusionTest)
        {
            bool isSinglePassXR = hdCamera.xr.enabled && hdCamera.xr.singlePassEnabled;
            int subviewCount = isSinglePassXR ? 2 : 1;
            var settings = new OcclusionCullingSettings(hdCamera.camera.GetInstanceID(), occlusionTest)
            {
                instanceMultiplier = (isSinglePassXR && !SystemInfo.supportsMultiview) ? 2 : 1,
            };
            Span<SubviewOcclusionTest> subviewOcclusionTests = stackalloc SubviewOcclusionTest[subviewCount];
            for (int subviewIndex = 0; subviewIndex < subviewCount; ++subviewIndex)
            {
                subviewOcclusionTests[subviewIndex] = new SubviewOcclusionTest()
                {
                    cullingSplitIndex = 0,
                    occluderSubviewIndex = subviewIndex,
                };
            }
            GPUResidentDrawer.InstanceOcclusionTest(renderGraph, settings, subviewOcclusionTests);
        }

        PrepassOutput RenderPrepass(RenderGraph renderGraph,
            TextureHandle colorBuffer,
            TextureHandle sssBuffer,
            TextureHandle thicknessTexture,
            TextureHandle vtFeedbackBuffer,
            CullingResults cullingResults,
            CullingResults customPassCullingResults,
            HDCamera hdCamera,
            AOVRequestData aovRequest,
            List<RTHandle> aovBuffers)
        {
            m_IsDepthBufferCopyValid = false;

            var result = new PrepassOutput();
            result.gbuffer = m_GBufferOutput;
            result.dbuffer = m_DBufferOutput;

            bool msaa = hdCamera.msaaEnabled;
            bool decalLayers = hdCamera.frameSettings.IsEnabled(FrameSettingsField.DecalLayers);
            bool renderingLayers = decalLayers || hdCamera.frameSettings.IsEnabled(FrameSettingsField.LightLayers) || hdCamera.frameSettings.IsEnabled(FrameSettingsField.RenderingLayerMaskBuffer);
            bool motionVectors = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MotionVectors);

            // TODO: See how to clean this. Some buffers are created outside, some inside functions...
            result.motionVectorsBuffer = motionVectors ? CreateMotionVectorBuffer(renderGraph, msaa, hdCamera.msaaSamples) : renderGraph.defaultResources.blackTextureXR;
            result.depthBuffer = CreateDepthBuffer(renderGraph, hdCamera.clearDepth, hdCamera.msaaSamples);
            result.stencilBuffer = result.depthBuffer;
            result.renderingLayersBuffer = renderingLayers ? CreateRenderingLayersBuffer(renderGraph, hdCamera.msaaSamples, decalLayers) : renderGraph.defaultResources.blackTextureXR;
            result.shadingRateImage = hdCamera.vrsEnabled ? renderGraph.ImportShadingRateImageTexture(RequestVrsHistory(hdCamera, 1)) : TextureHandle.nullHandle; // Allocate VRS texture if needed

            RenderXROcclusionMeshes(renderGraph, hdCamera, colorBuffer, result.depthBuffer);

            using (new XRSinglePassScope(renderGraph, hdCamera))
            {
                RenderCustomPass(renderGraph, hdCamera, colorBuffer, result, customPassCullingResults, cullingResults, CustomPassInjectionPoint.BeforeRendering, aovRequest, aovBuffers);

                RenderRayTracingDepthPrepass(renderGraph, cullingResults, hdCamera, result.depthBuffer);

                OccluderPass occluderPass = GetOccluderPass(hdCamera);

                bool shouldRenderMotionVectorAfterGBuffer = false;
                bool needsOccluderUpdate = (occluderPass == OccluderPass.DepthPrepass);
                for (int passIndex = 0; passIndex < (needsOccluderUpdate ? 2 : 1); ++passIndex)
                {
                    uint batchLayerMask = uint.MaxValue;
                    if (needsOccluderUpdate)
                    {
                        // first pass: test everything against previous frame final depth pyramid
                        // second pass: re-test culled against current frame intermediate depth pyramid
                        OcclusionTest occlusionTest = (passIndex == 0) ? OcclusionTest.TestAll : OcclusionTest.TestCulled;
                        InstanceOcclusionTest(renderGraph, hdCamera, occlusionTest);
                        batchLayerMask = occlusionTest.GetBatchLayerMask();
                    }

                    shouldRenderMotionVectorAfterGBuffer = RenderDepthPrepass(renderGraph, cullingResults, batchLayerMask, hdCamera, ref result);
                    if (!shouldRenderMotionVectorAfterGBuffer)
                    {
                        // If objects motion vectors are enabled, this will render the objects with motion vector into the target buffers (in addition to the depth)
                        // Note: An object with motion vector must not be render in the prepass otherwise we can have motion vector write that should have been rejected
                        RenderObjectsMotionVectors(renderGraph, cullingResults, batchLayerMask, hdCamera, result);
                    }

                    if (needsOccluderUpdate)
                    {
                        // first pass: make current frame intermediate depth pyramid
                        // second pass: make current frame final depth pyramid, set occlusion test results for later passes
                        UpdateInstanceOccluders(renderGraph, hdCamera, result.depthBuffer);
                        if (passIndex != 0)
                            InstanceOcclusionTest(renderGraph, hdCamera, OcclusionTest.TestAll);
                    }
                }

                // If we have MSAA, we need to complete the motion vector buffer before buffer resolves, hence we need to run camera mv first.
                // This is always fine since shouldRenderMotionVectorAfterGBuffer is always false for forward.
                bool needCameraMVBeforeResolve = msaa;
                if (needCameraMVBeforeResolve)
                {
                    RenderCameraMotionVectors(renderGraph, hdCamera, result.depthBuffer, result.motionVectorsBuffer);
                }

                PreRenderSky(renderGraph, hdCamera, result.depthBuffer, result.normalBuffer);

                m_VolumetricClouds.PreRenderVolumetricClouds(renderGraph, hdCamera);

                // At this point in forward all objects have been rendered to the prepass (depth/normal/motion vectors) so we can resolve them
                ResolvePrepassBuffers(renderGraph, hdCamera, ref result);

                if (IsComputeThicknessNeeded(hdCamera))
                    // Compute thicknes for AllOpaque before the GBuffer without reading DepthBuffer
                    RenderThickness(renderGraph, cullingResults, thicknessTexture, TextureHandle.nullHandle, hdCamera, HDRenderQueue.k_RenderQueue_AllOpaque, false);

                RenderDBuffer(renderGraph, hdCamera, ref result, cullingResults);

                needsOccluderUpdate = (occluderPass == OccluderPass.GBuffer);
                for (int passIndex = 0; passIndex < (needsOccluderUpdate ? 2 : 1); ++passIndex)
                {
                    uint batchLayerMask = uint.MaxValue;
                    if (needsOccluderUpdate)
                    {
                        // first pass: test everything against previous frame final depth pyramid
                        // second pass: re-test culled against current frame intermediate depth pyramid
                        OcclusionTest occlusionTest = (passIndex == 0) ? OcclusionTest.TestAll : OcclusionTest.TestCulled;
                        InstanceOcclusionTest(renderGraph, hdCamera, occlusionTest);
                        batchLayerMask = occlusionTest.GetBatchLayerMask();
                    }

                    RenderGBuffer(renderGraph, sssBuffer, vtFeedbackBuffer, ref result, cullingResults, batchLayerMask, hdCamera);

                    if (needsOccluderUpdate)
                    {
                        // first pass: make current frame intermediate depth pyramid
                        // second pass: make current frame final depth pyramid, set occlusion test results for later passes
                        UpdateInstanceOccluders(renderGraph, hdCamera, result.depthBuffer);
                        if (passIndex != 0)
                            InstanceOcclusionTest(renderGraph, hdCamera, OcclusionTest.TestAll);
                    }
                }

                if (shouldRenderMotionVectorAfterGBuffer)
                {
                    // See the call RenderObjectsMotionVectors() above and comment
                    // We need to complete the depth prepass before patching the normal buffer with decals
                    // Note: This pass will overwrite the deferred stencil bit as it happens after GBuffer pass
                    RenderObjectsMotionVectors(renderGraph, cullingResults, uint.MaxValue, hdCamera, result);
                }

                // Now that all prepass are rendered, we can patch the normal buffer
                DecalNormalPatch(renderGraph, hdCamera, ref result);

                // After Depth and Normals/roughness including decals
                bool depthBufferModified = RenderCustomPass(renderGraph, hdCamera, colorBuffer, result, customPassCullingResults, cullingResults, CustomPassInjectionPoint.AfterOpaqueDepthAndNormal, aovRequest, aovBuffers);

                // If the depth was already copied in RenderDBuffer, we force the copy again because the custom pass modified the depth.
                if (depthBufferModified)
                    m_IsDepthBufferCopyValid = false;

                // In both forward and deferred, everything opaque should have been rendered at this point so we can safely copy the depth buffer for later processing.
                GenerateDepthPyramid(renderGraph, hdCamera, ref result);
                DownsampleDepthForLowResTransparency(renderGraph, hdCamera, ref result);

                // In case we don't have MSAA, we always run camera motion vectors when is safe to assume Object MV are rendered
                if (!needCameraMVBeforeResolve)
                {
                    RenderCameraMotionVectors(renderGraph, hdCamera, result.depthBuffer, result.resolvedMotionVectorsBuffer);
                }

                // NOTE: Currently we profiled that generating the HTile for SSR and using it is not worth it the optimization.
                // However if the generated HTile will be used for something else but SSR, this should be made NOT resolve only and
                // re-enabled in the shader.
                BuildCoarseStencilAndResolveIfNeeded(renderGraph, hdCamera, resolveOnly: true, ref result);

                RenderTransparencyOverdraw(renderGraph, result.depthBuffer, cullingResults, hdCamera);
            }

            return result;
        }

        class RayTracingDepthPrepassData
        {
            public FrameSettings frameSettings;
            public TextureHandle depthBuffer;
            public RendererListHandle opaqueRenderList;
            public RendererListHandle transparentRenderList;
        }

        void RenderRayTracingDepthPrepass(RenderGraph renderGraph, CullingResults cull, HDCamera hdCamera, TextureHandle depthBuffer)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing))
                return;

            RecursiveRendering recursiveSettings = hdCamera.volumeStack.GetComponent<RecursiveRendering>();
            if (!(recursiveSettings.enable.value && GetRayTracingState() && GetRayTracingClusterState()))
                return;

            // The goal of this pass is to fill the depth buffer with object flagged for recursive rendering.
            // This will save performance because we reduce overdraw of non recursive rendering objects.
            // This is also required to avoid marking the pixels for various effects like motion blur and such.
            using (var builder = renderGraph.AddRenderPass<RayTracingDepthPrepassData>("RayTracing Depth Prepass", out var passData, ProfilingSampler.Get(HDProfileId.RayTracingDepthPrepass)))
            {
                passData.frameSettings = hdCamera.frameSettings;
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);
                passData.opaqueRenderList = builder.UseRendererList(renderGraph.CreateRendererList(CreateOpaqueRendererListDesc(cull, hdCamera.camera, m_RayTracingPrepassNames)));
                passData.transparentRenderList = builder.UseRendererList(renderGraph.CreateRendererList(CreateTransparentRendererListDesc(cull, hdCamera.camera, m_RayTracingPrepassNames)));

                builder.SetRenderFunc(
                    (RayTracingDepthPrepassData data, RenderGraphContext context) =>
                    {
                        DrawOpaqueRendererList(context.renderContext, context.cmd, data.frameSettings, data.opaqueRenderList);
                        DrawTransparentRendererList(context.renderContext, context.cmd, data.frameSettings, data.transparentRenderList);
                    });
            }
        }

        class DrawRendererListPassData
        {
            public FrameSettings frameSettings;
            public RendererListHandle rendererList;
        }

        // RenderDepthPrepass render both opaque and opaque alpha tested based on engine configuration.
        // Lit Forward only: We always render all materials
        // Lit Deferred: We always render depth prepass for alpha tested (optimization), other deferred material are render based on engine configuration.
        // Forward opaque with deferred renderer (DepthForwardOnly pass): We always render all materials
        // True is returned if motion vector must be rendered after GBuffer pass
        bool RenderDepthPrepass(RenderGraph renderGraph, CullingResults cull, uint batchLayerMask, HDCamera hdCamera, ref PrepassOutput output)
        {
            // Guidelines:
            // Lit shader can be in deferred or forward mode. In this case we use "DepthOnly" pass with "GBuffer" or "Forward" pass name
            // Other shader, including unlit are always forward and use "DepthForwardOnly" with "ForwardOnly" pass.
            // Those pass are exclusive so use only "DepthOnly" or "DepthForwardOnly" but not both at the same time, same for "Forward" and "DepthForwardOnly"
            // Any opaque material rendered in forward should have a depth prepass. If there is no depth prepass the lighting will be incorrect (deferred shadowing, contact shadow, SSAO), this may be acceptable depends on usage

            // Whatever the configuration we always render first opaque object then opaque alpha tested as they are more costly to render and could be reject by early-z
            // (but no Hi-z as it is disable with clip instruction). This is handled automatically with the RenderQueue value (OpaqueAlphaTested have a different value and thus are sorted after Opaque)

            // Forward material always output normal buffer.
            // Deferred material never output normal buffer.
            // Caution: Unlit material let normal buffer untouch. Caution as if people try to filter normal buffer, it can result in weird result.
            // TODO: Do we need a stencil bit to identify normal buffer not fill by unlit? So don't execute SSAO / SRR ?

            // Additional guidelines for motion vector:
            // We render object motion vector at the same time than depth prepass with MRT to save drawcall. Depth buffer is then fill with combination of depth prepass + motion vector.
            // For this we render first all objects that render depth only, then object that require object motion vector.
            // We use the excludeMotion filter option of DrawRenderer to gather object without object motion vector (only C++ can know if an object have object motion vector).
            // Caution: if there is no depth prepass we must render object motion vector after GBuffer pass otherwise some depth only objects can hide objects with motion vector and overwrite depth buffer but not update
            // the motion vector buffer resulting in artifacts

            // Additional guideline for decal
            // Decal are in their own render queue to allow to force them to render in depth buffer.
            // Thus it is not required to do a full depth prepass when decal are enabled
            // Mean when decal are enabled and we haven't request a full prepass in deferred, we can't guarantee that the prepass will be complete

            // With all this variant we have the following scenario of render target binding
            // decalsEnabled
            //     LitShaderMode.Forward
            //         Range Opaque both deferred and forward - depth + optional msaa + normal
            //         Range opaqueDecal for both deferred and forward - depth + optional msaa + normal + decal
            //         Range opaqueAlphaTest for both deferred and forward - depth + optional msaa + normal
            //         Range opaqueDecalAlphaTes for both deferred and forward - depth + optional msaa + normal + decal
            //    LitShaderMode.Deferred
            //         fullDeferredPrepass
            //             Range Opaque for deferred - depth
            //             Range opaqueDecal for deferred - depth + decal
            //             Range opaqueAlphaTest for deferred - depth
            //             Range opaqueDecalAlphaTes for deferred - depth + decal

            //             Range Opaque for forward - depth + normal
            //             Range opaqueDecal for forward - depth + normal + decal
            //             Range opaqueAlphaTest for forward - depth + normal
            //             Range opaqueDecalAlphaTes for forward - depth + normal + decal
            //         !fullDeferredPrepass
            //             Range opaqueDecal for deferred - depth + decal
            //             Range opaqueAlphaTest for deferred - depth
            //             Range opaqueDecalAlphaTes for deferred - depth + decal

            //             Range Opaque for forward - depth + normal
            //             Range opaqueDecal for forward - depth + normal + decal
            //             Range opaqueAlphaTest for forward - depth + normal
            //             Range opaqueDecalAlphaTesT for forward - depth + normal + decal
            // !decalsEnabled
            //     LitShaderMode.Forward
            //         Range Opaque..OpaqueDecalAlphaTest for deferred and forward - depth + optional msaa + normal
            //     LitShaderMode.Deferred
            //         fullDeferredPrepass
            //             Range Opaque..OpaqueDecalAlphaTest for deferred - depth

            //             Range Opaque..OpaqueDecalAlphaTest for forward - depth + normal
            //         !fullDeferredPrepass
            //             Range OpaqueAlphaTest..OpaqueDecalAlphaTest for deferred - depth

            //             Range Opaque..OpaqueDecalAlphaTest for forward - depth + normal

            bool msaa = hdCamera.msaaEnabled;
            if (!output.depthAsColor.IsValid() && msaa)
                output.depthAsColor = CreateDepthAsColorBuffer(renderGraph, hdCamera.msaaSamples);
            if (!output.normalBuffer.IsValid())
                output.normalBuffer = CreateNormalBuffer(renderGraph, hdCamera, hdCamera.msaaSamples);

            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects))
                return false;

            bool outputLayerMask = hdCamera.frameSettings.IsEnabled(FrameSettingsField.DecalLayers) || hdCamera.frameSettings.IsEnabled(FrameSettingsField.RenderingLayerMaskBuffer);
            bool decalsEnabled = hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals);
            bool fullDeferredPrepass = hdCamera.frameSettings.IsEnabled(FrameSettingsField.DepthPrepassWithDeferredRendering);
            // To avoid rendering objects twice (once in the depth pre-pass and once in the motion vector pass when the motion vector pass is enabled) we exclude the objects that have motion vectors.
            bool objectMotionEnabled = hdCamera.frameSettings.IsEnabled(FrameSettingsField.ObjectMotionVectors);
            bool excludeMotion = fullDeferredPrepass ? objectMotionEnabled : false;
            bool shouldRenderMotionVectorAfterGBuffer = (hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred) && !fullDeferredPrepass;

            // First prepass for relevant objects in deferred.
            // Alpha tested object have always a prepass even if enableDepthPrepassWithDeferredRendering is disabled
            if (hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred)
            {
                string deferredPassName = fullDeferredPrepass ? "Full Depth Prepass (Deferred)" :
                    (decalsEnabled ? "Partial Depth Prepass (Deferred - Decal + AlphaTest)" : "Partial Depth Prepass (Deferred - AlphaTest)");

                using (var builder = renderGraph.AddRenderPass<DrawRendererListPassData>(deferredPassName, out var passData, ProfilingSampler.Get(HDProfileId.DeferredDepthPrepass)))
                {
                    builder.AllowRendererListCulling(false);

                    passData.frameSettings = hdCamera.frameSettings;
                    passData.rendererList = builder.UseRendererList(renderGraph.CreateRendererList(CreateOpaqueRendererListDesc(
                        cull, hdCamera.camera, m_DepthOnlyPassNames,
                        renderQueueRange: fullDeferredPrepass ? HDRenderQueue.k_RenderQueue_AllOpaque :
                        (decalsEnabled ? HDRenderQueue.k_RenderQueue_OpaqueDecalAndAlphaTest : HDRenderQueue.k_RenderQueue_OpaqueAlphaTest),
                        stateBlock: m_AlphaToMaskBlock,
                        excludeObjectMotionVectors: excludeMotion,
                        batchLayerMask: batchLayerMask)));

                    output.depthBuffer = builder.UseDepthBuffer(output.depthBuffer, DepthAccess.ReadWrite);
                    if (outputLayerMask)
                        builder.UseColorBuffer(output.renderingLayersBuffer, 0);

                    builder.SetRenderFunc(
                        (DrawRendererListPassData data, RenderGraphContext context) =>
                        {
                            DrawOpaqueRendererList(context.renderContext, context.cmd, data.frameSettings, data.rendererList);
                        });
                }
            }

            string forwardPassName = hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred ? "Forward Depth Prepass (Deferred ForwardOnly)" : "Forward Depth Prepass";
            // Then prepass for forward materials.
            using (var builder = renderGraph.AddRenderPass<DrawRendererListPassData>(forwardPassName, out var passData, ProfilingSampler.Get(HDProfileId.ForwardDepthPrepass)))
            {
                passData.frameSettings = hdCamera.frameSettings;
                builder.AllowRendererListCulling(false);

                output.depthBuffer = builder.UseDepthBuffer(output.depthBuffer, DepthAccess.ReadWrite);
                int mrtIndex = 0;
                if (msaa)
                    builder.UseColorBuffer(output.depthAsColor, mrtIndex++);
                builder.UseColorBuffer(output.normalBuffer, mrtIndex++);

                if (outputLayerMask)
                    builder.UseColorBuffer(output.renderingLayersBuffer, mrtIndex++);

                if (hdCamera.frameSettings.litShaderMode == LitShaderMode.Forward)
                {
                    RenderStateBlock? stateBlock = hdCamera.msaaEnabled ? null : m_AlphaToMaskBlock;

                    passData.rendererList = builder.UseRendererList(renderGraph.CreateRendererList(
                        CreateOpaqueRendererListDesc(cull, hdCamera.camera, m_DepthOnlyAndDepthForwardOnlyPassNames, stateBlock: stateBlock, excludeObjectMotionVectors: objectMotionEnabled,
                        batchLayerMask: batchLayerMask)));
                }
                else if (hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred)
                {
                    // Forward only material that output normal buffer
                    passData.rendererList = builder.UseRendererList(renderGraph.CreateRendererList(
                        CreateOpaqueRendererListDesc(cull, hdCamera.camera, m_DepthForwardOnlyPassNames, stateBlock: m_AlphaToMaskBlock, excludeObjectMotionVectors: objectMotionEnabled,
                        batchLayerMask: batchLayerMask)));
                }

                builder.SetRenderFunc(
                    (DrawRendererListPassData data, RenderGraphContext context) =>
                    {
                        DrawOpaqueRendererList(context.renderContext, context.cmd, data.frameSettings, data.rendererList);
                    });
            }

            return shouldRenderMotionVectorAfterGBuffer;
        }

        void RenderObjectsMotionVectors(RenderGraph renderGraph, CullingResults cull, uint batchLayerMask, HDCamera hdCamera, in PrepassOutput output)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.ObjectMotionVectors) ||
                !hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects))
                return;

            using (var builder = renderGraph.AddRenderPass<DrawRendererListPassData>("Objects Motion Vectors Rendering", out var passData, ProfilingSampler.Get(HDProfileId.ObjectsMotionVector)))
            {
                // With all this variant we have the following scenario of render target binding
                // decalsEnabled
                //     LitShaderMode.Forward
                //         Range Opaque both deferred and forward - depth + optional msaa + motion + force zero decal + normal
                //         Range opaqueDecal for both deferred and forward - depth + optional msaa + motion + decal + normal
                //         Range opaqueAlphaTest for both deferred and forward - depth + optional msaa + motion + force zero decal + normal
                //         Range opaqueDecalAlphaTest for both deferred and forward - depth + optional msaa + motion + decal + normal
                //    LitShaderMode.Deferred
                //         Range Opaque for deferred - depth + motion + force zero decal
                //         Range opaqueDecal for deferred - depth + motion + decal
                //         Range opaqueAlphaTest for deferred - depth + motion + force zero decal
                //         Range opaqueDecalAlphaTes for deferred - depth + motion + decal

                //         Range Opaque for forward - depth + motion  + force zero decal + normal
                //         Range opaqueDecal for forward - depth + motion + decal + normal
                //         Range opaqueAlphaTest for forward - depth + motion + force zero decal + normal
                //         Range opaqueDecalAlphaTest for forward - depth + motion + decal + normal

                // !decalsEnabled
                //     LitShaderMode.Forward
                //         Range Opaque..OpaqueDecalAlphaTest for deferred and forward - depth + motion + optional msaa + normal
                //     LitShaderMode.Deferred
                //         Range Opaque..OpaqueDecalAlphaTest for deferred - depth + motion

                //         Range Opaque..OpaqueDecalAlphaTest for forward - depth + motion + normal


                // These flags are still required in SRP or the engine won't compute previous model matrices...
                // If the flag hasn't been set yet on this camera, motion vectors will skip a frame.
                hdCamera.camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;

                passData.frameSettings = hdCamera.frameSettings;
                builder.UseDepthBuffer(output.depthBuffer, DepthAccess.ReadWrite);
                BindMotionVectorPassColorBuffers(builder, output, hdCamera);

                RenderStateBlock? stateBlock = null;
                if (hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred || !hdCamera.msaaEnabled)
                    stateBlock = m_AlphaToMaskBlock;
                passData.rendererList = builder.UseRendererList(
                    renderGraph.CreateRendererList(CreateOpaqueRendererListDesc(cull, hdCamera.camera, HDShaderPassNames.s_MotionVectorsName, PerObjectData.MotionVectors, stateBlock: stateBlock, batchLayerMask: batchLayerMask)));

                builder.SetRenderFunc(
                    (DrawRendererListPassData data, RenderGraphContext context) =>
                    {
                        DrawOpaqueRendererList(context, data.frameSettings, data.rendererList);
                    });
            }
        }

        class GBufferPassData
        {
            public FrameSettings frameSettings;
            public RendererListHandle rendererList;
            public DBufferOutput dBuffer;
        }

        internal struct GBufferOutput
        {
            public TextureHandle[] mrt;
            public int gBufferCount;
            public int lightLayersTextureIndex;
            public int shadowMaskTextureIndex;
        }

        void SetupGBufferTargets(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle sssBuffer, TextureHandle vtFeedbackBuffer, ref PrepassOutput prepassOutput, FrameSettings frameSettings, RenderGraphBuilder builder)
        {
            prepassOutput.depthBuffer = builder.UseDepthBuffer(prepassOutput.depthBuffer, DepthAccess.ReadWrite);

            // If the gbuffer targets are already set up, then assume we are setting up for the second gbuffer pass when doing two-pass occlusion culling.
            // We want to continue to render to the same targets as the first pass in this case, so just mark them as used for this pass and early out.
            if (prepassOutput.gbuffer.gBufferCount != 0)
            {
                prepassOutput.gbuffer = WriteGBuffer(prepassOutput.gbuffer, builder);
                return;
            }

            bool clearGBuffer = NeedClearGBuffer(hdCamera);
            bool renderingLayers = frameSettings.IsEnabled(FrameSettingsField.LightLayers) || frameSettings.IsEnabled(FrameSettingsField.RenderingLayerMaskBuffer);
            bool shadowMasks = frameSettings.IsEnabled(FrameSettingsField.Shadowmask);

            int currentIndex = 0;
            prepassOutput.gbuffer.mrt[currentIndex] = builder.UseColorBuffer(sssBuffer, currentIndex++);
            prepassOutput.gbuffer.mrt[currentIndex] = builder.UseColorBuffer(prepassOutput.normalBuffer, currentIndex++);

#if UNITY_2020_2_OR_NEWER
            FastMemoryDesc gbufferFastMemDesc;
            gbufferFastMemDesc.inFastMemory = true;
            gbufferFastMemDesc.residencyFraction = 1.0f;
            gbufferFastMemDesc.flags = FastMemoryFlags.SpillTop;
#endif

            // If we are in deferred mode and the SSR is enabled, we need to make sure that the second gbuffer is cleared given that we are using that information for clear coat selection
            bool clearGBuffer2 = clearGBuffer || hdCamera.IsSSREnabled();
            prepassOutput.gbuffer.mrt[currentIndex] = builder.UseColorBuffer(renderGraph.CreateTexture(
                new TextureDesc(Vector2.one, true, true)
                {
                    format = GraphicsFormat.R8G8B8A8_UNorm,
                    clearBuffer = clearGBuffer2,
                    clearColor = Color.clear,
                    name = "GBuffer2"
#if UNITY_2020_2_OR_NEWER
                    , fastMemoryDesc = gbufferFastMemDesc
#endif
                }), currentIndex++);
            prepassOutput.gbuffer.mrt[currentIndex] = builder.UseColorBuffer(renderGraph.CreateTexture(
                new TextureDesc(Vector2.one, true, true)
                {
                    format = Builtin.GetLightingBufferFormat(),
                    clearBuffer = clearGBuffer,
                    clearColor = Color.clear,
                    name = "GBuffer3"
#if UNITY_2020_2_OR_NEWER
                    , fastMemoryDesc = gbufferFastMemDesc
#endif
                }), currentIndex++);

#if ENABLE_VIRTUALTEXTURES
            prepassOutput.gbuffer.mrt[currentIndex] = builder.UseColorBuffer(vtFeedbackBuffer, currentIndex++);
#endif

            prepassOutput.gbuffer.lightLayersTextureIndex = -1;
            prepassOutput.gbuffer.shadowMaskTextureIndex = -1;
            if (renderingLayers)
            {
                prepassOutput.gbuffer.mrt[currentIndex] = builder.UseColorBuffer(prepassOutput.renderingLayersBuffer, currentIndex);
                prepassOutput.gbuffer.lightLayersTextureIndex = currentIndex++;
            }
            if (shadowMasks)
            {
                prepassOutput.gbuffer.mrt[currentIndex] = builder.UseColorBuffer(renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one, true, true) { format = Builtin.GetShadowMaskBufferFormat(), clearBuffer = clearGBuffer, clearColor = Color.clear, name = "ShadowMasks" }), currentIndex);
                prepassOutput.gbuffer.shadowMaskTextureIndex = currentIndex++;
            }

            prepassOutput.gbuffer.gBufferCount = currentIndex;
        }

        // TODO RENDERGRAPH: For now we just bind globally for GBuffer/Forward passes.
        // We need to find a nice way to invalidate this kind of buffers when they should not be used anymore (after the last read).
        static void BindDBufferGlobalData(in DBufferOutput dBufferOutput, in RenderGraphContext ctx)
        {
            for (int i = 0; i < dBufferOutput.dBufferCount; ++i)
                ctx.cmd.SetGlobalTexture(HDShaderIDs._DBufferTexture[i], dBufferOutput.mrt[i]);
        }

        static GBufferOutput ReadGBuffer(GBufferOutput gBufferOutput, RenderGraphBuilder builder)
        {
            // We do the reads "in place" because we don't want to allocate a struct with dynamic arrays each time we do that and we want to keep loops for code sanity.
            for (int i = 0; i < gBufferOutput.gBufferCount; ++i)
                gBufferOutput.mrt[i] = builder.ReadTexture(gBufferOutput.mrt[i]);

            return gBufferOutput;
        }

        static GBufferOutput WriteGBuffer(GBufferOutput gBufferOutput, RenderGraphBuilder builder)
        {
            // We do the reads "in place" because we don't want to allocate a struct with dynamic arrays each time we do that and we want to keep loops for code sanity.
            for (int i = 0; i < gBufferOutput.gBufferCount; ++i)
                gBufferOutput.mrt[i] = builder.UseColorBuffer(gBufferOutput.mrt[i], i);

            return gBufferOutput;
        }

        // RenderGBuffer do the gbuffer pass. This is only called with deferred. If we use a depth prepass, then the depth prepass will perform the alpha testing for opaque alpha tested and we don't need to do it anymore
        // during Gbuffer pass. This is handled in the shader and the depth test (equal and no depth write) is done here.
        void RenderGBuffer(RenderGraph renderGraph, TextureHandle sssBuffer, TextureHandle vtFeedbackBuffer, ref PrepassOutput prepassOutput, CullingResults cull, uint batchLayerMask, HDCamera hdCamera)
        {
            if (hdCamera.frameSettings.litShaderMode != LitShaderMode.Deferred ||
                !hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects))
            {
                prepassOutput.gbuffer.gBufferCount = 0;
                return;
            }

            using (var builder = renderGraph.AddRenderPass<GBufferPassData>("GBuffer", out var passData, ProfilingSampler.Get(HDProfileId.GBuffer)))
            {
                builder.AllowRendererListCulling(false);

                FrameSettings frameSettings = hdCamera.frameSettings;

                passData.frameSettings = frameSettings;
                SetupGBufferTargets(renderGraph, hdCamera, sssBuffer, vtFeedbackBuffer, ref prepassOutput, frameSettings, builder);
                passData.rendererList = builder.UseRendererList(
                    renderGraph.CreateRendererList(CreateOpaqueRendererListDesc(cull, hdCamera.camera, HDShaderPassNames.s_GBufferName, m_CurrentRendererConfigurationBakedLighting,
                        batchLayerMask: batchLayerMask)));

                passData.dBuffer = ReadDBuffer(prepassOutput.dbuffer, builder);

                builder.SetRenderFunc(
                    (GBufferPassData data, RenderGraphContext context) =>
                    {
                        BindDBufferGlobalData(data.dBuffer, context);
                        DrawOpaqueRendererList(context, data.frameSettings, data.rendererList);
                    });
            }
        }

        class ThicknessPassData
        {
            public TextureHandle finalTextureArrayRT;
            public TextureHandle depthBuffer;
            public BufferHandle reindexMap;
            public HDCamera camera;

            public List<RendererListHandle> rendererLists;

            public int xrViewCount;
            public int xrViewIdx;
            public bool isXR;
            public bool isXRSinglePass;
            public bool readDepthBuffer;
        };

        bool IsComputeThicknessNeeded(HDCamera hdCamera)
        {
            FrameSettings frameSettings = hdCamera.frameSettings;

            return frameSettings.IsEnabled(FrameSettingsField.ComputeThickness) &&
                HDUtils.hdrpSettings.supportComputeThickness &&
                (int)HDUtils.hdrpSettings.computeThicknessLayerMask != 0;
        }

        TextureHandle CreateThicknessTexture(RenderGraph renderGraph, HDCamera hdCamera)
        {
            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;
            FrameSettings frameSettings = hdCamera.frameSettings;

            if (!IsComputeThicknessNeeded(hdCamera))
            {
                HDComputeThickness.Instance.SetTextureArray(renderGraph.defaultResources.blackTextureArrayXR);
                HDComputeThickness.Instance.SetReindexMap(m_ComputeThicknessReindexMap);
                return TextureHandle.nullHandle;
            }

            TextureHandle thicknessTexture;

            for (int i = 0; i < HDComputeThickness.computeThicknessMaxLayer; ++i)
            {
                m_ComputeThicknessReindexMapData[i] = HDComputeThickness.computeThicknessMaxLayer; // Incorrect index
                m_ComputeThicknessReindexSolver[i] = false;
                m_ComputeThicknessLayerMask[i] = 0;
            }

            uint requestedLayerMask = (uint)(int)currentAsset.currentPlatformRenderPipelineSettings.computeThicknessLayerMask;

            bool isXR = hdCamera.xr.enabled;
            int renderNeeded = 0;
            for (int layerIdx = 0; layerIdx < HDComputeThickness.computeThicknessMaxLayer; ++layerIdx)
            {
                if (((1u << layerIdx) & requestedLayerMask) != 0u)
                {
                    m_ComputeThicknessReindexMapData[layerIdx] = (uint)renderNeeded;
                    ++renderNeeded;
                }
            }

            if (renderNeeded == 0)
            {
                HDComputeThickness.Instance.SetTextureArray(renderGraph.defaultResources.blackTextureArrayXR);
                HDComputeThickness.Instance.SetReindexMap(m_ComputeThicknessReindexMap);
                return TextureHandle.nullHandle;
            }

            float downsizeScale;
            if (currentAsset.currentPlatformRenderPipelineSettings.computeThicknessResolution == ComputeThicknessResolution.Half)
                downsizeScale = 0.5f;
            else if (currentAsset.currentPlatformRenderPipelineSettings.computeThicknessResolution == ComputeThicknessResolution.Quarter)
                downsizeScale = 0.25f;
            else
                downsizeScale = 1.0f;

            int usedLayerCount = renderNeeded;
            if (isXR && hdCamera.xr.singlePassEnabled)
            {
                usedLayerCount = Mathf.Max(renderNeeded * hdCamera.xr.viewCount, 1);
            }

            // Red: Thickness world space, between [near; far] plane
            // Green: Overlap Count (possible memory footprint optim use second texture with R8_UInt Texture: 256 layers max)
            TextureDesc thicknessArrayRTDesc = new TextureDesc(Vector2.one * downsizeScale, true, false)
            {
                dimension = TextureDimension.Tex2DArray,
                format = GraphicsFormat.R16G16_SFloat,
                clearBuffer = true,
                clearColor = Color.black,
                slices = usedLayerCount,
                enableRandomWrite = true,
                name = "ThicknessArray"
            };
            thicknessTexture = renderGraph.CreateTexture(thicknessArrayRTDesc);

            m_ComputeThicknessReindexMap.SetData(m_ComputeThicknessReindexMapData);
            HDComputeThickness.Instance.SetTextureArray(thicknessTexture);
            HDComputeThickness.Instance.SetReindexMap(m_ComputeThicknessReindexMap);

            return thicknessTexture;
        }

        void RenderThickness(RenderGraph renderGraph, CullingResults cullingResults, TextureHandle thicknessTexture, TextureHandle depthBuffer, HDCamera hdCamera, RenderQueueRange renderQueue, bool readDepthBuffer)
        {
            HDRenderPipelineAsset currentAsset = HDRenderPipeline.currentAsset;
            FrameSettings frameSettings = hdCamera.frameSettings;

            if (!frameSettings.IsEnabled(FrameSettingsField.ComputeThickness) || !currentAsset.currentPlatformRenderPipelineSettings.supportComputeThickness)
            {
                HDComputeThickness.Instance.SetTextureArray(renderGraph.defaultResources.blackTextureArrayXR);
                HDComputeThickness.Instance.SetReindexMap(m_ComputeThicknessReindexMap);
                return;
            }

            uint requestedLayerMask = (uint)(int)currentAsset.currentPlatformRenderPipelineSettings.computeThicknessLayerMask;
            bool isXR = hdCamera.xr.enabled;

            using (var builder = renderGraph.AddRenderPass<ThicknessPassData>("ComputeThickness", out var passData, ProfilingSampler.Get(HDProfileId.ComputeThickness)))
            {
                builder.AllowPassCulling(false);
                builder.AllowRendererListCulling(false);

                float downsizeScale;
                if (currentAsset.currentPlatformRenderPipelineSettings.computeThicknessResolution == ComputeThicknessResolution.Half)
                    downsizeScale = 0.5f;
                else if (currentAsset.currentPlatformRenderPipelineSettings.computeThicknessResolution == ComputeThicknessResolution.Quarter)
                    downsizeScale = 0.25f;
                else
                    downsizeScale = 1.0f;

                for (int localViewId = 0; localViewId < m_MaxXRViewsCount; ++localViewId)
                {
                    m_ComputeThicknessOpaqueMaterial[localViewId].SetFloat(HDShaderIDs._DownsizeScale, Mathf.Round(1.0f / downsizeScale));
                    m_ComputeThicknessTransparentMaterial[localViewId].SetFloat(HDShaderIDs._DownsizeScale, Mathf.Round(1.0f / downsizeScale));
                }

                passData.isXR = isXR;
                passData.isXRSinglePass = hdCamera.xr.singlePassEnabled;
                passData.xrViewCount = (isXR && hdCamera.xr.singlePassEnabled) ? hdCamera.xr.viewCount : 1;
                passData.xrViewIdx = hdCamera.xr.multipassId;

                passData.readDepthBuffer = readDepthBuffer;
                if (readDepthBuffer)
                {
                    passData.depthBuffer = builder.ReadTexture(depthBuffer);
                }
                passData.finalTextureArrayRT = builder.WriteTexture(thicknessTexture);
                passData.reindexMap = renderGraph.ImportBuffer(m_ComputeThicknessReindexMap);

                passData.rendererLists = ListPool<RendererListHandle>.Get();
                passData.camera = hdCamera;

                for (int k = 0; k < HDComputeThickness.computeThicknessMaxLayer; ++k)
                {
                    if (m_ComputeThicknessReindexMapData[k] >= HDComputeThickness.computeThicknessMaxLayer)
                    {
                        continue;
                    }

                    int maxLoop = (isXR && hdCamera.xr.singlePassEnabled) ? hdCamera.xr.viewCount : 1;
                    // If XR create twice the same render lists for left & right eyes
                    for (int viewId = 0; viewId < maxLoop; ++viewId)
                    {
                        RendererUtils.RendererListDesc rendererListOpaque = new RendererUtils.RendererListDesc(m_ComputeThicknessShaderTags, cullingResults, hdCamera.camera)
                        {
                            rendererConfiguration = PerObjectData.None,
                            renderQueueRange = renderQueue,
                            sortingCriteria = SortingCriteria.BackToFront,
                            overrideMaterial = readDepthBuffer ? m_ComputeThicknessTransparentMaterial[viewId] : m_ComputeThicknessOpaqueMaterial[viewId],
                            overrideMaterialPassIndex = readDepthBuffer ? 1 : 0,
                            excludeObjectMotionVectors = false,
                            layerMask = (1 << k)
                        };

                        passData.rendererLists.Add(builder.UseRendererList(renderGraph.CreateRendererList(rendererListOpaque)));
                    }
                }

                builder.SetRenderFunc(
                    (ThicknessPassData data, RenderGraphContext ctx) =>
                    {
                        int idx = 0;
                        foreach (RendererListHandle rl in data.rendererLists)
                        {
                            int sliceIdx;
                            if (!data.isXR)
                            {
                                sliceIdx = idx;
                            }
                            else
                            {
                                // Single Pass
                                //      {Left:{Opaque, Transparent}, Right:{Opaque, Transparent}}
                                // Multi Pass (one eye per camera)
                                //      Left:{Opaque, Transparent} OR Right:{Opaque, Transparent}
                                if (data.isXRSinglePass)
                                    sliceIdx = idx / data.xrViewCount;
                                else
                                    sliceIdx = idx;
                            }
                            RTHandle rtHandle = (RTHandle)data.finalTextureArrayRT;
                            if (!data.isXR)
                            {
                                CoreUtils.SetRenderTarget(ctx.cmd, rtHandle, ClearFlag.None, Color.black, 0, CubemapFace.Unknown, sliceIdx);
                            }
                            else
                            {
                                if (data.isXRSinglePass)
                                {
                                    int viewId = idx % data.xrViewCount;
                                    CoreUtils.SetRenderTarget(ctx.cmd, rtHandle, ClearFlag.None, Color.black, 0, CubemapFace.Unknown, sliceIdx * data.xrViewCount + viewId);
                                }
                                else
                                {
                                    CoreUtils.SetRenderTarget(ctx.cmd, rtHandle, ClearFlag.None, Color.black, 0, CubemapFace.Unknown, sliceIdx);
                                }
                            }

                            CoreUtils.DrawRendererList(ctx.renderContext, ctx.cmd, rl);
                            ++idx;
                        }

                        ListPool<RendererListHandle>.Release(data.rendererLists);
                    });
            }
        }

        class ResolvePrepassData
        {
            public TextureHandle depthAsColorBufferMSAA;
            public TextureHandle normalBufferMSAA;
            public TextureHandle motionVectorBufferMSAA;
            public Material depthResolveMaterial;
            public int depthResolvePassIndex;
            public bool needMotionVectors;
        }

        void ResolvePrepassBuffers(RenderGraph renderGraph, HDCamera hdCamera, ref PrepassOutput output)
        {
            if (hdCamera.msaaSamples == MSAASamples.None)
            {
                output.resolvedNormalBuffer = output.normalBuffer;
                output.resolvedDepthBuffer = output.depthBuffer;
                output.resolvedMotionVectorsBuffer = output.motionVectorsBuffer;

                return;
            }

            using (var builder = renderGraph.AddRenderPass<ResolvePrepassData>("Resolve Prepass MSAA", out var passData))
            {
                // This texture stores a set of depth values that are required for evaluating a bunch of effects in MSAA mode (R = Samples Max Depth, G = Samples Min Depth, G =  Samples Average Depth)
                TextureHandle depthValuesBuffer = renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one, true, true) { format = GraphicsFormat.R32G32B32A32_SFloat, name = "DepthValuesBuffer" });

                passData.needMotionVectors = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MotionVectors);

                passData.depthResolveMaterial = m_MSAAResolveMaterial;
                passData.depthResolvePassIndex = SampleCountToPassIndex(hdCamera.msaaSamples);

                output.resolvedDepthBuffer = builder.UseDepthBuffer(CreateDepthBuffer(renderGraph, true, MSAASamples.None), DepthAccess.Write);
                output.depthValuesMSAA = builder.UseColorBuffer(depthValuesBuffer, 0);
                output.resolvedNormalBuffer = builder.UseColorBuffer(CreateNormalBuffer(renderGraph, hdCamera, MSAASamples.None), 1);

                if (passData.needMotionVectors)
                    output.resolvedMotionVectorsBuffer = builder.UseColorBuffer(CreateMotionVectorBuffer(renderGraph, false, MSAASamples.None), 2);
                else
                    output.resolvedMotionVectorsBuffer = TextureHandle.nullHandle;

                passData.normalBufferMSAA = builder.ReadTexture(output.normalBuffer);
                passData.depthAsColorBufferMSAA = builder.ReadTexture(output.depthAsColor);
                if (passData.needMotionVectors)
                    passData.motionVectorBufferMSAA = builder.ReadTexture(output.motionVectorsBuffer);

                builder.SetRenderFunc(
                    (ResolvePrepassData data, RenderGraphContext context) =>
                    {
                        data.depthResolveMaterial.SetTexture(HDShaderIDs._NormalTextureMS, data.normalBufferMSAA);
                        data.depthResolveMaterial.SetTexture(HDShaderIDs._DepthTextureMS, data.depthAsColorBufferMSAA);
                        if (data.needMotionVectors)
                        {
                            data.depthResolveMaterial.SetTexture(HDShaderIDs._MotionVectorTextureMS, data.motionVectorBufferMSAA);
                        }

                        CoreUtils.SetKeyword(context.cmd, "_HAS_MOTION_VECTORS", data.needMotionVectors);

                        context.cmd.DrawProcedural(Matrix4x4.identity, data.depthResolveMaterial, data.depthResolvePassIndex, MeshTopology.Triangles, 3, 1);
                    });
            }
        }

        class CopyDepthPassData
        {
            public TextureHandle inputDepth;
            public TextureHandle outputDepth;
            public GPUCopy GPUCopy;
            public int width;
            public int height;
        }

        void CopyDepthBufferIfNeeded(RenderGraph renderGraph, HDCamera hdCamera, ref PrepassOutput output)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects))
            {
                output.depthPyramidTexture = renderGraph.defaultResources.blackTextureXR;
                return;
            }

            if (!m_IsDepthBufferCopyValid)
            {
                using (var builder = renderGraph.AddRenderPass<CopyDepthPassData>("Copy depth buffer", out var passData, ProfilingSampler.Get(HDProfileId.CopyDepthBuffer)))
                {
                    var depthMipchainSize = hdCamera.depthMipChainSize;

                    //HACK - HACK - HACK - Do not remove, please take a gpu capture and analyze the placement of fences.
                    // Reason: The following issue occurs when Async compute for gpu light culling is enabled.
                    // In vulkan, dx12 and consoles the first read of a texture always triggers a depth decompression
                    // (in vulkan is seen as a vk event, in dx12 as a barrier, and in gnm as a straight up depth decompress compute job).
                    // Unfortunately, the current render graph implementation only see's the current texture as a read since the abstraction doesnt go too low.
                    // The GfxDevice has no context of passes so it can't put the barrier in the right spot... so for now hacking this by *assuming* this is the first read. :(
                    passData.inputDepth = builder.ReadWriteTexture(output.resolvedDepthBuffer);
                    //passData.inputDepth = builder.ReadTexture(output.resolvedDepthBuffer);

                    passData.outputDepth = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(depthMipchainSize.x, depthMipchainSize.y, true, true)
                        { format = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "CameraDepthBufferMipChain" }));

                    passData.GPUCopy = m_GPUCopy;
                    passData.width = hdCamera.actualWidth;
                    passData.height = hdCamera.actualHeight;

                    output.depthPyramidTexture = passData.outputDepth;

                    builder.SetRenderFunc(
                        (CopyDepthPassData data, RenderGraphContext context) =>
                        {
                            // TODO: maybe we don't actually need the top MIP level?
                            // That way we could avoid making the copy, and build the MIP hierarchy directly.
                            // The downside is that our SSR tracing accuracy would decrease a little bit.
                            // But since we never render SSR at full resolution, this may be acceptable.

                            // TODO: reading the depth buffer with a compute shader will cause it to decompress in place.
                            // On console, to preserve the depth test performance, we must NOT decompress the 'm_CameraDepthStencilBuffer' in place.
                            // We should call decompressDepthSurfaceToCopy() and decompress it to 'm_CameraDepthBufferMipChain'.
                            data.GPUCopy.SampleCopyChannel_xyzw2x(context.cmd, data.inputDepth, data.outputDepth, new RectInt(0, 0, data.width, data.height));
                        });
                }

                m_IsDepthBufferCopyValid = true;
            }
        }

        class ResolveStencilPassData
        {
            public HDCamera hdCamera;
            public ComputeShader resolveStencilCS;
            public bool resolveIsNecessary;
            public bool resolveOnly;

            public TextureHandle inputDepth;
            public TextureHandle resolvedStencil;
            public BufferHandle coarseStencilBuffer;
        }

        // This pass build the coarse stencil buffer if requested (i.e. when resolveOnly: false) and perform the MSAA resolve of the
        // full res stencil buffer if needed (a pass requires it and MSAA is on).
        void BuildCoarseStencilAndResolveIfNeeded(RenderGraph renderGraph, HDCamera hdCamera, bool resolveOnly, ref PrepassOutput output)
        {
            using (var builder = renderGraph.AddRenderPass<ResolveStencilPassData>("Resolve Stencil", out var passData, ProfilingSampler.Get(HDProfileId.BuildCoarseStencilAndResolveIfNeeded)))
            {
                bool MSAAEnabled = hdCamera.msaaEnabled;

                passData.hdCamera = hdCamera;
                passData.resolveOnly = resolveOnly;
                // With MSAA, the following features require a copy of the stencil, if none are active, no need to do the resolve.
                passData.resolveIsNecessary = (GetFeatureVariantsEnabled(hdCamera.frameSettings) || hdCamera.IsSSREnabled() || hdCamera.IsSSREnabled(transparent: true)) && MSAAEnabled;
                passData.resolveStencilCS = runtimeShaders.resolveStencilCS;
                passData.inputDepth = builder.ReadTexture(output.depthBuffer);
                passData.coarseStencilBuffer = builder.WriteBuffer(
                    renderGraph.CreateBuffer(new BufferDesc(HDUtils.DivRoundUp(m_MaxCameraWidth, 8) * HDUtils.DivRoundUp(m_MaxCameraHeight, 8) * m_MaxViewCount, sizeof(uint)) { name = "CoarseStencilBuffer" }));
                if (passData.resolveIsNecessary)
                    passData.resolvedStencil = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true) { format = GraphicsFormat.R8G8_UInt, enableRandomWrite = true, name = "StencilBufferResolved" }));
                else
                    passData.resolvedStencil = output.stencilBuffer;

                builder.SetRenderFunc(
                    (ResolveStencilPassData data, RenderGraphContext context) =>
                    {
                        if (data.resolveOnly && !data.resolveIsNecessary)
                            return;

                        ComputeShader cs = data.resolveStencilCS;
                        context.cmd.SetComputeBufferParam(cs, 0, HDShaderIDs._CoarseStencilBuffer, data.coarseStencilBuffer);
                        context.cmd.SetComputeTextureParam(cs, 0, HDShaderIDs._StencilTexture, data.inputDepth, 0, RenderTextureSubElement.Stencil);

                        if (data.resolveIsNecessary)
                            context.cmd.SetComputeTextureParam(cs, 0, HDShaderIDs._OutputStencilBuffer, data.resolvedStencil);

                        context.cmd.DisableKeyword(cs, new(cs, "MSAA2X"));
                        context.cmd.DisableKeyword(cs, new(cs, "MSAA4X"));
                        context.cmd.DisableKeyword(cs, new(cs, "MSAA8X"));
                        switch (data.hdCamera.msaaSamples)
                        {
                            case MSAASamples.MSAA2x:
                                context.cmd.EnableKeyword(cs, new(cs, "MSAA2X"));
                                break;
                            case MSAASamples.MSAA4x:
                                context.cmd.EnableKeyword(cs, new(cs, "MSAA4X"));
                                break;
                            case MSAASamples.MSAA8x:
                                context.cmd.EnableKeyword(cs, new(cs, "MSAA8X"));
                                break;
                        }

                        context.cmd.SetKeyword(cs, new(cs, "COARSE_STENCIL"), !data.resolveIsNecessary || !data.resolveOnly);
                        context.cmd.SetKeyword(cs, new(cs, "RESOLVE"), data.resolveIsNecessary);

                        int coarseStencilWidth = HDUtils.DivRoundUp(data.hdCamera.actualWidth, 8);
                        int coarseStencilHeight = HDUtils.DivRoundUp(data.hdCamera.actualHeight, 8);
                        context.cmd.DispatchCompute(cs, 0, coarseStencilWidth, coarseStencilHeight, data.hdCamera.viewCount);
                    });

                if (MSAAEnabled)
                    output.stencilBuffer = passData.resolvedStencil;
                output.coarseStencilBuffer = passData.coarseStencilBuffer;
            }
        }

        class RenderDBufferPassData
        {
            public RendererListHandle meshDecalsRendererList;
            public RendererListHandle vfxDecalsRendererList;
            public TextureHandle depthTexture;
            public TextureHandle decalBuffer;
        }

        internal struct DBufferOutput
        {
            public TextureHandle[] mrt;
            public int dBufferCount;
        }

        static string[] s_DBufferNames = { "DBuffer0", "DBuffer1", "DBuffer2", "DBuffer3" };

        static Color s_DBufferClearColor = new Color(0.0f, 0.0f, 0.0f, 1.0f);
        static Color s_DBufferClearColorNormal = new Color(0.5f, 0.5f, 0.5f, 1.0f); // for normals 0.5 is neutral
        static Color s_DBufferClearColorAOSBlend = new Color(1.0f, 1.0f, 1.0f, 1.0f);
        static Color[] s_DBufferClearColors = { s_DBufferClearColor, s_DBufferClearColorNormal, s_DBufferClearColor, s_DBufferClearColorAOSBlend };

        void SetupDBufferTargets(RenderGraph renderGraph, bool use4RTs, ref PrepassOutput output, RenderGraphBuilder builder)
        {
            GraphicsFormat[] rtFormat;
            Decal.GetMaterialDBufferDescription(out rtFormat);
            output.dbuffer.dBufferCount = use4RTs ? 4 : 3;

            // for alpha compositing, color is cleared to 0, alpha to 1
            // https://developer.nvidia.com/gpugems/GPUGems3/gpugems3_ch23.html
            for (int dbufferIndex = 0; dbufferIndex < output.dbuffer.dBufferCount; ++dbufferIndex)
            {
                output.dbuffer.mrt[dbufferIndex] = builder.UseColorBuffer(renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one, true, true) { format = rtFormat[dbufferIndex], name = s_DBufferNames[dbufferIndex], clearBuffer = true, clearColor = s_DBufferClearColors[dbufferIndex] }), dbufferIndex);
            }

            builder.UseDepthBuffer(output.resolvedDepthBuffer, DepthAccess.Write);
        }

        static DBufferOutput ReadDBuffer(DBufferOutput dBufferOutput, RenderGraphBuilder builder)
        {
            // We do the reads "in place" because we don't want to allocate a struct with dynamic arrays each time we do that and we want to keep loops for code sanity.
            for (int i = 0; i < dBufferOutput.dBufferCount; ++i)
                dBufferOutput.mrt[i] = builder.ReadTexture(dBufferOutput.mrt[i]);

            return dBufferOutput;
        }

        void RenderDBuffer(RenderGraph renderGraph, HDCamera hdCamera, ref PrepassOutput output, CullingResults cullingResults)
        {
            bool use4RTs = m_Asset.currentPlatformRenderPipelineSettings.decalSettings.perChannelMask;

            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals) ||
                !hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects))
            {
                // Return all black textures for default values.
                var blackTexture = renderGraph.defaultResources.blackTextureXR;
                output.depthPyramidTexture = blackTexture;
                output.dbuffer.dBufferCount = use4RTs ? 4 : 3;
                for (int i = 0; i < output.dbuffer.dBufferCount; ++i)
                    output.dbuffer.mrt[i] = blackTexture;
                return;
            }

            bool canReadBoundDepthBuffer = SystemInfo.graphicsDeviceType == GraphicsDeviceType.PlayStation4 ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.PlayStation5 ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.PlayStation5NGGC ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.XboxOne ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.XboxOneD3D12 ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.GameCoreXboxOne ||
                SystemInfo.graphicsDeviceType == GraphicsDeviceType.GameCoreXboxSeries;

            if (!canReadBoundDepthBuffer)
            {
                // We need to copy depth buffer texture if we want to bind it at this stage
                CopyDepthBufferIfNeeded(renderGraph, hdCamera, ref output);
            }

            // If we have an incomplete depth buffer use for decal we will need to do another copy
            // after the rendering of the GBuffer
            if ((hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred) &&
                !hdCamera.frameSettings.IsEnabled(FrameSettingsField.DepthPrepassWithDeferredRendering))
                m_IsDepthBufferCopyValid = false;

            using (var builder = renderGraph.AddRenderPass<RenderDBufferPassData>("DBufferRender", out var passData, ProfilingSampler.Get(HDProfileId.DBufferRender)))
            {
                builder.AllowRendererListCulling(false);

                passData.meshDecalsRendererList = builder.UseRendererList(renderGraph.CreateRendererList(new RendererUtils.RendererListDesc(m_MeshDecalsPassNames, cullingResults, hdCamera.camera)
                {
                    sortingCriteria = HDUtils.k_OpaqueSortingCriteria | SortingCriteria.RendererPriority,
                    rendererConfiguration = PerObjectData.None,
                    renderQueueRange = HDRenderQueue.k_RenderQueue_AllOpaque
                }));

                passData.vfxDecalsRendererList = builder.UseRendererList(renderGraph.CreateRendererList(
                    new RendererUtils.RendererListDesc(m_VfxDecalsPassNames, cullingResults, hdCamera.camera)
                    {
                        sortingCriteria = HDUtils.k_OpaqueSortingCriteria & ~SortingCriteria.OptimizeStateChanges,
                        rendererConfiguration = PerObjectData.None,
                        renderQueueRange = HDRenderQueue.k_RenderQueue_AllOpaque,
                    }));

                SetupDBufferTargets(renderGraph, use4RTs, ref output, builder);
                passData.decalBuffer = hdCamera.frameSettings.IsEnabled(FrameSettingsField.DecalLayers) ? builder.ReadTexture(output.renderingLayersBuffer) : renderGraph.defaultResources.blackTextureXR;
                passData.depthTexture = canReadBoundDepthBuffer ? builder.ReadTexture(output.resolvedDepthBuffer) : builder.ReadTexture(output.depthPyramidTexture);

                builder.SetRenderFunc(
                    (RenderDBufferPassData data, RenderGraphContext context) =>
                    {
                        context.cmd.SetGlobalTexture(HDShaderIDs._DecalPrepassTexture, data.decalBuffer);
                        context.cmd.SetGlobalTexture(HDShaderIDs._CameraDepthTexture, data.depthTexture);

                        CoreUtils.DrawRendererList(context.renderContext, context.cmd, data.meshDecalsRendererList);
                        CoreUtils.DrawRendererList(context.renderContext, context.cmd, data.vfxDecalsRendererList);
                        DecalSystem.instance.RenderIntoDBuffer(context.cmd);

                        context.cmd.ClearRandomWriteTargets();
                    });
            }
        }

        class DBufferNormalPatchData
        {
            public Material decalNormalBufferMaterial;
            public int dBufferCount;
            public int stencilRef;
            public int stencilMask;

            public DBufferOutput dBuffer;
            public TextureHandle depthStencilBuffer;
            public TextureHandle normalBuffer;
        }

        void DecalNormalPatch(RenderGraph renderGraph, HDCamera hdCamera, ref PrepassOutput output)
        {
            // Integrated Intel GPU on Mac don't support the texture format use for normal (RGBA_8UNORM) for SetRandomWriteTarget
            // So on Metal for now we don't patch normal buffer if we detect an intel GPU
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal && SystemInfo.graphicsDeviceVendorID == kIntelVendorId)
            {
                return;
            }

            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals) &&
                hdCamera.msaaSamples == MSAASamples.None && // MSAA not supported
                hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects))
            {
                using (var builder = renderGraph.AddRenderPass<DBufferNormalPatchData>("DBuffer Normal (forward)", out var passData, ProfilingSampler.Get(HDProfileId.DBufferNormal)))
                {
                    passData.dBufferCount = m_Asset.currentPlatformRenderPipelineSettings.decalSettings.perChannelMask ? 4 : 3;
                    passData.decalNormalBufferMaterial = m_DecalNormalBufferMaterial;
                    switch (hdCamera.frameSettings.litShaderMode)
                    {
                        case LitShaderMode.Forward:  // in forward rendering all pixels that decals wrote into have to be composited
                            passData.stencilMask = (int)StencilUsage.Decals;
                            passData.stencilRef = (int)StencilUsage.Decals;
                            break;
                        case LitShaderMode.Deferred: // in deferred rendering only pixels affected by both forward materials and decals need to be composited
                            passData.stencilMask = (int)StencilUsage.Decals | (int)StencilUsage.RequiresDeferredLighting;
                            passData.stencilRef = (int)StencilUsage.Decals;
                            break;
                        default:
                            throw new System.ArgumentOutOfRangeException("Unknown ShaderLitMode");
                    }

                    passData.dBuffer = ReadDBuffer(output.dbuffer, builder);
                    passData.normalBuffer = builder.WriteTexture(output.resolvedNormalBuffer);
                    passData.depthStencilBuffer = builder.ReadTexture(output.resolvedDepthBuffer);

                    builder.SetRenderFunc(
                        (DBufferNormalPatchData data, RenderGraphContext ctx) =>
                        {
                            data.decalNormalBufferMaterial.SetInt(HDShaderIDs._DecalNormalBufferStencilReadMask, data.stencilMask);
                            data.decalNormalBufferMaterial.SetInt(HDShaderIDs._DecalNormalBufferStencilRef, data.stencilRef);
                            for (int i = 0; i < data.dBufferCount; ++i)
                                data.decalNormalBufferMaterial.SetTexture(HDShaderIDs._DBufferTexture[i], data.dBuffer.mrt[i]);

                            CoreUtils.SetRenderTarget(ctx.cmd, data.depthStencilBuffer);
                            ctx.cmd.SetRandomWriteTarget(1, data.normalBuffer);
                            ctx.cmd.DrawProcedural(Matrix4x4.identity, data.decalNormalBufferMaterial, 0, MeshTopology.Triangles, 3, 1);
                            ctx.cmd.ClearRandomWriteTargets();
                        });
                }
            }
        }

        class DownsampleDepthForLowResPassData
        {
            public bool useGatherDownsample;
            public float downsampleScale;
            public Vector2Int loadOffset;
            public Material downsampleDepthMaterial;
            public TextureHandle depthTexture;
            public TextureHandle downsampledDepthBuffer;
            public Rect viewport;
        }

        internal int RequiredCheckerboardMipCountInDepthPyramid(HDCamera hdCamera)
        {
            int mipCount = 0;

            // lowres transparency needs 1 mip
            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.LowResTransparent)
                && hdCamera.isLowResScaleHalf
                && m_Asset.currentPlatformRenderPipelineSettings.lowresTransparentSettings.checkerboardDepthBuffer)
            {
                mipCount = Mathf.Max(mipCount, 1);
            }

            // Volumetric clouds need 1 mip
            if (VolumetricCloudsSystem.HasVolumetricClouds(hdCamera))
            {
                mipCount = Mathf.Max(mipCount, 1);
            }

            return mipCount;
        }

        void DownsampleDepthForLowResTransparency(RenderGraph renderGraph, HDCamera hdCamera, ref PrepassOutput output)
        {
            // If the depth buffer hasn't been already copied by the decal depth buffer pass, then we do the copy here.
            CopyDepthBufferIfNeeded(renderGraph, hdCamera, ref output);

            using (var builder = renderGraph.AddRenderPass<DownsampleDepthForLowResPassData>("Downsample Depth Buffer for Low Res Transparency", out var passData, ProfilingSampler.Get(HDProfileId.DownsampleDepth)))
            {
                passData.useGatherDownsample = false;
                if (hdCamera.isLowResScaleHalf)
                {
                    passData.downsampleDepthMaterial = m_DownsampleDepthMaterialLoad;
                    passData.loadOffset = m_Asset.currentPlatformRenderPipelineSettings.lowresTransparentSettings.checkerboardDepthBuffer
                        ? hdCamera.depthBufferMipChainInfo.mipLevelOffsetsCheckerboard[1]
                        : hdCamera.depthBufferMipChainInfo.mipLevelOffsets[1];
                }
                else
                {
                    passData.downsampleDepthMaterial = m_DownsampleDepthMaterialGather;
                    passData.useGatherDownsample = true;
                }
                passData.downsampleScale = hdCamera.lowResScale;
                passData.viewport = hdCamera.lowResViewport;
                passData.depthTexture = builder.ReadTexture(output.depthPyramidTexture);

                passData.downsampledDepthBuffer = builder.UseDepthBuffer(renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one * hdCamera.lowResScale, true, true) { format = CoreUtils.GetDefaultDepthStencilFormat(), name = "LowResDepthBuffer" }), DepthAccess.Write);

                builder.SetRenderFunc(
                    (DownsampleDepthForLowResPassData data, RenderGraphContext context) =>
                    {
                        Vector4 scaleBias = Vector4.zero;
                        if (data.useGatherDownsample)
                        {
                            float downsampleScaleInv = 1.0f / data.downsampleScale;
                            RenderTexture srcTexture = data.depthTexture;
                            RenderTexture destTexture = data.downsampledDepthBuffer;
                            scaleBias.x = ((float)destTexture.width / (float)srcTexture.width) * downsampleScaleInv;
                            scaleBias.y = ((float)destTexture.height / (float)srcTexture.height) * downsampleScaleInv;
                        }
                        else
                        {
                            scaleBias.z = data.loadOffset.x;
                            scaleBias.w = data.loadOffset.y;
                        }
                        context.cmd.SetGlobalVector(HDShaderIDs._ScaleBias, scaleBias);

                        context.cmd.SetViewport(data.viewport);
                        context.cmd.DrawProcedural(Matrix4x4.identity, data.downsampleDepthMaterial, 0, MeshTopology.Triangles, 3, 1, null);
                    });

                output.downsampledDepthBuffer = passData.downsampledDepthBuffer;
            }
        }

        class GenerateDepthPyramidPassData
        {
            public TextureHandle depthTexture;
            public HDUtils.PackedMipChainInfo mipInfo;
            public MipGenerator mipGenerator;
        }

        void GenerateDepthPyramid(RenderGraph renderGraph, HDCamera hdCamera, ref PrepassOutput output)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects))
            {
                output.depthPyramidTexture = renderGraph.defaultResources.blackTextureXR;
                return;
            }

            // If the depth buffer hasn't been already copied by the decal or low res depth buffer pass, then we do the copy here.
            CopyDepthBufferIfNeeded(renderGraph, hdCamera, ref output);

            using (var builder = renderGraph.AddRenderPass<GenerateDepthPyramidPassData>("Generate Depth Buffer MIP Chain", out var passData, ProfilingSampler.Get(HDProfileId.DepthPyramid)))
            {
                passData.depthTexture = builder.WriteTexture(output.depthPyramidTexture);
                passData.mipInfo = hdCamera.depthBufferMipChainInfo;
                passData.mipGenerator = m_MipGenerator;

                builder.SetRenderFunc(
                    (GenerateDepthPyramidPassData data, RenderGraphContext context) =>
                    {
                        data.mipGenerator.RenderMinDepthPyramid(context.cmd, data.depthTexture, data.mipInfo);
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
            public TextureHandle motionVectorsBuffer;
            public TextureHandle depthBuffer;
        }

        void RenderCameraMotionVectors(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthBuffer, TextureHandle motionVectorsBuffer)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.MotionVectors))
                return;

            using (var builder = renderGraph.AddRenderPass<CameraMotionVectorsPassData>("Camera Motion Vectors Rendering", out var passData, ProfilingSampler.Get(HDProfileId.CameraMotionVectors)))
            {
                // These flags are still required in SRP or the engine won't compute previous model matrices...
                // If the flag hasn't been set yet on this camera, motion vectors will skip a frame.
                hdCamera.camera.depthTextureMode |= DepthTextureMode.MotionVectors | DepthTextureMode.Depth;

                passData.cameraMotionVectorsMaterial = m_CameraMotionVectorsMaterial;
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.Read);
                passData.motionVectorsBuffer = builder.WriteTexture(motionVectorsBuffer);

                builder.SetRenderFunc(
                    (CameraMotionVectorsPassData data, RenderGraphContext context) =>
                    {
                        data.cameraMotionVectorsMaterial.SetInt(HDShaderIDs._StencilMask, (int)StencilUsage.ObjectMotionVector);
                        data.cameraMotionVectorsMaterial.SetInt(HDShaderIDs._StencilRef, (int)StencilUsage.ObjectMotionVector);
                        HDUtils.DrawFullScreen(context.cmd, data.cameraMotionVectorsMaterial, data.motionVectorsBuffer, data.depthBuffer, null, 0);
                    });
            }
        }
    }
}
