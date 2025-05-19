using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.RenderGraphModule
{
    /// <summary>
    /// Helper class allowing access to default resources (black or white texture, etc.) during render passes.
    /// </summary>
    [MovedFrom(true, "UnityEngine.Experimental.Rendering.RenderGraphModule", "UnityEngine.Rendering.RenderGraphModule")]
    public class RenderGraphDefaultResources
    {
        // We need to keep around a RTHandle version of default regular 2D textures since RenderGraph API is all RTHandle.
        RTHandle m_BlackTexture2D;
        RTHandle m_WhiteTexture2D;
        RTHandle m_ShadowTexture2D;

        /// <summary>Default black 2D texture.</summary>
        public TextureHandle blackTexture { get; private set; }
        /// <summary>Default white 2D texture.</summary>
        public TextureHandle whiteTexture { get; private set; }
        /// <summary>Default clear color XR 2D texture.</summary>
        public TextureHandle clearTextureXR { get; private set; }
        /// <summary>Default magenta XR 2D texture.</summary>
        public TextureHandle magentaTextureXR { get; private set; }
        /// <summary>Default black XR 2D texture.</summary>
        public TextureHandle blackTextureXR { get; private set; }
        /// <summary>Default black XR 2D Array texture.</summary>
        public TextureHandle blackTextureArrayXR { get; private set; }
        /// <summary>Default black (UInt) XR 2D texture.</summary>
        public TextureHandle blackUIntTextureXR { get; private set; }
        /// <summary>Default black XR 3D texture.</summary>
        public TextureHandle blackTexture3DXR { get; private set; }
        /// <summary>Default white XR 2D texture.</summary>
        public TextureHandle whiteTextureXR { get; private set; }
        /// <summary>Default 1x1 shadow texture.</summary>
        public TextureHandle defaultShadowTexture { get; private set; }

        internal RenderGraphDefaultResources()
        {
            InitDefaultResourcesIfNeeded();
        }

        private void InitDefaultResourcesIfNeeded()
        {
            if (m_BlackTexture2D == null)
                m_BlackTexture2D = RTHandles.Alloc(Texture2D.blackTexture);

            if (m_WhiteTexture2D == null)
                m_WhiteTexture2D = RTHandles.Alloc(Texture2D.whiteTexture);

            if (m_ShadowTexture2D == null)
                m_ShadowTexture2D = RTHandles.Alloc(1, 1, Experimental.Rendering.GraphicsFormat.D32_SFloat, isShadowMap: true, name: "DefaultShadowTexture");
        }

        internal void Cleanup()
        {
            m_BlackTexture2D?.Release();
            m_BlackTexture2D = null;

            m_WhiteTexture2D?.Release();
            m_WhiteTexture2D = null;

            m_ShadowTexture2D?.Release();
            m_ShadowTexture2D = null;
        }

        internal void InitializeForRendering(RenderGraph renderGraph)
        {
            InitDefaultResourcesIfNeeded();

            blackTexture = renderGraph.ImportTexture(m_BlackTexture2D, true);
            whiteTexture = renderGraph.ImportTexture(m_WhiteTexture2D, true);
            defaultShadowTexture = renderGraph.ImportTexture(m_ShadowTexture2D, true);

            clearTextureXR = renderGraph.ImportTexture(TextureXR.GetClearTexture(), true);
            magentaTextureXR = renderGraph.ImportTexture(TextureXR.GetMagentaTexture(), true);
            blackTextureXR = renderGraph.ImportTexture(TextureXR.GetBlackTexture(), true);
            blackTextureArrayXR = renderGraph.ImportTexture(TextureXR.GetBlackTextureArray(), true);
            blackUIntTextureXR = renderGraph.ImportTexture(TextureXR.GetBlackUIntTexture(), true);
            blackTexture3DXR = renderGraph.ImportTexture(TextureXR.GetBlackTexture3D(), true);
            whiteTextureXR = renderGraph.ImportTexture(TextureXR.GetWhiteTexture(), true);
        }
    }
}
