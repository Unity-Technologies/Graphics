using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    public class CustomRenderer : ScriptableRenderer
    {
        DrawObjectsPass m_RenderOpaqueForwardPass;
        ForwardLights m_ForwardLights;
        MainLightShadowCasterPass m_MainLightShadowCasterPass;
        AdditionalLightsShadowCasterPass m_AdditionalLightsShadowCasterPass;

        private RTHandle m_TargetColorHandle, m_TargetDepthHandle;

        public CustomRenderer(CustomRenderGraphData data) : base(data)
        {
            stripShadowsOffVariants = true;
            stripAdditionalLightOffVariants = true;

            m_ForwardLights = new ForwardLights();

            m_RenderOpaqueForwardPass = new DrawObjectsPass("Render Opaques", true, RenderPassEvent.BeforeRenderingOpaques + 1, RenderQueueRange.opaque, -1, StencilState.defaultValue, 0);
            m_MainLightShadowCasterPass = new MainLightShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);
            m_AdditionalLightsShadowCasterPass = new AdditionalLightsShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);

            m_TargetColorHandle = null;
            m_TargetDepthHandle = null;
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            m_MainLightShadowCasterPass?.Dispose();
            m_AdditionalLightsShadowCasterPass?.Dispose();

            base.Dispose(disposing);
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ConfigureCameraTarget(k_CameraTarget, k_CameraTarget);

            foreach (var feature in rendererFeatures)
            {
                feature.AddRenderPasses(this, ref renderingData);
                feature.SetupRenderPasses(this, in renderingData);
            }
            EnqueuePass(m_RenderOpaqueForwardPass);

            bool mainLightShadows = m_MainLightShadowCasterPass.Setup(ref renderingData);
            bool additionalLightShadows = m_AdditionalLightsShadowCasterPass.Setup(ref renderingData);

            if (mainLightShadows)
                EnqueuePass(m_MainLightShadowCasterPass);
            if (additionalLightShadows)
                EnqueuePass(m_AdditionalLightsShadowCasterPass);
        }


        static ProfilingSampler s_SetupLights = new ProfilingSampler("Setup URP lights.");
        private class SetupLightPassData
        {
            internal RenderingData renderingData;
            internal ForwardLights forwardLights;
        };
        private void SetupRenderGraphLights(RenderGraph renderGraph)
        {
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            m_ForwardLights.SetupRenderGraphLights(renderGraph, renderingData ,cameraData, lightData);
        }

        public override void OnBeginRenderGraphFrame()
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            resourceData.InitFrame();
        }

        public override void OnEndRenderGraphFrame()
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            resourceData.EndFrame();
        }

        internal override void OnRecordRenderGraph(RenderGraph renderGraph, ScriptableRenderContext context)
        {
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            UniversalShadowData shadowData = frameData.Get<UniversalShadowData>();

            m_ForwardLights.PreSetup(renderingData, cameraData, lightData);
            SetupRenderGraphLights(renderGraph);

            TextureHandle mainShadowsTexture = TextureHandle.nullHandle;
            TextureHandle additionalShadowsTexture = TextureHandle.nullHandle;

            if (m_MainLightShadowCasterPass.Setup(renderingData, cameraData, lightData, shadowData))
                mainShadowsTexture = m_MainLightShadowCasterPass.Render(renderGraph, frameData);
            if (m_AdditionalLightsShadowCasterPass.Setup(renderingData, cameraData, lightData, shadowData))
                additionalShadowsTexture = m_AdditionalLightsShadowCasterPass.Render(renderGraph, frameData);

            SetupRenderGraphCameraProperties(renderGraph, cameraData.camera.targetTexture == null);

            RenderTargetIdentifier targetColorId = cameraData.targetTexture != null ? new RenderTargetIdentifier(cameraData.targetTexture) : BuiltinRenderTextureType.CameraTarget;
            RenderTargetIdentifier targetDepthId = cameraData.targetTexture != null ? new RenderTargetIdentifier(cameraData.targetTexture) : BuiltinRenderTextureType.Depth;

            if (m_TargetColorHandle == null || m_TargetColorHandle.nameID != targetColorId)
            {
                m_TargetColorHandle?.Release();
                m_TargetColorHandle = RTHandles.Alloc(targetColorId, "Backbuffer color");
            }

            if (m_TargetDepthHandle == null || m_TargetDepthHandle.nameID != targetColorId)
            {
                m_TargetDepthHandle?.Release();
                m_TargetDepthHandle = RTHandles.Alloc(targetDepthId, "Backbuffer depth");
            }

            RenderTargetInfo importInfo = new RenderTargetInfo();
            RenderTargetInfo importInfoDepth = new RenderTargetInfo();

            if (cameraData.camera.targetTexture == null)
            {
                importInfo.width = Screen.width;
                importInfo.height = Screen.height;
                importInfo.volumeDepth = 1;
                importInfo.msaaSamples = 1;

                importInfo.format = UniversalRenderPipeline.MakeRenderTextureGraphicsFormat(cameraData.isHdrEnabled, cameraData.hdrColorBufferPrecision, Graphics.preserveFramebufferAlpha);

                importInfoDepth = importInfo;
                importInfoDepth.format = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil);
            }
            else
            {
                importInfo.width = cameraData.targetTexture.width;
                importInfo.height = cameraData.targetTexture.height;
                importInfo.volumeDepth = cameraData.targetTexture.volumeDepth;
                importInfo.msaaSamples = cameraData.targetTexture.antiAliasing;
                importInfo.format = cameraData.targetTexture.graphicsFormat;

                importInfoDepth = importInfo;
                importInfoDepth.format = cameraData.targetTexture.depthStencilFormat;

                // TODO: What to do here?? Alocate a temp depth likely what was happening before/
                // We could also just importInfoDepth.format = SystemInfo.GetGraphicsFormat(DefaultFormat.DepthStencil); to shut-up rendergraph and let the lower
                // level sort it out?
                if (importInfoDepth.format == GraphicsFormat.None)
                {
                    throw new System.Exception("Trying to render to a rendertexture without a depth buffer. URP+RG needs a depthbuffer to render.");
                }
            }


            ImportResourceParams importBackbufferParams = new ImportResourceParams();
            importBackbufferParams.clearOnFirstUse = true;
            importBackbufferParams.clearColor = cameraData.backgroundColor;
            importBackbufferParams.discardOnLastUse = false;

            var targetHandle = renderGraph.ImportTexture(m_TargetColorHandle, importInfo, importBackbufferParams);
            var depthHandle = renderGraph.ImportTexture(m_TargetDepthHandle, importInfoDepth, importBackbufferParams);


            if (!renderGraph.nativeRenderPassesEnabled)
            {
                ClearTargetsPass.Render(renderGraph, targetHandle, depthHandle, cameraData);
            }

            m_RenderOpaqueForwardPass.Render(renderGraph, frameData, targetHandle, depthHandle, mainShadowsTexture, additionalShadowsTexture);
        }

        [Obsolete(DeprecationMessage.CompatibilityScriptingAPIObsolete, false)]
        public override void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            UniversalRenderingData universalRenderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            m_ForwardLights.SetupLights(CommandBufferHelpers.GetUnsafeCommandBuffer(universalRenderingData.commandBuffer), universalRenderingData, cameraData, lightData);
        }

        internal override bool supportsNativeRenderPassRendergraphCompiler
        {
            get => SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLES3 // GLES doesn't support backbuffer MSAA resolve with the NRP API
                   && SystemInfo.graphicsDeviceType != GraphicsDeviceType.OpenGLCore
            ;
        }
    }
}
