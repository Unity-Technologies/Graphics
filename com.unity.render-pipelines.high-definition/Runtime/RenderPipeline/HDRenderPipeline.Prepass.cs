using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        Material m_DepthResolveMaterial;
        Material m_CameraMotionVectorsMaterial;
        Material m_DecalNormalBufferMaterial;
        Material m_DownsampleDepthMaterialHalfresCheckerboard;
        Material m_DownsampleDepthMaterialGather;

        // Need to cache to avoid alloc of arrays...
        GBufferOutput m_GBufferOutput;
        DBufferOutput m_DBufferOutput;

        GPUCopy m_GPUCopy;

        void InitializePrepass(HDRenderPipelineAsset hdAsset)
        {
            m_DepthResolveMaterial = CoreUtils.CreateEngineMaterial(asset.renderPipelineResources.shaders.depthValuesPS);
            m_CameraMotionVectorsMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.cameraMotionVectorsPS);
            m_DecalNormalBufferMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.decalNormalBufferPS);
            m_DownsampleDepthMaterialHalfresCheckerboard = CoreUtils.CreateEngineMaterial(defaultResources.shaders.downsampleDepthPS);
            m_DownsampleDepthMaterialGather = CoreUtils.CreateEngineMaterial(defaultResources.shaders.downsampleDepthPS);
            m_DownsampleDepthMaterialGather.EnableKeyword("GATHER_DOWNSAMPLE");

            m_GBufferOutput = new GBufferOutput();
            m_GBufferOutput.mrt = new TextureHandle[RenderGraph.kMaxMRTCount];

            m_DBufferOutput = new DBufferOutput();
            m_DBufferOutput.mrt = new TextureHandle[(int)Decal.DBufferMaterial.Count];

            m_GPUCopy = new GPUCopy(defaultResources.shaders.copyChannelCS);
        }

        void CleanupPrepass()
        {
            CoreUtils.Destroy(m_DepthResolveMaterial);
            CoreUtils.Destroy(m_CameraMotionVectorsMaterial);
            CoreUtils.Destroy(m_DecalNormalBufferMaterial);
            CoreUtils.Destroy(m_DownsampleDepthMaterialHalfresCheckerboard);
            CoreUtils.Destroy(m_DownsampleDepthMaterialGather);
        }

        bool NeedClearGBuffer(HDCamera hdCamera)
        {
            return m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled() || hdCamera.frameSettings.IsEnabled(FrameSettingsField.ClearGBuffers);
        }

        struct PrepassOutput
        {
            // Buffers that may be output by the prepass.
            // They will be MSAA depending on the frame settings
            public TextureHandle depthBuffer;
            public TextureHandle depthAsColor;
            public TextureHandle normalBuffer;
            public TextureHandle motionVectorsBuffer;

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
            public ComputeBufferHandle coarseStencilBuffer;
        }

        TextureHandle CreateDepthBuffer(RenderGraph renderGraph, bool clear, MSAASamples msaaSamples)
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
                depthBufferBits = DepthBits.Depth32,
                bindTextureMS = msaa,
                msaaSamples = msaaSamples,
                clearBuffer = clear,
                name = msaa ? "CameraDepthStencilMSAA" : "CameraDepthStencil"
#if UNITY_2020_2_OR_NEWER
                , fastMemoryDesc = fastMemDesc
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
                colorFormat = GraphicsFormat.R8G8B8A8_UNorm,
                clearBuffer = NeedClearGBuffer(hdCamera),
                clearColor = Color.black,
                bindTextureMS = msaa,
                msaaSamples = msaaSamples,
                enableRandomWrite = !msaa,
                name = msaa ? "NormalBufferMSAA" : "NormalBuffer"
#if UNITY_2020_2_OR_NEWER
                , fastMemoryDesc = fastMemDesc
