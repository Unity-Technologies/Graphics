using System;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices; // AggressiveInlining

namespace UnityEngine.Rendering.Universal
{
    internal sealed class DepthOfFieldBokehPostProcessPass : PostProcessPass
    {
        public const string k_TargetName = "CameraColorDepthOfFieldBokeh";
        const int k_DownSample = 2;

        Material m_Material;
        bool m_IsValid;

        Vector4[] m_BokehKernel;
        int m_BokehHash;

        // Needed if the device changes its render target width/height (ex, Mobile platform allows change of orientation)
        float m_BokehMaxRadius;
        float m_BokehRcpAspect;

        public DepthOfFieldBokehPostProcessPass(Shader shader)
        {
            this.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing - 1;
            this.profilingSampler = new ProfilingSampler("Blit Depth of Field (Bokeh)");

            m_Material = PostProcessUtils.LoadShader(shader, passName);
            m_IsValid = m_Material != null;
        }

        public override void Dispose()
        {
            CoreUtils.Destroy(m_Material);
            m_IsValid = false;
        }

        private class DoFBokehPassData
        {
            internal Material material;

            // Inputs
            internal TextureHandle sourceTexture;
            internal TextureHandle depthTexture;
            // Pass textures
            internal TextureHandle halfCoCTexture;
            internal TextureHandle fullCoCTexture;
            internal TextureHandle pingTexture;
            internal TextureHandle pongTexture;
            // Output texture
            internal TextureHandle destinationTexture;
            // Setup
            internal Vector4[] bokehKernel;
            internal Vector4 cocParams;
            internal int downSample;
            internal float uvMargin;
            internal bool useFastSRGBLinearConversion;
            internal bool enableAlphaOutput;
        };

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if (!m_IsValid)
                return;

            var depthOfField = volumeStack.GetComponent<DepthOfField>();
            if (!depthOfField.IsActive() || depthOfField.mode.value != DepthOfFieldMode.Bokeh)
                return;

            var cameraData = frameData.Get<UniversalCameraData>();
            if (cameraData.isSceneViewCamera)
                return;

            var postProcessingData = frameData.Get<UniversalPostProcessingData>();
            var resourceData = frameData.Get<UniversalResourceData>();

            var sourceTexture = resourceData.cameraColor;
            var destinationTexture = PostProcessUtils.CreateCompatibleTexture(renderGraph, sourceTexture, k_TargetName, true, FilterMode.Bilinear);
            var srcDesc = sourceTexture.GetDescriptor(renderGraph);

            int wh = srcDesc.width / k_DownSample;
            int hh = srcDesc.height / k_DownSample;

            // Pass Textures
            var fullCoCTextureDesc = PostProcessUtils.GetCompatibleDescriptor(srcDesc, srcDesc.width, srcDesc.height, Experimental.Rendering.GraphicsFormat.R8_UNorm);
            var fullCoCTexture = PostProcessUtils.CreateCompatibleTexture(renderGraph, fullCoCTextureDesc, "_FullCoCTexture", true, FilterMode.Bilinear);
            var pingTextureDesc = PostProcessUtils.GetCompatibleDescriptor(srcDesc, wh, hh, Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat);
            var pingTexture = PostProcessUtils.CreateCompatibleTexture(renderGraph, pingTextureDesc, "_PingTexture", true, FilterMode.Bilinear);
            var pongTextureDesc = PostProcessUtils.GetCompatibleDescriptor(srcDesc, wh, hh, Experimental.Rendering.GraphicsFormat.R16G16B16A16_SFloat);
            var pongTexture = PostProcessUtils.CreateCompatibleTexture(renderGraph, pongTextureDesc, "_PongTexture", true, FilterMode.Bilinear);

