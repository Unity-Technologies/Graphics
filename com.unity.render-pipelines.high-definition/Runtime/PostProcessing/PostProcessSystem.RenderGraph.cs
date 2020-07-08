using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{

    partial class PostProcessSystem
    {
        class ColorGradingPassData
        {
            public ColorGradingParameters   parameters;
            public TextureHandle            logLut;
        }

        class UberPostPassData
        {
            public UberPostParameters parameters;
            public TextureHandle source;
            public TextureHandle destination;
            public TextureHandle logLut;
            public TextureHandle bloomTexture;
        }

        class AlphaCopyPassData
        {
            public DoCopyAlphaParameters parameters;
            public TextureHandle source;
            public TextureHandle outputAlpha;
        }

        class GuardBandPassData
        {
            public ClearWithGuardBandsParameters parameters;
            public TextureHandle source;
        }

        class StopNaNPassData
        {
            public StopNaNParameters parameters;
            public TextureHandle source;
            public TextureHandle destination;
        }

        class DynamicExposureData
        {
            public ExposureParameters parameters;
            public TextureHandle source;
            public TextureHandle prevExposure;
            public TextureHandle nextExposure;
            public TextureHandle exposureDebugData;
            public TextureHandle tmpTarget1024;
            public TextureHandle tmpTarget32;
        }

        class ApplyExposureData
        {
            public ApplyExposureParameters parameters;
            public TextureHandle source;
            public TextureHandle destination;
            public TextureHandle prevExposure;
        }

        class TemporalAntiAliasingData
        {
            public TemporalAntiAliasingParameters parameters;
            public TextureHandle source;
            public TextureHandle destination;
            public TextureHandle motionVecTexture;
            public TextureHandle depthBuffer;
            public TextureHandle depthMipChain;
            public TextureHandle prevHistory;
            public TextureHandle nextHistory;
            public TextureHandle prevMVLen;
            public TextureHandle nextMVLen;
        }

        class SMAAData
        {
            public SMAAParameters parameters;
            public TextureHandle source;
            public TextureHandle destination;
            public TextureHandle depthBuffer;
            public TextureHandle smaaEdgeTex;
            public TextureHandle smaaBlendTex;
        }

        class FXAAData
        {
            public FXAAParameters parameters;
            public TextureHandle source;
            public TextureHandle destination;
        }

        class MotionBlurData
        {
            public MotionBlurParameters parameters;
            public TextureHandle source;
            public TextureHandle destination;
            public TextureHandle motionVecTexture;
            public TextureHandle preppedMotionVec;
            public TextureHandle minMaxTileVel;
            public TextureHandle maxTileNeigbourhood;
            public TextureHandle tileToScatterMax;
            public TextureHandle tileToScatterMin;
        }

        class PaniniProjectionData
        {
            public PaniniProjectionParameters parameters;
            public TextureHandle source;
            public TextureHandle destination;
        }

        class BloomData
        {
            public BloomParameters parameters;
            public TextureHandle source;
            public TextureHandle[] mipsDown = new TextureHandle[k_MaxBloomMipCount + 1];
            public TextureHandle[] mipsUp = new TextureHandle[k_MaxBloomMipCount + 1];
        }

        class CASData
        {
            public CASParameters parameters;
            public TextureHandle source;
            public TextureHandle destination;
        }

        TextureHandle GetPostprocessOutputHandle(RenderGraph renderGraph, string name)
        {
            return renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
            {
                name = name,
                colorFormat = m_ColorFormat,
                useMipMap = false,
                enableRandomWrite = true
            });
        }

        void FillBloomMipsTextureHandles(BloomData bloomData, RenderGraph renderGraph, RenderGraphBuilder builder)
        {
            for (int i = 0; i < m_BloomMipCount; i++)
            {
                var scale = new Vector2(m_BloomMipsInfo[i].z, m_BloomMipsInfo[i].w);
                var pixelSize = new Vector2Int((int)m_BloomMipsInfo[i].x, (int)m_BloomMipsInfo[i].y);

                bloomData.mipsDown[i] = builder.CreateTransientTexture(new TextureDesc(scale, true, true)
                { colorFormat = m_ColorFormat, enableRandomWrite = true });

                if (i != 0)
                {
                    bloomData.mipsUp[i] = builder.CreateTransientTexture(new TextureDesc(scale, true, true)
                    { colorFormat = m_ColorFormat, enableRandomWrite = true });

                }
            }

            // the mip up 0 will be used by uber, so not allocated as transient.
            var mip0Scale = new Vector2(m_BloomMipsInfo[0].z, m_BloomMipsInfo[0].w);
            bloomData.mipsUp[0] = renderGraph.CreateTexture(new TextureDesc(mip0Scale, true, true)
            {
                name = "Bloom final mip up",
                colorFormat = m_ColorFormat,
                useMipMap = false,
                enableRandomWrite = true
            });
        }

        public void Render(RenderGraph renderGraph,
                            HDCamera hdCamera,
                            BlueNoise blueNoise,
                            TextureHandle colorBuffer,
                            TextureHandle afterPostProcessTexture,
                            TextureHandle depthBuffer,
                            TextureHandle depthBufferMipChain,
                            TextureHandle motionVectors,
                            TextureHandle finalRT,
                            bool flipY)
        {
            var dynResHandler = DynamicResolutionHandler.instance;

            bool isSceneView = hdCamera.camera.cameraType == CameraType.SceneView;
            var source = colorBuffer;
            TextureHandle alphaTexture = renderGraph.defaultResources.whiteTextureXR;

            // Save the alpha and apply it back into the final pass if rendering in fp16 and post-processing in r11g11b10
            if (m_KeepAlpha)
            {
                using (var builder = renderGraph.AddRenderPass<AlphaCopyPassData>("Alpha Copy", out var passData, ProfilingSampler.Get(HDProfileId.AlphaCopy)))
                {
                    passData.parameters = PrepareCopyAlphaParameters(hdCamera);
                    passData.source = builder.ReadTexture(source);
                    passData.outputAlpha = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                    { name = "Alpha Channel Copy", colorFormat = GraphicsFormat.R16_SFloat, enableRandomWrite = true }));

                    builder.SetRenderFunc(
                    (AlphaCopyPassData data, RenderGraphContext ctx) =>
                    {
                        DoCopyAlpha(data.parameters,
                                        ctx.resources.GetTexture(data.source),
                                        ctx.resources.GetTexture(data.outputAlpha),
                                        ctx.cmd);
                    });

                    alphaTexture = passData.outputAlpha;
                }
            }

            if (m_PostProcessEnabled)
            {
                using (var builder = renderGraph.AddRenderPass<GuardBandPassData>("Guard Band Clear", out var passData, ProfilingSampler.Get(HDProfileId.GuardBandClear)))
                {
                    passData.source = builder.WriteTexture(source);
                    passData.parameters = PrepareClearWithGuardBandsParameters(hdCamera);

                    builder.SetRenderFunc(
                    (GuardBandPassData data, RenderGraphContext ctx) =>
                    {
                        ClearWithGuardBands(data.parameters, ctx.cmd, ctx.resources.GetTexture(data.source));
                    });

                    source = passData.source;
                }

                // Optional NaN killer before post-processing kicks in
                bool stopNaNs = hdCamera.stopNaNs && m_StopNaNFS;

#if UNITY_EDITOR
                if (isSceneView)
                    stopNaNs = HDAdditionalSceneViewSettings.sceneViewStopNaNs;
#endif
                if (stopNaNs)
                {
                    using (var builder = renderGraph.AddRenderPass<StopNaNPassData>("Stop NaNs", out var passData, ProfilingSampler.Get(HDProfileId.StopNaNs)))
                    {
                        passData.source = builder.ReadTexture(source);
                        passData.parameters = PrepareStopNaNParameters(hdCamera);
                        TextureHandle dest = GetPostprocessOutputHandle(renderGraph, "Stop NaNs Destination");
                        passData.destination = builder.WriteTexture(dest); ;

                        builder.SetRenderFunc(
                        (StopNaNPassData data, RenderGraphContext ctx) =>
                        {
                            DoStopNaNs(data.parameters, ctx.cmd, ctx.resources.GetTexture(data.source), ctx.resources.GetTexture(data.destination));
                        });

                        source = passData.destination;
                    }
                }

                // Dynamic exposure - will be applied in the next frame
                // Not considered as a post-process so it's not affected by its enabled state
                // Dynamic exposure - will be applied in the next frame
                // Not considered as a post-process so it's not affected by its enabled state
                if (!IsExposureFixed(hdCamera) && m_ExposureControlFS)
                {
                    var exposureParameters = PrepareExposureParameters(hdCamera);

                    GrabExposureRequiredTextures(hdCamera, out var prevExposure, out var nextExposure);

                    var prevExposureHandle = renderGraph.ImportTexture(prevExposure);
                    var nextExposureHandle = renderGraph.ImportTexture(nextExposure);

                    using (var builder = renderGraph.AddRenderPass<DynamicExposureData>("Dynamic Exposure", out var passData, ProfilingSampler.Get(HDProfileId.DynamicExposure)))
                    {
                        passData.source = builder.ReadTexture(source);
                        passData.parameters = PrepareExposureParameters(hdCamera);
                        passData.prevExposure = builder.ReadTexture(prevExposureHandle);
                        passData.nextExposure = builder.WriteTexture(nextExposureHandle);

                        if (m_Exposure.mode.value == ExposureMode.AutomaticHistogram)
                        {
                            passData.exposureDebugData = builder.WriteTexture(renderGraph.ImportTexture(m_DebugExposureData));
                            builder.SetRenderFunc(
                                (DynamicExposureData data, RenderGraphContext ctx) =>
                                {
                                    DoHistogramBasedExposure(data.parameters, ctx.cmd, ctx.resources.GetTexture(data.source),
                                                                                       ctx.resources.GetTexture(data.prevExposure),
                                                                                       ctx.resources.GetTexture(data.nextExposure),
                                                                                       ctx.resources.GetTexture(data.exposureDebugData));
                                });
                        }
                        else
                        {
                            passData.tmpTarget1024 = builder.CreateTransientTexture(new TextureDesc(1024, 1024, true, false)
                            { colorFormat = GraphicsFormat.R16G16_SFloat, enableRandomWrite = true, name = "Average Luminance Temp 1024" });
                            passData.tmpTarget32 = builder.CreateTransientTexture(new TextureDesc(32, 32, true, false)
                            { colorFormat = GraphicsFormat.R16G16_SFloat, enableRandomWrite = true, name = "Average Luminance Temp 32" });

                            builder.SetRenderFunc(
                                (DynamicExposureData data, RenderGraphContext ctx) =>
                                {
                                    DoDynamicExposure(data.parameters, ctx.cmd, ctx.resources.GetTexture(data.source),
                                                                                ctx.resources.GetTexture(data.prevExposure),
                                                                                ctx.resources.GetTexture(data.nextExposure),
                                                                                ctx.resources.GetTexture(data.tmpTarget1024),
                                                                                ctx.resources.GetTexture(data.tmpTarget32));
                                });
                        }
                    }

                    if (hdCamera.resetPostProcessingHistory)
                    {
                        using (var builder = renderGraph.AddRenderPass<ApplyExposureData>("Apply Exposure", out var passData, ProfilingSampler.Get(HDProfileId.ApplyExposure)))
                        {
                            passData.source = builder.ReadTexture(source);
                            passData.parameters = PrepareApplyExposureParameters(hdCamera);
                            RTHandle prevExp;
                            GrabExposureHistoryTextures(hdCamera, out prevExp, out _);
                            passData.prevExposure = builder.ReadTexture(renderGraph.ImportTexture(prevExp));

                            TextureHandle dest = GetPostprocessOutputHandle(renderGraph, "Apply Exposure Destination"); 
                            passData.destination = builder.WriteTexture(dest); ;

                            builder.SetRenderFunc(
                            (ApplyExposureData data, RenderGraphContext ctx) =>
                            {
                                ApplyExposure(data.parameters, ctx.cmd, ctx.resources.GetTexture(data.source), ctx.resources.GetTexture(data.destination), ctx.resources.GetTexture(data.prevExposure));
                            });

                            source = passData.destination;
                        }
                    }
                }

                // Temporal anti-aliasing goes first
                bool taaEnabled = false;


                //if (camera.frameSettings.IsEnabled(FrameSettingsField.CustomPostProcess))
                //{
                //    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.CustomPostProcessBeforeTAA)))
                //    {
                //        foreach (var typeString in HDRenderPipeline.defaultAsset.beforeTAACustomPostProcesses)
                //            RenderCustomPostProcess(cmd, camera, ref source, colorBuffer, Type.GetType(typeString));
                //    }
                //}

                if (m_AntialiasingFS)
                {
                    taaEnabled = hdCamera.antialiasing == HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;

                    if (taaEnabled)
                    {
                        using (var builder = renderGraph.AddRenderPass<TemporalAntiAliasingData>("Temporal Anti-Aliasing", out var passData, ProfilingSampler.Get(HDProfileId.TemporalAntialiasing)))
                        {
                            GrabTemporalAntialiasingHistoryTextures(hdCamera, out var prevHistory, out var nextHistory);
                            GrabVelocityMagnitudeHistoryTextures(hdCamera, out var prevMVLen, out var nextMVLen);

                            passData.source = builder.ReadTexture(source);
                            passData.parameters = PrepareTAAParameters(hdCamera);
                            passData.depthBuffer = builder.ReadTexture(depthBuffer);
                            passData.motionVecTexture = builder.ReadTexture(motionVectors);
                            passData.depthMipChain = builder.ReadTexture(depthBufferMipChain);
                            passData.prevHistory = builder.ReadTexture(renderGraph.ImportTexture(prevHistory));
                            if (passData.parameters.camera.resetPostProcessingHistory)
                            {
                                passData.prevHistory = builder.WriteTexture(passData.prevHistory);
                            }
                            passData.nextHistory = builder.WriteTexture(renderGraph.ImportTexture(nextHistory));
                            passData.prevMVLen = builder.ReadTexture(renderGraph.ImportTexture(prevMVLen));
                            passData.nextMVLen = builder.WriteTexture(renderGraph.ImportTexture(nextMVLen));

                            TextureHandle dest = GetPostprocessOutputHandle(renderGraph, "TAA Destination");
                            passData.destination = builder.WriteTexture(dest); ;

                            builder.SetRenderFunc(
                            (TemporalAntiAliasingData data, RenderGraphContext ctx) =>
                            {
                                DoTemporalAntialiasing(data.parameters, ctx.cmd, ctx.resources.GetTexture(data.source),
                                                                                 ctx.resources.GetTexture(data.destination),
                                                                                 ctx.resources.GetTexture(data.motionVecTexture),
                                                                                 ctx.resources.GetTexture(data.depthBuffer),
                                                                                 ctx.resources.GetTexture(data.depthMipChain),
                                                                                 ctx.resources.GetTexture(data.prevHistory),
                                                                                 ctx.resources.GetTexture(data.nextHistory),
                                                                                 ctx.resources.GetTexture(data.prevMVLen),
                                                                                 ctx.resources.GetTexture(data.nextMVLen));
                            });

                            source = passData.destination;
                        }
                    }
                    else if (hdCamera.antialiasing == HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing)
                    {
                        using (var builder = renderGraph.AddRenderPass<SMAAData>("Temporal Anti-Aliasing", out var passData, ProfilingSampler.Get(HDProfileId.SMAA)))
                        {
                            passData.source = builder.ReadTexture(source);
                            passData.parameters = PrepareSMAAParameters(hdCamera);
                            builder.ReadTexture(depthBuffer);
                            passData.depthBuffer = builder.WriteTexture(depthBuffer);
                            passData.smaaEdgeTex = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                            { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, enableRandomWrite = true, name = "SMAA Edge Texture" });
                            passData.smaaBlendTex = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                            { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, enableRandomWrite = true, name = "SMAA Blend Texture" });

                            TextureHandle dest = GetPostprocessOutputHandle(renderGraph, "SMAA Destination");
                            passData.destination = builder.WriteTexture(dest); ;

                            builder.SetRenderFunc(
                            (SMAAData data, RenderGraphContext ctx) =>
                            {
                                DoSMAA(data.parameters, ctx.cmd, ctx.resources.GetTexture(data.source),
                                                                 ctx.resources.GetTexture(data.smaaEdgeTex),
                                                                 ctx.resources.GetTexture(data.smaaBlendTex),
                                                                 ctx.resources.GetTexture(data.destination),
                                                                 ctx.resources.GetTexture(data.depthBuffer));
                            });

                            source = passData.destination;

                        }
                    }
                }

                //                if (camera.frameSettings.IsEnabled(FrameSettingsField.CustomPostProcess))
                //                {
                //                    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.CustomPostProcessBeforePP)))
                //                    {
                //                        foreach (var typeString in HDRenderPipeline.defaultAsset.beforePostProcessCustomPostProcesses)
                //                            RenderCustomPostProcess(cmd, camera, ref source, colorBuffer, Type.GetType(typeString));
                //                    }
                //                }

                //                // If Path tracing is enabled, then DoF is computed in the path tracer by sampling the lens aperure (when using the physical camera mode)
                //                bool isDoFPathTraced = (camera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) &&
                //                     camera.volumeStack.GetComponent<PathTracing>().enable.value &&
                //                     camera.camera.cameraType != CameraType.Preview &&
                //                     m_DepthOfField.focusMode == DepthOfFieldMode.UsePhysicalCamera);

                //                // Depth of Field is done right after TAA as it's easier to just re-project the CoC
                //                // map rather than having to deal with all the implications of doing it before TAA
                //                if (m_DepthOfField.IsActive() && !isSceneView && m_DepthOfFieldFS && !isDoFPathTraced)
                //                {
                //                    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DepthOfField)))
                //                    {
                //                        var destination = m_Pool.Get(Vector2.one, m_ColorFormat);
                //                        DoDepthOfField(cmd, camera, source, destination, taaEnabled);
                //                        PoolSource(ref source, destination);
                //                    }
                //                }

                // Motion blur after depth of field for aesthetic reasons (better to see motion
                // blurred bokeh rather than out of focus motion blur)
                if (m_MotionBlur.IsActive() && m_AnimatedMaterialsEnabled && !hdCamera.resetPostProcessingHistory && m_MotionBlurFS)
                {
                    using (var builder = renderGraph.AddRenderPass<MotionBlurData>("Motion Blur", out var passData, ProfilingSampler.Get(HDProfileId.MotionBlur)))
                    {
                        passData.source = builder.ReadTexture(source);
                        passData.parameters = PrepareMotionBlurParameters(hdCamera);

                        passData.motionVecTexture = builder.ReadTexture(motionVectors);

                        Vector2 tileTexScale = new Vector2((float)passData.parameters.tileTargetSize.x / hdCamera.actualWidth, (float)passData.parameters.tileTargetSize.y / hdCamera.actualHeight);

                        passData.preppedMotionVec = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                        { colorFormat = GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite = true, name = "Prepped Motion Vectors" });

                        passData.minMaxTileVel = builder.CreateTransientTexture(new TextureDesc(tileTexScale, true, true)
                        { colorFormat = GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite = true, name = "MinMax Tile Motion Vectors" });

                        passData.maxTileNeigbourhood = builder.CreateTransientTexture(new TextureDesc(tileTexScale, true, true)
                        { colorFormat = GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite = true, name = "Max Neighbourhood Tile" });

                        passData.tileToScatterMax = TextureHandle.nullHandle;
                        passData.tileToScatterMin = TextureHandle.nullHandle;

                        if (passData.parameters.motionblurSupportScattering)
                        {
                            passData.tileToScatterMax = builder.CreateTransientTexture(new TextureDesc(tileTexScale, true, true)
                            { colorFormat = GraphicsFormat.R32_UInt, enableRandomWrite = true, name = "Tile to Scatter Max" });

                            passData.tileToScatterMin = builder.CreateTransientTexture(new TextureDesc(tileTexScale, true, true)
                            { colorFormat = GraphicsFormat.R16_SFloat, enableRandomWrite = true, name = "Tile to Scatter Min" });
                        }

                        TextureHandle dest = GetPostprocessOutputHandle(renderGraph, "Motion Blur Destination");
                        passData.destination = builder.WriteTexture(dest); ;

                        builder.SetRenderFunc(
                        (MotionBlurData data, RenderGraphContext ctx) =>
                        {
                            DoMotionBlur(data.parameters, ctx.cmd, ctx.resources.GetTexture(data.source),
                                                                   ctx.resources.GetTexture(data.destination),
                                                                   ctx.resources.GetTexture(data.motionVecTexture),
                                                                   ctx.resources.GetTexture(data.preppedMotionVec),
                                                                   ctx.resources.GetTexture(data.minMaxTileVel),
                                                                   ctx.resources.GetTexture(data.maxTileNeigbourhood),
                                                                   ctx.resources.GetTexture(data.tileToScatterMax),
                                                                   ctx.resources.GetTexture(data.tileToScatterMin));
                        });

                        source = passData.destination;

                    }
                }

                // Panini projection is done as a fullscreen pass after all depth-based effects are
                // done and before bloom kicks in
                // This is one effect that would benefit from an overscan mode or supersampling in
                // HDRP to reduce the amount of resolution lost at the center of the screen
                if (m_PaniniProjection.IsActive() && !isSceneView && m_PaniniProjectionFS)
                {
                    using (var builder = renderGraph.AddRenderPass<PaniniProjectionData>("Panini Projection", out var passData, ProfilingSampler.Get(HDProfileId.PaniniProjection)))
                    {
                        passData.source = builder.ReadTexture(source);
                        passData.parameters = PreparePaniniProjectionParameters(hdCamera);
                        TextureHandle dest = GetPostprocessOutputHandle(renderGraph, "Panini Projection Destination");
                        passData.destination = builder.WriteTexture(dest);

                        builder.SetRenderFunc(
                        (PaniniProjectionData data, RenderGraphContext ctx) =>
                        {
                            DoPaniniProjection(data.parameters, ctx.cmd, ctx.resources.GetTexture(data.source), ctx.resources.GetTexture(data.destination));
                        });

                        source = passData.destination;
                    }
                }

                bool bloomActive = m_Bloom.IsActive() && m_BloomFS;
                TextureHandle bloomTexture = renderGraph.defaultResources.blackTextureXR;
                if (bloomActive)
                {
                    ComputeBloomMipSizesAndScales(hdCamera);
                    using (var builder = renderGraph.AddRenderPass<BloomData>("Bloom", out var passData, ProfilingSampler.Get(HDProfileId.Bloom)))
                    {
                        passData.source = builder.ReadTexture(source);
                        passData.parameters = PrepareBloomParameters(hdCamera);
                        FillBloomMipsTextureHandles(passData, renderGraph, builder);
                        passData.mipsUp[0] = builder.WriteTexture(passData.mipsUp[0]);


                        builder.SetRenderFunc(
                        (BloomData data, RenderGraphContext ctx) =>
                        {
                            var bloomMipDown = ctx.renderGraphPool.GetTempArray<RTHandle>(data.parameters.bloomMipCount);
                            var bloomMipUp   = ctx.renderGraphPool.GetTempArray<RTHandle>(data.parameters.bloomMipCount);

                            for(int i=0; i<data.parameters.bloomMipCount; ++i)
                            {
                                bloomMipDown[i] = ctx.resources.GetTexture(data.mipsDown[i]);
                                bloomMipUp[i]   = ctx.resources.GetTexture(data.mipsUp[i]);
                            }

                            DoBloom(data.parameters, ctx.cmd, ctx.resources.GetTexture(data.source), bloomMipDown, bloomMipUp);
                        });

                        bloomTexture = passData.mipsUp[0];
                    }
                }

                TextureHandle logLutOutput;
                using (var builder = renderGraph.AddRenderPass<ColorGradingPassData>("Color Grading", out var passData, ProfilingSampler.Get(HDProfileId.ColorGradingLUTBuilder)))
                {
                    TextureHandle logLut = renderGraph.CreateTexture(new TextureDesc(m_LutSize, m_LutSize)
                    {
                        name = "Color Grading Log Lut",
                        dimension = TextureDimension.Tex3D,
                        slices = m_LutSize,
                        depthBufferBits = DepthBits.None,
                        colorFormat = m_LutFormat,
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        anisoLevel = 0,
                        useMipMap = false,
                        enableRandomWrite = true
                    });

                    passData.parameters = PrepareColorGradingParameters();
                    passData.logLut = builder.WriteTexture(logLut);
                    logLutOutput = passData.logLut;

                    builder.SetRenderFunc(
                    (ColorGradingPassData data, RenderGraphContext ctx) =>
                    {
                        DoColorGrading(data.parameters, ctx.resources.GetTexture(data.logLut), ctx.cmd);
                    });
                }

                using (var builder = renderGraph.AddRenderPass<UberPostPassData>("Uber Post", out var passData, ProfilingSampler.Get(HDProfileId.UberPost)))
                {
                    TextureHandle dest = GetPostprocessOutputHandle(renderGraph, "Uber Post Destination");


                    passData.parameters = PrepareUberPostParameters(hdCamera, isSceneView);
                    passData.source = builder.ReadTexture(source);
                    passData.bloomTexture = builder.ReadTexture(bloomTexture);
                    passData.logLut = builder.ReadTexture(logLutOutput);
                    passData.destination = builder.WriteTexture(dest);


                    builder.SetRenderFunc(
                    (UberPostPassData data, RenderGraphContext ctx) =>
                    {
                        DoUberPostProcess(data.parameters,
                                                    ctx.resources.GetTexture(data.source),
                                                    ctx.resources.GetTexture(data.destination),
                                                    ctx.resources.GetTexture(data.logLut),
                                                    ctx.resources.GetTexture(data.bloomTexture),  // TODO: TMP VALUE, should be bloom texture and will be as soon as PP is ported to rendergraph.
                                                    ctx.cmd);
                    });

                    source = passData.destination;
                }

                m_HDInstance.PushFullScreenDebugTexture(renderGraph, source, FullScreenDebugMode.ColorLog);

                //                if (camera.frameSettings.IsEnabled(FrameSettingsField.CustomPostProcess))
                //                {
                //                    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.CustomPostProcessAfterPP)))
                //                    {
                //                        foreach (var typeString in HDRenderPipeline.defaultAsset.afterPostProcessCustomPostProcesses)
                //                            RenderCustomPostProcess(cmd, camera, ref source, colorBuffer, Type.GetType(typeString));
                //                    }
                //                }


                if (dynResHandler.DynamicResolutionEnabled() &&     // Dynamic resolution is on.
                    hdCamera.antialiasing == HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing &&
                    m_AntialiasingFS)
                {
                    using (var builder = renderGraph.AddRenderPass<FXAAData>("FXAA", out var passData, ProfilingSampler.Get(HDProfileId.FXAA)))
                    {
                        passData.source = builder.ReadTexture(source);
                        passData.parameters = PrepareFXAAParameters(hdCamera);
                        TextureHandle dest = GetPostprocessOutputHandle(renderGraph, "FXAA Destination");
                        passData.destination = builder.WriteTexture(dest); ;

                        builder.SetRenderFunc(
                        (FXAAData data, RenderGraphContext ctx) =>
                        {
                            DoFXAA(data.parameters, ctx.cmd, ctx.resources.GetTexture(data.source), ctx.resources.GetTexture(data.destination));
                        });

                        source = passData.destination;
                    }
                }

                hdCamera.resetPostProcessingHistory = false;
            }

            // Contrast Adaptive Sharpen Upscaling
            if (dynResHandler.DynamicResolutionEnabled() &&
                dynResHandler.filter == DynamicResUpscaleFilter.ContrastAdaptiveSharpen)
            {
                using (var builder = renderGraph.AddRenderPass<CASData>("Contrast Adaptive Sharpen", out var passData, ProfilingSampler.Get(HDProfileId.ContrastAdaptiveSharpen)))
                {
                    passData.source = builder.ReadTexture(source);
                    passData.parameters = PrepareContrastAdaptiveSharpeningParameters(hdCamera);
                    TextureHandle dest = GetPostprocessOutputHandle(renderGraph, "Contrast Adaptive Sharpen Destination");
                    passData.destination = builder.WriteTexture(dest); ;

                    builder.SetRenderFunc(
                    (CASData data, RenderGraphContext ctx) =>
                    {
                        DoContrastAdaptiveSharpening(data.parameters, ctx.cmd, ctx.resources.GetTexture(data.source), ctx.resources.GetTexture(data.destination));
                    });

                    source = passData.destination;
                }
            }

            using (var builder = renderGraph.AddRenderPass<FinalPassData>("Final Pass", out var passData, ProfilingSampler.Get(HDProfileId.FinalPost)))
            {
                passData.parameters = PrepareFinalPass(hdCamera, blueNoise, flipY);
                passData.source = builder.ReadTexture(source);
                passData.afterPostProcessTexture = builder.ReadTexture(afterPostProcessTexture);
                passData.alphaTexture = builder.ReadTexture(alphaTexture);
                passData.destination = builder.WriteTexture(finalRT);

                builder.SetRenderFunc(
                (FinalPassData data, RenderGraphContext ctx) =>
                {
                    DoFinalPass(data.parameters,
                                    ctx.resources.GetTexture(data.source),
                                    ctx.resources.GetTexture(data.afterPostProcessTexture),
                                    ctx.resources.GetTexture(data.destination),
                                    ctx.resources.GetTexture(data.alphaTexture),
                                    ctx.cmd);
                });
            }
        }

        class FinalPassData
        {
            public FinalPassParameters  parameters;
            public TextureHandle        source;
            public TextureHandle        afterPostProcessTexture;
            public TextureHandle        alphaTexture;
            public TextureHandle        destination;
        }
    }
}
