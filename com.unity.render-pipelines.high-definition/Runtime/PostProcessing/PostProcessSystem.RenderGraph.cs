using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class PostProcessSystem
    {
        public void Render( RenderGraph                 renderGraph,
                            HDCamera                    hdCamera,
                            BlueNoise                   blueNoise,
                            TextureHandle   colorBuffer,
                            TextureHandle   afterPostProcessTexture,
                            TextureHandle   depthBuffer,
                            TextureHandle   finalRT,
                            bool                        flipY)
        {
            var dynResHandler = DynamicResolutionHandler.instance;

            bool isSceneView = hdCamera.camera.cameraType == CameraType.SceneView;

            // TODO: Implement
            TextureHandle alphaTexture = new TextureHandle();
            //// Save the alpha and apply it back into the final pass if working in fp16
            //if (m_KeepAlpha)
            //{
            //    using (new ProfilingSample(cmd, "Alpha Copy", CustomSamplerId.AlphaCopy.GetSampler()))
            //    {
            //        DoCopyAlpha(cmd, hdCamera, colorBuffer);
            //    }
            //}

            var source = colorBuffer;

            //if (m_PostProcessEnabled)
            //{
                // Guard bands (also known as "horrible hack") to avoid bleeding previous RTHandle
                // content into smaller viewports with some effects like Bloom that rely on bilinear
                // filtering and can't use clamp sampler and the likes
                // Note: some platforms can't clear a partial render target so we directly draw black triangles
                //{
                //    int w = hdCamera.actualWidth;
                //    int h = hdCamera.actualHeight;
                //    cmd.SetRenderTarget(source, 0, CubemapFace.Unknown, -1);

                //    if (w < source.rt.width || h < source.rt.height)
                //    {
                //        cmd.SetViewport(new Rect(w, 0, k_RTGuardBandSize, h));
                //        cmd.DrawProcedural(Matrix4x4.identity, m_ClearBlackMaterial, 0, MeshTopology.Triangles, 3, 1);
                //        cmd.SetViewport(new Rect(0, h, w + k_RTGuardBandSize, k_RTGuardBandSize));
                //        cmd.DrawProcedural(Matrix4x4.identity, m_ClearBlackMaterial, 0, MeshTopology.Triangles, 3, 1);
                //    }
                //}

//                // Optional NaN killer before post-processing kicks in
//                bool stopNaNs = hdCamera.stopNaNs && m_StopNaNFS;

//#if UNITY_EDITOR
//                if (isSceneView)
//                    stopNaNs = HDRenderPipelinePreferences.sceneViewStopNaNs;
//#endif

//                if (stopNaNs)
//                {
//                    using (new ProfilingSample(cmd, "Stop NaNs", CustomSamplerId.StopNaNs.GetSampler()))
//                    {
//                        var destination = m_Pool.Get(Vector2.one, k_ColorFormat);
//                        DoStopNaNs(cmd, hdCamera, source, destination);
//                        PoolSource(ref source, destination);
//                    }
//                }
            //}

            //// Dynamic exposure - will be applied in the next frame
            //// Not considered as a post-process so it's not affected by its enabled state
            //if (!IsExposureFixed() && m_ExposureControlFS)
            //{
            //    using (new ProfilingSample(cmd, "Dynamic Exposure", CustomSamplerId.Exposure.GetSampler()))
            //    {
            //        DoDynamicExposure(cmd, hdCamera, source);
            //    }
            //}

            //if (m_PostProcessEnabled)
            //{
            //    // Temporal anti-aliasing goes first
            //    bool taaEnabled = false;

            //    if (m_AntialiasingFS)
            //    {
            //        taaEnabled = hdCamera.antialiasing == AntialiasingMode.TemporalAntialiasing;

            //        if (taaEnabled)
            //        {
            //            using (new ProfilingSample(cmd, "Temporal Anti-aliasing", CustomSamplerId.TemporalAntialiasing.GetSampler()))
            //            {
            //                var destination = m_Pool.Get(Vector2.one, k_ColorFormat);
            //                DoTemporalAntialiasing(cmd, hdCamera, source, destination, depthBuffer);
            //                PoolSource(ref source, destination);
            //            }
            //        }
            //        else if (hdCamera.antialiasing == AntialiasingMode.SubpixelMorphologicalAntiAliasing)
            //        {
            //            using (new ProfilingSample(cmd, "SMAA", CustomSamplerId.SMAA.GetSampler()))
            //            {
            //                var destination = m_Pool.Get(Vector2.one, k_ColorFormat);
            //                DoSMAA(cmd, hdCamera, source, destination, depthBuffer);
            //                PoolSource(ref source, destination);
            //            }
            //        }
            //    }

            //    if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.CustomPostProcess))
            //    {
            //        using (new ProfilingSample(cmd, "Custom Post Processes Before PP", CustomSamplerId.CustomPostProcessBeforePP.GetSampler()))
            //        {
            //            foreach (var typeString in HDRenderPipeline.currentAsset.beforePostProcessCustomPostProcesses)
            //                RenderCustomPostProcess(cmd, hdCamera, ref source, colorBuffer, Type.GetType(typeString));
            //        }
            //    }

            //    // Depth of Field is done right after TAA as it's easier to just re-project the CoC
            //    // map rather than having to deal with all the implications of doing it before TAA
            //    if (m_DepthOfField.IsActive() && !isSceneView && m_DepthOfFieldFS)
            //    {
            //        using (new ProfilingSample(cmd, "Depth of Field", CustomSamplerId.DepthOfField.GetSampler()))
            //        {
            //            var destination = m_Pool.Get(Vector2.one, k_ColorFormat);
            //            DoDepthOfField(cmd, hdCamera, source, destination, taaEnabled);
            //            PoolSource(ref source, destination);
            //        }
            //    }

            //    // Motion blur after depth of field for aesthetic reasons (better to see motion
            //    // blurred bokeh rather than out of focus motion blur)
            //    if (m_MotionBlur.IsActive() && m_AnimatedMaterialsEnabled && !m_ResetHistory && m_MotionBlurFS)
            //    {
            //        using (new ProfilingSample(cmd, "Motion Blur", CustomSamplerId.MotionBlur.GetSampler()))
            //        {
            //            var destination = m_Pool.Get(Vector2.one, k_ColorFormat);
            //            DoMotionBlur(cmd, hdCamera, source, destination);
            //            PoolSource(ref source, destination);
            //        }
            //    }

            //    // Panini projection is done as a fullscreen pass after all depth-based effects are
            //    // done and before bloom kicks in
            //    // This is one effect that would benefit from an overscan mode or supersampling in
            //    // HDRP to reduce the amount of resolution lost at the center of the screen
            //    if (m_PaniniProjection.IsActive() && !isSceneView && m_PaniniProjectionFS)
            //    {
            //        using (new ProfilingSample(cmd, "Panini Projection", CustomSamplerId.PaniniProjection.GetSampler()))
            //        {
            //            var destination = m_Pool.Get(Vector2.one, k_ColorFormat);
            //            DoPaniniProjection(cmd, hdCamera, source, destination);
            //            PoolSource(ref source, destination);
            //        }
            //    }

            //    // Combined post-processing stack - always runs if postfx is enabled
            //    using (new ProfilingSample(cmd, "Uber", CustomSamplerId.UberPost.GetSampler()))
            //    {
            //        // Feature flags are passed to all effects and it's their responsibility to check
            //        // if they are used or not so they can set default values if needed
            //        var cs = m_Resources.shaders.uberPostCS;
            //        var featureFlags = GetUberFeatureFlags(isSceneView);
            //        int kernel = GetUberKernel(cs, featureFlags);

            //        // Generate the bloom texture
            //        bool bloomActive = m_Bloom.IsActive() && m_BloomFS;

            //        if (bloomActive)
            //        {
            //            using (new ProfilingSample(cmd, "Bloom", CustomSamplerId.Bloom.GetSampler()))
            //            {
            //                DoBloom(cmd, hdCamera, source, cs, kernel);
            //            }
            //        }
            //        else
            //        {
            //            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._BloomTexture, TextureXR.GetBlackTexture());
            //            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._BloomDirtTexture, Texture2D.blackTexture);
            //            cmd.SetComputeVectorParam(cs, HDShaderIDs._BloomParams, Vector4.zero);
            //        }

            //        // Build the color grading lut
            //        using (new ProfilingSample(cmd, "Color Grading LUT Builder", CustomSamplerId.ColorGradingLUTBuilder.GetSampler()))
            //        {
            //            DoColorGrading(cmd, cs, kernel);
            //        }

            //        // Setup the rest of the effects
            //        DoLensDistortion(cmd, cs, kernel, featureFlags);
            //        DoChromaticAberration(cmd, cs, kernel, featureFlags);
            //        DoVignette(cmd, cs, kernel, featureFlags);

            //        // Run
            //        var destination = m_Pool.Get(Vector2.one, k_ColorFormat);

            //        bool outputColorLog = m_HDInstance.m_CurrentDebugDisplaySettings.data.fullScreenDebugMode == FullScreenDebugMode.ColorLog;
            //        cmd.SetComputeVectorParam(cs, "_DebugFlags", new Vector4(outputColorLog ? 1 : 0, 0, 0, 0));
            //        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, source);
            //        cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, destination);
            //        cmd.DispatchCompute(cs, kernel, (hdCamera.actualWidth + 7) / 8, (hdCamera.actualHeight + 7) / 8, hdCamera.viewCount);
            //        m_HDInstance.PushFullScreenDebugTexture(hdCamera, cmd, destination, FullScreenDebugMode.ColorLog);

            //        // Cleanup
            //        if (bloomActive) m_Pool.Recycle(m_BloomTexture);
            //        m_BloomTexture = null;

            //        PoolSource(ref source, destination);
            //    }

            //    if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.CustomPostProcess))
            //    {
            //        using (new ProfilingSample(cmd, "Custom Post Processes After PP", CustomSamplerId.CustomPostProcessAfterPP.GetSampler()))
            //        {
            //            foreach (var typeString in HDRenderPipeline.currentAsset.afterPostProcessCustomPostProcesses)
            //                RenderCustomPostProcess(cmd, hdCamera, ref source, colorBuffer, Type.GetType(typeString));
            //        }
            //    }
            //}

            //if (dynResHandler.DynamicResolutionEnabled() &&     // Dynamic resolution is on.
            //    hdCamera.antialiasing == AntialiasingMode.FastApproximateAntialiasing &&
            //    m_AntialiasingFS)
            //{
            //    using (new ProfilingSample(cmd, "FXAA", CustomSamplerId.FXAA.GetSampler()))
            //    {
            //        var destination = m_Pool.Get(Vector2.one, k_ColorFormat);
            //        DoFXAA(cmd, hdCamera, source, destination);
            //        PoolSource(ref source, destination);
            //    }
            //}

            using (var builder = renderGraph.AddRenderPass<FinalPassData>("Final Pass", out var passData, ProfilingSampler.Get(HDProfileId.FinalPost)))
            {
                passData.parameters = PrepareFinalPass(hdCamera, blueNoise, flipY);
                passData.source = builder.ReadTexture(source);
                passData.afterPostProcessTexture = builder.ReadTexture(afterPostProcessTexture);
                passData.alphaTexture = builder.ReadTexture(m_KeepAlpha ? alphaTexture : renderGraph.ImportTexture(TextureXR.GetWhiteTexture()));
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
