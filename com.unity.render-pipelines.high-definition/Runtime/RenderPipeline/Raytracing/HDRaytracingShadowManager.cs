using UnityEngine;
using UnityEngine.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
#if ENABLE_RAYTRACING
    public class HDRaytracingShadowManager
    {
        HDRenderPipelineAsset m_PipelineAsset = null;

        HDRaytracingManager m_RaytracingManager = null;
        SharedRTManager m_SharedRTManager = null;
        LightLoop m_LightLoop = null;
        GBufferManager m_GbufferManager = null;
        static int m_KernelFilter;

        // Buffers that hold the intermediate data of the shadow algorithm
        RTHandleSystem.RTHandle m_SNBuffer = null;
        RTHandleSystem.RTHandle m_UNBuffer = null;

        // Array that holds the shadow textures for the area lights
        RTHandleSystem.RTHandle m_AreaShadowTextureArray = null;

        // String values
        const string m_RayGenShaderName = "RayGenShadows";
        const string m_MissShaderName = "MissShaderShadows";

        public static readonly int _TargetAreaLight = Shader.PropertyToID("_TargetAreaLight");
        public static readonly int _SNBuffer = Shader.PropertyToID("_SNTextureUAV");
        public static readonly int _UNBuffer = Shader.PropertyToID("_UNTextureUAV");
        public static readonly int _DSNDUNUAV = Shader.PropertyToID("_DSNDUNUAV");

        // Denoise data
        public static readonly int _DenoiseRadius = Shader.PropertyToID("_DenoiseRadius");
        public static readonly int _GaussianSigma = Shader.PropertyToID("_GaussianSigma");

        // output Slot
        public static readonly int _ShadowSlot = Shader.PropertyToID("_ShadowSlot");
        public static readonly int _AreaShadowTexture = Shader.PropertyToID("_AreaShadowTexture");

        public HDRaytracingShadowManager()
        {
        }

        public void Init(HDRenderPipelineAsset asset, HDRaytracingManager raytracingManager, SharedRTManager sharedRTManager, LightLoop lightLoop, GBufferManager gbufferManager)
        {
            // Keep track of the pipeline asset
            m_PipelineAsset = asset;

            // keep track of the ray tracing manager
            m_RaytracingManager = raytracingManager;

            // Keep track of the shared rt manager
            m_SharedRTManager = sharedRTManager;

            // The lightloop that holds all the lights of the scene
            m_LightLoop = lightLoop;

            // GBuffer manager that holds all the data for shading the samples
            m_GbufferManager = gbufferManager;

            // Allocate the intermediate buffers
            m_SNBuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.ARGBHalf, sRGB: false, enableRandomWrite: true, useMipMap: false, name: "SNBuffer");
            m_UNBuffer = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.ARGBHalf, sRGB: false, enableRandomWrite: true, useMipMap: false, name: "UNBuffer");
            m_AreaShadowTextureArray = RTHandles.Alloc(Vector2.one, slices:4, dimension: TextureDimension.Tex2DArray ,filterMode: FilterMode.Point, colorFormat: RenderTextureFormat.ARGBHalf, sRGB: false, enableRandomWrite: true, useMipMap: false, name: "AreaShadowArrayBuffer");
        }

        public void Release()
        {
            RTHandles.Release(m_AreaShadowTextureArray);
            RTHandles.Release(m_UNBuffer);
            RTHandles.Release(m_SNBuffer);
        }

        public bool RenderAreaShadows(HDCamera hdCamera, CommandBuffer cmd, ScriptableRenderContext renderContext)
        {
            // Bind the texture because everyone needs it
            cmd.SetGlobalTexture(_AreaShadowTexture, m_AreaShadowTextureArray);

            // First thing to check is: Do we have a valid ray-tracing environment?
            HDRaytracingEnvironment rtEnvironement = m_RaytracingManager.CurrentEnvironment();
            Texture2DArray noiseTexture = m_RaytracingManager.m_RGNoiseTexture;
            if (rtEnvironement == null || noiseTexture == null)
            {
                return false;
            }

            // Try to grab the acceleration structure for the target camera
            HDRayTracingFilter raytracingFilter = hdCamera.camera.gameObject.GetComponent<HDRayTracingFilter>();
            RaytracingAccelerationStructure accelerationStructure = null;
            if (raytracingFilter != null)
            {
                accelerationStructure = m_RaytracingManager.RequestAccelerationStructure(raytracingFilter.layermask);
            }
            else if (hdCamera.camera.cameraType == CameraType.SceneView || hdCamera.camera.cameraType == CameraType.Preview)
            {
                // For the scene view, we want to use the default acceleration structure
                accelerationStructure = m_RaytracingManager.RequestAccelerationStructure(m_PipelineAsset.renderPipelineSettings.editorRaytracingFilterLayerMask);
            }
            // If no acceleration structure available, end it now
            if (accelerationStructure == null)
            {
                return false;
            }

            // If no shadows shader is available, just skip right away
            if (m_PipelineAsset.renderPipelineResources.shaders.areaBillateralFilterCS == null || m_PipelineAsset.renderPipelineResources.shaders.shadowsRaytracing == null)
            {
                return false;
            }

            // Fetch the shaders we will be using
            RaytracingShader shadowsShader = m_PipelineAsset.renderPipelineResources.shaders.shadowsRaytracing;
            ComputeShader bilateralFilter = m_PipelineAsset.renderPipelineResources.shaders.areaBillateralFilterCS;

            // Fetch the filter kernel
            m_KernelFilter = bilateralFilter.FindKernel("AreaBilateralShadow");

            // Define the shader pass to use for the reflection pass
            cmd.SetRaytracingShaderPass(shadowsShader, "RTRaytrace_Visibility");

            // Set the acceleration structure for the pass
            cmd.SetRaytracingAccelerationStructure(shadowsShader, HDShaderIDs._RaytracingAccelerationStructureName, accelerationStructure);

            // Inject the ray-tracing noise data
            cmd.SetRaytracingTextureParam(shadowsShader, m_RayGenShaderName, HDShaderIDs._RaytracingNoiseTexture, noiseTexture);
            cmd.SetRaytracingIntParams(shadowsShader, HDShaderIDs._RaytracingNoiseResolution, noiseTexture.width);
            cmd.SetRaytracingIntParams(shadowsShader, HDShaderIDs._RaytracingNumNoiseLayers, noiseTexture.depth);

            // Inject the ray generation data
            cmd.SetRaytracingFloatParams(shadowsShader, HDShaderIDs._RaytracingRayBias, rtEnvironement.rayBias);

            int numLights = m_LightLoop.m_lightList.lights.Count;

            for(int lightIdx = 0; lightIdx < numLights; ++lightIdx)
            {
                // If this is not a rectangular area light or it won't have shadows, skip it
                if(m_LightLoop.m_lightList.lights[lightIdx].lightType != GPULightType.Rectangle || m_LightLoop.m_lightList.lights[lightIdx].shadowIndex == -1) continue;
                using (new ProfilingSample(cmd, "Raytrace Area Shadow", CustomSamplerId.Raytracing.GetSampler()))
                {
                    // Inject the light data
                    cmd.SetRaytracingBufferParam(shadowsShader, m_RayGenShaderName, HDShaderIDs._LightDatas, m_LightLoop.lightDatas);
                    cmd.SetRaytracingIntParam(shadowsShader, _TargetAreaLight, lightIdx);
                    cmd.SetRaytracingIntParam(shadowsShader, HDShaderIDs._RaytracingNumSamples, rtEnvironement.shadowNumSamples);

                    // Set the data for the ray generation
                    cmd.SetRaytracingTextureParam(shadowsShader, m_RayGenShaderName, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                    cmd.SetRaytracingTextureParam(shadowsShader, m_RayGenShaderName, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                    cmd.SetRaytracingTextureParam(shadowsShader, m_RayGenShaderName, HDShaderIDs._GBufferTexture[0], m_GbufferManager.GetBuffer(0));
                    cmd.SetRaytracingTextureParam(shadowsShader, m_RayGenShaderName, HDShaderIDs._GBufferTexture[1], m_GbufferManager.GetBuffer(1));
                    cmd.SetRaytracingTextureParam(shadowsShader, m_RayGenShaderName, HDShaderIDs._GBufferTexture[2], m_GbufferManager.GetBuffer(2));
                    cmd.SetRaytracingTextureParam(shadowsShader, m_RayGenShaderName, HDShaderIDs._GBufferTexture[3], m_GbufferManager.GetBuffer(3));

                    // Set the output textures
                    cmd.SetRaytracingTextureParam(shadowsShader, m_RayGenShaderName, _SNBuffer, m_SNBuffer);
                    cmd.SetRaytracingTextureParam(shadowsShader, m_RayGenShaderName, _UNBuffer, m_UNBuffer);


                    // Run the shadow evaluation
                    cmd.DispatchRays(shadowsShader, m_RayGenShaderName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, 1);
                }

                using (new ProfilingSample(cmd, "Combine Area Shadow", CustomSamplerId.Raytracing.GetSampler()))
                {
                    // Inject all the parameters for the compute
                    cmd.SetComputeTextureParam(bilateralFilter, m_KernelFilter, _SNBuffer, m_SNBuffer);
                    cmd.SetComputeTextureParam(bilateralFilter, m_KernelFilter, _UNBuffer, m_UNBuffer);
                    cmd.SetComputeTextureParam(bilateralFilter, m_KernelFilter, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
                    cmd.SetComputeTextureParam(bilateralFilter, m_KernelFilter, HDShaderIDs._NormalBufferTexture, m_SharedRTManager.GetNormalBuffer());
                    cmd.SetComputeIntParam(bilateralFilter, _DenoiseRadius, rtEnvironement.shadowFilterRadius);
                    cmd.SetComputeFloatParam(bilateralFilter, _GaussianSigma, rtEnvironement.shadowFilterSigma);
                    cmd.SetComputeIntParam(bilateralFilter, _ShadowSlot, m_LightLoop.m_lightList.lights[lightIdx].shadowIndex);

                    // Set the output slot
                    cmd.SetComputeTextureParam(bilateralFilter, m_KernelFilter, _DSNDUNUAV, m_AreaShadowTextureArray);

                    // Texture dimensions
                    int texWidth = m_AreaShadowTextureArray.rt.width;
                    int texHeight = m_AreaShadowTextureArray.rt.width;

                    // Evaluate the dispatch parameters
                    int areaTileSize = 8;
                    int numTilesX = (texWidth + (areaTileSize - 1)) / areaTileSize;
                    int numTilesY = (texHeight + (areaTileSize - 1)) / areaTileSize;

                    // Compute the texture
                    cmd.DispatchCompute(bilateralFilter, m_KernelFilter, numTilesX, numTilesY, 1);
                }
            }
            return true;
        }
    }
#endif
}
