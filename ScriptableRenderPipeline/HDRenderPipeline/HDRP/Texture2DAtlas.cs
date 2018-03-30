using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering
{
    public class Texture2DAtlas
    {
        private RTHandle m_AtlasTexture = null;
        private int m_Width;
        private int m_Height;
        private RenderTextureFormat m_Format;

        public Texture2DAtlas(int width, int height, RenderTextureFormat format)
        {
            m_Width = width;
            m_Height = height;
            m_Format = format;
            m_AtlasTexture = RTHandle.Alloc(m_Width,
                m_Height,
                1,
                DepthBits.None,
                RenderTextureFormat.ARGB32,
                FilterMode.Point,
                TextureWrapMode.Clamp,
                TextureDimension.Tex2D,
                false,
                false,
                true,
                false);
        }

        public void Release()
        {
            RTHandle.Release(m_AtlasTexture);
        }

        public void AddTexture(CommandBuffer cmd, Texture texture)
        {
            float scaleW = (float)texture.width / m_Width;
            float scaleH = (float) texture.height / m_Height;
            HDUtils.BlitTexture(cmd, texture, m_AtlasTexture, new Vector4(1,1,0,0), new Vector4(scaleW, scaleH, 0, 0), 0, 0, false);
        }
    }
}
