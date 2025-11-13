using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices;  // AggressiveInlining

namespace UnityEngine.Rendering.Universal
{
    internal sealed class PostProcess : IDisposable
    {
        // Passes
        StopNanPostProcessPass m_StopNanPostProcessPass;
        SmaaPostProcessPass m_SmaaPostProcessPass;
        DepthOfFieldGaussianPostProcessPass m_DepthOfFieldGaussianPass;
        DepthOfFieldBokehPostProcessPass m_DepthOfFieldBokehPass;
        UpscalerPostProcessPass m_UpscalerPostProcessPass;
        StpPostProcessPass m_StpPostProcessPass;
        TemporalAntiAliasingPostProcessPass m_TemporalAntiAliasingPass;
        MotionBlurPostProcessPass m_MotionBlurPass;
        PaniniProjectionPostProcessPass m_PaniniProjectionPass;
        BloomPostProcessPass m_BloomPass;
        LensFlareScreenSpacePostProcessPass m_LensFlareScreenSpacePass;
        LensFlareDataDrivenPostProcessPass m_LensFlareDataDrivenPass;
        UberPostProcessPass m_UberPass;

        // Final Passes
        ScalingSetupPostProcessPass m_ScalingSetupFinalPostProcessPass;
        Fsr1UpscalePostProcessPass m_Fsr1UpscaleFinalPostProcessPass;
        FinalPostProcessPass m_FinalPostProcessPass;

        PostProcessData m_Resources;

        int m_DitheringTextureIndex;    // 8-bit dithering

        /// <summary>
        /// Creates a new <c>PostProcessPass</c> instance.
        /// </summary>
        /// <param name="postProcessResourceAssetData">The <c>PostProcessData</c> resources to use.</param>
        /// <param name="requestPostProColorFormat">Requested <c>GraphicsFormat</c> for postprocess rendering.</param>
        /// <seealso cref="RenderPassEvent"/>
        /// <seealso cref="PostProcessData"/>
        /// <seealso cref="PostProcessParams"/>
        /// <seealso cref="GraphicsFormat"/>
        public PostProcess(PostProcessData postProcessResourceAssetData)
        {
            Assertions.Assert.IsNotNull(postProcessResourceAssetData, "PostProcessData and resources cannot be null.");
            m_Resources = postProcessResourceAssetData;

            m_StopNanPostProcessPass   = new StopNanPostProcessPass(m_Resources.shaders.stopNanPS);
            m_SmaaPostProcessPass      = new SmaaPostProcessPass(m_Resources.shaders.subpixelMorphologicalAntialiasingPS, m_Resources.textures.smaaAreaTex, m_Resources.textures.smaaSearchTex);
            m_DepthOfFieldGaussianPass = new DepthOfFieldGaussianPostProcessPass(m_Resources.shaders.gaussianDepthOfFieldPS);
            m_DepthOfFieldBokehPass    = new DepthOfFieldBokehPostProcessPass(m_Resources.shaders.bokehDepthOfFieldPS);
            m_UpscalerPostProcessPass  = new UpscalerPostProcessPass(m_Resources.textures.blueNoise16LTex);
            m_StpPostProcessPass       = new StpPostProcessPass(m_Resources.textures.blueNoise16LTex);
            m_TemporalAntiAliasingPass = new TemporalAntiAliasingPostProcessPass(m_Resources.shaders.temporalAntialiasingPS);
            m_MotionBlurPass           = new MotionBlurPostProcessPass(m_Resources.shaders.cameraMotionBlurPS);
            m_PaniniProjectionPass     = new PaniniProjectionPostProcessPass(m_Resources.shaders.paniniProjectionPS);
            m_BloomPass                = new BloomPostProcessPass(m_Resources.shaders.bloomPS);
            m_LensFlareScreenSpacePass = new LensFlareScreenSpacePostProcessPass(m_Resources.shaders.LensFlareScreenSpacePS);
            m_LensFlareDataDrivenPass  = new LensFlareDataDrivenPostProcessPass(m_Resources.shaders.LensFlareDataDrivenPS);
            m_UberPass                 = new UberPostProcessPass(m_Resources.shaders.uberPostPS, m_Resources.textures.filmGrainTex);

            // Final post processing.
            m_ScalingSetupFinalPostProcessPass = new ScalingSetupPostProcessPass(m_Resources.shaders.scalingSetupPS);
            m_Fsr1UpscaleFinalPostProcessPass = new Fsr1UpscalePostProcessPass(m_Resources.shaders.easuPS);
            m_FinalPostProcessPass             = new FinalPostProcessPass(m_Resources.shaders.finalPostPassPS, m_Resources.textures.filmGrainTex);
        }

