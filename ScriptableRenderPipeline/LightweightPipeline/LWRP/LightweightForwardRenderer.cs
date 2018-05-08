using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.XR;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public enum RenderPassHandles
    {
        DepthPrepass,
        DirectionalShadows,
        LocalShadows,
        ScreenSpaceShadowResolve,
        ForwardLit,
        Count,
    }

    public enum MaterialHandles
    {
        Error,
        DepthCopy,
        Sampling,
        Blit,
        ScrenSpaceShadow,
        Count,
    }

    public static class RenderTargetHandles
    {
        public static int Color;
        public static int DepthAttachment;
        public static int DepthTexture;
        public static int OpaqueColor;
        public static int DirectionalShadowmap;
        public static int LocalShadowmap;
        public static int ScreenSpaceOcclusion;
    }

    public class LightweightForwardRenderer
    {
        // Lights are culled per-object. In platforms that don't use StructuredBuffer
        // the engine will set 4 light indices in the following constant unity_4LightIndices0
        // Additionally the engine set unity_4LightIndices1 but LWRP doesn't use that.
        const int k_MaxConstantLocalLights = 4;

        // LWRP uses a fixed constant buffer to hold light data. This must match the value of
        // MAX_VISIBLE_LIGHTS 16 in Input.hlsl
        const int k_MaxVisibleLocalLights = 16;

        const int k_MaxVertexLights = 4;
        public int maxSupportedLocalLightsPerPass
        {
            get
            {
                return useComputeBufferForPerObjectLightIndices ? k_MaxVisibleLocalLights : k_MaxConstantLocalLights;
            }
        }

        // TODO: Profile performance of using ComputeBuffer on mobiles that support it vs
        public bool useComputeBufferForPerObjectLightIndices
        {
            get { return !Application.isMobilePlatform && Application.platform != RuntimePlatform.WebGLPlayer; }
        }

        public int maxVisibleLocalLights { get { return k_MaxVisibleLocalLights; } }

        public int maxSupportedVertexLights { get { return k_MaxVertexLights; } }

        public PostProcessRenderContext postProcessRenderContext { get; private set; }

        public ComputeBuffer perObjectLightIndices { get; private set; }

        public FilterRenderersSettings opaqueFilterSettings { get; private set; }
        public FilterRenderersSettings transparentFilterSettings { get; private set; }

        Dictionary<int, Material> m_Materials = new Dictionary<int, Material>();
        Dictionary<int, RenderTargetIdentifier> m_ResourceMap = new Dictionary<int, RenderTargetIdentifier>();
        Dictionary<RenderPassHandles, ScriptableRenderPass> m_RenderPassSet = new Dictionary<RenderPassHandles, ScriptableRenderPass>();

        List<ScriptableRenderPass> m_ActiveShadowQueue = new List<ScriptableRenderPass>();
        List<ScriptableRenderPass> m_ActiveRenderPassQueue = new List<ScriptableRenderPass>();

        public LightweightForwardRenderer(LightweightPipelineAsset pipelineAsset)
        {
            // RenderTexture format depends on camera and pipeline (HDR, non HDR, etc)
            // Samples (MSAA) depend on camera and pipeline
            RegisterSurface("_CameraColorTexture", out RenderTargetHandles.Color);
            RegisterSurface("_CameraDepthAttachment", out RenderTargetHandles.DepthAttachment);
            RegisterSurface("_CameraDepthTexture", out RenderTargetHandles.DepthTexture);
            RegisterSurface("_CameraOpaqueTexture", out RenderTargetHandles.OpaqueColor);
            RegisterSurface("_DirectionalShadowmapTexture", out RenderTargetHandles.DirectionalShadowmap);
            RegisterSurface("_LocalShadowmapTexture", out RenderTargetHandles.LocalShadowmap);
            RegisterSurface("_ScreenSpaceShadowMapTexture", out RenderTargetHandles.ScreenSpaceOcclusion);

            RegisterMaterial(MaterialHandles.Error, CoreUtils.CreateEngineMaterial("Hidden/InternalErrorShader"));
            RegisterMaterial(MaterialHandles.DepthCopy, CoreUtils.CreateEngineMaterial(pipelineAsset.CopyDepthShader));
            RegisterMaterial(MaterialHandles.Sampling, CoreUtils.CreateEngineMaterial(pipelineAsset.SamplingShader));
            RegisterMaterial(MaterialHandles.Blit, CoreUtils.CreateEngineMaterial(pipelineAsset.BlitShader));
            RegisterMaterial(MaterialHandles.ScrenSpaceShadow, CoreUtils.CreateEngineMaterial(pipelineAsset.ScreenSpaceShadowShader));
            Debug.Assert(m_Materials.Count == (int)MaterialHandles.Count, "All materials in MaterialHandles should be registered in the renderer.");

            RegisterPass(RenderPassHandles.DepthPrepass, new DepthOnlyPass(this));
            RegisterPass(RenderPassHandles.DirectionalShadows, new DirectionalShadowsPass(this, pipelineAsset.DirectionalShadowAtlasResolution));
            RegisterPass(RenderPassHandles.LocalShadows, new LocalShadowsPass(this, pipelineAsset.LocalShadowAtlasResolution));
            RegisterPass(RenderPassHandles.ScreenSpaceShadowResolve, new ScreenSpaceShadowResolvePass(this));
            RegisterPass(RenderPassHandles.ForwardLit, new ForwardLitPass(this));
            Debug.Assert(m_RenderPassSet.Count == (int)RenderPassHandles.Count, "All render passes in Passes should be registered in the renderer");

            postProcessRenderContext = new PostProcessRenderContext();

            opaqueFilterSettings = new FilterRenderersSettings(true)
            {
                renderQueueRange = RenderQueueRange.opaque,
            };

            transparentFilterSettings = new FilterRenderersSettings(true)
            {
                renderQueueRange = RenderQueueRange.transparent,
            };
        }

        public void Dispose()
        {
            if (perObjectLightIndices != null)
            {
                perObjectLightIndices.Release();
                perObjectLightIndices = null;
            }

            for (int i = 0; i < m_Materials.Count; ++i)
                CoreUtils.Destroy(m_Materials[i]);
            m_Materials.Clear();
        }

        public Material GetMaterial(MaterialHandles resourceHandle)
        {
            int resourceHandleID = (int)resourceHandle;
            if (resourceHandleID < 0 || resourceHandleID >= m_Materials.Count)
                return null;

            return m_Materials[resourceHandleID];
        }

        public RenderTextureDescriptor CreateRTDesc(ref CameraData cameraData, float scaler = 1.0f)
        {
            Camera camera = cameraData.camera;
            RenderTextureDescriptor desc;
            if (cameraData.isStereoEnabled)
                desc = XRSettings.eyeTextureDesc;
            else
                desc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);

            float renderScale = cameraData.renderScale;
            desc.width = (int)((float)desc.width * renderScale * scaler);
            desc.height = (int)((float)desc.height * renderScale * scaler);
            return desc;
        }

        public void Setup(ref ScriptableRenderContext context, ref CullResults cullResults, ref CameraData cameraData, ref LightData lightData)
        {
            Clear();
            SetupPerObjectLightIndices(ref cullResults, ref lightData);
            RenderTextureDescriptor baseDescriptor = CreateRTDesc(ref cameraData);

            bool requiresCameraDepth = cameraData.requiresDepthTexture;

            ShadowData shadowData = lightData.shadowData;
            bool requiresDepthPrepass = shadowData.requiresScreenSpaceShadowResolve || cameraData.isSceneViewCamera || (requiresCameraDepth && !CanCopyDepth(ref cameraData));

            CommandBuffer cmd = CommandBufferPool.Get("Setup Rendering");
            if (requiresDepthPrepass)
                EnqueuePass(cmd, RenderPassHandles.DepthPrepass, baseDescriptor, null, RenderTargetHandles.DepthTexture);

            if (shadowData.renderDirectionalShadows)
            {
                EnqueuePass(cmd, RenderPassHandles.DirectionalShadows, baseDescriptor);
                if (shadowData.requiresScreenSpaceShadowResolve)
                    EnqueuePass(cmd, RenderPassHandles.ScreenSpaceShadowResolve, baseDescriptor, new[] {RenderTargetHandles.ScreenSpaceOcclusion});
            }

            if (shadowData.renderLocalShadows)
                EnqueuePass(cmd, RenderPassHandles.LocalShadows, baseDescriptor);

            bool requiresDepthAttachment = requiresCameraDepth && !requiresDepthPrepass;
            bool requiresColorAttachment = RequiresColorAttachment(ref cameraData, baseDescriptor) || requiresDepthAttachment;
            int[] colorHandles = (requiresColorAttachment) ? new[] {RenderTargetHandles.Color} : null;
            int depthHandle = (requiresColorAttachment) ? RenderTargetHandles.DepthAttachment : -1;
            EnqueuePass(cmd, RenderPassHandles.ForwardLit, baseDescriptor, colorHandles, depthHandle, cameraData.msaaSamples);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Execute(ref ScriptableRenderContext context, ref CullResults cullResults, ref CameraData cameraData,
            ref LightData lightData)
        {
            // TODO: The reason we have to separate passes into two queues is because shadows require different camera
            // context. We need to take a look at approaches to effectively share shadow between cameras, then we
            // can move this out
            for (int i = 0; i < m_ActiveShadowQueue.Count; ++i)
                m_ActiveShadowQueue[i].Execute(ref context, ref cullResults, ref cameraData, ref lightData);

            // SetupCameraProperties does the following:
            // Setup Camera RenderTarget and Viewport
            // VR Camera Setup and SINGLE_PASS_STEREO props
            // Setup camera view, proj and their inv matrices.
            // Setup properties: _WorldSpaceCameraPos, _ProjectionParams, _ScreenParams, _ZBufferParams, unity_OrthoParams
            // Setup camera world clip planes props
            // setup HDR keyword
            // Setup global time properties (_Time, _SinTime, _CosTime)
            context.SetupCameraProperties(cameraData.camera, cameraData.isStereoEnabled);

            for (int i = 0; i < m_ActiveRenderPassQueue.Count; ++i)
                m_ActiveRenderPassQueue[i].Execute(ref context, ref cullResults, ref cameraData, ref lightData);

#if UNITY_EDITOR
            if (cameraData.isSceneViewCamera)
                CopyDepth(ref context);
#endif

            DisposePasses(ref context);
        }

        void Clear()
        {
            m_ActiveShadowQueue.Clear();
            m_ActiveRenderPassQueue.Clear();
        }

        void RegisterSurface(string shaderProperty, out int handle)
        {
            handle = Shader.PropertyToID(shaderProperty);
            m_ResourceMap.Add(handle, new RenderTargetIdentifier(handle));
        }

        void RegisterMaterial(MaterialHandles handle, Material material)
        {
            m_Materials.Add((int)handle, material);
        }

        void RegisterPass(RenderPassHandles passHandle, ScriptableRenderPass pass)
        {
            m_RenderPassSet.Add(passHandle, pass);
        }

        void EnqueuePass(CommandBuffer cmd, RenderPassHandles passHandle, RenderTextureDescriptor baseDescriptor,
            int[] colorAttachmentHandles = null, int depthAttachmentHandle = -1, int samples = 1)
        {
            Debug.Assert((int)passHandle < m_RenderPassSet.Count, "Trying to add an invalid pass to renderer's frame");
            ScriptableRenderPass pass = m_RenderPassSet[passHandle];
            pass.Setup(cmd, baseDescriptor, colorAttachmentHandles, depthAttachmentHandle, samples);

            if (passHandle == RenderPassHandles.DirectionalShadows || passHandle == RenderPassHandles.LocalShadows)
                m_ActiveShadowQueue.Add(pass);
            else
                m_ActiveRenderPassQueue.Add(pass);
        }

        bool RequiresColorAttachment(ref CameraData cameraData, RenderTextureDescriptor baseDescriptor)
        {
            bool isScaledRender = !Mathf.Approximately(cameraData.renderScale, 1.0f);
            bool isTargetTexture2DArray = baseDescriptor.dimension == TextureDimension.Tex2DArray;
            return cameraData.isSceneViewCamera || isScaledRender || cameraData.isHdrEnabled ||
                cameraData.postProcessEnabled || cameraData.requiresOpaqueTexture || isTargetTexture2DArray;
        }

        bool CanCopyDepth(ref CameraData cameraData)
        {
            bool msaaEnabledForCamera = cameraData.msaaSamples > 1;
            bool supportsTextureCopy = SystemInfo.copyTextureSupport != CopyTextureSupport.None;
            bool supportsDepthTarget = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth);
            bool supportsDepthCopy = !msaaEnabledForCamera && (supportsDepthTarget || supportsTextureCopy);
            bool msaaDepthResolve = msaaEnabledForCamera && SystemInfo.supportsMultisampledTextures != 0;
            return supportsDepthCopy || msaaDepthResolve;
        }

        void CopyDepth(ref ScriptableRenderContext context)
        {
            // Restore Render target for additional editor rendering.
            // Note: Scene view camera always perform depth prepass
            CommandBuffer cmd = CommandBufferPool.Get("Copy Depth to Camera");
            CoreUtils.SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget);
            cmd.Blit(GetSurface(RenderTargetHandles.DepthTexture), BuiltinRenderTextureType.CameraTarget, GetMaterial(MaterialHandles.DepthCopy));
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void DisposePasses(ref ScriptableRenderContext context)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Release Resources");
            for (int i = 0; i < m_ActiveShadowQueue.Count; ++i)
                m_ActiveShadowQueue[i].Dispose(cmd);

            for (int i = 0; i < m_ActiveRenderPassQueue.Count; ++i)
                m_ActiveRenderPassQueue[i].Dispose(cmd);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void SetupPerObjectLightIndices(ref CullResults cullResults, ref LightData lightData)
        {
            if (lightData.totalAdditionalLightsCount > 0)
            {
                List<VisibleLight> visibleLights = lightData.visibleLights;
                int[] perObjectLightIndexMap = cullResults.GetLightIndexMap();
                int directionalLightCount = 0;

                // Disable all directional lights from the perobject light indices
                // Pipeline handles them globally
                for (int i = 0; i < visibleLights.Count; ++i)
                {
                    VisibleLight light = visibleLights[i];
                    if (light.lightType == LightType.Directional)
                    {
                        perObjectLightIndexMap[i] = -1;
                        ++directionalLightCount;
                    }
                    else
                        perObjectLightIndexMap[i] -= directionalLightCount;
                }
                cullResults.SetLightIndexMap(perObjectLightIndexMap);

                // if not using a compute buffer, engine will set indices in 2 vec4 constants
                // unity_4LightIndices0 and unity_4LightIndices1
                if (useComputeBufferForPerObjectLightIndices)
                {
                    int lightIndicesCount = cullResults.GetLightIndicesCount();
                    if (lightIndicesCount > 0)
                    {
                        if (perObjectLightIndices == null)
                        {
                            perObjectLightIndices = new ComputeBuffer(lightIndicesCount, sizeof(int));
                        }
                        else if (perObjectLightIndices.count < lightIndicesCount)
                        {
                            perObjectLightIndices.Release();
                            perObjectLightIndices = new ComputeBuffer(lightIndicesCount, sizeof(int));
                        }

                        cullResults.FillLightIndices(perObjectLightIndices);
                    }
                }
            }
        }

        public RenderTargetIdentifier GetSurface(int handle)
        {
            if (handle < 0)
            {
                Debug.LogError(string.Format("Handle {0} has not any surface registered to it.", handle));
                return new RenderTargetIdentifier();
            }

            return m_ResourceMap[handle];
        }
    }
}