#endif
                ,
                fallBackToBlackTexture = true
            };
            return renderGraph.CreateTexture(normalDesc);
        }

        TextureHandle CreateDecalPrepassBuffer(RenderGraph renderGraph, MSAASamples msaaSamples)
        {
            bool msaa = msaaSamples != MSAASamples.None;
            TextureDesc decalDesc = new TextureDesc(Vector2.one, true, true)
            { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, clearBuffer = true, clearColor = Color.clear, bindTextureMS = false, msaaSamples = msaaSamples, enableRandomWrite = !msaa, name = msaa ? "DecalPrepassBufferMSAA" : "DecalPrepassBuffer" };
            return renderGraph.CreateTexture(decalDesc);
        }

        TextureHandle CreateDepthAsColorBuffer(RenderGraph renderGraph, MSAASamples msaaSamples)
        {
            return renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            { colorFormat = GraphicsFormat.R32_SFloat, clearBuffer = true, clearColor = Color.black, bindTextureMS = true, msaaSamples = msaaSamples, name = "DepthAsColorMSAA" });
        }

        TextureHandle CreateMotionVectorBuffer(RenderGraph renderGraph, bool clear, MSAASamples msaaSamples)
        {
            bool msaa = msaaSamples != MSAASamples.None;
            TextureDesc motionVectorDesc = new TextureDesc(Vector2.one, true, true)
            { colorFormat = Builtin.GetMotionVectorFormat(), bindTextureMS = msaa, msaaSamples = msaaSamples, clearBuffer = clear, clearColor = Color.clear, name = msaa ? "Motion Vectors MSAA" : "Motion Vectors" };
            return renderGraph.CreateTexture(motionVectorDesc);
        }

        void BindMotionVectorPassColorBuffers(in RenderGraphBuilder builder, in PrepassOutput prepassOutput, TextureHandle decalBuffer, HDCamera hdCamera)
        {
            bool msaa = hdCamera.msaaSamples != MSAASamples.None;
            bool decalLayerEnabled = hdCamera.frameSettings.IsEnabled(FrameSettingsField.DecalLayers);

            int index = 0;
            if (msaa)
                builder.UseColorBuffer(prepassOutput.depthAsColor, index++);
            builder.UseColorBuffer(prepassOutput.motionVectorsBuffer, index++);
            if (decalLayerEnabled)
                builder.UseColorBuffer(decalBuffer, index++);
            builder.UseColorBuffer(prepassOutput.normalBuffer, index++);
        }

        PrepassOutput RenderPrepass(RenderGraph renderGraph,
            TextureHandle colorBuffer,
            TextureHandle sssBuffer,
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
            bool clearMotionVectors = hdCamera.camera.cameraType == CameraType.SceneView && !hdCamera.animateMaterials;
            bool motionVectors = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MotionVectors);

            // TODO: See how to clean this. Some buffers are created outside, some inside functions...
            result.motionVectorsBuffer = motionVectors ? CreateMotionVectorBuffer(renderGraph, msaa, hdCamera.msaaSamples) : renderGraph.defaultResources.blackTextureXR;
            result.depthBuffer = CreateDepthBuffer(renderGraph, hdCamera.clearDepth, hdCamera.msaaSamples);
            result.stencilBuffer = result.depthBuffer;

            RenderXROcclusionMeshes(renderGraph, hdCamera, colorBuffer, result.depthBuffer);

            using (new XRSinglePassScope(renderGraph, hdCamera))
            {
                RenderCustomPass(renderGraph, hdCamera, colorBuffer, result, customPassCullingResults, cullingResults, CustomPassInjectionPoint.BeforeRendering, aovRequest, aovBuffers);

                RenderRayTracingDepthPrepass(renderGraph, cullingResults, hdCamera, result.depthBuffer);

                ApplyCameraMipBias(hdCamera);

                bool shouldRenderMotionVectorAfterGBuffer = RenderDepthPrepass(renderGraph, cullingResults, hdCamera, ref result, out var decalBuffer);

                ResetCameraMipBias(hdCamera);

                if (!shouldRenderMotionVectorAfterGBuffer)
                {
                    // If objects motion vectors are enabled, this will render the objects with motion vector into the target buffers (in addition to the depth)
                    // Note: An object with motion vector must not be render in the prepass otherwise we can have motion vector write that should have been rejected
                    RenderObjectsMotionVectors(renderGraph, cullingResults, hdCamera, decalBuffer, result);
                }

                // If we have MSAA, we need to complete the motion vector buffer before buffer resolves, hence we need to run camera mv first.
                // This is always fine since shouldRenderMotionVectorAfterGBuffer is always false for forward.
                bool needCameraMVBeforeResolve = msaa;
                if (needCameraMVBeforeResolve)
                {
                    RenderCameraMotionVectors(renderGraph, hdCamera, result.depthBuffer, result.motionVectorsBuffer);
                }

                PreRenderSky(renderGraph, hdCamera, colorBuffer, result.depthBuffer, result.normalBuffer);

                PreRenderVolumetricClouds(renderGraph, hdCamera);

                // At this point in forward all objects have been rendered to the prepass (depth/normal/motion vectors) so we can resolve them
                ResolvePrepassBuffers(renderGraph, hdCamera, ref result);

                ApplyCameraMipBias(hdCamera);

                RenderDBuffer(renderGraph, hdCamera, decalBuffer, ref result, cullingResults);

                RenderGBuffer(renderGraph, sssBuffer, vtFeedbackBuffer, ref result, cullingResults, hdCamera);

                DecalNormalPatch(renderGraph, hdCamera, ref result);

                // After Depth and Normals/roughness including decals
                bool depthBufferModified = RenderCustomPass(renderGraph, hdCamera, colorBuffer, result, customPassCullingResults, cullingResults, CustomPassInjectionPoint.AfterOpaqueDepthAndNormal, aovRequest, aovBuffers);

                // If the depth was already copied in RenderDBuffer, we force the copy again because the custom pass modified the depth.
                if (depthBufferModified)
                    m_IsDepthBufferCopyValid = false;

                // Only on consoles is safe to read and write from/to the depth atlas
                bool mip1FromDownsampleForLowResTrans = SystemInfo.graphicsDeviceType == GraphicsDeviceType.PlayStation4 ||
                    SystemInfo.graphicsDeviceType == GraphicsDeviceType.PlayStation5 ||
                    SystemInfo.graphicsDeviceType == GraphicsDeviceType.PlayStation5NGGC ||
                    SystemInfo.graphicsDeviceType == GraphicsDeviceType.XboxOne ||
                    SystemInfo.graphicsDeviceType == GraphicsDeviceType.XboxOneD3D12 ||
                    SystemInfo.graphicsDeviceType == GraphicsDeviceType.GameCoreXboxOne ||
                    SystemInfo.graphicsDeviceType == GraphicsDeviceType.GameCoreXboxSeries;

                mip1FromDownsampleForLowResTrans = mip1FromDownsampleForLowResTrans && hdCamera.frameSettings.IsEnabled(FrameSettingsField.LowResTransparent) && hdCamera.isLowResScaleHalf;

                ResetCameraMipBias(hdCamera);

                DownsampleDepthForLowResTransparency(renderGraph, hdCamera, mip1FromDownsampleForLowResTrans, ref result);

                // In both forward and deferred, everything opaque should have been rendered at this point so we can safely copy the depth buffer for later processing.
                GenerateDepthPyramid(renderGraph, hdCamera, mip1FromDownsampleForLowResTrans, ref result);

                if (shouldRenderMotionVectorAfterGBuffer)
                {
                    // See the call RenderObjectsMotionVectors() above and comment
                    RenderObjectsMotionVectors(renderGraph, cullingResults, hdCamera, decalBuffer, result);
                }

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
            if (!recursiveSettings.enable.value)
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
        bool RenderDepthPrepass(RenderGraph renderGraph, CullingResults cull, HDCamera hdCamera, ref PrepassOutput output, out TextureHandle decalBuffer)
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

            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.OpaqueObjects))
            {
                decalBuffer = renderGraph.defaultResources.blackTextureXR;
                output.depthAsColor = CreateDepthAsColorBuffer(renderGraph, hdCamera.msaaSamples);
                output.normalBuffer = CreateNormalBuffer(renderGraph, hdCamera, hdCamera.msaaSamples);

                return false;
            }

            bool msaa = hdCamera.msaaEnabled;
            bool decalLayersEnabled = hdCamera.frameSettings.IsEnabled(FrameSettingsField.DecalLayers);
            bool decalsEnabled = hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals);
            bool fullDeferredPrepass = hdCamera.frameSettings.IsEnabled(FrameSettingsField.DepthPrepassWithDeferredRendering);
            // To avoid rendering objects twice (once in the depth pre-pass and once in the motion vector pass when the motion vector pass is enabled) we exclude the objects that have motion vectors.
            bool objectMotionEnabled = hdCamera.frameSettings.IsEnabled(FrameSettingsField.ObjectMotionVectors);
            bool excludeMotion = fullDeferredPrepass ? objectMotionEnabled : false;
            bool shouldRenderMotionVectorAfterGBuffer = (hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred) && !fullDeferredPrepass;

            // Needs to be created ahead because it's used in both pre-passes.
            if (decalLayersEnabled)
                decalBuffer = CreateDecalPrepassBuffer(renderGraph, hdCamera.msaaSamples);
            else
                decalBuffer = renderGraph.defaultResources.blackTextureXR;

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
                        excludeObjectMotionVectors: excludeMotion)));

                    output.depthBuffer = builder.UseDepthBuffer(output.depthBuffer, DepthAccess.ReadWrite);
                    if (decalLayersEnabled)
                        decalBuffer = builder.UseColorBuffer(decalBuffer, 0);

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

                output.depthBuffer = builder.UseDepthBuffer(output.depthBuffer, DepthAccess.ReadWrite);
                int mrtIndex = 0;
                if (msaa)
                    output.depthAsColor = builder.UseColorBuffer(CreateDepthAsColorBuffer(renderGraph, hdCamera.msaaSamples), mrtIndex++);
                output.normalBuffer = builder.UseColorBuffer(CreateNormalBuffer(renderGraph, hdCamera, hdCamera.msaaSamples), mrtIndex++);

                if (decalLayersEnabled)
                    decalBuffer = builder.UseColorBuffer(decalBuffer, mrtIndex++);

                if (hdCamera.frameSettings.litShaderMode == LitShaderMode.Forward)
                {
                    RenderStateBlock? stateBlock = null;
                    if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.AlphaToMask))
                        stateBlock = m_AlphaToMaskBlock;

                    passData.rendererList = builder.UseRendererList(renderGraph.CreateRendererList(
                        CreateOpaqueRendererListDesc(cull, hdCamera.camera, m_DepthOnlyAndDepthForwardOnlyPassNames, stateBlock: stateBlock, excludeObjectMotionVectors: objectMotionEnabled)));
                }
                else if (hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred)
                {
                    // Forward only material that output normal buffer
                    passData.rendererList = builder.UseRendererList(renderGraph.CreateRendererList(
                        CreateOpaqueRendererListDesc(cull, hdCamera.camera, m_DepthForwardOnlyPassNames, stateBlock: m_AlphaToMaskBlock, excludeObjectMotionVectors: excludeMotion)));
                }

                builder.SetRenderFunc(
                    (DrawRendererListPassData data, RenderGraphContext context) =>
                    {
                        DrawOpaqueRendererList(context.renderContext, context.cmd, data.frameSettings, data.rendererList);
                    });
            }

            return shouldRenderMotionVectorAfterGBuffer;
        }

        void RenderObjectsMotionVectors(RenderGraph renderGraph, CullingResults cull, HDCamera hdCamera, TextureHandle decalBuffer, in PrepassOutput output)
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
                BindMotionVectorPassColorBuffers(builder, output, decalBuffer, hdCamera);

                RenderStateBlock? stateBlock = null;
                if (hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred || !hdCamera.frameSettings.IsEnabled(FrameSettingsField.AlphaToMask))
                    stateBlock = m_AlphaToMaskBlock;
                passData.rendererList = builder.UseRendererList(
                    renderGraph.CreateRendererList(CreateOpaqueRendererListDesc(cull, hdCamera.camera, HDShaderPassNames.s_MotionVectorsName, PerObjectData.MotionVectors, stateBlock: stateBlock)));

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

        struct GBufferOutput
        {
            public TextureHandle[] mrt;
            public int gBufferCount;
            public int lightLayersTextureIndex;
            public int shadowMaskTextureIndex;
        }

        void SetupGBufferTargets(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle sssBuffer, TextureHandle vtFeedbackBuffer, ref PrepassOutput prepassOutput, FrameSettings frameSettings, RenderGraphBuilder builder)
        {
            bool clearGBuffer = NeedClearGBuffer(hdCamera);
            bool lightLayers = frameSettings.IsEnabled(FrameSettingsField.LightLayers);
            bool shadowMasks = frameSettings.IsEnabled(FrameSettingsField.Shadowmask);

            int currentIndex = 0;
            prepassOutput.depthBuffer = builder.UseDepthBuffer(prepassOutput.depthBuffer, DepthAccess.ReadWrite);
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
                    colorFormat = GraphicsFormat.R8G8B8A8_UNorm,
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
                    colorFormat = Builtin.GetLightingBufferFormat(),
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
            if (lightLayers)
            {
                prepassOutput.gbuffer.mrt[currentIndex] = builder.UseColorBuffer(renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, clearBuffer = clearGBuffer, clearColor = Color.clear, name = "LightLayers" }), currentIndex);
                prepassOutput.gbuffer.lightLayersTextureIndex = currentIndex++;
            }
            if (shadowMasks)
            {
                prepassOutput.gbuffer.mrt[currentIndex] = builder.UseColorBuffer(renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one, true, true) { colorFormat = Builtin.GetShadowMaskBufferFormat(), clearBuffer = clearGBuffer, clearColor = Color.clear, name = "ShadowMasks" }), currentIndex);
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

        // RenderGBuffer do the gbuffer pass. This is only called with deferred. If we use a depth prepass, then the depth prepass will perform the alpha testing for opaque alpha tested and we don't need to do it anymore
        // during Gbuffer pass. This is handled in the shader and the depth test (equal and no depth write) is done here.
        void RenderGBuffer(RenderGraph renderGraph, TextureHandle sssBuffer, TextureHandle vtFeedbackBuffer, ref PrepassOutput prepassOutput, CullingResults cull, HDCamera hdCamera)
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
                    renderGraph.CreateRendererList(CreateOpaqueRendererListDesc(cull, hdCamera.camera, HDShaderPassNames.s_GBufferName, m_CurrentRendererConfigurationBakedLighting)));

                passData.dBuffer = ReadDBuffer(prepassOutput.dbuffer, builder);

                builder.SetRenderFunc(
                    (GBufferPassData data, RenderGraphContext context) =>
                    {
                        BindDBufferGlobalData(data.dBuffer, context);
                        DrawOpaqueRendererList(context, data.frameSettings, data.rendererList);
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
                    new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R32G32B32A32_SFloat, name = "DepthValuesBuffer" });

                passData.needMotionVectors = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MotionVectors);

                passData.depthResolveMaterial = m_DepthResolveMaterial;
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
                    passData.inputDepth = builder.ReadTexture(output.resolvedDepthBuffer);

                    passData.outputDepth = builder.WriteTexture(renderGraph.CreateTexture(
                        new TextureDesc(depthMipchainSize.x, depthMipchainSize.y, true, true)
                        { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "CameraDepthBufferMipChain" }));

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
            public int resolveKernel;
            public bool resolveIsNecessary;
            public bool resolveOnly;

            public TextureHandle inputDepth;
            public TextureHandle resolvedStencil;
            public ComputeBufferHandle coarseStencilBuffer;
        }

        // This pass build the coarse stencil buffer if requested (i.e. when resolveOnly: false) and perform the MSAA resolve of the
        // full res stencil buffer if needed (a pass requires it and MSAA is on).
        void BuildCoarseStencilAndResolveIfNeeded(RenderGraph renderGraph, HDCamera hdCamera, bool resolveOnly, ref PrepassOutput output)
        {
            using (var builder = renderGraph.AddRenderPass<ResolveStencilPassData>("Resolve Stencil", out var passData, ProfilingSampler.Get(HDProfileId.BuildCoarseStencilAndResolveIfNeeded)))
            {
                bool MSAAEnabled = hdCamera.msaaEnabled;
                int kernel = SampleCountToPassIndex(hdCamera.msaaSamples);

                passData.hdCamera = hdCamera;
                passData.resolveOnly = resolveOnly;
                // With MSAA, the following features require a copy of the stencil, if none are active, no need to do the resolve.
                passData.resolveIsNecessary = (GetFeatureVariantsEnabled(hdCamera.frameSettings) || hdCamera.IsSSREnabled() || hdCamera.IsSSREnabled(transparent: true)) && MSAAEnabled;
                passData.resolveStencilCS = defaultResources.shaders.resolveStencilCS;
                if (passData.resolveIsNecessary && resolveOnly)
                    passData.resolveKernel = (kernel - 1) + 7;
                else
                    passData.resolveKernel = passData.resolveIsNecessary ? kernel + 3 : kernel; // We have a different variant if we need to resolve to non-MSAA stencil

                passData.inputDepth = builder.ReadTexture(output.depthBuffer);
                passData.coarseStencilBuffer = builder.WriteComputeBuffer(
                    renderGraph.CreateComputeBuffer(new ComputeBufferDesc(HDUtils.DivRoundUp(m_MaxCameraWidth, 8) * HDUtils.DivRoundUp(m_MaxCameraHeight, 8) * m_MaxViewCount, sizeof(uint)) { name = "CoarseStencilBuffer" }));
                if (passData.resolveIsNecessary)
                    passData.resolvedStencil = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R8G8_UInt, enableRandomWrite = true, name = "StencilBufferResolved" }));
                else
                    passData.resolvedStencil = output.stencilBuffer;

                builder.SetRenderFunc(
                    (ResolveStencilPassData data, RenderGraphContext context) =>
                    {
                        if (data.resolveOnly && !data.resolveIsNecessary)
                            return;

                        ComputeShader cs = data.resolveStencilCS;
                        context.cmd.SetComputeBufferParam(cs, data.resolveKernel, HDShaderIDs._CoarseStencilBuffer, data.coarseStencilBuffer);
                        context.cmd.SetComputeTextureParam(cs, data.resolveKernel, HDShaderIDs._StencilTexture, data.inputDepth, 0, RenderTextureSubElement.Stencil);

                        if (data.resolveIsNecessary)
                            context.cmd.SetComputeTextureParam(cs, data.resolveKernel, HDShaderIDs._OutputStencilBuffer, data.resolvedStencil);

                        int coarseStencilWidth = HDUtils.DivRoundUp(data.hdCamera.actualWidth, 8);
                        int coarseStencilHeight = HDUtils.DivRoundUp(data.hdCamera.actualHeight, 8);
                        context.cmd.DispatchCompute(cs, data.resolveKernel, coarseStencilWidth, coarseStencilHeight, data.hdCamera.viewCount);
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

        struct DBufferOutput
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
                    new TextureDesc(Vector2.one, true, true) { colorFormat = rtFormat[dbufferIndex], name = s_DBufferNames[dbufferIndex], clearBuffer = true, clearColor = s_DBufferClearColors[dbufferIndex] }), dbufferIndex);
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

        void RenderDBuffer(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle decalBuffer, ref PrepassOutput output, CullingResults cullingResults)
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
                    sortingCriteria = SortingCriteria.CommonOpaque | SortingCriteria.RendererPriority,
                    rendererConfiguration = PerObjectData.None,
                    renderQueueRange = HDRenderQueue.k_RenderQueue_AllOpaque
                }));

                passData.vfxDecalsRendererList = builder.UseRendererList(renderGraph.CreateRendererList(
                    new RendererUtils.RendererListDesc(m_VfxDecalsPassNames, cullingResults, hdCamera.camera)
                    {
                        sortingCriteria = SortingCriteria.CommonOpaque & ~SortingCriteria.OptimizeStateChanges,
                        rendererConfiguration = PerObjectData.None,
                        renderQueueRange = HDRenderQueue.k_RenderQueue_AllOpaque,
                    }));

                SetupDBufferTargets(renderGraph, use4RTs, ref output, builder);
                passData.decalBuffer = builder.ReadTexture(decalBuffer);
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
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal && SystemInfo.graphicsDeviceName.Contains("Intel"))
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
            public float sourceWidth;
            public float sourceHeight;
            public float downsampleScale;
            public Material downsampleDepthMaterial;
            public TextureHandle depthTexture;
            public TextureHandle downsampledDepthBuffer;

            // Data needed for potentially writing
            public Vector2Int mip0Offset;
            public bool computesMip1OfAtlas;
        }

        void DownsampleDepthForLowResTransparency(RenderGraph renderGraph, HDCamera hdCamera, bool computeMip1OfPyramid, ref PrepassOutput output)
        {
            // If the depth buffer hasn't been already copied by the decal depth buffer pass, then we do the copy here.
            CopyDepthBufferIfNeeded(renderGraph, hdCamera, ref output);

            using (var builder = renderGraph.AddRenderPass<DownsampleDepthForLowResPassData>("Downsample Depth Buffer for Low Res Transparency", out var passData, ProfilingSampler.Get(HDProfileId.DownsampleDepth)))
            {
                passData.useGatherDownsample = false;
                if (hdCamera.isLowResScaleHalf)
                {
                    if (m_Asset.currentPlatformRenderPipelineSettings.lowresTransparentSettings.checkerboardDepthBuffer)
                    {
                        m_DownsampleDepthMaterialHalfresCheckerboard.EnableKeyword("CHECKERBOARD_DOWNSAMPLE");
                    }
                    else
                    {
                        m_DownsampleDepthMaterialHalfresCheckerboard.DisableKeyword("CHECKERBOARD_DOWNSAMPLE");
                    }
                    if (computeMip1OfPyramid)
                    {
                        passData.mip0Offset = hdCamera.depthBufferMipChainInfo.mipLevelOffsets[1];
                        m_DownsampleDepthMaterialHalfresCheckerboard.EnableKeyword("OUTPUT_FIRST_MIP_OF_MIPCHAIN");
                    }
                    passData.downsampleDepthMaterial = m_DownsampleDepthMaterialHalfresCheckerboard;
                }
                else
                {
                    m_DownsampleDepthMaterialGather.EnableKeyword("GATHER_DOWNSAMPLE");
                    passData.downsampleDepthMaterial = m_DownsampleDepthMaterialGather;
                    passData.useGatherDownsample = true;
                }

                passData.computesMip1OfAtlas = computeMip1OfPyramid;
                passData.downsampleScale = hdCamera.lowResScale;
                passData.sourceWidth = hdCamera.actualWidth;
                passData.sourceHeight = hdCamera.actualHeight;
                passData.depthTexture = builder.ReadTexture(output.depthPyramidTexture);
                if (computeMip1OfPyramid)
                {
                    passData.depthTexture = builder.WriteTexture(passData.depthTexture);
                }

                passData.downsampledDepthBuffer = builder.UseDepthBuffer(renderGraph.CreateTexture(
                    new TextureDesc(Vector2.one * hdCamera.lowResScale, true, true) { depthBufferBits = DepthBits.Depth32, name = "LowResDepthBuffer" }), DepthAccess.Write);

                builder.SetRenderFunc(
                    (DownsampleDepthForLowResPassData data, RenderGraphContext context) =>
                    {
                        if (data.computesMip1OfAtlas)
                        {
                            data.downsampleDepthMaterial.SetVector(HDShaderIDs._DstOffset, new Vector4(data.mip0Offset.x, data.mip0Offset.y, 0.0f, 0.0f));
                            context.cmd.SetRandomWriteTarget(1, data.depthTexture);
                        }

                        if (data.useGatherDownsample)
                        {
                            float downsampleScaleInv = 1.0f / data.downsampleScale;
                            RenderTexture srcTexture = data.depthTexture;
                            RenderTexture destTexture = data.downsampledDepthBuffer;
                            float uvScaleX = ((float)destTexture.width / (float)srcTexture.width) * downsampleScaleInv;
                            float uvScaleY = ((float)destTexture.height / (float)srcTexture.height) * downsampleScaleInv;
                            data.downsampleDepthMaterial.SetVector(HDShaderIDs._ScaleBias, new Vector4(uvScaleX, uvScaleY, 0.0f, 0.0f));
                        }

                        float destWidth = data.sourceWidth * data.downsampleScale;
                        float destHeight = data.sourceHeight * data.downsampleScale;
                        Rect targetViewport = new Rect(0.0f, 0.0f, destWidth, destHeight);
                        context.cmd.SetViewport(targetViewport);
                        context.cmd.DrawProcedural(Matrix4x4.identity, data.downsampleDepthMaterial, 0, MeshTopology.Triangles, 3, 1, null);

                        if (data.computesMip1OfAtlas)
                        {
                            context.cmd.ClearRandomWriteTargets();
                        }
                    });

                output.downsampledDepthBuffer = passData.downsampledDepthBuffer;
            }
        }

        class GenerateDepthPyramidPassData
        {
            public TextureHandle depthTexture;
            public HDUtils.PackedMipChainInfo mipInfo;
            public MipGenerator mipGenerator;

            public bool mip0AlreadyComputed;
        }

        void GenerateDepthPyramid(RenderGraph renderGraph, HDCamera hdCamera, bool mip0AlreadyComputed, ref PrepassOutput output)
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
                passData.mip0AlreadyComputed = mip0AlreadyComputed;

                builder.SetRenderFunc(
                    (GenerateDepthPyramidPassData data, RenderGraphContext context) =>
                    {
                        data.mipGenerator.RenderMinDepthPyramid(context.cmd, data.depthTexture, data.mipInfo, data.mip0AlreadyComputed);
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
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);
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
