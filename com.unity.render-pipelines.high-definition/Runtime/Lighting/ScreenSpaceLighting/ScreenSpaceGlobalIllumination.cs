using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL]
    // Define if we use SSGI, RTGI or none
    enum IndirectDiffuseMode
    {
        Off,
        ScreenSpace,
        Raytrace
    }

    public partial class HDRenderPipeline
    {
        // Buffers used for the evaluation
        RTHandle m_IndirectDiffuseBuffer0 = null;
        RTHandle m_IndirectDiffuseBuffer1 = null;
        RTHandle m_IndirectDiffuseHitPointBuffer = null;

        // The set of kernels that we shall be using
        int m_TraceGlobalIlluminationKernel;
        int m_TraceGlobalIlluminationHalfKernel;
        int m_ReprojectGlobalIlluminationKernel;
        int m_ReprojectGlobalIlluminationHalfKernel;
        int m_BilateralUpSampleColorTMKernel;

        // Temporal variables (to avoid allocation at runtime)
        Vector4 halfScreenSize = new Vector4(0.0f, 0.0f, 0.0f, 0.0f);
        Vector2 firstMipOffset = new Vector2(0.0f, 0.0f);

        void InitScreenSpaceGlobalIllumination()
        {
            if (m_Asset.currentPlatformRenderPipelineSettings.supportSSGI)
            {
                m_IndirectDiffuseBuffer0 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "IndirectDiffuseBuffer0");
                m_IndirectDiffuseBuffer1 = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "IndirectDiffuseBuffer1");
                m_IndirectDiffuseHitPointBuffer = RTHandles.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16_SFloat, dimension: TextureXR.dimension, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, autoGenerateMips: false, name: "IndirectDiffuseHitBuffer");

                // Grab the sets of shaders that we'll be using
                ComputeShader ssGICS = m_Asset.renderPipelineResources.shaders.screenSpaceGlobalIlluminationCS;
                ComputeShader bilateralUpsampleCS = m_Asset.renderPipelineResources.shaders.bilateralUpsampleCS;

                // Grab the set of kernels that we shall be using
                m_TraceGlobalIlluminationKernel = ssGICS.FindKernel("TraceGlobalIllumination");
                m_TraceGlobalIlluminationHalfKernel = ssGICS.FindKernel("TraceGlobalIlluminationHalf");
                m_ReprojectGlobalIlluminationKernel = ssGICS.FindKernel("ReprojectGlobalIllumination");
                m_ReprojectGlobalIlluminationHalfKernel = ssGICS.FindKernel("ReprojectGlobalIlluminationHalf");
                m_BilateralUpSampleColorTMKernel = bilateralUpsampleCS.FindKernel("BilateralUpSampleColorTM");
            }
        }

        void ReleaseScreenSpaceGlobalIllumination()
        {
            if (m_IndirectDiffuseBuffer0 != null)
                RTHandles.Release(m_IndirectDiffuseBuffer0);
            if (m_IndirectDiffuseBuffer1 != null)
                RTHandles.Release(m_IndirectDiffuseBuffer1);
            if (m_IndirectDiffuseHitPointBuffer != null)
                RTHandles.Release(m_IndirectDiffuseHitPointBuffer);
        }

        // This is shared between SSGI and RTGI
        IndirectDiffuseMode GetIndirectDiffuseMode(HDCamera hdCamera)
        {
            IndirectDiffuseMode mode = IndirectDiffuseMode.Off;

            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.SSGI))
            {
                var settings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();
                if (settings.enable.value)
                {
                    // RTGI is only valid if raytracing is enabled
                    bool raytracing = hdCamera.frameSettings.IsEnabled(FrameSettingsField.RayTracing) && settings.rayTracing.value;
                    mode = raytracing ? IndirectDiffuseMode.Raytrace : IndirectDiffuseMode.ScreenSpace;
                }
            }
            return mode;
        }

        // Bind the indirect diffuse texture for the lightloop to read from it
        void BindIndirectDiffuseTexture(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._IndirectDiffuseTexture, m_IndirectDiffuseBuffer0);
        }

        // If there is no SSGI, bind a black 1x1 texture
        static void BindBlackIndirectDiffuseTexture(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._IndirectDiffuseTexture, TextureXR.GetBlackTexture());
        }

        void RenderSSGI(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext, int frameCount)
        {
            // Grab the global illumination volume component
            GlobalIllumination giSettings = hdCamera.volumeStack.GetComponent<GlobalIllumination>();

            // Grab the noise texture manager
            BlueNoise blueNoise = GetBlueNoiseManager();

            // Grab the shaders we shall be using
            ComputeShader ssGICS = m_Asset.renderPipelineResources.shaders.screenSpaceGlobalIlluminationCS;

            // Evaluate the dispatch parameters
            int texWidth, texHeight;
            if (giSettings.fullResolutionSS)
            {
                texWidth = hdCamera.actualWidth;
                texHeight = hdCamera.actualHeight;
                halfScreenSize.Set(texWidth * 0.5f, texHeight * 0.5f, 2.0f / texWidth, 2.0f / texHeight);
            }
            else
            {
                texWidth = hdCamera.actualWidth / 2;
                texHeight = hdCamera.actualHeight / 2;
                halfScreenSize.Set(texWidth, texHeight, 1.0f / texWidth, 1.0f / texHeight);
            }
            int areaTileSize = 8;
            int numTilesXHR = (texWidth + (areaTileSize - 1)) / areaTileSize;
            int numTilesYHR = (texHeight + (areaTileSize - 1)) / areaTileSize;

            // Based on if we are doing it in half resolution or full, we need to define initial and final buffer to avoid a useless blit
            RTHandle buffer0, buffer1;
            if (!giSettings.fullResolutionSS)
            {
                buffer0 = m_IndirectDiffuseBuffer0;
                buffer1 = m_IndirectDiffuseBuffer1;
            }
            else
            {
                buffer0 = m_IndirectDiffuseBuffer1;
                buffer1 = m_IndirectDiffuseBuffer0;
            }

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.SsgiPass)))
            {
                // Fetch the right tracing kernel
                int currentKernel = giSettings.fullResolutionSS ? m_TraceGlobalIlluminationKernel : m_TraceGlobalIlluminationHalfKernel;

                // Inject all the input scalars
                float n = hdCamera.camera.nearClipPlane;
                float f = hdCamera.camera.farClipPlane;
                float thickness = giSettings.depthBufferThickness.value;
                float thicknessScale = 1.0f / (1.0f + thickness);
                float thicknessBias = -n / (f - n) * (thickness * thicknessScale);
                cmd.SetComputeFloatParam(ssGICS, HDShaderIDs._IndirectDiffuseThicknessScale, thicknessScale);
                cmd.SetComputeFloatParam(ssGICS, HDShaderIDs._IndirectDiffuseThicknessBias, thicknessBias);
                cmd.SetComputeIntParam(ssGICS, HDShaderIDs._IndirectDiffuseSteps, giSettings.raySteps);
                cmd.SetComputeFloatParam(ssGICS, HDShaderIDs._IndirectDiffuseMaximalRadius, giSettings.maximalRadius);
                // Inject half screen size if required
                if (!giSettings.fullResolutionSS)
                    cmd.SetComputeVectorParam(ssGICS, HDShaderIDs._HalfScreenSize, halfScreenSize);

                // Inject the ray-tracing sampling data
                blueNoise.BindDitheredRNGData1SPP(cmd);

                // Inject all the input textures/buffers
                cmd.SetComputeTextureParam(ssGICS, currentKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthTexture());
                cmd.SetComputeTextureParam(ssGICS, currentKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                cmd.SetComputeTextureParam(ssGICS, currentKernel, HDShaderIDs._IndirectDiffuseHitPointTextureRW, m_IndirectDiffuseHitPointBuffer);
                var info = m_SharedRTManager.GetDepthBufferMipChainInfo();
                cmd.SetComputeBufferParam(ssGICS, currentKernel, HDShaderIDs._DepthPyramidMipLevelOffsets, info.GetOffsetBufferData(m_DepthPyramidMipLevelOffsetsBuffer));
                cmd.SetGlobalBuffer(HDShaderIDs.g_vLightListGlobal, m_TileAndClusterData.lightList);

                // Do the ray marching
                cmd.DispatchCompute(ssGICS, currentKernel, numTilesXHR, numTilesYHR, hdCamera.viewCount);

                // Fetch the right kernel to use
                currentKernel = giSettings.fullResolutionSS ? m_ReprojectGlobalIlluminationKernel : m_ReprojectGlobalIlluminationHalfKernel;

                // Update global constant buffer.
                // This should probably be a shader specific uniform instead of reusing the global constant buffer one since it's the only one udpated here.
                m_ShaderVariablesRayTracingCB._RaytracingIntensityClamp = giSettings.clampValueSS;
                ConstantBuffer.PushGlobal(cmd, m_ShaderVariablesRayTracingCB, HDShaderIDs._ShaderVariablesRaytracing);

                // Inject all the input scalars
                cmd.SetComputeVectorParam(ssGICS, HDShaderIDs._ColorPyramidUvScaleAndLimitPrevFrame, HDUtils.ComputeViewportScaleAndLimit(hdCamera.historyRTHandleProperties.previousViewportSize, hdCamera.historyRTHandleProperties.previousRenderTargetSize));

                // Bind all the input buffers
                cmd.SetComputeTextureParam(ssGICS, currentKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                cmd.SetComputeTextureParam(ssGICS, currentKernel, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                cmd.SetComputeTextureParam(ssGICS, currentKernel, HDShaderIDs._IndirectDiffuseHitPointTexture, m_IndirectDiffuseHitPointBuffer);
                var previousColorPyramid = hdCamera.GetPreviousFrameRT((int)HDCameraFrameHistoryType.ColorBufferMipChain);
                cmd.SetComputeTextureParam(ssGICS, currentKernel, HDShaderIDs._ColorPyramidTexture, previousColorPyramid != null ? previousColorPyramid : TextureXR.GetBlackTexture());
                var historyDepthBuffer = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.Depth);
                cmd.SetComputeTextureParam(ssGICS, currentKernel, HDShaderIDs._HistoryDepthTexture, historyDepthBuffer != null ? historyDepthBuffer : TextureXR.GetBlackTexture());
                cmd.SetComputeBufferParam(ssGICS, currentKernel, HDShaderIDs._DepthPyramidMipLevelOffsets, info.GetOffsetBufferData(m_DepthPyramidMipLevelOffsetsBuffer));

                // Bind the output texture
                cmd.SetComputeTextureParam(ssGICS, currentKernel, HDShaderIDs._IndirectDiffuseTextureRW, buffer1);

                // Do the reprojection
                cmd.DispatchCompute(ssGICS, currentKernel, numTilesXHR, numTilesYHR, hdCamera.viewCount);

                float historyValidity = 1.0f;
#if UNITY_HDRP_DXR_TESTS_DEFINE
                if (Application.isPlaying)
                    historyValidity = 0.0f;
                else
#endif
                    // We need to check if something invalidated the history buffers
                    historyValidity *= ValidRayTracingHistory(hdCamera) ? 1.0f : 0.0f;

                // Do the denoising part
                SSGIDenoiser ssgiDenoiser = GetSSGIDenoiser();
                ssgiDenoiser.Denoise(cmd, hdCamera, buffer1, buffer0, halfResolution: !giSettings.fullResolutionSS, historyValidity: historyValidity);

                // If this was a half resolution effect, we still have to upscale it
                if (!giSettings.fullResolutionSS)
                {
                    ComputeShader bilateralUpsampleCS = m_Asset.renderPipelineResources.shaders.bilateralUpsampleCS;

                    // Re-evaluate the dispatch parameters (we are evaluating the upsample in full resolution)
                    numTilesXHR = (hdCamera.actualWidth + (areaTileSize - 1)) / areaTileSize;
                    numTilesYHR = (hdCamera.actualHeight + (areaTileSize - 1)) / areaTileSize;

                    // Inject the input scalars
                    cmd.SetComputeVectorParam(bilateralUpsampleCS, HDShaderIDs._HalfScreenSize, halfScreenSize);
                    firstMipOffset.Set(HDShadowUtils.Asfloat((uint)info.mipLevelOffsets[1].x), HDShadowUtils.Asfloat((uint)info.mipLevelOffsets[1].y));
                    cmd.SetComputeVectorParam(bilateralUpsampleCS, HDShaderIDs._DepthPyramidFirstMipLevelOffset, firstMipOffset);

                    // Inject all the input buffers
                    cmd.SetComputeTextureParam(bilateralUpsampleCS, m_BilateralUpSampleColorTMKernel, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthTexture());
                    cmd.SetComputeTextureParam(bilateralUpsampleCS, m_BilateralUpSampleColorTMKernel, HDShaderIDs._LowResolutionTexture, buffer1);
                    cmd.SetComputeBufferParam(bilateralUpsampleCS, m_BilateralUpSampleColorTMKernel, HDShaderIDs._DepthPyramidMipLevelOffsets, info.GetOffsetBufferData(m_DepthPyramidMipLevelOffsetsBuffer));

                    // Inject the output textures
                    cmd.SetComputeTextureParam(bilateralUpsampleCS, m_BilateralUpSampleColorTMKernel, HDShaderIDs._OutputUpscaledTexture, buffer0);

                    // Upscale the buffer to full resolution
                    cmd.DispatchCompute(bilateralUpsampleCS, m_BilateralUpSampleColorTMKernel, numTilesXHR, numTilesYHR, hdCamera.viewCount);
                }

                (RenderPipelineManager.currentPipeline as HDRenderPipeline).PushFullScreenDebugTexture(hdCamera, cmd, m_IndirectDiffuseBuffer0, FullScreenDebugMode.ScreenSpaceGlobalIllumination);
            }
        }
    }
}
