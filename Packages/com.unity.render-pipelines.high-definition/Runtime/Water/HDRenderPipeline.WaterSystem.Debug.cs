using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        void RenderWaterMaskDebug(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthBuffer, WaterGBuffer waterGBuffer)
        {
            WaterRendering settings = hdCamera.volumeStack.GetComponent<WaterRendering>();
            if (!ShouldRenderWater(hdCamera))
                return;

            if (waterGBuffer.debugRequired)
            {
                // Grab all the water surfaces in the scene
                var waterSurfaces = WaterSurface.instancesAsArray;
                int numWaterSurfaces = WaterSurface.instanceCount;

                for (int surfaceIdx = 0; surfaceIdx < numWaterSurfaces; ++surfaceIdx)
                {
                    // Grab the current water surface
                    WaterSurface currentWater = waterSurfaces[surfaceIdx];

                    // If the resources are invalid, we cannot render this surface
                    if (currentWater.debugMode == WaterDebugMode.None)
                        continue;

                    // Render the water surface
                    RenderWaterSurfaceMask(renderGraph, hdCamera, currentWater, settings, surfaceIdx, colorBuffer, depthBuffer);
                }
            }
        }

        class WaterRenderingMaskData
        {
            // All the parameters required to simulate and render the water
            public WaterRenderingParameters parameters;
            public ShaderVariablesWaterDebug waterDebugCB;

            // Simulation buffers
            public TextureHandle displacementBuffer;
            public TextureHandle additionalDataBuffer;
            public TextureHandle deformationBuffer;

            // Output buffers
            public TextureHandle colorBuffer;
            public TextureHandle depthBuffer;
        }

        void RenderWaterSurfaceMask(RenderGraph renderGraph, HDCamera hdCamera, WaterSurface currentWater, WaterRendering settings, int surfaceIdx, TextureHandle colorBuffer, TextureHandle depthBuffer)
        {
            using (var builder = renderGraph.AddRenderPass<WaterRenderingMaskData>("Render Water Surface Mask Debug", out var passData, ProfilingSampler.Get(HDProfileId.WaterSurfaceRenderingMaskDebug)))
            {
                // Prepare all the internal parameters
                passData.parameters = PrepareWaterRenderingParameters(hdCamera, settings, currentWater, surfaceIdx, false);
                passData.waterDebugCB._WaterDebugMode = (int) currentWater.debugMode;
                passData.waterDebugCB._WaterMaskDebugMode = (int) currentWater.waterMaskDebugMode;
                if (currentWater.waterCurrentDebugMode == WaterCurrentDebugMode.Large)
                    passData.waterDebugCB._WaterCurrentDebugMode = 0;
                else
                    passData.waterDebugCB._WaterCurrentDebugMode = currentWater.ripplesMotionMode == WaterPropertyOverrideMode.Custom ? 1 : 0;
                passData.waterDebugCB._CurrentDebugMultiplier = currentWater.currentDebugMultiplier;
                passData.waterDebugCB._WaterFoamDebugMode = (int)currentWater.waterFoamDebugMode;

                // Allocate all the intermediate textures
                passData.colorBuffer = builder.UseColorBuffer(colorBuffer, 0);
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);

                // Import all the textures into the system
                passData.displacementBuffer = renderGraph.ImportTexture(currentWater.simulation.gpuBuffers.displacementBuffer);
                passData.additionalDataBuffer = renderGraph.ImportTexture(currentWater.simulation.gpuBuffers.additionalDataBuffer);
                passData.deformationBuffer = passData.parameters.deformation ? renderGraph.ImportTexture(currentWater.deformationBuffer) : renderGraph.defaultResources.blackTexture;

                // Request the output textures
                builder.SetRenderFunc(
                    (WaterRenderingMaskData data, RenderGraphContext ctx) =>
                    {
                        ConstantBuffer.UpdateData(ctx.cmd, data.parameters.waterCB);
                        ConstantBuffer.UpdateData(ctx.cmd, data.parameters.waterRenderingCB);
                        ConstantBuffer.UpdateData(ctx.cmd, data.parameters.waterDeformationCB);

                        // We will be writing directly to the color and depth buffers
                        CoreUtils.SetRenderTarget(ctx.cmd, data.colorBuffer, data.depthBuffer);

                        // Raise the keywords for band count
                        SetupWaterShaderKeyword(ctx.cmd, data.parameters.numActiveBands, data.parameters.activeCurrent);

                        // Prepare the material property block for the rendering
                        data.parameters.mbp.SetTexture(HDShaderIDs._WaterDisplacementBuffer, data.displacementBuffer);
                        data.parameters.mbp.SetTexture(HDShaderIDs._WaterAdditionalDataBuffer, data.additionalDataBuffer);
                        data.parameters.mbp.SetTexture(HDShaderIDs._WaterDeformationBuffer, data.deformationBuffer);

                        // Bind the global water textures
                        data.parameters.mbp.SetTexture(HDShaderIDs._WaterMask, data.parameters.waterMask);
                        data.parameters.mbp.SetTexture(HDShaderIDs._SimulationFoamMask, data.parameters.simulationFoamMask);
                        if (data.parameters.activeCurrent)
                        {
                            data.parameters.mbp.SetTexture(HDShaderIDs._Group0CurrentMap, data.parameters.largeCurrentMap);
                            data.parameters.mbp.SetTexture(HDShaderIDs._Group1CurrentMap, data.parameters.ripplesCurrentMap);
                            data.parameters.mbp.SetTexture(HDShaderIDs._WaterSectorData, data.parameters.sectorDataBuffer);
                        }

                        // Normally we should bind this into the material property block, but on metal there seems to be an issue. This fixes it.
                        ctx.cmd.SetGlobalFloat("_CullWaterMask", (int)CullMode.Off);

                        // Bind the debug constant buffer
                        ConstantBuffer.Push(ctx.cmd, data.waterDebugCB, data.parameters.waterMaterial, HDShaderIDs._ShaderVariablesWaterDebug);

                        // For the debug mode, we don't bother using the indirect method as we do not care about perf.
                        DrawWaterSurface(ctx.cmd, data.parameters, k_WaterMaskPass, false, null, null, null);

                        // Reset the keywords
                        ResetWaterShaderKeyword(ctx.cmd);
                    });
            }
        }
    }
}
