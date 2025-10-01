using System;
using System.Collections.Generic;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices; // AggressiveInlining

namespace UnityEngine.Rendering.Universal
{
    internal sealed class DepthOfFieldGaussianPostProcessPass : ScriptableRenderPass, IDisposable
    {
        public const string k_TargetName = "_DoFTarget";

        Material m_Material;
        Material m_MaterialCoc;
        bool m_IsValid;

        Experimental.Rendering.GraphicsFormat m_CoCFormat;

        // Settings
        public DepthOfField depthOfField { get; set; }

        // Input
        public TextureHandle sourceTexture { get; set; }

        // Output
        public TextureHandle destinationTexture { get; set; }

        public DepthOfFieldGaussianPostProcessPass(Shader shader)
        {
            this.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing - 1;
            this.profilingSampler = null;

            m_Material = PostProcessUtils.LoadShader(shader, passName);
            m_MaterialCoc = PostProcessUtils.LoadShader(shader, passName);
            m_IsValid = m_Material != null && m_MaterialCoc != null;

            // Depth of Field
            //
            // CoC
            // UUM-41070: We require `Linear | Render` but with the deprecated FormatUsage this was checking `Blend`
            // For now, we keep checking for `Blend` until the performance hit of doing the correct checks is evaluated
            if (SystemInfo.IsFormatSupported(Experimental.Rendering.GraphicsFormat.R16_UNorm, Experimental.Rendering.GraphicsFormatUsage.Blend))
                m_CoCFormat = Experimental.Rendering.GraphicsFormat.R16_UNorm;
            else if (SystemInfo.IsFormatSupported(Experimental.Rendering.GraphicsFormat.R16_SFloat, Experimental.Rendering.GraphicsFormatUsage.Blend))
                m_CoCFormat = Experimental.Rendering.GraphicsFormat.R16_SFloat;
            else // Expect CoC banding
                m_CoCFormat = Experimental.Rendering.GraphicsFormat.R8_UNorm;
        }

        public void Dispose()
        {
            CoreUtils.Destroy(m_Material);
            CoreUtils.Destroy(m_MaterialCoc);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsValid()
        {
            return m_IsValid;
        }

        private class DoFGaussianPassData
        {
            // Setup
            internal int downsample;
            internal RenderingData renderingData;
            internal Vector3 cocParams;
            internal bool highQualitySamplingValue;
            internal bool enableAlphaOutput;
            // Inputs
            internal TextureHandle sourceTexture;
            internal TextureHandle depthTexture;
            internal Material material;
            internal Material materialCoC;
            // Pass textures
            internal TextureHandle halfCoCTexture;
            internal TextureHandle fullCoCTexture;
            internal TextureHandle pingTexture;     // TODO: pingpong texture idiom is common, we should have a helper abstraction in core.
            internal TextureHandle pongTexture;
            internal RenderTargetIdentifier[] multipleRenderTargets = new RenderTargetIdentifier[2];
            // Output textures
            internal TextureHandle destination;
        };
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            Assertions.Assert.IsTrue(sourceTexture.IsValid(), $"Source texture must be set for DepthOfFieldGaussianPostProcessPass.");
            Assertions.Assert.IsTrue(destinationTexture.IsValid(), $"Destination texture must be set for DepthOfFieldGaussianPostProcessPass.");

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

            var srcDesc = sourceTexture.GetDescriptor(renderGraph);
            var colorFormat = srcDesc.colorFormat;

            int downSample = 2;
            int wh = srcDesc.width / downSample;
            int hh = srcDesc.height / downSample;

            // Pass Textures
            var fullCoCTextureDesc = PostProcessUtils.GetCompatibleDescriptor(srcDesc, srcDesc.width, srcDesc.height, m_CoCFormat);
            var fullCoCTexture = PostProcessUtils.CreateCompatibleTexture(renderGraph, fullCoCTextureDesc, "_FullCoCTexture", true, FilterMode.Bilinear);
            var halfCoCTextureDesc = PostProcessUtils.GetCompatibleDescriptor(srcDesc, wh, hh, m_CoCFormat);
            var halfCoCTexture = PostProcessUtils.CreateCompatibleTexture(renderGraph, halfCoCTextureDesc, "_HalfCoCTexture", true, FilterMode.Bilinear);
            var pingTextureDesc = PostProcessUtils.GetCompatibleDescriptor(srcDesc, wh, hh, colorFormat);
            var pingTexture = PostProcessUtils.CreateCompatibleTexture(renderGraph, pingTextureDesc, "_PingTexture", true, FilterMode.Bilinear);
            var pongTextureDesc = PostProcessUtils.GetCompatibleDescriptor(srcDesc, wh, hh, colorFormat);
            var pongTexture = PostProcessUtils.CreateCompatibleTexture(renderGraph, pongTextureDesc, "_PongTexture", true, FilterMode.Bilinear);

            using (var builder = renderGraph.AddUnsafePass<DoFGaussianPassData>("Depth of Field - Gaussian", out var passData))
            {
                // Setup
                float farStart = depthOfField.gaussianStart.value;
                float farEnd = Mathf.Max(farStart, depthOfField.gaussianEnd.value);

                // Assumes a radius of 1 is 1 at 1080p
                // Past a certain radius our gaussian kernel will look very bad so we'll clamp it for
                // very high resolutions (4K+).
                float maxRadius = depthOfField.gaussianMaxRadius.value * (wh / 1080f);
                maxRadius = Mathf.Min(maxRadius, 2f);

                passData.downsample = downSample;
                passData.cocParams = new Vector3(farStart, farEnd, maxRadius);
                passData.highQualitySamplingValue = depthOfField.highQualitySampling.value;
                passData.enableAlphaOutput = cameraData.isAlphaOutputEnabled;

                passData.material = m_Material;
                passData.materialCoC = m_MaterialCoc;

                // Inputs
                passData.sourceTexture = sourceTexture;
                builder.UseTexture(sourceTexture, AccessFlags.Read);

                passData.depthTexture = resourceData.cameraDepthTexture;
                builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);

                // Pass Textures
                passData.fullCoCTexture = fullCoCTexture;
                builder.UseTexture(fullCoCTexture, AccessFlags.ReadWrite);

                passData.halfCoCTexture = halfCoCTexture;
                builder.UseTexture(halfCoCTexture, AccessFlags.ReadWrite);

                passData.pingTexture = pingTexture;
                builder.UseTexture(pingTexture, AccessFlags.ReadWrite);

                passData.pongTexture = pongTexture;
                builder.UseTexture(pongTexture, AccessFlags.ReadWrite);

                // Outputs
                passData.destination = destinationTexture;
                builder.UseTexture(destinationTexture, AccessFlags.Write);

                builder.SetRenderFunc(static (DoFGaussianPassData data, UnsafeGraphContext context) =>
                {
                    var dofMat = data.material;
                    var dofMatCoC = data.materialCoC;   // TODO: is materialCoC needed here? It's setup the same way. Can we use the same material?
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);

                    RTHandle sourceTextureHdl = data.sourceTexture;
                    RTHandle dstHdl = data.destination;

                    // Setup Materials
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_SetupDoF)))
                    {
                        // Dof material
                        Vector4 sourceSize = PostProcessUtils.CalcShaderSourceSize(data.sourceTexture);

                        dofMat.SetVector(ShaderConstants._CoCParams, data.cocParams);
                        dofMat.SetVector(ShaderConstants._DownSampleScaleFactor, new Vector4(1.0f / data.downsample, 1.0f / data.downsample, data.downsample, data.downsample));
                        dofMat.SetVector(ShaderConstants._SourceSize, sourceSize);
                        // PostProcessUtils.SetGlobalShaderSourceSize(cmd, data.sourceTexture);

                        CoreUtils.SetKeyword(dofMat, ShaderKeywordStrings.HighQualitySampling, data.highQualitySamplingValue);
                        CoreUtils.SetKeyword(dofMat, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, data.enableAlphaOutput);

                        // Dof CoC material
                        dofMatCoC.SetVector(ShaderConstants._CoCParams, data.cocParams);
                        dofMatCoC.SetVector(ShaderConstants._SourceSize, sourceSize);

                        CoreUtils.SetKeyword(dofMatCoC, ShaderKeywordStrings.HighQualitySampling, data.highQualitySamplingValue);
                    }

