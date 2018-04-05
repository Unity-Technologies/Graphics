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
            public Rect m_Rect = new Rect(0,0,0,0);


            public AtlasNode Allocate(int width, int height)
            {
                // not a leaf node, try children
                if(m_RightChild != null)
                {
                    AtlasNode node = m_RightChild.Allocate(width, height);
                    if(node == null)
                    {
                        node = m_BottomChild.Allocate(width, height);
                    }
                    return node;
                }

                //leaf node, check for fit
                if ((width <= m_Rect.width) && (height <= m_Rect.height))
                {
                    // perform the split
                    m_RightChild = new AtlasNode();
                    m_BottomChild = new AtlasNode();

                    if (width > height) // logic to decide which way to split               +--------+------+
                    {                                                                   //  |        |      |
                        m_RightChild.m_Rect.x = m_Rect.x + width;                       //  |        |      |
                        m_RightChild.m_Rect.y = m_Rect.y;                               //  +--------+------+
                        m_RightChild.m_Rect.width = m_Rect.width - width;               //  |               |
                        m_RightChild.m_Rect.height = height;                            //  |               |
                                                                                        //  +---------------+        
                        m_BottomChild.m_Rect.x = m_Rect.x;
                        m_BottomChild.m_Rect.y = m_Rect.y + height;
                        m_BottomChild.m_Rect.width = m_Rect.width;
                        m_BottomChild.m_Rect.height = m_Rect.height - height;
                    }
                    else
                    {                                                                   //  +---+-----------+  
                        m_RightChild.m_Rect.x = m_Rect.x + width;                       //  |   |           |
                        m_RightChild.m_Rect.y = m_Rect.y;                               //  |   |           |
                        m_RightChild.m_Rect.width = m_Rect.width - width;               //  +---+           +
                        m_RightChild.m_Rect.height = m_Rect.height;                     //  |   |           |
                                                                                        //  |   |           |
                        m_BottomChild.m_Rect.x = m_Rect.x;                              //  +---+-----------+        
                        m_BottomChild.m_Rect.y = m_Rect.y + height;
                        m_BottomChild.m_Rect.width = m_Rect.width;
                        m_BottomChild.m_Rect.height = m_Rect.height - height;
                    }
                    m_Rect.width = width;
                    m_Rect.height = height;
                    return this;
                }
                return null;
            }
        }

        private AtlasNode m_Root;

        public AtlasAllocator(int width, int height)
        {
            m_Root = new AtlasNode();
            m_Root.m_Rect.Set(0, 0, width, height);
        }

        public Rect Allocate(int width, int height)
        {
            AtlasNode node = m_Root.Allocate(width, height);
            if(node != null)
            { 
                return node.m_Rect;
            }
            else
            {
                return new Rect(0, 0, 0, 0);
            }
        }
    }


    public class Texture2DAtlas
    {
        private RTHandle m_AtlasTexture = null;
        private int m_Width;
        private int m_Height;
        private RenderTextureFormat m_Format;

        
        public RTHandle AtlasTexture
        {
            get
            {
                return m_AtlasTexture;
            }
        }

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

        public Vector4 AddTexture(CommandBuffer cmd, Texture texture)
        {
            float scaleW = (float)texture.width / m_Width;
            float scaleH = (float) texture.height / m_Height;
            Vector4 scaleBias = new Vector4(scaleW, scaleH, 0, 0);
            for (int mipLevel = 0; mipLevel < (texture as Texture2D).mipmapCount; mipLevel++)
            {
                cmd.SetRenderTarget(m_AtlasTexture, mipLevel);
                HDUtils.BlitQuad(cmd, texture, new Vector4(1, 1, 0, 0), scaleBias, mipLevel, false);
            }
            return scaleBias;
        }
    }
}
