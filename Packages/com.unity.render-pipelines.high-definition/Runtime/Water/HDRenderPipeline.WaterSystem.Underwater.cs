using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Flag that allows us to track in which current under water volume we are.
        internal int m_UnderWaterSurfaceIndex;

        // Underwater kernels
        ComputeShader m_UnderWaterRenderingCS;
        int m_WaterClearIndirectArgsKernel;
        int m_WaterLineEvaluationKernel;
        int m_WaterComplexPropagationKernel;
        int m_WaterSimplePropagationKernel;
        int m_UnderWaterKernel;

        // Resolution of the binning tile
        const int underWaterTileSize = 16;

        void InitializeUnderWaterResources()
        {
            // Kernels
            m_UnderWaterRenderingCS = m_Asset.renderPipelineResources.shaders.underWaterRenderingCS;
            m_WaterClearIndirectArgsKernel = m_UnderWaterRenderingCS.FindKernel("ClearIndirectArgs");
            m_WaterLineEvaluationKernel = m_UnderWaterRenderingCS.FindKernel("LineEvaluation");
            m_WaterComplexPropagationKernel = m_UnderWaterRenderingCS.FindKernel("ComplexPropagation");
            m_WaterSimplePropagationKernel = m_UnderWaterRenderingCS.FindKernel("SimplePropagation");
            m_UnderWaterKernel = m_UnderWaterRenderingCS.FindKernel("UnderWater");
        }

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
                if (currentWater.IsInfinite())
                {
                    // If the resources are invalid, we cannot render this surface
                    if (!currentWater.simulation.ValidResources((int)m_WaterBandResolution, WaterConsts.k_WaterHighBandCount))
                        continue;

                    // Maximal possible wave height of the current setup
                    float maxWaveHeight = HDRenderPipeline.EvaluateMaxAmplitude(currentWater.simulation.spectrum.patchSizes.x, currentWater.simulation.spectrum.patchWindSpeed.x * WaterConsts.k_MeterPerSecondToKilometerPerHour);

                    // Evaluate the vertical boundaries of the volume
                    float topPlane = currentWater.transform.position.y + Mathf.Max(Mathf.Max(maxWaveHeight * 2.0f, 0.1f), currentWater.volumeHeight);
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
            public Vector2Int threadGroup8;
            public Vector2Int threadGroup16;
            public bool compressedTileData;
            public bool appendTiles;

            // Shader data
            public ComputeShader underWaterCS;
            public int indirectClearKernel;
            public int lineEvaluationKernel;
            public int complexPropagationKernel;
            public int simplePropagationKernel;
            public int underWaterKernel;

            // Constant buffers
            public ShaderVariablesWaterRendering waterRenderingCB;
            public ShaderVariablesUnderWater underWaterCB;
            public ShaderVariablesWater waterCB;

            public TextureHandle colorBuffer;
            public TextureHandle depthBuffer;
            public TextureHandle causticsData;
            public TextureHandle waterGBuffer3;
            public BufferHandle cameraHeightBuffer;

            // WIP buffers
            public TextureHandle waterLineDebugTexture1;
            public TextureHandle waterLineDebugTexture2;

            // Water Line Data
            public BufferHandle indirectArgsBuffer;
            public BufferHandle appendTileBuffer;
            public BufferHandle tileBuffer;
            public TextureHandle lineTexture0;
            public TextureHandle lineTexture1;

            // Water rendered to this buffer
            public TextureHandle outputColorBuffer;
        }

        TextureHandle RenderUnderWaterVolume(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthBuffer, WaterGBuffer waterGBuffer)
        {
            // Are we in the volume of any surface at all?
            if (m_UnderWaterSurfaceIndex == -1
                || WaterSurface.instancesAsArray == null
                || !hdCamera.frameSettings.IsEnabled(FrameSettingsField.Water)
                || !waterGBuffer.valid)
                return colorBuffer;

            WaterRendering settings = hdCamera.volumeStack.GetComponent<WaterRendering>();

            // Execute the unique lighting pass
            using (var builder = renderGraph.AddRenderPass<UnderWaterRenderingData>("Render Under Water", out var passData, ProfilingSampler.Get(HDProfileId.WaterSurfaceRenderingUnderWater)))
            {
                // Fetch the water surface we will be using
                WaterSurface waterSurface = WaterSurface.instancesAsArray[m_UnderWaterSurfaceIndex];

                // Prepare all the parameters
                passData.width = hdCamera.actualWidth;
                passData.height = hdCamera.actualHeight;
                passData.viewCount = hdCamera.viewCount;
                passData.threadGroup8 = new Vector2Int((passData.width + 7) / 8, (passData.height + 7) / 8);
                passData.threadGroup16 = new Vector2Int((passData.width + 15) / 16, (passData.height + 15) / 16);
                passData.compressedTileData = passData.threadGroup16.x < 256u && passData.threadGroup16.y < 256u;
                passData.appendTiles = !hdCamera.xr.singlePassEnabled;

                // Shader data
                passData.underWaterCS = m_UnderWaterRenderingCS;
                passData.indirectClearKernel = m_WaterClearIndirectArgsKernel;
                passData.lineEvaluationKernel = m_WaterLineEvaluationKernel;
                passData.complexPropagationKernel = m_WaterComplexPropagationKernel;
                passData.simplePropagationKernel = m_WaterSimplePropagationKernel;
                passData.underWaterKernel = m_UnderWaterKernel;

                // All the required textures
                passData.colorBuffer = builder.ReadTexture(colorBuffer);
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.Read);
                passData.cameraHeightBuffer = builder.ReadBuffer(renderGraph.ImportBuffer(m_WaterCameraHeightBuffer));
                passData.waterGBuffer3 = builder.ReadTexture(waterGBuffer.waterGBuffer3);

                // Water line
                passData.tileBuffer = builder.CreateTransientBuffer(new BufferDesc(passData.threadGroup16.x * passData.threadGroup16.y * passData.viewCount, sizeof(uint) * 2, GraphicsBuffer.Target.Structured) { name = "Water Line Tile" });
                passData.lineTexture0 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R8_UInt, enableRandomWrite = true, name = "Water Line 0" });
                passData.lineTexture1 = builder.CreateTransientTexture(new TextureDesc(Vector2.one, true, true) { colorFormat = GraphicsFormat.R8_UInt, enableRandomWrite = true, name = "Water Line 1" });
                if (passData.appendTiles)
                {
                    passData.indirectArgsBuffer = builder.CreateTransientBuffer(new BufferDesc(3 * 2, sizeof(uint), GraphicsBuffer.Target.IndirectArguments) { name = "Water Line Indirect" });
                    passData.appendTileBuffer = builder.CreateTransientBuffer(new BufferDesc(passData.threadGroup16.x * passData.threadGroup16.y, sizeof(uint), GraphicsBuffer.Target.Append) { name = "Water Line Tile Append" });
                }

                // Bind the caustics buffer that may be required
                passData.causticsData = waterSurface.caustics ? renderGraph.ImportTexture(waterSurface.simulation.gpuBuffers.causticsBuffer) : renderGraph.defaultResources.blackTexture;

                // Fill the under water CB
                passData.underWaterCB._MaxViewDistanceMultiplier = waterSurface.absorptionDistanceMultiplier;
                passData.underWaterCB._WaterScatteringColor = waterSurface.underWaterScatteringColorMode == WaterSurface.UnderWaterScatteringColorMode.ScatteringColor ? waterSurface.scatteringColor : waterSurface.underWaterScatteringColor;
                passData.underWaterCB._WaterRefractionColor = waterSurface.refractionColor;
                passData.underWaterCB._OutScatteringCoeff = -Mathf.Log(0.02f) / waterSurface.absorptionDistance;
                passData.underWaterCB._WaterTransitionSize = hdCamera.camera.nearClipPlane * 2.0f;
                passData.underWaterCB._WaterLineTileCountX = (uint)passData.threadGroup16.x;
                passData.underWaterCB._WaterLineTileCountY = (uint)passData.threadGroup16.y;
                passData.underWaterCB._UnderWaterAmbientProbeContribution = waterSurface.underWaterAmbientProbeContribution;

                // Fill the water rendering CB
                passData.waterRenderingCB._CausticsIntensity = waterSurface.causticsIntensity;
                passData.waterRenderingCB._CausticsPlaneBlendDistance = waterSurface.causticsPlaneBlendDistance;
                passData.waterRenderingCB._PatchOffset = waterSurface.transform.position;
                passData.waterRenderingCB._WaterCausticsEnabled = waterSurface.caustics ? 1 : 0;
                passData.waterRenderingCB._WaterSurfaceTransform = waterSurface.simulation.rendering.waterToWorldMatrix;
                passData.waterRenderingCB._WaterSurfaceTransform_Inverse = waterSurface.simulation.rendering.worldToWaterMatrix;
                passData.waterRenderingCB._WaterAmbientProbe = m_WaterAmbientProbe;

                // Fill the water CB
                passData.waterCB._CausticsRegionSize = waterSurface.simulation.spectrum.patchSizes[waterSurface.causticsBand];
                passData.waterCB._WaterUpDirection = new float4(waterSurface.UpVector(), 0.0f);

                // Request the output textures
                passData.outputColorBuffer = builder.WriteTexture(CreateColorBuffer(m_RenderGraph, hdCamera, false));

                // Run the deferred lighting
                builder.SetRenderFunc(
                    (UnderWaterRenderingData data, RenderGraphContext ctx) =>
                    {
                        // Bind the input gbuffer data
                        ConstantBuffer.Push(ctx.cmd, data.underWaterCB, data.underWaterCS, HDShaderIDs._ShaderVariablesUnderWater);
                        ConstantBuffer.Push(ctx.cmd, data.waterRenderingCB, data.underWaterCS, HDShaderIDs._ShaderVariablesWaterRendering);
                        ConstantBuffer.Push(ctx.cmd, data.waterCB, data.underWaterCS, HDShaderIDs._ShaderVariablesWater);

                        if (data.appendTiles)
                        {
                            // Reset the tile buffer counter
                            ((GraphicsBuffer)data.appendTileBuffer).SetCounterValue(0u);

                            // Clear the indirect buffer argument
                            ctx.cmd.SetComputeBufferParam(data.underWaterCS, data.indirectClearKernel, HDShaderIDs._IndirectBufferRW, data.indirectArgsBuffer);
                            ctx.cmd.DispatchCompute(data.underWaterCS, data.indirectClearKernel, 1, 1, 1);
                        }

                        // Clear the post complex propagation texture
                        ctx.cmd.SetRenderTarget(data.lineTexture1);
                        ctx.cmd.ClearRenderTarget(false, true, Color.clear);

                        // Raise the compression flag
                        CoreUtils.SetKeyword(ctx.cmd, "USE_APPEND_TILES", data.appendTiles);
                        CoreUtils.SetKeyword(ctx.cmd, "COMPRESSED_TILE_DATA", data.compressedTileData);

                        // Run the water line evaluation
                        ctx.cmd.SetComputeTextureParam(data.underWaterCS, data.lineEvaluationKernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                        ctx.cmd.SetComputeTextureParam(data.underWaterCS, data.lineEvaluationKernel, HDShaderIDs._StencilTexture, data.depthBuffer, 0, RenderTextureSubElement.Stencil);
                        ctx.cmd.SetComputeBufferParam(data.underWaterCS, data.lineEvaluationKernel, HDShaderIDs._WaterCameraHeightBuffer, data.cameraHeightBuffer);
                        ctx.cmd.SetComputeTextureParam(data.underWaterCS, data.lineEvaluationKernel, HDShaderIDs._WaterLineTextureRW, data.lineTexture0);
                        ctx.cmd.SetComputeBufferParam(data.underWaterCS, data.lineEvaluationKernel, HDShaderIDs._ComplexTileListRW, data.appendTileBuffer);
                        ctx.cmd.SetComputeBufferParam(data.underWaterCS, data.lineEvaluationKernel, HDShaderIDs._IndirectBufferRW, data.indirectArgsBuffer);
                        if (data.compressedTileData)
                            ctx.cmd.SetComputeBufferParam(data.underWaterCS, data.lineEvaluationKernel, HDShaderIDs._WaterLineTileDataBufferRW, data.tileBuffer);
                        ctx.cmd.DispatchCompute(data.underWaterCS, data.lineEvaluationKernel, data.threadGroup16.x, data.threadGroup16.y, data.viewCount);

                        // Run complex propagation on the relevant tiles
                        ctx.cmd.SetComputeBufferParam(data.underWaterCS, data.complexPropagationKernel, HDShaderIDs._WaterCameraHeightBuffer, data.cameraHeightBuffer);
                        ctx.cmd.SetComputeBufferParam(data.underWaterCS, data.complexPropagationKernel, HDShaderIDs._ComplexTileList, data.appendTileBuffer);
                        ctx.cmd.SetComputeTextureParam(data.underWaterCS, data.complexPropagationKernel, HDShaderIDs._WaterLineTexture, data.lineTexture0);
                        ctx.cmd.SetComputeTextureParam(data.underWaterCS, data.complexPropagationKernel, HDShaderIDs._WaterLineTextureRW, data.lineTexture1);
                        ctx.cmd.SetComputeBufferParam(data.underWaterCS, data.complexPropagationKernel, HDShaderIDs._WaterLineTileDataBufferRW, data.tileBuffer);
                        if (data.appendTiles)
                            ctx.cmd.DispatchCompute(data.underWaterCS, data.complexPropagationKernel, data.indirectArgsBuffer, 0);
                        else
                            ctx.cmd.DispatchCompute(data.underWaterCS, data.complexPropagationKernel, data.threadGroup16.x, data.threadGroup16.y, data.viewCount);

                        // Run simple propagation on all tiles
                        ctx.cmd.SetComputeBufferParam(data.underWaterCS, data.simplePropagationKernel, HDShaderIDs._WaterCameraHeightBuffer, data.cameraHeightBuffer);
                        if (data.compressedTileData)
                            ctx.cmd.SetComputeBufferParam(data.underWaterCS, data.simplePropagationKernel, HDShaderIDs._WaterLineTileDataBuffer, data.tileBuffer);
                        ctx.cmd.SetComputeTextureParam(data.underWaterCS, data.simplePropagationKernel, HDShaderIDs._WaterLineTexture, data.lineTexture1);
                        ctx.cmd.SetComputeTextureParam(data.underWaterCS, data.simplePropagationKernel, HDShaderIDs._WaterRegionTextureRW, data.lineTexture0);
                        ctx.cmd.DispatchCompute(data.underWaterCS, data.simplePropagationKernel, data.threadGroup16.x, data.threadGroup16.y, data.viewCount);

                        // Raise the compression flag
                        CoreUtils.SetKeyword(ctx.cmd, "COMPRESSED_TILE_DATA", false);
                        CoreUtils.SetKeyword(ctx.cmd, "USE_APPEND_TILES", false);

                        // Apply the under water post process.
                        ctx.cmd.SetComputeTextureParam(data.underWaterCS, data.underWaterKernel, HDShaderIDs._WaterGBufferTexture3, data.waterGBuffer3);
                        ctx.cmd.SetComputeTextureParam(data.underWaterCS, data.underWaterKernel, HDShaderIDs._WaterCausticsDataBuffer, data.causticsData);
                        ctx.cmd.SetComputeTextureParam(data.underWaterCS, data.underWaterKernel, HDShaderIDs._CameraColorTexture, data.colorBuffer);
                        ctx.cmd.SetComputeTextureParam(data.underWaterCS, data.underWaterKernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                        ctx.cmd.SetComputeTextureParam(data.underWaterCS, data.underWaterKernel, HDShaderIDs._StencilTexture, data.depthBuffer, 0, RenderTextureSubElement.Stencil);
                        ctx.cmd.SetComputeBufferParam(data.underWaterCS, data.underWaterKernel, HDShaderIDs._WaterCameraHeightBuffer, data.cameraHeightBuffer);
                        ctx.cmd.SetComputeTextureParam(data.underWaterCS, data.underWaterKernel, HDShaderIDs._WaterRegionTexture, data.lineTexture0);
                        ctx.cmd.SetComputeTextureParam(data.underWaterCS, data.underWaterKernel, HDShaderIDs._CameraColorTextureRW, data.outputColorBuffer);
                        ctx.cmd.DispatchCompute(data.underWaterCS, data.underWaterKernel, data.threadGroup8.x, data.threadGroup8.y, data.viewCount);
                    });

                // Return the new color buffer
                return passData.outputColorBuffer;
            }
        }
    }
}
