using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.RenderGraphModule
{
    /// <summary>
    /// Helper class allowing access to default resources (black or white texture, etc.) during render passes.
    /// </summary>
    public class RenderGraphDefaultResources
    {
        bool m_IsValid;

        // We need to keep around a RTHandle version of default regular 2D textures since RenderGraph API is all RTHandle.
        RTHandle m_BlackTexture2D;
        RTHandle m_WhiteTexture2D;

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

        internal RenderGraphDefaultResources()
        {
            m_BlackTexture2D = RTHandles.Alloc(Texture2D.blackTexture);
            m_WhiteTexture2D = RTHandles.Alloc(Texture2D.whiteTexture);
        }

        internal void Cleanup()
        {
            m_BlackTexture2D.Release();
            m_WhiteTexture2D.Release();
        }

        internal void InitializeForRendering(RenderGraph renderGraph)
        {
            blackTexture = renderGraph.ImportTexture(m_BlackTexture2D);
            whiteTexture = renderGraph.ImportTexture(m_WhiteTexture2D);

            clearTextureXR = renderGraph.ImportTexture(TextureXR.GetClearTexture());
            magentaTextureXR = renderGraph.ImportTexture(TextureXR.GetMagentaTexture());
            blackTextureXR = renderGraph.ImportTexture(TextureXR.GetBlackTexture());
            blackTextureArrayXR = renderGraph.ImportTexture(TextureXR.GetBlackTextureArray());
            blackUIntTextureXR = renderGraph.ImportTexture(TextureXR.GetBlackUIntTexture());
            blackTexture3DXR = renderGraph.ImportTexture(TextureXR.GetBlackTexture3D());
            whiteTextureXR = renderGraph.ImportTexture(TextureXR.GetWhiteTexture());
        }

    }
}