            using (var builder = renderGraph.AddUnsafePass<DoFBokehPassData>(passName, out var passData, profilingSampler))
            {
                // Setup
                // "A Lens and Aperture Camera Model for Synthetic Image Generation" [Potmesil81]
                float F = depthOfField.focalLength.value / 1000f;
                float A = depthOfField.focalLength.value / depthOfField.aperture.value;
                float P = depthOfField.focusDistance.value;
                float maxCoC = (A * F) / (P - F);
                float maxRadius = GetMaxBokehRadiusInPixels(srcDesc.height);
                float rcpAspect = 1f / (wh / (float)hh);

                // Prepare the bokeh kernel constant buffer
                int hash = depthOfField.GetHashCode();
                if (hash != m_BokehHash || maxRadius != m_BokehMaxRadius || rcpAspect != m_BokehRcpAspect)
                {
                    m_BokehHash = hash;
                    m_BokehMaxRadius = maxRadius;
                    m_BokehRcpAspect = rcpAspect;
                    PrepareBokehKernel( ref m_BokehKernel,
                                        depthOfField.bladeCount.value,
                                        depthOfField.bladeCurvature.value,
                                        depthOfField.bladeRotation.value,
                                        maxRadius, rcpAspect);
                }
                float uvMargin = (1.0f / srcDesc.height) * k_DownSample;

                passData.bokehKernel = m_BokehKernel;
                passData.downSample = k_DownSample;
                passData.uvMargin = uvMargin;
                passData.cocParams = new Vector4(P, maxCoC, maxRadius, rcpAspect);
                passData.useFastSRGBLinearConversion = postProcessingData.useFastSRGBLinearConversion;
                passData.enableAlphaOutput = cameraData.isAlphaOutputEnabled;

                // Inputs
                passData.sourceTexture = sourceTexture;
                builder.UseTexture(sourceTexture, AccessFlags.Read);

                passData.depthTexture = resourceData.cameraDepthTexture;
                builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);

                passData.material = m_Material;

                // Pass Textures
                passData.fullCoCTexture = fullCoCTexture;
                builder.UseTexture(fullCoCTexture, AccessFlags.ReadWrite);
                passData.pingTexture = pingTexture;
                builder.UseTexture(pingTexture, AccessFlags.ReadWrite);
                passData.pongTexture = pongTexture;
                builder.UseTexture(pongTexture, AccessFlags.ReadWrite);

                // Outputs
                passData.destinationTexture = destinationTexture;
                builder.UseTexture(destinationTexture, AccessFlags.Write);

                // TODO RENDERGRAPH: properly setup dependencies between passes
                builder.SetRenderFunc(static (DoFBokehPassData data, UnsafeGraphContext context) =>
                {
                    var dofMat = data.material;
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
                    RTHandle sourceTextureHdl = data.sourceTexture;
                    RTHandle dst = data.destinationTexture;

                    // Setup
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_SetupDoF)))
                    {
                        Vector4 sourceSize = PostProcessUtils.CalcShaderSourceSize(data.sourceTexture);

                        dofMat.SetVector(ShaderConstants._CoCParams, data.cocParams);
                        dofMat.SetVectorArray(ShaderConstants._BokehKernel, data.bokehKernel);
                        dofMat.SetVector(ShaderConstants._DownSampleScaleFactor, new Vector4(1.0f / data.downSample, 1.0f / data.downSample, data.downSample,data.downSample));
                        dofMat.SetVector(ShaderConstants._BokehConstants, new Vector4(data.uvMargin, data.uvMargin * 2.0f));
                        dofMat.SetVector(ShaderConstants._SourceSize, sourceSize);

                        CoreUtils.SetKeyword(dofMat, ShaderKeywordStrings.UseFastSRGBLinearConversion, data.useFastSRGBLinearConversion);
                        CoreUtils.SetKeyword(dofMat, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, data.enableAlphaOutput);
                    }

