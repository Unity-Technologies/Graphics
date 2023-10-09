using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        void RenderWaterAsWireFrame(CommandBuffer cmd, HDCamera hdCamera)
        {
            // If the water is disabled, no need to render
            WaterRendering settings = hdCamera.volumeStack.GetComponent<WaterRendering>();
            if (!ShouldRenderWater(hdCamera))
                return;

            // Copy the frustum data to the GPU (not done otherwise)
            PropagateFrustumDataToGPU(hdCamera);

            // Loop through the water surfaces
            int numWaterSurfaces = WaterSurface.instanceCount;
            var waterSurfaces = WaterSurface.instancesAsArray;

            for (int surfaceIdx = 0; surfaceIdx < numWaterSurfaces; ++surfaceIdx)
            {
                // Grab the current water surface
                WaterSurface currentWater = waterSurfaces[surfaceIdx];

                // Render the water surface
                WaterRenderingParameters parameters = PrepareWaterRenderingParameters(hdCamera, settings, currentWater, surfaceIdx, surfaceIdx == m_UnderWaterSurfaceIndex);

                // Grab the gpu buffers of the surface
                WaterSimulationResourcesGPU gpuBuffers = currentWater.simulation.gpuBuffers;

                // Render the water surface (will be rendered as wireframe because of the hidden render state)
                RenderWaterSurface(cmd,
                    gpuBuffers.displacementBuffer, gpuBuffers.additionalDataBuffer, TextureXR.GetBlackTexture(), TextureXR.GetBlackTexture(),
                    parameters.deformation ? currentWater.deformationBuffer : Texture2D.blackTexture, Texture2D.blackTexture,
                    TextureXR.GetBlackTexture(), TextureXR.GetBlackTexture(),
                    null, null,
                    m_WaterCameraHeightBuffer, m_WaterPatchDataBuffer, m_WaterIndirectDispatchBuffer, m_WaterCameraFrustrumBuffer, parameters);
            }
        }
    }
}
