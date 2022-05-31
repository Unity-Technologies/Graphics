using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Buffers required to evaluate the probe
        internal ComputeBuffer m_CloudsDynamicProbeBuffer = null;
        internal ComputeBuffer m_CloudsStaticProbeBuffer = null;

        void InitializeVolumetricCloudsAmbientProbe()
        {
            // Buffer is stored packed to be used directly by shader code (27 coeffs in 7 float4)
            // Compute buffer storing the pre-convolved resulting SH For volumetric lighting. L2 SH => 9 float per component.
            m_CloudsDynamicProbeBuffer = new ComputeBuffer(7, 16);
            m_CloudsStaticProbeBuffer = new ComputeBuffer(7, 16);
        }

        void ReleaseVolumetricCloudsAmbientProbe()
        {
            CoreUtils.SafeRelease(m_CloudsDynamicProbeBuffer);
            CoreUtils.SafeRelease(m_CloudsStaticProbeBuffer);
        }

        internal ComputeBuffer RenderVolumetricCloudsAmbientProbe(RenderGraph renderGraph, HDCamera hdCamera, SkyUpdateContext lightingSky, bool staticSky)
        {
            // Grab the volume settings
            VolumetricClouds settings = hdCamera.volumeStack.GetComponent<VolumetricClouds>();

            // If the clouds are enabled on this camera
            ComputeBuffer probeBuffer = staticSky ? m_CloudsStaticProbeBuffer : m_CloudsDynamicProbeBuffer;
            if (HasVolumetricClouds(hdCamera, in settings) && lightingSky.skyRenderer != null)
            {
                using (new RenderGraphProfilingScope(renderGraph, ProfilingSampler.Get(HDProfileId.VolumetricCloudsAmbientProbe)))
                {
                    TextureHandle outputCubemap = renderGraph.CreateTexture(new TextureDesc(16, 16)
                    { slices = TextureXR.slices, dimension = TextureDimension.Cube, colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true });

                    outputCubemap = m_SkyManager.RenderSkyToCubemap(renderGraph, lightingSky, hdCamera, includeSunInBaking: false, renderCloudLayers: false, outputCubemap);
                    m_SkyManager.UpdateAmbientProbe(renderGraph, outputCubemap, outputForClouds: true, null, null, probeBuffer, new Vector4(settings.ambientLightProbeDimmer.value, 0.0f, 0.0f, 0.0f), null);
                }
            }
            return probeBuffer;
        }
    }
}