                    // Compute CoC
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_DOFComputeCOC)))
                    {
                        dofMatCoC.SetTexture(ShaderConstants._CameraDepthTextureID, data.depthTexture);
                        Blitter.BlitCameraTexture(cmd, data.sourceTexture, data.fullCoCTexture, dofMatCoC, ShaderPass.k_ComputeCoc);
                    }

                    // Downscale & prefilter color + CoC
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_DOFDownscalePrefilter)))
                    {
                        dofMat.SetTexture(ShaderConstants._FullCoCTexture, data.fullCoCTexture);

                        // Handle packed shader output
                        data.multipleRenderTargets[0] = data.halfCoCTexture;
                        data.multipleRenderTargets[1] = data.pingTexture;
                        CoreUtils.SetRenderTarget(cmd, data.multipleRenderTargets, data.halfCoCTexture);

                        Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                        Blitter.BlitTexture(cmd, data.sourceTexture, viewportScale, dofMat, ShaderPass.k_DownscalePrefilter);
                    }

                    // Blur H
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_DOFBlurH)))
                    {
                        dofMat.SetTexture(ShaderConstants._HalfCoCTexture, data.halfCoCTexture);
                        Blitter.BlitCameraTexture(cmd, data.pingTexture, data.pongTexture, dofMat, ShaderPass.k_BlurH);
                    }

                    // Blur V
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_DOFBlurV)))
                    {
                        Blitter.BlitCameraTexture(cmd, data.pongTexture, data.pingTexture, dofMat, ShaderPass.k_BlurV);
                    }

                    // Composite
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_DOFComposite)))
                    {
                        dofMat.SetTexture(ShaderConstants._ColorTexture, data.pingTexture);
                        dofMat.SetTexture(ShaderConstants._FullCoCTexture, data.fullCoCTexture);
                        Blitter.BlitCameraTexture(cmd, sourceTextureHdl, dstHdl, dofMat, ShaderPass.k_Composite);
                    }
                });
            }
        }

        // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
        public static class ShaderConstants
        {
            public static readonly int _ColorTexture = Shader.PropertyToID("_ColorTexture");
            public static readonly int _CameraDepthTextureID = Shader.PropertyToID("_CameraDepthTexture");
            public static readonly int _SourceSize = Shader.PropertyToID("_SourceSize");

            public static readonly int _DownSampleScaleFactor = Shader.PropertyToID("_DownSampleScaleFactor");

            public static readonly int _CoCParams = Shader.PropertyToID("_CoCParams");
            public static readonly int _FullCoCTexture = Shader.PropertyToID("_FullCoCTexture");
            public static readonly int _HalfCoCTexture = Shader.PropertyToID("_HalfCoCTexture");
        }

        public static class ShaderPass
        {
            public const int k_ComputeCoc = 0;
            public const int k_DownscalePrefilter = 1;
            public const int k_BlurH = 2;
            public const int k_BlurV = 3;
            public const int k_Composite = 4;
        }
    }
}
