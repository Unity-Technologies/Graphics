using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Setup to match FORWARD_ECCENTRICITY in VolumetricCloudsUtilies.hlsl
        // Kinda empirical because they are not using the same phase function
        const float m_VolumetricCloudsAnisotropy = 0.7f;

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
                // We include background clouds in the ambient probe because we assume they are located above the volumetric clouds (in the background) so they should block sky light
                m_SkyManager.RenderSkyAmbientProbe(renderGraph, lightingSky, hdCamera, probeBuffer, renderBackgroundClouds: true, HDProfileId.VolumetricCloudsAmbientProbe,
                    settings.ambientLightProbeDimmer.value, m_VolumetricCloudsAnisotropy);
            }

            return probeBuffer;
        }
    }
}
