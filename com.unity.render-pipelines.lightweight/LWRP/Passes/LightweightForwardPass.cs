using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public abstract class LightweightForwardPass : ScriptableRenderPass
    {
        private RenderTargetHandle colorAttachmentHandle { get; set; }
        private RenderTargetHandle depthAttachmentHandle { get; set; }
        private RenderTextureDescriptor descriptor { get; set; }
        private Material errorMaterial { get; set; }
        protected ClearFlag clearFlag { get; set; }
        protected Color clearColor { get; set; }

        const string k_SwitchRTs = "Switch RT";


        List<ShaderPassName> m_LegacyShaderPassNames;
        protected RendererConfiguration rendererConfiguration;
        protected bool dynamicBatching;

        protected LightweightForwardPass(Material errorMaterial)
        {
            m_LegacyShaderPassNames = new List<ShaderPassName>();
            m_LegacyShaderPassNames.Add(new ShaderPassName("Always"));
            m_LegacyShaderPassNames.Add(new ShaderPassName("ForwardBase"));
            m_LegacyShaderPassNames.Add(new ShaderPassName("PrepassBase"));
            m_LegacyShaderPassNames.Add(new ShaderPassName("Vertex"));
            m_LegacyShaderPassNames.Add(new ShaderPassName("VertexLMRGBM"));
            m_LegacyShaderPassNames.Add(new ShaderPassName("VertexLM"));

            this.errorMaterial = errorMaterial;

            RegisterShaderPassName("LightweightForward");
            RegisterShaderPassName("SRPDefaultUnlit");
        }

        public void Setup(
            RenderTextureDescriptor baseDescriptor,
            RenderTargetHandle colorAttachmentHandle,
            RenderTargetHandle depthAttachmentHandle,
            ClearFlag clearFlag,
            Color clearColor,
            RendererConfiguration configuration,
            bool dynamicbatching)
        {
            this.colorAttachmentHandle = colorAttachmentHandle;
            this.depthAttachmentHandle = depthAttachmentHandle;
            this.clearColor = clearColor;
            this.clearFlag = clearFlag;
            descriptor = baseDescriptor;
            this.rendererConfiguration = configuration;
            this.dynamicBatching = dynamicbatching;
        }

        protected void SetRenderTarget(CommandBuffer cmd, RenderBufferLoadAction loadOp, RenderBufferStoreAction storeOp, ClearFlag clearFlag, Color clearColor)
        {
            if (colorAttachmentHandle != RenderTargetHandle.CameraTarget)
            {
                if (depthAttachmentHandle != RenderTargetHandle.CameraTarget)
                    SetRenderTarget(
                        cmd,
                        colorAttachmentHandle.Identifier(),
                        loadOp,
                        storeOp,
                        depthAttachmentHandle.Identifier(),
                        loadOp,
                        storeOp,
                        clearFlag,
                        clearColor,
                        descriptor.dimension);
                else
                    SetRenderTarget(cmd, colorAttachmentHandle.Identifier(), loadOp, storeOp, clearFlag, clearColor, descriptor.dimension);
            }
            else
            {
                SetRenderTarget(cmd, BuiltinRenderTextureType.CameraTarget, loadOp, storeOp, clearFlag, clearColor, descriptor.dimension);
            }
        }

        [Conditional("DEVELOPMENT_BUILD"), Conditional("UNITY_EDITOR")]
        protected void RenderObjectsWithError(ref ScriptableRenderContext context, ref CullResults cullResults, Camera camera, FilterRenderersSettings filterSettings, SortFlags sortFlags)
        {
            if (errorMaterial != null)
            {
                DrawRendererSettings errorSettings = new DrawRendererSettings(camera, m_LegacyShaderPassNames[0]);
                for (int i = 1; i < m_LegacyShaderPassNames.Count; ++i)
                    errorSettings.SetShaderPassName(i, m_LegacyShaderPassNames[i]);

                errorSettings.sorting.flags = sortFlags;
                errorSettings.rendererConfiguration = RendererConfiguration.None;
                errorSettings.SetOverrideMaterial(errorMaterial, 0);
                context.DrawRenderers(cullResults.visibleRenderers, ref errorSettings, filterSettings);
            }
        }
    }
}
