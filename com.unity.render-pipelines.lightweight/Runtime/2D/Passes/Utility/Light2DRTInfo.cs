using System;
using UnityEngine;


namespace UnityEngine.Experimental.Rendering.LWRP
{
    [Serializable]
    public class Light2DRTInfo
    {
        const int k_DefaultPixelWidth = 1;
        const int k_DefaultPixelHeight = 1;

        public bool m_UseRenderTexture;
        public int m_PixelWidth;
        public int m_PixelHeight;
        public FilterMode m_FilterMode;

        public RenderTexture GetRenderTexture(RenderTextureFormat format)
        {
            int width = m_PixelWidth > 0 ? m_PixelWidth : k_DefaultPixelWidth;
            int height = m_PixelHeight > 0 ? m_PixelHeight : k_DefaultPixelHeight;

            RenderTextureDescriptor renderTextureDescriptor = new RenderTextureDescriptor(width, height, format);
            renderTextureDescriptor.sRGB = false;
            renderTextureDescriptor.useMipMap = false;
            renderTextureDescriptor.autoGenerateMips = false;
            renderTextureDescriptor.depthBufferBits = 0;

            RenderTexture retTexture = RenderTexture.GetTemporary(renderTextureDescriptor);
            retTexture.wrapMode = TextureWrapMode.Clamp;
            retTexture.filterMode = m_FilterMode;

            retTexture.DiscardContents(true, true);

            return retTexture;
        }


        public Light2DRTInfo(bool useRenderTexture, int pixelWidth, int pixelHeight, FilterMode filterMode)
        {
            m_UseRenderTexture = useRenderTexture;
            m_PixelWidth = pixelWidth;
            m_PixelHeight = pixelHeight;
            m_FilterMode = filterMode;
        }
    }
}
