using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        DebugOverlay m_DebugOverlay = new DebugOverlay();
        TextureHandle m_DebugFullScreenTexture;
        ComputeBufferHandle m_DebugFullScreenComputeBuffer;

        class TransparencyOverdrawPassData
        {
            public TransparencyOverdrawParameters parameters;
            public TextureHandle output;
            public TextureHandle depthBuffer;
            public RendererListHandle transparencyRL;
            public RendererListHandle transparencyAfterPostRL;
            public RendererListHandle transparencyLowResRL;
        }

        void RenderTransparencyOverdraw(RenderGraph renderGraph, TextureHandle depthBuffer, CullingResults cull, HDCamera hdCamera)
        {
            if (m_CurrentDebugDisplaySettings.IsDebugDisplayEnabled() && m_CurrentDebugDisplaySettings.data.fullScreenDebugMode == FullScreenDebugMode.TransparencyOverdraw)
            {
                TextureHandle transparencyOverdrawOutput = TextureHandle.nullHandle;
                using (var builder = renderGraph.AddRenderPass<TransparencyOverdrawPassData>("Transparency Overdraw", out var passData))
                {
                    passData.parameters = PrepareTransparencyOverdrawParameters(hdCamera, cull);
                    passData.output = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GetColorBufferFormat() }));
                    passData.depthBuffer = builder.ReadTexture(depthBuffer);
                    passData.transparencyRL = builder.UseRendererList(renderGraph.CreateRendererList(passData.parameters.transparencyRL));
                    passData.transparencyAfterPostRL = builder.UseRendererList(renderGraph.CreateRendererList(passData.parameters.transparencyAfterPostRL));
                    passData.transparencyLowResRL = builder.UseRendererList(renderGraph.CreateRendererList(passData.parameters.transparencyLowResRL));

                    builder.SetRenderFunc(
                    (TransparencyOverdrawPassData data, RenderGraphContext ctx) =>
                    {
                        RenderTransparencyOverdraw( data.parameters,
                                                    data.output,
                                                    data.depthBuffer,
                                                    data.transparencyRL,
                                                    data.transparencyAfterPostRL,
                                                    data.transparencyLowResRL,
                                                    ctx.renderContext, ctx.cmd);
                    });

                    transparencyOverdrawOutput = passData.output;
                }

                PushFullScreenDebugTexture(renderGraph, transparencyOverdrawOutput, FullScreenDebugMode.TransparencyOverdraw);
            }
        }

        class FullScreenDebugPassData
        {
            public FullScreenDebugParameters parameters;
            public TextureHandle output;
            public TextureHandle depthBuffer;
            public ComputeBufferHandle debugBuffer;
            public RendererListHandle rendererList;
        }

        void RenderFullScreenDebug(RenderGraph renderGraph, TextureHandle colorBuffer, TextureHandle depthBuffer, CullingResults cull, HDCamera hdCamera)
        {
            TextureHandle fullscreenDebugOutput = TextureHandle.nullHandle;
            ComputeBufferHandle fullscreenDebugBuffer = ComputeBufferHandle.nullHandle;
            using (var builder = renderGraph.AddRenderPass<FullScreenDebugPassData>("FullScreen Debug", out var passData))
            {
                passData.parameters = PrepareFullScreenDebugParameters(hdCamera, cull);
                passData.output = builder.WriteTexture(colorBuffer);
                passData.depthBuffer = builder.ReadTexture(depthBuffer);
                passData.debugBuffer = builder.WriteComputeBuffer(renderGraph.CreateComputeBuffer(new ComputeBufferDesc(hdCamera.actualWidth * hdCamera.actualHeight * hdCamera.viewCount, sizeof(uint))));
                passData.rendererList = builder.UseRendererList(renderGraph.CreateRendererList(passData.parameters.rendererList));

                builder.SetRenderFunc(
                (FullScreenDebugPassData data, RenderGraphContext ctx) =>
                {
                    RenderFullScreenDebug(  data.parameters,
                                            data.output,
                                            data.depthBuffer,
                                            data.debugBuffer,
                                            data.rendererList,
                                            ctx.renderContext, ctx.cmd);
                });

                fullscreenDebugOutput = passData.output;
                fullscreenDebugBuffer = passData.debugBuffer;
            }

            m_DebugFullScreenComputeBuffer = fullscreenDebugBuffer;
            PushFullScreenDebugTexture(renderGraph, ResolveMSAAColor(renderGraph, hdCamera, fullscreenDebugOutput));
        }

        class ResolveFullScreenDebugPassData
        {
            public DebugParameters debugParameters;
            public TextureHandle output;
            public TextureHandle input;
            public TextureHandle depthPyramid;
            public ComputeBufferHandle fullscreenBuffer;
        }

        TextureHandle ResolveFullScreenDebug(RenderGraph renderGraph, in DebugParameters debugParameters, TextureHandle inputFullScreenDebug, TextureHandle depthPyramid)
        {
            using (var builder = renderGraph.AddRenderPass<ResolveFullScreenDebugPassData>("ResolveFullScreenDebug", out var passData))
            {
                passData.debugParameters = debugParameters;
                passData.input = builder.ReadTexture(inputFullScreenDebug);
                passData.depthPyramid = builder.ReadTexture(depthPyramid);
                // On Vulkan, not binding the Random Write Target will result in an invalid drawcall.
                // To avoid that, if the compute buffer is invalid, we bind a dummy compute buffer anyway.
                if (m_DebugFullScreenComputeBuffer.IsValid())
                    passData.fullscreenBuffer = builder.ReadComputeBuffer(m_DebugFullScreenComputeBuffer);
                else
                    passData.fullscreenBuffer = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(4, sizeof(uint)));
                passData.output = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, name = "ResolveFullScreenDebug" }));

                builder.SetRenderFunc(
                (ResolveFullScreenDebugPassData data, RenderGraphContext ctx) =>
                {
                    ResolveFullScreenDebug(data.debugParameters, ctx.renderGraphPool.GetTempMaterialPropertyBlock(), data.input, data.depthPyramid, data.output, data.fullscreenBuffer, ctx.cmd);
                });

                return passData.output;
            }
        }

        class ResolveColorPickerDebugPassData
        {
            public DebugParameters debugParameters;
            public TextureHandle output;
            public TextureHandle input;
        }

        TextureHandle ResolveColorPickerDebug(RenderGraph renderGraph, in DebugParameters debugParameters, TextureHandle inputColorPickerDebug)
        {
            using (var builder = renderGraph.AddRenderPass<ResolveColorPickerDebugPassData>("ResolveColorPickerDebug", out var passData))
            {
                passData.debugParameters = debugParameters;
                passData.input = builder.ReadTexture(inputColorPickerDebug);
                passData.output = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, name = "ResolveColorPickerDebug" }));

                builder.SetRenderFunc(
                (ResolveColorPickerDebugPassData data, RenderGraphContext ctx) =>
                {
                    ResolveColorPickerDebug(data.debugParameters, data.input, data.output, ctx.cmd);
                });

                return passData.output;
            }
        }

        class DebugOverlayPassData
        {
            public DebugParameters debugParameters;
            public TextureHandle colorBuffer;
            public TextureHandle depthBuffer;
        }

        void RenderSkyReflectionOverlay(RenderGraph renderGraph, in DebugParameters debugParameters, TextureHandle colorBuffer, TextureHandle depthBuffer)
        {
            var lightingDebug = debugParameters.debugDisplaySettings.data.lightingDebugSettings;
            if (!lightingDebug.displaySkyReflection)
                return;

            using (var builder = renderGraph.AddRenderPass<DebugOverlayPassData>("SkyReflectionOverlay", out var passData))
            {
                passData.debugParameters = debugParameters;
                passData.colorBuffer = builder.UseColorBuffer(colorBuffer, 0);
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);

                builder.SetRenderFunc(
                (DebugOverlayPassData data, RenderGraphContext ctx) =>
                {
                    RenderSkyReflectionOverlay(data.debugParameters, ctx.cmd, ctx.renderGraphPool.GetTempMaterialPropertyBlock());
                });
            }
        }

        void RenderRayCountOverlay(RenderGraph renderGraph, in DebugParameters debugParameters, TextureHandle colorBuffer, TextureHandle depthBuffer)
        {
            if (!debugParameters.rayTracingSupported)
                return;

            using (var builder = renderGraph.AddRenderPass<DebugOverlayPassData>("RenderRayCountOverlay", out var passData))
            {
                passData.debugParameters = debugParameters;
                passData.colorBuffer = builder.UseColorBuffer(colorBuffer, 0);
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);

                builder.SetRenderFunc(
                (DebugOverlayPassData data, RenderGraphContext ctx) =>
                {
                    RenderRayCountOverlay(data.debugParameters, ctx.cmd);
                });
            }
        }

        class DebugLightLoopOverlayPassData
            : DebugOverlayPassData
        {
            public TextureHandle depthPyramidTexture;
            public ComputeBufferHandle tileList;
            public ComputeBufferHandle lightList;
            public ComputeBufferHandle perVoxelLightList;
            public ComputeBufferHandle dispatchIndirect;
        }

        void RenderLightLoopDebugOverlay(RenderGraph renderGraph, in DebugParameters debugParameters, TextureHandle colorBuffer, TextureHandle depthBuffer, in BuildGPULightListOutput lightLists, TextureHandle depthPyramidTexture)
        {
            var lightingDebug = debugParameters.debugDisplaySettings.data.lightingDebugSettings;
            if (lightingDebug.tileClusterDebug == TileClusterDebug.None
                && !lightingDebug.displayCookieAtlas
                && !lightingDebug.displayPlanarReflectionProbeAtlas)
                return;

            using (var builder = renderGraph.AddRenderPass<DebugLightLoopOverlayPassData>("RenderLightLoopDebugOverlay", out var passData))
            {
                passData.debugParameters = debugParameters;
                passData.colorBuffer = builder.UseColorBuffer(colorBuffer, 0);
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);

                if (lightingDebug.tileClusterDebug != TileClusterDebug.None)
                {
                    passData.depthPyramidTexture = builder.ReadTexture(depthPyramidTexture);
                    passData.tileList = builder.ReadComputeBuffer(lightLists.tileList);
                    passData.lightList = builder.ReadComputeBuffer(lightLists.lightList);
                    passData.perVoxelLightList = builder.ReadComputeBuffer(lightLists.perVoxelLightLists);
                    passData.dispatchIndirect = builder.ReadComputeBuffer(lightLists.dispatchIndirectBuffer);
                }

                builder.SetRenderFunc(
                (DebugLightLoopOverlayPassData data, RenderGraphContext ctx) =>
                {
                    RenderLightLoopDebugOverlay(data.debugParameters, ctx.cmd, data.tileList, data.lightList, data.perVoxelLightList, data.dispatchIndirect, data.depthPyramidTexture);

                });
            }
        }

        void RenderProbeVolumeDebugOverlay(RenderGraph renderGraph, in DebugParameters debugParameters, TextureHandle colorBuffer, TextureHandle depthBuffer)
        {
            if (!m_SupportProbeVolume || debugParameters.debugDisplaySettings.data.lightingDebugSettings.probeVolumeDebugMode == ProbeVolumeDebugMode.None)
                return;

            using (var builder = renderGraph.AddRenderPass<DebugOverlayPassData>("RenderProbeVolumeDebugOverlay", out var passData))
            {
                passData.debugParameters = debugParameters;
                passData.colorBuffer = builder.UseColorBuffer(colorBuffer, 0);
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);

                builder.SetRenderFunc(
                (DebugLightLoopOverlayPassData data, RenderGraphContext ctx) =>
                {
                    RenderProbeVolumeDebugOverlay(data.debugParameters, ctx.cmd);
                });
            }
        }

        class RenderShadowsDebugOverlayPassData
            : DebugOverlayPassData
        {
            public ShadowResult shadowTextures;
        }

        void RenderShadowsDebugOverlay(RenderGraph renderGraph, in DebugParameters debugParameters, TextureHandle colorBuffer, TextureHandle depthBuffer, in ShadowResult shadowResult)
        {
            LightingDebugSettings lightingDebug = debugParameters.debugDisplaySettings.data.lightingDebugSettings;
            if (lightingDebug.shadowDebugMode == ShadowMapDebugMode.None)
                return;

            using (var builder = renderGraph.AddRenderPass<RenderShadowsDebugOverlayPassData>("RenderShadowsDebugOverlay", out var passData))
            {
                passData.debugParameters = debugParameters;
                passData.colorBuffer = builder.UseColorBuffer(colorBuffer, 0);
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);
                passData.shadowTextures = HDShadowManager.ReadShadowResult(shadowResult, builder);

                builder.SetRenderFunc(
                (RenderShadowsDebugOverlayPassData data, RenderGraphContext ctx) =>
                {
                    var debugParams = data.debugParameters;

                    var shadowAtlases = new HDShadowManager.ShadowDebugAtlasTextures();
                    shadowAtlases.punctualShadowAtlas = data.shadowTextures.punctualShadowResult;
                    shadowAtlases.cascadeShadowAtlas = data.shadowTextures.directionalShadowResult;
                    shadowAtlases.areaShadowAtlas = data.shadowTextures.areaShadowResult;
                    shadowAtlases.cachedPunctualShadowAtlas = data.shadowTextures.cachedPunctualShadowResult;
                    shadowAtlases.cachedAreaShadowAtlas = data.shadowTextures.cachedAreaShadowResult;

                    RenderShadowsDebugOverlay(debugParams, shadowAtlases, ctx.cmd, ctx.renderGraphPool.GetTempMaterialPropertyBlock());
                });
            }
        }

        void RenderDecalOverlay(RenderGraph renderGraph, in DebugParameters debugParameters, TextureHandle colorBuffer, TextureHandle depthBuffer)
        {
            if (!debugParameters.debugDisplaySettings.data.decalsDebugSettings.displayAtlas)
                return;

            using (var builder = renderGraph.AddRenderPass<DebugOverlayPassData>("SkyReflectionOverlay", out var passData))
            {
                passData.debugParameters = debugParameters;
                passData.colorBuffer = builder.UseColorBuffer(colorBuffer, 0);
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);

                builder.SetRenderFunc(
                (DebugOverlayPassData data, RenderGraphContext ctx) =>
                {
                    DecalSystem.instance.RenderDebugOverlay(data.debugParameters.hdCamera, ctx.cmd, data.debugParameters.debugDisplaySettings, data.debugParameters.debugOverlay);
                });
            }
        }

        void RenderDebugOverlays(   RenderGraph                 renderGraph,
                                    in DebugParameters          debugParameters,
                                    TextureHandle               colorBuffer,
                                    TextureHandle               depthBuffer,
                                    TextureHandle               depthPyramidTexture,
                                    in BuildGPULightListOutput  lightLists,
                                    in ShadowResult             shadowResult)
        {

            RenderSkyReflectionOverlay(renderGraph, debugParameters, colorBuffer, depthBuffer);
            RenderRayCountOverlay(renderGraph, debugParameters, colorBuffer, depthBuffer);
            RenderLightLoopDebugOverlay(renderGraph, debugParameters, colorBuffer, depthBuffer, lightLists, depthPyramidTexture);
            RenderProbeVolumeDebugOverlay(renderGraph, debugParameters, colorBuffer, depthBuffer);
            RenderShadowsDebugOverlay(renderGraph, debugParameters, colorBuffer, depthBuffer, shadowResult);
            RenderDecalOverlay(renderGraph, debugParameters, colorBuffer, depthBuffer);
        }

        class RenderLightVolumesPassData
        {
            public DebugLightVolumes.RenderLightVolumesParameters   parameters;
            // Render target that holds the light count in floating points
            public TextureHandle                                    lightCountBuffer;
            // Render target that holds the color accumulated value
            public TextureHandle                                    colorAccumulationBuffer;
            // The output texture of the debug
            public TextureHandle                                    debugLightVolumesTexture;
            // Required depth texture given that we render multiple render targets
            public TextureHandle                                    depthBuffer;
            public TextureHandle                                    destination;
        }

        static void RenderLightVolumes(RenderGraph renderGraph, in DebugParameters debugParameters, TextureHandle destination, TextureHandle depthBuffer, CullingResults cullResults)
        {
            using (var builder = renderGraph.AddRenderPass<RenderLightVolumesPassData>("LightVolumes", out var passData))
            {
                passData.parameters = s_lightVolumes.PrepareLightVolumeParameters(debugParameters.hdCamera, debugParameters.debugDisplaySettings.data.lightingDebugSettings, cullResults);
                passData.lightCountBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat= GraphicsFormat.R32_SFloat, clearBuffer = true, clearColor = Color.black, name = "LightVolumeCount" });
                passData.colorAccumulationBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, clearBuffer = true, clearColor = Color.black, name = "LightVolumeColorAccumulation" });
                passData.debugLightVolumesTexture = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, clearBuffer = true, clearColor = Color.black, enableRandomWrite = true, name = "LightVolumeDebugLightVolumesTexture" });
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);
                passData.destination = builder.WriteTexture(destination);

                builder.SetRenderFunc(
                (RenderLightVolumesPassData data, RenderGraphContext ctx) =>
                {
                    RenderTargetIdentifier[] mrt = ctx.renderGraphPool.GetTempArray<RenderTargetIdentifier>(2);
                    mrt[0] = data.lightCountBuffer;
                    mrt[1] = data.colorAccumulationBuffer;

                    DebugLightVolumes.RenderLightVolumes(   ctx.cmd,
                                                            data.parameters,
                                                            mrt, data.lightCountBuffer,
                                                            data.colorAccumulationBuffer,
                                                            data.debugLightVolumesTexture,
                                                            data.depthBuffer,
                                                            data.destination,
                                                            ctx.renderGraphPool.GetTempMaterialPropertyBlock());
                });
            }
        }


        class DebugImageHistogramData
        {
            public PostProcessSystem.DebugImageHistogramParameters parameters;
            public TextureHandle source;
        }

        void GenerateDebugImageHistogram(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source)
        {
            using (var builder = renderGraph.AddRenderPass<DebugImageHistogramData>("Generate Debug Image Histogram", out var passData, ProfilingSampler.Get(HDProfileId.FinalImageHistogram)))
            {
                passData.source = builder.ReadTexture(source);
                passData.parameters = m_PostProcessSystem.PrepareDebugImageHistogramParameters(hdCamera);
                builder.SetRenderFunc(
                (DebugImageHistogramData data, RenderGraphContext ctx) =>
                {
                    PostProcessSystem.GenerateDebugImageHistogram(data.parameters, ctx.cmd, data.source);
                });
            }
        }

        class DebugExposureData
        {
            public DebugParameters debugParameters;
            public Vector4 proceduralMeteringParams1;
            public Vector4 proceduralMeteringParams2;
            public TextureHandle colorBuffer;
            public TextureHandle debugFullScreenTexture;
            public TextureHandle output;
            public TextureHandle currentExposure;
            public TextureHandle previousExposure;
            public TextureHandle debugExposureData;
            public HableCurve customToneMapCurve;
            public int lutSize;
            public ComputeBuffer histogramBuffer;
        }

        TextureHandle RenderExposureDebug(RenderGraph renderGraph, HDCamera hdCamera, DebugParameters debugParameters, TextureHandle colorBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<DebugExposureData>("Debug Exposure", out var passData))
            {
                m_PostProcessSystem.ComputeProceduralMeteringParams(hdCamera, out passData.proceduralMeteringParams1, out passData.proceduralMeteringParams2);

                passData.debugParameters = debugParameters;
                passData.colorBuffer = builder.ReadTexture(colorBuffer);
                passData.debugFullScreenTexture = builder.ReadTexture(m_DebugFullScreenTexture);
                passData.output = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, name = "ExposureDebug" }));
                passData.currentExposure = builder.ReadTexture(renderGraph.ImportTexture(m_PostProcessSystem.GetExposureTexture(hdCamera)));
                passData.previousExposure = builder.ReadTexture(renderGraph.ImportTexture(m_PostProcessSystem.GetPreviousExposureTexture(hdCamera)));
                passData.debugExposureData = builder.ReadTexture(renderGraph.ImportTexture(m_PostProcessSystem.GetExposureDebugData()));
                passData.customToneMapCurve = m_PostProcessSystem.GetCustomToneMapCurve();
                passData.lutSize = m_PostProcessSystem.GetLutSize();
                passData.histogramBuffer = debugParameters.debugDisplaySettings.data.lightingDebugSettings.exposureDebugMode == ExposureDebugMode.FinalImageHistogramView ? m_PostProcessSystem.GetDebugImageHistogramBuffer() : m_PostProcessSystem.GetHistogramBuffer();

                builder.SetRenderFunc(
                (DebugExposureData data, RenderGraphContext ctx) =>
                {
                    RenderExposureDebug(data.debugParameters, data.colorBuffer, data.debugFullScreenTexture,
                                        data.previousExposure,
                                        data.currentExposure,
                                        data.debugExposureData,
                                        data.output,
                                        data.customToneMapCurve,
                                        data.lutSize,
                                        data.proceduralMeteringParams1,
                                        data.proceduralMeteringParams2,
                                        data.histogramBuffer, ctx.cmd);

                });

                return passData.output;
            }
        }

        TextureHandle RenderDebug(  RenderGraph                 renderGraph,
                                    HDCamera                    hdCamera,
                                    TextureHandle               colorBuffer,
                                    TextureHandle               depthBuffer,
                                    TextureHandle               depthPyramidTexture,
                                    TextureHandle               colorPickerDebugTexture,
                                    in BuildGPULightListOutput  lightLists,
                                    in ShadowResult             shadowResult,
                                    CullingResults              cullResults)
        {
            // We don't want any overlay for these kind of rendering
            if (hdCamera.camera.cameraType == CameraType.Reflection || hdCamera.camera.cameraType == CameraType.Preview)
                return colorBuffer;

            TextureHandle output = colorBuffer;
            var debugParameters = PrepareDebugParameters(hdCamera, GetDepthBufferMipChainInfo());

            if (debugParameters.resolveFullScreenDebug)
            {
                output = ResolveFullScreenDebug(renderGraph, debugParameters, m_DebugFullScreenTexture, depthPyramidTexture);
                // If we have full screen debug, this is what we want color picked, so we replace color picker input texture with the new one.
                if (debugParameters.colorPickerEnabled)
                    colorPickerDebugTexture = PushColorPickerDebugTexture(renderGraph, output);

                m_FullScreenDebugPushed = false;
                m_DebugFullScreenComputeBuffer = ComputeBufferHandle.nullHandle;
            }

            if (debugParameters.exposureDebugEnabled)
                output = RenderExposureDebug(renderGraph, hdCamera, debugParameters, colorBuffer);

            if (debugParameters.colorPickerEnabled)
                output = ResolveColorPickerDebug(renderGraph, debugParameters, colorPickerDebugTexture);

            if (debugParameters.debugDisplaySettings.data.lightingDebugSettings.displayLightVolumes)
            {
                RenderLightVolumes(renderGraph, debugParameters, output, depthBuffer, cullResults);
            }

            RenderDebugOverlays(renderGraph, debugParameters, output, depthBuffer, depthPyramidTexture, lightLists, shadowResult);

            return output;
        }

        class DebugViewMaterialData
        {
            public TextureHandle outputColor;
            public TextureHandle outputDepth;
            public RendererListHandle opaqueRendererList;
            public RendererListHandle transparentRendererList;
            public Material debugGBufferMaterial;
            public FrameSettings frameSettings;

            public bool decalsEnabled;
            public ComputeBufferHandle perVoxelOffset;
            public DBufferOutput dbuffer;
            public GBufferOutput gbuffer;
            public TextureHandle depthBuffer;

            public Texture clearColorTexture;
            public RenderTexture clearDepthTexture;
            public bool clearDepth;
        }

        TextureHandle RenderDebugViewMaterial(RenderGraph renderGraph, CullingResults cull, HDCamera hdCamera, BuildGPULightListOutput lightLists, DBufferOutput dbuffer, GBufferOutput gbuffer, TextureHandle depthBuffer)
        {
            bool msaa = hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA);

            var output = renderGraph.CreateTexture(
                new TextureDesc(Vector2.one, true, true)
                {
                    colorFormat = GetColorBufferFormat(),
                    enableRandomWrite = !msaa,
                    bindTextureMS = msaa,
                    enableMSAA = msaa,
                    clearBuffer = true,
                    clearColor = Color.clear,
                    name = msaa ? "CameraColorMSAA" : "CameraColor"
                });

            if (m_CurrentDebugDisplaySettings.data.materialDebugSettings.IsDebugGBufferEnabled() && hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred)
            {
                using (var builder = renderGraph.AddRenderPass<DebugViewMaterialData>("DebugViewMaterialGBuffer", out var passData, ProfilingSampler.Get(HDProfileId.DebugViewMaterialGBuffer)))
                {
                    passData.debugGBufferMaterial = m_currentDebugViewMaterialGBuffer;
                    passData.outputColor = builder.WriteTexture(output);
                    passData.gbuffer = ReadGBuffer(gbuffer, builder);
                    passData.depthBuffer = builder.ReadTexture(depthBuffer);

                    builder.SetRenderFunc(
                        (DebugViewMaterialData data, RenderGraphContext context) =>
                        {
                            var gbufferHandles = data.gbuffer;
                            for (int i = 0; i < gbufferHandles.gBufferCount; ++i)
                            {
                                data.debugGBufferMaterial.SetTexture(HDShaderIDs._GBufferTexture[i], gbufferHandles.mrt[i]);
                            }
                            data.debugGBufferMaterial.SetTexture(HDShaderIDs._CameraDepthTexture, data.depthBuffer);

                            HDUtils.DrawFullScreen(context.cmd, data.debugGBufferMaterial, data.outputColor);
                        });
                }
            }
            else
            {
                using (var builder = renderGraph.AddRenderPass<DebugViewMaterialData>("DisplayDebug ViewMaterial", out var passData, ProfilingSampler.Get(HDProfileId.DisplayDebugViewMaterial)))
                {
                    passData.frameSettings = hdCamera.frameSettings;
                    passData.outputColor = builder.UseColorBuffer(output, 0);
                    passData.outputDepth = builder.UseDepthBuffer(CreateDepthBuffer(renderGraph, true, hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA)), DepthAccess.ReadWrite);

                    // When rendering debug material we shouldn't rely on a depth prepass for optimizing the alpha clip test. As it is control on the material inspector side
                    // we must override the state here.
                    passData.opaqueRendererList = builder.UseRendererList(
                        renderGraph.CreateRendererList(CreateOpaqueRendererListDesc(cull, hdCamera.camera, m_AllForwardOpaquePassNames,
                            rendererConfiguration: m_CurrentRendererConfigurationBakedLighting,
                            stateBlock: m_DepthStateOpaque)));
                    passData.transparentRendererList = builder.UseRendererList(
                        renderGraph.CreateRendererList(CreateTransparentRendererListDesc(cull, hdCamera.camera, m_AllTransparentPassNames,
                            rendererConfiguration: m_CurrentRendererConfigurationBakedLighting,
                            stateBlock: m_DepthStateNoWrite)));

                    passData.decalsEnabled = (hdCamera.frameSettings.IsEnabled(FrameSettingsField.Decals)) && (DecalSystem.m_DecalDatasCount > 0);
                    passData.perVoxelOffset = builder.ReadComputeBuffer(lightLists.perVoxelOffset);
                    passData.dbuffer = ReadDBuffer(dbuffer, builder);

                    passData.clearColorTexture = Compositor.CompositionManager.GetClearTextureForStackedCamera(hdCamera);   // returns null if is not a stacked camera
                    passData.clearDepthTexture = Compositor.CompositionManager.GetClearDepthForStackedCamera(hdCamera);     // returns null if is not a stacked camera
                    passData.clearDepth = hdCamera.clearDepth;

                    builder.SetRenderFunc(
                    (DebugViewMaterialData data, RenderGraphContext context) =>
                    {
                        if (data.clearColorTexture != null)
                        {
                            HDUtils.BlitColorAndDepth(context.cmd, data.clearColorTexture, data.clearDepthTexture, new Vector4(1, 1, 0, 0), 0, !data.clearDepth);
                        }

                        BindDefaultTexturesLightingBuffers(context.defaultResources, context.cmd);

                        BindDBufferGlobalData(data.dbuffer, context);
                        DrawOpaqueRendererList(context, data.frameSettings, data.opaqueRendererList);

                        if (data.decalsEnabled)
                            DecalSystem.instance.SetAtlas(context.cmd); // for clustered decals
                        if (data.perVoxelOffset.IsValid())
                            context.cmd.SetGlobalBuffer(HDShaderIDs.g_vLayeredOffsetsBuffer, data.perVoxelOffset);

                        DrawTransparentRendererList(context, data.frameSettings, data.transparentRendererList);
                    });
                }
            }

            return output;
        }

        class PushFullScreenDebugPassData
        {
            public TextureHandle    input;
            public TextureHandle    output;
            public int              mipIndex;
        }

        void PushFullScreenLightingDebugTexture(RenderGraph renderGraph, TextureHandle input)
        {
            // In practice, this is only useful for the SingleShadow debug view.
            // TODO: See how we can make this nicer than a specific functions just for one case.
            if (NeedsFullScreenDebugMode() && m_FullScreenDebugPushed == false)
            {
                PushFullScreenDebugTexture(renderGraph, input);
            }
        }

        internal void PushFullScreenDebugTexture(RenderGraph renderGraph, TextureHandle input, FullScreenDebugMode debugMode)
        {
            if (debugMode == m_CurrentDebugDisplaySettings.data.fullScreenDebugMode)
            {
                PushFullScreenDebugTexture(renderGraph, input);
            }
        }

        void PushFullScreenDebugTextureMip(RenderGraph renderGraph, TextureHandle input, int lodCount, Vector4 scaleBias, FullScreenDebugMode debugMode)
        {
            if (debugMode == m_CurrentDebugDisplaySettings.data.fullScreenDebugMode)
            {
                var mipIndex = Mathf.FloorToInt(m_CurrentDebugDisplaySettings.data.fullscreenDebugMip * lodCount);

                PushFullScreenDebugTexture(renderGraph, input, mipIndex);
            }
        }

        void PushFullScreenDebugTexture(RenderGraph renderGraph, TextureHandle input, int mipIndex = -1)
        {
            using (var builder = renderGraph.AddRenderPass<PushFullScreenDebugPassData>("Push Full Screen Debug", out var passData))
            {
                passData.mipIndex = mipIndex;
                passData.input = builder.ReadTexture(input);
                passData.output = builder.UseColorBuffer(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, name = "DebugFullScreen" }), 0);

                builder.SetRenderFunc(
                (PushFullScreenDebugPassData data, RenderGraphContext ctx) =>
                {
                    if (data.mipIndex != -1)
                        HDUtils.BlitCameraTexture(ctx.cmd, data.input, data.output, data.mipIndex);
                    else
                        HDUtils.BlitCameraTexture(ctx.cmd, data.input, data.output);
                });

                m_DebugFullScreenTexture = passData.output;
            }

            // We need this flag because otherwise if no full screen debug is pushed (like for example if the corresponding pass is disabled), when we render the result in RenderDebug m_DebugFullScreenTempBuffer will contain potential garbage
            m_FullScreenDebugPushed = true;
        }

        void PushFullScreenExposureDebugTexture(RenderGraph renderGraph, TextureHandle input)
        {
            if (m_CurrentDebugDisplaySettings.data.lightingDebugSettings.exposureDebugMode != ExposureDebugMode.None)
            {
                PushFullScreenDebugTexture(renderGraph, input);
            }
        }