                    // Compute CoC
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_DOFComputeCOC)))
                    {
                        dofMat.SetTexture(ShaderConstants._CameraDepthTextureID, data.depthTexture);
                        Blitter.BlitCameraTexture(cmd, sourceTextureHdl, data.fullCoCTexture, dofMat, ShaderPass.k_ComputeCoc);
                    }

                    // Downscale and Prefilter Color + CoC
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_DOFDownscalePrefilter)))
                    {
                        dofMat.SetTexture(ShaderConstants._FullCoCTexture, data.fullCoCTexture);
                        Blitter.BlitCameraTexture(cmd, sourceTextureHdl, data.pingTexture, dofMat, ShaderPass.k_DownscalePrefilter);
                    }

                    // Blur
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_DOFBlurBokeh)))
                    {
                        Blitter.BlitCameraTexture(cmd, data.pingTexture, data.pongTexture, dofMat, ShaderPass.k_Blur);
                    }

                    // Post Filtering
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_DOFPostFilter)))
                    {
                        Blitter.BlitCameraTexture(cmd, data.pongTexture, data.pingTexture, dofMat, ShaderPass.k_PostFilter);
                    }

                    // Composite
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_DOFComposite)))
                    {
                        dofMat.SetTexture(ShaderConstants._DofTexture, data.pingTexture);
                        Blitter.BlitCameraTexture(cmd, sourceTextureHdl, dst, dofMat, ShaderPass.k_Composite);
                    }
                });
            }

            resourceData.cameraColor = destinationTexture;
        }

        static void PrepareBokehKernel(ref Vector4[] bokehKernel, int bladeCount, float bladeCurvature, float bladeRotation, float maxRadius, float rcpAspect)
        {
            const int kRings = 4;
            const int kPointsPerRing = 7;

            // Check the existing array
            if (bokehKernel == null)
                bokehKernel = new Vector4[42];

            // Fill in sample points (concentric circles transformed to rotated N-Gon)
            int idx = 0;
            float bladeCountf = bladeCount;
            float curvature = 1f - bladeCurvature;
            float rotation = bladeRotation * Mathf.Deg2Rad;
            const float PI = Mathf.PI;
            const float TWO_PI = Mathf.PI * 2f;

            for (int ring = 1; ring < kRings; ring++)
            {
                float bias = 1f / kPointsPerRing;
                float radius = (ring + bias) / (kRings - 1f + bias);
                int points = ring * kPointsPerRing;

                for (int point = 0; point < points; point++)
                {
                    // Angle on ring
                    float phi = 2f * PI * point / points;

                    // Transform to rotated N-Gon
                    // Adapted from "CryEngine 3 Graphics Gems" [Sousa13]
                    float nt = Mathf.Cos(PI / bladeCountf);
                    float dt = Mathf.Cos(phi - (TWO_PI / bladeCountf) * Mathf.Floor((bladeCountf * phi + Mathf.PI) / TWO_PI));
                    float r = radius * Mathf.Pow(nt / dt, curvature);
                    float u = r * Mathf.Cos(phi - rotation);
                    float v = r * Mathf.Sin(phi - rotation);

                    float uRadius = u * maxRadius;
                    float vRadius = v * maxRadius;
                    float uRadiusPowTwo = uRadius * uRadius;
                    float vRadiusPowTwo = vRadius * vRadius;
                    float kernelLength = Mathf.Sqrt((uRadiusPowTwo + vRadiusPowTwo));
                    float uRCP = uRadius * rcpAspect;

                    bokehKernel[idx] = new Vector4(uRadius, vRadius, kernelLength, uRCP);
                    idx++;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float GetMaxBokehRadiusInPixels(float viewportHeight)
        {
            // Estimate the maximum radius of bokeh (empirically derived from the ring count)
            const float kRadiusInPixels = 14f;
            return Mathf.Min(0.05f, kRadiusInPixels / viewportHeight);
        }

        // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
        public static class ShaderConstants
        {
            public static readonly int _CameraDepthTextureID = Shader.PropertyToID("_CameraDepthTexture");
            public static readonly int _SourceSize = Shader.PropertyToID("_SourceSize");

            public static readonly int _FullCoCTexture = Shader.PropertyToID("_FullCoCTexture");
            public static readonly int _DofTexture = Shader.PropertyToID("_DofTexture");
            public static readonly int _CoCParams = Shader.PropertyToID("_CoCParams");
            public static readonly int _BokehKernel = Shader.PropertyToID("_BokehKernel");
            public static readonly int _BokehConstants = Shader.PropertyToID("_BokehConstants");
            public static readonly int _DownSampleScaleFactor = Shader.PropertyToID("_DownSampleScaleFactor");
        }

        public static class ShaderPass
        {
            public const int k_ComputeCoc = 0;
            public const int k_DownscalePrefilter = 1;
            public const int k_Blur = 2;
            public const int k_PostFilter = 3;
            public const int k_Composite = 4;
        }
    }
}
