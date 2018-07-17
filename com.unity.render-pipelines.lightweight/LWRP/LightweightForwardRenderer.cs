using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine.Rendering;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
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
        public static bool useComputeBufferForPerObjectLightIndices
        {
            get
            {
                return SystemInfo.supportsComputeShaders &&
                       SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLCore &&
                       !Application.isMobilePlatform &&
                       Application.platform != RuntimePlatform.WebGLPlayer;
            }
        }

        public int maxVisibleLocalLights { get { return k_MaxVisibleLocalLights; } }

        public int maxSupportedVertexLights { get { return k_MaxVertexLights; } }

        public PostProcessRenderContext postProcessRenderContext { get; private set; }

        public ComputeBuffer perObjectLightIndices { get; private set; }

        public FilterRenderersSettings opaqueFilterSettings { get; private set; }
        public FilterRenderersSettings transparentFilterSettings { get; private set; }

        List<ScriptableRenderPass> m_ActiveRenderPassQueue = new List<ScriptableRenderPass>();

        Material[] m_Materials;

        
        public LightweightForwardRenderer(LightweightPipelineAsset pipelineAsset)
        {
            m_Materials = new Material[(int)MaterialHandles.Count]
            {
                CoreUtils.CreateEngineMaterial("Hidden/InternalErrorShader"),
                CoreUtils.CreateEngineMaterial(pipelineAsset.copyDepthShader),
                CoreUtils.CreateEngineMaterial(pipelineAsset.samplingShader),
                CoreUtils.CreateEngineMaterial(pipelineAsset.blitShader),
                CoreUtils.CreateEngineMaterial(pipelineAsset.screenSpaceShadowShader),
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
            float renderScale = cameraData.renderScale;
            
            if (cameraData.isStereoEnabled)
            {
                return XRGraphicsConfig.eyeTextureDesc; 
            }
            else
            {
                desc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);
            }
            desc.colorFormat = cameraData.isHdrEnabled ? RenderTextureFormat.DefaultHDR :
                RenderTextureFormat.Default;
            desc.enableRandomWrite = false;
            desc.width = (int)((float)desc.width * renderScale * scaler);
            desc.height = (int)((float)desc.height * renderScale * scaler);
            return desc;
        }



        public void Execute(ref ScriptableRenderContext context, ref CullResults cullResults, ref RenderingData renderingData)
        {
            for (int i = 0; i < m_ActiveRenderPassQueue.Count; ++i)
                m_ActiveRenderPassQueue[i].Execute(ref context, ref cullResults, ref renderingData);

            DisposePasses(ref context);
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


        public void Clear()
        {
            m_ActiveRenderPassQueue.Clear();
        }

        public void EnqueuePass(ScriptableRenderPass pass)
        {
            m_ActiveRenderPassQueue.Add(pass);
        }

        public static bool RequiresIntermediateColorTexture(ref CameraData cameraData, RenderTextureDescriptor baseDescriptor, bool requiresCameraDepth)
        {
            if (cameraData.isOffscreenRender)
                return false;

            bool isScaledRender = !Mathf.Approximately(cameraData.renderScale, 1.0f);
            bool isTargetTexture2DArray = baseDescriptor.dimension == TextureDimension.Tex2DArray;
            return requiresCameraDepth || cameraData.isSceneViewCamera || isScaledRender || cameraData.isHdrEnabled ||
                cameraData.postProcessEnabled || cameraData.requiresOpaqueTexture || isTargetTexture2DArray || !cameraData.isDefaultViewport;
        }

        public static bool CanCopyDepth(ref CameraData cameraData)
        {
            bool msaaEnabledForCamera = (int)cameraData.msaaSamples > 1;
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

            for (int i = 0; i < m_ActiveRenderPassQueue.Count; ++i)
                m_ActiveRenderPassQueue[i].Dispose(cmd);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void SetupPerObjectLightIndices(ref CullResults cullResults, ref LightData lightData)
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

        public static ClearFlag GetCameraClearFlag(Camera camera)
        {
            ClearFlag clearFlag = ClearFlag.None;
            CameraClearFlags cameraClearFlags = camera.clearFlags;
            if (cameraClearFlags != CameraClearFlags.Nothing)
            {
                clearFlag |= ClearFlag.Depth;
                if (cameraClearFlags == CameraClearFlags.Color || cameraClearFlags == CameraClearFlags.Skybox)
                    clearFlag |= ClearFlag.Color;
            }

            return clearFlag;
        }

        public static RendererConfiguration GetRendererConfiguration(int localLightsCount)
        {
            RendererConfiguration configuration = RendererConfiguration.PerObjectReflectionProbes | RendererConfiguration.PerObjectLightmaps | RendererConfiguration.PerObjectLightProbe;
            if (localLightsCount > 0)
            {
                if (useComputeBufferForPerObjectLightIndices)
                    configuration |= RendererConfiguration.ProvideLightIndices;
                else
                    configuration |= RendererConfiguration.PerObjectLightIndices8;
            }

            return configuration;
        }
    }
}
