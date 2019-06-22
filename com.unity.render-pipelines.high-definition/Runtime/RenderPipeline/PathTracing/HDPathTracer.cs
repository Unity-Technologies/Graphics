using System;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [Serializable, VolumeComponentMenu("Path Tracing")]
    public sealed class PathTracing : VolumeComponent
    {
        [Tooltip("Enable. Enables path tracing (thus disabling most other passes).")]
        public BoolParameter enable = new BoolParameter(false);

        [Tooltip("Max Samples. Defines the maximum number of paths cast within each pixel.")]
        public ClampedIntParameter maxSamples = new ClampedIntParameter(256, 1, 512);

        [Tooltip("Max Depth. Defines the maximum recursion for each path.")]
        public ClampedIntParameter maxDepth = new ClampedIntParameter(4, 1, 10);

        [Tooltip("Max Intensity. Defines the maximum intensity value computed for a path.")]
        public ClampedFloatParameter maxIntensity = new ClampedFloatParameter(10f, 0f, 100f);

        [Tooltip("Ray Length. Defines the maximum travel distance of rays.")]
        public ClampedFloatParameter rayLength = new ClampedFloatParameter(10f, 0f, 100f);
    }

#if ENABLE_RAYTRACING
    public class HDPathTracer
    {
        // External structures
        HDRenderPipelineAsset m_PipelineAsset = null;
        RenderPipelineResources m_PipelineResources = null;
        SkyManager m_SkyManager = null;
        HDRaytracingManager m_RaytracingManager = null;
        SharedRTManager m_SharedRTManager = null;

        // Intermediate buffer that stores the reflection pre-denoising
        RTHandleSystem.RTHandle m_RaytracingFlagTarget = null;
        RTHandleSystem.RTHandle m_DebugRaytracingTexture = null;

        // The kernel that allows us to override the color buffer
        Material m_RaytracingFlagMaterial = null;

        // String values
        const string m_RayGenShaderName = "RayGen";
        const string m_MissShaderName = "Miss";
        const string m_ClosestHitShaderName = "ClosestHit";

        RenderStateBlock m_RaytracingFlagStateBlock;

        public HDPathTracer()
        {
        }

        public void Init(HDRenderPipelineAsset asset, SkyManager skyManager, HDRaytracingManager raytracingManager, SharedRTManager sharedRTManager)
        {
            // Keep track of the pipeline asset
            m_PipelineAsset = asset;
            m_PipelineResources = asset.renderPipelineResources;

            // Keep track of the sky manager
            m_SkyManager = skyManager;

            // keep track of the ray tracing manager
            m_RaytracingManager = raytracingManager;

            // Keep track of the shared rt manager
            m_SharedRTManager = sharedRTManager;

            m_RaytracingFlagTarget = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R8_SNorm, enableRandomWrite: true, useMipMap: false, name: "RaytracingFlagTexture");
            m_DebugRaytracingTexture = RTHandles.Alloc(Vector2.one, filterMode: FilterMode.Point, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite: true, useDynamicScale: true, useMipMap: false, name: "DebugRaytracingBuffer");

            m_RaytracingFlagStateBlock = new RenderStateBlock
            {
                depthState = new DepthState(false, CompareFunction.LessEqual),
                mask = RenderStateMask.Depth
            };
        }

        static RTHandleSystem.RTHandle HistoryBufferAllocatorFunction(string viewName, int frameIndex, RTHandleSystem rtHandleSystem)
        {
            return rtHandleSystem.Alloc(Vector2.one, TextureXR.slices, colorFormat: GraphicsFormat.R16G16B16A16_SFloat, dimension: TextureXR.dimension,
                                        enableRandomWrite: true, useMipMap: false, autoGenerateMips: false,
                                        name: string.Format("PathTracingHistoryBuffer{0}", frameIndex));
        }

        public void Release()
        {
            RTHandles.Release(m_DebugRaytracingTexture);
            RTHandles.Release(m_RaytracingFlagTarget);

            if (m_RaytracingFlagMaterial != null)
            {
                CoreUtils.Destroy(m_RaytracingFlagMaterial);
            }
        }

        public void Render(HDCamera hdCamera, CommandBuffer cmd, RTHandleSystem.RTHandle outputTexture, ScriptableRenderContext renderContext, int frameCount)
        {
            // First thing to check is: Do we have a valid ray-tracing environment?
            HDRaytracingEnvironment rtEnvironment = m_RaytracingManager.CurrentEnvironment();
            HDRenderPipeline renderPipeline = m_RaytracingManager.GetRenderPipeline();
            RaytracingShader pathTracingShader = m_PipelineAsset.renderPipelineRayTracingResources.pathTracing;

            PathTracing pathTracingSettings = VolumeManager.instance.stack.GetComponent<PathTracing>();
            LightCluster lightClusterSettings = VolumeManager.instance.stack.GetComponent<LightCluster>();

            // Check the validity of the state before computing the effect
            bool invalidState = rtEnvironment == null || !pathTracingSettings.enable.value || pathTracingShader == null
                || m_PipelineResources.textures.owenScrambledTex == null || m_PipelineResources.textures.scramblingTex == null;

            // If any resource or game-object is missing We stop right away
            if (invalidState)
                return;

            // Grab the history buffer (hijack the reflections one)
            RTHandleSystem.RTHandle history = hdCamera.GetCurrentFrameRT((int)HDCameraFrameHistoryType.RaytracedReflection)
                ?? hdCamera.AllocHistoryFrameRT((int)HDCameraFrameHistoryType.RaytracedReflection, HistoryBufferAllocatorFunction, 1);

            // Grab the acceleration structure and the list of HD lights for the target camera
            RaytracingAccelerationStructure accelerationStructure = m_RaytracingManager.RequestAccelerationStructure(rtEnvironment.raytracedLayerMask);
            HDRaytracingLightCluster lightCluster = m_RaytracingManager.RequestLightCluster(rtEnvironment.raytracedLayerMask);

            // Define the shader pass to use for the reflection pass
            cmd.SetRaytracingShaderPass(pathTracingShader, "PathTracingDXR");

            // Set the acceleration structure for the pass
            cmd.SetRaytracingAccelerationStructure(pathTracingShader, HDShaderIDs._RaytracingAccelerationStructureName, accelerationStructure);

            // Inject the ray-tracing sampling data
            cmd.SetGlobalTexture(HDShaderIDs._OwenScrambledTexture, m_PipelineResources.textures.owenScrambledTex);
            cmd.SetGlobalTexture(HDShaderIDs._ScramblingTexture, m_PipelineResources.textures.scramblingTex);

            // Inject the ray generation data
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingRayBias, rtEnvironment.rayBias);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingNumSamples, pathTracingSettings.maxSamples.value);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingMaxRecursion, pathTracingSettings.maxDepth.value);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingIntensityClamp, pathTracingSettings.maxIntensity.value);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingRayMaxLength, pathTracingSettings.rayLength.value);
            cmd.SetGlobalFloat(HDShaderIDs._RaytracingCameraNearPlane, hdCamera.camera.nearClipPlane);

            // Set the data for the ray generation
            //cmd.SetRaytracingTextureParam(pathTracingShader, HDShaderIDs._DepthTexture, m_SharedRTManager.GetDepthStencilBuffer());
            cmd.SetRaytracingTextureParam(pathTracingShader, HDShaderIDs._CameraColorTextureRW, outputTexture);
            cmd.SetGlobalInt(HDShaderIDs._RaytracingFrameIndex, frameCount);

            // Compute an approximate pixel spread angle value (in radians)
            float pixelSpreadAngle = hdCamera.camera.fieldOfView * (Mathf.PI / 180.0f) / Mathf.Min(hdCamera.actualWidth, hdCamera.actualHeight);
            cmd.SetRaytracingFloatParam(pathTracingShader, HDShaderIDs._RaytracingPixelSpreadAngle, pixelSpreadAngle);

            // LightLoop data
            cmd.SetGlobalBuffer(HDShaderIDs._RaytracingLightCluster, lightCluster.GetCluster());
            cmd.SetGlobalBuffer(HDShaderIDs._LightDatasRT, lightCluster.GetLightDatas());
            cmd.SetGlobalVector(HDShaderIDs._MinClusterPos, lightCluster.GetMinClusterPos());
            cmd.SetGlobalVector(HDShaderIDs._MaxClusterPos, lightCluster.GetMaxClusterPos());
            cmd.SetGlobalInt(HDShaderIDs._LightPerCellCount, lightClusterSettings.maxNumLightsPercell.value);
            cmd.SetGlobalInt(HDShaderIDs._PunctualLightCountRT, lightCluster.GetPunctualLightCount());
            cmd.SetGlobalInt(HDShaderIDs._AreaLightCountRT, lightCluster.GetAreaLightCount());

            // Note: Just in case, we rebind the directional light data (in case they were not)
            //cmd.SetGlobalBuffer(HDShaderIDs._DirectionalLightDatas, renderPipeline.directionalLightDatas);
            //cmd.SetGlobalInt(HDShaderIDs._DirectionalLightCount, renderPipeline.m_lightList.directionalLights.Count);

            // Set the data for the ray miss
            cmd.SetRaytracingTextureParam(pathTracingShader, HDShaderIDs._SkyTexture, m_SkyManager.skyReflection);

            // Additional data for path tracing
            cmd.SetRaytracingTextureParam(pathTracingShader, HDShaderIDs._AccumulatedFrameTexture, history);
            cmd.SetRaytracingMatrixParam(pathTracingShader, HDShaderIDs._PixelCoordToViewDirWS, hdCamera.mainViewConstants.pixelCoordToViewDirWS);

            // Run the computation
            cmd.DispatchRays(pathTracingShader, m_RayGenShaderName, (uint)hdCamera.actualWidth, (uint)hdCamera.actualHeight, 1);
        }
    }
#endif
}
