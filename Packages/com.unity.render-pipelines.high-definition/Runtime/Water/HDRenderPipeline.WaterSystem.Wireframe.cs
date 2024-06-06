namespace UnityEngine.Rendering.HighDefinition
{
    partial class WaterSystem
    {
        internal void RenderWaterAsWireFrame(CommandBuffer cmd, HDCamera hdCamera)
        {
            // If the water is disabled, no need to render
            if (!ShouldRenderWater(hdCamera))
                return;

            WaterRendering settings = hdCamera.volumeStack.GetComponent<WaterRendering>();

            var data = new WaterRenderingData();
            PrepareWaterRenderingData(data, hdCamera);
            data.BindGlobal(cmd);

            var waterSurfaces = WaterSurface.instancesAsArray;
            for (int surfaceIdx = 0; surfaceIdx < data.numSurfaces; ++surfaceIdx)
            {
                // Grab the current water surface
                WaterSurface currentWater = waterSurfaces[surfaceIdx];
                ref var surfaceData = ref data.surfaces[surfaceIdx];

                // Render the water surface
                PrepareSurfaceGBufferData(hdCamera, settings, currentWater, surfaceIdx, ref surfaceData);
                RenderWaterSurface(cmd, data, ref surfaceData);
            }
        }
    }
}
