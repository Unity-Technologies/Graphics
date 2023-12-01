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
        ComputeShader m_WaterLineCS;
        int m_ClearWaterLine;
        int m_WaterLineEvaluation1D;
        int m_WaterLineBoundsPropagation;

        GraphicsBuffer m_DefaultWaterLineBuffer;

        // Resolution of the binning tile
        const int underWaterTileSize = 16;

        void InitializeUnderWaterResources()
        {
            // Kernels
            m_WaterLineCS = m_Asset.renderPipelineResources.shaders.waterLineCS;
            m_ClearWaterLine = m_WaterLineCS.FindKernel("ClearWaterLine");
            m_WaterLineEvaluation1D = m_WaterLineCS.FindKernel("LineEvaluation1D");
            m_WaterLineBoundsPropagation = m_WaterLineCS.FindKernel("BoundsPropagation");
        }

        void EvaluateUnderWaterSurface(HDCamera hdCamera)
        {
            // Flag that allows us to track which surface is the one we will be using for the under water rendering
            m_UnderWaterSurfaceIndex = -1;

            // Grab all the water surfaces in the scene
            var waterSurfaces = WaterSurface.instancesAsArray;
            int numWaterSurfaces = Mathf.Min(WaterSurface.instanceCount, k_MaxNumWaterSurfaceProfiles);

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

        float2 GetScreenSpaceBounds(float2 direction, HDCamera hdCamera)
        {
            float corner_1 = math.dot(direction, new float2(0, 0));
            float corner_2 = math.dot(direction, new float2(0, hdCamera.actualHeight));
            float corner_3 = math.dot(direction, new float2(hdCamera.actualWidth, 0));
            float corner_4 = math.dot(direction, new float2(hdCamera.actualWidth, hdCamera.actualHeight));

            return new float2(Min4(corner_1, corner_2, corner_3, corner_4), Max4(corner_1, corner_2, corner_3, corner_4));
        }

        unsafe void UpdateShaderVariablesGlobalWater(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            cb._EnableWater = 0;
            cb._UnderWaterSurfaceIndex = -1;

            if (!ShouldRenderWater(hdCamera))
                return;

            cb._EnableWater = 1;

            // Evaluate the ambient probe for the frame
            WaterRendering settings = hdCamera.volumeStack.GetComponent<WaterRendering>();
            m_WaterAmbientProbe = EvaluateWaterAmbientProbe(hdCamera, settings.ambientProbeDimmer.value);
            cb._UnderWaterAmbientProbeLuminance = m_WaterAmbientProbe.w;


            EvaluateUnderWaterSurface(hdCamera);
            if (m_UnderWaterSurfaceIndex == -1)
            {
                // Put some parameters that will fill waterline with constant value
                cb._BoundsSS = new float4(0, 0, -1, 1);
                cb._UpDirectionX = 0;
                cb._UpDirectionY = 1;
                cb._BufferStride = 0;

                return;
            }

            WaterSurface waterSurface = WaterSurface.instancesAsArray[m_UnderWaterSurfaceIndex];

            int maxPixelCount = 2 + Mathf.CeilToInt(Mathf.Sqrt(hdCamera.actualWidth * hdCamera.actualWidth + hdCamera.actualHeight * hdCamera.actualHeight));

            var upDirection = hdCamera.mainViewConstants.viewProjMatrix.MultiplyVector(waterSurface.UpVector());
            var upVector = new Vector2(upDirection.x, -upDirection.y).normalized;
            var rightVector = new Vector2(upVector.y, -upVector.x);

            Vector2 rightBounds = GetScreenSpaceBounds(rightVector, hdCamera);
            Vector2 upBounds = GetScreenSpaceBounds(upVector, hdCamera);
            var boundsSS = new float4(rightBounds.x, rightBounds.y, upBounds.x, upBounds.y);

            m_WaterLineBufferSize = maxPixelCount;
            m_ReductionSize = HDUtils.DivRoundUp(Mathf.CeilToInt(boundsSS.y - boundsSS.x), 64);

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

            int causticsBandIndex = SanitizeCausticsBand(waterSurface.causticsBand, waterSurface.simulation.numActiveBands);

            cb._UnderWaterSurfaceIndex = m_UnderWaterSurfaceIndex;
            cb._UnderWaterSurfaceTransform_Inverse = waterSurface.simulation.rendering.worldToWaterMatrix;
            cb._UnderWaterCausticsIntensity = waterSurface.caustics ? waterSurface.causticsIntensity : 0.0f; ;
            cb._UnderWaterCausticsPlaneBlendDistance = waterSurface.causticsPlaneBlendDistance;
            cb._UnderWaterCausticsTilingFactor = 1.0f / waterSurface.causticsTilingFactor;
            cb._UnderWaterCausticsMaxLOD = EvaluateCausticsMaxLOD(waterSurface.causticsResolution);
            cb._UnderWaterCausticsShadowIntensity = waterSurface.causticsDirectionalShadow ? waterSurface.causticsDirectionalShadowDimmer : 1.0f;
            cb._UnderWaterCausticsRegionSize = waterSurface.simulation.spectrum.patchSizes[causticsBandIndex];
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

            public TextureHandle depthBuffer;
            public BufferHandle cameraHeightBuffer;

            // Water Line data
            public BufferHandle waterLine;
            public int waterLineBufferWidth;
            public int reductionSize;
        }

        void RenderWaterLine(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthBuffer, ref TransparentPrepassOutput refractionOutput)
        {
            // Are we in the volume of any surface at all?
            if (m_UnderWaterSurfaceIndex == -1
                || !ShouldRenderWater(hdCamera)
                || !refractionOutput.waterGBuffer.valid)
                return;

            // Execute the unique lighting pass
            using (var builder = renderGraph.AddRenderPass<WaterLineRenderingData>("Render Water Line", out var passData, ProfilingSampler.Get(HDProfileId.WaterSurfaceRenderingWaterLine)))
            {
                // Fetch the water surface we will be using
                WaterSurface waterSurface = WaterSurface.instancesAsArray[m_UnderWaterSurfaceIndex];

                refractionOutput.waterLine = renderGraph.CreateBuffer(new BufferDesc(m_WaterLineBufferSize * hdCamera.viewCount, sizeof(int), GraphicsBuffer.Target.Structured) { name = "Waterline" });
                refractionOutput.waterAmbientProbe = m_WaterAmbientProbe;
                refractionOutput.underWaterSurface = WaterSurface.instancesAsArray[m_UnderWaterSurfaceIndex];

                // Prepare all the parameters
                passData.viewCount = hdCamera.viewCount;
                passData.threadGroup8 = new Vector2Int(HDUtils.DivRoundUp(hdCamera.actualWidth, 8), HDUtils.DivRoundUp(hdCamera.actualHeight, 8));
                passData.threadGroup16 = new Vector2Int(HDUtils.DivRoundUp(hdCamera.actualWidth, 16), HDUtils.DivRoundUp(hdCamera.actualHeight, 16));

                // Shader data
                passData.underWaterCS = m_WaterLineCS;
                passData.clearKernel = m_ClearWaterLine;
                passData.lineEvaluationKernel = m_WaterLineEvaluation1D;
                passData.boundsPropagationKernel = m_WaterLineBoundsPropagation;

                // All the required textures
                passData.depthBuffer = builder.ReadTexture(depthBuffer);
                passData.cameraHeightBuffer = builder.ReadBuffer(refractionOutput.waterGBuffer.cameraHeight);

                // Water line data
                passData.waterLine = builder.WriteBuffer(refractionOutput.waterLine);

                passData.waterLineBufferWidth = m_WaterLineBufferSize;
                passData.reductionSize = m_ReductionSize;

                builder.SetRenderFunc(
                    (WaterLineRenderingData data, RenderGraphContext ctx) =>
                    {
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
            }
        }
    }
}