        /// <summary>
        /// Disposes used resources.
        /// </summary>
        public void Dispose()
        {
            m_FinalPostProcessPass?.Dispose();
            m_Fsr1UpscaleFinalPostProcessPass?.Dispose();
            m_ScalingSetupFinalPostProcessPass?.Dispose();

            m_UberPass?.Dispose();
            m_LensFlareDataDrivenPass?.Dispose();
            m_LensFlareScreenSpacePass?.Dispose();
            m_BloomPass?.Dispose();
            m_PaniniProjectionPass?.Dispose();
            m_MotionBlurPass?.Dispose();
            m_TemporalAntiAliasingPass?.Dispose();
            m_StpPostProcessPass?.Dispose();
            m_UpscalerPostProcessPass.Dispose();
            m_DepthOfFieldBokehPass?.Dispose();
            m_DepthOfFieldGaussianPass?.Dispose();
            m_SmaaPostProcessPass?.Dispose();
            m_StopNanPostProcessPass?.Dispose();
        }

        // Some Android devices do not support sRGB backbuffer
        // We need to do the conversion manually on those
        // Also if HDR output is active
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool RequireSRGBConversionBlitToBackBuffer(UniversalCameraData cameraData, bool enableColorEncodingIfNeeded)
        {
            return cameraData.requireSrgbConversion && enableColorEncodingIfNeeded;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        int GetNextDitherIndex()
        {
#if LWRP_DEBUG_STATIC_POSTFX // Used by QA for automated testing
            return 0;
#else
            if (++m_DitheringTextureIndex >= m_Resources.textures.blueNoise16LTex.Length)
                m_DitheringTextureIndex = 0;
            return m_DitheringTextureIndex;
#endif
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        Texture2D GetNextDitherTexture()
        {
            return m_Resources.textures.blueNoise16LTex[GetNextDitherIndex()];
        }

        static void UpdateGlobalDebugHandlerPass(RenderGraph renderGraph, UniversalCameraData cameraData, bool isFinalPass)
        {
            // NOTE: Debug handling injects a global state render pass.
            DebugHandler debugHandler = ScriptableRenderPass.GetActiveDebugHandler(cameraData);
            bool resolveToDebugScreen = debugHandler != null && debugHandler.WriteToDebugScreenTexture(cameraData.resolveFinalTarget);
            debugHandler?.UpdateShaderGlobalPropertiesForFinalValidationPass(renderGraph, cameraData, isFinalPass && !resolveToDebugScreen);
        }

        // If hasFinalPass == true, Film Grain and Dithering are setup in the final pass, otherwise they are setup in this pass.
        public void RenderPostProcessing(RenderGraph renderGraph, ContextContainer frameData, bool hasFinalPass, bool enableColorEncodingIfNeeded)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            // NOTE: Debug handling injects a global state render pass.
            UpdateGlobalDebugHandlerPass(renderGraph, cameraData, !hasFinalPass);

            // We blit back and forth without msaa until the last blit.
            // `resourceData.cameraColor` is the current post-process input for each pass.
            var colorSourceDesc = resourceData.cameraColor.GetDescriptor(renderGraph);

            // Optional NaN killer before post-processing kicks in
            // stopNaN may be null on Adreno 3xx. It doesn't support full shader level 3.5, but SystemInfo.graphicsShaderLevel is 35.
            m_StopNanPostProcessPass.RecordRenderGraph(renderGraph, frameData);

            // Subpixel Morphological Anti Aliasing
            m_SmaaPostProcessPass.RecordRenderGraph(renderGraph, frameData);

            // Depth of Field
            // Adreno 3xx SystemInfo.graphicsShaderLevel is 35, but instancing support is disabled due to buggy drivers.
            // DOF shader uses #pragma target 3.5 which adds requirement for instancing support, thus marking the shader unsupported on those devices.
            m_DepthOfFieldGaussianPass.RecordRenderGraph(renderGraph, frameData);
            m_DepthOfFieldBokehPass.RecordRenderGraph(renderGraph, frameData);

            // Temporal Anti Aliasing / Upscaling
#if ENABLE_UPSCALER_FRAMEWORK
            m_UpscalerPostProcessPass.RecordRenderGraph(renderGraph, frameData);
#endif
            m_StpPostProcessPass.RecordRenderGraph(renderGraph, frameData);
            m_TemporalAntiAliasingPass.RecordRenderGraph(renderGraph, frameData);

            m_MotionBlurPass.RecordRenderGraph(renderGraph, frameData);
            m_PaniniProjectionPass.RecordRenderGraph(renderGraph, frameData);

            // Bloom & Screen Space Lens Flares
            {
                var volumeStack = VolumeManager.instance.stack;
                var bloom = volumeStack.GetComponent<Bloom>();
                var lensFlareScreenSpace = volumeStack.GetComponent<ScreenSpaceLensFlare>();

                // NOTE: Even if bloom is not active we might need to run it for the texture if the lensFlareScreenSpace pass is active.
                if (!bloom.IsActive() && LensFlareScreenSpacePostProcessPass.IsActive(volumeStack, frameData))
                    bloom.intensity = lensFlareScreenSpace.intensity;   // Set Bloom Active to true.

                m_BloomPass.RecordRenderGraph(renderGraph, frameData);

                // We need to take into account how many valid mips the bloom pass produced.
                var mipPyramid = m_BloomPass.mipPyramid;
                int bloomMipCount = mipPyramid.mipCount;
                int bloomMipMax = Mathf.Clamp(bloomMipCount - 1, 0, bloom.maxIterations.value / 2);
                int bloomMipIndex = Mathf.Clamp(lensFlareScreenSpace.bloomMip.value, 0, bloomMipMax);

                var prevCameraColor = resourceData.cameraColor;
                resourceData.cameraColor = mipPyramid.GetResultMip(bloomMipIndex);;

                m_LensFlareScreenSpacePass.Setup(colorSourceDesc.width, colorSourceDesc.height, bloomMipIndex);
                m_LensFlareScreenSpacePass.RecordRenderGraph(renderGraph, frameData);

                resourceData.cameraColor = prevCameraColor;
            }

            // Lens Flares are procedurally generated and blended to the destination texture.
            m_LensFlareDataDrivenPass.RecordRenderGraph(renderGraph, frameData);

            // Uber post
            var ditherTexture = cameraData.isDitheringEnabled ? GetNextDitherTexture() : null;
            var hdrOperations = HDROutputUtils.Operation.None;
            var applySrgbEncoding = RequireSRGBConversionBlitToBackBuffer(cameraData, enableColorEncodingIfNeeded);

            bool requireHDROutput = PostProcessUtils.RequireHDROutput(cameraData);
            if (requireHDROutput)
            {
                // Color space conversion is already applied through color grading, do encoding if uber post is the last pass
                // Otherwise encoding will happen in the final post process pass or the final blit pass
                hdrOperations = !hasFinalPass && enableColorEncodingIfNeeded ? HDROutputUtils.Operation.ColorEncoding : HDROutputUtils.Operation.None;
            }

            bool renderOverlayUI = requireHDROutput && enableColorEncodingIfNeeded;
            m_UberPass.Setup(ditherTexture, hdrOperations, applySrgbEncoding, !hasFinalPass, renderOverlayUI);
            m_UberPass.RecordRenderGraph(renderGraph, frameData);
        }

#region FinalPass
        public void RenderFinalPostProcessing(RenderGraph renderGraph, ContextContainer frameData, bool enableColorEncodingIfNeeded)
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            // NOTE: Debug handling injects a global state render pass.
            UpdateGlobalDebugHandlerPass(renderGraph, cameraData, true);

            var resourceData = frameData.Get<UniversalResourceData>();
            var sourceDesc = renderGraph.GetTextureDesc(resourceData.cameraColor);

            HDROutputUtils.Operation hdrOperations = HDROutputUtils.Operation.None;
            bool requireHDROutput = PostProcessUtils.RequireHDROutput(cameraData);
            if (requireHDROutput)
            {
                // Final post process pass always does color encoding
                hdrOperations = enableColorEncodingIfNeeded ? HDROutputUtils.Operation.ColorEncoding : HDROutputUtils.Operation.None;
                // If the color space conversion wasn't applied by the uber pass, do it here
                if (!cameraData.postProcessEnabled)
                    hdrOperations |= HDROutputUtils.Operation.ColorConversion;
            }

            FinalPostProcessPass.FilteringOperation filteringOperation = FinalPostProcessPass.FilteringOperation.Linear;

            // Reuse RCAS pass as an optional standalone post sharpening pass for TAA.
            // This avoids the cost of EASU and is available for other upscaling options.
            // If FSR is enabled then FSR settings override the TAA settings and we perform RCAS only once.
            // If STP is enabled, then TAA sharpening has already been performed inside STP.
            if(PostProcessUtils.IsTaaSharpeningEnabled(cameraData))
                filteringOperation = FinalPostProcessPass.FilteringOperation.TaaSharpening;

            bool applyFxaa = PostProcessUtils.IsFxaaEnabled(cameraData);

            if (cameraData.imageScalingMode != ImageScalingMode.None)
            {
                // When FXAA is enabled in scaled renders, we execute it in a separate blit since it's not designed to be used in
                // situations where the input and output resolutions do not match.
                // When FSR is active, we always need an additional pass since it has a very particular color encoding requirement.

                // NOTE: An ideal implementation could inline this color conversion logic into the UberPost pass, but the current code structure would make
                //       this process very complex. Specifically, we'd need to guarantee that the uber post output is always written to a UNORM format render
                //       target in order to preserve the precision of specially encoded color data.
                bool isSetupRequired = (applyFxaa || PostProcessUtils.IsFsrEnabled(cameraData));

                // When FXAA is needed while scaling is active, we must perform it before the scaling takes place.
                if (isSetupRequired)
                {
                    m_ScalingSetupFinalPostProcessPass.Setup(hdrOperations);
                    m_ScalingSetupFinalPostProcessPass.RecordRenderGraph(renderGraph, frameData);

                    // Indicate that we no longer need to perform FXAA in the final pass since it was already perfomed here.
                    applyFxaa = false;
                }

                // Upscaling (and downscaling)

                var upscaledDesc = sourceDesc;
                upscaledDesc.width = cameraData.pixelWidth;
                upscaledDesc.height = cameraData.pixelHeight;
                // NOTE: upscaledDesc.format != scalingSetupDesc.format (in resourceData.cameraColor)

                switch (cameraData.imageScalingMode)
                {
                    case ImageScalingMode.Upscaling:
                    {
                        switch (cameraData.upscalingFilter)
                        {
                            case ImageUpscalingFilter.Point:
                            {
                                // TAA post sharpening is an RCAS pass, avoid overriding it with point sampling.
                                if (filteringOperation != FinalPostProcessPass.FilteringOperation.TaaSharpening)
                                    filteringOperation = FinalPostProcessPass.FilteringOperation.Point;
                                break;
                            }
                            case ImageUpscalingFilter.Linear:
                            {
                                break;
                            }
                            case ImageUpscalingFilter.FSR:
                            {
                                m_Fsr1UpscaleFinalPostProcessPass.Setup(upscaledDesc);
                                m_Fsr1UpscaleFinalPostProcessPass.RecordRenderGraph(renderGraph, frameData);
                                filteringOperation = FinalPostProcessPass.FilteringOperation.FsrSharpening;
                                break;
                            }
                        }
                        break;
                    }
                    case ImageScalingMode.Downscaling:
                    {
                        // In the downscaling case, we don't perform any sort of filter override logic since we always want linear filtering
                        // and it's already the default option in the shader.

                        // Also disable TAA post sharpening pass when downscaling.
                        filteringOperation = FinalPostProcessPass.FilteringOperation.Linear;
                        break;
                    }
                }
            }

            var ditherTexture = cameraData.isDitheringEnabled ? GetNextDitherTexture() : null;
            bool applySrgbEncoding = RequireSRGBConversionBlitToBackBuffer(cameraData, enableColorEncodingIfNeeded);
            bool renderOverlayUI = requireHDROutput && enableColorEncodingIfNeeded && cameraData.rendersOverlayUI;
            m_FinalPostProcessPass.Setup(ditherTexture, filteringOperation, hdrOperations, applySrgbEncoding, applyFxaa, renderOverlayUI);
            m_FinalPostProcessPass.RecordRenderGraph(renderGraph, frameData);
        }
#endregion
    }
}
