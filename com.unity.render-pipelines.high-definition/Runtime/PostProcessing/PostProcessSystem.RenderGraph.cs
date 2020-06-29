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

        public void Render(RenderGraph renderGraph,
                            HDCamera hdCamera,
                            BlueNoise blueNoise,
                            TextureHandle colorBuffer,
                            TextureHandle afterPostProcessTexture,
                            TextureHandle depthBuffer,
                            TextureHandle depthBufferMipChain,
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
                // Guard bands (also known as "horrible hack") to avoid bleeding previous RTHandle
                // content into smaller viewports with some effects like Bloom that rely on bilinear
                // filtering and can't use clamp sampler and the likes
                // Note: some platforms can't clear a partial render target so we directly draw black triangles
                using (var builder = renderGraph.AddRenderPass<GuardBandPassData>("Guard Band Clear", out var passData, ProfilingSampler.Get(HDProfileId.GuardBandClear)))
                {
                    passData.source = builder.ReadTexture(source);
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
                        TextureHandle dest = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                        {
                            name = "Stop NaNs Destination",
                            colorFormat = m_ColorFormat,
                            useMipMap = false,
                            enableRandomWrite = true
                        });
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
                        passData.prevExposure = prevExposureHandle;
                        passData.nextExposure = nextExposureHandle;

                        if (m_Exposure.mode.value == ExposureMode.AutomaticHistogram)
                        {
                            passData.exposureDebugData = renderGraph.ImportTexture(m_DebugExposureData);
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
                            passData.prevExposure = renderGraph.ImportTexture(prevExp);

                            TextureHandle dest = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                            {
                                name = "Apply Exposure Destination",
                                colorFormat = m_ColorFormat,
                                useMipMap = false,
                                enableRandomWrite = true
                            });
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
                            passData.depthMipChain = builder.ReadTexture(depthBufferMipChain);
                            passData.prevHistory = renderGraph.ImportTexture(prevHistory);
                            passData.nextHistory = renderGraph.ImportTexture(nextHistory);
                            passData.prevMVLen = renderGraph.ImportTexture(prevMVLen);
                            passData.nextMVLen = renderGraph.ImportTexture(nextMVLen);

                            TextureHandle dest = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                            {
                                name = "TAA Destination",
                                colorFormat = m_ColorFormat,
                                useMipMap = false,
                                enableRandomWrite = true
                            });
                            passData.destination = builder.WriteTexture(dest); ;

                            builder.SetRenderFunc(
                            (TemporalAntiAliasingData data, RenderGraphContext ctx) =>
                            {
                                DoTemporalAntialiasing(data.parameters, ctx.cmd, ctx.resources.GetTexture(data.source),
                                                                                 ctx.resources.GetTexture(data.destination),
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
                        using (var builder = renderGraph.AddRenderPass<SMAAData>("Temporal Anti-Aliasing", out var passData, ProfilingSampler.Get(HDProfileId.TemporalAntialiasing)))
                        {

                            passData.source = builder.ReadTexture(source);
                            passData.parameters = PrepareSMAAParameters(hdCamera);
                            builder.ReadTexture(depthBuffer);
                            passData.depthBuffer = builder.WriteTexture(depthBuffer);
                            passData.smaaEdgeTex = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                            { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, enableRandomWrite = true, name = "SMAA Edge Texture" });
                            passData.smaaBlendTex = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                            { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, enableRandomWrite = true, name = "SMAA Blend Texture" });
                            TextureHandle dest = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                            {
                                name = "SMAA Destination",
                                colorFormat = m_ColorFormat,
                                useMipMap = false,
                                enableRandomWrite = true
                            });
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


                            //  DoSMAA(PrepareSMAAParameters(camera), cmd, source, smaaEdgeTex, smaaBlendTex, destination, depthBuffer);

                        }
                        //{
                        //    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.SMAA)))
                        //    {
                        //        var destination = m_Pool.Get(Vector2.one, m_ColorFormat);
                        //        RTHandle smaaEdgeTex, smaaBlendTex;
                        //        AllocateSMAARenderTargets(camera, out smaaEdgeTex, out smaaBlendTex);
                        //        DoSMAA(PrepareSMAAParameters(camera), cmd, source, smaaEdgeTex, smaaBlendTex, destination, depthBuffer);
                        //        RecycleSMAARenderTargets(smaaEdgeTex, smaaBlendTex);
                        //        PoolSource(ref source, destination);
                        //    }
                        //}
                    }
                }

                // TODO RENDERGRAPH: Implement

                //            // Dynamic exposure - will be applied in the next frame
                //            // Not considered as a post-process so it's not affected by its enabled state
                //            if (!IsExposureFixed() && m_ExposureControlFS)
                //            {
                //                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.DynamicExposure)))
                //                {
                //                    if (m_Exposure.mode.value == ExposureMode.AutomaticHistogram)
                //                    {
                //                        DoHistogramBasedExposure(cmd, camera, source);
                //                    }
                //                    else
                //                    {
                //                        DoDynamicExposure(cmd, camera, source);
                //                    }

                //                    // On reset history we need to apply dynamic exposure immediately to avoid
                //                    // white or black screen flashes when the current exposure isn't anywhere
                //                    // near 0
                //                    if (camera.resetPostProcessingHistory)
                //                    {
                //                        var destination = m_Pool.Get(Vector2.one, m_ColorFormat);

                //                        var cs = m_Resources.shaders.applyExposureCS;
                //                        int kernel = cs.FindKernel("KMain");

                //                        // Note: we call GetPrevious instead of GetCurrent because the textures
                //                        // are swapped internally as the system expects the texture will be used
                //                        // on the next frame. So the actual "current" for this frame is in
                //                        // "previous".
                //                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureTexture, GetPreviousExposureTexture(camera));
                //                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, source);
                //                        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, destination);
                //                        cmd.DispatchCompute(cs, kernel, (camera.actualWidth + 7) / 8, (camera.actualHeight + 7) / 8, camera.viewCount);

                //                        PoolSource(ref source, destination);
                //                    }
                //                }
                //            }

                if (m_PostProcessEnabled)
                {
                    //                // Temporal anti-aliasing goes first
                    //                bool taaEnabled = false;

                    //                if (m_AntialiasingFS)
                    //                {
                    //                    taaEnabled = camera.antialiasing == AntialiasingMode.TemporalAntialiasing;

                    //                    if (taaEnabled)
                    //                    {
                    //                        using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.TemporalAntialiasing)))
                    //                        {
                    //                            var destination = m_Pool.Get(Vector2.one, m_ColorFormat);
                    //                            DoTemporalAntialiasing(cmd, camera, source, destination, depthBuffer, depthMipChain);
                    //                            PoolSource(ref source, destination);
                    //                        }
                    //                    }
                    //                    else if (camera.antialiasing == AntialiasingMode.SubpixelMorphologicalAntiAliasing)
                    //                    {
                    //                        using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.SMAA)))
                    //                        {
                    //                            var destination = m_Pool.Get(Vector2.one, m_ColorFormat);
                    //                            DoSMAA(cmd, camera, source, destination, depthBuffer);
                    //                            PoolSource(ref source, destination);
                    //                        }
                    //                    }
                    //                }

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

                    //                // Motion blur after depth of field for aesthetic reasons (better to see motion
                    //                // blurred bokeh rather than out of focus motion blur)
                    //                if (m_MotionBlur.IsActive() && m_AnimatedMaterialsEnabled && !camera.resetPostProcessingHistory && m_MotionBlurFS)
                    //                {
                    //                    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.MotionBlur)))
                    //                    {
                    //                        var destination = m_Pool.Get(Vector2.one, m_ColorFormat);
                    //                        DoMotionBlur(cmd, camera, source, destination);
                    //                        PoolSource(ref source, destination);
                    //                    }
                    //                }

                    //                // Panini projection is done as a fullscreen pass after all depth-based effects are
                    //                // done and before bloom kicks in
                    //                // This is one effect that would benefit from an overscan mode or supersampling in
                    //                // HDRP to reduce the amount of resolution lost at the center of the screen
                    //                if (m_PaniniProjection.IsActive() && !isSceneView && m_PaniniProjectionFS)
                    //                {
                    //                    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.PaniniProjection)))
                    //                    {
                    //                        var destination = m_Pool.Get(Vector2.one, m_ColorFormat);
                    //                        DoPaniniProjection(cmd, camera, source, destination);
                    //                        PoolSource(ref source, destination);
                    //                    }
                    //                }

                    // Uber post-process
                    //// Generate the bloom texture
                    //bool bloomActive = m_Bloom.IsActive() && m_BloomFS;

                    //if (bloomActive)
                    //{
                    //    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.Bloom)))
                    //    {
                    //        DoBloom(cmd, camera, source, uberPostParams.uberPostCS, uberPostParams.uberPostKernel);
                    //    }
                    //}
                    //else
                    //{
                    //    cmd.SetComputeTextureParam(uberPostParams.uberPostCS, uberPostParams.uberPostKernel, HDShaderIDs._BloomTexture, TextureXR.GetBlackTexture());
                    //    cmd.SetComputeTextureParam(uberPostParams.uberPostCS, uberPostParams.uberPostKernel, HDShaderIDs._BloomDirtTexture, Texture2D.blackTexture);
                    //    cmd.SetComputeVectorParam(uberPostParams.uberPostCS, HDShaderIDs._BloomParams, Vector4.zero);
                    //}

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
                        TextureHandle dest = renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                        {
                            name = "Uber Post Destination",
                            colorFormat = m_ColorFormat,
                            useMipMap = false,
                            enableRandomWrite = true
                        });

                        passData.parameters = PrepareUberPostParameters(hdCamera, isSceneView);
                        passData.source = builder.ReadTexture(source);
                        passData.logLut = builder.ReadTexture(logLutOutput);
                        passData.destination = builder.WriteTexture(dest);

                        builder.SetRenderFunc(
                        (UberPostPassData data, RenderGraphContext ctx) =>
                        {
                        //// Temp until bloom is implemented.
                        //ctx.cmd.SetComputeTextureParam(data.parameters.uberPostCS, data.parameters.uberPostKernel, HDShaderIDs._BloomTexture, TextureXR.GetBlackTexture());
                        //ctx.cmd.SetComputeTextureParam(data.parameters.uberPostCS, data.parameters.uberPostKernel, HDShaderIDs._BloomDirtTexture, Texture2D.blackTexture);
                        //ctx.cmd.SetComputeVectorParam(data.parameters.uberPostCS, HDShaderIDs._BloomParams, Vector4.zero);


                        DoUberPostProcess(data.parameters,
                                                ctx.resources.GetTexture(data.source),
                                                ctx.resources.GetTexture(data.destination),
                                                ctx.resources.GetTexture(data.logLut),
                                                ctx.resources.GetTexture(data.source),  // TODO: TMP VALUE, should be bloom texture and will be as soon as PP is ported to rendergraph.
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
                }

                //            if (dynResHandler.DynamicResolutionEnabled() &&     // Dynamic resolution is on.
                //                camera.antialiasing == AntialiasingMode.FastApproximateAntialiasing &&
                //                m_AntialiasingFS)
                //            {
                //                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.FXAA)))
                //                {
                //                    var destination = m_Pool.Get(Vector2.one, m_ColorFormat);
                //                    DoFXAA(cmd, camera, source, destination);
                //                    PoolSource(ref source, destination);
                //                }
                //            }

                //            // Contrast Adaptive Sharpen Upscaling
                //            if (dynResHandler.DynamicResolutionEnabled() &&
                //                dynResHandler.filter == DynamicResUpscaleFilter.ContrastAdaptiveSharpen)
                //            {
                //                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ContrastAdaptiveSharpen)))
                //                {
                //                    var destination = m_Pool.Get(Vector2.one, m_ColorFormat);

                //                    var cs = m_Resources.shaders.contrastAdaptiveSharpenCS;
                //                    int kInit = cs.FindKernel("KInitialize");
                //                    int kMain = cs.FindKernel("KMain");
                //                    if (kInit >= 0 && kMain >= 0)
                //                    {
                //                        cmd.SetComputeFloatParam(cs, HDShaderIDs._Sharpness, 1);
                //                        cmd.SetComputeTextureParam(cs, kMain, HDShaderIDs._InputTexture, source);
                //                        cmd.SetComputeVectorParam(cs, HDShaderIDs._InputTextureDimensions, new Vector4(source.rt.width, source.rt.height));
                //                        cmd.SetComputeTextureParam(cs, kMain, HDShaderIDs._OutputTexture, destination);
                //                        cmd.SetComputeVectorParam(cs, HDShaderIDs._OutputTextureDimensions, new Vector4(destination.rt.width, destination.rt.height));

                //                        ValidateComputeBuffer(ref m_ContrastAdaptiveSharpen, 2, sizeof(uint) * 4);

                //                        cmd.SetComputeBufferParam(cs, kInit, "CasParameters", m_ContrastAdaptiveSharpen);
                //                        cmd.SetComputeBufferParam(cs, kMain, "CasParameters", m_ContrastAdaptiveSharpen);

                //                        cmd.DispatchCompute(cs, kInit, 1, 1, 1);

                //                        int dispatchX = (int)System.Math.Ceiling(destination.rt.width / 16.0f);
                //                        int dispatchY = (int)System.Math.Ceiling(destination.rt.height / 16.0f);

                //                        cmd.DispatchCompute(cs, kMain, dispatchX, dispatchY, camera.viewCount);
                //                    }

                //                    PoolSource(ref source, destination);
                //                }
                //            }

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

                hdCamera.resetPostProcessingHistory = false;
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
