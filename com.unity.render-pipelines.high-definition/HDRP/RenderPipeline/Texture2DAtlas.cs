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

            public AtlasNode Allocate(int width, int height)
            {
                // not a leaf node, try children
                if (m_RightChild != null)
                {
                    AtlasNode node = m_RightChild.Allocate(width, height);
                    if (node == null)
                    {
                        node = m_BottomChild.Allocate(width, height);
                    }
                    return node;
                }

                //leaf node, check for fit
                if ((width <= m_Rect.x) && (height <= m_Rect.y))
                {
                    // perform the split
                    m_RightChild = new AtlasNode();
                    m_BottomChild = new AtlasNode();

                    if (width > height) // logic to decide which way to split
                    {                                                           //  +--------+------+
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

        public AtlasAllocator(int width, int height)
        {
            m_Root = new AtlasNode();
            m_Root.m_Rect.Set(width, height, 0, 0);
            m_Width = width;
            m_Height = height;
        }

        public bool Allocate(ref Vector4 result, int width, int height)
        {
            AtlasNode node = m_Root.Allocate(width, height);
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
        private RTHandleSystem.RTHandle m_AtlasTexture = null;
        private int m_Width;
        private int m_Height;
        private RenderTextureFormat m_Format;
        private AtlasAllocator m_AtlasAllocator = null;
        private Dictionary<IntPtr, Vector4> m_AllocationCache = new Dictionary<IntPtr, Vector4>();
        private Dictionary<IntPtr, uint> m_CustomRenderTextureUpdateCache = new Dictionary<IntPtr, uint>();

        public RTHandleSystem.RTHandle AtlasTexture
        {
            get
            {
                return m_AtlasTexture;
            }
        }

        public Texture2DAtlas(int width, int height, RenderTextureFormat format, bool generateMipMaps = false, FilterMode filterMode = FilterMode.Point)
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
                    generateMipMaps);

            m_AtlasAllocator = new AtlasAllocator(width, height);
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

        void Blit2DTexture(CommandBuffer cmd, Vector4 scaleBias, Texture texture, int mipCount)
        {
            for (int mipLevel = 0; mipLevel < mipCount; mipLevel++)
            {
                cmd.SetRenderTarget(m_AtlasTexture, mipLevel);
                HDUtils.BlitQuad(cmd, texture, new Vector4(1, 1, 0, 0), scaleBias, mipLevel, false);
            }
        }

        void BlitCubemap(CommandBuffer cmd, Vector4 scaleBias, Texture texture, int mipCount)
        {
            scaleBias.x /= 3;
            scaleBias.y /= 2;
            for (int mipLevel = 0; mipLevel < mipCount; mipLevel++)
            {
                cmd.SetRenderTarget(m_AtlasTexture, mipLevel);
                HDUtils.BlitCube(cmd, texture, new Vector4(1, 1, 0, 0), scaleBias, mipLevel, false);
            }
        }

        void UpdateCustomRenderTexture(CommandBuffer cmd, Vector4 scaleBias, CustomRenderTexture crt)
        {
            // Compute the number of mipmaps using the size of the texture: mipCout = 1 + floot(log2(maxSize))
            // TODO: maybe too much mipmaps ?
            float   maxSize = Mathf.Max(crt.width, crt.height, crt.depth);
            int     mipCount = 1 + Mathf.FloorToInt(Mathf.Log(maxSize) / Mathf.Log(2));

            if (crt.dimension == TextureDimension.Tex2D)
                Blit2DTexture(cmd, scaleBias, crt, mipCount);
            else if (crt.dimension == TextureDimension.Cube)
                BlitCubemap(cmd, scaleBias, crt, mipCount);
        }

        public bool AddTexture(CommandBuffer cmd, ref Vector4 scaleBias, Texture texture)
        {
            Texture2D           tex2D = texture as Texture2D;
            Cubemap             cubemap = texture as Cubemap;
            CustomRenderTexture crt = texture as CustomRenderTexture;
            IntPtr              key = texture.GetNativeTexturePtr();
            bool                cached = false;
            
            if (m_AllocationCache.TryGetValue(key, out scaleBias))
                cached = true;

            // Update the custom render texture if needed
            if (crt != null && cached)
            {
                uint updateCount;
                if (m_CustomRenderTextureUpdateCache.TryGetValue(key, out updateCount))
                {
                    if (crt.updateCount != updateCount)
                        UpdateCustomRenderTexture(cmd, scaleBias, crt);
                    m_CustomRenderTextureUpdateCache[key] = crt.updateCount;
                }
            }
            
            // If the texture is already in the atlas, return else we add it
            if (cached)
                return true;
            
            int                 mipCount = 1;
            int                 width = texture.width;
            int                 height = texture.height;
            bool                cube = false;

            if (tex2D != null)
                mipCount = tex2D.mipmapCount;
            else if (cubemap != null)
            {
                cube = true;
                mipCount = cubemap.mipmapCount;
            }
            else if (crt != null)
            {
                mipCount = 1 + Mathf.FloorToInt(Mathf.Log(Mathf.Max(crt.width, crt.height)) / Mathf.Log(2));
                cube = crt.dimension == TextureDimension.Cube;
                m_CustomRenderTextureUpdateCache[key] = crt.updateCount;
            }
            else // Unknown texture type
            {
                return false;
            }

            if (cube)
            {
                // For the cubemap, faces are organized like this:
                // +-----+
                // |3|4|5|
                // +-----+
                // |0|1|2|
                // +-----+
                width *= 3;
                height *= 2;
            }
            
            if (m_AtlasAllocator.Allocate(ref scaleBias, width, height))
            {
                scaleBias.Scale(new Vector4(1.0f / m_Width, 1.0f / m_Height, 1.0f / m_Width, 1.0f / m_Height));
                if (cube)
                    BlitCubemap(cmd, scaleBias, texture, mipCount);
                else
                    Blit2DTexture(cmd, scaleBias, texture, mipCount);
                m_AllocationCache.Add(key, scaleBias);
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
