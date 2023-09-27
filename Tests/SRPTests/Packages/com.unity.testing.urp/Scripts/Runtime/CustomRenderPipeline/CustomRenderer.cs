using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
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
        private void SetupRenderGraphLights(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            m_ForwardLights.SetupRenderGraphLights(renderGraph, ref renderingData);
        }

        internal override void OnRecordRenderGraph(RenderGraph renderGraph, ScriptableRenderContext context, ref RenderingData renderingData)
        {
            CameraData cameraData = renderingData.cameraData;

            m_ForwardLights.PreSetup(ref renderingData);
            SetupRenderGraphLights(renderGraph, ref renderingData);

            TextureHandle mainShadowsTexture = TextureHandle.nullHandle;
            TextureHandle additionalShadowsTexture = TextureHandle.nullHandle;

            if (m_MainLightShadowCasterPass.Setup(ref renderingData))
                mainShadowsTexture = m_MainLightShadowCasterPass.Render(renderGraph, ref renderingData);
            if (m_AdditionalLightsShadowCasterPass.Setup(ref renderingData))
                additionalShadowsTexture = m_AdditionalLightsShadowCasterPass.Render(renderGraph, ref renderingData);

            SetupRenderGraphCameraProperties(renderGraph, renderingData.cameraData.camera.targetTexture == null);

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
            importBackbufferParams.clearColor = renderingData.cameraData.backgroundColor;
            importBackbufferParams.discardOnLastUse = false;

            var targetHandle = renderGraph.ImportTexture(m_TargetColorHandle, importInfo, importBackbufferParams);
            var depthHandle = renderGraph.ImportTexture(m_TargetDepthHandle, importInfoDepth, importBackbufferParams);


            if (!renderGraph.NativeRenderPassesEnabled)
            {
                ClearTargetsPass.Render(renderGraph, targetHandle, depthHandle, renderingData.cameraData);
            }

            m_RenderOpaqueForwardPass.Render(renderGraph, targetHandle, depthHandle, mainShadowsTexture, additionalShadowsTexture, ref renderingData);
        }

        public override void SetupLights(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_ForwardLights.SetupLights(renderingData.commandBuffer, ref renderingData);
        }

        internal override bool supportsNativeRenderPassRendergraphCompiler { get => true; }
    }
}