#if ENABLE_VIRTUALTEXTURES
        class PushFullScreenVTDebugPassData
        {
            public TextureHandle    input;
            public TextureHandle    output;
            public Material         material;
            public bool             msaa;
        }

        void PushFullScreenVTFeedbackDebugTexture(RenderGraph renderGraph, TextureHandle input, bool msaa)
        {
            if (FullScreenDebugMode.RequestedVirtualTextureTiles == m_CurrentDebugDisplaySettings.data.fullScreenDebugMode)
            {
                using (var builder = renderGraph.AddRenderPass<PushFullScreenVTDebugPassData>("Push Full Screen Debug", out var passData))
                {
                    passData.material = m_VTDebugBlit;
                    passData.msaa = msaa;
                    passData.input = builder.ReadTexture(input);
                    passData.output = builder.UseColorBuffer(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, name = "DebugFullScreen" }), 0);

                    builder.SetRenderFunc(
                    (PushFullScreenVTDebugPassData data, RenderGraphContext ctx) =>
                    {
                        CoreUtils.SetRenderTarget(ctx.cmd, data.output);
                        data.material.SetTexture(data.msaa ? HDShaderIDs._BlitTextureMSAA : HDShaderIDs._BlitTexture, data.input);
                        ctx.cmd.DrawProcedural(Matrix4x4.identity, data.material, data.msaa ? 1 : 0, MeshTopology.Triangles, 3, 1);
                    });

                    m_DebugFullScreenTexture = passData.output;

                }

                m_FullScreenDebugPushed = true;
            }
        }
#endif

        TextureHandle PushColorPickerDebugTexture(RenderGraph renderGraph, TextureHandle input)
        {
            using (var builder = renderGraph.AddRenderPass<PushFullScreenDebugPassData>("Push To Color Picker", out var passData))
            {
                passData.input = builder.ReadTexture(input);
                passData.output = builder.UseColorBuffer(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, name = "DebugColorPicker" }), 0);

                builder.SetRenderFunc(
                (PushFullScreenDebugPassData data, RenderGraphContext ctx) =>
                {
                    HDUtils.BlitCameraTexture(ctx.cmd, data.input, data.output);
                });

                return passData.output;
            }
        }
    }
}
