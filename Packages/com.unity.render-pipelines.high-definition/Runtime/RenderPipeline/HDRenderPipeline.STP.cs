using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.Assertions;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        bool GrabStpFeedbackHistoryTextures(HDCamera camera, out RTHandle previous, out RTHandle next)
        {
            return GrabSizedPostProcessHistoryTextures(camera, HDCameraFrameHistoryType.StpFeedback, "STP Feedback", GraphicsFormat.A2B10G10R10_UNormPack32, m_AfterDynamicResUpscaleRes, out previous, out next);
        }

        class StpSetupCommonConstantsData
        {
            public StpUtils.ConstantParams constants;
        }

        class STPPrepassData
        {
            public ComputeShader cs;
            public int kernelIndex;
            public int viewCount;

            public StpUtils.ConstantParams constants;

            public TextureHandle inputColor;
            public TextureHandle inputDepth;
            public TextureHandle inputMotion;
            public TextureHandle inputStencil;
            public TextureHandle priorLuma;
            public TextureHandle priorDepthMotion;
            public TextureHandle priorFeedback;

            public TextureHandle intermediateColor;
            public TextureHandle depthMotion;
            public TextureHandle luma;
        }

        class STPTaaData
        {
            public ComputeShader cs;
            public int kernelIndex;
            public int viewCount;
            public bool quality;

            public StpUtils.ConstantParams constants;

            public TextureHandle intermediateColor;
            public TextureHandle depthMotion;
            public TextureHandle luma;
            public TextureHandle priorFeedback;

            public TextureHandle feedback;
            public TextureHandle color;
        }

        class STPCleanerData
        {
            public ComputeShader cs;
            public int kernelIndex;
            public int viewCount;

            public StpUtils.ConstantParams constants;

            public TextureHandle feedback;

            public TextureHandle color;
        }

        TextureHandle DoStpPasses(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle inputColor, TextureHandle inputDepth, TextureHandle inputMotion, TextureHandle inputStencil, BlueNoise blueNoise)
        {
            HDCamera.StpTextures curTextures;
            HDCamera.StpTextures prevTextures;
            hdCamera.GetStpTextures(out curTextures, out prevTextures);

            bool hasValidHistoryTextures = prevTextures != null;

            int curFrameIndex = hdCamera.taaFrameIndex & 1;
            int prevFrameIndex = curFrameIndex ^ 1;

            RTHandle nextDepthMotion = curTextures.depthMotionTextures[curFrameIndex];
            RTHandle prevDepthMotion = hasValidHistoryTextures ? prevTextures.depthMotionTextures[prevFrameIndex] : curTextures.depthMotionTextures[prevFrameIndex];

            // The dimensions of the history textures should always match the current viewport size exactly.
            Debug.Assert((nextDepthMotion.rt.width == postProcessViewportSize.x) && (nextDepthMotion.rt.height == postProcessViewportSize.y));

            RTHandle nextLuma = curTextures.lumaTextures[curFrameIndex];
            RTHandle prevLuma = hasValidHistoryTextures ? prevTextures.lumaTextures[prevFrameIndex] : curTextures.lumaTextures[prevFrameIndex];

            RTHandle intermediateColor = curTextures.intermediateColorTexture;

            RTHandle prevFeedback;
            RTHandle nextFeedback;
            bool hasFeedbackHistory = GrabStpFeedbackHistoryTextures(hdCamera, out prevFeedback, out nextFeedback);

            // Ensure that the shader knows when it has valid history data
            // We can have invalid history data if the camera post processing history is reset manually due to a camera cut or because
            // this is the first frame and we haven't actually generated history data yet.
            bool hasValidHistory = !hdCamera.resetPostProcessingHistory && hasValidHistoryTextures && hasFeedbackHistory;

            Assert.IsTrue(Mathf.IsPowerOfTwo(m_BlueNoise.textures16L.Length));

            Texture2D noiseTexture = m_BlueNoise.textures16L[hdCamera.taaFrameIndex & (m_BlueNoise.textures16L.Length - 1)];

            StpUtils.ConstantParams constants;

            constants.nearPlane = hdCamera.projectionParams.y;
            constants.farPlane = hdCamera.projectionParams.z;
            constants.frameIndex = hdCamera.taaFrameIndex;
            constants.hasValidHistory = hasValidHistory;
            constants.stencilMask = (int)StencilUsage.ExcludeFromTAA;
            constants.currentDeltaTime = hdCamera.currentRenderDeltaTime;
            constants.lastDeltaTime = hdCamera.lastRenderDeltaTime;

            constants.currentImageSize = new Vector2(nextDepthMotion.rt.width, nextDepthMotion.rt.height);
            constants.priorImageSize = new Vector2(prevDepthMotion.rt.width, prevDepthMotion.rt.height);
            constants.feedbackImageSize = new Vector2(hdCamera.finalViewport.width, hdCamera.finalViewport.height);

            constants.noiseTexture = noiseTexture;

            // STP requires non-jittered versions of the current, previous, and "previous previous" projection matrix.
            // NOTE: The "prevProjMatrix" and "prevPrevProjMatirx" values are always unjittered because the non-jittered matrices are restored at the end of each frame.
            constants.currentProj = hdCamera.mainViewConstants.nonJitteredProjMatrix;
            constants.lastProj = hdCamera.mainViewConstants.prevProjMatrix;
            constants.lastLastProj = hdCamera.mainViewConstants.prevPrevProjMatrix;

            constants.currentView = hdCamera.mainViewConstants.viewMatrix;
            constants.lastView = hdCamera.mainViewConstants.prevViewMatrix;
            constants.lastLastView = hdCamera.mainViewConstants.prevPrevViewMatrix;

            // NOTE: STP assumes the view matrices also contain the camera position. However, HDRP may be configured to perform camera relative rendering which
            //       removes the camera translation from the view matrices. We inject the camera position directly into the view matrix here to make sure we don't
            //       run into issues when camera relative rendering is enabled.
            //
            //       Also, the previous world space camera position variable is specified as a value relative to the current world space camera position.
            //       We must add both values together in order to produce the last camera position as an absolute world space value.
            Vector3 currentPosition = hdCamera.mainViewConstants.worldSpaceCameraPos;
            Vector3 lastPosition = hdCamera.mainViewConstants.prevWorldSpaceCameraPos + hdCamera.mainViewConstants.worldSpaceCameraPos;
            Vector3 lastLastPosition = hdCamera.mainViewConstants.prevPrevWorldSpaceCameraPos + lastPosition;

            constants.currentView.SetColumn(3, new Vector4(-currentPosition.x, -currentPosition.y, -currentPosition.z, 1.0f));
            constants.lastView.SetColumn(3, new Vector4(-lastPosition.x, -lastPosition.y, -lastPosition.z, 1.0f));
            constants.lastLastView.SetColumn(3, new Vector4(-lastLastPosition.x, -lastLastPosition.y, -lastLastPosition.z, 1.0f));

            bool stpQuality = currentAsset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings.stpQuality;
            bool stpResponsive = currentAsset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings.stpResponsive;

            using (var builder = renderGraph.AddRenderPass<StpSetupCommonConstantsData>("STP Setup Common Constants", out var passData))
            {
                passData.constants = constants;

                builder.SetRenderFunc(
                    (StpSetupCommonConstantsData data, RenderGraphContext context) =>
                    {
                        StpUtils.SetCommonConstants(context.cmd, data.constants);
                    });
            }

            STPPrepassData prepassData;

            using (var builder = renderGraph.AddRenderPass<STPPrepassData>("STP Prepass", out var passData, ProfilingSampler.Get(HDProfileId.StpPrepass)))
            {
                passData.cs = defaultResources.shaders.stpPrepassCs;
                passData.cs.shaderKeywords = null;

                if (stpQuality)
                {
                    passData.cs.EnableKeyword("ENABLE_QUALITY_MODE");
                }

                if (stpResponsive)
                {
                    passData.cs.EnableKeyword("ENABLE_RESPONSIVE_FEATURE");
                }

                passData.kernelIndex = passData.cs.FindKernel("StpPrepass");
                passData.viewCount = hdCamera.viewCount;

                passData.constants = constants;

                passData.inputColor = builder.ReadTexture(inputColor);
                passData.inputDepth = builder.ReadTexture(inputDepth);
                passData.inputMotion = builder.ReadTexture(inputMotion);
                passData.inputStencil = builder.ReadTexture(inputStencil);
                passData.priorLuma = builder.ReadTexture(renderGraph.ImportTexture(prevLuma));
                passData.priorDepthMotion = builder.ReadTexture(renderGraph.ImportTexture(prevDepthMotion));
                passData.priorFeedback = builder.ReadTexture(renderGraph.ImportTexture(prevFeedback));

                passData.depthMotion = builder.WriteTexture(renderGraph.ImportTexture(nextDepthMotion));
                passData.intermediateColor = builder.WriteTexture(renderGraph.ImportTexture(intermediateColor));
                passData.luma = builder.WriteTexture(renderGraph.ImportTexture(nextLuma));

                builder.SetRenderFunc(
                    (STPPrepassData data, RenderGraphContext ctx) =>
                    {
                        int dispatchX = HDUtils.DivRoundUp((int)data.constants.currentImageSize.x, 8);
                        int dispatchY = HDUtils.DivRoundUp((int)data.constants.currentImageSize.y, 8);

                        StpUtils.SetInlineConstants(ctx.cmd, data.constants);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, HDShaderIDs._StpInputColor, data.inputColor);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, HDShaderIDs._StpInputDepth, data.inputDepth);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, HDShaderIDs._StpInputMotion, data.inputMotion);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, HDShaderIDs._StpInputStencil, data.inputStencil, 0, RenderTextureSubElement.Stencil);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, HDShaderIDs._StpPriorLuma, data.priorLuma);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, HDShaderIDs._StpPriorDepthMotion, data.priorDepthMotion);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, HDShaderIDs._StpPriorFeedback, data.priorFeedback);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, HDShaderIDs._StpIntermediateColor, data.intermediateColor);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, HDShaderIDs._StpDepthMotion, data.depthMotion);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, HDShaderIDs._StpLuma, data.luma);

                        ctx.cmd.DispatchCompute(data.cs, data.kernelIndex, dispatchX, dispatchY, data.viewCount);
                    });

                prepassData = passData;
            }

            STPTaaData stpTaaData;

            using (var builder = renderGraph.AddRenderPass<STPTaaData>("STP TAA", out var passData, ProfilingSampler.Get(HDProfileId.StpTaa)))
            {
                passData.cs = defaultResources.shaders.stpTaaCs;
                passData.cs.shaderKeywords = null;
                passData.quality = stpQuality;

                if (stpQuality)
                {
                    passData.cs.EnableKeyword("ENABLE_QUALITY_MODE");
                }

                passData.kernelIndex = passData.cs.FindKernel("StpTaa");
                passData.viewCount = hdCamera.viewCount;

                passData.constants = constants;

                passData.intermediateColor = builder.ReadTexture(prepassData.intermediateColor);
                passData.depthMotion = builder.ReadTexture(prepassData.depthMotion);
                passData.luma = builder.ReadTexture(prepassData.luma);
                passData.priorFeedback = builder.ReadTexture(renderGraph.ImportTexture(prevFeedback));

                passData.feedback = builder.WriteTexture(renderGraph.ImportTexture(nextFeedback));

                if (!stpQuality)
                {
                    passData.color = builder.WriteTexture(GetPostprocessUpsampledOutputHandle(hdCamera, renderGraph, "STP Output Color"));
                }

                builder.SetRenderFunc(
                    (STPTaaData data, RenderGraphContext ctx) =>
                    {
                        int dispatchX = HDUtils.DivRoundUp((int)data.constants.feedbackImageSize.x, 8);
                        int dispatchY = HDUtils.DivRoundUp((int)data.constants.feedbackImageSize.y, 8);

                        StpUtils.SetTaaConstants(ctx.cmd, data.constants);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, HDShaderIDs._StpIntermediateColor, data.intermediateColor);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, HDShaderIDs._StpDepthMotion, data.depthMotion);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, HDShaderIDs._StpLuma, data.luma);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, HDShaderIDs._StpPriorFeedback, data.priorFeedback);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, HDShaderIDs._StpFeedback, data.feedback);

                        if (!data.quality)
                        {
                            ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, HDShaderIDs._StpColor, data.color);
                        }

                        ctx.cmd.DispatchCompute(data.cs, data.kernelIndex, dispatchX, dispatchY, data.viewCount);
                    });

                stpTaaData = passData;
            }

            TextureHandle colorOut = stpTaaData.color;
            if (stpQuality)
            {
                STPCleanerData stpCleanerData;

                using (var builder = renderGraph.AddRenderPass<STPCleanerData>("STP Cleaner", out var passData, ProfilingSampler.Get(HDProfileId.StpCleaner)))
                {
                    passData.cs = defaultResources.shaders.stpCleanerCs;
                    passData.cs.shaderKeywords = null;

                    passData.kernelIndex = passData.cs.FindKernel("StpCleaner");
                    passData.viewCount = hdCamera.viewCount;

                    passData.constants = constants;

                    passData.feedback = builder.ReadTexture(renderGraph.ImportTexture(nextFeedback));

                    passData.color = builder.WriteTexture(GetPostprocessUpsampledOutputHandle(hdCamera, renderGraph, "STP Output Color"));

                    builder.SetRenderFunc(
                        (STPCleanerData data, RenderGraphContext ctx) =>
                        {
                            int dispatchX = HDUtils.DivRoundUp((int)data.constants.feedbackImageSize.x, 8);
                            int dispatchY = HDUtils.DivRoundUp((int)data.constants.feedbackImageSize.y, 8);

                            StpUtils.SetCleanerConstants(ctx.cmd, data.constants);

                            ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, HDShaderIDs._StpFeedback, data.feedback);
                            ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, HDShaderIDs._StpColor, data.color);

                            ctx.cmd.DispatchCompute(data.cs, data.kernelIndex, dispatchX, dispatchY, data.viewCount);
                        });

                    stpCleanerData = passData;
                }

                colorOut = stpCleanerData.color;
            }

            return colorOut;
        }
    }
}
