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
        ExposurePass m_ExposurePass;

        private RTHandle m_TargetColorHandle, m_TargetDepthHandle;

        // Owns the 3D group only, the base vars come from the default path (SetupRenderGraphDefaultCameraProperties).
        // A custom renderer using ForwardLights and the shadow passes fills and pushes the 3D vars itself, otherwise
        // the main light, its cookie and the shadow globals stay at zero. Dirty flags keep the push to what was filled.
        GlobalShaderVariablesUploader m_GlobalShaderVarsUploader = new GlobalShaderVariablesUploader(GlobalShaderVariablesGroup.Only3D);

        private class PushGlobalShaderVarsPassData
        {
            internal GlobalShaderVariablesUploader uploader;
        }

        public CustomRenderer(CustomRenderGraphData data) : base(data)
        {
            m_ForwardLights = new ForwardLights();

            m_RenderOpaqueForwardPass = new DrawObjectsPass("Render Opaques", true, RenderPassEvent.BeforeRenderingOpaques + 1, RenderQueueRange.opaque, -1, StencilState.defaultValue, 0);
            m_MainLightShadowCasterPass = new MainLightShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);
            m_AdditionalLightsShadowCasterPass = new AdditionalLightsShadowCasterPass(RenderPassEvent.BeforeRenderingShadows);

            m_TargetColorHandle = null;
            m_TargetDepthHandle = null;

            m_ExposurePass = new ExposurePass();
        }

        /// <inheritdoc />
        protected override void Dispose(bool disposing)
        {
            m_MainLightShadowCasterPass?.Dispose();
            m_AdditionalLightsShadowCasterPass?.Dispose();

            base.Dispose(disposing);
        }

        private void SetupRenderGraphLights(RenderGraph renderGraph)
        {
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();

            m_ForwardLights.SetupRenderGraphLights(renderGraph, renderingData ,cameraData, lightData);
        }

        // Must run once both shadow passes are set up, so their per-frame state is final.
        private void UpdateGlobalShaderVariables()
        {
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            UniversalShadowData shadowData = frameData.Get<UniversalShadowData>();

            // Nothing filled this uploader before this point, so the whole fill window lives in this method.
            m_GlobalShaderVarsUploader.BeginFill();

            m_ForwardLights.UpdateGlobalShaderVariables(m_GlobalShaderVarsUploader, cameraData, lightData);

            m_MainLightShadowCasterPass.UpdateGlobalShaderVariables(m_GlobalShaderVarsUploader, lightData, shadowData);
            m_AdditionalLightsShadowCasterPass.UpdateGlobalShaderVariables(m_GlobalShaderVarsUploader, shadowData.supportsSoftShadows);

            // Nothing fills the uploader after this point, freeze the staged state as the defaults of this camera.
            m_GlobalShaderVarsUploader.EndFill();
        }

        // Recorded after SetupRenderGraphDefaultCameraProperties, which pushes the base variables and lets
        // cmd.SetupCameraProperties overwrite part of the global state.
        private void PushGlobalShaderVariables(RenderGraph renderGraph)
        {
            using (var builder = renderGraph.AddUnsafePass<PushGlobalShaderVarsPassData>("Push Global Shader Variables", out var passData))
            {
                passData.uploader = m_GlobalShaderVarsUploader;

                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (PushGlobalShaderVarsPassData data, UnsafeGraphContext rgContext) =>
                {
                    data.uploader.PushDefaultToGlobal(rgContext.cmd);
                });
            }
        }

        public override void OnBeginRenderGraphFrame()
        {
            base.OnBeginRenderGraphFrame();

            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            resourceData.InitFrame();
        }

        public override void OnEndRenderGraphFrame()
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            resourceData.EndFrame();
        }

        private class InitShadowGlobalsPassData
        {
            internal MainLightShadowCasterPass mainLightShadowCasterPass;
            internal AdditionalLightsShadowCasterPass additionalLightsShadowCasterPass;
        };

        internal override void InitRenderGraphFrame(RenderGraph renderGraph)
        {
            base.InitRenderGraphFrame(renderGraph);

            // The shadow passes record nothing in empty mode, so the default shadowmaps and the empty keywords/params
            // have to be published here instead. Runs after the pass in base.InitRenderGraphFrame, which resets the shadow keywords.
            using (var builder = renderGraph.AddUnsafePass<InitShadowGlobalsPassData>("Init Shadow Globals", out var passData))
            {
                passData.mainLightShadowCasterPass = m_MainLightShadowCasterPass;
                passData.additionalLightsShadowCasterPass = m_AdditionalLightsShadowCasterPass;

                builder.AllowGlobalStateModification(true);

                TextureHandle defaultShadowTexture = renderGraph.defaultResources.defaultShadowTexture;
                MainLightShadowCasterPass.SetDefaultShadowmapGlobalTexture(builder, defaultShadowTexture);
                AdditionalLightsShadowCasterPass.SetDefaultShadowmapGlobalTexture(builder, defaultShadowTexture);

                builder.SetRenderFunc(static (InitShadowGlobalsPassData data, UnsafeGraphContext rgContext) =>
                {
                    data.mainLightShadowCasterPass.ApplyEmptyShadowmapGlobals(rgContext.cmd);
                    data.additionalLightsShadowCasterPass.ApplyEmptyShadowmapGlobals(rgContext.cmd);
                });
            }
        }

        internal override void OnRecordRenderGraph(RenderGraph renderGraph, ScriptableRenderContext context)
        {
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalLightData lightData = frameData.Get<UniversalLightData>();
            UniversalShadowData shadowData = frameData.Get<UniversalShadowData>();
            var resourceData = frameData.Get<UniversalResourceData>();

            m_ForwardLights.PreSetup(renderingData, cameraData, lightData);
            SetupRenderGraphLights(renderGraph);

            m_MainLightShadowCasterPass.Setup(renderingData, cameraData, lightData, shadowData);
            m_MainLightShadowCasterPass.RecordRenderGraph(renderGraph, frameData);
            TextureHandle mainShadowsTexture = resourceData.mainShadowsTexture;

            m_AdditionalLightsShadowCasterPass.Setup(renderingData, cameraData, lightData, shadowData);
            m_AdditionalLightsShadowCasterPass.RecordRenderGraph(renderGraph, frameData);
            TextureHandle additionalShadowsTexture = resourceData.additionalShadowsTexture;

            UpdateGlobalShaderVariables();

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

            SetupRenderGraphDefaultCameraProperties(renderGraph, targetHandle.IsValid() ? targetHandle : depthHandle);
            PushGlobalShaderVariables(renderGraph);

            m_ExposurePass.RecordRenderGraph(renderGraph, frameData);

            m_RenderOpaqueForwardPass.Render(renderGraph, frameData, targetHandle, depthHandle, mainShadowsTexture, additionalShadowsTexture);
        }
    }
}
