using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    public class UniversalRenderer2D : ScriptableRenderer, IRenderPass2D
    {
        private Renderer2DData m_RendererData;
        Light2DCullResult m_LightCullResult;
        private RTHandle m_ColorAttachmentHandle;
        private RTHandle m_DepthTextureHandle;
        private RTHandle m_DepthAttachmentHandle;
        private RTHandle m_NormalAttachmentHandle;
        private FinalBlitPass m_FinalBlitPass;
        private RTHandle[] m_GBuffers;
        private Material m_ClearMaterial;

        public UniversalRenderer2D(Renderer2DData data) : base(data)
        {
            m_RendererData = data;
            m_LightCullResult = new Light2DCullResult();
            m_RendererData.lightCullResult = m_LightCullResult;
            m_ClearMaterial = CoreUtils.CreateEngineMaterial(data.clearShader);
            var blitMaterial = CoreUtils.CreateEngineMaterial(data.blitShader);
            m_FinalBlitPass = new FinalBlitPass(RenderPassEvent.AfterRendering + 1, blitMaterial);
            m_GBuffers = new RTHandle[3];
        }

        void CreateGBuffers(ref RenderingData renderingData)
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
                // gbufferSlice.memoryless = RenderTextureMemoryless.Color;
                for (var i = 0; i < m_GBuffers.Length; i++)
                {
                    RenderingUtils.ReAllocateIfNeeded(ref m_GBuffers[i], gbufferSlice, FilterMode.Bilinear, TextureWrapMode.Clamp, name: Render2DLightingPass.k_ShapeLightTextureNames[i]);
                }
            }

            {
                var colorDescriptor = cameraTargetDescriptor;
                colorDescriptor.depthBufferBits = 0;
                RenderingUtils.ReAllocateIfNeeded(ref m_ColorAttachmentHandle, colorDescriptor, FilterMode.Bilinear, wrapMode: TextureWrapMode.Clamp, name: "_CameraColorTexture");
            }

            {
                var depthDescriptor = cameraTargetDescriptor;
                depthDescriptor.colorFormat = RenderTextureFormat.Depth;
                depthDescriptor.depthBufferBits = 32;

                if (!cameraData.resolveFinalTarget && true)
                    depthDescriptor.bindMS = depthDescriptor.msaaSamples > 1 && !SystemInfo.supportsMultisampleAutoResolve && (SystemInfo.supportsMultisampledTextures != 0);
                RenderingUtils.ReAllocateIfNeeded(ref m_DepthAttachmentHandle, depthDescriptor, FilterMode.Point, wrapMode: TextureWrapMode.Clamp, name: "_CameraDepthAttachment");
            }

            {
                var depthDescriptor = cameraTargetDescriptor;
                depthDescriptor.colorFormat = RenderTextureFormat.Depth;
                depthDescriptor.depthBufferBits = 32;
                depthDescriptor.width = width;
                depthDescriptor.height = height;

                if (!cameraData.resolveFinalTarget && true)
                    depthDescriptor.bindMS = depthDescriptor.msaaSamples > 1 && !SystemInfo.supportsMultisampleAutoResolve && (SystemInfo.supportsMultisampledTextures != 0);

                RenderingUtils.ReAllocateIfNeeded(ref m_DepthTextureHandle, depthDescriptor, FilterMode.Point, wrapMode: TextureWrapMode.Clamp, name: "_CameraDepthTexture");
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
                RenderingUtils.ReAllocateIfNeeded(ref m_NormalAttachmentHandle, desc, FilterMode.Point, wrapMode: TextureWrapMode.Clamp, name: "_CameraNormalTexture");
            }
        }

        /// <summary>
        /// Passes to add
        /// - Depth Pass - if use depth
        /// - Copy Depth Pass - if depth is scaled down
        /// - For each layer batch
        ///   - Normal Pass
        ///   - Lighting Pass
        ///   - Render Pass
        ///
        /// Class designs
        /// -------------
        /// Framework: Renderer, Pass, ResourceManager, ShaderManager, BatchManager
        /// Components: Light2D
        /// Data: RendererData2D, BlendStyle
        ///
        /// Test-ability
        /// ------------
        /// - what if we only have DUMB pass with no ability to read from external context/renderingData
        ///
        /// What if we go full deferred
        /// - GBuffer Pass
        ///   - we can attach up to 4 GBuffers, essentially meta data needed for the lighting pass
        ///   - shader can write to anyone of them for whatever reason
        /// - Lighting Pass
        ///   - read from the 4 GBuffers
        ///   - Look at the lights and combine with the 4 GBuffers
        ///   - to form the final color
        ///
        /// Strategy 2:
        /// - Use GBuffer as light texture
        /// -
        ///
        /// </summary>
        /// <param name="context"></param>
        /// <param name="renderingData"></param>
        public override void Setup(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            ref var cameraData = ref renderingData.cameraData;
            ref var cameraTargetDescriptor = ref cameraData.cameraTargetDescriptor;

            CreateGBuffers(ref renderingData);
            ConfigureCameraTarget(m_ColorAttachmentHandle, m_DepthAttachmentHandle);

            var layerBatches = LayerUtility.CalculateBatches(m_RendererData.lightCullResult, out var batchCount);
            for (var i = 0; i < batchCount; i += 1)
            {
                ref var layerBatch = ref layerBatches[i];
                layerBatch.FilterLights(m_RendererData.lightCullResult.visibleLights);

                if (layerBatch.lightStats.totalNormalMapUsage > 0)
                {
                    var normalPass = new DrawNormal2DPass();
                    normalPass.Setup(layerBatch, m_NormalAttachmentHandle, m_DepthTextureHandle);
                    EnqueuePass(normalPass);
                }

                var m_DrawGlobalLightPass = new DrawGlobalLight2DPass(m_RendererData, m_ClearMaterial);
                m_DrawGlobalLightPass.Setup(layerBatch, m_GBuffers, m_DepthTextureHandle);
                EnqueuePass(m_DrawGlobalLightPass);

                var m_DrawLightPass = new DrawLight2DPass(m_RendererData);
                m_DrawLightPass.Setup(layerBatch, m_GBuffers, m_DepthTextureHandle, m_NormalAttachmentHandle);
                EnqueuePass(m_DrawLightPass);

                var m_DrawObjectPass = new DrawRenderer2DPass(m_RendererData);
                m_DrawObjectPass.Setup(layerBatch, m_GBuffers);
                m_DrawObjectPass.ConfigureTarget(m_ColorAttachmentHandle, m_DepthAttachmentHandle);
                EnqueuePass(m_DrawObjectPass);
            }

            m_FinalBlitPass.Setup(cameraTargetDescriptor, m_ColorAttachmentHandle);
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
            if (m_GBuffers != null)
            {
                foreach(var gb in m_GBuffers)
                    gb?.Release();
            }

            m_ColorAttachmentHandle?.Release();
            m_DepthAttachmentHandle?.Release();
            m_DepthTextureHandle?.Release();
            m_NormalAttachmentHandle?.Release();
        }

        public Renderer2DData rendererData => m_RendererData;
    }
}
