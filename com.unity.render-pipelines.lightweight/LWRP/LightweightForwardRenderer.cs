using System;
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
        public static int ScreenSpaceShadowmap;
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

        // TODO: Profile performance of using ComputeBuffer on mobiles that support it
        public bool useComputeBufferForPerObjectLightIndices
        {
            get
            {
                return SystemInfo.supportsComputeShaders &&
                    !Application.isMobilePlatform && Application.platform != RuntimePlatform.WebGLPlayer;
            }
        }

        public int maxVisibleLocalLights { get { return k_MaxVisibleLocalLights; } }

        public int maxSupportedVertexLights { get { return k_MaxVertexLights; } }

        public PostProcessRenderContext postProcessRenderContext { get; private set; }

        public ComputeBuffer perObjectLightIndices { get; private set; }

        public FilterRenderersSettings opaqueFilterSettings { get; private set; }
        public FilterRenderersSettings transparentFilterSettings { get; private set; }

        Dictionary<int, RenderTargetIdentifier> m_ResourceMap = new Dictionary<int, RenderTargetIdentifier>();
        List<ScriptableRenderPass> m_ActiveShadowQueue = new List<ScriptableRenderPass>();
        List<ScriptableRenderPass> m_ActiveRenderPassQueue = new List<ScriptableRenderPass>();

        Material[] m_Materials;
        ScriptableRenderPass[] m_RenderPassSet = new ScriptableRenderPass[(int)RenderPassHandles.Count];

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
            RegisterSurface("_ScreenSpaceShadowMapTexture", out RenderTargetHandles.ScreenSpaceShadowmap);

            m_Materials = new Material[(int)MaterialHandles.Count]
            {
                CoreUtils.CreateEngineMaterial("Hidden/InternalErrorShader"),
                CoreUtils.CreateEngineMaterial(pipelineAsset.copyDepthShader),
                CoreUtils.CreateEngineMaterial(pipelineAsset.samplingShader),
                CoreUtils.CreateEngineMaterial(pipelineAsset.blitShader),
                CoreUtils.CreateEngineMaterial(pipelineAsset.screenSpaceShadowShader),
            };

            m_RenderPassSet = new ScriptableRenderPass[(int)RenderPassHandles.Count]
            {
                new DepthOnlyPass(this),
                new DirectionalShadowsPass(this),
                new LocalShadowsPass(this),
                new ScreenSpaceShadowResolvePass(this),
                new ForwardLitPass(this),
            };

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

            for (int i = 0; i < m_Materials.Length; ++i)
                CoreUtils.Destroy(m_Materials[i]);
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
            desc.colorFormat = cameraData.isHdrEnabled ? RenderTextureFormat.DefaultHDR :
                RenderTextureFormat.Default;
            desc.enableRandomWrite = false;
            desc.width = (int)((float)desc.width * renderScale * scaler);
            desc.height = (int)((float)desc.height * renderScale * scaler);
            return desc;
        }

        public void Setup(ref ScriptableRenderContext context, ref CullResults cullResults, ref RenderingData renderingData)
        {
            Clear();

            SetupPerObjectLightIndices(ref cullResults, ref renderingData.lightData);
            RenderTextureDescriptor baseDescriptor = CreateRTDesc(ref renderingData.cameraData);
            RenderTextureDescriptor shadowDescriptor = baseDescriptor;
            shadowDescriptor.dimension = TextureDimension.Tex2D;

            bool requiresCameraDepth = renderingData.cameraData.requiresDepthTexture;
            bool requiresDepthPrepass = renderingData.shadowData.requiresScreenSpaceShadowResolve ||
                renderingData.cameraData.isSceneViewCamera || (requiresCameraDepth && !CanCopyDepth(ref renderingData.cameraData));

            // For now VR requires a depth prepass until we figure out how to properly resolve texture2DMS in stereo
            requiresDepthPrepass |= renderingData.cameraData.isStereoEnabled;

            CommandBuffer cmd = CommandBufferPool.Get("Setup Rendering");
            if (requiresDepthPrepass)
                EnqueuePass(cmd, RenderPassHandles.DepthPrepass, baseDescriptor, null, RenderTargetHandles.DepthTexture);

            if (renderingData.shadowData.renderDirectionalShadows)
            {
                EnqueuePass(cmd, RenderPassHandles.DirectionalShadows, shadowDescriptor);
                if (renderingData.shadowData.requiresScreenSpaceShadowResolve)
                    EnqueuePass(cmd, RenderPassHandles.ScreenSpaceShadowResolve, baseDescriptor, new[] {RenderTargetHandles.ScreenSpaceShadowmap});
            }

            if (renderingData.shadowData.renderLocalShadows)
                EnqueuePass(cmd, RenderPassHandles.LocalShadows, shadowDescriptor);

            bool requiresDepthAttachment = requiresCameraDepth && !requiresDepthPrepass;
            bool requiresColorAttachment = RequiresIntermediateColorTexture(ref renderingData.cameraData, baseDescriptor, requiresDepthAttachment);
            int[] colorHandles = (requiresColorAttachment) ? new[] {RenderTargetHandles.Color} : null;
            int depthHandle = (requiresDepthAttachment) ? RenderTargetHandles.DepthAttachment : -1;
            EnqueuePass(cmd, RenderPassHandles.ForwardLit, baseDescriptor, colorHandles, depthHandle, renderingData.cameraData.msaaSamples);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Execute(ref ScriptableRenderContext context, ref CullResults cullResults, ref RenderingData renderingData)
        {
            // TODO: The reason we have to separate passes into two queues is because shadows require different camera
            // context. We need to take a look at approaches to effectively share shadow between cameras, then we
            // can move this out
            for (int i = 0; i < m_ActiveShadowQueue.Count; ++i)
                m_ActiveShadowQueue[i].Execute(ref context, ref cullResults, ref renderingData);

            // SetupCameraProperties does the following:
            // Setup Camera RenderTarget and Viewport
            // VR Camera Setup and SINGLE_PASS_STEREO props
            // Setup camera view, proj and their inv matrices.
            // Setup properties: _WorldSpaceCameraPos, _ProjectionParams, _ScreenParams, _ZBufferParams, unity_OrthoParams
            // Setup camera world clip planes props
            // setup HDR keyword
            // Setup global time properties (_Time, _SinTime, _CosTime)
            context.SetupCameraProperties(renderingData.cameraData.camera, renderingData.cameraData.isStereoEnabled);

            for (int i = 0; i < m_ActiveRenderPassQueue.Count; ++i)
                m_ActiveRenderPassQueue[i].Execute(ref context, ref cullResults, ref renderingData);

#if UNITY_EDITOR
            if (renderingData.cameraData.isSceneViewCamera)
            {
                // Restore Render target for additional editor rendering.
                // Note: Scene view camera always perform depth prepass
                CommandBuffer cmd = CommandBufferPool.Get("Copy Depth to Camera");
                CoreUtils.SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget);
                cmd.EnableShaderKeyword(LightweightKeywordStrings.DepthNoMsaa);
                cmd.DisableShaderKeyword(LightweightKeywordStrings.DepthMsaa2);
                cmd.DisableShaderKeyword(LightweightKeywordStrings.DepthMsaa4);
                cmd.Blit(GetSurface(RenderTargetHandles.DepthTexture), BuiltinRenderTextureType.CameraTarget, GetMaterial(MaterialHandles.DepthCopy));
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
#endif

            DisposePasses(ref context);
        }

        public RenderTargetIdentifier GetSurface(int handle)
        {
            if (handle == -1)
                return BuiltinRenderTextureType.CameraTarget;

            RenderTargetIdentifier renderTargetID;
            if (!m_ResourceMap.TryGetValue(handle, out renderTargetID))
            {
                Debug.LogError(string.Format("Handle {0} has not any surface registered to it.", handle));
                return new RenderTargetIdentifier();
            }

            return renderTargetID;
        }

        public Material GetMaterial(MaterialHandles handle)
        {
            int handleID = (int)handle;
            if (handleID >= m_Materials.Length)
            {
                Debug.LogError(string.Format("Material {0} is not registered.",
                        Enum.GetName(typeof(MaterialHandles), handleID)));
                return null;
            }

            return m_Materials[handleID];
        }

        ScriptableRenderPass GetPass(RenderPassHandles handle)
        {
            int handleID = (int)handle;
            if (handleID >= m_RenderPassSet.Length)
            {
                Debug.LogError(string.Format("Render Pass {0} is not registered.",
                        Enum.GetName(typeof(RenderPassHandles), handleID)));
                return null;
            }

            return m_RenderPassSet[handleID];
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

        void EnqueuePass(CommandBuffer cmd, RenderPassHandles passHandle, RenderTextureDescriptor baseDescriptor,
            int[] colorAttachmentHandles = null, int depthAttachmentHandle = -1, int samples = 1)
        {
            ScriptableRenderPass pass = GetPass(passHandle);
            pass.Setup(cmd, baseDescriptor, colorAttachmentHandles, depthAttachmentHandle, samples);

            if (passHandle == RenderPassHandles.DirectionalShadows || passHandle == RenderPassHandles.LocalShadows)
                m_ActiveShadowQueue.Add(pass);
            else
                m_ActiveRenderPassQueue.Add(pass);
        }

        bool RequiresIntermediateColorTexture(ref CameraData cameraData, RenderTextureDescriptor baseDescriptor, bool requiresCameraDepth)
        {
            if (cameraData.isOffscreenRender)
                return false;

            bool isScaledRender = !Mathf.Approximately(cameraData.renderScale, 1.0f);
            bool isTargetTexture2DArray = baseDescriptor.dimension == TextureDimension.Tex2DArray;
            return requiresCameraDepth || cameraData.isSceneViewCamera || isScaledRender || cameraData.isHdrEnabled ||
                cameraData.postProcessEnabled || cameraData.requiresOpaqueTexture || isTargetTexture2DArray || !cameraData.isDefaultViewport;
        }

        bool CanCopyDepth(ref CameraData cameraData)
        {
            bool msaaEnabledForCamera = cameraData.msaaSamples > 1;
            bool supportsTextureCopy = SystemInfo.copyTextureSupport != CopyTextureSupport.None;
            bool supportsDepthTarget = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.Depth);
            bool supportsDepthCopy = !msaaEnabledForCamera && (supportsDepthTarget || supportsTextureCopy);

            // TODO:  We don't have support to highp Texture2DMS currently and this breaks depth precision.
            // currently disabling it until shader changes kick in.
            //bool msaaDepthResolve = msaaEnabledForCamera && SystemInfo.supportsMultisampledTextures != 0;
            bool msaaDepthResolve = false;
            return supportsDepthCopy || msaaDepthResolve;
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
            if (lightData.totalAdditionalLightsCount == 0)
                return;

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
}
