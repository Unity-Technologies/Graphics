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
            public ComputeShader uberPostCS;
            public int uberPostKernel;
            public bool outputColorLog;
            public int width;
            public int height;
            public int viewCount;

            public Vector4 logLutSettings;

            public Vector4 lensDistortionParams1;
            public Vector4 lensDistortionParams2;

            public Texture spectralLut;
            public Vector4 chromaticAberrationParameters;

            public Vector4 vignetteParams1;
            public Vector4 vignetteParams2;
            public Vector4 vignetteColor;
            public Texture vignetteMask;

            public Texture bloomDirtTexture;
            public Vector4 bloomParams;
            public Vector4 bloomTint;
            public Vector4 bloomBicubicParams;
            public Vector4 bloomDirtTileOffset;
            public Vector4 bloomThreshold;

            public Vector4 alphaScaleBias;

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
            public ComputeShader fxaaCS;
            public int fxaaKernel;
            public int width;
            public int height;
            public int viewCount;

            public TextureHandle source;
            public TextureHandle destination;
        }

        class MotionBlurData
        {
            public ComputeShader motionVecPrepCS;
            public ComputeShader tileGenCS;
            public ComputeShader tileNeighbourhoodCS;
            public ComputeShader tileMergeCS;
            public ComputeShader motionBlurCS;

            public int motionVecPrepKernel;
            public int tileGenKernel;
            public int tileNeighbourhoodKernel;
            public int tileMergeKernel;
            public int motionBlurKernel;

            public HDCamera camera;

            public Vector4 tileTargetSize;
            public Vector4 motionBlurParams0;
            public Vector4 motionBlurParams1;
            public Vector4 motionBlurParams2;
            public Vector4 motionBlurParams3;

            public bool motionblurSupportScattering;

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
            public ComputeShader bloomPrefilterCS;
            public ComputeShader bloomBlurCS;
            public ComputeShader bloomUpsampleCS;

            public int bloomPrefilterKernel;
            public int bloomBlurKernel;
            public int bloomDownsampleKernel;
            public int bloomUpsampleKernel;

            public int viewCount;
            public int bloomMipCount;
            public Vector4[] bloomMipInfo = new Vector4[k_MaxBloomMipCount + 1];

            public float bloomScatterParam;
            public Vector4 thresholdParams;

            public TextureHandle source;
            public TextureHandle[] mipsDown = new TextureHandle[k_MaxBloomMipCount + 1];
            public TextureHandle[] mipsUp = new TextureHandle[k_MaxBloomMipCount + 1];
        }

        class CASData
        {
            public ComputeShader casCS;
            public int initKernel;
            public int mainKernel;
            public int viewCount;
            public int inputWidth;
            public int inputHeight;
            public int outputWidth;
            public int outputHeight;

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

            cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, data.tmpTarget1024);
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
                        passData.destination = builder.WriteTexture(dest);

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
            passData.destination = builder.WriteTexture(dest);
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
                                DoPhysicallyBasedDepthOfField(data.parameters, ctx.cmd, data.source, data.destination, data.fullresCoC, data.prevCoC, data.nextCoC, data.motionVecTexture, data.pingFarRGB, data.depthBuffer, data.taaEnabled);
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

        #region Motion Blur

        void PrepareMotionBlurPassData(RenderGraph renderGraph, in RenderGraphBuilder builder, MotionBlurData data, HDCamera hdCamera, TextureHandle source, TextureHandle motionVectors, TextureHandle depthTexture)
        {
            data.camera = hdCamera;

            int tileSize = 32;

            if (m_MotionBlurSupportsScattering)
            {
                tileSize = 16;
            }

            int tileTexWidth = Mathf.CeilToInt(hdCamera.actualWidth / tileSize);
            int tileTexHeight = Mathf.CeilToInt(hdCamera.actualHeight / tileSize);
            data.tileTargetSize = new Vector4(tileTexWidth, tileTexHeight, 1.0f / tileTexWidth, 1.0f / tileTexHeight);

            float screenMagnitude = (new Vector2(hdCamera.actualWidth, hdCamera.actualHeight).magnitude);
            data.motionBlurParams0 = new Vector4(
                screenMagnitude,
                screenMagnitude * screenMagnitude,
                m_MotionBlur.minimumVelocity.value,
                m_MotionBlur.minimumVelocity.value * m_MotionBlur.minimumVelocity.value
            );

            data.motionBlurParams1 = new Vector4(
                m_MotionBlur.intensity.value,
                m_MotionBlur.maximumVelocity.value / screenMagnitude,
                0.25f, // min/max velocity ratio for high quality.
                m_MotionBlur.cameraRotationVelocityClamp.value
            );

            uint sampleCount = (uint)m_MotionBlur.sampleCount;
            data.motionBlurParams2 = new Vector4(
                m_MotionBlurSupportsScattering ? (sampleCount + (sampleCount & 1)) : sampleCount,
                tileSize,
                m_MotionBlur.depthComparisonExtent.value,
                m_MotionBlur.cameraMotionBlur.value ? 0.0f : 1.0f
            );

            data.motionVecPrepCS = m_Resources.shaders.motionBlurMotionVecPrepCS;
            data.motionVecPrepKernel = data.motionVecPrepCS.FindKernel("MotionVecPreppingCS");
            data.motionVecPrepCS.shaderKeywords = null;

            if (!m_MotionBlur.cameraMotionBlur.value)
            {
                data.motionVecPrepCS.EnableKeyword("CAMERA_DISABLE_CAMERA");
            }
            else
            {
                var clampMode = m_MotionBlur.specialCameraClampMode.value;
                if (clampMode == CameraClampMode.None)
                    data.motionVecPrepCS.EnableKeyword("NO_SPECIAL_CLAMP");
                else if (clampMode == CameraClampMode.Rotation)
                    data.motionVecPrepCS.EnableKeyword("CAMERA_ROT_CLAMP");
                else if (clampMode == CameraClampMode.Translation)
                    data.motionVecPrepCS.EnableKeyword("CAMERA_TRANS_CLAMP");
                else if (clampMode == CameraClampMode.SeparateTranslationAndRotation)
                    data.motionVecPrepCS.EnableKeyword("CAMERA_SEPARATE_CLAMP");
                else if (clampMode == CameraClampMode.FullCameraMotionVector)
                    data.motionVecPrepCS.EnableKeyword("CAMERA_FULL_CLAMP");
            }

            data.motionBlurParams3 = new Vector4(
                m_MotionBlur.cameraTranslationVelocityClamp.value,
                m_MotionBlur.cameraVelocityClamp.value,
                0, 0);


            data.tileGenCS = m_Resources.shaders.motionBlurGenTileCS;
            data.tileGenCS.shaderKeywords = null;
            if (m_MotionBlurSupportsScattering)
            {
                data.tileGenCS.EnableKeyword("SCATTERING");
            }
            data.tileGenKernel = data.tileGenCS.FindKernel("TileGenPass");

            data.tileNeighbourhoodCS = m_Resources.shaders.motionBlurNeighborhoodTileCS;
            data.tileNeighbourhoodCS.shaderKeywords = null;
            if (m_MotionBlurSupportsScattering)
            {
                data.tileNeighbourhoodCS.EnableKeyword("SCATTERING");
            }
            data.tileNeighbourhoodKernel = data.tileNeighbourhoodCS.FindKernel("TileNeighbourhood");

            data.tileMergeCS = m_Resources.shaders.motionBlurMergeTileCS;
            data.tileMergeKernel = data.tileMergeCS.FindKernel("TileMerge");

            data.motionBlurCS = m_Resources.shaders.motionBlurCS;
            data.motionBlurCS.shaderKeywords = null;
            CoreUtils.SetKeyword(data.motionBlurCS, "ENABLE_ALPHA", m_EnableAlpha);
            data.motionBlurKernel = data.motionBlurCS.FindKernel("MotionBlurCS");

            data.motionblurSupportScattering = m_MotionBlurSupportsScattering;

            data.source = builder.ReadTexture(source);
            data.motionVecTexture = builder.ReadTexture(motionVectors);
            data.depthBuffer = builder.ReadTexture(depthTexture);

            Vector2 tileTexScale = new Vector2((float)data.tileTargetSize.x / hdCamera.actualWidth, (float)data.tileTargetSize.y / hdCamera.actualHeight);

            data.preppedMotionVec = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite = true, name = "Prepped Motion Vectors" });

            data.minMaxTileVel = builder.CreateTransientTexture(new TextureDesc(tileTexScale, true, true)
                { colorFormat = GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite = true, name = "MinMax Tile Motion Vectors" });

            data.maxTileNeigbourhood = builder.CreateTransientTexture(new TextureDesc(tileTexScale, true, true)
                { colorFormat = GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite = true, name = "Max Neighborhood Tile" });

            data.tileToScatterMax = TextureHandle.nullHandle;
            data.tileToScatterMin = TextureHandle.nullHandle;

            if (data.motionblurSupportScattering)
            {
                data.tileToScatterMax = builder.CreateTransientTexture(new TextureDesc(tileTexScale, true, true)
                    { colorFormat = GraphicsFormat.R32_UInt, enableRandomWrite = true, name = "Tile to Scatter Max" });

                data.tileToScatterMin = builder.CreateTransientTexture(new TextureDesc(tileTexScale, true, true)
                    { colorFormat = GraphicsFormat.R16_SFloat, enableRandomWrite = true, name = "Tile to Scatter Min" });
            }

            data.destination = builder.WriteTexture(GetPostprocessOutputHandle(renderGraph, "Motion Blur Destination"));
        }

        static void DoMotionBlur(MotionBlurData data, CommandBuffer cmd)
        {
            int tileSize = 32;

            if (data.motionblurSupportScattering)
            {
                tileSize = 16;
            }

            // -----------------------------------------------------------------------------
            // Prep motion vectors

            // - Pack normalized motion vectors and linear depth in R11G11B10
            ComputeShader cs;
            int kernel;
            int threadGroupX;
            int threadGroupY;
            int groupSizeX = 8;
            int groupSizeY = 8;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.MotionBlurMotionVecPrep)))
            {
                cs = data.motionVecPrepCS;
                kernel = data.motionVecPrepKernel;
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._MotionVecAndDepth, data.preppedMotionVec);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._CameraDepthTexture, data.depthBuffer);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams, data.motionBlurParams0);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams1, data.motionBlurParams1);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams2, data.motionBlurParams2);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams3, data.motionBlurParams3);

                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._CameraMotionVectorsTexture, data.motionVecTexture);

                cmd.SetComputeMatrixParam(cs, HDShaderIDs._PrevVPMatrixNoTranslation, data.camera.mainViewConstants.prevViewProjMatrixNoCameraTrans);
                cmd.SetComputeMatrixParam(cs, HDShaderIDs._CurrVPMatrixNoTranslation, data.camera.mainViewConstants.viewProjectionNoCameraTrans);

                threadGroupX = (data.camera.actualWidth + (groupSizeX - 1)) / groupSizeX;
                threadGroupY = (data.camera.actualHeight + (groupSizeY - 1)) / groupSizeY;
                cmd.DispatchCompute(cs, kernel, threadGroupX, threadGroupY, data.camera.viewCount);
            }


            // -----------------------------------------------------------------------------
            // Generate MinMax motion vectors tiles

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.MotionBlurTileMinMax)))
            {
                // We store R11G11B10 with RG = Max vel and B = Min vel magnitude
                cs = data.tileGenCS;
                kernel = data.tileGenKernel;

                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileMinMaxMotionVec, data.minMaxTileVel);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._MotionVecAndDepth, data.preppedMotionVec);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams, data.motionBlurParams0);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams1, data.motionBlurParams1);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams2, data.motionBlurParams2);


                if (data.motionblurSupportScattering)
                {
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileToScatterMax, data.tileToScatterMax);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileToScatterMin, data.tileToScatterMin);
                }

                threadGroupX = (data.camera.actualWidth + (tileSize - 1)) / tileSize;
                threadGroupY = (data.camera.actualHeight + (tileSize - 1)) / tileSize;
                cmd.DispatchCompute(cs, kernel, threadGroupX, threadGroupY, data.camera.viewCount);
            }

            // -----------------------------------------------------------------------------
            // Generate max tiles neigbhourhood

            using (new ProfilingScope(cmd, data.motionblurSupportScattering ? ProfilingSampler.Get(HDProfileId.MotionBlurTileScattering) : ProfilingSampler.Get(HDProfileId.MotionBlurTileNeighbourhood)))
            {
                cs = data.tileNeighbourhoodCS;
                kernel = data.tileNeighbourhoodKernel;


                cmd.SetComputeVectorParam(cs, HDShaderIDs._TileTargetSize, data.tileTargetSize);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileMinMaxMotionVec, data.minMaxTileVel);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileMaxNeighbourhood, data.maxTileNeigbourhood);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams, data.motionBlurParams0);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams1, data.motionBlurParams1);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams2, data.motionBlurParams2);

                if (data.motionblurSupportScattering)
                {
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileToScatterMax, data.tileToScatterMax);
                    cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileToScatterMin, data.tileToScatterMin);
                }
                groupSizeX = 8;
                groupSizeY = 8;
                threadGroupX = ((int)data.tileTargetSize.x + (groupSizeX - 1)) / groupSizeX;
                threadGroupY = ((int)data.tileTargetSize.y + (groupSizeY - 1)) / groupSizeY;
                cmd.DispatchCompute(cs, kernel, threadGroupX, threadGroupY, data.camera.viewCount);
            }

            // -----------------------------------------------------------------------------
            // Merge min/max info spreaded above.

            if (data.motionblurSupportScattering)
            {
                cs = data.tileMergeCS;
                kernel = data.tileMergeKernel;
                cmd.SetComputeVectorParam(cs, HDShaderIDs._TileTargetSize, data.tileTargetSize);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileToScatterMax, data.tileToScatterMax);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileToScatterMin, data.tileToScatterMin);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileMaxNeighbourhood, data.maxTileNeigbourhood);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams, data.motionBlurParams0);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams1, data.motionBlurParams1);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams2, data.motionBlurParams2);

                cmd.DispatchCompute(cs, kernel, threadGroupX, threadGroupY, data.camera.viewCount);
            }

            // -----------------------------------------------------------------------------
            // Blur kernel
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.MotionBlurKernel)))
            {
                cs = data.motionBlurCS;
                kernel = data.motionBlurKernel;

                cmd.SetComputeVectorParam(cs, HDShaderIDs._TileTargetSize, data.tileTargetSize);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._MotionVecAndDepth, data.preppedMotionVec);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, data.destination);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._TileMaxNeighbourhood, data.maxTileNeigbourhood);
                cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, data.source);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams, data.motionBlurParams0);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams1, data.motionBlurParams1);
                cmd.SetComputeVectorParam(cs, HDShaderIDs._MotionBlurParams2, data.motionBlurParams2);

                groupSizeX = 16;
                groupSizeY = 16;
                threadGroupX = (data.camera.actualWidth + (groupSizeX - 1)) / groupSizeX;
                threadGroupY = (data.camera.actualHeight + (groupSizeY - 1)) / groupSizeY;
                cmd.DispatchCompute(cs, kernel, threadGroupX, threadGroupY, data.camera.viewCount);
            }
        }

        TextureHandle MotionBlurPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthTexture, TextureHandle motionVectors, TextureHandle source)
        {
            if (m_MotionBlur.IsActive() && m_AnimatedMaterialsEnabled && !hdCamera.resetPostProcessingHistory && m_MotionBlurFS)
            {
                using (var builder = renderGraph.AddRenderPass<MotionBlurData>("Motion Blur", out var passData, ProfilingSampler.Get(HDProfileId.MotionBlur)))
                {
                    PrepareMotionBlurPassData(renderGraph, builder, passData, hdCamera, source, motionVectors, depthTexture);

                    builder.SetRenderFunc(
                        (MotionBlurData data, RenderGraphContext ctx) =>
                        {
                            DoMotionBlur(data, ctx.cmd);
                        });

                    source = passData.destination;
                }
            }

            return source;
        }

        #endregion

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

        #region Bloom

        void PrepareBloomData(RenderGraph renderGraph, in RenderGraphBuilder builder, BloomData passData, HDCamera camera, TextureHandle source)
        {
            passData.viewCount = camera.viewCount;
            passData.bloomPrefilterCS = m_Resources.shaders.bloomPrefilterCS;
            passData.bloomPrefilterKernel = passData.bloomPrefilterCS.FindKernel("KMain");

            passData.bloomPrefilterCS.shaderKeywords = null;
            if (m_Bloom.highQualityPrefiltering)
                passData.bloomPrefilterCS.EnableKeyword("HIGH_QUALITY");
            else
                passData.bloomPrefilterCS.EnableKeyword("LOW_QUALITY");
            if (m_EnableAlpha)
                passData.bloomPrefilterCS.EnableKeyword("ENABLE_ALPHA");

            passData.bloomBlurCS = m_Resources.shaders.bloomBlurCS;
            passData.bloomBlurKernel = passData.bloomBlurCS.FindKernel("KMain");
            passData.bloomDownsampleKernel = passData.bloomBlurCS.FindKernel("KDownsample");
            passData.bloomUpsampleCS = m_Resources.shaders.bloomUpsampleCS;
            passData.bloomUpsampleCS.shaderKeywords = null;

            var highQualityFiltering = m_Bloom.highQualityFiltering;
            // We switch to bilinear upsampling as it goes less wide than bicubic and due to our border/RTHandle handling, going wide on small resolution
            // where small mips have a strong influence, might result problematic.
            if (camera.actualWidth < 800 || camera.actualHeight < 450) highQualityFiltering = false;

            if (highQualityFiltering)
                passData.bloomUpsampleCS.EnableKeyword("HIGH_QUALITY");
            else
                passData.bloomUpsampleCS.EnableKeyword("LOW_QUALITY");

            passData.bloomUpsampleKernel = passData.bloomUpsampleCS.FindKernel("KMain");
            passData.bloomScatterParam = Mathf.Lerp(0.05f, 0.95f, m_Bloom.scatter.value);
            passData.thresholdParams = GetBloomThresholdParams();

            var resolution = m_Bloom.resolution;
            float scaleW = 1f / ((int)resolution / 2f);
            float scaleH = 1f / ((int)resolution / 2f);

            // If the scene is less than 50% of 900p, then we operate on full res, since it's going to be cheap anyway and this might improve quality in challenging situations.
            if (camera.actualWidth < 800 || camera.actualHeight < 450)
            {
                scaleW = 1.0f;
                scaleH = 1.0f;
            }

            if (m_Bloom.anamorphic.value)
            {
                // Positive anamorphic ratio values distort vertically - negative is horizontal
                float anamorphism = m_PhysicalCamera.anamorphism * 0.5f;
                scaleW *= anamorphism < 0 ? 1f + anamorphism : 1f;
                scaleH *= anamorphism > 0 ? 1f - anamorphism : 1f;
            }

            // Determine the iteration count
            int maxSize = Mathf.Max(camera.actualWidth, camera.actualHeight);
            int iterations = Mathf.FloorToInt(Mathf.Log(maxSize, 2f) - 2 - (resolution == BloomResolution.Half ? 0 : 1));
            passData.bloomMipCount = Mathf.Clamp(iterations, 1, k_MaxBloomMipCount);

            for (int i = 0; i < passData.bloomMipCount; i++)
            {
                float p = 1f / Mathf.Pow(2f, i + 1f);
                float sw = scaleW * p;
                float sh = scaleH * p;
                int pw, ph;
                if (camera.DynResRequest.hardwareEnabled)
                {
                    pw = Mathf.Max(1, Mathf.CeilToInt(sw * camera.actualWidth));
                    ph = Mathf.Max(1, Mathf.CeilToInt(sh * camera.actualHeight));
                }
                else
                {
                    pw = Mathf.Max(1, Mathf.RoundToInt(sw * camera.actualWidth));
                    ph = Mathf.Max(1, Mathf.RoundToInt(sh * camera.actualHeight));
                }
                var scale = new Vector2(sw, sh);
                var pixelSize = new Vector2Int(pw, ph);

                passData.bloomMipInfo[i] = new Vector4(pw, ph, sw, sh);
                passData.mipsDown[i] = builder.CreateTransientTexture(new TextureDesc(scale, true, true)
                    { colorFormat = m_ColorFormat, enableRandomWrite = true, name = "BloomMipDown" });

                if (i != 0)
                {
                    passData.mipsUp[i] = builder.CreateTransientTexture(new TextureDesc(scale, true, true)
                        { colorFormat = m_ColorFormat, enableRandomWrite = true, name = "BloomMipUp" });
                }
            }

            // the mip up 0 will be used by uber, so not allocated as transient.
            m_BloomBicubicParams = new Vector4(passData.bloomMipInfo[0].x, passData.bloomMipInfo[0].y, 1.0f / passData.bloomMipInfo[0].x, 1.0f / passData.bloomMipInfo[0].y);
            var mip0Scale = new Vector2(passData.bloomMipInfo[0].z, passData.bloomMipInfo[0].w);
            passData.mipsUp[0] = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(mip0Scale, true, true)
            {
                name = "Bloom final mip up",
                colorFormat = m_ColorFormat,
                useMipMap = false,
                enableRandomWrite = true
            }));
            passData.source = builder.ReadTexture(source);
        }

        TextureHandle BloomPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source)
        {
            bool bloomActive = m_Bloom.IsActive() && m_BloomFS;
            TextureHandle bloomTexture = renderGraph.defaultResources.blackTextureXR;
            if (bloomActive)
            {
                using (var builder = renderGraph.AddRenderPass<BloomData>("Bloom", out var passData, ProfilingSampler.Get(HDProfileId.Bloom)))
                {
                    PrepareBloomData(renderGraph, builder, passData, hdCamera, source);

                    builder.SetRenderFunc(
                        (BloomData data, RenderGraphContext ctx) =>
                        {
                            RTHandle sourceRT = data.source;

                            // All the computes for this effect use the same group size so let's use a local
                            // function to simplify dispatches
                            // Make sure the thread group count is sufficient to draw the guard bands
                            void DispatchWithGuardBands(CommandBuffer cmd, ComputeShader shader, int kernelId, in Vector2Int size, in int viewCount)
                            {
                                int w = size.x;
                                int h = size.y;

                                if (w < sourceRT.rt.width && w % 8 < k_RTGuardBandSize)
                                    w += k_RTGuardBandSize;
                                if (h < sourceRT.rt.height && h % 8 < k_RTGuardBandSize)
                                    h += k_RTGuardBandSize;

                                cmd.DispatchCompute(shader, kernelId, (w + 7) / 8, (h + 7) / 8, viewCount);
                            }

                            // Pre-filtering
                            ComputeShader cs;
                            int kernel;
                            {
                                var size = new Vector2Int((int)data.bloomMipInfo[0].x, (int)data.bloomMipInfo[0].y);
                                cs = data.bloomPrefilterCS;
                                kernel = data.bloomPrefilterKernel;

                                ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, sourceRT);
                                ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, data.mipsUp[0]); // Use m_BloomMipsUp as temp target
                                ctx.cmd.SetComputeVectorParam(cs, HDShaderIDs._TexelSize, new Vector4(size.x, size.y, 1f / size.x, 1f / size.y));
                                ctx.cmd.SetComputeVectorParam(cs, HDShaderIDs._BloomThreshold, data.thresholdParams);
                                DispatchWithGuardBands(ctx.cmd, cs, kernel, size, data.viewCount);

                                cs = data.bloomBlurCS;
                                kernel = data.bloomBlurKernel;

                                ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, data.mipsUp[0]);
                                ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, data.mipsDown[0]);
                                ctx.cmd.SetComputeVectorParam(cs, HDShaderIDs._TexelSize, new Vector4(size.x, size.y, 1f / size.x, 1f / size.y));
                                DispatchWithGuardBands(ctx.cmd, cs, kernel, size, data.viewCount);
                            }

                            // Blur pyramid
                            kernel = data.bloomDownsampleKernel;

                            for (int i = 0; i < data.bloomMipCount - 1; i++)
                            {
                                var src = data.mipsDown[i];
                                var dst = data.mipsDown[i + 1];
                                var size = new Vector2Int((int)data.bloomMipInfo[i + 1].x, (int)data.bloomMipInfo[i + 1].y);

                                ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputTexture, src);
                                ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, dst);
                                ctx.cmd.SetComputeVectorParam(cs, HDShaderIDs._TexelSize, new Vector4(size.x, size.y, 1f / size.x, 1f / size.y));
                                DispatchWithGuardBands(ctx.cmd, cs, kernel, size, data.viewCount);
                            }

                            // Upsample & combine
                            cs = data.bloomUpsampleCS;
                            kernel = data.bloomUpsampleKernel;

                            for (int i = data.bloomMipCount - 2; i >= 0; i--)
                            {
                                var low = (i == data.bloomMipCount - 2) ? data.mipsDown : data.mipsUp;
                                var srcLow = low[i + 1];
                                var srcHigh = data.mipsDown[i];
                                var dst = data.mipsUp[i];
                                var highSize = new Vector2Int((int)data.bloomMipInfo[i].x, (int)data.bloomMipInfo[i].y);
                                var lowSize = new Vector2Int((int)data.bloomMipInfo[i + 1].x, (int)data.bloomMipInfo[i + 1].y);

                                ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputLowTexture, srcLow);
                                ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._InputHighTexture, srcHigh);
                                ctx.cmd.SetComputeTextureParam(cs, kernel, HDShaderIDs._OutputTexture, dst);
                                ctx.cmd.SetComputeVectorParam(cs, HDShaderIDs._Params, new Vector4(data.bloomScatterParam, 0f, 0f, 0f));
                                ctx.cmd.SetComputeVectorParam(cs, HDShaderIDs._BloomBicubicParams, new Vector4(lowSize.x, lowSize.y, 1f / lowSize.x, 1f / lowSize.y));
                                ctx.cmd.SetComputeVectorParam(cs, HDShaderIDs._TexelSize, new Vector4(highSize.x, highSize.y, 1f / highSize.x, 1f / highSize.y));
                                DispatchWithGuardBands(ctx.cmd, cs, kernel, highSize, data.viewCount);
                            }
                        });

                    bloomTexture = passData.mipsUp[0];
                }
            }

            return bloomTexture;
        }

        #endregion

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

                return passData.logLut;
            }
        }

        #endregion

        // Grabs all active feature flags
        UberPostFeatureFlags GetUberFeatureFlags(bool isSceneView)
        {
            var flags = UberPostFeatureFlags.None;

            if (m_ChromaticAberration.IsActive() && m_ChromaticAberrationFS)
                flags |= UberPostFeatureFlags.ChromaticAberration;

            if (m_Vignette.IsActive() && m_VignetteFS)
                flags |= UberPostFeatureFlags.Vignette;

            if (m_LensDistortion.IsActive() && !isSceneView && m_LensDistortionFS)
                flags |= UberPostFeatureFlags.LensDistortion;

            if (m_EnableAlpha)
            {
                flags |= UberPostFeatureFlags.EnableAlpha;
            }

            return flags;
        }

        void PrepareLensDistortionParameters(UberPostPassData data, UberPostFeatureFlags flags)
        {
            if ((flags & UberPostFeatureFlags.LensDistortion) != UberPostFeatureFlags.LensDistortion)
                return;

            data.uberPostCS.EnableKeyword("LENS_DISTORTION");

            float amount = 1.6f * Mathf.Max(Mathf.Abs(m_LensDistortion.intensity.value * 100f), 1f);
            float theta = Mathf.Deg2Rad * Mathf.Min(160f, amount);
            float sigma = 2f * Mathf.Tan(theta * 0.5f);
            var center = m_LensDistortion.center.value * 2f - Vector2.one;
            data.lensDistortionParams1 = new Vector4(
                center.x,
                center.y,
                Mathf.Max(m_LensDistortion.xMultiplier.value, 1e-4f),
                Mathf.Max(m_LensDistortion.yMultiplier.value, 1e-4f)
            );
            data.lensDistortionParams2 = new Vector4(
                m_LensDistortion.intensity.value >= 0f ? theta : 1f / theta,
                sigma,
                1f / m_LensDistortion.scale.value,
                m_LensDistortion.intensity.value * 100f
            );
        }

        void PrepareChromaticAberrationParameters(UberPostPassData data, UberPostFeatureFlags flags)
        {
            if ((flags & UberPostFeatureFlags.ChromaticAberration) != UberPostFeatureFlags.ChromaticAberration)
                return;

            data.uberPostCS.EnableKeyword("CHROMATIC_ABERRATION");

            var spectralLut = m_ChromaticAberration.spectralLut.value;

            // If no spectral lut is set, use a pre-generated one
            if (spectralLut == null)
            {
                if (m_InternalSpectralLut == null)
                {
                    m_InternalSpectralLut = new Texture2D(3, 1, GraphicsFormat.R8G8B8A8_SRGB, TextureCreationFlags.None)
                    {
                        name = "Chromatic Aberration Spectral LUT",
                        filterMode = FilterMode.Bilinear,
                        wrapMode = TextureWrapMode.Clamp,
                        anisoLevel = 0,
                        hideFlags = HideFlags.DontSave
                    };

                    m_InternalSpectralLut.SetPixels(new[]
                    {
                        new Color(1f, 0f, 0f, 1f),
                        new Color(0f, 1f, 0f, 1f),
                        new Color(0f, 0f, 1f, 1f)
                    });

                    m_InternalSpectralLut.Apply();
                }

                spectralLut = m_InternalSpectralLut;
            }

            data.spectralLut = spectralLut;
            data.chromaticAberrationParameters = new Vector4(m_ChromaticAberration.intensity.value * 0.05f, m_ChromaticAberration.maxSamples, 0f, 0f);
        }

        void PrepareVignetteParameters(UberPostPassData data, UberPostFeatureFlags flags)
        {
            if ((flags & UberPostFeatureFlags.Vignette) != UberPostFeatureFlags.Vignette)
                return;

            data.uberPostCS.EnableKeyword("VIGNETTE");

            if (m_Vignette.mode.value == VignetteMode.Procedural)
            {
                float roundness = (1f - m_Vignette.roundness.value) * 6f + m_Vignette.roundness.value;
                data.vignetteParams1 = new Vector4(m_Vignette.center.value.x, m_Vignette.center.value.y, 0f, 0f);
                data.vignetteParams2 = new Vector4(m_Vignette.intensity.value * 3f, m_Vignette.smoothness.value * 5f, roundness, m_Vignette.rounded.value ? 1f : 0f);
                data.vignetteColor = m_Vignette.color.value;
                data.vignetteMask = Texture2D.blackTexture;
            }
            else // Masked
            {
                var color = m_Vignette.color.value;
                color.a = Mathf.Clamp01(m_Vignette.opacity.value);

                data.vignetteParams1 = new Vector4(0f, 0f, 1f, 0f);
                data.vignetteColor = color;
                data.vignetteMask = m_Vignette.mask.value;
            }
        }

        Vector4 GetBloomThresholdParams()
        {
            const float k_Softness = 0.5f;
            float lthresh = Mathf.GammaToLinearSpace(m_Bloom.threshold.value);
            float knee = lthresh * k_Softness + 1e-5f;
            return new Vector4(lthresh, lthresh - knee, knee * 2f, 0.25f / knee);
        }

        void PrepareUberBloomParameters(UberPostPassData data, HDCamera camera)
        {
            float intensity = Mathf.Pow(2f, m_Bloom.intensity.value) - 1f; // Makes intensity easier to control
            var tint = m_Bloom.tint.value.linear;
            var luma = ColorUtils.Luminance(tint);
            tint = luma > 0f ? tint * (1f / luma) : Color.white;

            var dirtTexture = m_Bloom.dirtTexture.value == null ? Texture2D.blackTexture : m_Bloom.dirtTexture.value;
            int dirtEnabled = m_Bloom.dirtTexture.value != null && m_Bloom.dirtIntensity.value > 0f ? 1 : 0;
            float dirtRatio = (float)dirtTexture.width / (float)dirtTexture.height;
            float screenRatio = (float)camera.actualWidth / (float)camera.actualHeight;
            var dirtTileOffset = new Vector4(1f, 1f, 0f, 0f);
            float dirtIntensity = m_Bloom.dirtIntensity.value * intensity;

            if (dirtRatio > screenRatio)
            {
                dirtTileOffset.x = screenRatio / dirtRatio;
                dirtTileOffset.z = (1f - dirtTileOffset.x) * 0.5f;
            }
            else if (screenRatio > dirtRatio)
            {
                dirtTileOffset.y = dirtRatio / screenRatio;
                dirtTileOffset.w = (1f - dirtTileOffset.y) * 0.5f;
            }

            data.bloomDirtTexture = dirtTexture;
            data.bloomParams = new Vector4(intensity, dirtIntensity, 1f, dirtEnabled);
            data.bloomTint = (Vector4)tint;
            data.bloomDirtTileOffset = dirtTileOffset;
            data.bloomThreshold = GetBloomThresholdParams();
            data.bloomBicubicParams = m_BloomBicubicParams;
        }

        void PrepareAlphaScaleParameters(UberPostPassData data, HDCamera camera)
        {
            if (m_EnableAlpha)
                data.alphaScaleBias = Compositor.CompositionManager.GetAlphaScaleAndBiasForCamera(camera);
            else
                data.alphaScaleBias = new Vector4(1.0f, 0.0f, 0.0f, 0.0f);
        }

        TextureHandle UberPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle logLut, TextureHandle bloomTexture, TextureHandle source)
        {
            bool isSceneView = hdCamera.camera.cameraType == CameraType.SceneView;
            using (var builder = renderGraph.AddRenderPass<UberPostPassData>("Uber Post", out var passData, ProfilingSampler.Get(HDProfileId.UberPost)))
            {
                TextureHandle dest = GetPostprocessOutputHandle(renderGraph, "Uber Post Destination");

                // Feature flags are passed to all effects and it's their responsibility to check
                // if they are used or not so they can set default values if needed
                passData.uberPostCS = m_Resources.shaders.uberPostCS;
                passData.uberPostCS.shaderKeywords = null;
                var featureFlags = GetUberFeatureFlags(isSceneView);
                passData.uberPostKernel = passData.uberPostCS.FindKernel("Uber");
                if (m_EnableAlpha)
                {
                    passData.uberPostCS.EnableKeyword("ENABLE_ALPHA");
                }

                passData.outputColorLog = m_HDInstance.m_CurrentDebugDisplaySettings.data.fullScreenDebugMode == FullScreenDebugMode.ColorLog;
                passData.width = hdCamera.actualWidth;
                passData.height = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;

                // Color grading
                // This should be EV100 instead of EV but given that EV100(0) isn't equal to 1, it means
                // we can't use 0 as the default neutral value which would be confusing to users
                float postExposureLinear = Mathf.Pow(2f, m_ColorAdjustments.postExposure.value);
                passData.logLutSettings = new Vector4(1f / m_LutSize, m_LutSize - 1f, postExposureLinear, 0f);

                // Setup the rest of the effects
                PrepareLensDistortionParameters(passData, featureFlags);
                PrepareChromaticAberrationParameters(passData, featureFlags);
                PrepareVignetteParameters(passData, featureFlags);
                PrepareUberBloomParameters(passData, hdCamera);
                PrepareAlphaScaleParameters(passData, hdCamera);

                passData.source = builder.ReadTexture(source);
                passData.bloomTexture = builder.ReadTexture(bloomTexture);
                passData.logLut = builder.ReadTexture(logLut);
                passData.destination = builder.WriteTexture(dest);

                builder.SetRenderFunc(
                    (UberPostPassData data, RenderGraphContext ctx) =>
                    {
                        // Color grading
                        ctx.cmd.SetComputeTextureParam(data.uberPostCS, data.uberPostKernel, HDShaderIDs._LogLut3D, data.logLut);
                        ctx.cmd.SetComputeVectorParam(data.uberPostCS, HDShaderIDs._LogLut3D_Params, data.logLutSettings);

                        // Lens distortion
                        ctx.cmd.SetComputeVectorParam(data.uberPostCS, HDShaderIDs._DistortionParams1, data.lensDistortionParams1);
                        ctx.cmd.SetComputeVectorParam(data.uberPostCS, HDShaderIDs._DistortionParams2, data.lensDistortionParams2);

                        // Chromatic aberration
                        ctx.cmd.SetComputeTextureParam(data.uberPostCS, data.uberPostKernel, HDShaderIDs._ChromaSpectralLut, data.spectralLut);
                        ctx.cmd.SetComputeVectorParam(data.uberPostCS, HDShaderIDs._ChromaParams, data.chromaticAberrationParameters);

                        // Vignette
                        ctx.cmd.SetComputeVectorParam(data.uberPostCS, HDShaderIDs._VignetteParams1, data.vignetteParams1);
                        ctx.cmd.SetComputeVectorParam(data.uberPostCS, HDShaderIDs._VignetteParams2, data.vignetteParams2);
                        ctx.cmd.SetComputeVectorParam(data.uberPostCS, HDShaderIDs._VignetteColor, data.vignetteColor);
                        ctx.cmd.SetComputeTextureParam(data.uberPostCS, data.uberPostKernel, HDShaderIDs._VignetteMask, data.vignetteMask);

                        // Bloom
                        ctx.cmd.SetComputeTextureParam(data.uberPostCS, data.uberPostKernel, HDShaderIDs._BloomTexture, data.bloomTexture);
                        ctx.cmd.SetComputeTextureParam(data.uberPostCS, data.uberPostKernel, HDShaderIDs._BloomDirtTexture, data.bloomDirtTexture);
                        ctx.cmd.SetComputeVectorParam(data.uberPostCS, HDShaderIDs._BloomParams, data.bloomParams);
                        ctx.cmd.SetComputeVectorParam(data.uberPostCS, HDShaderIDs._BloomTint, data.bloomTint);
                        ctx.cmd.SetComputeVectorParam(data.uberPostCS, HDShaderIDs._BloomBicubicParams, data.bloomBicubicParams);
                        ctx.cmd.SetComputeVectorParam(data.uberPostCS, HDShaderIDs._BloomDirtScaleOffset, data.bloomDirtTileOffset);
                        ctx.cmd.SetComputeVectorParam(data.uberPostCS, HDShaderIDs._BloomThreshold, data.bloomThreshold);

                        // Alpha scale and bias (only used when alpha is enabled)
                        ctx.cmd.SetComputeVectorParam(data.uberPostCS, HDShaderIDs._AlphaScaleBias, data.alphaScaleBias);

                        // Dispatch uber post
                        ctx.cmd.SetComputeVectorParam(data.uberPostCS, "_DebugFlags", new Vector4(data.outputColorLog ? 1 : 0, 0, 0, 0));
                        ctx.cmd.SetComputeTextureParam(data.uberPostCS, data.uberPostKernel, HDShaderIDs._InputTexture, data.source);
                        ctx.cmd.SetComputeTextureParam(data.uberPostCS, data.uberPostKernel, HDShaderIDs._OutputTexture, data.destination);
                        ctx.cmd.DispatchCompute(data.uberPostCS, data.uberPostKernel, (data.width + 7) / 8, (data.height + 7) / 8, data.viewCount);
                    });

                source = passData.destination;
            }

            return source;
        }

        TextureHandle FXAAPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source)
        {
            if (hdCamera.DynResRequest.enabled &&     // Dynamic resolution is on.
                hdCamera.antialiasing == HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing &&
                m_AntialiasingFS)
            {
                using (var builder = renderGraph.AddRenderPass<FXAAData>("FXAA", out var passData, ProfilingSampler.Get(HDProfileId.FXAA)))
                {
                    passData.fxaaCS = m_Resources.shaders.FXAACS;
                    passData.fxaaKernel = passData.fxaaCS.FindKernel("FXAA");
                    passData.width = hdCamera.actualWidth;
                    passData.height = hdCamera.actualHeight;
                    passData.viewCount = hdCamera.viewCount;

                    passData.source = builder.ReadTexture(source);
                    passData.destination = builder.WriteTexture(GetPostprocessOutputHandle(renderGraph, "FXAA Destination"));;

                    builder.SetRenderFunc(
                        (FXAAData data, RenderGraphContext ctx) =>
                        {
                            ctx.cmd.SetComputeTextureParam(data.fxaaCS, data.fxaaKernel, HDShaderIDs._InputTexture, data.source);
                            ctx.cmd.SetComputeTextureParam(data.fxaaCS, data.fxaaKernel, HDShaderIDs._OutputTexture, data.destination);
                            ctx.cmd.DispatchCompute(data.fxaaCS, data.fxaaKernel, (data.width + 7) / 8, (data.height + 7) / 8, data.viewCount);
                        });

                    source = passData.destination;
                }
            }

            return source;
        }

        TextureHandle ContrastAdaptiveSharpeningPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle source)
        {
            if (hdCamera.DynResRequest.enabled &&
                hdCamera.DynResRequest.filter == DynamicResUpscaleFilter.ContrastAdaptiveSharpen)
            {
                using (var builder = renderGraph.AddRenderPass<CASData>("Contrast Adaptive Sharpen", out var passData, ProfilingSampler.Get(HDProfileId.ContrastAdaptiveSharpen)))
                {
                    passData.casCS = m_Resources.shaders.contrastAdaptiveSharpenCS;
                    passData.initKernel = passData.casCS.FindKernel("KInitialize");
                    passData.mainKernel = passData.casCS.FindKernel("KMain");
                    passData.viewCount = hdCamera.viewCount;
                    passData.inputWidth = hdCamera.actualWidth;
                    passData.inputHeight = hdCamera.actualHeight;
                    passData.outputWidth = Mathf.RoundToInt(hdCamera.finalViewport.width);
                    passData.outputHeight = Mathf.RoundToInt(hdCamera.finalViewport.height);
                    passData.source = builder.ReadTexture(source);
                    passData.destination = builder.WriteTexture(GetPostprocessUpsampledOutputHandle(renderGraph, "Contrast Adaptive Sharpen Destination"));;
                    passData.casParametersBuffer = builder.CreateTransientComputeBuffer(new ComputeBufferDesc(2, sizeof(uint) * 4) { name = "Cas Parameters" });

                    builder.SetRenderFunc(
                        (CASData data, RenderGraphContext ctx) =>
                        {
                            ctx.cmd.SetComputeFloatParam(data.casCS, HDShaderIDs._Sharpness, 1);
                            ctx.cmd.SetComputeTextureParam(data.casCS, data.mainKernel, HDShaderIDs._InputTexture, data.source);
                            ctx.cmd.SetComputeVectorParam(data.casCS, HDShaderIDs._InputTextureDimensions, new Vector4(data.inputWidth, data.inputHeight));
                            ctx.cmd.SetComputeTextureParam(data.casCS, data.mainKernel, HDShaderIDs._OutputTexture, data.destination);
                            ctx.cmd.SetComputeVectorParam(data.casCS, HDShaderIDs._OutputTextureDimensions, new Vector4(data.outputWidth, data.outputHeight));
                            ctx.cmd.SetComputeBufferParam(data.casCS, data.initKernel, "CasParameters", data.casParametersBuffer);
                            ctx.cmd.SetComputeBufferParam(data.casCS, data.mainKernel, "CasParameters", data.casParametersBuffer);
                            ctx.cmd.DispatchCompute(data.casCS, data.initKernel, 1, 1, 1);

                            int dispatchX = HDUtils.DivRoundUp(data.outputWidth, 16);
                            int dispatchY = HDUtils.DivRoundUp(data.outputHeight, 16);

                            ctx.cmd.DispatchCompute(data.casCS, data.mainKernel, dispatchX, dispatchY, data.viewCount);
                        });

                    source = passData.destination;
                }
            }
            return source;
        }

        #region Final Pass

        class FinalPassData
        {
            public bool postProcessEnabled;
            public Material finalPassMaterial;
            public HDCamera hdCamera;
            public BlueNoise blueNoise;
            public bool flipY;
            public System.Random random;
            public bool useFXAA;
            public bool enableAlpha;
            public bool keepAlpha;
            public bool dynamicResIsOn;
            public DynamicResUpscaleFilter dynamicResFilter;

            public bool filmGrainEnabled;
            public Texture filmGrainTexture;
            public float filmGrainIntensity;
            public float filmGrainResponse;

            public bool ditheringEnabled;

            public TextureHandle source;
            public TextureHandle afterPostProcessTexture;
            public TextureHandle alphaTexture;
            public TextureHandle destination;
        }

        void FinalPass(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle afterPostProcessTexture, TextureHandle alphaTexture, TextureHandle finalRT, TextureHandle source, BlueNoise blueNoise, bool flipY)
        {
            using (var builder = renderGraph.AddRenderPass<FinalPassData>("Final Pass", out var passData, ProfilingSampler.Get(HDProfileId.FinalPost)))
            {
                // General
                passData.postProcessEnabled = m_PostProcessEnabled;
                passData.finalPassMaterial = m_FinalPassMaterial;
                passData.hdCamera = hdCamera;
                passData.blueNoise = blueNoise;
                passData.flipY = flipY;
                passData.random = m_Random;
                passData.enableAlpha = m_EnableAlpha;
                passData.keepAlpha = m_KeepAlpha;
                passData.dynamicResIsOn = hdCamera.canDoDynamicResolution && hdCamera.DynResRequest.enabled;
                passData.dynamicResFilter = hdCamera.DynResRequest.filter;
                passData.useFXAA = hdCamera.antialiasing == HDAdditionalCameraData.AntialiasingMode.FastApproximateAntialiasing && !passData.dynamicResIsOn && m_AntialiasingFS;

                // Film Grain
                passData.filmGrainEnabled = m_FilmGrain.IsActive() && m_FilmGrainFS;
                if (m_FilmGrain.type.value != FilmGrainLookup.Custom)
                    passData.filmGrainTexture = m_Resources.textures.filmGrainTex[(int)m_FilmGrain.type.value];
                else
                    passData.filmGrainTexture = m_FilmGrain.texture.value;
                passData.filmGrainIntensity = m_FilmGrain.intensity.value;
                passData.filmGrainResponse = m_FilmGrain.response.value;

                // Dithering
                passData.ditheringEnabled = hdCamera.dithering && m_DitheringFS;

                passData.source = builder.ReadTexture(source);
                passData.afterPostProcessTexture = builder.ReadTexture(afterPostProcessTexture);
                passData.alphaTexture = builder.ReadTexture(alphaTexture);
                passData.destination = builder.WriteTexture(finalRT);

                builder.SetRenderFunc(
                    (FinalPassData data, RenderGraphContext ctx) =>
                    {
                        // Final pass has to be done in a pixel shader as it will be the one writing straight
                        // to the backbuffer eventually
                        Material finalPassMaterial = data.finalPassMaterial;

                        finalPassMaterial.shaderKeywords = null;
                        finalPassMaterial.SetTexture(HDShaderIDs._InputTexture, data.source);

                        if (data.dynamicResIsOn)
                        {
                            switch (data.dynamicResFilter)
                            {
                                case DynamicResUpscaleFilter.Bilinear:
                                    finalPassMaterial.EnableKeyword("BILINEAR");
                                    break;
                                case DynamicResUpscaleFilter.CatmullRom:
                                    finalPassMaterial.EnableKeyword("CATMULL_ROM_4");
                                    break;
                                case DynamicResUpscaleFilter.Lanczos:
                                    finalPassMaterial.EnableKeyword("LANCZOS");
                                    break;
                                case DynamicResUpscaleFilter.ContrastAdaptiveSharpen:
                                    finalPassMaterial.EnableKeyword("CONTRASTADAPTIVESHARPEN");
                                    break;
                            }
                        }

                        if (data.postProcessEnabled)
                        {
                            if (data.useFXAA)
                                finalPassMaterial.EnableKeyword("FXAA");

                            if (data.filmGrainEnabled)
                            {
                                if (data.filmGrainTexture != null) // Fail safe if the resources asset breaks :/
                                {
#if HDRP_DEBUG_STATIC_POSTFX
                                    float offsetX = 0;
                                    float offsetY = 0;
#else
                                    float offsetX = (float)(data.random.NextDouble());
                                    float offsetY = (float)(data.random.NextDouble());
#endif

                                    finalPassMaterial.EnableKeyword("GRAIN");
                                    finalPassMaterial.SetTexture(HDShaderIDs._GrainTexture, data.filmGrainTexture);
                                    finalPassMaterial.SetVector(HDShaderIDs._GrainParams, new Vector2(data.filmGrainIntensity * 4f, data.filmGrainResponse));

                                    float uvScaleX = data.hdCamera.actualWidth / (float)data.filmGrainTexture.width;
                                    float uvScaleY = data.hdCamera.actualHeight / (float)data.filmGrainTexture.height;
                                    float scaledOffsetX = offsetX * uvScaleX;
                                    float scaledOffsetY = offsetY * uvScaleY;

                                    finalPassMaterial.SetVector(HDShaderIDs._GrainTextureParams, new Vector4(uvScaleX, uvScaleY, offsetX, offsetY));
                                }
                            }

                            if (data.ditheringEnabled)
                            {
                                var blueNoiseTexture = data.blueNoise.textureArray16L;

#if HDRP_DEBUG_STATIC_POSTFX
                                int textureId = 0;
#else
                                int textureId = (int)data.hdCamera.GetCameraFrameCount() % blueNoiseTexture.depth;
#endif

                                finalPassMaterial.EnableKeyword("DITHER");
                                finalPassMaterial.SetTexture(HDShaderIDs._BlueNoiseTexture, blueNoiseTexture);
                                finalPassMaterial.SetVector(HDShaderIDs._DitherParams, new Vector3(data.hdCamera.actualWidth / blueNoiseTexture.width,
                                    data.hdCamera.actualHeight / blueNoiseTexture.height, textureId));
                            }
                        }

                        RTHandle alphaRTHandle = data.alphaTexture; // Need explicit cast otherwise we get a wrong implicit conversion to RenderTexture :/
                        finalPassMaterial.SetTexture(HDShaderIDs._AlphaTexture, alphaRTHandle);
                        finalPassMaterial.SetFloat(HDShaderIDs._KeepAlpha, data.keepAlpha ? 1.0f : 0.0f);

                        if (data.enableAlpha)
                            finalPassMaterial.EnableKeyword("ENABLE_ALPHA");
                        else
                            finalPassMaterial.DisableKeyword("ENABLE_ALPHA");

                        finalPassMaterial.SetVector(HDShaderIDs._UVTransform,
                            data.flipY
                            ? new Vector4(1.0f, -1.0f, 0.0f, 1.0f)
                            : new Vector4(1.0f, 1.0f, 0.0f, 0.0f)
                        );

                        finalPassMaterial.SetVector(HDShaderIDs._ViewPortSize,
                            new Vector4(data.hdCamera.finalViewport.width, data.hdCamera.finalViewport.height, 1.0f / data.hdCamera.finalViewport.width, 1.0f / data.hdCamera.finalViewport.height));

                        // Blit to backbuffer
                        Rect backBufferRect = data.hdCamera.finalViewport;

                        // When post process is not the final pass, we render at (0,0) so that subsequent rendering does not have to bother about viewports.
                        // Final viewport is handled in the final blit in this case
                        if (!HDUtils.PostProcessIsFinalPass(data.hdCamera))
                        {
                            backBufferRect.x = backBufferRect.y = 0;
                        }

                        if (data.hdCamera.frameSettings.IsEnabled(FrameSettingsField.AfterPostprocess))
                        {
                            finalPassMaterial.EnableKeyword("APPLY_AFTER_POST");
                            finalPassMaterial.SetTexture(HDShaderIDs._AfterPostProcessTexture, data.afterPostProcessTexture);
                        }
                        else
                        {
                            finalPassMaterial.SetTexture(HDShaderIDs._AfterPostProcessTexture, TextureXR.GetBlackTexture());
                        }

                        HDUtils.DrawFullScreen(ctx.cmd, backBufferRect, finalPassMaterial, data.destination);
                    });
            }
        }

        #endregion

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
    }
}
