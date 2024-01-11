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
            if (!waterGBuffer.debugRequired || !ShouldRenderWater(hdCamera))
                return;

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

        class WaterRenderingMaskData
        {
            // All the parameters required to simulate and render the water
            public WaterRenderingParameters parameters;
            public ShaderVariablesWaterDebug waterDebugCB;

            // Simulation buffers
            public TextureHandle displacementBuffer;
            public TextureHandle additionalDataBuffer;
            public TextureHandle foamData;
            public TextureHandle deformationBuffer;
            public TextureHandle deformationSGBuffer;

            // Other resources
            public BufferHandle indirectBuffer;
            public BufferHandle patchDataBuffer;
            public BufferHandle frustumBuffer;
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
                builder.UseColorBuffer(colorBuffer, 0);
                builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);

                // Import all the textures into the system
                passData.displacementBuffer = renderGraph.ImportTexture(currentWater.simulation.gpuBuffers.displacementBuffer);
                passData.additionalDataBuffer = renderGraph.ImportTexture(currentWater.simulation.gpuBuffers.additionalDataBuffer);
                passData.foamData = passData.parameters.foam ? renderGraph.ImportTexture(currentWater.FoamBuffer()) : renderGraph.defaultResources.blackTexture;
                passData.deformationBuffer = passData.parameters.deformation ? renderGraph.ImportTexture(currentWater.deformationBuffer) : renderGraph.defaultResources.blackTexture;
                passData.deformationSGBuffer = passData.parameters.deformation ? renderGraph.ImportTexture(currentWater.deformationSGBuffer) : renderGraph.defaultResources.blackTexture;

                // For GPU culling
                passData.indirectBuffer = renderGraph.ImportBuffer(m_WaterIndirectDispatchBuffer);
                passData.patchDataBuffer = renderGraph.ImportBuffer(m_WaterPatchDataBuffer);
                passData.frustumBuffer = renderGraph.ImportBuffer(m_WaterCameraFrustrumBuffer);

                // Request the output textures
                builder.SetRenderFunc(
                    (WaterRenderingMaskData data, RenderGraphContext ctx) =>
                    {
                        SetupCommonRenderingData(ctx.cmd, data.displacementBuffer, data.additionalDataBuffer, TextureXR.GetBlackTexture(),
                            data.foamData, data.deformationBuffer, data.deformationSGBuffer, data.parameters);

                        // Normally we should bind this into the material property block, but on metal there seems to be an issue. This fixes it.
                        ctx.cmd.SetGlobalFloat(HDShaderIDs._CullWaterMask, (int)CullMode.Off);

                        // Bind the debug constant buffer
                        ConstantBuffer.Push(ctx.cmd, data.waterDebugCB, data.parameters.waterMaterial, HDShaderIDs._ShaderVariablesWaterDebug);

                        // For the debug mode, we don't bother using the indirect method as we do not care about perf.
                        DrawWaterSurface(ctx.cmd, data.parameters, k_PassesWaterMask, data.patchDataBuffer, data.indirectBuffer, data.frustumBuffer);

                        // Reset the keywords
                        ResetWaterShaderKeyword(ctx.cmd);
                    });
            }
        }
    }
}
