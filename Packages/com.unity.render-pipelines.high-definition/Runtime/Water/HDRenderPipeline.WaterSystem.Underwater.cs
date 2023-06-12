using Unity.Mathematics;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipeline
    {
        // Flag that allows us to track in which current under water volume we are.
        internal int m_UnderWaterSurfaceIndex;

        // Water line data
        int m_WaterLineBufferSize;
        int m_ReductionSize;

        // Underwater kernels
        ComputeShader m_UnderWaterRenderingCS;
        int m_ClearWaterLine;
        int m_WaterLineEvaluation1D;
        int m_WaterLineBoundsPropagation;
        int m_UnderWaterKernel;
        int m_UnderWaterDirectionalCausticsKernel;

        // Resolution of the binning tile
        const int underWaterTileSize = 16;

        void InitializeUnderWaterResources()
        {
            // Kernels
            m_UnderWaterRenderingCS = m_Asset.renderPipelineResources.shaders.underWaterRenderingCS;
            m_ClearWaterLine = m_UnderWaterRenderingCS.FindKernel("ClearWaterLine");
            m_WaterLineEvaluation1D = m_UnderWaterRenderingCS.FindKernel("LineEvaluation1D");
            m_WaterLineBoundsPropagation = m_UnderWaterRenderingCS.FindKernel("BoundsPropagation");
            m_UnderWaterKernel = m_UnderWaterRenderingCS.FindKernel("UnderWater");
            m_UnderWaterDirectionalCausticsKernel = m_UnderWaterRenderingCS.FindKernel("UnderWaterDirectionalCaustics");
        }

        void EvaluateUnderWaterSurface(HDCamera hdCamera)
        {
            // Flag that allows us to track which surface is the one we will be using for the under water rendering
            m_UnderWaterSurfaceIndex = -1;

            // Grab all the water surfaces in the scene
            var waterSurfaces = WaterSurface.instancesAsArray;
            int numWaterSurfaces = WaterSurface.instanceCount;

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


        float Min4(float a, float b, float c, float d) => Mathf.Min(Mathf.Min(a, b), Mathf.Min(c, d));
        float Max4(float a, float b, float c, float d) => Mathf.Max(Mathf.Max(a, b), Mathf.Max(c, d));

        Vector2 GetScreenSpaceBounds(Vector2 direction, HDCamera hdCamera)
        {
            float corner_1 = Vector2.Dot(direction, Vector2.zero);
            float corner_2 = Vector2.Dot(direction, new Vector2(0, hdCamera.actualHeight));
            float corner_3 = Vector2.Dot(direction, new Vector2(hdCamera.actualWidth, 0));
            float corner_4 = Vector2.Dot(direction, new Vector2(hdCamera.actualWidth, hdCamera.actualHeight));

            return new Vector2(Min4(corner_1, corner_2, corner_3, corner_4), Max4(corner_1, corner_2, corner_3, corner_4));
        }

        unsafe void UpdateShaderVariablesGlobalWater(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            cb._EnableUnderwater = 0;
            if (!ShouldRenderWater(hdCamera))
                return;

            EvaluateUnderWaterSurface(hdCamera);
            if (m_UnderWaterSurfaceIndex == -1)
            {
                // Put some parameters that will fill waterline with constant value
                cb._BoundsSS = new float4(0, 0, -1, 1);
                cb._UpDirectionX = 0;
                cb._UpDirectionY = 1;

                return;
            }

            WaterSurface waterSurface = WaterSurface.instancesAsArray[m_UnderWaterSurfaceIndex];

            int maxPixelCount = 2 + Mathf.CeilToInt(Mathf.Sqrt(hdCamera.actualWidth * hdCamera.actualWidth + hdCamera.actualHeight * hdCamera.actualHeight));

            var upVector = hdCamera.mainViewConstants.viewProjMatrix.MultiplyVector(waterSurface.UpVector());
            upVector = new Vector2(upVector.x, -upVector.y).normalized;
            var rightVector = new Vector2(upVector.y, -upVector.x);

            Vector2 rightBounds = GetScreenSpaceBounds(rightVector, hdCamera);
            Vector2 upBounds = GetScreenSpaceBounds(upVector, hdCamera);
            var boundsSS = new float4(rightBounds.x, rightBounds.y, upBounds.x, upBounds.y);

            m_WaterLineBufferSize = maxPixelCount;
            m_ReductionSize = HDUtils.DivRoundUp(Mathf.CeilToInt(boundsSS.y - boundsSS.x), 128);

            if (m_ReductionSize == 0)
            {
                // Can happen if water and camera upVector are aligned
                // Put some parameters that will fill waterline with constant value
                m_ReductionSize = 1;
                boundsSS.z = -1;
                boundsSS.w = 1;
            }

            cb._BoundsSS = boundsSS;
            cb._UpDirectionX = upVector.x;
            cb._UpDirectionY = upVector.y;
            cb._BufferStride = maxPixelCount;
            cb._EnableUnderwater = 1;
        }

        class WaterLineRenderingData
        {
            // Input data
            public int viewCount;
            public Vector2Int threadGroup8;
            public Vector2Int threadGroup16;

            // Shader data
            public ComputeShader underWaterCS;
            public int clearKernel;
            public int lineEvaluationKernel;
            public int boundsPropagationKernel;

            // Constant buffers
            public ShaderVariablesUnderWater underWaterCB;

            public TextureHandle depthBuffer;
            public BufferHandle cameraHeightBuffer;

            // Water Line data
            public BufferHandle waterLine;
            public int waterLineBufferWidth;
            public int reductionSize;
        }

        BufferHandle RenderWaterLine(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthBuffer, WaterGBuffer waterGBuffer)
        {
            // Execute the unique lighting pass
            using (var builder = renderGraph.AddRenderPass<WaterLineRenderingData>("Render Water Line", out var passData, ProfilingSampler.Get(HDProfileId.WaterSurfaceRenderingWaterLine)))
            {
                // Fetch the water surface we will be using
                WaterSurface waterSurface = WaterSurface.instancesAsArray[m_UnderWaterSurfaceIndex];

                // Prepare all the parameters
                passData.viewCount = hdCamera.viewCount;
                passData.threadGroup8 = new Vector2Int(HDUtils.DivRoundUp(hdCamera.actualWidth, 8), HDUtils.DivRoundUp(hdCamera.actualHeight, 8));
                passData.threadGroup16 = new Vector2Int(HDUtils.DivRoundUp(hdCamera.actualWidth, 16), HDUtils.DivRoundUp(hdCamera.actualHeight, 16));

                // Shader data
                passData.underWaterCS = m_UnderWaterRenderingCS;
                passData.clearKernel = m_ClearWaterLine;
                passData.lineEvaluationKernel = m_WaterLineEvaluation1D;
                passData.boundsPropagationKernel = m_WaterLineBoundsPropagation;

                // All the required textures
                passData.depthBuffer = builder.ReadTexture(depthBuffer);
                passData.cameraHeightBuffer = builder.ReadBuffer(waterGBuffer.cameraHeight);

                // Water line data
                passData.waterLine = renderGraph.CreateBuffer(new BufferDesc(m_WaterLineBufferSize * hdCamera.viewCount, sizeof(int), GraphicsBuffer.Target.Structured) { name = "Waterline" });
                passData.waterLine = builder.WriteBuffer(passData.waterLine);

                passData.waterLineBufferWidth = m_WaterLineBufferSize;
                passData.reductionSize = m_ReductionSize;

                // Fill the under water CB
                passData.underWaterCB._WaterRefractionColor = waterSurface.refractionColor;
                passData.underWaterCB._WaterScatteringColor = waterSurface.underWaterScatteringColorMode == WaterSurface.UnderWaterScatteringColorMode.ScatteringColor ? waterSurface.scatteringColor : waterSurface.underWaterScatteringColor;
                passData.underWaterCB._MaxViewDistanceMultiplier = waterSurface.absorptionDistanceMultiplier;
                passData.underWaterCB._OutScatteringCoeff = -Mathf.Log(0.02f) / waterSurface.absorptionDistance;
                passData.underWaterCB._WaterTransitionSize = hdCamera.camera.nearClipPlane * 2.0f;
                passData.underWaterCB._UnderWaterAmbientProbeContribution = waterSurface.underWaterAmbientProbeContribution;

                builder.SetRenderFunc(
                    (WaterLineRenderingData data, RenderGraphContext ctx) =>
                    {
                        // Bind the constant buffer
                        ConstantBuffer.Push(ctx.cmd, data.underWaterCB, data.underWaterCS, HDShaderIDs._ShaderVariablesUnderWater);

                        // Clear water line buffer
                        ctx.cmd.SetComputeBufferParam(data.underWaterCS, data.clearKernel, HDShaderIDs._WaterLineBufferRW, data.waterLine);
                        ctx.cmd.DispatchCompute(data.underWaterCS, data.clearKernel, HDUtils.DivRoundUp(data.waterLineBufferWidth * data.viewCount, 8), 1, 1);

                        // Generate line buffer
                        ctx.cmd.SetComputeTextureParam(data.underWaterCS, data.lineEvaluationKernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                        ctx.cmd.SetComputeTextureParam(data.underWaterCS, data.lineEvaluationKernel, HDShaderIDs._StencilTexture, data.depthBuffer, 0, RenderTextureSubElement.Stencil);
                        ctx.cmd.SetComputeBufferParam(data.underWaterCS, data.lineEvaluationKernel, HDShaderIDs._WaterLineBufferRW, data.waterLine);
                        ctx.cmd.DispatchCompute(data.underWaterCS, data.lineEvaluationKernel, data.threadGroup8.x, data.threadGroup8.y, data.viewCount);

                        // Determine line bounds
                        ctx.cmd.SetComputeBufferParam(data.underWaterCS, data.boundsPropagationKernel, HDShaderIDs._WaterLineBufferRW, data.waterLine);
                        ctx.cmd.SetComputeBufferParam(data.underWaterCS, data.boundsPropagationKernel, HDShaderIDs._WaterCameraHeightBuffer, data.cameraHeightBuffer);
                        ctx.cmd.DispatchCompute(data.underWaterCS, data.boundsPropagationKernel, data.reductionSize, data.viewCount, 1);
                    });

                return passData.waterLine;
            }
        }

        class UnderWaterRenderingData
        {
            // Input data
            public int viewCount;
            public Vector2Int threadGroup8;

            // Shader data
            public ComputeShader underWaterCS;
            public int underWaterKernel;

            // Constant buffers
            public ShaderVariablesWaterRendering waterRenderingCB;
            public ShaderVariablesUnderWater underWaterCB;
            public ShaderVariablesWater waterCB;

            public TextureHandle colorBuffer;
            public TextureHandle depthBuffer;
            public TextureHandle normalBuffer;
            public TextureHandle causticsData;
            public BufferHandle cameraHeightBuffer;

            // WIP buffers
            public TextureHandle waterLineDebugTexture1;
            public TextureHandle waterLineDebugTexture2;

            // Water Line Data
            public BufferHandle waterLine;

            // Water rendered to this buffer
            public TextureHandle outputColorBuffer;
        }

        TextureHandle RenderUnderWaterVolume(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle colorBuffer, TextureHandle depthBuffer, TextureHandle normalBuffer, WaterGBuffer waterGBuffer, out BufferHandle waterLine)
        {
            waterLine = default;

            // Are we in the volume of any surface at all?
            if (m_UnderWaterSurfaceIndex == -1 || !ShouldRenderWater(hdCamera) || !waterGBuffer.valid)
                return colorBuffer;

            waterLine = RenderWaterLine(renderGraph, hdCamera, depthBuffer, waterGBuffer);

            // Execute the unique lighting pass
            using (var builder = renderGraph.AddRenderPass<UnderWaterRenderingData>("Render Under Water", out var passData, ProfilingSampler.Get(HDProfileId.WaterSurfaceRenderingUnderWater)))
            {
                // Fetch the water surface we will be using
                WaterSurface waterSurface = WaterSurface.instancesAsArray[m_UnderWaterSurfaceIndex];

                // Prepare all the parameters
                passData.viewCount = hdCamera.viewCount;
                passData.threadGroup8 = new Vector2Int(HDUtils.DivRoundUp(hdCamera.actualWidth, 8), HDUtils.DivRoundUp(hdCamera.actualHeight, 8));

                // Shader data
                passData.underWaterCS = m_UnderWaterRenderingCS;
                passData.underWaterKernel = waterSurface.causticsDirectionalShadow ? m_UnderWaterDirectionalCausticsKernel : m_UnderWaterKernel;

                // All the required textures
                passData.colorBuffer = builder.ReadTexture(colorBuffer);
                passData.depthBuffer = builder.UseDepthBuffer(depthBuffer, DepthAccess.Read);
                passData.normalBuffer = builder.ReadTexture(normalBuffer);
                passData.cameraHeightBuffer = builder.ReadBuffer(waterGBuffer.cameraHeight);
                passData.waterLine = builder.ReadBuffer(waterLine);

                // Bind the caustics buffer that may be required
                passData.causticsData = waterSurface.caustics ? renderGraph.ImportTexture(waterSurface.simulation.gpuBuffers.causticsBuffer) : renderGraph.defaultResources.blackTexture;

                // Fill the under water CB
                passData.underWaterCB._WaterScatteringColor = waterSurface.underWaterScatteringColorMode == WaterSurface.UnderWaterScatteringColorMode.ScatteringColor ? waterSurface.scatteringColor : waterSurface.underWaterScatteringColor;
                passData.underWaterCB._WaterRefractionColor = waterSurface.refractionColor;
                passData.underWaterCB._MaxViewDistanceMultiplier = waterSurface.absorptionDistanceMultiplier;
                passData.underWaterCB._OutScatteringCoeff = -Mathf.Log(0.02f) / waterSurface.absorptionDistance;
                passData.underWaterCB._UnderWaterAmbientProbeContribution = waterSurface.underWaterAmbientProbeContribution;

                // Fill the water rendering CB
                passData.waterRenderingCB._CausticsIntensity = waterSurface.caustics ? waterSurface.causticsIntensity : 0.0f;
                passData.waterRenderingCB._CausticsShadowIntensity = waterSurface.causticsDirectionalShadow ? waterSurface.causticsDirectionalShadowDimmer : 1.0f;
                passData.waterRenderingCB._CausticsPlaneBlendDistance = waterSurface.causticsPlaneBlendDistance;
                passData.waterRenderingCB._CausticsMaxLOD = EvaluateCausticsMaxLOD(waterSurface.causticsResolution);
                passData.waterRenderingCB._CausticsTilingFactor = 1.0f / waterSurface.causticsTilingFactor;
                passData.waterRenderingCB._PatchOffset = waterSurface.transform.position;
                passData.waterRenderingCB._WaterSurfaceTransform = waterSurface.simulation.rendering.waterToWorldMatrix;
                passData.waterRenderingCB._WaterSurfaceTransform_Inverse = waterSurface.simulation.rendering.worldToWaterMatrix;
                passData.waterRenderingCB._WaterAmbientProbe = m_WaterAmbientProbe;

                // Fill the water CB
                int causticsBandIndex = HDRenderPipeline.SanitizeCausticsBand(waterSurface.causticsBand, waterSurface.simulation.numActiveBands);
                passData.waterCB._CausticsRegionSize = waterSurface.simulation.spectrum.patchSizes[causticsBandIndex];
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

                        // Apply the under water post process.
                        ctx.cmd.SetComputeTextureParam(data.underWaterCS, data.underWaterKernel, HDShaderIDs._WaterCausticsDataBuffer, data.causticsData);
                        ctx.cmd.SetComputeTextureParam(data.underWaterCS, data.underWaterKernel, HDShaderIDs._CameraColorTexture, data.colorBuffer);
                        ctx.cmd.SetComputeTextureParam(data.underWaterCS, data.underWaterKernel, HDShaderIDs._DepthTexture, data.depthBuffer);
                        ctx.cmd.SetComputeTextureParam(data.underWaterCS, data.underWaterKernel, HDShaderIDs._StencilTexture, data.depthBuffer, 0, RenderTextureSubElement.Stencil);
                        ctx.cmd.SetComputeBufferParam(data.underWaterCS, data.underWaterKernel, HDShaderIDs._WaterCameraHeightBuffer, data.cameraHeightBuffer);
                        ctx.cmd.SetComputeBufferParam(data.underWaterCS, data.underWaterKernel, HDShaderIDs._WaterLineBuffer, data.waterLine);
                        ctx.cmd.SetComputeTextureParam(data.underWaterCS, data.underWaterKernel, HDShaderIDs._CameraColorTextureRW, data.outputColorBuffer);
                        ctx.cmd.SetComputeTextureParam(data.underWaterCS, data.underWaterKernel, HDShaderIDs._NormalBufferTexture, data.normalBuffer);
                        ctx.cmd.DispatchCompute(data.underWaterCS, data.underWaterKernel, data.threadGroup8.x, data.threadGroup8.y, data.viewCount);
                    });

                // Return the new color buffer
                return passData.outputColorBuffer;
            }
        }
    }
}
