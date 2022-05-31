using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        TextureDesc GetVolumetricCloudsIntermediateLightingBufferDesc()
        {
            int skyResolution = (int)m_Asset.currentPlatformRenderPipelineSettings.lightLoopSettings.skyReflectionSize;
            return new TextureDesc(skyResolution, skyResolution, false, true)
            { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true };
        }

        TextureDesc GetVolumetricCloudsIntermediateDepthBufferDesc()
        {
            int skyResolution = (int)m_Asset.currentPlatformRenderPipelineSettings.lightLoopSettings.skyReflectionSize;
            return new TextureDesc(skyResolution, skyResolution, false, true)
            { colorFormat = GraphicsFormat.R32_SFloat, enableRandomWrite = true };
        }

        TextureDesc GetVolumetricCloudsIntermediateCubeTextureDesc()
        {
            int skyResolution = (int)m_Asset.currentPlatformRenderPipelineSettings.lightLoopSettings.skyReflectionSize;
            return new TextureDesc(skyResolution, skyResolution, false, false)
            { slices = TextureXR.slices, dimension = TextureDimension.Cube, colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, useMipMap = true, autoGenerateMips = false };
        }

        class VolumetricCloudsSkyLowPassData
        {
            // Resolution parameters
            public int traceWidth;
            public int traceHeight;
            public int intermediateWidth;
            public int intermediateHeight;
            public int finalWidth;
            public int finalHeight;

            // Used kernels
            public int renderKernel;
            public int preUpscaleKernel;
            public int finalUpscaleKernel;

            // Data common to all volumetric cloud passes
            public VolumetricCloudCommonData commonData;

            // Sky
            public Material cloudCombinePass;
            public CubemapFace cubemapFace;

            public Matrix4x4[] pixelCoordToViewDir;

            public TextureHandle intermediateLightingBuffer;
            public TextureHandle intermediateDepthBuffer;
            public TextureHandle output;
            public TextureHandle maxZMask;
            public ComputeBufferHandle ambientProbeBuffer;
        }

        void PrepareVolumetricCloudsSkyLowPassData(RenderGraph renderGraph, RenderGraphBuilder builder,
            HDCamera hdCamera, int width, int height, Matrix4x4[] pixelCoordToViewDir, CubemapFace cubemapFace,
            VolumetricClouds settings, ComputeBuffer ambientProbeBuffer, VolumetricCloudsSkyLowPassData data)
        {
            // Compute the cloud model data
            CloudModelData cloudModelData = GetCloudModelData(settings);

            // Fill the common data
            FillVolumetricCloudsCommonData(false, settings, TVolumetricCloudsCameraType.Sky, in cloudModelData, ref data.commonData);

            // We need to make sure that the allocated size of the history buffers and the dispatch size are perfectly equal.
            // The ideal approach would be to have a function for that returns the converted size from a viewport and texture size.
            // but for now we do it like this.
            // Final resolution at which the effect should be exported
            data.finalWidth = width;
            data.finalHeight = height;
            // Intermediate resolution at which the effect is accumulated
            data.intermediateWidth = Mathf.RoundToInt(0.5f * width);
            data.intermediateHeight = Mathf.RoundToInt(0.5f * height);
            // Resolution at which the effect is traced
            data.traceWidth = Mathf.RoundToInt(0.25f * width);
            data.traceHeight = Mathf.RoundToInt(0.25f * height);

            // Sky
            data.cubemapFace = cubemapFace;
            data.cloudCombinePass = m_CloudCombinePass;

            // Kernels
            data.renderKernel = m_CloudRenderKernel;
            data.preUpscaleKernel = m_PreUpscaleCloudsSkyKernel;
            data.finalUpscaleKernel = m_UpscaleAndCombineCloudsSkyKernel;

            data.pixelCoordToViewDir = pixelCoordToViewDir;

            // Update the constant buffer
            VolumetricCloudsCameraData cameraData;
            cameraData.cameraType = data.commonData.cameraType;
            cameraData.traceWidth = data.traceWidth;
            cameraData.traceHeight = data.traceHeight;
            cameraData.intermediateWidth = data.intermediateWidth;
            cameraData.intermediateHeight = data.intermediateHeight;
            cameraData.finalWidth = data.finalWidth;
            cameraData.finalHeight = data.finalHeight;
            cameraData.viewCount = 1;
            cameraData.enableExposureControl = data.commonData.enableExposureControl;
            cameraData.lowResolution = true;
            cameraData.enableIntegration = false;
            UpdateShaderVariableslClouds(ref data.commonData.cloudsCB, hdCamera, settings, cameraData, cloudModelData, false);

            int skyResolution = (int)m_Asset.currentPlatformRenderPipelineSettings.lightLoopSettings.skyReflectionSize;
            data.intermediateLightingBuffer = builder.CreateTransientTexture(GetVolumetricCloudsIntermediateLightingBufferDesc());
            data.intermediateDepthBuffer = builder.CreateTransientTexture(GetVolumetricCloudsIntermediateDepthBufferDesc());
            data.output = builder.WriteTexture(renderGraph.CreateTexture(GetVolumetricCloudsIntermediateCubeTextureDesc()));
            data.maxZMask = builder.ReadTexture(renderGraph.defaultResources.blackTextureXR);
            data.ambientProbeBuffer = builder.ReadComputeBuffer(renderGraph.ImportComputeBuffer(ambientProbeBuffer));
        }

        static void TraceVolumetricClouds_Sky_Low(CommandBuffer cmd, VolumetricCloudsSkyLowPassData passData, MaterialPropertyBlock mpb)
        {
            // Compute the number of tiles to evaluate
            int traceTX = (passData.traceWidth + (8 - 1)) / 8;
            int traceTY = (passData.traceHeight + (8 - 1)) / 8;

            // Bind the sampling textures
            BlueNoise.BindDitheredTextureSet(cmd, passData.commonData.ditheredTextureSet);

            // Bind the constant buffer
            ConstantBuffer.Push(cmd, passData.commonData.cloudsCB, passData.commonData.volumetricCloudsCS, HDShaderIDs._ShaderVariablesClouds);

            CoreUtils.SetKeyword(cmd, "LOCAL_VOLUMETRIC_CLOUDS", false);

            // Ray-march the clouds for this frame
            CoreUtils.SetKeyword(cmd, "PHYSICALLY_BASED_SUN", passData.commonData.cloudsCB._PhysicallyBasedSun == 1);
            cmd.SetComputeTextureParam(passData.commonData.volumetricCloudsCS, passData.renderKernel, HDShaderIDs._MaxZMaskTexture, passData.maxZMask);
            cmd.SetComputeTextureParam(passData.commonData.volumetricCloudsCS, passData.renderKernel, HDShaderIDs._Worley128RGBA, passData.commonData.worley128RGBA);
            cmd.SetComputeTextureParam(passData.commonData.volumetricCloudsCS, passData.renderKernel, HDShaderIDs._ErosionNoise, passData.commonData.erosionNoise);
            cmd.SetComputeTextureParam(passData.commonData.volumetricCloudsCS, passData.renderKernel, HDShaderIDs._CloudMapTexture, passData.commonData.cloudMapTexture);
            cmd.SetComputeTextureParam(passData.commonData.volumetricCloudsCS, passData.renderKernel, HDShaderIDs._CloudLutTexture, passData.commonData.cloudLutTexture);
            cmd.SetComputeBufferParam(passData.commonData.volumetricCloudsCS, passData.renderKernel, HDShaderIDs._VolumetricCloudsAmbientProbeBuffer, passData.ambientProbeBuffer);

            // Output buffers
            cmd.SetComputeTextureParam(passData.commonData.volumetricCloudsCS, passData.renderKernel, HDShaderIDs._CloudsLightingTextureRW, passData.intermediateLightingBuffer);
            cmd.SetComputeTextureParam(passData.commonData.volumetricCloudsCS, passData.renderKernel, HDShaderIDs._CloudsDepthTextureRW, passData.intermediateDepthBuffer);

            cmd.DispatchCompute(passData.commonData.volumetricCloudsCS, passData.renderKernel, traceTX, traceTY, 1);
            CoreUtils.SetKeyword(cmd, "PHYSICALLY_BASED_SUN", false);

            mpb.SetTexture(HDShaderIDs._VolumetricCloudsUpscaleTextureRW, passData.intermediateLightingBuffer);
            CoreUtils.SetRenderTarget(cmd, passData.output, ClearFlag.None, miplevel: 2, cubemapFace: passData.cubemapFace);
            CoreUtils.DrawFullScreen(cmd, passData.cloudCombinePass, mpb, 3);
        }

        class VolumetricCloudsSkyHighPassData
        {
            // Resolution parameters
            public int finalWidth;
            public int finalHeight;

            // Compute shader and kernels
            public int renderKernel;
            public int combineKernel;

            // Data common to all volumetric cloud passes
            public VolumetricCloudCommonData commonData;

            // Sky
            public bool renderForCubeMap;
            public CubemapFace cubemapFace;
            public Material cloudCombinePass;

            public Matrix4x4[] pixelCoordToViewDir;

            public TextureHandle intermediateLightingBuffer0;
            public TextureHandle intermediateLightingBuffer1;
            public TextureHandle intermediateDepthBuffer;
            public TextureHandle output;
            public TextureHandle maxZMask;
            public ComputeBufferHandle ambientProbeBuffer;
        }

        void PrepareVolumetricCloudsSkyHighPassData(RenderGraph renderGraph, RenderGraphBuilder builder,
            HDCamera hdCamera, int width, int height, Matrix4x4[] pixelCoordToViewDir, CubemapFace cubemapFace,
            VolumetricClouds settings, ComputeBuffer ambientProbeBuffer,
            TextureHandle output, VolumetricCloudsSkyHighPassData data)
        {
            // Compute the cloud model data
            CloudModelData cloudModelData = GetCloudModelData(settings);

            // Fill the common data
            FillVolumetricCloudsCommonData(false, settings, TVolumetricCloudsCameraType.Sky, in cloudModelData, ref data.commonData);

            // If this is a baked reflection, we run everything at full res
            data.finalWidth = width;
            data.finalHeight = height;

            // Sky
            data.cubemapFace = cubemapFace;
            data.cloudCombinePass = m_CloudCombinePass;

            // Compute shader and kernels
            data.renderKernel = m_CloudRenderKernel;
            data.combineKernel = m_CombineCloudsSkyKernel;

            data.pixelCoordToViewDir = pixelCoordToViewDir;

            // Update the constant buffer
            VolumetricCloudsCameraData cameraData;
            cameraData.cameraType = data.commonData.cameraType;
            cameraData.traceWidth = data.finalWidth;
            cameraData.traceHeight = data.finalHeight;
            cameraData.intermediateWidth = data.finalWidth;
            cameraData.intermediateHeight = data.finalHeight;
            cameraData.finalWidth = data.finalWidth;
            cameraData.finalHeight = data.finalHeight;
            cameraData.viewCount = 1;
            cameraData.enableExposureControl = data.commonData.enableExposureControl;
            cameraData.lowResolution = false;
            cameraData.enableIntegration = false;
            UpdateShaderVariableslClouds(ref data.commonData.cloudsCB, hdCamera, settings, cameraData, cloudModelData, false);

            int skyResolution = (int)m_Asset.currentPlatformRenderPipelineSettings.lightLoopSettings.skyReflectionSize;
            data.intermediateLightingBuffer0 = builder.CreateTransientTexture(GetVolumetricCloudsIntermediateLightingBufferDesc());
            data.intermediateDepthBuffer = builder.CreateTransientTexture(GetVolumetricCloudsIntermediateDepthBufferDesc());
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
            {
                data.intermediateLightingBuffer1 = builder.CreateTransientTexture(GetVolumetricCloudsIntermediateLightingBufferDesc());
                data.output = builder.ReadWriteTexture(output);
            }
            else
            {
                data.output = builder.WriteTexture(output);
            }
            data.maxZMask = builder.ReadTexture(renderGraph.defaultResources.blackTextureXR);
            data.ambientProbeBuffer = builder.ReadComputeBuffer(renderGraph.ImportComputeBuffer(ambientProbeBuffer));
        }

        static void RenderVolumetricClouds_Sky_High(CommandBuffer cmd, VolumetricCloudsSkyHighPassData passData, MaterialPropertyBlock mpb)
        {
            // Compute the number of tiles to evaluate
            int finalTX = (passData.finalWidth + (8 - 1)) / 8;
            int finalTY = (passData.finalHeight + (8 - 1)) / 8;

            // Bind the sampling textures
            BlueNoise.BindDitheredTextureSet(cmd, passData.commonData.ditheredTextureSet);

            // Set the multi compile
            CoreUtils.SetKeyword(cmd, "LOCAL_VOLUMETRIC_CLOUDS", false);

            // Bind the constant buffer
            ConstantBuffer.Push(cmd, passData.commonData.cloudsCB, passData.commonData.volumetricCloudsCS, HDShaderIDs._ShaderVariablesClouds);

            // Ray-march the clouds for this frame
            CoreUtils.SetKeyword(cmd, "PHYSICALLY_BASED_SUN", passData.commonData.cloudsCB._PhysicallyBasedSun == 1);
            cmd.SetComputeTextureParam(passData.commonData.volumetricCloudsCS, passData.renderKernel, HDShaderIDs._MaxZMaskTexture, passData.maxZMask);
            cmd.SetComputeTextureParam(passData.commonData.volumetricCloudsCS, passData.renderKernel, HDShaderIDs._Worley128RGBA, passData.commonData.worley128RGBA);
            cmd.SetComputeTextureParam(passData.commonData.volumetricCloudsCS, passData.renderKernel, HDShaderIDs._ErosionNoise, passData.commonData.erosionNoise);
            cmd.SetComputeTextureParam(passData.commonData.volumetricCloudsCS, passData.renderKernel, HDShaderIDs._CloudMapTexture, passData.commonData.cloudMapTexture);
            cmd.SetComputeTextureParam(passData.commonData.volumetricCloudsCS, passData.renderKernel, HDShaderIDs._CloudLutTexture, passData.commonData.cloudLutTexture);
            cmd.SetComputeBufferParam(passData.commonData.volumetricCloudsCS, passData.renderKernel, HDShaderIDs._VolumetricCloudsAmbientProbeBuffer, passData.ambientProbeBuffer);

            // Output buffers
            cmd.SetComputeTextureParam(passData.commonData.volumetricCloudsCS, passData.renderKernel, HDShaderIDs._CloudsLightingTextureRW, passData.intermediateLightingBuffer0);
            cmd.SetComputeTextureParam(passData.commonData.volumetricCloudsCS, passData.renderKernel, HDShaderIDs._CloudsDepthTextureRW, passData.intermediateDepthBuffer);

            // Trace the clouds
            cmd.DispatchCompute(passData.commonData.volumetricCloudsCS, passData.renderKernel, finalTX, finalTY, 1);

            // Reset the multi compile
            CoreUtils.SetKeyword(cmd, "PHYSICALLY_BASED_SUN", false);

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
            {
                // On Intel GPUs on OSX, due to the fact that we cannot always rely on pre-exposure the hardware blending fails and turns into Nans when
                // the values are close to the max fp16 value. We do the blending manually on metal to avoid that behavior.
                // Copy the target face of the cubemap into a temporary texture
                cmd.CopyTexture(passData.output, (int)passData.cubemapFace, 0, passData.intermediateLightingBuffer1, 0, 0);

                // Output the result into the output buffer
                mpb.SetTexture(HDShaderIDs._CameraColorTexture, passData.intermediateLightingBuffer1);
                mpb.SetTexture(HDShaderIDs._VolumetricCloudsUpscaleTextureRW, passData.intermediateLightingBuffer0);
                CoreUtils.SetRenderTarget(cmd, passData.output, ClearFlag.None, 0, passData.cubemapFace);
                CoreUtils.DrawFullScreen(cmd, passData.cloudCombinePass, mpb, 1);
            }
            else
            {
                // Output the result into the output buffer
                mpb.SetTexture(HDShaderIDs._VolumetricCloudsUpscaleTextureRW, passData.intermediateLightingBuffer0);
                CoreUtils.SetRenderTarget(cmd, passData.output, ClearFlag.None, 0, passData.cubemapFace);
                CoreUtils.DrawFullScreen(cmd, passData.cloudCombinePass, mpb, 2);
            }
        }

        class VolumetricCloudsPreUpscalePassData
        {
            public Material cloudCombinePass;
            public TextureHandle input;
            public TextureHandle output;
            public Matrix4x4[] pixelCoordToViewDir;
        }

        class VolumetricCloudsUpscalePassData
        {
            public Material cloudCombinePass;
            public TextureHandle input;
            public TextureHandle intermediateBuffer;
            public TextureHandle output;
            public Matrix4x4[] pixelCoordToViewDir;
        }

        internal TextureHandle RenderVolumetricClouds_Sky(RenderGraph renderGraph, HDCamera hdCamera, Matrix4x4[] pixelCoordToViewDir, VolumetricClouds settings, int width, int height, ComputeBuffer probeBuffer, TextureHandle skyboxCubemap)
        {
            // If the current volume does not enable the feature, quit right away.
            if (!HasVolumetricClouds(hdCamera, in settings))
                return skyboxCubemap;

            if (hdCamera.frameSettings.IsEnabled(FrameSettingsField.FullResolutionCloudsForSky))
            {
                using (var builder = renderGraph.AddRenderPass<VolumetricCloudsSkyHighPassData>("FullResolutionCloudsForSky", out var passData, ProfilingSampler.Get(HDProfileId.VolumetricCloudsTrace)))
                {
                    PrepareVolumetricCloudsSkyHighPassData(renderGraph, builder, hdCamera, width, height, pixelCoordToViewDir, CubemapFace.Unknown, settings, probeBuffer, skyboxCubemap, passData);

                    builder.SetRenderFunc(
                    (VolumetricCloudsSkyHighPassData data, RenderGraphContext ctx) =>
                    {
                        for (int faceIdx = 0; faceIdx < 6; ++faceIdx)
                        {
                            // Update the cubemap face and the inverse projection matrix
                            data.cubemapFace = (CubemapFace)faceIdx;
                            data.commonData.cloudsCB._CloudsPixelCoordToViewDirWS = data.pixelCoordToViewDir[faceIdx];
                            data.commonData.cloudsCB._ValidMaxZMask = 0;

                            // Render the face straight to the output cubemap
                            RenderVolumetricClouds_Sky_High(ctx.cmd, data, ctx.renderGraphPool.GetTempMaterialPropertyBlock());
                        }
                    });

                    return passData.output;
                }
            }
            else
            {
                TextureHandle intermediateCubemap;

                using (var builder = renderGraph.AddRenderPass<VolumetricCloudsSkyLowPassData>("LowResolutionCloudsForSky", out var passData, ProfilingSampler.Get(HDProfileId.VolumetricCloudsTrace)))
                {
                    PrepareVolumetricCloudsSkyLowPassData(renderGraph, builder, hdCamera, width, height, pixelCoordToViewDir, CubemapFace.Unknown, settings, probeBuffer, passData);

                    builder.SetRenderFunc(
                    (VolumetricCloudsSkyLowPassData data, RenderGraphContext ctx) =>
                    {
                        for (int faceIdx = 0; faceIdx < 6; ++faceIdx)
                        {
                            // Update the cubemap face and the inverse projection matrix
                            data.cubemapFace = (CubemapFace)faceIdx;
                            data.commonData.cloudsCB._CloudsPixelCoordToViewDirWS = data.pixelCoordToViewDir[faceIdx];
                            data.commonData.cloudsCB._ValidMaxZMask = 0;

                            // Render the face straight to the output cubemap
                            TraceVolumetricClouds_Sky_Low(ctx.cmd, data, ctx.renderGraphPool.GetTempMaterialPropertyBlock());
                        }
                    });

                    intermediateCubemap = passData.output;
                }

                using (var builder = renderGraph.AddRenderPass<VolumetricCloudsPreUpscalePassData>("VolumetricCloudsPreUpscale", out var passData, ProfilingSampler.Get(HDProfileId.VolumetricCloudsPreUpscale)))
                {
                    int skyResolution = (int)m_Asset.currentPlatformRenderPipelineSettings.lightLoopSettings.skyReflectionSize;

                    passData.cloudCombinePass = m_CloudCombinePass;
                    passData.pixelCoordToViewDir = pixelCoordToViewDir;
                    passData.input = builder.ReadTexture(intermediateCubemap);
                    passData.output = builder.WriteTexture(renderGraph.CreateTexture(GetVolumetricCloudsIntermediateCubeTextureDesc()));

                    builder.SetRenderFunc(
                    (VolumetricCloudsPreUpscalePassData data, RenderGraphContext ctx) =>
                    {
                        for (int faceIdx = 0; faceIdx < 6; ++faceIdx)
                        {
                            var mpb = ctx.renderGraphPool.GetTempMaterialPropertyBlock();
                            mpb.Clear();
                            mpb.SetTexture(HDShaderIDs._VolumetricCloudsTexture, data.input);
                            mpb.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, data.pixelCoordToViewDir[faceIdx]);
                            mpb.SetInt(HDShaderIDs._Mipmap, 2);
                            CoreUtils.SetRenderTarget(ctx.cmd, data.output, ClearFlag.None, 1, (CubemapFace)faceIdx);
                            CoreUtils.DrawFullScreen(ctx.cmd, data.cloudCombinePass, mpb, 4);
                        }
                    });

                    intermediateCubemap = passData.output;
                }

                using (var builder = renderGraph.AddRenderPass<VolumetricCloudsUpscalePassData>("VolumetricCloudsUpscaleAndCombine", out var passData, ProfilingSampler.Get(HDProfileId.VolumetricCloudsUpscaleAndCombine)))
                {
                    passData.cloudCombinePass = m_CloudCombinePass;
                    passData.pixelCoordToViewDir = pixelCoordToViewDir;
                    passData.input = builder.ReadTexture(intermediateCubemap);
                    if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
                    {
                        passData.intermediateBuffer = builder.CreateTransientTexture(GetVolumetricCloudsIntermediateLightingBufferDesc());
                        passData.output = builder.ReadWriteTexture(skyboxCubemap);
                    }
                    else
                    {
                        passData.output = builder.WriteTexture(skyboxCubemap);
                    }

                    builder.SetRenderFunc(
                    (VolumetricCloudsUpscalePassData data, RenderGraphContext ctx) =>
                    {
                        for (int faceIdx = 0; faceIdx < 6; ++faceIdx)
                        {
                            var mpb = ctx.renderGraphPool.GetTempMaterialPropertyBlock();

                            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
                            {
                                // On Intel GPUs on OSX, due to the fact that we cannot always rely on pre-exposure the hardware blending fails and turns into Nans when
                                // the values are close to the max fp16 value. We do the blending manually on metal to avoid that behavior.
                                // Copy the target face of the cubemap into a temporary texture
                                ctx.cmd.CopyTexture(data.output, faceIdx, 0, data.intermediateBuffer, 0, 0);

                                mpb.Clear();
                                mpb.SetTexture(HDShaderIDs._CameraColorTexture, data.intermediateBuffer);
                                mpb.SetTexture(HDShaderIDs._VolumetricCloudsTexture, data.input);
                                mpb.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, data.pixelCoordToViewDir[faceIdx]);
                                mpb.SetInt(HDShaderIDs._Mipmap, 1);
                                CoreUtils.SetRenderTarget(ctx.cmd, data.output, ClearFlag.None, 0, (CubemapFace)faceIdx);
                                CoreUtils.DrawFullScreen(ctx.cmd, data.cloudCombinePass, mpb, 5);
                            }
                            else
                            {
                                mpb.Clear();
                                mpb.SetTexture(HDShaderIDs._VolumetricCloudsTexture, data.input);
                                mpb.SetMatrix(HDShaderIDs._PixelCoordToViewDirWS, data.pixelCoordToViewDir[faceIdx]);
                                mpb.SetInt(HDShaderIDs._Mipmap, 1);
                                CoreUtils.SetRenderTarget(ctx.cmd, data.output, ClearFlag.None, 0, (CubemapFace)faceIdx);
                                CoreUtils.DrawFullScreen(ctx.cmd, data.cloudCombinePass, mpb, 6);
                            }
                        }
                    });

                    return passData.output;
                }
            }
        }
    }
}
