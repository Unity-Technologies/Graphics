using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class VolumetricCloudsSystem
    {
        int skyReflectionSize => (int)m_RenderPipeline.asset.currentPlatformRenderPipelineSettings.lightLoopSettings.skyReflectionSize;

        TextureDesc GetVolumetricCloudsIntermediateLightingBufferDesc()
        {
            return new TextureDesc(skyReflectionSize, skyReflectionSize, false, true)
            { format = GraphicsFormat.B10G11R11_UFloatPack32, enableRandomWrite = true };
        }

        TextureDesc GetVolumetricCloudsIntermediateDepthBufferDesc()
        {
            return new TextureDesc(skyReflectionSize, skyReflectionSize, false, true)
            { format = GraphicsFormat.R16G16_SFloat, enableRandomWrite = true };
        }

        TextureDesc GetVolumetricCloudsIntermediateCubeTextureDesc()
        {
            return new TextureDesc(skyReflectionSize, skyReflectionSize, false, false)
            { dimension = TextureDimension.Cube, format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, useMipMap = true, autoGenerateMips = false };
        }

        TextureDesc GetVolumetricCloudsMetalCopyBufferDesc()
        {
            return new TextureDesc(skyReflectionSize, skyReflectionSize, false, true)
            { format = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = false };
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

            // Data common to all volumetric cloud passes
            public VolumetricCloudCommonData commonData;

            // Sky
            public Material cloudCombinePass;
            public CubemapFace cubemapFace;

            public Matrix4x4[] pixelCoordToViewDir;

            public TextureHandle intermediateLightingBuffer;
            public TextureHandle intermediateDepthBuffer;
            public TextureHandle output;
            public BufferHandle ambientProbeBuffer;
        }

        void PrepareVolumetricCloudsSkyLowPassData(RenderGraph renderGraph, RenderGraphBuilder builder,
            HDCamera hdCamera, int width, int height, Matrix4x4[] pixelCoordToViewDir, CubemapFace cubemapFace,
            VolumetricClouds settings, GraphicsBuffer ambientProbeBuffer, VolumetricCloudsSkyLowPassData data)
        {
            // Compute the cloud model data
            CloudModelData cloudModelData = GetCloudModelData(settings);

            // Fill the common data
            FillVolumetricCloudsCommonData(hdCamera, false, settings, TVolumetricCloudsCameraType.Sky, in cloudModelData, ref data.commonData);

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
            cameraData.enableExposureControl = data.commonData.enableExposureControl;
            cameraData.lowResolution = true;
            cameraData.enableIntegration = false;
            UpdateShaderVariablesClouds(ref data.commonData.cloudsCB, hdCamera, settings, cameraData, cloudModelData, false);

            data.intermediateLightingBuffer = builder.CreateTransientTexture(GetVolumetricCloudsIntermediateLightingBufferDesc());
            data.intermediateDepthBuffer = builder.CreateTransientTexture(GetVolumetricCloudsIntermediateDepthBufferDesc());
            data.output = builder.WriteTexture(renderGraph.CreateTexture(GetVolumetricCloudsIntermediateCubeTextureDesc()));
            data.ambientProbeBuffer = builder.ReadBuffer(renderGraph.ImportBuffer(ambientProbeBuffer));
        }

        static void TraceVolumetricClouds_Sky_Low(CommandBuffer cmd, VolumetricCloudsSkyLowPassData passData, MaterialPropertyBlock mpb)
        {
            // Compute the number of tiles to evaluate
            int traceTX = HDUtils.DivRoundUp(passData.traceWidth, 8);
            int traceTY = HDUtils.DivRoundUp(passData.traceHeight, 8);

            // Bind the sampling textures
            BlueNoise.BindDitheredTextureSet(cmd, passData.commonData.ditheredTextureSet);

            // Bind the constant buffer
            ConstantBuffer.UpdateData(cmd, passData.commonData.cloudsCB);
            ConstantBuffer.Set<ShaderVariablesClouds>(passData.commonData.volumetricCloudsCS, HDShaderIDs._ShaderVariablesClouds);
            ConstantBuffer.Set<ShaderVariablesClouds>(passData.commonData.volumetricCloudsTraceCS, HDShaderIDs._ShaderVariablesClouds);

            // Ray-march the clouds for this frame
            DoVolumetricCloudsTrace(cmd, traceTX, traceTY, 1, in passData.commonData,
                passData.ambientProbeBuffer, TextureXR.GetBlackTextureArray(), TextureXR.GetBlackTextureArray(),
                passData.intermediateLightingBuffer, passData.intermediateDepthBuffer);

            mpb.SetTexture(HDShaderIDs._VolumetricCloudsLightingTexture, passData.intermediateLightingBuffer);
            mpb.SetTexture(HDShaderIDs._VolumetricCloudsDepthTexture, passData.intermediateDepthBuffer);
            CoreUtils.SetRenderTarget(cmd, passData.output, ClearFlag.None, miplevel: 2, cubemapFace: passData.cubemapFace);
            CoreUtils.DrawFullScreen(cmd, passData.cloudCombinePass, mpb, 3);
        }

        class VolumetricCloudsSkyHighPassData
        {
            // Resolution parameters
            public int finalWidth;
            public int finalHeight;

            // Data common to all volumetric cloud passes
            public VolumetricCloudCommonData commonData;

            // Sky
            public bool renderForCubeMap;
            public CubemapFace cubemapFace;
            public Material cloudCombinePass;

            public Matrix4x4[] pixelCoordToViewDir;

            public TextureHandle intermediateLightingBuffer;
            public TextureHandle cameraColorCopy;
            public TextureHandle intermediateDepthBuffer;
            public TextureHandle output;
            public BufferHandle ambientProbeBuffer;
        }

        void PrepareVolumetricCloudsSkyHighPassData(RenderGraph renderGraph, RenderGraphBuilder builder,
            HDCamera hdCamera, int width, int height, Matrix4x4[] pixelCoordToViewDir, CubemapFace cubemapFace,
            VolumetricClouds settings, GraphicsBuffer ambientProbeBuffer,
            TextureHandle output, VolumetricCloudsSkyHighPassData data)
        {
            // Compute the cloud model data
            CloudModelData cloudModelData = GetCloudModelData(settings);

            // Fill the common data
            FillVolumetricCloudsCommonData(hdCamera, false, settings, TVolumetricCloudsCameraType.Sky, in cloudModelData, ref data.commonData);

            // If this is a baked reflection, we run everything at full res
            data.finalWidth = width;
            data.finalHeight = height;

            // Sky
            data.cubemapFace = cubemapFace;
            data.cloudCombinePass = m_CloudCombinePass;

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
            cameraData.enableExposureControl = data.commonData.enableExposureControl;
            cameraData.lowResolution = false;
            cameraData.enableIntegration = false;
            UpdateShaderVariablesClouds(ref data.commonData.cloudsCB, hdCamera, settings, cameraData, cloudModelData, false);

            data.intermediateLightingBuffer = builder.CreateTransientTexture(GetVolumetricCloudsIntermediateLightingBufferDesc());
            data.intermediateDepthBuffer = builder.CreateTransientTexture(GetVolumetricCloudsIntermediateDepthBufferDesc());
            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
            {
                data.cameraColorCopy = builder.CreateTransientTexture(GetVolumetricCloudsMetalCopyBufferDesc());
                data.output = builder.ReadWriteTexture(output);
            }
            else
            {
                data.output = builder.WriteTexture(output);
            }
            data.ambientProbeBuffer = builder.ReadBuffer(renderGraph.ImportBuffer(ambientProbeBuffer));
        }

        static void RenderVolumetricClouds_Sky_High(CommandBuffer cmd, VolumetricCloudsSkyHighPassData passData, MaterialPropertyBlock mpb)
        {
            // Compute the number of tiles to evaluate
            int finalTX = (passData.finalWidth + (8 - 1)) / 8;
            int finalTY = (passData.finalHeight + (8 - 1)) / 8;

            // Bind the sampling textures
            BlueNoise.BindDitheredTextureSet(cmd, passData.commonData.ditheredTextureSet);

            // Bind the constant buffer
            ConstantBuffer.Push(cmd, passData.commonData.cloudsCB, passData.commonData.volumetricCloudsTraceCS, HDShaderIDs._ShaderVariablesClouds);

            // Ray-march the clouds for this frame
            DoVolumetricCloudsTrace(cmd, finalTX, finalTY, 1, in passData.commonData,
                passData.ambientProbeBuffer, TextureXR.GetBlackTextureArray(), TextureXR.GetBlackTextureArray(),
                passData.intermediateLightingBuffer, passData.intermediateDepthBuffer);

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
            {
                // On Intel GPUs on OSX, due to the fact that we cannot always rely on pre-exposure the hardware blending fails and turns into Nans when
                // the values are close to the max fp16 value. We do the blending manually on metal to avoid that behavior.
                // Copy the target face of the cubemap into a temporary texture
                cmd.CopyTexture(passData.output, (int)passData.cubemapFace, 0, passData.cameraColorCopy, 0, 0);

                // Output the result into the output buffer
                mpb.SetTexture(HDShaderIDs._CameraColorTexture, passData.cameraColorCopy);
                mpb.SetTexture(HDShaderIDs._VolumetricCloudsLightingTexture, passData.intermediateLightingBuffer);
                mpb.SetTexture(HDShaderIDs._VolumetricCloudsDepthTexture, passData.intermediateDepthBuffer);
                CoreUtils.SetRenderTarget(cmd, passData.output, ClearFlag.None, 0, passData.cubemapFace);
                CoreUtils.DrawFullScreen(cmd, passData.cloudCombinePass, mpb, 1);
            }
            else
            {
                // Output the result into the output buffer
                mpb.SetTexture(HDShaderIDs._VolumetricCloudsLightingTexture, passData.intermediateLightingBuffer);
                mpb.SetTexture(HDShaderIDs._VolumetricCloudsDepthTexture, passData.intermediateDepthBuffer);
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

        unsafe internal void UpdatePixelCoordToViewDir(ref ShaderVariablesClouds cb, in Matrix4x4 pixelCoordToViewDir)
        {
            for (int j = 0; j < 16; ++j)
                cb._CloudsPixelCoordToViewDirWS[j] = pixelCoordToViewDir[j];
        }

        internal void RenderVolumetricClouds_Sky(RenderGraph renderGraph, HDCamera hdCamera, Matrix4x4[] pixelCoordToViewDir, VolumetricClouds settings, SkyRenderer skyRenderer,
            int width, int height, GraphicsBuffer probeBuffer, TextureHandle skyboxCubemap)
        {
            // If the current volume does not enable the feature, quit right away.
            if (!HasVolumetricClouds(hdCamera, in settings))
                return;

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
                            UpdatePixelCoordToViewDir(ref data.commonData.cloudsCB, data.pixelCoordToViewDir[faceIdx]);

                            // Render the face straight to the output cubemap
                            RenderVolumetricClouds_Sky_High(ctx.cmd, data, ctx.renderGraphPool.GetTempMaterialPropertyBlock());
                        }
                    });
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
                            UpdatePixelCoordToViewDir(ref data.commonData.cloudsCB, data.pixelCoordToViewDir[faceIdx]);

                            // Render the face straight to the output cubemap
                            TraceVolumetricClouds_Sky_Low(ctx.cmd, data, ctx.renderGraphPool.GetTempMaterialPropertyBlock());
                        }
                    });

                    intermediateCubemap = passData.output;
                }

                using (var builder = renderGraph.AddRenderPass<VolumetricCloudsPreUpscalePassData>("VolumetricCloudsPreUpscale", out var passData, ProfilingSampler.Get(HDProfileId.VolumetricCloudsPreUpscale)))
                {
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

                using (var builder = renderGraph.AddRenderPass<VolumetricCloudsUpscalePassData>("VolumetricCloudsUpscale", out var passData, ProfilingSampler.Get(HDProfileId.VolumetricCloudsUpscale)))
                {
                    passData.cloudCombinePass = m_CloudCombinePass;
                    passData.pixelCoordToViewDir = pixelCoordToViewDir;
                    passData.input = builder.ReadTexture(intermediateCubemap);
                    if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.Metal)
                    {
                        passData.intermediateBuffer = builder.CreateTransientTexture(GetVolumetricCloudsMetalCopyBufferDesc());
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
                }
            }
        }
    }
}
