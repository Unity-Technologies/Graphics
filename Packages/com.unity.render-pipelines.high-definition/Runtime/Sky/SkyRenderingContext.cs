using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    internal class SkyRenderingContext
    {
        SphericalHarmonicsL2 m_AmbientProbe;

        public SphericalHarmonicsL2 ambientProbe => m_AmbientProbe;

        public GraphicsBuffer ambientProbeResult { get; private set; }
        public GraphicsBuffer diffuseAmbientProbeBuffer { get; private set; }
        public GraphicsBuffer volumetricAmbientProbeBuffer { get; private set; }
        public GraphicsBuffer cloudAmbientProbeBuffer { get; private set; }
        public RTHandle skyboxCubemapRT { get; private set; }
        public CubemapArray skyboxBSDFCubemapArray { get; private set; }
        public bool supportsConvolution { get; private set; } = false;

        internal bool ambientProbeIsReady = false;

        public SkyRenderingContext(int resolution, int bsdfCount, bool supportsConvolution, SphericalHarmonicsL2 ambientProbe, string name)
        {
            m_AmbientProbe = ambientProbe;
            this.supportsConvolution = supportsConvolution;

            // Compute buffer storing the resulting SH from diffuse convolution. L2 SH => 9 float per component.
            ambientProbeResult = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 27, 4);
            // Buffer is stored packed to be used directly by shader code (27 coeffs in 7 float4)
            // Compute buffer storing the pre-convolved resulting SH For volumetric lighting. L2 SH => 9 float per component.
            volumetricAmbientProbeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 7, 16);
            // Compute buffer storing the diffuse convolution SH For diffuse ambient lighting. L2 SH => 9 float per component.
            diffuseAmbientProbeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 7, 16);
            // Same as diffuseAmbientProbeBuffer but contains only the sky. To be used by CloudRenderers
            cloudAmbientProbeBuffer = new GraphicsBuffer(GraphicsBuffer.Target.Structured, 7, 16);

            skyboxCubemapRT = RTHandles.Alloc(resolution, resolution, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureDimension.Cube, useMipMap: true, autoGenerateMips: false, filterMode: FilterMode.Trilinear, name: name);

            if (supportsConvolution)
            {
                skyboxBSDFCubemapArray = new CubemapArray(resolution, bsdfCount, GraphicsFormat.R16G16B16A16_SFloat, TextureCreationFlags.MipChain)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Repeat,
                    wrapModeV = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Trilinear,
                    anisoLevel = 0,
                    name = "SkyboxCubemapConvolution"
                };
            }
        }

        public void Reset()
        {
            ambientProbeIsReady = false;
        }

        public void Cleanup()
        {
            RTHandles.Release(skyboxCubemapRT);
            if (skyboxBSDFCubemapArray != null)
            {
                CoreUtils.Destroy(skyboxBSDFCubemapArray);
            }

            ambientProbeResult.Release();
            diffuseAmbientProbeBuffer.Release();
            volumetricAmbientProbeBuffer.Release();
            cloudAmbientProbeBuffer.Release();
        }

        public void ClearAmbientProbe()
        {
            m_AmbientProbe = new SphericalHarmonicsL2();
        }

        public void UpdateAmbientProbe(in SphericalHarmonicsL2 probe)
        {
            m_AmbientProbe = probe;
        }

        public void OnComputeAmbientProbeDone(AsyncGPUReadbackRequest request)
        {
            if (!request.hasError)
            {
                var result = request.GetData<float>();
                for (int channel = 0; channel < 3; ++channel)
                {
                    for (int coeff = 0; coeff < 9; ++coeff)
                    {
                        m_AmbientProbe[channel, coeff] = result[channel * 9 + coeff];
                    }
                }

                ambientProbeIsReady = true;
            }
        }
    }
}
