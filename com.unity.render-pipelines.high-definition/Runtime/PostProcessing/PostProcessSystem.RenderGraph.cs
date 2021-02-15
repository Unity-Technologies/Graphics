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
            public ComputeShader builderCS;
            public int builderKernel;

            public int lutSize;

            public Vector4 colorFilter;
            public Vector3 lmsColorBalance;
            public Vector4 hueSatCon;
            public Vector4 channelMixerR;
            public Vector4 channelMixerG;
            public Vector4 channelMixerB;
            public Vector4 shadows;
            public Vector4 midtones;
            public Vector4 highlights;
            public Vector4 shadowsHighlightsLimits;
            public Vector4 lift;
            public Vector4 gamma;
            public Vector4 gain;
            public Vector4 splitShadows;
            public Vector4 splitHighlights;

            public ColorCurves curves;
            public HableCurve hableCurve;

            public Vector4 miscParams;

            public Texture externalLuT;
            public float lutContribution;

            public TonemappingMode tonemappingMode;

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
            public ComputeShader nanKillerCS;
            public int nanKillerKernel;
            public int width;
            public int height;
            public int viewCount;

            public TextureHandle source;
            public TextureHandle destination;
        }

        class DynamicExposureData
        {
            public ComputeShader exposureCS;
            public ComputeShader histogramExposureCS;
            public int exposurePreparationKernel;
            public int exposureReductionKernel;

            public Texture textureMeteringMask;
            public Texture exposureCurve;

            public HDCamera camera;

            public ComputeBuffer histogramBuffer;

            public ExposureMode exposureMode;
            public bool histogramUsesCurve;
            public bool histogramOutputDebugData;

            public int[] exposureVariants;
            public Vector4 exposureParams;
            public Vector4 exposureParams2;
            public Vector4 proceduralMaskParams;
            public Vector4 proceduralMaskParams2;
            public Vector4 histogramExposureParams;
            public Vector4 adaptationParams;

            public TextureHandle source;
            public TextureHandle prevExposure;
            public TextureHandle nextExposure;
            public TextureHandle exposureDebugData;
            public TextureHandle tmpTarget1024;
            public TextureHandle tmpTarget32;
        }

        class ApplyExposureData
        {
            public ComputeShader applyExposureCS;
            public int applyExposureKernel;
            public int width;
            public int height;
            public int viewCount;

            public TextureHandle source;
            public TextureHandle destination;
            public TextureHandle prevExposure;
        }

        class TemporalAntiAliasingData
        {
            public Material temporalAAMaterial;
            public MaterialPropertyBlock taaHistoryPropertyBlock;
            public MaterialPropertyBlock taaPropertyBlock;
            public bool resetPostProcessingHistory;

            public Vector4 previousScreenSize;
            public Vector4 taaParameters;
            public Vector4 taaFilterWeights;
            public bool motionVectorRejection;

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
            public Material smaaMaterial;
            public Texture smaaAreaTex;
            public Texture smaaSearchTex;
            public Vector4 smaaRTMetrics;

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
            public ComputeShader paniniProjectionCS;
            public int paniniProjectionKernel;
            public Vector4 paniniParams;
            public int width;
            public int height;
            public int viewCount;

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
            public HDCamera hdCamera;
            public CustomPostProcessVolumeComponent customPostProcess;
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
                    passData.nanKillerCS = m_Resources.shaders.nanKillerCS;
                    passData.nanKillerKernel = passData.nanKillerCS.FindKernel("KMain");
                    passData.width = hdCamera.actualWidth;
                    passData.height = hdCamera.actualHeight;
                    passData.viewCount = hdCamera.viewCount;
                    passData.nanKillerCS.shaderKeywords = null;
                    if (m_EnableAlpha)
                        passData.nanKillerCS.EnableKeyword("ENABLE_ALPHA");
                    passData.source = builder.ReadTexture(source);
                    passData.destination = builder.WriteTexture(GetPostprocessOutputHandle(renderGraph, "Stop NaNs Destination"));;

                    builder.SetRenderFunc(
                        (StopNaNPassData data, RenderGraphContext ctx) =>
                        {
                            ctx.cmd.SetComputeTextureParam(data.nanKillerCS, data.nanKillerKernel, HDShaderIDs._InputTexture, data.source);
                            ctx.cmd.SetComputeTextureParam(data.nanKillerCS, data.nanKillerKernel, HDShaderIDs._OutputTexture, data.destination);
                            ctx.cmd.DispatchCompute(data.nanKillerCS, data.nanKillerKernel, (data.width + 7) / 8, (data.height + 7) / 8, data.viewCount);
                        });

                    return passData.destination;
                }
            }

            return source;
        }

        void PrepareExposurePassData(RenderGraph renderGraph, RenderGraphBuilder builder, HDCamera hdCamera, TextureHandle source, DynamicExposureData passData)
        {
            passData.exposureCS = m_Resources.shaders.exposureCS;
            passData.histogramExposureCS = m_Resources.shaders.histogramExposureCS;
            passData.histogramExposureCS.shaderKeywords = null;

            passData.camera = hdCamera;

            // Setup variants
            var adaptationMode = m_Exposure.adaptationMode.value;

            if (!Application.isPlaying || hdCamera.resetPostProcessingHistory)
                adaptationMode = AdaptationMode.Fixed;

            passData.exposureVariants = m_ExposureVariants;
            passData.exposureVariants[0] = 1; // (int)exposureSettings.luminanceSource.value;
            passData.exposureVariants[1] = (int)m_Exposure.meteringMode.value;
            passData.exposureVariants[2] = (int)adaptationMode;
            passData.exposureVariants[3] = 0;

            bool useTextureMask = m_Exposure.meteringMode.value == MeteringMode.MaskWeighted && m_Exposure.weightTextureMask.value != null;
            passData.textureMeteringMask = useTextureMask ? m_Exposure.weightTextureMask.value : Texture2D.whiteTexture;

            ComputeProceduralMeteringParams(hdCamera, out passData.proceduralMaskParams, out passData.proceduralMaskParams2);

            bool isHistogramBased = m_Exposure.mode.value == ExposureMode.AutomaticHistogram;
            bool needsCurve = (isHistogramBased && m_Exposure.histogramUseCurveRemapping.value) || m_Exposure.mode.value == ExposureMode.CurveMapping;

            passData.histogramUsesCurve = m_Exposure.histogramUseCurveRemapping.value;
            passData.adaptationParams = new Vector4(m_Exposure.adaptationSpeedLightToDark.value, m_Exposure.adaptationSpeedDarkToLight.value, 0.0f, 0.0f);

            passData.exposureMode = m_Exposure.mode.value;

            float limitMax = m_Exposure.limitMax.value;
            float limitMin = m_Exposure.limitMin.value;

            float curveMin = 0.0f;
            float curveMax = 0.0f;
            if (needsCurve)
            {
                PrepareExposureCurveData(out curveMin, out curveMax);
                limitMin = curveMin;
                limitMax = curveMax;
            }

            passData.exposureParams = new Vector4(m_Exposure.compensation.value + m_DebugExposureCompensation, limitMin, limitMax, 0f);
            passData.exposureParams2 = new Vector4(curveMin, curveMax, ColorUtils.lensImperfectionExposureScale, ColorUtils.s_LightMeterCalibrationConstant);

            passData.exposureCurve = m_ExposureCurveTexture;

            if (isHistogramBased)
            {
                ValidateComputeBuffer(ref m_HistogramBuffer, k_HistogramBins, sizeof(uint));
                m_HistogramBuffer.SetData(m_EmptyHistogram);    // Clear the histogram

                Vector2 histogramFraction = m_Exposure.histogramPercentages.value / 100.0f;
                float evRange = limitMax - limitMin;
                float histScale = 1.0f / Mathf.Max(1e-5f, evRange);
                float histBias = -limitMin * histScale;
                passData.histogramExposureParams = new Vector4(histScale, histBias, histogramFraction.x, histogramFraction.y);

                passData.histogramBuffer = m_HistogramBuffer;
                passData.histogramOutputDebugData = m_HDInstance.m_CurrentDebugDisplaySettings.data.lightingDebugSettings.exposureDebugMode == ExposureDebugMode.HistogramView;
                if (passData.histogramOutputDebugData)
                {
                    passData.histogramExposureCS.EnableKeyword("OUTPUT_DEBUG_DATA");
                }

                passData.exposurePreparationKernel = passData.histogramExposureCS.FindKernel("KHistogramGen");
                passData.exposureReductionKernel = passData.histogramExposureCS.FindKernel("KHistogramReduce");
            }
            else
            {
                passData.exposurePreparationKernel = passData.exposureCS.FindKernel("KPrePass");
                passData.exposureReductionKernel = passData.exposureCS.FindKernel("KReduction");
            }

            GrabExposureRequiredTextures(hdCamera, out var prevExposure, out var nextExposure);

            passData.source = builder.ReadTexture(source);
            passData.prevExposure = builder.ReadTexture(renderGraph.ImportTexture(prevExposure));
            passData.nextExposure = builder.WriteTexture(renderGraph.ImportTexture(nextExposure));
        }

        void GrabExposureRequiredTextures(HDCamera camera, out RTHandle prevExposure, out RTHandle nextExposure)
        {
            GrabExposureHistoryTextures(camera, out prevExposure, out nextExposure);
            if (camera.resetPostProcessingHistory)
            {
                // For Dynamic Exposure, we need to undo the pre-exposure from the color buffer to calculate the correct one
                // When we reset history we must setup neutral value
                prevExposure = m_EmptyExposureTexture; // Use neutral texture
            }
        }

        static void DoDynamicExposure(DynamicExposureData data, CommandBuffer cmd)
        {
            var cs = data.exposureCS;
            int kernel;

            kernel = data.exposurePreparationKernel;
            cmd.SetComputeIntParams(cs, HDShaderIDs._Variants, data.exposureVariants);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PreviousExposureTexture, data.prevExposure);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._SourceTexture, data.source);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams2, data.exposureParams2);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureWeightMask, data.textureMeteringMask);

            cmd.SetComputeVectorParam(cs, HDShaderIDs._ProceduralMaskParams, data.proceduralMaskParams);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ProceduralMaskParams2, data.proceduralMaskParams2);

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, data.tmpTarget32);
            cmd.DispatchCompute(cs, kernel, 1024 / 8, 1024 / 8, 1);

            // Reduction: 1st pass (1024 -> 32)
            kernel = data.exposureReductionKernel;
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PreviousExposureTexture, data.prevExposure);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureCurveTexture, Texture2D.blackTexture);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, data.tmpTarget1024);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, data.tmpTarget32);
            cmd.DispatchCompute(cs, kernel, 32, 32, 1);

            cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams, data.exposureParams);

            // Reduction: 2nd pass (32 -> 1) + evaluate exposure
            if (data.exposureMode == ExposureMode.Automatic)
            {
                data.exposureVariants[3] = 1;
            }
            else if (data.exposureMode == ExposureMode.CurveMapping)
            {
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureCurveTexture, data.exposureCurve);
                data.exposureVariants[3] = 2;
            }

            cmd.SetComputeVectorParam(cs, HDShaderIDs._AdaptationParams, data.adaptationParams);
            cmd.SetComputeIntParams(cs, HDShaderIDs._Variants, data.exposureVariants);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PreviousExposureTexture, data.prevExposure);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, data.tmpTarget32);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, data.nextExposure);
            cmd.DispatchCompute(cs, kernel, 1, 1, 1);
        }

        static void DoHistogramBasedExposure(DynamicExposureData data, CommandBuffer cmd)
        {
            var cs = data.histogramExposureCS;
            int kernel;

            cmd.SetComputeVectorParam(cs, HDShaderIDs._ProceduralMaskParams, data.proceduralMaskParams);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ProceduralMaskParams2, data.proceduralMaskParams2);

            cmd.SetComputeVectorParam(cs, HDShaderIDs._HistogramExposureParams, data.histogramExposureParams);

            // Generate histogram.
            kernel = data.exposurePreparationKernel;
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PreviousExposureTexture, data.prevExposure);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._SourceTexture, data.source);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureWeightMask, data.textureMeteringMask);

            cmd.SetComputeIntParams(cs, HDShaderIDs._Variants, data.exposureVariants);

            cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._HistogramBuffer, data.histogramBuffer);

            int threadGroupSizeX = 16;
            int threadGroupSizeY = 8;
            int dispatchSizeX = HDUtils.DivRoundUp(data.camera.actualWidth / 2, threadGroupSizeX);
            int dispatchSizeY = HDUtils.DivRoundUp(data.camera.actualHeight / 2, threadGroupSizeY);
            cmd.DispatchCompute(cs, kernel, dispatchSizeX, dispatchSizeY, 1);

            // Now read the histogram
            kernel = data.exposureReductionKernel;
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams, data.exposureParams);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._ExposureParams2, data.exposureParams2);
            cmd.SetComputeVectorParam(cs, HDShaderIDs._AdaptationParams, data.adaptationParams);
            cmd.SetComputeBufferParam(cs, kernel, HDShaderIDs._HistogramBuffer, data.histogramBuffer);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._PreviousExposureTexture, data.prevExposure);
            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, data.nextExposure);

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureCurveTexture, data.exposureCurve);
            data.exposureVariants[3] = 0;
            if (data.histogramUsesCurve)
            {
                data.exposureVariants[3] = 2;
            }
            cmd.SetComputeIntParams(cs, HDShaderIDs._Variants, data.exposureVariants);

            if (data.histogramOutputDebugData)
            {
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._ExposureDebugTexture, data.exposureDebugData);
            }

            cmd.DispatchCompute(cs, kernel, 1, 1, 1);
        }

        TextureHandle DynamicExposurePass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source)
        {
            // Dynamic exposure - will be applied in the next frame
            // Not considered as a post-process so it's not affected by its enabled state
            if (!IsExposureFixed(hdCamera) && m_ExposureControlFS)
            {
                using (var builder = renderGraph.AddRenderPass<DynamicExposureData>("Dynamic Exposure", out var passData, ProfilingSampler.Get(HDProfileId.DynamicExposure)))
                {
                    PrepareExposurePassData(renderGraph, builder, hdCamera, source, passData);

                    if (m_Exposure.mode.value == ExposureMode.AutomaticHistogram)
                    {
                        passData.exposureDebugData = builder.WriteTexture(renderGraph.ImportTexture(m_DebugExposureData));
                        builder.SetRenderFunc(
                            (DynamicExposureData data, RenderGraphContext ctx) =>
                            {
                                DoHistogramBasedExposure(data, ctx.cmd);
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
                                DoDynamicExposure(data, ctx.cmd);
                            });
                    }
                }

                if (hdCamera.resetPostProcessingHistory)
                {
                    using (var builder = renderGraph.AddRenderPass<ApplyExposureData>("Apply Exposure", out var passData, ProfilingSampler.Get(HDProfileId.ApplyExposure)))
                    {
                        passData.applyExposureCS = m_Resources.shaders.applyExposureCS;
                        passData.applyExposureKernel = passData.applyExposureCS.FindKernel("KMain");
                        passData.width = hdCamera.actualWidth;
                        passData.height = hdCamera.actualHeight;
                        passData.viewCount = hdCamera.viewCount;
                        passData.source = builder.ReadTexture(source);
                        passData.prevExposure = builder.ReadTexture(renderGraph.ImportTexture(GetPreviousExposureTexture(hdCamera)));

                        TextureHandle dest = GetPostprocessOutputHandle(renderGraph, "Apply Exposure Destination");
                        passData.destination = builder.WriteTexture(dest);;

                        builder.SetRenderFunc(
                            (ApplyExposureData data, RenderGraphContext ctx) =>
                            {
                                // Note: we use previous instead of current because the textures
                                // are swapped internally as the system expects the texture will be used
                                // on the next frame. So the actual "current" for this frame is in
                                // "previous".
                                ctx.cmd.SetComputeTextureParam(data.applyExposureCS, data.applyExposureKernel, HDShaderIDs._ExposureTexture, data.prevExposure);
                                ctx.cmd.SetComputeTextureParam(data.applyExposureCS, data.applyExposureKernel, HDShaderIDs._InputTexture, data.source);
                                ctx.cmd.SetComputeTextureParam(data.applyExposureCS, data.applyExposureKernel, HDShaderIDs._OutputTexture, data.destination);
                                ctx.cmd.DispatchCompute(data.applyExposureCS, data.applyExposureKernel, (data.width + 7) / 8, (data.height + 7) / 8, data.viewCount);
                            });

                        source = passData.destination;
                    }
                }
            }

            return source;
        }

        void PrepareTAAPassData(RenderGraph renderGraph, RenderGraphBuilder builder, TemporalAntiAliasingData passData, HDCamera camera,
            TextureHandle depthBuffer, TextureHandle motionVectors, TextureHandle depthBufferMipChain, TextureHandle sourceTexture, bool postDoF, string outputName)
        {
            passData.resetPostProcessingHistory = camera.resetPostProcessingHistory;

            float minAntiflicker = 0.0f;
            float maxAntiflicker = 3.5f;
            float motionRejectionMultiplier = Mathf.Lerp(0.0f, 250.0f, camera.taaMotionVectorRejection * camera.taaMotionVectorRejection * camera.taaMotionVectorRejection);

            // The anti flicker becomes much more aggressive on higher values
            float temporalContrastForMaxAntiFlicker = 0.7f - Mathf.Lerp(0.0f, 0.3f, Mathf.SmoothStep(0.5f, 1.0f, camera.taaAntiFlicker));

            passData.taaParameters = new Vector4(camera.taaHistorySharpening, postDoF ? maxAntiflicker : Mathf.Lerp(minAntiflicker, maxAntiflicker, camera.taaAntiFlicker), motionRejectionMultiplier, temporalContrastForMaxAntiFlicker);

            // Precompute weights used for the Blackman-Harris filter. TODO: Note that these are slightly wrong as they don't take into account the jitter size. This needs to be fixed at some point.
            float crossWeights = Mathf.Exp(-2.29f * 2);
            float plusWeights = Mathf.Exp(-2.29f);
            float centerWeight = 1;

            float totalWeight = centerWeight + (4 * plusWeights);
            if (camera.TAAQuality == HDAdditionalCameraData.TAAQualityLevel.High)
            {
                totalWeight += crossWeights * 4;
            }

            // Weights will be x: central, y: plus neighbours, z: cross neighbours, w: total
            passData.taaFilterWeights = new Vector4(centerWeight / totalWeight, plusWeights / totalWeight, crossWeights / totalWeight, totalWeight);

            passData.temporalAAMaterial = m_TemporalAAMaterial;
            passData.temporalAAMaterial.shaderKeywords = null;

            if (m_EnableAlpha)
            {
                passData.temporalAAMaterial.EnableKeyword("ENABLE_ALPHA");
            }

            if (camera.taaHistorySharpening == 0)
            {
                passData.temporalAAMaterial.EnableKeyword("FORCE_BILINEAR_HISTORY");
            }

            if (camera.taaHistorySharpening != 0 && camera.taaAntiRinging && camera.TAAQuality == HDAdditionalCameraData.TAAQualityLevel.High)
            {
                passData.temporalAAMaterial.EnableKeyword("ANTI_RINGING");
            }

            passData.motionVectorRejection = camera.taaMotionVectorRejection > 0;
            if (passData.motionVectorRejection)
            {
                passData.temporalAAMaterial.EnableKeyword("ENABLE_MV_REJECTION");
            }

            if (postDoF)
            {
                passData.temporalAAMaterial.EnableKeyword("POST_DOF");
            }
            else
            {
                switch (camera.TAAQuality)
                {
                    case HDAdditionalCameraData.TAAQualityLevel.Low:
                        passData.temporalAAMaterial.EnableKeyword("LOW_QUALITY");
                        break;
                    case HDAdditionalCameraData.TAAQualityLevel.Medium:
                        passData.temporalAAMaterial.EnableKeyword("MEDIUM_QUALITY");
                        break;
                    case HDAdditionalCameraData.TAAQualityLevel.High:
                        passData.temporalAAMaterial.EnableKeyword("HIGH_QUALITY");
                        break;
                    default:
                        passData.temporalAAMaterial.EnableKeyword("MEDIUM_QUALITY");
                        break;
                }
            }

            GrabTemporalAntialiasingHistoryTextures(camera, out var prevHistory, out var nextHistory, postDoF);

            passData.taaHistoryPropertyBlock = m_TAAHistoryBlitPropertyBlock;
            passData.taaPropertyBlock = m_TAAPropertyBlock;
            Vector2Int prevViewPort = camera.historyRTHandleProperties.previousViewportSize;
            passData.previousScreenSize = new Vector4(prevViewPort.x, prevViewPort.y, 1.0f / prevViewPort.x, 1.0f / prevViewPort.y);

            passData.source = builder.ReadTexture(sourceTexture);
            passData.depthBuffer = builder.ReadTexture(depthBuffer);
            passData.motionVecTexture = builder.ReadTexture(motionVectors);
            passData.depthMipChain = builder.ReadTexture(depthBufferMipChain);
            passData.prevHistory = builder.ReadTexture(renderGraph.ImportTexture(prevHistory));
            if (passData.resetPostProcessingHistory)
            {
                passData.prevHistory = builder.WriteTexture(passData.prevHistory);
            }
            passData.nextHistory = builder.WriteTexture(renderGraph.ImportTexture(nextHistory));
            if (!postDoF)
            {
                GrabVelocityMagnitudeHistoryTextures(camera, out var prevMVLen, out var nextMVLen);
                passData.prevMVLen = builder.ReadTexture(renderGraph.ImportTexture(prevMVLen));
                passData.nextMVLen = builder.WriteTexture(renderGraph.ImportTexture(nextMVLen));
            }
            else
            {
                passData.prevMVLen = TextureHandle.nullHandle;
                passData.nextMVLen = TextureHandle.nullHandle;
            }

            passData.destination = builder.WriteTexture(GetPostprocessOutputHandle(renderGraph, outputName));;

            TextureHandle dest = GetPostprocessOutputHandle(renderGraph, "Post-DoF TAA Destination");
            passData.destination = builder.WriteTexture(dest);;
        }

        TextureHandle DoTemporalAntialiasing(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthBuffer, TextureHandle motionVectors, TextureHandle depthBufferMipChain, TextureHandle sourceTexture, bool postDoF, string outputName)
        {
            using (var builder = renderGraph.AddRenderPass<TemporalAntiAliasingData>("Temporal Anti-Aliasing", out var passData, ProfilingSampler.Get(HDProfileId.TemporalAntialiasing)))
            {
                PrepareTAAPassData(renderGraph, builder, passData, hdCamera, depthBuffer, motionVectors, depthBufferMipChain, sourceTexture, postDoF, outputName);

                builder.SetRenderFunc(
                    (TemporalAntiAliasingData data, RenderGraphContext ctx) =>
                    {
                        RTHandle source = data.source;
                        RTHandle nextMVLenTexture = data.nextMVLen;
                        RTHandle prevMVLenTexture = data.prevMVLen;

                        if (data.resetPostProcessingHistory)
                        {
                            data.taaHistoryPropertyBlock.SetTexture(HDShaderIDs._BlitTexture, source);
                            var rtScaleSource = source.rtHandleProperties.rtHandleScale;
                            data.taaHistoryPropertyBlock.SetVector(HDShaderIDs._BlitScaleBias, new Vector4(rtScaleSource.x, rtScaleSource.y, 0.0f, 0.0f));
                            data.taaHistoryPropertyBlock.SetFloat(HDShaderIDs._BlitMipLevel, 0);
                            HDUtils.DrawFullScreen(ctx.cmd, HDUtils.GetBlitMaterial(source.rt.dimension), data.prevHistory, data.taaHistoryPropertyBlock, 0);
                            HDUtils.DrawFullScreen(ctx.cmd, HDUtils.GetBlitMaterial(source.rt.dimension), data.nextHistory, data.taaHistoryPropertyBlock, 0);
                        }

                        data.taaPropertyBlock.SetInt(HDShaderIDs._StencilMask, (int)StencilUsage.ExcludeFromTAA);
                        data.taaPropertyBlock.SetInt(HDShaderIDs._StencilRef, (int)StencilUsage.ExcludeFromTAA);
                        data.taaPropertyBlock.SetTexture(HDShaderIDs._CameraMotionVectorsTexture, data.motionVecTexture);
                        data.taaPropertyBlock.SetTexture(HDShaderIDs._InputTexture, source);
                        data.taaPropertyBlock.SetTexture(HDShaderIDs._InputHistoryTexture, data.prevHistory);
                        if (prevMVLenTexture != null && data.motionVectorRejection)
                        {
                            data.taaPropertyBlock.SetTexture(HDShaderIDs._InputVelocityMagnitudeHistory, prevMVLenTexture);
                        }

                        data.taaPropertyBlock.SetTexture(HDShaderIDs._DepthTexture, data.depthMipChain);

                        var taaHistorySize = data.previousScreenSize;

                        data.taaPropertyBlock.SetVector(HDShaderIDs._TaaPostParameters, data.taaParameters);
                        data.taaPropertyBlock.SetVector(HDShaderIDs._TaaHistorySize, taaHistorySize);
                        data.taaPropertyBlock.SetVector(HDShaderIDs._TaaFilterWeights, data.taaFilterWeights);

                        CoreUtils.SetRenderTarget(ctx.cmd, data.destination, data.depthBuffer);
                        ctx.cmd.SetRandomWriteTarget(1, data.nextHistory);
                        if (nextMVLenTexture != null && data.motionVectorRejection)
                        {
                            ctx.cmd.SetRandomWriteTarget(2, nextMVLenTexture);
                        }

                        ctx.cmd.DrawProcedural(Matrix4x4.identity, data.temporalAAMaterial, 0, MeshTopology.Triangles, 3, 1, data.taaPropertyBlock);
                        ctx.cmd.DrawProcedural(Matrix4x4.identity, data.temporalAAMaterial, 1, MeshTopology.Triangles, 3, 1, data.taaPropertyBlock);
                        ctx.cmd.ClearRandomWriteTargets();
                    });

                return passData.destination;
            }
        }

        TextureHandle SMAAPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthBuffer, TextureHandle source)
        {
            using (var builder = renderGraph.AddRenderPass<SMAAData>("Subpixel Morphological Anti-Aliasing", out var passData, ProfilingSampler.Get(HDProfileId.SMAA)))
            {
                passData.smaaMaterial = m_SMAAMaterial;
                passData.smaaAreaTex = m_Resources.textures.SMAAAreaTex;
                passData.smaaSearchTex = m_Resources.textures.SMAASearchTex;
                passData.smaaMaterial.shaderKeywords = null;
                passData.smaaRTMetrics = new Vector4(1.0f / hdCamera.actualWidth, 1.0f / hdCamera.actualHeight, hdCamera.actualWidth, hdCamera.actualHeight);

                switch (hdCamera.SMAAQuality)
                {
                    case HDAdditionalCameraData.SMAAQualityLevel.Low:
                        passData.smaaMaterial.EnableKeyword("SMAA_PRESET_LOW");
                        break;
                    case HDAdditionalCameraData.SMAAQualityLevel.Medium:
                        passData.smaaMaterial.EnableKeyword("SMAA_PRESET_MEDIUM");
                        break;
                    case HDAdditionalCameraData.SMAAQualityLevel.High:
                        passData.smaaMaterial.EnableKeyword("SMAA_PRESET_HIGH");
                        break;
                    default:
                        passData.smaaMaterial.EnableKeyword("SMAA_PRESET_HIGH");
                        break;
                }

                passData.source = builder.ReadTexture(source);
                passData.depthBuffer = builder.ReadWriteTexture(depthBuffer);
                passData.smaaEdgeTex = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, enableRandomWrite = true, clearBuffer = true, name = "SMAA Edge Texture" });
                passData.smaaBlendTex = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                    { colorFormat = GraphicsFormat.R8G8B8A8_UNorm, enableRandomWrite = true, clearBuffer = true, name = "SMAA Blend Texture" });

                TextureHandle dest = GetPostprocessOutputHandle(renderGraph, "SMAA Destination");
                passData.destination = builder.WriteTexture(dest);;

                builder.SetRenderFunc(
                    (SMAAData data, RenderGraphContext ctx) =>
                    {
                        data.smaaMaterial.SetVector(HDShaderIDs._SMAARTMetrics, data.smaaRTMetrics);
                        data.smaaMaterial.SetTexture(HDShaderIDs._SMAAAreaTex, data.smaaAreaTex);
                        data.smaaMaterial.SetTexture(HDShaderIDs._SMAASearchTex, data.smaaSearchTex);
                        data.smaaMaterial.SetInt(HDShaderIDs._StencilRef, (int)StencilUsage.SMAA);
                        data.smaaMaterial.SetInt(HDShaderIDs._StencilMask, (int)StencilUsage.SMAA);

                        // -----------------------------------------------------------------------------
                        // EdgeDetection stage
                        ctx.cmd.SetGlobalTexture(HDShaderIDs._InputTexture, data.source);
                        HDUtils.DrawFullScreen(ctx.cmd, data.smaaMaterial, data.smaaEdgeTex, data.depthBuffer, null, (int)SMAAStage.EdgeDetection);

                        // -----------------------------------------------------------------------------
                        // BlendWeights stage
                        ctx.cmd.SetGlobalTexture(HDShaderIDs._InputTexture, data.smaaEdgeTex);
                        HDUtils.DrawFullScreen(ctx.cmd, data.smaaMaterial, data.smaaBlendTex, data.depthBuffer, null, (int)SMAAStage.BlendWeights);

                        // -----------------------------------------------------------------------------
                        // NeighborhoodBlending stage
                        ctx.cmd.SetGlobalTexture(HDShaderIDs._InputTexture, data.source);
                        data.smaaMaterial.SetTexture(HDShaderIDs._SMAABlendTex, data.smaaBlendTex);
                        HDUtils.DrawFullScreen(ctx.cmd, data.smaaMaterial, data.destination, null, (int)SMAAStage.NeighborhoodBlending);
                    });

                return passData.destination;
            }
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
                if (taaEnabled && m_IsDoFHisotoryValid != m_DepthOfField.physicallyBased)
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
                            { colorFormat = k_CoCFormat, enableRandomWrite = true, useMipMap = true, name = "Full res CoC" }));

                        passData.pingFarRGB = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                            { colorFormat = m_ColorFormat, useMipMap = true, enableRandomWrite = true, name = "DoF Source Pyramid" });

                        builder.SetRenderFunc(
                            (DepthofFieldData data, RenderGraphContext ctx) =>
                            {
                                DoPhysicallyBasedDepthOfField(data.parameters, ctx.cmd, data.source, data.destination, data.fullresCoC, data.prevCoC, data.nextCoC, data.motionVecTexture, data.pingFarRGB, data.taaEnabled);
                            });

                        source = passData.destination;
                        m_HDInstance.PushFullScreenDebugTexture(renderGraph, passData.fullresCoC, FullScreenDebugMode.DepthOfFieldCoc);
                    }
                }
            }

            // When physically based DoF is enabled, TAA runs two times, first to stabilize the color buffer before DoF and then after DoF to accumulate more aperture samples
            if (taaEnabled && m_DepthOfField.physicallyBased)
            {
                source = DoTemporalAntialiasing(renderGraph, hdCamera, depthBuffer, motionVectors, depthBufferMipChain, source, postDoF: true, "Post-DoF TAA Destination");
                // Temporary hack to make post-dof TAA work with rendergraph (still the first frame flashes black). We need a better solution.
                m_IsDoFHisotoryValid = true;
                postDoFTAAEnabled = true;
            }
            else
            {
                // Temporary hack to make post-dof TAA work with rendergraph (still the first frame flashes black). We need a better solution.
                m_IsDoFHisotoryValid = false;
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
                    passData.destination = builder.WriteTexture(dest);;

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

        Vector2 CalcViewExtents(HDCamera camera)
        {
            float fovY = camera.camera.fieldOfView * Mathf.Deg2Rad;
            float aspect = (float)camera.actualWidth / (float)camera.actualHeight;

            float viewExtY = Mathf.Tan(0.5f * fovY);
            float viewExtX = aspect * viewExtY;

            return new Vector2(viewExtX, viewExtY);
        }

        Vector2 CalcCropExtents(HDCamera camera, float d)
        {
            // given
            //    S----------- E--X-------
            //    |    `  ~.  /,´
            //    |-- ---    Q
            //    |        ,/    `
            //  1 |      ,´/       `
            //    |    ,´ /         ´
            //    |  ,´  /           ´
            //    |,`   /             ,
            //    O    /
            //    |   /               ,
            //  d |  /
            //    | /                ,
            //    |/                .
            //    P
            //    |              ´
            //    |         , ´
            //    +-    ´
            //
            // have X
            // want to find E

            float viewDist = 1f + d;

            var projPos = CalcViewExtents(camera);
            var projHyp = Mathf.Sqrt(projPos.x * projPos.x + 1f);

            float cylDistMinusD = 1f / projHyp;
            float cylDist = cylDistMinusD + d;
            var cylPos = projPos * cylDistMinusD;

            return cylPos * (viewDist / cylDist);
        }

        TextureHandle PaniniProjectionPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source)
        {
            bool isSceneView = hdCamera.camera.cameraType == CameraType.SceneView;
            if (m_PaniniProjection.IsActive() && !isSceneView && m_PaniniProjectionFS)
            {
                using (var builder = renderGraph.AddRenderPass<PaniniProjectionData>("Panini Projection", out var passData, ProfilingSampler.Get(HDProfileId.PaniniProjection)))
                {
                    passData.width = hdCamera.actualWidth;
                    passData.height = hdCamera.actualHeight;
                    passData.viewCount = hdCamera.viewCount;
                    passData.paniniProjectionCS = m_Resources.shaders.paniniProjectionCS;
                    passData.paniniProjectionCS.shaderKeywords = null;

                    float distance = m_PaniniProjection.distance.value;
                    var viewExtents = CalcViewExtents(hdCamera);
                    var cropExtents = CalcCropExtents(hdCamera, distance);

                    float scaleX = cropExtents.x / viewExtents.x;
                    float scaleY = cropExtents.y / viewExtents.y;
                    float scaleF = Mathf.Min(scaleX, scaleY);

                    float paniniD = distance;
                    float paniniS = Mathf.Lerp(1.0f, Mathf.Clamp01(scaleF), m_PaniniProjection.cropToFit.value);

                    if (1f - Mathf.Abs(paniniD) > float.Epsilon)
                        passData.paniniProjectionCS.EnableKeyword("GENERIC");
                    else
                        passData.paniniProjectionCS.EnableKeyword("UNITDISTANCE");

                    passData.paniniParams = new Vector4(viewExtents.x, viewExtents.y, paniniD, paniniS);
                    passData.paniniProjectionKernel = passData.paniniProjectionCS.FindKernel("KMain");

                    passData.source = builder.ReadTexture(source);
                    passData.destination = builder.WriteTexture(GetPostprocessOutputHandle(renderGraph, "Panini Projection Destination"));

                    builder.SetRenderFunc(
                        (PaniniProjectionData data, RenderGraphContext ctx) =>
                        {
                            var cs = data.paniniProjectionCS;
                            int kernel = data.paniniProjectionKernel;

                            ctx.cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, data.paniniParams);
                            ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, data.source);
                            ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, data.destination);
                            ctx.cmd.DispatchCompute(cs, kernel, (data.width + 7) / 8, (data.height + 7) / 8, data.viewCount);
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

        #region Color Grading

        void PrepareColorGradingParameters(ColorGradingPassData passData)
        {
            passData.tonemappingMode = m_TonemappingFS ? m_Tonemapping.mode.value : TonemappingMode.None;

            passData.builderCS = m_Resources.shaders.lutBuilder3DCS;
            passData.builderKernel = passData.builderCS.FindKernel("KBuild");

            // Setup lut builder compute & grab the kernel we need
            passData.builderCS.shaderKeywords = null;

            if (m_Tonemapping.IsActive() && m_TonemappingFS)
            {
                switch (passData.tonemappingMode)
                {
                    case TonemappingMode.Neutral: passData.builderCS.EnableKeyword("TONEMAPPING_NEUTRAL"); break;
                    case TonemappingMode.ACES: passData.builderCS.EnableKeyword("TONEMAPPING_ACES"); break;
                    case TonemappingMode.Custom: passData.builderCS.EnableKeyword("TONEMAPPING_CUSTOM"); break;
                    case TonemappingMode.External: passData.builderCS.EnableKeyword("TONEMAPPING_EXTERNAL"); break;
                }
            }
            else
            {
                passData.builderCS.EnableKeyword("TONEMAPPING_NONE");
            }

            passData.lutSize = m_LutSize;

            //passData.colorFilter;
            passData.lmsColorBalance = GetColorBalanceCoeffs(m_WhiteBalance.temperature.value, m_WhiteBalance.tint.value);
            passData.hueSatCon = new Vector4(m_ColorAdjustments.hueShift.value / 360f, m_ColorAdjustments.saturation.value / 100f + 1f, m_ColorAdjustments.contrast.value / 100f + 1f, 0f);
            passData.channelMixerR = new Vector4(m_ChannelMixer.redOutRedIn.value / 100f, m_ChannelMixer.redOutGreenIn.value / 100f, m_ChannelMixer.redOutBlueIn.value / 100f, 0f);
            passData.channelMixerG = new Vector4(m_ChannelMixer.greenOutRedIn.value / 100f, m_ChannelMixer.greenOutGreenIn.value / 100f, m_ChannelMixer.greenOutBlueIn.value / 100f, 0f);
            passData.channelMixerB = new Vector4(m_ChannelMixer.blueOutRedIn.value / 100f, m_ChannelMixer.blueOutGreenIn.value / 100f, m_ChannelMixer.blueOutBlueIn.value / 100f, 0f);

            ComputeShadowsMidtonesHighlights(out passData.shadows, out passData.midtones, out passData.highlights, out passData.shadowsHighlightsLimits);
            ComputeLiftGammaGain(out passData.lift, out passData.gamma, out passData.gain);
            ComputeSplitToning(out passData.splitShadows, out passData.splitHighlights);

            // Be careful, if m_Curves is modified between preparing the render pass and executing it, result will be wrong.
            // However this should be fine for now as all updates should happen outisde rendering.
            passData.curves = m_Curves;

            if (passData.tonemappingMode == TonemappingMode.Custom)
            {
                passData.hableCurve = m_HableCurve;
                passData.hableCurve.Init(
                    m_Tonemapping.toeStrength.value,
                    m_Tonemapping.toeLength.value,
                    m_Tonemapping.shoulderStrength.value,
                    m_Tonemapping.shoulderLength.value,
                    m_Tonemapping.shoulderAngle.value,
                    m_Tonemapping.gamma.value
                );
            }
            else if (passData.tonemappingMode == TonemappingMode.External)
            {
                passData.externalLuT = m_Tonemapping.lutTexture.value;
                passData.lutContribution = m_Tonemapping.lutContribution.value;
            }

            passData.colorFilter = m_ColorAdjustments.colorFilter.value.linear;
            passData.miscParams = new Vector4(m_ColorGradingFS ? 1f : 0f, 0f, 0f, 0f);
        }

        // Returns color balance coefficients in the LMS space
        public static Vector3 GetColorBalanceCoeffs(float temperature, float tint)
        {
            // Range ~[-1.5;1.5] works best
            float t1 = temperature / 65f;
            float t2 = tint / 65f;

            // Get the CIE xy chromaticity of the reference white point.
            // Note: 0.31271 = x value on the D65 white point
            float x = 0.31271f - t1 * (t1 < 0f ? 0.1f : 0.05f);
            float y = ColorUtils.StandardIlluminantY(x) + t2 * 0.05f;

            // Calculate the coefficients in the LMS space.
            var w1 = new Vector3(0.949237f, 1.03542f, 1.08728f); // D65 white point
            var w2 = ColorUtils.CIExyToLMS(x, y);
            return new Vector3(w1.x / w2.x, w1.y / w2.y, w1.z / w2.z);
        }

        void ComputeShadowsMidtonesHighlights(out Vector4 shadows, out Vector4 midtones, out Vector4 highlights, out Vector4 limits)
        {
            float weight;

            shadows = m_ShadowsMidtonesHighlights.shadows.value;
            shadows.x = Mathf.GammaToLinearSpace(shadows.x);
            shadows.y = Mathf.GammaToLinearSpace(shadows.y);
            shadows.z = Mathf.GammaToLinearSpace(shadows.z);
            weight = shadows.w * (Mathf.Sign(shadows.w) < 0f ? 1f : 4f);
            shadows.x = Mathf.Max(shadows.x + weight, 0f);
            shadows.y = Mathf.Max(shadows.y + weight, 0f);
            shadows.z = Mathf.Max(shadows.z + weight, 0f);
            shadows.w = 0f;

            midtones = m_ShadowsMidtonesHighlights.midtones.value;
            midtones.x = Mathf.GammaToLinearSpace(midtones.x);
            midtones.y = Mathf.GammaToLinearSpace(midtones.y);
            midtones.z = Mathf.GammaToLinearSpace(midtones.z);
            weight = midtones.w * (Mathf.Sign(midtones.w) < 0f ? 1f : 4f);
            midtones.x = Mathf.Max(midtones.x + weight, 0f);
            midtones.y = Mathf.Max(midtones.y + weight, 0f);
            midtones.z = Mathf.Max(midtones.z + weight, 0f);
            midtones.w = 0f;

            highlights = m_ShadowsMidtonesHighlights.highlights.value;
            highlights.x = Mathf.GammaToLinearSpace(highlights.x);
            highlights.y = Mathf.GammaToLinearSpace(highlights.y);
            highlights.z = Mathf.GammaToLinearSpace(highlights.z);
            weight = highlights.w * (Mathf.Sign(highlights.w) < 0f ? 1f : 4f);
            highlights.x = Mathf.Max(highlights.x + weight, 0f);
            highlights.y = Mathf.Max(highlights.y + weight, 0f);
            highlights.z = Mathf.Max(highlights.z + weight, 0f);
            highlights.w = 0f;

            limits = new Vector4(
                m_ShadowsMidtonesHighlights.shadowsStart.value,
                m_ShadowsMidtonesHighlights.shadowsEnd.value,
                m_ShadowsMidtonesHighlights.highlightsStart.value,
                m_ShadowsMidtonesHighlights.highlightsEnd.value
            );
        }

        void ComputeLiftGammaGain(out Vector4 lift, out Vector4 gamma, out Vector4 gain)
        {
            lift = m_LiftGammaGain.lift.value;
            lift.x = Mathf.GammaToLinearSpace(lift.x) * 0.15f;
            lift.y = Mathf.GammaToLinearSpace(lift.y) * 0.15f;
            lift.z = Mathf.GammaToLinearSpace(lift.z) * 0.15f;

            float lumLift = ColorUtils.Luminance(lift);
            lift.x = lift.x - lumLift + lift.w;
            lift.y = lift.y - lumLift + lift.w;
            lift.z = lift.z - lumLift + lift.w;
            lift.w = 0f;

            gamma = m_LiftGammaGain.gamma.value;
            gamma.x = Mathf.GammaToLinearSpace(gamma.x) * 0.8f;
            gamma.y = Mathf.GammaToLinearSpace(gamma.y) * 0.8f;
            gamma.z = Mathf.GammaToLinearSpace(gamma.z) * 0.8f;

            float lumGamma = ColorUtils.Luminance(gamma);
            gamma.w += 1f;
            gamma.x = 1f / Mathf.Max(gamma.x - lumGamma + gamma.w, 1e-03f);
            gamma.y = 1f / Mathf.Max(gamma.y - lumGamma + gamma.w, 1e-03f);
            gamma.z = 1f / Mathf.Max(gamma.z - lumGamma + gamma.w, 1e-03f);
            gamma.w = 0f;

            gain = m_LiftGammaGain.gain.value;
            gain.x = Mathf.GammaToLinearSpace(gain.x) * 0.8f;
            gain.y = Mathf.GammaToLinearSpace(gain.y) * 0.8f;
            gain.z = Mathf.GammaToLinearSpace(gain.z) * 0.8f;

            float lumGain = ColorUtils.Luminance(gain);
            gain.w += 1f;
            gain.x = gain.x - lumGain + gain.w;
            gain.y = gain.y - lumGain + gain.w;
            gain.z = gain.z - lumGain + gain.w;
            gain.w = 0f;
        }

        void ComputeSplitToning(out Vector4 shadows, out Vector4 highlights)
        {
            // As counter-intuitive as it is, to make split-toning work the same way it does in
            // Adobe products we have to do all the maths in sRGB... So do not convert these to
            // linear before sending them to the shader, this isn't a bug!
            shadows = m_SplitToning.shadows.value;
            highlights = m_SplitToning.highlights.value;

            // Balance is stored in `shadows.w`
            shadows.w = m_SplitToning.balance.value / 100f;
            highlights.w = 0f;
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

                PrepareColorGradingParameters(passData);
                passData.logLut = builder.WriteTexture(logLut);
                logLutOutput = passData.logLut;

                builder.SetRenderFunc(
                    (ColorGradingPassData data, RenderGraphContext ctx) =>
                    {
                        var builderCS = data.builderCS;
                        var builderKernel = data.builderKernel;

                        // Fill-in constant buffers & textures.
                        // TODO: replace with a real constant buffers
                        ctx.cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._OutputTexture, data.logLut);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Size, new Vector4(data.lutSize, 1f / (data.lutSize - 1f), 0f, 0f));
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ColorBalance, data.lmsColorBalance);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ColorFilter, data.colorFilter);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ChannelMixerRed, data.channelMixerR);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ChannelMixerGreen, data.channelMixerG);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ChannelMixerBlue, data.channelMixerB);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._HueSatCon, data.hueSatCon);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Lift, data.lift);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Gamma, data.gamma);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Gain, data.gain);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Shadows, data.shadows);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Midtones, data.midtones);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Highlights, data.highlights);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ShaHiLimits, data.shadowsHighlightsLimits);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._SplitShadows, data.splitShadows);
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._SplitHighlights, data.splitHighlights);

                        // YRGB
                        ctx.cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveMaster, data.curves.master.value.GetTexture());
                        ctx.cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveRed, data.curves.red.value.GetTexture());
                        ctx.cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveGreen, data.curves.green.value.GetTexture());
                        ctx.cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveBlue, data.curves.blue.value.GetTexture());

                        // Secondary curves
                        ctx.cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveHueVsHue, data.curves.hueVsHue.value.GetTexture());
                        ctx.cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveHueVsSat, data.curves.hueVsSat.value.GetTexture());
                        ctx.cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveLumVsSat, data.curves.lumVsSat.value.GetTexture());
                        ctx.cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._CurveSatVsSat, data.curves.satVsSat.value.GetTexture());

                        // Artist-driven tonemap curve
                        if (data.tonemappingMode == TonemappingMode.Custom)
                        {
                            ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._CustomToneCurve, data.hableCurve.uniforms.curve);
                            ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ToeSegmentA, data.hableCurve.uniforms.toeSegmentA);
                            ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ToeSegmentB, data.hableCurve.uniforms.toeSegmentB);
                            ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._MidSegmentA, data.hableCurve.uniforms.midSegmentA);
                            ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._MidSegmentB, data.hableCurve.uniforms.midSegmentB);
                            ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ShoSegmentA, data.hableCurve.uniforms.shoSegmentA);
                            ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._ShoSegmentB, data.hableCurve.uniforms.shoSegmentB);
                        }
                        else if (data.tonemappingMode == TonemappingMode.External)
                        {
                            ctx.cmd.SetComputeTextureParam(builderCS, builderKernel, HDShaderIDs._LogLut3D, data.externalLuT);
                            ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._LogLut3D_Params, new Vector4(1f / data.lutSize, data.lutSize - 1f, data.lutContribution, 0f));
                        }

                        // Misc parameters
                        ctx.cmd.SetComputeVectorParam(builderCS, HDShaderIDs._Params, data.miscParams);

                        // Generate the lut
                        // See the note about Metal & Intel in LutBuilder3D.compute
                        // GetKernelThreadGroupSizes  is currently broken on some binary versions.
                        //builderCS.GetKernelThreadGroupSizes(builderKernel, out uint threadX, out uint threadY, out uint threadZ);
                        uint threadX = 4;
                        uint threadY = 4;
                        uint threadZ = 4;
                        ctx.cmd.DispatchCompute(builderCS, builderKernel,
                            (int)((data.lutSize + threadX - 1u) / threadX),
                            (int)((data.lutSize + threadY - 1u) / threadY),
                            (int)((data.lutSize + threadZ - 1u) / threadZ)
                        );
                    });
            }

            return logLutOutput;
        }

        #endregion

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
                    passData.destination = builder.WriteTexture(dest);;

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
                    TextureHandle dest = GetPostprocessOutputHandle(renderGraph, "Contrast Adaptive Sharpen Destination");
                    passData.destination = builder.WriteTexture(dest);;

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

        internal void DoUserAfterOpaqueAndSky(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthBuffer, TextureHandle normalBuffer)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.CustomPostProcess))
                return;

            using (new RenderGraphProfilingScope(renderGraph, ProfilingSampler.Get(HDProfileId.CustomPostProcessAfterOpaqueAndSky)))
            {
                TextureHandle source = colorBuffer;
                bool needBlitToColorBuffer = DoCustomPostProcess(renderGraph, hdCamera, ref source, depthBuffer, normalBuffer, HDRenderPipeline.defaultAsset.beforeTransparentCustomPostProcesses);

                if (needBlitToColorBuffer)
                {
                    HDRenderPipeline.BlitCameraTexture(renderGraph, source, colorBuffer);
                }
            }
        }

        bool DoCustomPostProcess(RenderGraph renderGraph, HDCamera hdCamera, ref TextureHandle source, TextureHandle depthBuffer, TextureHandle normalBuffer, List<string> postProcessList)
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

        TextureHandle CustomPostProcessPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source, TextureHandle depthBuffer, TextureHandle normalBuffer, List<string> postProcessList, HDProfileId profileId)
        {
            if (!hdCamera.frameSettings.IsEnabled(FrameSettingsField.CustomPostProcess))
                return source;

            using (new RenderGraphProfilingScope(renderGraph, ProfilingSampler.Get(profileId)))
            {
                DoCustomPostProcess(renderGraph, hdCamera, ref source, depthBuffer, normalBuffer, postProcessList);
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

                source = CustomPostProcessPass(renderGraph, hdCamera, source, depthBuffer, normalBuffer, HDRenderPipeline.defaultAsset.beforeTAACustomPostProcesses, HDProfileId.CustomPostProcessBeforeTAA);

                // Temporal anti-aliasing goes first
                if (m_AntialiasingFS)
                {
                    if (hdCamera.antialiasing == HDAdditionalCameraData.AntialiasingMode.TemporalAntialiasing)
                    {
                        source = DoTemporalAntialiasing(renderGraph, hdCamera, depthBuffer, motionVectors, depthBufferMipChain, source, postDoF: false, "TAA Destination");
                    }
                    else if (hdCamera.antialiasing == HDAdditionalCameraData.AntialiasingMode.SubpixelMorphologicalAntiAliasing)
                    {
                        source = SMAAPass(renderGraph, hdCamera, depthBuffer, source);
                    }
                }

                source = CustomPostProcessPass(renderGraph, hdCamera, source, depthBuffer, normalBuffer, HDRenderPipeline.defaultAsset.beforePostProcessCustomPostProcesses, HDProfileId.CustomPostProcessBeforePP);

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

                source = CustomPostProcessPass(renderGraph, hdCamera, source, depthBuffer, normalBuffer, HDRenderPipeline.defaultAsset.afterPostProcessCustomPostProcesses, HDProfileId.CustomPostProcessAfterPP);

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
