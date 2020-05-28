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

        public void Render( RenderGraph     renderGraph,
                            HDCamera        hdCamera,
                            BlueNoise       blueNoise,
                            TextureHandle   colorBuffer,
                            TextureHandle   afterPostProcessTexture,
                            TextureHandle   depthBuffer,
                            TextureHandle   finalRT,
                            bool                        flipY)
        {
            var dynResHandler = DynamicResolutionHandler.instance;

            bool isSceneView = hdCamera.camera.cameraType == CameraType.SceneView;
            var source = colorBuffer;
            TextureHandle alphaTexture = new TextureHandle();

            // TODO RENDERGRAPH: Implement
            //            // Save the alpha and apply it back into the final pass if rendering in fp16 and post-processing in r11g11b10
            //            if (m_KeepAlpha)
            //            {
            //                using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.AlphaCopy)))
            //                {
            //                    DoCopyAlpha(cmd, camera, colorBuffer);
            //                }
            //            }
            //            var source = colorBuffer;

            //            if (m_PostProcessEnabled)
            //            {
            //                // Guard bands (also known as "horrible hack") to avoid bleeding previous RTHandle
            //                // content into smaller viewports with some effects like Bloom that rely on bilinear
            //                // filtering and can't use clamp sampler and the likes
            //                // Note: some platforms can't clear a partial render target so we directly draw black triangles
            //                {
            //                    int w = camera.actualWidth;
            //                    int h = camera.actualHeight;
            //                    cmd.SetRenderTarget(source, 0, CubemapFace.Unknown, -1);

            //                    if (w < source.rt.width || h < source.rt.height)
            //                    {
            //                        cmd.SetViewport(new Rect(w, 0, k_RTGuardBandSize, h));
            //                        cmd.DrawProcedural(Matrix4x4.identity, m_ClearBlackMaterial, 0, MeshTopology.Triangles, 3, 1);
            //                        cmd.SetViewport(new Rect(0, h, w + k_RTGuardBandSize, k_RTGuardBandSize));
            //                        cmd.DrawProcedural(Matrix4x4.identity, m_ClearBlackMaterial, 0, MeshTopology.Triangles, 3, 1);
            //                    }
            //                }

            //                // Optional NaN killer before post-processing kicks in
            //                bool stopNaNs = camera.stopNaNs && m_StopNaNFS;

            //#if UNITY_EDITOR
            //                if (isSceneView)
            //                    stopNaNs = HDAdditionalSceneViewSettings.sceneViewStopNaNs;
            //#endif

            //                if (stopNaNs)
            //                {
            //                    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.StopNaNs)))
            //                    {
            //                        var destination = m_Pool.Get(Vector2.one, m_ColorFormat);
            //                        DoStopNaNs(cmd, camera, source, destination);
            //                        PoolSource(ref source, destination);
            //                    }
            //                }
            //            }

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
                        // Temp until bloom is implemented.
                        ctx.cmd.SetComputeTextureParam(data.parameters.uberPostCS, data.parameters.uberPostKernel, HDShaderIDs._BloomTexture, TextureXR.GetBlackTexture());
                        ctx.cmd.SetComputeTextureParam(data.parameters.uberPostCS, data.parameters.uberPostKernel, HDShaderIDs._BloomDirtTexture, Texture2D.blackTexture);
                        ctx.cmd.SetComputeVectorParam(data.parameters.uberPostCS, HDShaderIDs._BloomParams, Vector4.zero);


                        DoUberPostProcess(  data.parameters,
                                            ctx.resources.GetTexture(data.source),
                                            ctx.resources.GetTexture(data.destination),
                                            ctx.resources.GetTexture(data.logLut),
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
                passData.alphaTexture = builder.ReadTexture(m_KeepAlpha ? alphaTexture : renderGraph.defaultResources.whiteTextureXR);
                passData.destination = builder.WriteTexture(finalRT);

                builder.SetRenderFunc(
                (FinalPassData data, RenderGraphContext ctx) =>
                {
                    DoFinalPass(    data.parameters,
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
