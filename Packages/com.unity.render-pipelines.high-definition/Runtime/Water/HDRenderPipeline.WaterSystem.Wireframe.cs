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
            if (!settings.enable.value || !hdCamera.frameSettings.IsEnabled(FrameSettingsField.Water) || WaterSurface.instanceCount == 0)
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

                // If the resources are invalid, we cannot render this surface
                if (!currentWater.simulation.ValidResources((int)m_WaterBandResolution, k_WaterHighBandCount))
                    continue;

                // Render the water surface
                WaterRenderingParameters parameters = PrepareWaterRenderingParameters(hdCamera, settings, currentWater, surfaceIdx, surfaceIdx == m_UnderWaterSurfaceIndex);

                // Render the water surface (will be rendered as wireframe because of the hidden render state)
                RenderWaterSurface(cmd,
                    currentWater.simulation.gpuBuffers.displacementBuffer, currentWater.simulation.gpuBuffers.additionalDataBuffer, currentWater.simulation.gpuBuffers.causticsBuffer,
                    m_WaterCameraHeightBuffer, m_WaterPatchDataBuffer, m_WaterIndirectDispatchBuffer, m_WaterCameraFrustrumBuffer, parameters);
            }
        }
    }
}
