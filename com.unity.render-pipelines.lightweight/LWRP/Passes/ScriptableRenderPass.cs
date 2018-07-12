using System.Collections.Generic;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.LightweightPipeline
{
    public abstract class ScriptableRenderPass
    {
        protected LightweightForwardRenderer renderer { get; }

        private List<ShaderPassName> m_ShaderPassNames = new List<ShaderPassName>();

        protected ScriptableRenderPass(LightweightForwardRenderer renderer)
        {
            this.renderer = renderer;
        }

        public virtual void Dispose(CommandBuffer cmd)
        {}

        public abstract void Execute(ref ScriptableRenderContext context, ref CullResults cullResults, ref RenderingData renderingData);

        protected void RegisterShaderPassName(string passName)
        {
            m_ShaderPassNames.Add(new ShaderPassName(passName));
        }

        protected DrawRendererSettings CreateDrawRendererSettings(Camera camera, SortFlags sortFlags, RendererConfiguration rendererConfiguration, bool supportsDynamicBatching)
        {
            DrawRendererSettings settings = new DrawRendererSettings(camera, m_ShaderPassNames[0]);
            for (int i = 1; i < m_ShaderPassNames.Count; ++i)
                settings.SetShaderPassName(i, m_ShaderPassNames[i]);
            settings.sorting.flags = sortFlags;
            settings.rendererConfiguration = rendererConfiguration;
            settings.flags = DrawRendererFlags.EnableInstancing;
            if (supportsDynamicBatching)
                settings.flags |= DrawRendererFlags.EnableDynamicBatching;
            return settings;
        }

        protected static void SetRenderTarget(
            CommandBuffer cmd,
            RenderTargetIdentifier colorAttachment,
            RenderBufferLoadAction colorLoadAction,
            RenderBufferStoreAction colorStoreAction,
            ClearFlag clearFlag,
            Color clearColor,
            TextureDimension dimension)
        {
            if (dimension == TextureDimension.Tex2DArray)
                CoreUtils.SetRenderTarget(cmd, colorAttachment, clearFlag, clearColor, 0, CubemapFace.Unknown, -1);
            else
                CoreUtils.SetRenderTarget(cmd, colorAttachment, colorLoadAction, colorStoreAction, clearFlag, clearColor);
        }

        protected static void SetRenderTarget(
            CommandBuffer cmd,
            RenderTargetIdentifier colorAttachment,
            RenderBufferLoadAction colorLoadAction,
            RenderBufferStoreAction colorStoreAction,
            RenderTargetIdentifier depthAttachment,
            RenderBufferLoadAction depthLoadAction,
            RenderBufferStoreAction depthStoreAction,
            ClearFlag clearFlag,
            Color clearColor,
            TextureDimension dimension)
        {
            if (dimension == TextureDimension.Tex2DArray)
                CoreUtils.SetRenderTarget(cmd, colorAttachment, depthAttachment,
                    clearFlag, clearColor, 0, CubemapFace.Unknown, -1);
            else
                CoreUtils.SetRenderTarget(cmd, colorAttachment, colorLoadAction, colorStoreAction,
                    depthAttachment, depthLoadAction, depthStoreAction, clearFlag, clearColor);
        }
    }
}
