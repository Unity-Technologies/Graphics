using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.XR;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
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
        public static int Depth;
        public static int DepthCopy;
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

        //RenderGraphNode m_RenderGraph;
        List<ScriptableRenderPass> m_ShadowPassList = new List<ScriptableRenderPass>();
        List<ScriptableRenderPass> m_RenderPassList = new List<ScriptableRenderPass>();
        Dictionary<int, RenderTargetIdentifier> m_ResourceMap = new Dictionary<int, RenderTargetIdentifier>();

        DepthOnlyPass depthOnlyPass;
        DirectionalShadowsPass directionalShadowPass;
        LocalShadowsPass localShadowsPass;
        ScreenSpaceShadowOcclusionPass screenSpaceShadowOcclusionPass;
        ForwardLitPass forwardLitPass;

        Dictionary<int, Material> m_Materials;
        List<RenderPassAttachment> m_AttachmentList;

        RenderPass m_RenderPass;

        public LightweightForwardRenderer(LightweightPipelineAsset pipelineAsset)
        {
            // RenderTexture format depends on camera and pipeline (HDR, non HDR, etc)
            // Samples (MSAA) depend on camera and pipeline
            AddSurface("_CameraColorTexture", out RenderTargetHandles.Color);
            AddSurface("_CameraDepthTexture", out RenderTargetHandles.Depth);
            AddSurface("_CameraDepthTextureCopy", out RenderTargetHandles.DepthCopy);
            AddSurface("_CameraOpaqueTexture", out RenderTargetHandles.OpaqueColor);
            AddSurface("_DirectionalShadowmapTexture", out RenderTargetHandles.DirectionalShadowmap);
            AddSurface("_LocalShadowmapTexture", out RenderTargetHandles.LocalShadowmap);
            AddSurface("_ScreenSpaceShadowMapTexture", out RenderTargetHandles.ScreenSpaceOcclusion);

            m_Materials = new Dictionary<int, Material>((int)MaterialHandles.Count);
            m_Materials.Add((int)MaterialHandles.Error, CoreUtils.CreateEngineMaterial("Hidden/InternalErrorShader"));
            m_Materials.Add((int)MaterialHandles.DepthCopy, CoreUtils.CreateEngineMaterial(pipelineAsset.CopyDepthShader));
            m_Materials.Add((int)MaterialHandles.Sampling, CoreUtils.CreateEngineMaterial(pipelineAsset.SamplingShader));
            m_Materials.Add((int)MaterialHandles.Blit, CoreUtils.CreateEngineMaterial(pipelineAsset.BlitShader));
            m_Materials.Add((int)MaterialHandles.ScrenSpaceShadow, CoreUtils.CreateEngineMaterial(pipelineAsset.ScreenSpaceShadowShader));
            Debug.Assert(m_Materials.Count == (int)MaterialHandles.Count, "All materials in MaterialHandles should be created.");

            depthOnlyPass = new DepthOnlyPass(this);
            directionalShadowPass = new DirectionalShadowsPass(this, pipelineAsset.DirectionalShadowAtlasResolution);
            localShadowsPass = new LocalShadowsPass(this, pipelineAsset.LocalShadowAtlasResolution);
            screenSpaceShadowOcclusionPass = new ScreenSpaceShadowOcclusionPass(this);
            forwardLitPass = new ForwardLitPass(this);

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

        void AddSurface(string shaderProperty, out int handle)
        {
            handle = Shader.PropertyToID(shaderProperty);
            m_ResourceMap.Add(handle, new RenderTargetIdentifier(handle));
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
            CommandBuffer cmd = CommandBufferPool.Get("Setup Rendering");
            m_ShadowPassList.Clear();
            m_RenderPassList.Clear();
            SetupPerObjectLightIndices(ref cullResults, ref lightData);
            RenderTextureDescriptor baseDescriptor = CreateRTDesc(ref cameraData);

            bool requiresCameraDepth = cameraData.requiresDepthTexture || cameraData.postProcessEnabled || cameraData.isSceneViewCamera;
            bool shadowsEnabledForCamera = cameraData.maxShadowDistance > 0.0f;
            bool msaaEnabledForCamera = cameraData.msaaSamples > 1;
            bool supportsTexture2DMS = SystemInfo.supportsMultisampledTextures != 0;
            bool supportsTextureCopy = SystemInfo.copyTextureSupport != CopyTextureSupport.None;
            bool copyShaderSupported = GetMaterial(MaterialHandles.DepthCopy).shader.isSupported && (msaaEnabledForCamera == supportsTexture2DMS);
            bool supportsDepthCopy = copyShaderSupported || supportsTextureCopy;
            bool requiresDepthPrepassToResolveMsaa = msaaEnabledForCamera && !supportsTexture2DMS;
            bool renderDirectionalShadows = shadowsEnabledForCamera && lightData.shadowData.supportsDirectionalShadows;
            bool requiresScreenSpaceOcclusion = renderDirectionalShadows && lightData.shadowData.requiresScreenSpaceOcclusion;
            bool requiresDepthPrepass = requiresCameraDepth && (!supportsDepthCopy || requiresScreenSpaceOcclusion || requiresDepthPrepassToResolveMsaa);

            bool needsMsaaResolve = (msaaEnabledForCamera && !LightweightPipeline.PlatformSupportsMSAABackBuffer());
            bool depthRenderBuffer = requiresCameraDepth && !requiresDepthPrepass;
            bool intermediateRenderTexture = cameraData.isSceneViewCamera ||
                !Mathf.Approximately(cameraData.renderScale, 1.0f) ||
                cameraData.isHdrEnabled ||
                baseDescriptor.dimension == TextureDimension.Tex2DArray ||
                cameraData.postProcessEnabled ||
                needsMsaaResolve ||
                depthRenderBuffer ||
                cameraData.requiresOpaqueTexture;

            if (requiresDepthPrepass)
            {
                depthOnlyPass.Setup(cmd, baseDescriptor, 1);
                m_RenderPassList.Add(depthOnlyPass);
            }

            if (renderDirectionalShadows)
            {
                directionalShadowPass.Setup(cmd, baseDescriptor, 1);
                m_ShadowPassList.Add(directionalShadowPass);
                if (requiresScreenSpaceOcclusion)
                {
                    screenSpaceShadowOcclusionPass.Setup(cmd, baseDescriptor, 1);
                    m_RenderPassList.Add(screenSpaceShadowOcclusionPass);
                }
            }

            if (shadowsEnabledForCamera && lightData.shadowData.supportsLocalShadows)
            {
                localShadowsPass.Setup(cmd, baseDescriptor, 1);
                m_ShadowPassList.Add(localShadowsPass);
            }

            int colorHandle = (intermediateRenderTexture) ? RenderTargetHandles.Color : -1;
            int depthHandle = (depthRenderBuffer) ? RenderTargetHandles.Depth : -1;

            forwardLitPass.colorHandles = new[] { colorHandle };
            forwardLitPass.depthHandle = depthHandle;
            forwardLitPass.Setup(cmd, baseDescriptor, cameraData.msaaSamples);

            m_RenderPassList.Add(forwardLitPass);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Execute(ref ScriptableRenderContext context, ref CullResults cullResults, ref CameraData cameraData,
            ref LightData lightData)
        {
            for (int i = 0; i < m_ShadowPassList.Count; ++i)
                m_ShadowPassList[i].Execute(ref context, ref cullResults, ref cameraData, ref lightData);

            // SetupCameraProperties does the following:
            // Setup Camera RenderTarget and Viewport
            // VR Camera Setup and SINGLE_PASS_STEREO props
            // Setup camera view, proj and their inv matrices.
            // Setup properties: _WorldSpaceCameraPos, _ProjectionParams, _ScreenParams, _ZBufferParams, unity_OrthoParams
            // Setup camera world clip planes props
            // setup HDR keyword
            // Setup global time properties (_Time, _SinTime, _CosTime)
            context.SetupCameraProperties(cameraData.camera, cameraData.isStereoEnabled);

            for (int i = 0; i < m_RenderPassList.Count; ++i)
                m_RenderPassList[i].Execute(ref context, ref cullResults, ref cameraData, ref lightData);

#if UNITY_EDITOR
            if (cameraData.isSceneViewCamera)
                CopyDepth(ref context);
#endif

            DisposePasses(ref context);
        }

        void CopyDepth(ref ScriptableRenderContext context)
        {
            // Restore Render target for additional editor rendering.
            CommandBuffer cmd = CommandBufferPool.Get("Copy Depth to Camera");
            CoreUtils.SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget);
            cmd.Blit(GetSurface(RenderTargetHandles.Depth), BuiltinRenderTextureType.CameraTarget, GetMaterial(MaterialHandles.DepthCopy));
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        void DisposePasses(ref ScriptableRenderContext context)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Release Resources");
            for (int i = 0; i < m_ShadowPassList.Count; ++i)
                m_ShadowPassList[i].Dispose(cmd);

            for (int i = 0; i < m_RenderPassList.Count; ++i)
                m_RenderPassList[i].Dispose(cmd);

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
