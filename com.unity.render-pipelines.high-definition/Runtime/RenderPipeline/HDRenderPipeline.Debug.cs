using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        TextureHandle m_DebugFullScreenTexture;

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

        class ResolveFullScreenDebugPassData
        {
            public DebugParameters debugParameters;
            public TextureHandle output;
            public TextureHandle input;
            public TextureHandle depthPyramid;
        }

        TextureHandle ResolveFullScreenDebug(RenderGraph renderGraph, in DebugParameters debugParameters, TextureHandle inputFullScreenDebug, TextureHandle depthPyramid)
        {
            using (var builder = renderGraph.AddRenderPass<ResolveFullScreenDebugPassData>("ResolveFullScreenDebug", out var passData))
            {
                passData.debugParameters = debugParameters;
                passData.input = builder.ReadTexture(inputFullScreenDebug);
                passData.depthPyramid = builder.ReadTexture(depthPyramid);
                passData.output = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, name = "ResolveFullScreenDebug" }));

                builder.SetRenderFunc(
                (ResolveFullScreenDebugPassData data, RenderGraphContext ctx) =>
                {
                    ResolveFullScreenDebug(data.debugParameters, ctx.renderGraphPool.GetTempMaterialPropertyBlock(), data.input, data.depthPyramid, data.output, ctx.cmd);
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

        class RenderDebugOverlayPassData
        {
            public DebugParameters debugParameters;
            public TextureHandle colorBuffer;
            public TextureHandle depthBuffer;
            public TextureHandle depthPyramidTexture;
            public ComputeBufferHandle tileList;
            public ComputeBufferHandle lightList;
            public ComputeBufferHandle perVoxelLightList;
            public ComputeBufferHandle dispatchIndirect;
            public ShadowResult shadowTextures;
        }

        void RenderDebugOverlays(   RenderGraph                 renderGraph,
                                    in DebugParameters          debugParameters,
                                    TextureHandle               colorBuffer,
                                    TextureHandle               depthBuffer,
                                    TextureHandle               depthPyramidTexture,
                                    in BuildGPULightListOutput  lightLists,
                                    in ShadowResult             shadowResult)
        {
            using (var builder = renderGraph.AddRenderPass<RenderDebugOverlayPassData>("DebugOverlay", out var passData))
            {
                passData.debugParameters = debugParameters;
                passData.colorBuffer = builder.UseColorBuffer(colorBuffer, 0);
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);
                passData.depthPyramidTexture = builder.ReadTexture(depthPyramidTexture);
                passData.shadowTextures = HDShadowManager.ReadShadowResult(shadowResult, builder);
                passData.tileList = builder.ReadComputeBuffer(lightLists.tileList);
                passData.lightList = builder.ReadComputeBuffer(lightLists.lightList);
                passData.perVoxelLightList = builder.ReadComputeBuffer(lightLists.perVoxelLightLists);
                passData.dispatchIndirect = builder.ReadComputeBuffer(lightLists.dispatchIndirectBuffer);

                builder.SetRenderFunc(
                (RenderDebugOverlayPassData data, RenderGraphContext ctx) =>
                {
                    var debugParams = data.debugParameters;

                    HDUtils.ResetOverlay();
                    float x = 0.0f;
                    float overlayRatio = debugParams.debugDisplaySettings.data.debugOverlayRatio;
                    float overlaySize = Math.Min(debugParams.hdCamera.actualHeight, debugParams.hdCamera.actualWidth) * overlayRatio;
                    float y = debugParams.hdCamera.actualHeight - overlaySize;

                    var shadowAtlases = new HDShadowManager.ShadowDebugAtlasTextures();
                    shadowAtlases.punctualShadowAtlas = data.shadowTextures.punctualShadowResult;
                    shadowAtlases.cascadeShadowAtlas = data.shadowTextures.directionalShadowResult;
                    shadowAtlases.areaShadowAtlas = data.shadowTextures.areaShadowResult;
                    shadowAtlases.cachedPunctualShadowAtlas = data.shadowTextures.cachedPunctualShadowResult;
                    shadowAtlases.cachedAreaShadowAtlas = data.shadowTextures.cachedAreaShadowResult;

                    RenderSkyReflectionOverlay(debugParams, ctx.cmd, ctx.renderGraphPool.GetTempMaterialPropertyBlock(), ref x, ref y, overlaySize);
                    RenderRayCountOverlay(debugParams, ctx.cmd, ref x, ref y, overlaySize);
                    RenderLightLoopDebugOverlay(debugParams, ctx.cmd, ref x, ref y, overlaySize, data.tileList, data.lightList, data.perVoxelLightList, data.dispatchIndirect, data.depthPyramidTexture);
                    RenderShadowsDebugOverlay(debugParams, shadowAtlases, ctx.cmd, ref x, ref y, overlaySize, ctx.renderGraphPool.GetTempMaterialPropertyBlock());
                    DecalSystem.instance.RenderDebugOverlay(debugParams.hdCamera, ctx.cmd, debugParams.debugDisplaySettings, ref x, ref y, overlaySize, debugParams.hdCamera.actualWidth);
                });
            }
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

        TextureHandle RenderDebug(  RenderGraph                 renderGraph,
                                    HDCamera                    hdCamera,
                                    TextureHandle               colorBuffer,
                                    TextureHandle               depthBuffer,
                                    TextureHandle               depthPyramidTexture,
                                    TextureHandle               fullScreenDebugTexture,
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
                output = ResolveFullScreenDebug(renderGraph, debugParameters, fullScreenDebugTexture, depthPyramidTexture);
                // If we have full screen debug, this is what we want color picked, so we replace color picker input texture with the new one.
                if (debugParameters.colorPickerEnabled)
                    colorPickerDebugTexture = PushColorPickerDebugTexture(renderGraph, output);

                m_FullScreenDebugPushed = false;
            }

            // TODO RENDERGRAPH (Needs post processing in Rendergraph to properly be implemented)
            if(debugParameters.exposureDebugEnabled)
            {
                // For reference the following is what is called in the non-render-graph version.
                // RenderExposureDebug(debugParams, m_CameraColorBuffer, m_DebugFullScreenTempBuffer,m_PostProcessSystem.GetPreviousExposureTexture(hdCamera), m_PostProcessSystem.GetExposureTexture(hdCamera),
                //    m_PostProcessSystem.GetExposureDebugData(),m_IntermediateAfterPostProcessBuffer, m_PostProcessSystem.GetCustomToneMapCurve(), m_PostProcessSystem.GetLutSize(), m_PostProcessSystem.GetHistogramBuffer(), cmd);
            }

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
        }

        TextureHandle RenderDebugViewMaterial(RenderGraph renderGraph, CullingResults cull, HDCamera hdCamera)
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

                    builder.SetRenderFunc(
                    (DebugViewMaterialData data, RenderGraphContext context) =>
                    {
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
                    passData.outputDepth = builder.UseDepthBuffer(CreateDepthBuffer(renderGraph, hdCamera.frameSettings.IsEnabled(FrameSettingsField.MSAA)), DepthAccess.ReadWrite);

                    // When rendering debug material we shouldn't rely on a depth prepass for optimizing the alpha clip test. As it is control on the material inspector side
                    // we must override the state here.
                    passData.opaqueRendererList = builder.UseRendererList(
                        renderGraph.CreateRendererList(CreateOpaqueRendererListDesc(cull, hdCamera.camera, m_AllForwardOpaquePassNames,
                            rendererConfiguration: m_CurrentRendererConfigurationBakedLighting,
                            stateBlock: m_DepthStateOpaque)));
                    passData.transparentRendererList = builder.UseRendererList(
                        renderGraph.CreateRendererList(CreateTransparentRendererListDesc(cull, hdCamera.camera, m_AllTransparentPassNames,
                            rendererConfiguration: m_CurrentRendererConfigurationBakedLighting,
                            stateBlock: m_DepthStateOpaque)));

                    builder.SetRenderFunc(
                    (DebugViewMaterialData data, RenderGraphContext context) =>
                    {
                        DrawOpaqueRendererList(context, data.frameSettings, data.opaqueRendererList);
                        DrawTransparentRendererList(context, data.frameSettings, data.transparentRendererList);
                    });
                }
            }

            return output;
        }

        class PushFullScreenDebugPassData
        {
            public TextureHandle input;
            public TextureHandle output;
            public int mipIndex;
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
