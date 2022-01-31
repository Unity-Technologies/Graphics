using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Buffers required to evaluate the probe
        internal ComputeBuffer m_CloudsAmbientProbeBuffer = null;

        // Structures to read back the probe from the GPU
        static SphericalHarmonicsL2 m_CloudsAmbientProbe = new SphericalHarmonicsL2();
        static bool m_CloudsAmbientProbeIsReady = false;

        void InitializeVolumetricCloudsAmbientProbe()
        {
            m_CloudsAmbientProbeBuffer = new ComputeBuffer(9 * 3, sizeof(float));
        }

        void ReleaseVolumetricCloudsAmbientProbe()
        {
            CoreUtils.SafeRelease(m_CloudsAmbientProbeBuffer);
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

        static void OnComputeAmbientProbeDone(AsyncGPUReadbackRequest request)
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

        internal void PreRenderVolumetricClouds_AmbientProbe(RenderGraph renderGraph, HDCamera hdCamera)
        {
            if (hdCamera.lightingSky.skyRenderer != null)
            {
                using (new RenderGraphProfilingScope(renderGraph, ProfilingSampler.Get(HDProfileId.VolumetricCloudsAmbientProbe)))
                {
                    TextureHandle outputCubemap = renderGraph.CreateTexture(new TextureDesc(16, 16)
                    { slices = TextureXR.slices, dimension = TextureDimension.Cube, colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true });

                    outputCubemap = m_SkyManager.RenderSkyToCubemap(renderGraph, hdCamera.lightingSky, hdCamera, includeSunInBaking: false, renderCloudLayers: false, outputCubemap);
                    m_SkyManager.UpdateAmbientProbe(renderGraph, outputCubemap, outputForClouds: true, m_CloudsAmbientProbeBuffer, null, null, null, OnComputeAmbientProbeDone);
                }
            }
            else
            {
                m_CloudsAmbientProbeIsReady = false;
            }
        }
    }
}
