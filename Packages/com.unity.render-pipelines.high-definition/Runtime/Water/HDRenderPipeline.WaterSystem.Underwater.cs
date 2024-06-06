using Unity.Mathematics;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    partial class WaterSystem
    {
        // Flag that allows us to track in which current under water volume we are.
        internal int m_UnderWaterSurfaceIndex;
        internal Vector4 m_UnderWaterUpHeight;

        // Water line data
        int m_WaterLineBufferSize;
        int m_ReductionSize;

        // Underwater kernels
        ComputeShader m_WaterLineCS;
        int m_ClearWaterLine;
        int m_WaterLineEvaluation1D;
        int m_WaterLineBoundsPropagation;

        GraphicsBuffer m_DefaultWaterLineBuffer;

        void InitializeUnderWaterResources()
        {
            // Kernels
            m_WaterLineCS = m_RuntimeResources.waterLineCS;
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
            Vector3 cameraWSPos = hdCamera.mainViewConstants.worldSpaceCameraPos;

            // First we need to define in which volume the camera is.
            // We consider that the camera can only be in one volume at a time and it is the first one
            // we find.
            int currentPriority = -1;
            for (int surfaceIdx = 0; surfaceIdx < numWaterSurfaces; ++surfaceIdx)
            {
                // Grab the current water surface
                WaterSurface currentWater = waterSurfaces[surfaceIdx];

                // Does this surface need underwater rendering?
                if (!currentWater.underWater || currentWater.debugMode != WaterDebugMode.None)
                    continue;

                var upDirection = currentWater.UpVector();

                // If the surface is infinite, we need to check if the camera is between the top plane + max displacement  and the top plane - volume depth
                if (currentWater.IsInfinite())
                {
                    // Maximal possible wave height of the current setup
                    float maxWaveHeight = WaterSystem.EvaluateMaxAmplitude(currentWater.simulation.spectrum.patchSizes.x, currentWater.simulation.spectrum.patchWindSpeed.x * WaterConsts.k_MeterPerSecondToKilometerPerHour);
                    float maxDeformation = Mathf.Max(Mathf.Max(maxWaveHeight * 2.0f, 0.1f), currentWater.volumeHeight);

                    // Evaluate the vertical boundaries of the volume
                    float surfaceHeight = math.dot(currentWater.transform.position, upDirection);
                    float cameraHeight = math.dot(cameraWSPos, upDirection);
                    float topPlane = surfaceHeight + maxDeformation;
                    float bottomPlane = surfaceHeight - currentWater.volumeDepth;

                    // If both conditions are true, the camera is inside this volume
                    bool isBetweenPlanes = cameraHeight > bottomPlane && cameraHeight < topPlane;
                    if (isBetweenPlanes && currentPriority < currentWater.volumePrority)
                    {
                        float heightRWS = (surfaceHeight - cameraHeight) - maxDeformation;
                        m_UnderWaterUpHeight = new Vector4(upDirection.x, upDirection.y, upDirection.z, heightRWS);
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
                            m_UnderWaterUpHeight = new Vector4(upDirection.x, upDirection.y, upDirection.z, float.MinValue);
                            m_UnderWaterSurfaceIndex = surfaceIdx;
                            currentPriority = currentWater.volumePrority;
                        }
                    }
                }
            }
        }

        internal RenderTexture GetUnderWaterSurfaceCaustics()
        {
            return WaterSurface.instancesAsArray[m_UnderWaterSurfaceIndex].simulation.gpuBuffers.causticsBuffer.rt;
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

        unsafe Vector4 EvaluateWaterAmbientProbe(HDCamera hdCamera, float ambientProbeDimmer)
        {
            SphericalHarmonicsL2 probeSH = SphericalHarmonicMath.RescaleCoefficients(m_RenderPipeline.skyManager.GetAmbientProbe(hdCamera), ambientProbeDimmer);
            Vector4 ambient = HDRenderPipeline.EvaluateAmbientProbe(probeSH, Vector3.up);
            return new Vector4(ambient.x, ambient.y, ambient.z, ambient.x * 0.2126729f + ambient.y * 0.7151522f + ambient.z * 0.072175f);
        }

        internal unsafe void UpdateShaderVariablesGlobalWater(ref ShaderVariablesGlobal cb, HDCamera hdCamera)
        {
            cb._EnableWater = 0;
            cb._UnderWaterSurfaceIndex = -1;

            // Put some parameters that will fill waterline with constant value
            cb._BoundsSS = new float4(0, 0, -1, 1);
            cb._UpDirectionX = 0;
            cb._UpDirectionY = 1;
            cb._BufferStride = 0;

            if (!ShouldRenderWater(hdCamera))
                return;

            cb._EnableWater = 1;

            // Evaluate the ambient probe for the frame
            WaterRendering settings = hdCamera.volumeStack.GetComponent<WaterRendering>();

            cb._WaterAmbientProbe = EvaluateWaterAmbientProbe(hdCamera, settings.ambientProbeDimmer.value);

            EvaluateUnderWaterSurface(hdCamera);
            if (m_UnderWaterSurfaceIndex == -1)
                return;

            WaterSurface waterSurface = WaterSurface.instancesAsArray[m_UnderWaterSurfaceIndex];

            int maxPixelCount = 2 + Mathf.CeilToInt(Mathf.Sqrt(hdCamera.actualWidth * hdCamera.actualWidth + hdCamera.actualHeight * hdCamera.actualHeight));

            var upDirection = hdCamera.mainViewConstants.viewProjMatrix.MultiplyVector(waterSurface.UpVector());
            if (upDirection.sqrMagnitude < 0.001f)
                upDirection.y = -1.0f;
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

            var albedo = new Vector3(waterSurface.scatteringColor.r, waterSurface.scatteringColor.g, waterSurface.scatteringColor.b);
            var extinction = waterSurface.underWaterExtinction;

            cb._UnderWaterScatteringExtinction = new Vector4(albedo.x*extinction.x, albedo.y*extinction.y, albedo.z*extinction.z, (extinction.x+extinction.y+extinction.z) / 3.0f);
            cb._UnderWaterSurfaceIndex = m_UnderWaterSurfaceIndex;
            cb._UnderWaterUpHeight = m_UnderWaterUpHeight;
            cb._UnderWaterSurfaceTransform_Inverse = waterSurface.simulation.rendering.worldToWaterMatrix;
            cb._UnderWaterCausticsIntensity = waterSurface.caustics ? waterSurface.causticsIntensity : 0.0f;
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

        internal void RenderWaterLine(RenderGraph renderGraph, HDCamera hdCamera, TextureHandle depthBuffer, ref HDRenderPipeline.TransparentPrepassOutput refractionOutput)
        {
            // Are we in the volume of any surface at all?
            if (m_UnderWaterSurfaceIndex == -1
                || !ShouldRenderWater(hdCamera)
                || !refractionOutput.waterGBuffer.valid)
                return;

            // TODO: depending on m_UnderWaterUpHeight.w and near clip plane value, we can skip this pass entirely
            // cause we can be sure camera is underwater. However we need to make sure GetUnderWaterDistance() still returns sensible values on GPU
            // either init the waterline buffer with good values (that requires a compute dispatch)
            // or find values for _BoundsSS & co to make sure the function always returns a negative value

            // Execute the unique lighting pass
            using (var builder = renderGraph.AddRenderPass<WaterLineRenderingData>("Render Water Line", out var passData, ProfilingSampler.Get(HDProfileId.WaterLineRendering)))
            {
                // Fetch the water surface we will be using
                WaterSurface waterSurface = WaterSurface.instancesAsArray[m_UnderWaterSurfaceIndex];

                refractionOutput.waterLine = renderGraph.CreateBuffer(new BufferDesc(m_WaterLineBufferSize * hdCamera.viewCount, sizeof(int), GraphicsBuffer.Target.Structured) { name = "Waterline" });
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
