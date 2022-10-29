using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        const int m_SkyCubemapResolution = 16;

        // Buffers required to evaluate the probe
        internal ComputeBuffer m_CloudsDynamicProbeBuffer = null;
        internal ComputeBuffer m_CloudsStaticProbeBuffer = null;

        Matrix4x4[] m_facePixelCoordToViewDirMatrices = new Matrix4x4[6];

        void InitializeVolumetricCloudsAmbientProbe()
        {
            // Buffer is stored packed to be used directly by shader code (27 coeffs in 7 float4)
            // Compute buffer storing the pre-convolved resulting SH For volumetric lighting. L2 SH => 9 float per component.
            m_CloudsDynamicProbeBuffer = new ComputeBuffer(7, 16);
            m_CloudsStaticProbeBuffer = new ComputeBuffer(7, 16);

            var cubemapScreenSize = new Vector4((float)m_SkyCubemapResolution, (float)m_SkyCubemapResolution, 1.0f / (float)m_SkyCubemapResolution, 1.0f / (float)m_SkyCubemapResolution);

            for (int i = 0; i < 6; ++i)
            {
                var lookAt = Matrix4x4.LookAt(Vector3.zero, CoreUtils.lookAtList[i], CoreUtils.upVectorList[i]);
                var worldToView = lookAt * Matrix4x4.Scale(new Vector3(1.0f, 1.0f, -1.0f)); // Need to scale -1.0 on Z to match what is being done in the camera.wolrdToCameraMatrix API. ...

                m_facePixelCoordToViewDirMatrices[i] = HDUtils.ComputePixelCoordToWorldSpaceViewDirectionMatrix(0.5f * Mathf.PI, Vector2.zero, cubemapScreenSize, worldToView, true);
            }
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
                    TextureHandle outputCubemap = renderGraph.CreateTexture(new TextureDesc(m_SkyCubemapResolution, m_SkyCubemapResolution)
                    { slices = TextureXR.slices, dimension = TextureDimension.Cube, colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true });

                    outputCubemap = m_SkyManager.RenderSkyToCubemap(renderGraph, lightingSky, hdCamera, m_facePixelCoordToViewDirMatrices, renderCloudLayers: false, outputCubemap);
                    m_SkyManager.UpdateAmbientProbe(renderGraph, outputCubemap, outputForClouds: true, null, null, probeBuffer, new Vector4(settings.ambientLightProbeDimmer.value, 0.7f, 0.0f, 0.0f), null);
                }
            }
            return probeBuffer;
        }
    }
}
