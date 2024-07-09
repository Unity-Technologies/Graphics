using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class WaterSystem
    {
        ShaderVariablesWaterDebug[] m_WaterDebugCBs = new ShaderVariablesWaterDebug[k_MaxNumWaterSurfaceProfiles];

        class WaterRenderingMaskData : WaterRenderingData
        {
            public ShaderVariablesWaterDebug[] waterDebugCBs;
        }

        internal void RenderWaterDebug(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthBuffer, WaterGBuffer waterGBuffer)
        {
            if (waterGBuffer.debugRequired)
                RenderWaterDebug(renderGraph, hdCamera, colorBuffer, depthBuffer);
        }

        internal void RenderWaterDebug(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthBuffer)
        {
            if (!ShouldRenderWater(hdCamera))
                return;

            using (var builder = renderGraph.AddRenderPass<WaterRenderingMaskData>("Render Water Surface Mask Debug", out var passData, ProfilingSampler.Get(HDProfileId.WaterMaskDebug)))
            {
                WaterRendering settings = hdCamera.volumeStack.GetComponent<WaterRendering>();
                PrepareWaterRenderingData(passData, hdCamera);

                passData.waterDebugCBs = m_WaterDebugCBs;

                var waterSurfaces = WaterSurface.instancesAsArray;
                for (int surfaceIdx = 0; surfaceIdx < passData.numSurfaces; ++surfaceIdx)
                {
                    // Grab the current water surface
                    WaterSurface currentWater = waterSurfaces[surfaceIdx];
                    ref var surfaceData = ref passData.surfaces[surfaceIdx];

                    surfaceData.renderDebug = m_RenderPipeline.NeedDebugDisplay() || currentWater.debugMode != WaterDebugMode.None;
                    if (!surfaceData.renderDebug) continue;

                    // Prepare all the internal parameters
                    PrepareSurfaceGBufferData(hdCamera, settings, currentWater, surfaceIdx, ref surfaceData);

                    // Debug Data
                    ref var debugCB = ref passData.waterDebugCBs[surfaceIdx];
                    debugCB._WaterDebugMode = (int)currentWater.debugMode;
                    debugCB._WaterMaskDebugMode = (int)currentWater.waterMaskDebugMode;
                    debugCB._WaterCurrentDebugMode = (int)currentWater.waterCurrentDebugMode;
                    debugCB._CurrentDebugMultiplier = currentWater.currentDebugMultiplier;
                    debugCB._WaterFoamDebugMode = (int)currentWater.waterFoamDebugMode;

                    if (currentWater.waterCurrentDebugMode == WaterCurrentDebugMode.Ripples && currentWater.ripplesMotionMode == WaterPropertyOverrideMode.Inherit)
                        debugCB._WaterCurrentDebugMode = (int)WaterCurrentDebugMode.Large;
                    if (currentWater.surfaceType == WaterSurfaceType.Pool)
                        debugCB._WaterCurrentDebugMode = (int)WaterCurrentDebugMode.Ripples;
                }

                // Output buffers
                builder.UseColorBuffer(colorBuffer, 0);
                builder.UseDepthBuffer(depthBuffer, DepthAccess.ReadWrite);

                // Request the output textures
                builder.SetRenderFunc(
                    (WaterRenderingMaskData data, RenderGraphContext ctx) =>
                    {
                        ctx.cmd.SetGlobalTexture(HDShaderIDs._WaterSectorData, data.sectorDataBuffer);
                        ctx.cmd.SetGlobalBuffer(HDShaderIDs._WaterPatchData, data.patchDataBuffer);
                        ctx.cmd.SetGlobalBuffer(HDShaderIDs._FrustumGPUBuffer, data.frustumBuffer);

                        // Normally we should bind this into the material property block, but on metal there seems to be an issue. This fixes it.
                        ctx.cmd.SetGlobalFloat(HDShaderIDs._CullWaterMask, (int)CullMode.Off);

                        for (int surfaceIdx = 0; surfaceIdx < data.numSurfaces; ++surfaceIdx)
                        {
                            ref var surfaceData = ref data.surfaces[surfaceIdx];

                            if (surfaceData.renderDebug)
                            {
                                ctx.cmd.SetBufferData(data.perCameraCB, data.sharedPerCameraDataArray, surfaceData.surfaceIndex, 0, 1);
                                ConstantBuffer.Push(ctx.cmd, data.waterDebugCBs[surfaceIdx], surfaceData.waterMaterial, HDShaderIDs._ShaderVariablesWaterDebug);

                                SetupWaterShaderKeyword(ctx.cmd, data.decalWorkflow, surfaceData.numActiveBands, surfaceData.activeCurrent);
                                DrawWaterSurface(ctx.cmd, k_PassesWaterDebug, data, ref surfaceData);
                                ResetWaterShaderKeyword(ctx.cmd);
                            }

                        }
                    });
            }
        }
    }
}
