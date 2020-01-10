using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        RenderGraphMutableResource m_DebugFullScreenTexture;

        class ResolveFullScreenDebugPassData
        {
            public DebugParameters debugParameters;
            public RenderGraphMutableResource output;
            public RenderGraphResource input;
            public RenderGraphResource depthPyramid;
        }

        RenderGraphMutableResource ResolveFullScreenDebug(RenderGraph renderGraph, in DebugParameters debugParameters, RenderGraphResource inputFullScreenDebug, RenderGraphResource depthPyramid)
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
                    ResolveFullScreenDebug(data.debugParameters,
                                            ctx.renderGraphPool.GetTempMaterialPropertyBlock(),
                                            ctx.resources.GetTexture(data.input),
                                            ctx.resources.GetTexture(data.depthPyramid),
                                            ctx.resources.GetTexture(data.output), ctx.cmd);
                });

                return passData.output;
            }
        }

        class ResolveColorPickerDebugPassData
        {
            public DebugParameters debugParameters;
            public RenderGraphMutableResource output;
            public RenderGraphResource input;
        }

        RenderGraphMutableResource ResolveColorPickerDebug(RenderGraph renderGraph, in DebugParameters debugParameters, RenderGraphResource inputColorPickerDebug)
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
                    ResolveColorPickerDebug(data.debugParameters,
                                            ctx.resources.GetTexture(data.input),
                                            ctx.resources.GetTexture(data.output),
                                            ctx.cmd);
                });

                return passData.output;
            }
        }

        class RenderDebugOverlayPassData
        {
            public DebugParameters debugParameters;
            public RenderGraphMutableResource colorBuffer;
            public RenderGraphMutableResource depthBuffer;
            public RenderGraphResource depthPyramidTexture;
            public ShadowResult shadowTextures;
        }

        void RenderDebugOverlays(   RenderGraph renderGraph,
                                    in DebugParameters debugParameters,
                                    RenderGraphMutableResource colorBuffer,
                                    RenderGraphMutableResource depthBuffer,
                                    RenderGraphResource depthPyramidTexture,
                                    in ShadowResult shadowResult)
        {
            using (var builder = renderGraph.AddRenderPass<RenderDebugOverlayPassData>("DebugOverlay", out var passData))
            {
                passData.debugParameters = debugParameters;
                passData.colorBuffer = builder.UseColorBuffer(colorBuffer, 0);
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);
                passData.depthPyramidTexture = builder.ReadTexture(depthPyramidTexture);
                passData.shadowTextures = HDShadowManager.ReadShadowResult(shadowResult, builder);

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
                    shadowAtlases.punctualShadowAtlas = data.shadowTextures.punctualShadowResult.IsValid() ? ctx.resources.GetTexture(data.shadowTextures.punctualShadowResult) : null;
                    shadowAtlases.cascadeShadowAtlas = data.shadowTextures.directionalShadowResult.IsValid() ? ctx.resources.GetTexture(data.shadowTextures.directionalShadowResult) : null;
                    shadowAtlases.areaShadowAtlas = data.shadowTextures.areaShadowResult.IsValid() ? ctx.resources.GetTexture(data.shadowTextures.areaShadowResult) : null;

                    RenderSkyReflectionOverlay(debugParams, ctx.cmd, ctx.renderGraphPool.GetTempMaterialPropertyBlock(), ref x, ref y, overlaySize);
                    RenderRayCountOverlay(debugParams, ctx.cmd, ref x, ref y, overlaySize);
                    RenderLightLoopDebugOverlay(debugParams, ctx.cmd, ref x, ref y, overlaySize, ctx.resources.GetTexture(data.depthPyramidTexture));
                    RenderShadowsDebugOverlay(debugParams, shadowAtlases, ctx.cmd, ref x, ref y, overlaySize, ctx.renderGraphPool.GetTempMaterialPropertyBlock());
                    DecalSystem.instance.RenderDebugOverlay(debugParams.hdCamera, ctx.cmd, debugParams.debugDisplaySettings, ref x, ref y, overlaySize, debugParams.hdCamera.actualWidth);
                });
            }
        }

        class RenderLightVolumesPassData
        {
            public DebugLightVolumes.RenderLightVolumesParameters   parameters;
            // Render target that holds the light count in floating points
            public RenderGraphMutableResource                       lightCountBuffer;
            // Render target that holds the color accumulated value
            public RenderGraphMutableResource                       colorAccumulationBuffer;
            // The output texture of the debug
            public RenderGraphMutableResource                       debugLightVolumesTexture;
            // Required depth texture given that we render multiple render targets
            public RenderGraphMutableResource                       depthBuffer;
            public RenderGraphMutableResource                       destination;
        }

        static void RenderLightVolumes(RenderGraph renderGraph, in DebugParameters debugParameters, RenderGraphMutableResource destination, RenderGraphMutableResource depthBuffer, CullingResults cullResults)
        {
            using (var builder = renderGraph.AddRenderPass<RenderLightVolumesPassData>("LightVolumes", out var passData))
            {
                passData.parameters = s_lightVolumes.PrepareLightVolumeParameters(debugParameters.hdCamera, debugParameters.debugDisplaySettings.data.lightingDebugSettings, cullResults);
                passData.lightCountBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat= GraphicsFormat.R32_SFloat, clearBuffer = true, clearColor = Color.black, name = "LightVolumeCount" }));
                passData.colorAccumulationBuffer = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, clearBuffer = true, clearColor = Color.black, name = "LightVolumeColorAccumulation" }));
                passData.debugLightVolumesTexture = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, clearBuffer = true, clearColor = Color.black, enableRandomWrite = true, name = "LightVolumeDebugLightVolumesTexture" }));
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);
                passData.destination = builder.WriteTexture(destination);

                builder.SetRenderFunc(
                (RenderLightVolumesPassData data, RenderGraphContext ctx) =>
                {
                    RenderTargetIdentifier[] mrt = ctx.renderGraphPool.GetTempArray<RenderTargetIdentifier>(2);
                    var lightCountBuffer = ctx.resources.GetTexture(data.lightCountBuffer);
                    var colorAccumulationBuffer = ctx.resources.GetTexture(data.colorAccumulationBuffer);
                    mrt[0] = lightCountBuffer;
                    mrt[1] = colorAccumulationBuffer;

                    DebugLightVolumes.RenderLightVolumes(   ctx.cmd,
                                                            data.parameters,
                                                            mrt, lightCountBuffer,
                                                            colorAccumulationBuffer,
                                                            ctx.resources.GetTexture(data.debugLightVolumesTexture),
                                                            ctx.resources.GetTexture(data.depthBuffer),
                                                            ctx.resources.GetTexture(data.destination),
                                                            ctx.renderGraphPool.GetTempMaterialPropertyBlock());
                });
            }
        }

        RenderGraphMutableResource RenderDebug( RenderGraph                 renderGraph,
                                                HDCamera                    hdCamera,
                                                RenderGraphMutableResource  colorBuffer,
                                                RenderGraphMutableResource  depthBuffer,
                                                RenderGraphResource         depthPyramidTexture,
                                                RenderGraphResource         fullScreenDebugTexture,
                                                RenderGraphResource         colorPickerDebugTexture,
                                                in ShadowResult             shadowResult,
                                                CullingResults              cullResults)
        {
            // We don't want any overlay for these kind of rendering
            if (hdCamera.camera.cameraType == CameraType.Reflection || hdCamera.camera.cameraType == CameraType.Preview)
                return colorBuffer;

            RenderGraphMutableResource output = colorBuffer;
            var debugParameters = PrepareDebugParameters(hdCamera, GetDepthBufferMipChainInfo());

            if (debugParameters.resolveFullScreenDebug)
            {
                output = ResolveFullScreenDebug(renderGraph, debugParameters, fullScreenDebugTexture, depthPyramidTexture);
                // If we have full screen debug, this is what we want color picked, so we replace color picker input texture with the new one.
                if (debugParameters.colorPickerEnabled)
                    colorPickerDebugTexture = PushColorPickerDebugTexture(renderGraph, output);

                m_FullScreenDebugPushed = false;
            }

            if (debugParameters.colorPickerEnabled)
                output = ResolveColorPickerDebug(renderGraph, debugParameters, colorPickerDebugTexture);

            if (debugParameters.debugDisplaySettings.data.lightingDebugSettings.displayLightVolumes)
            {
                RenderLightVolumes(renderGraph, debugParameters, output, depthBuffer, cullResults);
            }

            RenderDebugOverlays(renderGraph, debugParameters, output, depthBuffer, depthPyramidTexture, shadowResult);

            return output;
        }

        class DebugViewMaterialData
        {
            public RenderGraphMutableResource outputColor;
            public RenderGraphMutableResource outputDepth;
            public RenderGraphResource opaqueRendererList;
            public RenderGraphResource transparentRendererList;
            public Material debugGBufferMaterial;
            public FrameSettings frameSettings;
        }

        void RenderDebugViewMaterial(RenderGraph renderGraph, CullingResults cull, HDCamera hdCamera, RenderGraphMutableResource output)
        {
            if (m_CurrentDebugDisplaySettings.data.materialDebugSettings.IsDebugGBufferEnabled() && hdCamera.frameSettings.litShaderMode == LitShaderMode.Deferred)
            {
                using (var builder = renderGraph.AddRenderPass<DebugViewMaterialData>("DebugViewMaterialGBuffer", out var passData, ProfilingSampler.Get(HDProfileId.DebugViewMaterialGBuffer)))
                {
                    passData.debugGBufferMaterial = m_currentDebugViewMaterialGBuffer;
                    passData.outputColor = builder.WriteTexture(output);

                    builder.SetRenderFunc(
                    (DebugViewMaterialData data, RenderGraphContext context) =>
                    {
                        var res = context.resources;
                        HDUtils.DrawFullScreen(context.cmd, data.debugGBufferMaterial, res.GetTexture(data.outputColor));
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
                        var res = context.resources;
                        DrawOpaqueRendererList(context, data.frameSettings, res.GetRendererList(data.opaqueRendererList));
                        DrawTransparentRendererList(context, data.frameSettings, res.GetRendererList(data.transparentRendererList));
                    });
                }
            }
        }

        class PushFullScreenDebugPassData
        {
            public RenderGraphResource input;
            public RenderGraphMutableResource output;
            public Vector4 scaleBias;
            public int mipIndex;
        }

        void PushFullScreenLightingDebugTexture(RenderGraph renderGraph, RenderGraphResource input)
        {
            // In practice, this is only useful for the SingleShadow debug view.
            // TODO: See how we can make this nicer than a specific functions just for one case.
            if (NeedsFullScreenDebugMode() && m_FullScreenDebugPushed == false)
            {
                PushFullScreenDebugTexture(renderGraph, input, Vector4.one);
            }
        }

        void PushFullScreenDebugTexture(RenderGraph renderGraph, RenderGraphResource input, FullScreenDebugMode debugMode)
        {
            if (debugMode == m_CurrentDebugDisplaySettings.data.fullScreenDebugMode)
            {
                PushFullScreenDebugTexture(renderGraph, input, Vector4.one);
            }
        }

        void PushFullScreenDebugTextureMip(RenderGraph renderGraph, RenderGraphResource input, int lodCount, Vector4 scaleBias, FullScreenDebugMode debugMode)
        {
            if (debugMode == m_CurrentDebugDisplaySettings.data.fullScreenDebugMode)
            {
                var mipIndex = Mathf.FloorToInt(m_CurrentDebugDisplaySettings.data.fullscreenDebugMip * (lodCount));

                PushFullScreenDebugTexture(renderGraph, input, scaleBias, mipIndex);
            }
        }

        void PushFullScreenDebugTexture(RenderGraph renderGraph, RenderGraphResource input, Vector4 scaleBias, int mipIndex = -1)
        {
            using (var builder = renderGraph.AddRenderPass<PushFullScreenDebugPassData>("Push Full Screen Debug", out var passData))
            {
                passData.scaleBias = mipIndex != -1 ? scaleBias : new Vector4(1.0f, 1.0f, 0.0f, 0.0f);
                passData.mipIndex = mipIndex;
                passData.input = builder.ReadTexture(input);
                passData.output = builder.UseColorBuffer(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, name = "DebugFullScreen" }), 0);

                builder.SetRenderFunc(
                (PushFullScreenDebugPassData data, RenderGraphContext ctx) =>
                {
                    if (data.mipIndex != -1)
                        HDUtils.BlitCameraTexture(ctx.cmd, ctx.resources.GetTexture(passData.input), ctx.resources.GetTexture(passData.output), data.scaleBias, data.mipIndex);
                    else
                        HDUtils.BlitCameraTexture(ctx.cmd, ctx.resources.GetTexture(passData.input), ctx.resources.GetTexture(passData.output));
                });

                m_DebugFullScreenTexture = passData.output;
            }

            // We need this flag because otherwise if no full screen debug is pushed (like for example if the corresponding pass is disabled), when we render the result in RenderDebug m_DebugFullScreenTempBuffer will contain potential garbage
            m_FullScreenDebugPushed = true;
        }

        RenderGraphResource PushColorPickerDebugTexture(RenderGraph renderGraph, RenderGraphResource input)
        {
            using (var builder = renderGraph.AddRenderPass<PushFullScreenDebugPassData>("Push To Color Picker", out var passData))
            {
                passData.input = builder.ReadTexture(input);
                passData.output = builder.UseColorBuffer(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, name = "DebugColorPicker" }), 0);

                builder.SetRenderFunc(
                (PushFullScreenDebugPassData data, RenderGraphContext ctx) =>
                {
                    HDUtils.BlitCameraTexture(ctx.cmd, ctx.resources.GetTexture(passData.input), ctx.resources.GetTexture(passData.output));
                });

                return passData.output;
            }
        }
    }
}
