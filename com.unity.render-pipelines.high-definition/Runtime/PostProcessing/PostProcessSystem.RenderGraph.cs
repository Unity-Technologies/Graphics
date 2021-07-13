using System;
using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{

    partial class PostProcessSystem
    {
        class ColorGradingPassData
        {
            public ColorGradingParameters parameters;
            public TextureHandle logLut;
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
            public TextureHandle depthBuffer;
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

            public ComputeBufferHandle casParametersBuffer;
        }

        class DepthofFieldData
        {
            public DepthOfFieldParameters parameters;
            public TextureHandle source;
            public TextureHandle destination;
            public TextureHandle depthBuffer;
            public TextureHandle motionVecTexture;
            public TextureHandle pingNearRGB;
            public TextureHandle pongNearRGB;
            public TextureHandle nearCoC;
            public TextureHandle nearAlpha;
            public TextureHandle dilatedNearCoC;
            public TextureHandle pingFarRGB;
            public TextureHandle pongFarRGB;
            public TextureHandle farCoC;
            public TextureHandle fullresCoC;
            public TextureHandle[] mips = new TextureHandle[4];
            public TextureHandle dilationPingPongRT;
            public TextureHandle prevCoC;
            public TextureHandle nextCoC;

            public ComputeBufferHandle bokehNearKernel;
            public ComputeBufferHandle bokehFarKernel;
            public ComputeBufferHandle bokehIndirectCmd;
            public ComputeBufferHandle nearBokehTileList;
            public ComputeBufferHandle farBokehTileList;

            public bool taaEnabled;
        }

        class CustomPostProcessData
        {
            public TextureHandle source;
            public TextureHandle destination;
            public TextureHandle depthBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle motionVecTexture;
            public HDCamera hdCamera;
            public CustomPostProcessVolumeComponent customPostProcess;
        }

        TextureHandle GetPostprocessOutputHandle(RenderGraph renderGraph,  string name, bool dynamicResolution = true)
        {
            return renderGraph.CreateTexture(new TextureDesc(Vector2.one, dynamicResolution, true)
            {
                name = name,
                colorFormat = m_ColorFormat,
                useMipMap = false,
                enableRandomWrite = true
            });
        }

        TextureHandle GetPostprocessUpsampledOutputHandle(RenderGraph renderGraph, string name)
        {
            return GetPostprocessOutputHandle(renderGraph, name, false);
        }

        void FillBloomMipsTextureHandles(BloomData bloomData, RenderGraph renderGraph, RenderGraphBuilder builder)
        {
            for (int i = 0; i < m_BloomMipCount; i++)
            {
                var scale = new Vector2(m_BloomMipsInfo[i].z, m_BloomMipsInfo[i].w);
                var pixelSize = new Vector2Int((int)m_BloomMipsInfo[i].x, (int)m_BloomMipsInfo[i].y);

                bloomData.mipsDown[i] = builder.CreateTransientTexture(new TextureDesc(scale, true, true)
                { colorFormat = m_ColorFormat, enableRandomWrite = true, name = "BloomMipDown" });

                if (i != 0)
                {
                    bloomData.mipsUp[i] = builder.CreateTransientTexture(new TextureDesc(scale, true, true)
                    { colorFormat = m_ColorFormat, enableRandomWrite = true, name = "BloomMipUp" });

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

        TextureHandle DoCopyAlpha(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source)
        {
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
                        DoCopyAlpha(data.parameters, data.source, data.outputAlpha, ctx.cmd);
                    });

                    return passData.outputAlpha;
                }
            }

            return renderGraph.defaultResources.whiteTextureXR;
        }

        TextureHandle StopNaNsPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source)
        {
            // Optional NaN killer before post-processing kicks in
            bool stopNaNs = hdCamera.stopNaNs && m_StopNaNFS;

#if UNITY_EDITOR
            bool isSceneView = hdCamera.camera.cameraType == CameraType.SceneView;
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
                        DoStopNaNs(data.parameters, ctx.cmd, data.source, data.destination);
                    });

                    return passData.destination;
                }
            }

            return source;
        }

        TextureHandle DynamicExposurePass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source)
        {
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
                                DoHistogramBasedExposure(data.parameters, ctx.cmd, data.source,
                                                                                   data.prevExposure,
                                                                                   data.nextExposure,
                                                                                   data.exposureDebugData);
                            });
                    }
                    else
                    {
                        passData.tmpTarget1024 = builder.CreateTransientTexture(new TextureDesc(1024, 1024, false, false)
                        { colorFormat = GraphicsFormat.R16G16_SFloat, enableRandomWrite = true, name = "Average Luminance Temp 1024" });
                        passData.tmpTarget32 = builder.CreateTransientTexture(new TextureDesc(32, 32, false, false)
                        { colorFormat = GraphicsFormat.R16G16_SFloat, enableRandomWrite = true, name = "Average Luminance Temp 32" });

                        builder.SetRenderFunc(
                            (DynamicExposureData data, RenderGraphContext ctx) =>
                            {
                                DoDynamicExposure(data.parameters, ctx.cmd, data.source,
                                                                            data.prevExposure,
                                                                            data.nextExposure,
                                                                            data.tmpTarget1024,
                                                                            data.tmpTarget32);
                            });
                    }
                }

                if (hdCamera.resetPostProcessingHistory)
                {
                    using (var builder = renderGraph.AddRenderPass<ApplyExposureData>("Apply Exposure", out var passData, ProfilingSampler.Get(HDProfileId.ApplyExposure)))
                    {
                        passData.source = builder.ReadTexture(source);
                        passData.parameters = PrepareApplyExposureParameters(hdCamera);
                        passData.prevExposure = builder.ReadTexture(renderGraph.ImportTexture(GetPreviousExposureTexture(hdCamera)));

                        TextureHandle dest = GetPostprocessOutputHandle(renderGraph, "Apply Exposure Destination");
                        passData.destination = builder.WriteTexture(dest);

                        builder.SetRenderFunc(
                        (ApplyExposureData data, RenderGraphContext ctx) =>
                        {
                            ApplyExposure(data.parameters, ctx.cmd, data.source, data.destination, data.prevExposure);
                        });

                        source = passData.destination;
                    }
                }
            }

            return source;
        }

        TextureHandle DoTemporalAntialiasing(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthBuffer, TextureHandle motionVectors, TextureHandle depthBufferMipChain, TextureHandle source)
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
                if (passData.parameters.resetPostProcessingHistory)
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
                    DoTemporalAntialiasing(data.parameters, ctx.cmd, data.source,
                                                                     data.destination,
                                                                     data.motionVecTexture,
                                                                     data.depthBuffer,
                                                                     data.depthMipChain,
                                                                     data.prevHistory,
                                                                     data.nextHistory,
                                                                     data.prevMVLen,
                                                                     data.nextMVLen);
                });

                source = passData.destination;
            }

            return source;
        }

        TextureHandle SMAAPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthBuffer, TextureHandle source)
        {
            using (var builder = renderGraph.AddRenderPass<SMAAData>("Subpixel Morphological Anti-Aliasing", out var passData, ProfilingSampler.Get(HDProfileId.SMAA)))
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
                    DoSMAA(data.parameters, ctx.cmd, data.source,
                                                     data.smaaEdgeTex,
                                                     data.smaaBlendTex,
                                                     data.destination,
                                                     data.depthBuffer);
                });

                source = passData.destination;
            }

            return source;
        }

        TextureHandle DepthOfFieldPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthBuffer, TextureHandle motionVectors, TextureHandle depthBufferMipChain, TextureHandle source)
        {
            bool postDoFTAAEnabled = false;
            bool isSceneView = hdCamera.camera.cameraType == CameraType.SceneView;
            bool taaEnabled = hdCamera.antialiasing == HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing;

            // If Path tracing is enabled, then DoF is computed in the path tracer by sampling the lens aperure (when using the physical camera mode)
            bool isDoFPathTraced = (hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) &&
                 hdCamera.volumeStack.GetComponent<PathTracing>().enable.value &&
                 hdCamera.camera.cameraType != CameraType.Preview &&
                 m_DepthOfField.focusMode == DepthOfFieldMode.UsePhysicalCamera);

            // Depth of Field is done right after TAA as it's easier to just re-project the CoC
            // map rather than having to deal with all the implications of doing it before TAA
            if (m_DepthOfField.IsActive() && !isSceneView && m_DepthOfFieldFS && !isDoFPathTraced)
            {
                // If we switch DoF modes and the old one was not using TAA, make sure we invalidate the history
                // Note: for Rendergraph the m_IsDoFHisotoryValid perhaps should be moved to the "pass data" struct
                if (taaEnabled && hdCamera.dofHistoryIsValid != m_DepthOfField.physicallyBased)
                {
                    hdCamera.resetPostProcessingHistory = true;
                }

                var dofParameters = PrepareDoFParameters(hdCamera);

                bool useHistoryMips = m_DepthOfField.physicallyBased;
                GrabCoCHistory(hdCamera, out var prevCoC, out var nextCoC, useMips: useHistoryMips);
                var prevCoCHandle = renderGraph.ImportTexture(prevCoC);
                var nextCoCHandle = renderGraph.ImportTexture(nextCoC);

                using (var builder = renderGraph.AddRenderPass<DepthofFieldData>("Depth of Field", out var passData, ProfilingSampler.Get(HDProfileId.DepthOfField)))
                {
                    passData.source = builder.ReadTexture(source);
                    passData.depthBuffer = builder.ReadTexture(depthBuffer);
                    passData.parameters = dofParameters;
                    passData.prevCoC = builder.ReadTexture(prevCoCHandle);
                    passData.nextCoC = builder.ReadWriteTexture(nextCoCHandle);

                    float scale = 1f / (float)passData.parameters.resolution;
                    var screenScale = new Vector2(scale, scale);

                    TextureHandle dest = GetPostprocessOutputHandle(renderGraph, "DoF Destination");
                    passData.destination = builder.WriteTexture(dest);
                    passData.motionVecTexture = builder.ReadTexture(motionVectors);
                    passData.taaEnabled = taaEnabled;

                    if (!m_DepthOfField.physicallyBased)
                    {
                        if (passData.parameters.nearLayerActive)
                        {
                            passData.pingNearRGB = builder.CreateTransientTexture(new TextureDesc(screenScale, true, true)
                            { colorFormat = m_ColorFormat, enableRandomWrite = true, name = "Ping Near RGB" });

                            passData.pongNearRGB = builder.CreateTransientTexture(new TextureDesc(screenScale, true, true)
                            { colorFormat = m_ColorFormat, enableRandomWrite = true, name = "Pong Near RGB" });

                            passData.nearCoC = builder.CreateTransientTexture(new TextureDesc(screenScale, true, true)
                            { colorFormat = k_CoCFormat, enableRandomWrite = true, name = "Near CoC" });

                            passData.nearAlpha = builder.CreateTransientTexture(new TextureDesc(screenScale, true, true)
                            { colorFormat = k_CoCFormat, enableRandomWrite = true, name = "Near Alpha" });

                            passData.dilatedNearCoC = builder.CreateTransientTexture(new TextureDesc(screenScale, true, true)
                            { colorFormat = k_CoCFormat, enableRandomWrite = true, name = "Dilated Near CoC" });

                        }
                        else
                        {
                            passData.pingNearRGB = TextureHandle.nullHandle;
                            passData.pongNearRGB = TextureHandle.nullHandle;
                            passData.nearCoC = TextureHandle.nullHandle;
                            passData.nearAlpha = TextureHandle.nullHandle;
                            passData.dilatedNearCoC = TextureHandle.nullHandle;
                        }

                        if (passData.parameters.farLayerActive)
                        {
                            passData.pingFarRGB = builder.CreateTransientTexture(new TextureDesc(screenScale, true, true)
                            { colorFormat = m_ColorFormat, useMipMap = true, enableRandomWrite = true, name = "Ping Far RGB" });

                            passData.pongFarRGB = builder.CreateTransientTexture(new TextureDesc(screenScale, true, true)
                            { colorFormat = m_ColorFormat, enableRandomWrite = true, name = "Pong Far RGB" });

                            passData.farCoC = builder.CreateTransientTexture(new TextureDesc(screenScale, true, true)
                            { colorFormat = k_CoCFormat, useMipMap = true, enableRandomWrite = true, name = "Far CoC" });
                        }
                        else
                        {
                            passData.pingFarRGB = TextureHandle.nullHandle;
                            passData.pongFarRGB = TextureHandle.nullHandle;
                            passData.farCoC = TextureHandle.nullHandle;
                        }

                        passData.fullresCoC = builder.ReadWriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                            { colorFormat = k_CoCFormat, enableRandomWrite = true, name = "Full res CoC" }));

                        GetDoFResolutionScale(passData.parameters, out float unused, out float resolutionScale);
                        float actualNearMaxBlur = passData.parameters.nearMaxBlur * resolutionScale;
                        int passCount = Mathf.CeilToInt((actualNearMaxBlur + 2f) / 4f);

                        passData.dilationPingPongRT = TextureHandle.nullHandle;
                        if (passCount > 1)
                        {
                            passData.dilationPingPongRT = builder.CreateTransientTexture(new TextureDesc(screenScale, true, true)
                            { colorFormat = k_CoCFormat, enableRandomWrite = true, name = "Dilation ping pong CoC" });
                        }

                        var mipScale = scale;
                        for (int i = 0; i < 4; ++i)
                        {
                            mipScale *= 0.5f;
                            var size = new Vector2Int(Mathf.RoundToInt(hdCamera.actualWidth * mipScale), Mathf.RoundToInt(hdCamera.actualHeight * mipScale));

                            passData.mips[i] = builder.CreateTransientTexture(new TextureDesc(new Vector2(mipScale, mipScale), true, true)
                            {
                                colorFormat = m_ColorFormat,
                                enableRandomWrite = true,
                                name = "CoC Mip"
                            });
                        }

                        passData.bokehNearKernel = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(dofParameters.nearSampleCount * dofParameters.nearSampleCount, sizeof(uint)) { name = "Bokeh Near Kernel" });
                        passData.bokehFarKernel = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(dofParameters.farSampleCount * dofParameters.farSampleCount, sizeof(uint)) { name = "Bokeh Far Kernel" });
                        passData.bokehIndirectCmd = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(3 * 2, sizeof(uint), ComputeBufferType.IndirectArguments) { name = "Bokeh Indirect Cmd" });
                        passData.nearBokehTileList = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(dofParameters.threadGroup8.x * dofParameters.threadGroup8.y, sizeof(uint), ComputeBufferType.Append) { name = "Bokeh Near Tile List" });
                        passData.farBokehTileList = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(dofParameters.threadGroup8.x * dofParameters.threadGroup8.y, sizeof(uint), ComputeBufferType.Append) { name = "Bokeh Far Tile List" });

                        builder.SetRenderFunc(
                        (DepthofFieldData data, RenderGraphContext ctx) =>
                        {
                            var mipsHandles = ctx.renderGraphPool.GetTempArray<RTHandle>(4);

                            for (int i = 0; i < 4; ++i)
                            {
                                mipsHandles[i] = data.mips[i];
                            }

                            ((ComputeBuffer)data.nearBokehTileList).SetCounterValue(0u);
                            ((ComputeBuffer)data.farBokehTileList).SetCounterValue(0u);

                            DoDepthOfField(data.parameters, ctx.cmd, data.source, data.destination, data.depthBuffer, data.pingNearRGB, data.pongNearRGB, data.nearCoC, data.nearAlpha,
                                           data.dilatedNearCoC, data.pingFarRGB, data.pongFarRGB, data.farCoC, data.fullresCoC, mipsHandles, data.dilationPingPongRT, data.prevCoC, data.nextCoC, data.motionVecTexture,
                                           data.bokehNearKernel, data.bokehFarKernel, data.bokehIndirectCmd, data.nearBokehTileList, data.farBokehTileList, data.taaEnabled);
                        });

                        source = passData.destination;


                        m_HDInstance.PushFullScreenDebugTexture(renderGraph, passData.fullresCoC, FullScreenDebugMode.DepthOfFieldCoc);
                    }
                    else
                    {
                        passData.fullresCoC = builder.ReadWriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                            { colorFormat = k_CoCFormat, enableRandomWrite = true, useMipMap = false, name = "Full res CoC" }));

                        passData.pingFarRGB = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                        { colorFormat = m_ColorFormat, useMipMap = true, enableRandomWrite = true, name = "DoF Source Pyramid" });

                        float scaleFactor = 1.0f / passData.parameters.minMaxCoCTileSize;
                        passData.pingNearRGB = builder.CreateTransientTexture(new TextureDesc(Vector2.one * scaleFactor, true, true)
                            { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, useMipMap = false, enableRandomWrite = true, name = "CoC Min Max Tiles" });

                        passData.pongNearRGB = builder.CreateTransientTexture(new TextureDesc(Vector2.one * scaleFactor, true, true)
                            { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, useMipMap = false, enableRandomWrite = true, name = "CoC Min Max Tiles" });


                        builder.SetRenderFunc(
                            (DepthofFieldData data, RenderGraphContext ctx) =>
                            {
                                DoPhysicallyBasedDepthOfField(data.parameters, ctx.cmd, data.source, data.destination, data.fullresCoC, data.prevCoC, data.nextCoC, data.motionVecTexture, data.pingFarRGB, data.depthBuffer, data.pingNearRGB, data.pongNearRGB, data.taaEnabled);
                            });

                        source = passData.destination;
                        m_HDInstance.PushFullScreenDebugTexture(renderGraph, passData.fullresCoC, FullScreenDebugMode.DepthOfFieldCoc);
                    }
                }
            }

            // When physically based DoF is enabled, TAA runs two times, first to stabilize the color buffer before DoF and then after DoF to accumulate more aperture samples
            if (taaEnabled && m_DepthOfField.physicallyBased)
            {
                bool postDof = true;

                using (var builder = renderGraph.AddRenderPass<TemporalAntiAliasingData>("Temporal Anti-Aliasing", out var passData, ProfilingSampler.Get(HDProfileId.TemporalAntialiasing)))
                {
                    GrabTemporalAntialiasingHistoryTextures(hdCamera, out var prevHistory, out var nextHistory, postDof);

                    passData.source = builder.ReadTexture(source);
                    passData.parameters = PrepareTAAParameters(hdCamera, postDof);
                    passData.depthBuffer = builder.ReadTexture(depthBuffer);
                    passData.motionVecTexture = builder.ReadTexture(motionVectors);
                    passData.depthMipChain = builder.ReadTexture(depthBufferMipChain);
                    passData.prevHistory = builder.ReadTexture(renderGraph.ImportTexture(prevHistory));
                    if (passData.parameters.resetPostProcessingHistory)
                    {
                        passData.prevHistory = builder.WriteTexture(passData.prevHistory);
                    }
                    passData.nextHistory = builder.WriteTexture(renderGraph.ImportTexture(nextHistory));

                    // Note: In case we run TAA for a second time (post-dof), we can use the same velocity history (and not write the output)
                    GrabVelocityMagnitudeHistoryTextures(hdCamera, out var prevMVLen, out var nextMVLen);
                    passData.prevMVLen = builder.ReadTexture(renderGraph.ImportTexture(prevMVLen));
                    passData.nextMVLen = TextureHandle.nullHandle;

                    TextureHandle dest = GetPostprocessOutputHandle(renderGraph, "Post-DoF TAA Destination");
                    passData.destination = builder.WriteTexture(dest);

                    builder.SetRenderFunc(
                        (TemporalAntiAliasingData data, RenderGraphContext ctx) =>
                        {
                            DoTemporalAntialiasing(data.parameters, ctx.cmd, data.source,
                                data.destination,
                                data.motionVecTexture,
                                data.depthBuffer,
                                data.depthMipChain,
                                data.prevHistory,
                                data.nextHistory,
                                data.prevMVLen,
                                data.nextMVLen);
                        });

                    source = passData.destination;
                }

                hdCamera.dofHistoryIsValid = true;
                postDoFTAAEnabled = true;
                
            }
            else
            {
                // Temporary hack to make post-dof TAA work with rendergraph (still the first frame flashes black). We need a better solution.
                hdCamera.dofHistoryIsValid = false;
            }

            if (!postDoFTAAEnabled)
            {
                ReleasePostDoFTAAHistoryTextures(hdCamera);
            }

            return source;
        }

        TextureHandle MotionBlurPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthTexture, TextureHandle motionVectors, TextureHandle source)
        {
            if (m_MotionBlur.IsActive() && m_AnimatedMaterialsEnabled && !hdCamera.resetPostProcessingHistory && m_MotionBlurFS)
            {
                using (var builder = renderGraph.AddRenderPass<MotionBlurData>("Motion Blur", out var passData, ProfilingSampler.Get(HDProfileId.MotionBlur)))
                {
                    passData.source = builder.ReadTexture(source);
                    passData.parameters = PrepareMotionBlurParameters(hdCamera);

                    passData.motionVecTexture = builder.ReadTexture(motionVectors);
                    passData.depthBuffer = builder.ReadTexture(depthTexture);

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
                    passData.destination = builder.WriteTexture(dest);

                    builder.SetRenderFunc(
                    (MotionBlurData data, RenderGraphContext ctx) =>
                    {
                        DoMotionBlur(data.parameters, ctx.cmd, data.source,
                                                               data.destination,
                                                               data.depthBuffer,
                                                               data.motionVecTexture,
                                                               data.preppedMotionVec,
                                                               data.minMaxTileVel,
                                                               data.maxTileNeigbourhood,
                                                               data.tileToScatterMax,
                                                               data.tileToScatterMin);
                    });

                    source = passData.destination;
                }
            }

            return source;
        }

        TextureHandle PaniniProjectionPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source)
        {
            bool isSceneView = hdCamera.camera.cameraType == CameraType.SceneView;
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
                        DoPaniniProjection(data.parameters, ctx.cmd, data.source, data.destination);
                    });

                    source = passData.destination;
                }
            }

            return source;
        }

        TextureHandle BloomPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source)
        {
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
                        var bloomMipUp = ctx.renderGraphPool.GetTempArray<RTHandle>(data.parameters.bloomMipCount);

                        for (int i = 0; i < data.parameters.bloomMipCount; ++i)
                        {
                            bloomMipDown[i] = data.mipsDown[i];
                            bloomMipUp[i] = data.mipsUp[i];
                        }

                        DoBloom(data.parameters, ctx.cmd, data.source, bloomMipDown, bloomMipUp);
                    });

                    bloomTexture = passData.mipsUp[0];
                }
            }

            return bloomTexture;
        }

        TextureHandle ColorGradingPass(RenderGraph renderGraph, HDCamera hdCamera)
        {
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
                    DoColorGrading(data.parameters, data.logLut, ctx.cmd);
                });
            }

            return logLutOutput;
        }

        TextureHandle UberPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle logLut, TextureHandle bloomTexture, TextureHandle source)
        {
            bool isSceneView = hdCamera.camera.cameraType == CameraType.SceneView;
            using (var builder = renderGraph.AddRenderPass<UberPostPassData>("Uber Post", out var passData, ProfilingSampler.Get(HDProfileId.UberPost)))
            {
                TextureHandle dest = GetPostprocessOutputHandle(renderGraph, "Uber Post Destination");

                passData.parameters = PrepareUberPostParameters(hdCamera, isSceneView);
                passData.source = builder.ReadTexture(source);
                passData.bloomTexture = builder.ReadTexture(bloomTexture);
                passData.logLut = builder.ReadTexture(logLut);
                passData.destination = builder.WriteTexture(dest);

                builder.SetRenderFunc(
                (UberPostPassData data, RenderGraphContext ctx) =>
                {
                    DoUberPostProcess(data.parameters,
                                          data.source,
                                        data.destination,
                                        data.logLut,
                                        data.bloomTexture,
                                        ctx.cmd);
                });

                source = passData.destination;
            }

            return source;
        }

        TextureHandle FXAAPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source)
        {
            if (DynamicResolutionHandler.instance.DynamicResolutionEnabled() &&     // Dynamic resolution is on.
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
                        DoFXAA(data.parameters, ctx.cmd, data.source, data.destination);
                    });

                    source = passData.destination;
                }
            }

            return source;
        }

        TextureHandle ContrastAdaptiveSharpeningPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source)
        {
            var dynResHandler = DynamicResolutionHandler.instance;

            if (dynResHandler.DynamicResolutionEnabled() &&
                dynResHandler.filter == DynamicResUpscaleFilter.ContrastAdaptiveSharpen)
            {
                using (var builder = renderGraph.AddRenderPass<CASData>("Contrast Adaptive Sharpen", out var passData, ProfilingSampler.Get(HDProfileId.ContrastAdaptiveSharpen)))
                {
                    passData.source = builder.ReadTexture(source);
                    passData.parameters = PrepareContrastAdaptiveSharpeningParameters(hdCamera);
                    TextureHandle dest = GetPostprocessUpsampledOutputHandle(renderGraph, "Contrast Adaptive Sharpen Destination");
                    passData.destination = builder.WriteTexture(dest);

                    passData.casParametersBuffer = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(2, sizeof(uint) * 4) { name = "Cas Parameters" });

                    builder.SetRenderFunc(
                    (CASData data, RenderGraphContext ctx) =>
                    {
                        DoContrastAdaptiveSharpening(data.parameters, ctx.cmd, data.source, data.destination, data.casParametersBuffer);
                    });

                    source = passData.destination;
                }
            }
            return source;
        }

        void FinalPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle afterPostProcessTexture, TextureHandle alphaTexture, TextureHandle finalRT, TextureHandle source, BlueNoise blueNoise, bool flipY)
        {
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
                    DoFinalPass(data.parameters, data.source, data.afterPostProcessTexture, data.destination, data.alphaTexture, ctx.cmd);
                });
            }
        }

        internal void DoUserAfterOpaqueAndSky(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVectors)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.CustomPostProcess))
                return;

            using (new RenderGraphProfilingScope(renderGraph, ProfilingSampler.Get(HDProfileId.CustomPostProcessAfterOpaqueAndSky)))
            {
                TextureHandle source = colorBuffer;
                bool needBlitToColorBuffer = DoCustomPostProcess(renderGraph, hdCamera, ref source, depthBuffer, normalBuffer, motionVectors, HDRenderPipeline.defaultAsset.beforeTransparentCustomPostProcesses);

                if (needBlitToColorBuffer)
                {
                    HDRenderPipeline.BlitCameraTexture(renderGraph, source, colorBuffer);
                }
            }
        }

        bool DoCustomPostProcess(RenderGraph renderGraph, HDCamera hdCamera, ref TextureHandle source, TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVectors, List<string> postProcessList)
        {
            bool customPostProcessExecuted = false;
            foreach (var typeString in postProcessList)
            {
                var customPostProcessComponentType = Type.GetType(typeString);
                if (customPostProcessComponentType == null)
                    continue;

                var stack = hdCamera.volumeStack;

                if (stack.GetComponent(customPostProcessComponentType) is CustomPostProcessVolumeComponent customPP)
                {
                    customPP.SetupIfNeeded();

                    if (customPP is IPostProcessComponent pp && pp.IsActive())
                    {
                        if (hdCamera.camera.cameraType != CameraType.SceneView || customPP.visibleInSceneView)
                        {
                            using (var builder = renderGraph.AddRenderPass<CustomPostProcessData>(customPP.name, out var passData))
                            {
                                // TODO RENDERGRAPH
                                // These buffer are always bound in custom post process for now.
                                // We don't have the information that they are being used or not.
                                // Until we can upgrade CustomPP to be full render graph, we'll always read and bind them globally.
                                passData.depthBuffer = builder.ReadTexture(depthBuffer);
                                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                                passData.motionVecTexture = builder.ReadTexture(motionVectors);

                                passData.source = builder.ReadTexture(source);
                                passData.destination = builder.UseColorBuffer(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                                { colorFormat = m_ColorFormat, enableRandomWrite = true, name = "CustomPostProcesDestination" }), 0);
                                passData.hdCamera = hdCamera;
                                passData.customPostProcess = customPP;
                                builder.SetRenderFunc(
                                (CustomPostProcessData data, RenderGraphContext ctx) =>
                                {
                                    // Temporary: see comment above
                                    ctx.cmd.SetGlobalTexture(HDShaderIDs._CameraDepthTexture, data.depthBuffer);
                                    ctx.cmd.SetGlobalTexture(HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                                    ctx.cmd.SetGlobalTexture(HDShaderIDs._CameraMotionVectorsTexture, data.motionVecTexture);

                                    data.customPostProcess.Render(ctx.cmd, data.hdCamera, data.source, data.destination);
                                });

                                customPostProcessExecuted = true;
                                source = passData.destination;
                            }
                        }
                    }
                }
            }

            return customPostProcessExecuted;
        }

        TextureHandle CustomPostProcessPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source, TextureHandle depthBuffer, TextureHandle normalBuffer, TextureHandle motionVectors, List<string> postProcessList, HDProfileId profileId)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.CustomPostProcess))
                return source;

            using (new RenderGraphProfilingScope(renderGraph, ProfilingSampler.Get(profileId)))
            {
                DoCustomPostProcess(renderGraph, hdCamera, ref source, depthBuffer, normalBuffer, motionVectors, postProcessList);
            }

            return source;
        }

        public void Render(RenderGraph renderGraph,
                            HDCamera hdCamera,
                            BlueNoise blueNoise,
                            TextureHandle colorBuffer,
                            TextureHandle afterPostProcessTexture,
                            TextureHandle depthBuffer,
                            TextureHandle depthBufferMipChain,
                            TextureHandle normalBuffer,
                            TextureHandle motionVectors,
                            TextureHandle finalRT,
                            bool flipY)
        {
            renderGraph.BeginProfilingSampler(ProfilingSampler.Get(HDProfileId.PostProcessing));

            var source = colorBuffer;
            TextureHandle alphaTexture = DoCopyAlpha(renderGraph, hdCamera, source);

            // Note: whether a pass is really executed or not is generally inside the Do* functions.
            // with few exceptions.

            if (m_PostProcessEnabled)
            {
                source = StopNaNsPass(renderGraph, hdCamera, source);

                source = DynamicExposurePass(renderGraph, hdCamera, source);

                source = CustomPostProcessPass(renderGraph, hdCamera, source, depthBuffer, normalBuffer, motionVectors, HDRenderPipeline.defaultAsset.beforeTAACustomPostProcesses, HDProfileId.CustomPostProcessBeforeTAA);

                // Temporal anti-aliasing goes first
                if (m_AntialiasingFS)
                {
                    if (hdCamera.antialiasing == HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing)
                    {
                        source = DoTemporalAntialiasing(renderGraph, hdCamera, depthBuffer, motionVectors, depthBufferMipChain, source);
                    }
                    else if (hdCamera.antialiasing == HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing)
                    {
                        source = SMAAPass(renderGraph, hdCamera, depthBuffer, source);
                    }
                }

                source = CustomPostProcessPass(renderGraph, hdCamera, source, depthBuffer, normalBuffer, motionVectors, HDRenderPipeline.defaultAsset.beforePostProcessCustomPostProcesses, HDProfileId.CustomPostProcessBeforePP);

                source = DepthOfFieldPass(renderGraph, hdCamera, depthBuffer, motionVectors, depthBufferMipChain, source);

                // Motion blur after depth of field for aesthetic reasons (better to see motion
                // blurred bokeh rather than out of focus motion blur)
                source = MotionBlurPass(renderGraph, hdCamera, depthBuffer, motionVectors, source);

                // Panini projection is done as a fullscreen pass after all depth-based effects are
                // done and before bloom kicks in
                // This is one effect that would benefit from an overscan mode or supersampling in
                // HDRP to reduce the amount of resolution lost at the center of the screen
                source = PaniniProjectionPass(renderGraph, hdCamera, source);

                TextureHandle bloomTexture = BloomPass(renderGraph, hdCamera, source);
                TextureHandle logLutOutput = ColorGradingPass(renderGraph, hdCamera);
                source = UberPass(renderGraph, hdCamera, logLutOutput, bloomTexture, source);
                m_HDInstance.PushFullScreenDebugTexture(renderGraph, source, FullScreenDebugMode.ColorLog);

                source = CustomPostProcessPass(renderGraph, hdCamera, source, depthBuffer, normalBuffer, motionVectors, HDRenderPipeline.defaultAsset.afterPostProcessCustomPostProcesses, HDProfileId.CustomPostProcessAfterPP);

                source = FXAAPass(renderGraph, hdCamera, source);

                hdCamera.resetPostProcessingHistory = false;
            }

            // Contrast Adaptive Sharpen Upscaling
            source = ContrastAdaptiveSharpeningPass(renderGraph, hdCamera, source);

            FinalPass(renderGraph, hdCamera, afterPostProcessTexture, alphaTexture, finalRT, source, blueNoise, flipY);

            renderGraph.EndProfilingSampler(ProfilingSampler.Get(HDProfileId.PostProcessing));
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
