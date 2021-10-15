using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Resources required to evaluate the ambient probe for the clouds
        ComputeShader m_ComputeAmbientProbeCS;
        int m_AmbientProbeConvolutionNoMipKernel;
        internal static readonly int m_AmbientProbeInputCubemap = Shader.PropertyToID("_AmbientProbeInputCubemap");
        internal static readonly int m_AmbientProbeOutputBufferParam = Shader.PropertyToID("_AmbientProbeOutputBuffer");

        // Buffers required to evaluate the probe
        internal ComputeBuffer m_CloudsAmbientProbeBuffer = null;
        internal RTHandle m_CloudsAmbientProbeSky = null;

        // Structures to read back the probe from the GPU
        static SphericalHarmonicsL2 m_CloudsAmbientProbe = new SphericalHarmonicsL2();
        static bool m_CloudsAmbientProbeIsReady = false;

        void InitializeVolumetricCloudsAmbientProbe()
        {
            m_ComputeAmbientProbeCS = m_Asset.renderPipelineResources.shaders.ambientProbeConvolutionCS;
            m_AmbientProbeConvolutionNoMipKernel = m_ComputeAmbientProbeCS.FindKernel("AmbientProbeConvolutionNoMip");
            m_CloudsAmbientProbeBuffer = new ComputeBuffer(9 * 3, sizeof(float));
            m_CloudsAmbientProbeSky = RTHandles.Alloc(16, 16, TextureXR.slices, dimension: TextureDimension.Cube, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true);
        }

        void ReleaseVolumetricCloudsAmbientProbe()
        {
            CoreUtils.SafeRelease(m_CloudsAmbientProbeBuffer);
            RTHandles.Release(m_CloudsAmbientProbeSky);
        }

        // Function that fills the buffer with the ambient probe values
        unsafe void SetPreconvolvedAmbientLightProbe(ref ShaderVariablesClouds cb, VolumetricClouds settings)
        {
            if (m_CloudsAmbientProbeIsReady)
            {
                SphericalHarmonicsL2 probeSH = SphericalHarmonicMath.UndoCosineRescaling(m_CloudsAmbientProbe);
                probeSH = SphericalHarmonicMath.RescaleCoefficients(probeSH, settings.ambientLightProbeDimmer.value);
                ZonalHarmonicsL2.GetCornetteShanksPhaseFunction(m_PhaseZHClouds, 0.0f);
                SphericalHarmonicsL2 finalSH = SphericalHarmonicMath.PremultiplyCoefficients(SphericalHarmonicMath.Convolve(probeSH, m_PhaseZHClouds));
                SphericalHarmonicMath.PackCoefficients(m_PackedCoeffsProbe, finalSH);

                // Evaluate the probe at the top and bottom (above and under the clouds)
                cb._AmbientProbeTop = EvaluateAmbientProbe(m_PackedCoeffsProbe, Vector3.down);
                cb._AmbientProbeBottom = EvaluateAmbientProbe(m_PackedCoeffsProbe, Vector3.up);
            }
            else
            {
                cb._AmbientProbeTop = Vector4.zero;
                cb._AmbientProbeBottom = Vector4.zero;
            }
        }

        public static void OnComputeAmbientProbeDone(AsyncGPUReadbackRequest request)
        {
            if (!request.hasError)
            {
                var result = request.GetData<float>();
                for (int channel = 0; channel < 3; ++channel)
                    for (int coeff = 0; coeff < 9; ++coeff)
                        m_CloudsAmbientProbe[channel, coeff] = result[channel * 9 + coeff];

                m_CloudsAmbientProbeIsReady = true;
            }
        }

        class VolumetricCloudsAmbientProbeData
        {
            public ComputeShader computeProbeCS;
            public int kernel;
            public SkyManager skyManager;
            public SkyRenderer skyRenderer;
            public TextureHandle skyCubemap;
            public ComputeBufferHandle skyBuffer;
            public ComputeBufferHandle ambientProbeBuffer;
        }

        internal void PreRenderVolumetricClouds_AmbientProbe(RenderGraph renderGraph, HDCamera hdCamera)
        {
            using (var builder = renderGraph.AddRenderPass<VolumetricCloudsAmbientProbeData>("Volumetric Clouds Ambient Probe", out var passData, ProfilingSampler.Get(HDProfileId.VolumetricCloudsAmbientProbe)))
            {
                builder.EnableAsyncCompute(false);

                passData.computeProbeCS = m_ComputeAmbientProbeCS;
                passData.skyManager = m_SkyManager;
                passData.skyRenderer = hdCamera.lightingSky.skyRenderer;
                passData.skyCubemap = builder.ReadWriteTexture(renderGraph.ImportTexture(m_CloudsAmbientProbeSky));
                passData.kernel = m_AmbientProbeConvolutionNoMipKernel;
                passData.ambientProbeBuffer = builder.ReadComputeBuffer(builder.WriteComputeBuffer(renderGraph.ImportComputeBuffer(m_CloudsAmbientProbeBuffer)));

                builder.SetRenderFunc(
                    (VolumetricCloudsAmbientProbeData data, RenderGraphContext ctx) =>
                    {
                        if (data.skyRenderer != null)
                        {
                            // Render the sky into a low resolution cubemap
                            data.skyManager.RenderSkyOnlyToCubemap(ctx.cmd, data.skyCubemap, false, data.skyRenderer);

                            // Evaluate the probe
                            ctx.cmd.SetComputeTextureParam(data.computeProbeCS, data.kernel, m_AmbientProbeInputCubemap, data.skyCubemap);
                            ctx.cmd.SetComputeBufferParam(data.computeProbeCS, data.kernel, m_AmbientProbeOutputBufferParam, data.ambientProbeBuffer);
                            ctx.cmd.DispatchCompute(data.computeProbeCS, data.kernel, 1, 1, 1);

                            // Enqueue the read back
                            ctx.cmd.RequestAsyncReadback(data.ambientProbeBuffer, OnComputeAmbientProbeDone);
                        }
                        else
                        {
                            m_CloudsAmbientProbeIsReady = false;
                        }
                    });
            }
        }
    }
}
