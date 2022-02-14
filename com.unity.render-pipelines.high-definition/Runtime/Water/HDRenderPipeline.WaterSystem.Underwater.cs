using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Flag that allows us to track in which current unwater water volume we are
        internal int m_UnderWaterSurfaceIndex;

        void EvaluateUnderWaterSurface(HDCamera hdCamera)
        {
            // Flag that allows us to track which surface is the one we will be using for the under water rendering
            m_UnderWaterSurfaceIndex = -1;

            // Grab all the water surfaces in the scene
            var waterSurfaces = WaterSurface.instancesAsArray;
            int numWaterSurfaces = WaterSurface.instanceCount;

            // If the water is disabled , no need to render or simulate
            WaterRendering settings = hdCamera.volumeStack.GetComponent<WaterRendering>();
            if (!settings.enable.value || !hdCamera.frameSettings.IsEnabled(FrameSettingsField.Water) || numWaterSurfaces == 0)
                return;

            // Grab the camera's world space position
            Vector3 cameraWSPos = hdCamera.camera.transform.position;

            // First we need to define in which volume the camera is.
            // We consider that the camera can only be in one volume at a time and it is the first one
            // we find.
            int currentPriority = -1;
            for (int surfaceIdx = 0; surfaceIdx < numWaterSurfaces; ++surfaceIdx)
            {
                // Grab the current water surface
                WaterSurface currentWater = waterSurfaces[surfaceIdx];

                // Does this surface need underwater rendering?
                if (!currentWater.underWater)
                    continue;

                // If the surface is infinite, we need to check if the camera is between the top plane + max displacement  and the top plane - volume depth
                if (currentWater.infinite)
                {
                    // If the resources are invalid, we cannot render this surface
                    if (!currentWater.simulation.ValidResources((int)m_WaterBandResolution, k_WaterHighBandCount))
                        continue;

                    // Maximal possible wave height of the current setup
                    float maxWaveHeight;
                    Vector4 waveAmpltiude;
                    ComputeMaximumWaveHeight(currentWater.amplitude, currentWater.simulation.patchWindSpeed.x, currentWater.highBandCount, out waveAmpltiude, out maxWaveHeight);

                    // Evaluate the vertical boundaries of the volume
                    float topPlane = currentWater.transform.position.y + k_MaxWaterSurfaceElevation;
                    float bottomPlane = currentWater.transform.position.y - currentWater.volumeDepth;

                    // If both conditions are true, the camera is inside this volume
                    bool isBetweenPlanes = cameraWSPos.y > bottomPlane && cameraWSPos.y < topPlane;
                    if (isBetweenPlanes && currentPriority < currentWater.volumePrority)
                    {
                        m_UnderWaterSurfaceIndex = surfaceIdx;
                        currentPriority = currentWater.volumePrority;
                    }
                }
                else
                {
                    if (currentWater.volumeBounds != null)
                    {
                        // If the specified bounds contains the camera, we found a match
                        if (currentWater.volumeBounds.bounds.Contains(cameraWSPos) && currentPriority < currentWater.volumePrority)
                        {
                            m_UnderWaterSurfaceIndex = surfaceIdx;
                            currentPriority = currentWater.volumePrority;
                        }
                    }
                }
            }
        }

        class UnderWaterRenderingData
        {
            // Input data
            public int width;
            public int height;
            public int viewCount;
            public ComputeShader simulationCS;
            public int findVerticalDisplKernel;
            public ComputeShader waterLightingCS;
            public int underWaterKernel;
            public ShaderVariablesWaterRendering waterRenderingCB;
            public ShaderVariablesUnderWater underWaterCB;
            public ShaderVariablesWater waterCB;

            public TextureHandle colorBuffer;
            public TextureHandle depthBuffer;
            public TextureHandle causticsData;
            public ComputeBufferHandle cameraHeightBuffer;

            // Water rendered to this buffer
            public TextureHandle outputColorBuffer;
        }

        TextureHandle RenderUnderWaterVolume(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthBuffer)
        {
            // Are we in the volume of any surface at all?
            if (m_UnderWaterSurfaceIndex == -1 || WaterSurface.instancesAsArray == null || !hdCamera.frameSettings.IsEnabled(FrameSettingsField.Water))
                return colorBuffer;

            // Execute the unique lighting pass
            using (var builder = renderGraph.AddRenderPass<UnderWaterRenderingData>("Render Under Water", out var passData, ProfilingSampler.Get(HDProfileId.WaterSurfaceRenderingUnderWater)))
            {
                // Fetch the water surface we will be using
                WaterSurface waterSurface = WaterSurface.instancesAsArray[m_UnderWaterSurfaceIndex];

                // Prepare all the parameters
                passData.width = hdCamera.actualWidth;
                passData.height = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;
                passData.waterLightingCS = m_WaterLightingCS;
                passData.underWaterKernel = m_UnderWaterKernel;

                // Fill the under water CB
                passData.underWaterCB._MaxViewDistanceMultiplier = waterSurface.absorbtionDistanceMultiplier;
                passData.underWaterCB._WaterScatteringColor = waterSurface.scatteringColor;
                passData.underWaterCB._WaterRefractionColor = waterSurface.refractionColor;
                passData.underWaterCB._OutScatteringCoeff = -Mathf.Log(0.02f) / waterSurface.absorptionDistance;
                passData.underWaterCB._WaterTransitionSize = waterSurface.transitionSize;

                // All the required textures
                passData.colorBuffer = builder.ReadTexture(colorBuffer);
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.Read);
                passData.cameraHeightBuffer = builder.ReadComputeBuffer(renderGraph.ImportComputeBuffer(m_WaterCameraHeightBuffer));

                // Bind the caustics buffer that may be required
                bool simulationCaustics = waterSurface.caustics && waterSurface.causticsAlgorithm == WaterSurface.WaterCausticsType.Simulation;
                passData.causticsData = simulationCaustics ? renderGraph.ImportTexture(waterSurface.simulation.causticsBuffer) : renderGraph.defaultResources.blackTexture;

                // Fill the water rendering CB
                passData.waterRenderingCB._CausticsIntensity = waterSurface.causticsIntensity;
                passData.waterRenderingCB._CausticsTiling = waterSurface.causticsTiling;
                passData.waterRenderingCB._CausticsPlaneBlendDistance = waterSurface.causticsPlaneBlendDistance;
                passData.waterRenderingCB._PatchOffset = waterSurface.transform.position;
                passData.waterRenderingCB._WaterCausticsType = waterSurface.caustics ? (simulationCaustics ? 0 : 1) : 0;

                // Fill the water CB
                passData.waterCB._CausticsRegionSize = waterSurface.simulation.patchSizes[waterSurface.causticsBand];

                // Request the output textures
                passData.outputColorBuffer = builder.WriteTexture(CreateColorBuffer(m_RenderGraph, hdCamera, false));

                // Run the deferred lighting
                builder.SetRenderFunc(
                    (UnderWaterRenderingData data, RenderGraphContext ctx) =>
                    {
                        // Evaluate the dispatch parameters
                        int tileX = (data.width + 7) / 8;
                        int tileY = (data.height + 7) / 8;

                        // Bind the input gbuffer data
                        ConstantBuffer.Push(ctx.cmd, data.underWaterCB, data.waterLightingCS, HDShaderIDs._ShaderVariablesUnderWater);
                        ConstantBuffer.Push(ctx.cmd, data.waterRenderingCB, data.waterLightingCS, HDShaderIDs._ShaderVariablesWaterRendering);
                        ConstantBuffer.Push(ctx.cmd, data.waterCB, data.waterLightingCS, HDShaderIDs._ShaderVariablesWater);
                        ctx.cmd.SetComputeTextureParam(data.waterLightingCS, data.underWaterKernel, HDShaderIDs._WaterCausticsDataBuffer, data.causticsData);
                        ctx.cmd.SetComputeTextureParam(data.waterLightingCS, data.underWaterKernel, HDShaderIDs._CameraColorTexture, data.colorBuffer);
                        ctx.cmd.SetComputeTextureParam(data.waterLightingCS, data.underWaterKernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                        ctx.cmd.SetComputeTextureParam(data.waterLightingCS, data.underWaterKernel, HDShaderIDs._StencilTexture, data.depthBuffer, 0, RenderTextureSubElement.Stencil);
                        ctx.cmd.SetComputeBufferParam(data.waterLightingCS, data.underWaterKernel, HDShaderIDs._WaterCameraHeightBuffer, data.cameraHeightBuffer);

                        // Bind the output texture
                        ctx.cmd.SetComputeTextureParam(data.waterLightingCS, data.underWaterKernel, HDShaderIDs._CameraColorTextureRW, data.outputColorBuffer);

                        // Run the lighting
                        ctx.cmd.DispatchCompute(data.waterLightingCS, data.underWaterKernel, tileX, tileY, data.viewCount);
                    });

                // Return the new color buffer
                return passData.outputColorBuffer;
            }
        }
    }
}
