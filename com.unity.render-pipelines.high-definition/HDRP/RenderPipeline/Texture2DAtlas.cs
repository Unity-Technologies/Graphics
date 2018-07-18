using System;
using System.Collections.Generic;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering
{
    public class AtlasAllocator
    {
        private class AtlasNode
        {
            public AtlasNode m_RightChild = null;
            public AtlasNode m_BottomChild = null;
            public Vector4 m_Rect = new Vector4(0, 0, 0, 0); // x,y is width and height (scale) z,w offset into atlas (bias)

            public AtlasNode Allocate(int width, int height, bool powerOfTwoPadding)
            {
                // not a leaf node, try children
                if (m_RightChild != null)
                {
                    AtlasNode node = m_RightChild.Allocate(width, height, powerOfTwoPadding);
                    if (node == null)
                    {
                        node = m_BottomChild.Allocate(width, height, powerOfTwoPadding);
                    }
                    return node;
                }
                
                int wPadd = 0;
                int hPadd = 0;
                
                if (powerOfTwoPadding)
                {
                    wPadd = (int)m_Rect.z % width;
                    hPadd = (int)m_Rect.w % height;
                }

                //leaf node, check for fit
                if ((width <= m_Rect.x - wPadd) && (height <= m_Rect.y - hPadd))
                {
                    // perform the split
                    m_RightChild = new AtlasNode();
                    m_BottomChild = new AtlasNode();
                    
                    m_Rect.z += wPadd;
                    m_Rect.w += hPadd;
                    m_Rect.x -= wPadd;
                    m_Rect.y -= hPadd;

                    if (width > height) // logic to decide which way to split
                    {
                                                                                //  +--------+------+
                        m_RightChild.m_Rect.z = m_Rect.z + width;               //  |        |      |
                        m_RightChild.m_Rect.w = m_Rect.w;                       //  +--------+------+
                        m_RightChild.m_Rect.x = m_Rect.x - width;               //  |               |
                        m_RightChild.m_Rect.y = height;                         //  |               |
                                                                                //  +---------------+
                        m_BottomChild.m_Rect.z = m_Rect.z;
                        m_BottomChild.m_Rect.w = m_Rect.w + height;
                        m_BottomChild.m_Rect.x = m_Rect.x;
                        m_BottomChild.m_Rect.y = m_Rect.y - height;
                    }
                    else
                    {                                                           //  +---+-----------+
                        m_RightChild.m_Rect.z = m_Rect.z + width;               //  |   |           |
                        m_RightChild.m_Rect.w = m_Rect.w;                       //  |   |           |
                        m_RightChild.m_Rect.x = m_Rect.x - width;               //  +---+           +
                        m_RightChild.m_Rect.y = m_Rect.y;                       //  |   |           |
                                                                                //  +---+-----------+
                        m_BottomChild.m_Rect.z = m_Rect.z;
                        m_BottomChild.m_Rect.w = m_Rect.w + height;
                        m_BottomChild.m_Rect.x = width;
                        m_BottomChild.m_Rect.y = m_Rect.y - height;
                    }
                    m_Rect.x = width;
                    m_Rect.y = height;
                    return this;
                }
                return null;
            }

            public void Release()
            {
                if (m_RightChild != null)
                {
                    m_RightChild.Release();
                    m_BottomChild.Release();
                }
                m_RightChild = null;
                m_BottomChild = null;
            }
        }

        private AtlasNode m_Root;
        private int m_Width;
        private int m_Height;
        private bool powerOfTwoPadding;

        public AtlasAllocator(int width, int height, bool potPadding)
        {
            m_Root = new AtlasNode();
            m_Root.m_Rect.Set(width, height, 0, 0);
            m_Width = width;
            m_Height = height;
            powerOfTwoPadding = potPadding;
        }

        public bool Allocate(ref Vector4 result, int width, int height)
        {
            AtlasNode node = m_Root.Allocate(width, height, powerOfTwoPadding);
            if (node != null)
            {
                result = node.m_Rect;
                return true;
            }
            else
            {
                result = Vector4.zero;
                return false;
            }
        }

        public void Release()
        {
            m_Root.Release();
            m_Root = new AtlasNode();
            m_Root.m_Rect.Set(m_Width, m_Height, 0, 0);
        }
    }

    public class Texture2DAtlas
    {
        protected RTHandleSystem.RTHandle m_AtlasTexture = null;
        protected int m_Width;
        protected int m_Height;
        protected RenderTextureFormat m_Format;
        protected AtlasAllocator m_AtlasAllocator = null;
        protected Dictionary<IntPtr, Vector4> m_AllocationCache = new Dictionary<IntPtr, Vector4>();
        protected Dictionary<IntPtr, uint> m_CustomRenderTextureUpdateCache = new Dictionary<IntPtr, uint>();

        public RTHandleSystem.RTHandle AtlasTexture
        {
            get
            {
                return m_AtlasTexture;
            }
        }

        public Texture2DAtlas(int width, int height, RenderTextureFormat format, bool generateMipMaps = false, FilterMode filterMode = FilterMode.Point, bool powerOfTwoPadding = false)
        {
            m_Width = width;
            m_Height = height;
            m_Format = format;
            m_AtlasTexture = RTHandles.Alloc(m_Width,
                    m_Height,
                    1,
                    DepthBits.None,
                    m_Format,
                    filterMode,
                    TextureWrapMode.Clamp,
                    TextureDimension.Tex2D,
                    false,
                    false,
                    true,
                    generateMipMaps,
                    name: "Texture2DAtlas");

            m_AtlasAllocator = new AtlasAllocator(width, height, powerOfTwoPadding);
        }

        public void Release()
        {
            ResetAllocator();
            RTHandles.Release(m_AtlasTexture);
        }

        public void ResetAllocator()
        {
            m_AtlasAllocator.Release();
            m_AllocationCache.Clear();
        }

        protected int GetTextureMipmapCount(Texture texture)
        {
            // We don't care about the real mipmap count in the texture because they are generated by the atlas
            float maxSize = Mathf.Max(texture.width, texture.height);
            return Mathf.CeilToInt(Mathf.Log(maxSize) / Mathf.Log(2));
        }

        protected bool Is2D(Texture texture)
        {
            CustomRenderTexture crt = texture as CustomRenderTexture;
            return (texture is Texture2D || (crt != null && crt.dimension == TextureDimension.Tex2D));
        }

        protected void Blit2DTexture(CommandBuffer cmd, Vector4 scaleBias, Texture texture)
        {
            int mipCount = GetTextureMipmapCount(texture);

            for (int mipLevel = 0; mipLevel < mipCount; mipLevel++)
            {
                cmd.SetRenderTarget(m_AtlasTexture, mipLevel);
                HDUtils.BlitQuad(cmd, texture, new Vector4(1, 1, 0, 0), scaleBias, mipLevel, true);
            }
        }

        protected virtual void BlitTexture(CommandBuffer cmd, Vector4 scaleBias, Texture texture)
        {
            // This atlas only support 2D texture so we only blit 2D textures
            if (Is2D(texture))
                Blit2DTexture(cmd, scaleBias, texture);
        }

        protected virtual bool AllocateTexture(CommandBuffer cmd, ref Vector4 scaleBias, Texture texture, int width, int height)
        {
            if (m_AtlasAllocator.Allocate(ref scaleBias, width, height))
            {
                scaleBias.Scale(new Vector4(1.0f / m_Width, 1.0f / m_Height, 1.0f / m_Width, 1.0f / m_Height));
                BlitTexture(cmd, scaleBias, texture);
                m_AllocationCache.Add(texture.GetNativeTexturePtr(), scaleBias);
                return true;
            }
            else
            {
                return false;
            }
        }

        protected bool IsCached(CommandBuffer cmd, out Vector4 scaleBias, Texture texture)
        {
            bool                cached = false;
            IntPtr              key = texture.GetNativeTexturePtr();
            CustomRenderTexture crt = texture as CustomRenderTexture;

            if (m_AllocationCache.TryGetValue(key, out scaleBias))
                cached = true;

            // Update the custom render texture if needed
            if (crt != null && cached)
            {
                uint updateCount;
                if (m_CustomRenderTextureUpdateCache.TryGetValue(key, out updateCount))
                {
                    if (crt.updateCount != updateCount)
                        BlitTexture(cmd, scaleBias, crt);
                }
                m_CustomRenderTextureUpdateCache[key] = crt.updateCount;
            }

            return cached;
        }

        public virtual bool AddTexture(CommandBuffer cmd, ref Vector4 scaleBias, Texture texture)
        {
            if (IsCached(cmd, out scaleBias, texture))
                return true;
            
            // We only support 2D texture in this class, support for other textures are provided by child classes (ex: PowerOfTwoTextureAtlas)
            if (!Is2D(texture))
                return false;

            return AllocateTexture(cmd, ref scaleBias, texture, texture.width, texture.height);
        }
    }
}
