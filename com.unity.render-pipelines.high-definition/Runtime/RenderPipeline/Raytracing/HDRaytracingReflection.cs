using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
#if ENABLE_RAYTRACING
    public class HDRaytracingReflections
    {
        // External structures
        HDRenderPipelineAsset m_PipelineAsset = null;
        SkyManager m_SkyManager = null;
        HDRaytracingManager m_RaytracingManager = null;
        SharedRTManager m_SharedRTManager = null;

        // The target denoising kernel
        static int m_KernelFilter;

        // Intermediate buffer that stores the reflection pre-denoising
        RTHandleSystem.RTHandle m_IntermediateBuffer = null;

        // Light cluster structure
        public HDRaytracingLightCluster m_LightCluster = null;

        // String values
        const string m_RayGenShaderName = "RayGenReflections";
        const string m_MissShaderName = "MissShaderReflections";
        const string m_ClosestHitShaderName = "ClosestHitMain";

        // Shader Identifiers
        public static readonly int _DenoiseRadius = Shader.PropertyToID("_DenoiseRadius");
        public static readonly int _GaussianSigma = Shader.PropertyToID("_GaussianSigma");

        public static readonly int _RaytracingLightCluster = Shader.PropertyToID("_RaytracingLightCluster");
        public static readonly int _MinClusterPos = Shader.PropertyToID("_MinClusterPos");
        public static readonly int _MaxClusterPos = Shader.PropertyToID("_MaxClusterPos");
        public static readonly int _LightPerCellCount = Shader.PropertyToID("_LightPerCellCount");
        public static readonly int _LightDatasRT = Shader.PropertyToID("_LightDatasRT");
        public static readonly int _PunctualLightCountRT = Shader.PropertyToID("_PunctualLightCountRT");
        public static readonly int _AreaLightCountRT = Shader.PropertyToID("_AreaLightCountRT");
        public static readonly int _PixelSpreadAngle = Shader.PropertyToID("_PixelSpreadAngle");

        public HDRaytracingReflections()
        {
        }

        public void Init(HDRenderPipelineAsset asset, SkyManager skyManager, HDRaytracingManager raytracingManager, SharedRTManager sharedRTManager)
        {
            // Keep track of the pipeline asset
            m_PipelineAsset = asset;

            // Keep track of the sky manager
            m_SkyManager = skyManager;

            // keep track of the ray tracing manager
            m_RaytracingManager = raytracingManager;

            // Keep track of the shared rt manager
            m_SharedRTManager = sharedRTManager;

            m_IntermediateBuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "IntermediateReflectionBuffer");

            // Allocate the light cluster
            m_LightCluster = new HDRaytracingLightCluster();
            m_LightCluster.Initialize(asset, raytracingManager);

        }

        public void Release()
        {
            m_LightCluster.ReleaseResources();
            m_LightCluster = null;

            RTHandles.Release(m_IntermediateBuffer);
        }

        public void RenderReflections(HDCamera hdCamera, CommandBuffer cmd, RTHandleSystem.RTHandle outputTexture, ScriptableRenderContext renderContext)
        {
            // First thing to check is: Do we have a valid ray-tracing environment?
            HDRaytracingEnvironment rtEnvironement = m_RaytracingManager.CurrentEnvironment();
            Texture2DArray noiseTexture = m_RaytracingManager.m_RGNoiseTexture;
            ComputeShader bilateralFilter = m_PipelineAsset.renderPipelineResources.shaders.reflectionBilateralFilterCS;
            RaytracingShader reflectionShader = m_PipelineAsset.renderPipelineResources.shaders.reflectionRaytracing;
            bool missingResources = rtEnvironement == null || noiseTexture == null || bilateralFilter == null || reflectionShader == null;

            // Try to grab the acceleration structure and the list of HD lights for the target camera
            RaytracingAccelerationStructure accelerationStructure = m_RaytracingManager.RequestAccelerationStructure(hdCamera);
            List<HDAdditionalLightData> lightData = m_RaytracingManager.RequestHDLightList(hdCamera);

            // If no acceleration structure available, end it now
            if (accelerationStructure == null || lightData == null || missingResources)
                return;

            // Evaluate the light cluster
            // TODO: Do only this once per frame and share it between primary visibility and reflection (if any of them request it)
            m_LightCluster.EvaluateLightClusters(cmd, hdCamera, lightData);

            // Define the shader pass to use for the reflection pass
            cmd.SetRaytracingShaderPass(reflectionShader, "RTRaytrace_Reflections");

            // Set the acceleration structure for the pass
            cmd.SetRaytracingAccelerationStructure(reflectionShader, HDShaderIDs._RaytracingAccelerationStructureName, accelerationStructure);

            // Inject the ray-tracing noise data
            cmd.SetRaytracingTextureParam(reflectionShader, m_RayGenShaderName, HDShaderIDs._RaytracingNoiseTexture, noiseTexture);
            cmd.SetRaytracingIntParams(reflectionShader, HDShaderIDs._RaytracingNoiseResolution, noiseTexture.width);
            cmd.SetRaytracingIntParams(reflectionShader, HDShaderIDs._RaytracingNumNoiseLayers, noiseTexture.depth);

            // Inject the ray generation data
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingRayBias, rtEnvironement.rayBias);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingRayMaxLength, rtEnvironement.reflRayLength);
            cmd.SetRaytracingIntParams(reflectionShader, HDShaderIDs._RaytracingNumSamples, rtEnvironement.reflNumMaxSamples);

            // Set the data for the ray generation
            cmd.SetRaytracingTextureParam(reflectionShader, m_RayGenShaderName, HDShaderIDs._SsrLightingTextureRW, m_IntermediateBuffer);
            cmd.SetRaytracingTextureParam(reflectionShader, m_RayGenShaderName, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetRaytracingTextureParam(reflectionShader, m_RayGenShaderName, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());

            // Compute the pixel spread value
            float pixelSpreadAngle = Mathf.Atan(2.0f * Mathf.Tan(hdCamera.camera.fieldOfView * Mathf.PI / 360.0f) / Mathf.Min(hdCamera.actualWidth, hdCamera.actualHeight));
            cmd.SetRaytracingFloatParam(reflectionShader, _PixelSpreadAngle, pixelSpreadAngle);

            if(lightData.Count != 0)
            {
                // LightLoop data
                cmd.SetGlobalBuffer(_RaytracingLightCluster, m_LightCluster.GetCluster());
                cmd.SetGlobalBuffer(_LightDatasRT, m_LightCluster.GetLightDatas());
                cmd.SetGlobalVector(_MinClusterPos, m_LightCluster.GetMinClusterPos());
                cmd.SetGlobalVector(_MaxClusterPos, m_LightCluster.GetMaxClusterPos());
                cmd.SetGlobalInt(_LightPerCellCount, rtEnvironement.maxNumLightsPercell);
                cmd.SetGlobalInt(_PunctualLightCountRT, m_LightCluster.GetPunctualLightCount());
                cmd.SetGlobalInt(_AreaLightCountRT, m_LightCluster.GetAreaLightCount());
            }

            // Set the data for the ray miss
            cmd.SetRaytracingTextureParam(reflectionShader, m_MissShaderName, HDShaderIDs._SkyTexture, m_SkyManager.skyReflection);

            // Run the calculus
            cmd.DispatchRays(reflectionShader, m_RayGenShaderName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, 1);

            using (new ProfilingSample(cmd, "Filter Reflection", CustomSamplerId.RaytracingFilterReflection.GetSampler()))
            {
                switch (rtEnvironement.reflFilterMode)
                {
                    case HDRaytracingEnvironment.ReflectionsFilterMode.Bilateral:
                    {
                        // Fetch the right filter to use
                        m_KernelFilter = bilateralFilter.FindKernel("GaussianBilateralFilter");

                        // Inject all the parameters for the compute
                        cmd.SetComputeIntParam(bilateralFilter, _DenoiseRadius, rtEnvironement.reflBilateralRadius);
                        cmd.SetComputeFloatParam(bilateralFilter, _GaussianSigma, rtEnvironement.reflBilateralSigma);
                        cmd.SetComputeTextureParam(bilateralFilter, m_KernelFilter, "_SourceTexture", m_IntermediateBuffer);
                        cmd.SetComputeTextureParam(bilateralFilter, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                        cmd.SetComputeTextureParam(bilateralFilter, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());

                        // Set the output slot
                        cmd.SetComputeTextureParam(bilateralFilter, m_KernelFilter, "_OutputTexture", outputTexture);

                        // Texture dimensions
                        int texWidth = outputTexture.rt.width;
                        int texHeight = outputTexture.rt.width;

                        // Evaluate the dispatch parameters
                        int areaTileSize = 8;
                        int numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
                        int numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;

                        // Compute the texture
                        cmd.DispatchCompute(bilateralFilter, m_KernelFilter, numTilesX, numTilesY, 1);
                    }
                    break;
                    case HDRaytracingEnvironment.ReflectionsFilterMode.None:
                    {
                        HDUtils.BlitCameraTexture(cmd, hdCamera, m_IntermediateBuffer, outputTexture);
                    }
                    break;
                }
            }
        }
    }
#endif
}
