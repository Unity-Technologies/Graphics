using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class VolumetricCloudsSystem
    {
        RTHandle RequestVolumetricCloudsHistoryTexture(HDCamera hdCamera, bool current, HDCameraFrameHistoryType type, bool fullscale, VolumetricClouds settings)
        {
            RTHandle texture = current ? hdCamera.GetCurrentFrameRT((int)type) : hdCamera.GetPreviousFrameRT((int)type);
            if (texture != null)
                return texture;

            GraphicsFormat format = type == HDCameraFrameHistoryType.VolumetricClouds0 ? GetCloudsColorFormat(settings, true) : GraphicsFormat.R16G16B16A16_SFloat;
            var name = string.Format("CloudsHistory{0}", type);
            var historyAlloc = new HDCamera.CustomHistoryAllocator(Vector2.one * (fullscale ? 1.0f : 0.5f), format, name);

            return hdCamera.AllocHistoryFrameRT((int)type, historyAlloc.Allocator, 2);
        }

        private int CombineVolumetricCloudsHistoryStateToMask(HDCamera hdCamera)
        {
            // Combine the flags to define the current history validity mask
            return (hdCamera.planet.renderingSpace == RenderingSpace.World ? (int)HDCamera.HistoryEffectFlags.CustomBit0 : 0);
        }

        private bool EvaluateVolumetricCloudsHistoryValidity(HDCamera hdCamera)
        {
            // Evaluate the history validity
            int flagMask = CombineVolumetricCloudsHistoryStateToMask(hdCamera);
            return hdCamera.EffectHistoryValidity(HDCamera.HistoryEffectSlot.VolumetricClouds, flagMask);
        }

        private void PropagateVolumetricCloudsHistoryValidity(HDCamera hdCamera)
        {
            int flagMask = CombineVolumetricCloudsHistoryStateToMask(hdCamera);
            hdCamera.PropagateEffectHistoryValidity(HDCamera.HistoryEffectSlot.VolumetricClouds, flagMask);
        }

        struct VolumetricCloudsParameters_Accumulation
        {
            // Resolution parameters
            public int traceWidth;
            public int traceHeight;
            public int intermediateWidth;
            public int intermediateHeight;
            public int finalWidth;
            public int finalHeight;
            public int viewCount;

            public bool historyValidity;
            public Vector2Int previousViewportSize;

            // Compute shader and kernels
            public int reprojectKernel;
            public int upscaleClouds;

            // Data common to all volumetric cloud passes
            public VolumetricCloudCommonData commonData;
        }

        VolumetricCloudsParameters_Accumulation PrepareVolumetricCloudsParameters_Accumulation(HDCamera hdCamera, VolumetricClouds settings, TVolumetricCloudsCameraType cameraType,
            bool historyValidity, float downscaling)
        {
            VolumetricCloudsParameters_Accumulation parameters = new VolumetricCloudsParameters_Accumulation();

            // Compute the cloud model data
            CloudModelData cloudModelData = GetCloudModelData(settings);

            // Fill the common data
            FillVolumetricCloudsCommonData(hdCamera, hdCamera.exposureControlFS, settings, cameraType, in cloudModelData, ref parameters.commonData);

            // Flag for the perceptual blending
            float perceptualBlending = settings.perceptualBlending.value;

            // We need to make sure that the allocated size of the history buffers and the dispatch size are perfectly equal.
            // The ideal approach would be to have a function for that returns the converted size from a viewport and texture size.
            // but for now we do it like this.
            // Final resolution at which the effect should be exported
            parameters.finalWidth = hdCamera.actualWidth;
            parameters.finalHeight = hdCamera.actualHeight;
            // Intermediate resolution at which the effect is accumulated
            parameters.intermediateWidth = Mathf.RoundToInt(downscaling * hdCamera.actualWidth);
            parameters.intermediateHeight = Mathf.RoundToInt(downscaling * hdCamera.actualHeight);
            // Resolution at which the effect is traced
            parameters.traceWidth = Mathf.RoundToInt(0.5f * downscaling * hdCamera.actualWidth);
            parameters.traceHeight = Mathf.RoundToInt(0.5f * downscaling * hdCamera.actualHeight);

            parameters.viewCount = hdCamera.viewCount;
            parameters.historyValidity = historyValidity;

            float historyScale =  hdCamera.intermediateDownscaling * (hdCamera.volumetricCloudsFullscaleHistory ? 1.0f : 2.0f);
            parameters.previousViewportSize = new Vector2Int(
                x: Mathf.RoundToInt(historyScale * hdCamera.historyRTHandleProperties.previousViewportSize.x),
                y: Mathf.RoundToInt(historyScale * hdCamera.historyRTHandleProperties.previousViewportSize.y)
            );

            // Compute shader and kernels
            parameters.reprojectKernel = settings.ghostingReduction.value ? m_ReprojectCloudsRejectionKernel : m_ReprojectCloudsKernel;

            bool quarterRes = downscaling == 0.5f;
            if (parameters.commonData.perceptualBlending)
                parameters.upscaleClouds = quarterRes ? m_UpscaleCloudsPerceptualKernel : m_CombineCloudsPerceptualKernel;
            else
                parameters.upscaleClouds = quarterRes ? m_UpscaleCloudsKernel : m_CombineCloudsKernel;

            // Update the constant buffer
            VolumetricCloudsCameraData cameraData;
            cameraData.cameraType = parameters.commonData.cameraType;
            cameraData.traceWidth = parameters.traceWidth;
            cameraData.traceHeight = parameters.traceHeight;
            cameraData.intermediateWidth = parameters.intermediateWidth;
            cameraData.intermediateHeight = parameters.intermediateHeight;
            cameraData.finalWidth = parameters.finalWidth;
            cameraData.finalHeight = parameters.finalHeight;
            cameraData.enableExposureControl = parameters.commonData.enableExposureControl;
            cameraData.lowResolution = true;
            cameraData.enableIntegration = true;
            UpdateShaderVariablesClouds(ref parameters.commonData.cloudsCB, hdCamera, settings, cameraData, cloudModelData, false);

            return parameters;
        }

        static void TraceVolumetricClouds_Accumulation(CommandBuffer cmd, VolumetricCloudsParameters_Accumulation parameters,
            GraphicsBuffer ambientProbe, RTHandle colorBuffer, RTHandle depthPyramid,
            RTHandle tracedCloudsLighting, RTHandle tracedCloudsDepth,
            RTHandle currentHistory0Buffer, RTHandle previousHistory0Buffer,
            RTHandle currentHistory1Buffer, RTHandle previousHistory1Buffer,
            RTHandle cloudsLighting,  RTHandle cloudsDepth)
        {
            // Compute the number of tiles to evaluate
            int traceTX = HDUtils.DivRoundUp(parameters.traceWidth, 8);
            int traceTY = HDUtils.DivRoundUp(parameters.traceHeight, 8);

            // Compute the number of tiles to evaluate
            int intermediateTX = HDUtils.DivRoundUp(parameters.intermediateWidth, 8);
            int intermediateTY = HDUtils.DivRoundUp(parameters.intermediateHeight, 8);

            // Compute the number of tiles to evaluate
            int finalTX = HDUtils.DivRoundUp(parameters.finalWidth, 8);
            int finalTY = HDUtils.DivRoundUp(parameters.finalHeight, 8);

            // Bind the sampling textures
            BlueNoise.BindDitheredTextureSet(cmd, parameters.commonData.ditheredTextureSet);

            // We only need to handle history buffers if this is not a reflection probe
            // We need to make sure that the allocated size of the history buffers and the dispatch size are perfectly equal.
            // The ideal approach would be to have a function for that returns the converted size from a viewport and texture size.
            // but for now we do it like this.
            Vector2Int previousViewportSize = previousHistory0Buffer.GetScaledSize(parameters.previousViewportSize);
            parameters.commonData.cloudsCB._HistoryViewportScale.Set(previousViewportSize.x / (float)previousHistory0Buffer.rt.width, previousViewportSize.y / (float)previousHistory0Buffer.rt.height);

            // Bind the constant buffer (global as we need it for the .shader as well)
            ConstantBuffer.PushGlobal(cmd, parameters.commonData.cloudsCB, HDShaderIDs._ShaderVariablesClouds);
            ConstantBuffer.Set<ShaderVariablesClouds>(parameters.commonData.volumetricCloudsCS, HDShaderIDs._ShaderVariablesClouds);

            // Ray-march the clouds for this frame
            DoVolumetricCloudsTrace(cmd, traceTX, traceTY, parameters.viewCount, in parameters.commonData,
                ambientProbe, colorBuffer, depthPyramid,
                tracedCloudsLighting, tracedCloudsDepth);

            // We only reproject for realtime clouds
            DoVolumetricCloudsReproject(cmd, parameters.reprojectKernel, intermediateTX, intermediateTY, parameters.viewCount, in parameters.commonData,
                tracedCloudsLighting, tracedCloudsDepth, depthPyramid,
                true, !parameters.historyValidity, previousHistory0Buffer, previousHistory1Buffer,
                currentHistory0Buffer, currentHistory1Buffer);

            DoVolumetricCloudsUpscale(cmd, parameters.upscaleClouds, finalTX, finalTY, parameters.viewCount, in parameters.commonData,
                currentHistory0Buffer, currentHistory1Buffer, colorBuffer, depthPyramid,
                cloudsLighting, cloudsDepth);
        }

        class VolumetricCloudsAccumulationData
        {
            public VolumetricCloudsParameters_Accumulation parameters;

            // Inputs
            public TextureHandle colorBuffer;
            public TextureHandle depthPyramid;
            public TextureHandle motionVectors;
            public BufferHandle ambientProbeBuffer;

            // History and history output
            public TextureHandle previousHistoryBuffer0;
            public TextureHandle currentHistoryBuffer0;
            public TextureHandle previousHistoryBuffer1;
            public TextureHandle currentHistoryBuffer1;

            // Intermediate buffers
            public TextureHandle tracedCloudsLighting;
            public TextureHandle tracedCloudsDepth;

            // Cloud pass output
            public TextureHandle cloudsLighting;
            public TextureHandle cloudsDepth;
        }

        VolumetricCloudsOutput RenderVolumetricClouds_Accumulation(RenderGraph renderGraph, HDCamera hdCamera, TVolumetricCloudsCameraType cameraType, TextureHandle colorBuffer, TextureHandle depthPyramid)
        {
            using (var builder = renderGraph.AddRenderPass<VolumetricCloudsAccumulationData>("Volumetric Clouds", out var passData, ProfilingSampler.Get(HDProfileId.VolumetricClouds)))
            {
                builder.EnableAsyncCompute(false);
                VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();

                // When DRS scale is lower than threshold, trace in half res instead of quarter res
                float threshold = m_RenderPipeline.asset.currentPlatformRenderPipelineSettings.dynamicResolutionSettings.lowResVolumetricCloudsMinimumThreshold;
                bool halfRes = DynamicResolutionHandler.instance.GetCurrentScale() * 100.0f < threshold;
                float downscaling = halfRes ? 1.0f : 0.5f;

                bool fullscaleHistory = DynamicResolutionHandler.instance.DynamicResolutionEnabled() ? true : false;
                if (fullscaleHistory != hdCamera.volumetricCloudsFullscaleHistory)
                {
                    // If history size is invalid, release it
                    hdCamera.ReleaseHistoryFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds0);
                    hdCamera.ReleaseHistoryFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds1);
                    hdCamera.volumetricCloudsFullscaleHistory = fullscaleHistory;
                }

                // If history buffers are null, it means they were not allocated yet so they will potentially contain garbage at first frame.
                var historyValidity = EvaluateVolumetricCloudsHistoryValidity(hdCamera) && hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds0) != null;

                // Parameters
                passData.parameters = PrepareVolumetricCloudsParameters_Accumulation(hdCamera, settings, cameraType, historyValidity, downscaling);

                // Input buffers
                passData.colorBuffer = builder.ReadTexture(colorBuffer);
                passData.depthPyramid = builder.ReadTexture(depthPyramid);
                passData.ambientProbeBuffer = builder.ReadBuffer(renderGraph.ImportBuffer(m_CloudsDynamicProbeBuffer));

                // History and pass output
                hdCamera.intermediateDownscaling = downscaling;
                passData.currentHistoryBuffer0 = renderGraph.ImportTexture(RequestVolumetricCloudsHistoryTexture(hdCamera, true, HDCameraFrameHistoryType.VolumetricClouds0, fullscaleHistory, settings));
                passData.previousHistoryBuffer0 = renderGraph.ImportTexture(RequestVolumetricCloudsHistoryTexture(hdCamera, false, HDCameraFrameHistoryType.VolumetricClouds0, fullscaleHistory, settings));
                passData.currentHistoryBuffer1 = renderGraph.ImportTexture(RequestVolumetricCloudsHistoryTexture(hdCamera, true, HDCameraFrameHistoryType.VolumetricClouds1, fullscaleHistory, settings));
                passData.previousHistoryBuffer1 = renderGraph.ImportTexture(RequestVolumetricCloudsHistoryTexture(hdCamera, false, HDCameraFrameHistoryType.VolumetricClouds1, fullscaleHistory, settings));

                CreateTracingTextures(renderGraph, builder, settings, downscaling * 0.5f, out passData.tracedCloudsLighting, out passData.tracedCloudsDepth);
                CreateOutputTextures(renderGraph, builder, settings, out passData.cloudsLighting, out passData.cloudsDepth);

                builder.SetRenderFunc(
                    (VolumetricCloudsAccumulationData data, RenderGraphContext ctx) =>
                    {
                        TraceVolumetricClouds_Accumulation(ctx.cmd, data.parameters,
                            data.ambientProbeBuffer, data.colorBuffer, data.depthPyramid,
                            data.tracedCloudsLighting, data.tracedCloudsDepth,
                            data.currentHistoryBuffer0, data.previousHistoryBuffer0,
                            data.currentHistoryBuffer1, data.previousHistoryBuffer1,
                            data.cloudsLighting, data.cloudsDepth);
                    });

                // Make sure to mark the history frame index validity.
                PropagateVolumetricCloudsHistoryValidity(hdCamera);

                // Gather and return the buffers
                VolumetricCloudsOutput cloudsData = new VolumetricCloudsOutput();
                cloudsData.lightingBuffer = passData.cloudsLighting;
                cloudsData.depthBuffer = passData.cloudsDepth;
                cloudsData.valid = true;
                return cloudsData;
            }
        }
    }
}
