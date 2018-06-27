using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.XR;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public class LightweightDeferredRenderer : ScriptableRenderer
    {
        const string k_GBufferProfilerTag = "Render GBuffer";

        Material m_BlitMaterial;
        Material m_DefaultBlitMaterial;
        Material m_DeferredLightingMaterial;
        Mesh m_PointLightProxyMesh;
        Mesh m_SpotLightProxyMesh;

        MaterialPropertyBlock m_DeferredLightingProperties = new MaterialPropertyBlock();

        int m_CameraColorTextureHandle;
        RenderTargetIdentifier m_CameraColorTexture;
        RenderTargetIdentifier m_CameraTarget;

        readonly bool m_SupportsHDR;

        public LightweightDeferredRenderer(LightweightPipelineAsset asset)
        {
            m_SupportsHDR = asset.supportsHDR;

            m_BlitMaterial = CoreUtils.CreateEngineMaterial(asset.blitTransientShader);
            m_DefaultBlitMaterial = CoreUtils.CreateEngineMaterial(asset.blitShader);
            m_DeferredLightingMaterial = CoreUtils.CreateEngineMaterial(asset.deferredLightingShader);

            m_PointLightProxyMesh = asset.pointLightProxyMesh;
            m_SpotLightProxyMesh = asset.spotLightPointMesh;

            m_CameraColorTextureHandle = Shader.PropertyToID("_CameraColorTexture");
            m_CameraColorTexture = new RenderTargetIdentifier(m_CameraColorTextureHandle);
        }

        public override void Setup(ref ScriptableRenderContext context, ref CullResults cullResults,
            ref RenderingData renderingData)
        {
            if (renderingData.cameraData.isSceneViewCamera)
            {
                RenderTextureDescriptor baseDescriptor = CreateRTDesc(ref renderingData.cameraData);
                baseDescriptor.depthBufferBits = 32;
                baseDescriptor.sRGB = true;
                baseDescriptor.msaaSamples = 1;

                CommandBuffer cmd = CommandBufferPool.Get("Create color texture");
                cmd.GetTemporaryRT(m_CameraColorTextureHandle, baseDescriptor, FilterMode.Bilinear);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
                context.Submit();
                m_CameraTarget = m_CameraColorTexture;
            }
            else
            {
                m_CameraTarget = BuiltinRenderTextureType.CameraTarget;
            }
        }

        public override void Execute(ref ScriptableRenderContext context, ref CullResults cullResults,
            ref RenderingData renderingData)
        {
            Camera camera = renderingData.cameraData.camera;
            float renderScale = renderingData.cameraData.renderScale;
            int cameraPixelWidth = (int) (camera.pixelWidth * renderScale);
            int cameraPixelHeight = (int) (camera.pixelHeight * renderScale);

            context.SetupCameraProperties(renderingData.cameraData.camera, renderingData.cameraData.isStereoEnabled);

            var depth = new AttachmentDescriptor(RenderTextureFormat.Depth);
            depth.Clear(Color.black, 1.0f, 0);
            using (RenderPass rp = context.BeginRenderPass(cameraPixelWidth, cameraPixelHeight, 1, depth))
            {
                var gBufferAlbedoId = context.CreateAttachment(new AttachmentDescriptor(RenderTextureFormat.ARGB32));
                var gBufferSpecRoughId = context.CreateAttachment(new AttachmentDescriptor(RenderTextureFormat.ARGB32));
                var gBufferNormalId = context.CreateAttachment(new AttachmentDescriptor(RenderTextureFormat.ARGB2101010));

                var lightAccumulation = new AttachmentDescriptor(m_SupportsHDR ? RenderTextureFormat.DefaultHDR : RenderTextureFormat.Default);
                lightAccumulation.Clear(Color.black, 1.0f, 0);
                lightAccumulation.BindSurface(m_CameraTarget, false, true);
                var lightAccumulationId = context.CreateAttachment(lightAccumulation);

                using (context.BeginSubPass(new AttachmentList { gBufferAlbedoId, gBufferSpecRoughId, gBufferNormalId, lightAccumulationId }))
                {
                    GBufferPass(ref context, ref cullResults, camera);
                }

                using (context.BeginSubPass(new AttachmentList { lightAccumulationId }, new AttachmentList { gBufferAlbedoId, gBufferSpecRoughId, gBufferNormalId, rp.depthId }, true))
                {
                    LightingPass(ref context, ref cullResults, ref renderingData.lightData);
                }

                using (context.BeginSubPass(new AttachmentList { lightAccumulationId }))
                {
                    context.DrawSkybox(camera);
                }

                //using (new RenderPass.SubPass(rp, new AttachmentList { lightAccumulationId }, null))
                //{
                //    TransparentPass();
                //}
            }

            if (renderingData.cameraData.isSceneViewCamera)
            {
                CommandBuffer cmd = CommandBufferPool.Get("Final Blit");
                cmd.SetGlobalTexture("_BlitTex", m_CameraColorTexture);
                cmd.Blit(m_CameraTarget, BuiltinRenderTextureType.CameraTarget, m_DefaultBlitMaterial);
                CoreUtils.SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        public void GBufferPass(ref ScriptableRenderContext context, ref CullResults cullResults, Camera camera)
        {
            CommandBuffer cmd = CommandBufferPool.Get(k_GBufferProfilerTag);
            using (new ProfilingSample(cmd, k_GBufferProfilerTag))
            {
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                var drawSettings = new DrawRendererSettings(camera, new ShaderPassName("LightweightDeferred"))
                {
                    sorting = {flags = SortFlags.CommonOpaque},
                    rendererConfiguration = RendererConfiguration.PerObjectLightmaps | RendererConfiguration.PerObjectLightProbe,
                    flags = DrawRendererFlags.EnableInstancing,
                };

                var filterSettings = new FilterRenderersSettings(true)
                {
                    renderQueueRange = RenderQueueRange.opaque,
                };

                context.DrawRenderers(cullResults.visibleRenderers, ref drawSettings, filterSettings);
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void LightingPass(ref ScriptableRenderContext context, ref CullResults cullResults, ref LightData lightData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Deferred Lighting");
            List<VisibleLight> visibleLights = lightData.visibleLights;

            m_DeferredLightingProperties.Clear();
            Vector4 lightPosition;
            Vector4 lightColor;
            Vector4 lightAttenuation;
            Vector4 lightSpotDirection;
            Vector4 lightSpotAttenuation;
            InitializeLightConstants(visibleLights, lightData.mainLightIndex, MixedLightingSetup.None, out lightPosition, out lightColor, out lightAttenuation, out lightSpotDirection, out lightSpotAttenuation);

            m_DeferredLightingProperties.SetVector(PerCameraBuffer._MainLightPosition, new Vector4(lightPosition.x, lightPosition.y, lightPosition.z, lightAttenuation.w));
            m_DeferredLightingProperties.SetVector(PerCameraBuffer._MainLightColor, lightColor);
            LightweightPipeline.DrawFullScreen(cmd, m_DeferredLightingMaterial, m_DeferredLightingProperties);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void TransparentPass()
        {
            //using (var cmd = new CommandBuffer { name = "Forwward Lighting Setup" })
            //{

            //    SetupLightShaderVariables(cullResults, camera, loop, cmd);
            //    loop.ExecuteCommandBuffer(cmd);

            //    var settings = new DrawRendererSettings(camera, new ShaderPassName("ForwardSinglePass"))
            //    {
            //        sorting = { flags = SortFlags.CommonTransparent },
            //        rendererConfiguration = RendererConfiguration.PerObjectLightmaps | RendererConfiguration.PerObjectLightProbe,
            //    };
            //    var filterSettings = new FilterRenderersSettings(true) { renderQueueRange = RenderQueueRange.transparent };
            //    loop.DrawRenderers(cullResults.visibleRenderers, ref settings, filterSettings);
            //}
        }

        public void FinalPass(ref ScriptableRenderContext context, ref CullResults cullResults, ref CameraData cameraData)
        {
            CommandBuffer cmd = CommandBufferPool.Get("Final Blit Pass");
            LightweightPipeline.DrawFullScreen(cmd, m_BlitMaterial);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public RenderTextureDescriptor CreateRTDesc(ref CameraData cameraData, float scaler = 1.0f)
        {
            Camera camera = cameraData.camera;
            RenderTextureDescriptor desc;
#if !UNITY_SWITCH
            if (cameraData.isStereoEnabled)
                desc = XRSettings.eyeTextureDesc;
            else
#endif
                desc = new RenderTextureDescriptor(camera.pixelWidth, camera.pixelHeight);

            float renderScale = cameraData.renderScale;
            desc.colorFormat = cameraData.isHdrEnabled ? RenderTextureFormat.DefaultHDR :
                RenderTextureFormat.Default;
            desc.enableRandomWrite = false;
            desc.width = (int)((float)desc.width * renderScale * scaler);
            desc.height = (int)((float)desc.height * renderScale * scaler);
            return desc;
        }
    }
}
