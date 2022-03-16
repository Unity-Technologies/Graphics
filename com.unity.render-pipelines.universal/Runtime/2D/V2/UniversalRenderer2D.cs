using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    internal class UniversalRenderer2D : ScriptableRenderer, IRenderPass2D
    {
        private Renderer2DData m_RendererData;
        Light2DCullResult m_LightCullResult;
        private FinalBlitPass m_FinalBlitPass;
        private Attachments m_Attachments;

        public class Attachments
        {
            private RTHandle[] m_Buffers;
            private RTHandle m_ColorAttachment;
            private RTHandle m_DepthAttachment;
            private RTHandle m_NormalAttachment;
            private RTHandle m_DepthTexture;

            public RTHandle[] buffers => m_Buffers;
            public RTHandle normalAttachment => m_NormalAttachment;
            public RTHandle colorAttachment => m_ColorAttachment;
            public RTHandle depthAttachment => m_DepthAttachment;

            public RTHandle intermediateDepthAttachment => m_DepthTexture;

            public Attachments()
            {
                m_Buffers = new RTHandle[4];
            }

            public void Init(ScriptableRenderContext context, ref RenderingData renderingData, Renderer2DData rendererData)
            {
                ref var cameraData = ref renderingData.cameraData;
                ref var cameraTargetDescriptor = ref cameraData.cameraTargetDescriptor;

                var renderTextureScale = Mathf.Clamp(rendererData.lightRenderTextureScale, 0.01f, 1.0f);
                var width = (int)(renderingData.cameraData.cameraTargetDescriptor.width * renderTextureScale);
                var height = (int)(renderingData.cameraData.cameraTargetDescriptor.height * renderTextureScale);

                {
                    var gbufferSlice = cameraTargetDescriptor;
                    // var desc = this.GetBlendStyleRenderTextureDesc(renderingData);
                    gbufferSlice.depthBufferBits = 0; // make sure no depth surface is actually created
                    gbufferSlice.stencilFormat = GraphicsFormat.None;
                    gbufferSlice.graphicsFormat = QualitySettings.activeColorSpace == ColorSpace.Linear ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
                    gbufferSlice.width = width;
                    gbufferSlice.height = height;
                    gbufferSlice.msaaSamples = 1;
                    gbufferSlice.useMipMap = false;
                    gbufferSlice.autoGenerateMips = false;
                    for (var i = 0; i < m_Buffers.Length; i++)
                    {
                        RenderingUtils.ReAllocateIfNeeded(ref m_Buffers[i], gbufferSlice, FilterMode.Bilinear, TextureWrapMode.Clamp, name: Render2DLightingPass.k_ShapeLightTextureNames[i]);
                    }
                }

                {
                    var colorDescriptor = cameraTargetDescriptor;
                    colorDescriptor.depthBufferBits = 0;
                    RenderingUtils.ReAllocateIfNeeded(ref m_ColorAttachment, colorDescriptor, FilterMode.Bilinear, wrapMode: TextureWrapMode.Clamp, name: "_CameraColorAttachment");
                }

                {
                    var depthDescriptor = cameraTargetDescriptor;
                    depthDescriptor.colorFormat = RenderTextureFormat.Depth;
                    depthDescriptor.depthBufferBits = 32;

                    if (!cameraData.resolveFinalTarget && true)
                        depthDescriptor.bindMS = depthDescriptor.msaaSamples > 1 && !SystemInfo.supportsMultisampleAutoResolve && (SystemInfo.supportsMultisampledTextures != 0);

                    RenderingUtils.ReAllocateIfNeeded(ref m_DepthAttachment, depthDescriptor, FilterMode.Point, wrapMode: TextureWrapMode.Clamp, name: "_CameraDepthAttachment");
                }

                {
                    var depthDescriptor = cameraTargetDescriptor;
                    depthDescriptor.colorFormat = RenderTextureFormat.Depth;
                    depthDescriptor.depthBufferBits = 32;
                    depthDescriptor.width = width;
                    depthDescriptor.height = height;
                    if (!cameraData.resolveFinalTarget && true)
                        depthDescriptor.bindMS = depthDescriptor.msaaSamples > 1 && !SystemInfo.supportsMultisampleAutoResolve && (SystemInfo.supportsMultisampledTextures != 0);

                    RenderingUtils.ReAllocateIfNeeded(ref m_DepthTexture, depthDescriptor, FilterMode.Point, wrapMode: TextureWrapMode.Clamp, name: "_CameraDepthTexture");
                }

                {
                    var desc = cameraTargetDescriptor;
                    desc.graphicsFormat = QualitySettings.activeColorSpace == ColorSpace.Linear ? GraphicsFormat.R8G8B8A8_SRGB : GraphicsFormat.R8G8B8A8_UNorm;
                    desc.depthBufferBits = 0;
                    desc.width = width;
                    desc.height = height;
                    desc.msaaSamples = 1;
                    desc.useMipMap = false;
                    desc.autoGenerateMips = false;
                    RenderingUtils.ReAllocateIfNeeded(ref m_NormalAttachment, desc, FilterMode.Point, wrapMode: TextureWrapMode.Clamp, name: "_NormalAttachment");
                }


                // setup some globals
                {
                    var cmd = CommandBufferPool.Get();
                    cmd.SetGlobalTexture("_NormalMap", normalAttachment);
                    foreach (var gb in buffers)
                        cmd.SetGlobalTexture(gb.name, gb.nameID);
                    context.ExecuteCommandBuffer(cmd);
                    CommandBufferPool.Release(cmd);
                }
            }

            public void Release()
            {
                foreach(var gb in buffers)
                    gb?.Release();

                m_ColorAttachment?.Release();
                m_DepthAttachment?.Release();
                m_DepthTexture?.Release();
                m_NormalAttachment?.Release();
            }
        }

        public UniversalRenderer2D(Renderer2DData data) : base(data)
        {
            m_RendererData = data;
            m_LightCullResult = new Light2DCullResult();
            m_RendererData.lightCullResult = m_LightCullResult;
            var blitMaterial = CoreUtils.CreateEngineMaterial(data.blitShader);
            m_FinalBlitPass = new FinalBlitPass(RenderPassEvent.AfterRendering + 1, blitMaterial);
            m_Attachments = new Attachments();
        }

        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref var cameraData = ref renderingData.cameraData;
            ref var cameraTargetDescriptor = ref cameraData.cameraTargetDescriptor;

            m_Attachments.Init(context, ref renderingData, m_RendererData);
            ConfigureCameraTarget(m_Attachments.colorAttachment, m_Attachments.depthAttachment);

            var layerBatches = LayerUtility.CalculateBatches(m_RendererData.lightCullResult, out var batchCount);
            for (var i = 0; i < batchCount; i += 1)
            {
                ref var layerBatch = ref layerBatches[i];
                layerBatch.FilterLights(m_RendererData.lightCullResult.visibleLights);

                if (layerBatch.lightStats.totalNormalMapUsage > 0)
                {
                    var normalPass = new DrawNormal2DPass(layerBatch, m_Attachments);
                    EnqueuePass(normalPass);
                }

                var m_DrawLightPass = new DrawLight2DPass(m_RendererData, layerBatch, m_Attachments);
                EnqueuePass(m_DrawLightPass);

                var m_DrawObjectPass = new DrawRenderer2DPass(m_RendererData, layerBatch, m_Attachments);
                EnqueuePass(m_DrawObjectPass);
            }

            m_FinalBlitPass.Setup(cameraTargetDescriptor, m_Attachments.colorAttachment);
            EnqueuePass(m_FinalBlitPass);
        }

        public override void SetupCullingParameters(ref ScriptableCullingParameters cullingParameters, ref CameraData cameraData)
        {
            cullingParameters.cullingOptions = CullingOptions.None;
            cullingParameters.isOrthographic = cameraData.camera.orthographic;
            cullingParameters.shadowDistance = 0.0f;
            m_LightCullResult.SetupCulling(ref cullingParameters, cameraData.camera);
        }

        protected override void Dispose(bool disposing)
        {
            m_Attachments.Release();
        }

        public Renderer2DData rendererData => m_RendererData;
    }
}
