using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Allocation of the first history buffer
        static RTHandle VolumetricClouds0HistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one * 0.5f, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension,
                enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                name: string.Format("{0}_CloudsHistory0Buffer{1}", viewName, frameIndex));
        }

        // Allocation of the second history buffer
        static RTHandle VolumetricClouds1HistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one * 0.5f, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension,
                enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                name: string.Format("{0}_CloudsHistory1Buffer{1}", viewName, frameIndex));
        }

        // Functions to request the history buffers
        static RTHandle RequestCurrentVolumetricCloudsHistoryTexture0(HDCamera hdCamera)
        {
            return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds0)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds0,
                VolumetricClouds0HistoryBufferAllocatorFunction, 2);
        }

        static RTHandle RequestPreviousVolumetricCloudsHistoryTexture0(HDCamera hdCamera)
        {
            return hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds0)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds0,
                VolumetricClouds0HistoryBufferAllocatorFunction, 2);
        }

        static RTHandle RequestCurrentVolumetricCloudsHistoryTexture1(HDCamera hdCamera)
        {
            return hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds1)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds1,
                VolumetricClouds1HistoryBufferAllocatorFunction, 2);
        }

        static RTHandle RequestPreviousVolumetricCloudsHistoryTexture1(HDCamera hdCamera)
        {
            return hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds1)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds1,
                VolumetricClouds1HistoryBufferAllocatorFunction, 2);
        }

        private int CombineVolumetricCloudsHistoryStateToMask(HDCamera hdCamera)
        {
            // Combine the flags to define the current mask (we use the custom bit 0 to track the locality of the clouds.
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
            public int depthDownscaleKernel;
            public int reprojectKernel;
            public int upscaleClouds;

            // Data common to all volumetric cloud passes
            public VolumetricCloudCommonData commonData;
        }

        VolumetricCloudsParameters_Accumulation PrepareVolumetricCloudsParameters_Accumulation(HDCamera hdCamera, VolumetricClouds settings, TVolumetricCloudsCameraType cameraType, bool historyValidity)
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
            parameters.intermediateWidth = Mathf.RoundToInt(0.5f * hdCamera.actualWidth);
            parameters.intermediateHeight = Mathf.RoundToInt(0.5f * hdCamera.actualHeight);
            // Resolution at which the effect is traced
            parameters.traceWidth = Mathf.RoundToInt(0.25f * hdCamera.actualWidth);
            parameters.traceHeight = Mathf.RoundToInt(0.25f * hdCamera.actualHeight);

            parameters.viewCount = hdCamera.viewCount;
            parameters.previousViewportSize = hdCamera.historyRTHandleProperties.previousViewportSize;
            parameters.historyValidity = historyValidity;

            // Compute shader and kernels
            parameters.depthDownscaleKernel = m_CloudDownscaleDepthKernel;
            parameters.reprojectKernel = settings.ghostingReduction.value ? m_ReprojectCloudsRejectionKernel : m_ReprojectCloudsKernel;
            parameters.upscaleClouds = hdCamera.msaaEnabled ? m_UpscaleCloudsKernel : m_UpscaleCloudsPerceptualKernel;

            // Update the constant buffer
            VolumetricCloudsCameraData cameraData;
            cameraData.cameraType = parameters.commonData.cameraType;
            cameraData.traceWidth = parameters.traceWidth;
            cameraData.traceHeight = parameters.traceHeight;
            cameraData.intermediateWidth = parameters.intermediateWidth;
            cameraData.intermediateHeight = parameters.intermediateHeight;
            cameraData.finalWidth = parameters.finalWidth;
            cameraData.finalHeight = parameters.finalHeight;
            cameraData.viewCount = parameters.viewCount;
            cameraData.enableExposureControl = parameters.commonData.enableExposureControl;
            cameraData.lowResolution = true;
            cameraData.enableIntegration = true;
            UpdateShaderVariablesClouds(ref parameters.commonData.cloudsCB, hdCamera, settings, cameraData, cloudModelData, false);

            // If this is a default camera, we want the improved blending, otherwise we don't (in the case of a planar)
            parameters.commonData.cloudsCB._ImprovedTransmittanceBlend = parameters.commonData.cameraType == TVolumetricCloudsCameraType.Default ? perceptualBlending : 0.0f;
            parameters.commonData.cloudsCB._CubicTransmittance = parameters.commonData.cameraType == TVolumetricCloudsCameraType.Default && hdCamera.msaaEnabled ? perceptualBlending : 0;

            return parameters;
        }

        static void TraceVolumetricClouds_Accumulation(CommandBuffer cmd, VolumetricCloudsParameters_Accumulation parameters, GraphicsBuffer ambientProbe,
            RTHandle colorBuffer, RTHandle depthPyramid, RTHandle motionVectors, RTHandle volumetricLightingTexture,
            RTHandle currentHistory0Buffer, RTHandle previousHistory0Buffer,
            RTHandle currentHistory1Buffer, RTHandle previousHistory1Buffer,
            RTHandle intermediateCloudsLighting,
            RTHandle halfResDepthBuffer, RTHandle intermediateCloudsDepth,
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
            parameters.commonData.cloudsCB._HistoryViewportSize = new Vector2(previousViewportSize.x, previousViewportSize.y);
            parameters.commonData.cloudsCB._HistoryBufferSize = new Vector2(previousHistory0Buffer.rt.width, previousHistory0Buffer.rt.height);

            // Bind the constant buffer (global as we need it for the .shader as well)
            ConstantBuffer.PushGlobal(cmd, parameters.commonData.cloudsCB, HDShaderIDs._ShaderVariablesClouds);
            ConstantBuffer.Set<ShaderVariablesClouds>(parameters.commonData.volumetricCloudsCS, HDShaderIDs._ShaderVariablesClouds);

            // Depth downscale
            DoVolumetricCloudsPrepare(cmd, parameters.depthDownscaleKernel, intermediateTX, intermediateTY, parameters.viewCount, in parameters.commonData,
                depthPyramid, halfResDepthBuffer);

            // Ray-march the clouds for this frame
            DoVolumetricCloudsTrace(cmd, traceTX, traceTY, parameters.viewCount, in parameters.commonData,
                volumetricLightingTexture, halfResDepthBuffer, ambientProbe,
                intermediateCloudsLighting, intermediateCloudsDepth);

            // We only reproject for realtime clouds
            DoVolumetricCloudsReproject(cmd, parameters.reprojectKernel, intermediateTX, intermediateTY, parameters.viewCount, in parameters.commonData,
                intermediateCloudsLighting, intermediateCloudsDepth, halfResDepthBuffer,
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
            public TextureHandle volumetricLighting;

            // History and history output
            public TextureHandle previousHistoryBuffer0;
            public TextureHandle currentHistoryBuffer0;
            public TextureHandle previousHistoryBuffer1;
            public TextureHandle currentHistoryBuffer1;

            // Intermediate buffers
            public TextureHandle halfResCloudsLighting;
            public TextureHandle halfResDepthBuffer;
            public TextureHandle halfResCloudsDepth;

            // Cloud pass output
            public TextureHandle cloudsLighting;
            public TextureHandle cloudsDepth;
        }

        VolumetricCloudsOutput RenderVolumetricClouds_Accumulation(RenderGraph renderGraph, HDCamera hdCamera, TVolumetricCloudsCameraType cameraType,
            TextureHandle colorBuffer, TextureHandle depthPyramid, TextureHandle motionVectors, TextureHandle volumetricLighting)
        {
            using (var builder = renderGraph.AddRenderPass<VolumetricCloudsAccumulationData>("Volumetric Clouds", out var passData, ProfilingSampler.Get(HDProfileId.VolumetricClouds)))
            {
                builder.EnableAsyncCompute(false);
                VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();

                // If history buffers are null, it means they were not allocated yet so they will potentially contain garbage at first frame.
                var historyValidity = EvaluateVolumetricCloudsHistoryValidity(hdCamera) && hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.VolumetricClouds0) != null;

                // Parameters
                passData.parameters = PrepareVolumetricCloudsParameters_Accumulation(hdCamera, settings, cameraType, historyValidity);

                // Input buffers
                passData.colorBuffer = builder.ReadTexture(colorBuffer);
                passData.depthPyramid = builder.ReadTexture(depthPyramid);
                passData.motionVectors = builder.ReadTexture(motionVectors);
                passData.ambientProbeBuffer = builder.ReadBuffer(renderGraph.ImportBuffer(m_CloudsDynamicProbeBuffer));
                passData.volumetricLighting = builder.ReadTexture(volumetricLighting);

                // History and pass output
                passData.currentHistoryBuffer0 = renderGraph.ImportTexture(RequestCurrentVolumetricCloudsHistoryTexture0(hdCamera));
                passData.previousHistoryBuffer0 = renderGraph.ImportTexture(RequestPreviousVolumetricCloudsHistoryTexture0(hdCamera));
                passData.currentHistoryBuffer1 = renderGraph.ImportTexture(RequestCurrentVolumetricCloudsHistoryTexture1(hdCamera));
                passData.previousHistoryBuffer1 = renderGraph.ImportTexture(RequestPreviousVolumetricCloudsHistoryTexture1(hdCamera));

                // Intermediate textures
                passData.halfResCloudsLighting = builder.CreateTransientTexture(new TextureDesc(Vector2.one * 0.5f, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Half Res Clouds Lighting" });
                passData.halfResDepthBuffer = builder.CreateTransientTexture(new TextureDesc(Vector2.one * 0.5f, true, true)
                { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Half Res Scene Depth" });
                passData.halfResCloudsDepth = builder.CreateTransientTexture(new TextureDesc(Vector2.one * 0.5f, true, true)
                { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Half Res Clouds Depth" });

                // Output of the clouds
                passData.cloudsLighting = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, name = "Volumetric Clouds Lighting Texture" }));
                passData.cloudsDepth = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(Vector2.one, true, true)
                { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true, name = "Volumetric Clouds Depth Texture" }));

                builder.SetRenderFunc(
                    (VolumetricCloudsAccumulationData data, RenderGraphContext ctx) =>
                    {
                        TraceVolumetricClouds_Accumulation(ctx.cmd, data.parameters, data.ambientProbeBuffer,
                            data.colorBuffer, data.depthPyramid, data.motionVectors, data.volumetricLighting,
                            data.currentHistoryBuffer0, data.previousHistoryBuffer0,
                            data.currentHistoryBuffer1, data.previousHistoryBuffer1,
                            data.halfResCloudsLighting,
                            data.halfResDepthBuffer, data.halfResCloudsDepth,
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
