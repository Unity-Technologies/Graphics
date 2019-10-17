using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class AtlasAllocatorDynamic
    {
        // Warning: AtlasNode will create GC.Allocs as it is a reference type.
        // Previously AtlasAllocator rarely freed nodes, so the GC was infrequent.
        // Now that we have added methods to free and reallocate sections of the atlas on the fly, we need to consider
        // how to avoid creating garbage.
        //
        // TODO: Pool AtlasNode(s) -  one idea is to switch to struct type and pre-allocate in flat array:
        // struct AtlasNode
        // {
        //     int m_Parent;
        //     int m_LeftChild;
        //     int m_RightChild;
        //     int m_Padding;
        //     Vector4 m_Rect;
        // }

        // AtlasNode[] atlasNodes = new AtlasNode[MAX_CAPACITY];
        // int atlasNodeNext = 0;
        // int atlasNodeFreelist = -1;

        private class AtlasNode
        {
            public AtlasNode m_Parent;
            public AtlasNode m_LeftChild;
            public AtlasNode m_RightChild;
            public bool isOccupied;
            public Vector4 m_Rect;

            public AtlasNode(AtlasNode parent)
            {
                m_Parent = parent;
                m_LeftChild = null;
                m_RightChild = null;
                isOccupied = false;
                m_Rect = new Vector4(0, 0, 0, 0); // x,y is width and height (scale) z,w offset into atlas (bias)
            }

            protected bool IsLeafNode()
            {
                // Note: Only need to check if m_LeftChild == null, as either both are allocated (split), or none are allocated (leaf).
                return (m_LeftChild == null) && (m_RightChild == null);
            }

            public AtlasNode Allocate(int width, int height)
            {
                if (Mathf.Min(width, height) < 1)
                {
                    // Degenerate allocation requested.
                    Debug.Assert(false, "Error: Texture2DAtlasDynamic: Attempted to allocate a degenerate region. Please ensure width and height are >= 1");
                    return null;
                }

                // not a leaf node, try children
                // TODO: Rather than always going left, then right, we might want to always attempt to allocate in the smaller child, then larger.
                if (!IsLeafNode())
                {
                    AtlasNode node = null;
                    if (m_LeftChild != null)
                    {
                        node = m_LeftChild.Allocate(width, height);
                    }
                    if (node == null && m_RightChild != null)
                    {
                        node = m_RightChild.Allocate(width, height);
                    }
                    return node;
                }

                if (isOccupied) { return null; }

                // leaf node, check for fit
                if ((width <= m_Rect.x) && (height <= m_Rect.y))
                {
                    // perform the split
                    m_LeftChild = new AtlasNode(this);
                    m_RightChild = new AtlasNode(this);

                    float deltaX = m_Rect.x - width;
                    float deltaY = m_Rect.y - height;
                    // Debug.Log("deltaX = " + deltaX);
                    // Debug.Log("deltaY = " + deltaY);

                    if (deltaX >= deltaY)
                    {
                        // Debug.Log("Split horizontally");
                        //  +--------+------+
                        //  |        |      |
                        //  |        |      |
                        //  |        |      |
                        //  |        |      |
                        //  +--------+------+
                        m_LeftChild.m_Rect.x = width;
                        m_LeftChild.m_Rect.y = m_Rect.y;
                        m_LeftChild.m_Rect.z = m_Rect.z;
                        m_LeftChild.m_Rect.w = m_Rect.w;

                        m_RightChild.m_Rect.x = deltaX;
                        m_RightChild.m_Rect.y = m_Rect.y;
                        m_RightChild.m_Rect.z = m_Rect.z + width;
                        m_RightChild.m_Rect.w = m_Rect.w;

                        if (deltaY < 1)
                        {
                            m_LeftChild.isOccupied = true;
                            return m_LeftChild;
                        }
                        else
                        {
                            AtlasNode node = m_LeftChild.Allocate(width, height);
                            if (node != null) { node.isOccupied = true; }
                            return node;
                        }
                    }
                    else
                    {
                        // Debug.Log("Split vertically.");
                        //  +---------------+
                        //  |               |
                        //  |---------------|
                        //  |               |
                        //  |               |
                        //  +---------------+
                        m_LeftChild.m_Rect.x = m_Rect.x;
                        m_LeftChild.m_Rect.y = height;
                        m_LeftChild.m_Rect.z = m_Rect.z;
                        m_LeftChild.m_Rect.w = m_Rect.w;

                        m_RightChild.m_Rect.x = m_Rect.x;
                        m_RightChild.m_Rect.y = deltaY;
                        m_RightChild.m_Rect.z = m_Rect.z;
                        m_RightChild.m_Rect.w = m_Rect.w + height;

                        if (deltaX < 1)
                        {
                            m_LeftChild.isOccupied = false;
                            return m_LeftChild;
                        }
                        else
                        {
                            AtlasNode node = m_LeftChild.Allocate(width, height);
                            if (node != null) { node.isOccupied = true; }
                            return node;
                        }
                    }
                }
                return null;
            }

            public void Release()
            {
                if (m_LeftChild != null)
                {
                    m_LeftChild.Release();
                    m_RightChild.Release();
                }
                m_LeftChild = null;
                m_RightChild = null;
            }

            public void ReleaseAndMerge()
            {
                AtlasNode n = this;
                do
                {
                    n.Release();
                    n.isOccupied = false;
                    n = n.m_Parent;
                }
                while (n != null && n.IsMergeNeeded());
            }

            protected bool IsMergeNeeded()
            {
                return m_LeftChild.IsLeafNode() && (!m_LeftChild.isOccupied)
                    && m_RightChild.IsLeafNode() && (!m_RightChild.isOccupied);
            }
        }

        private AtlasNode m_Root;
        private int m_Width;
        private int m_Height;
        private Dictionary<int, AtlasNode> m_NodeFromID = new Dictionary<int, AtlasNode>();

        public AtlasAllocatorDynamic(int width, int height)
        {
            m_Root = new AtlasNode(null);
            m_Root.m_Rect.Set(width, height, 0, 0);
            m_Width = width;
            m_Height = height;

            // string debug = "";
            // DebugStringFromNode(ref debug, m_Root);
            // Debug.Log("Allocating atlas = " + debug);
        }

        public bool Allocate(ref Vector4 result, int key, int width, int height)
        {
            AtlasNode node = m_Root.Allocate(width, height);
            if (node != null)
            {
                result = node.m_Rect;
                m_NodeFromID.Add(key, node);
                return true;
            }
            else
            {
                result = Vector4.zero;
                return false;
            }
        }

        public void Release(int key)
        {
            if (m_NodeFromID.TryGetValue(key, out AtlasNode node))
            {
                node.ReleaseAndMerge();
                m_NodeFromID.Remove(key);
                return;
            }
        }

        public void Release()
        {
            m_Root.Release();
            m_Root = new AtlasNode(null);
            m_Root.m_Rect.Set(m_Width, m_Height, 0, 0);
            m_NodeFromID.Clear();
        }

        public string DebugStringFromRoot(int depthMax = -1)
        {
            string res = "";
            DebugStringFromNode(ref res, m_Root, 0, depthMax);
            return res;
        }

        private void DebugStringFromNode(ref string res, AtlasNode n, int depthCurrent = 0, int depthMax = -1)
        {
            res += "{[" + depthCurrent + "], isOccupied = " + (n.isOccupied ? "true" : "false") + ","  + n.m_Rect.x + "," + n.m_Rect.y + ", " + n.m_Rect.z + ", " + n.m_Rect.w + "}\n";

            if (depthMax == -1 || depthCurrent < depthMax)
            {
                if (n.m_LeftChild != null)
                {
                    DebugStringFromNode(ref res, n.m_LeftChild, depthCurrent + 1, depthMax);
                }

                if (n.m_RightChild != null)
                {
                    DebugStringFromNode(ref res, n.m_RightChild, depthCurrent + 1, depthMax);
                }
            }
        }
    }

    class Texture2DAtlasDynamic
    {
        private RTHandle m_AtlasTexture = null;
        private bool isAtlasTextureOwner = false;
        private int m_Width;
        private int m_Height;
        private GraphicsFormat m_Format;
        private AtlasAllocatorDynamic m_AtlasAllocator = null;
        private Dictionary<int, Vector4> m_AllocationCache = new Dictionary<int, Vector4>();

        public RTHandle AtlasTexture
        {
            get
            {
                return m_AtlasTexture;
            }
        }

        public Texture2DAtlasDynamic(int width, int height, GraphicsFormat format)
        {
            m_Width = width;
            m_Height = height;
            m_Format = format;
            m_AtlasTexture = RTHandles.Alloc(m_Width,
                    m_Height,
                    1,
                    DepthBits.None,
                    m_Format,
                    FilterMode.Point,
                    TextureWrapMode.Clamp,
                    TextureDimension.Tex2D,
                    false,
                    true,
                    false,
                    false,
                    1,
                    0,
                    MSAASamples.None,
                    false,
                    false);
            isAtlasTextureOwner = true;

            m_AtlasAllocator = new AtlasAllocatorDynamic(width, height);
        }

        public Texture2DAtlasDynamic(int width, int height, RTHandle atlasTexture)
        {
            m_Width = width;
            m_Height = height;
            m_Format = atlasTexture.rt.graphicsFormat;
            m_AtlasTexture = atlasTexture;
            isAtlasTextureOwner = false;

            m_AtlasAllocator = new AtlasAllocatorDynamic(width, height);
        }

        public void Release()
        {
            ResetAllocator();
            if (isAtlasTextureOwner) { RTHandles.Release(m_AtlasTexture); }
        }

        public void ResetAllocator()
        {
            m_AtlasAllocator.Release();
            m_AllocationCache.Clear();
        }

        public bool AddTexture(CommandBuffer cmd, ref Vector4 scaleBias, Texture texture)
        {
            int key = texture.GetInstanceID();
            if (!m_AllocationCache.TryGetValue(key, out scaleBias))
            {
                int width = texture.width;
                int height = texture.height;
                if (m_AtlasAllocator.Allocate(ref scaleBias, key, width, height))
                {
                    scaleBias.Scale(new Vector4(1.0f / m_Width, 1.0f / m_Height, 1.0f / m_Width, 1.0f / m_Height));
                    for (int mipLevel = 0; mipLevel < (texture as Texture2D).mipmapCount; mipLevel++)
                    {
                        cmd.SetRenderTarget(m_AtlasTexture, mipLevel);
                        HDUtils.BlitQuad(cmd, texture, new Vector4(1, 1, 0, 0), scaleBias, mipLevel, false);
                    }
                    m_AllocationCache.Add(key, scaleBias);
                    return true;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        public bool EnsureTextureSlot(out bool isUploadNeeded, ref Vector4 scaleBias, int key, int width, int height)
        {
            isUploadNeeded = false;
            if (m_AllocationCache.TryGetValue(key, out scaleBias)) { return true; }

            // Debug.Log("EnsureTextureSlot Before = " + m_AtlasAllocator.DebugStringFromRoot());
            if (!m_AtlasAllocator.Allocate(ref scaleBias, key, width, height)) { return false; }
            // Debug.Log("EnsureTextureSlot After = " + m_AtlasAllocator.DebugStringFromRoot());

            isUploadNeeded = true;
            scaleBias.Scale(new Vector4(1.0f / m_Width, 1.0f / m_Height, 1.0f / m_Width, 1.0f / m_Height));
            m_AllocationCache.Add(key, scaleBias);
            return true;
        }

        public void ReleaseTextureSlot(int key)
        {
            m_AtlasAllocator.Release(key);
            m_AllocationCache.Remove(key);
        }

        public string DebugStringFromRoot(int depthMax = -1)
        {
            return m_AtlasAllocator.DebugStringFromRoot(depthMax);
        }
    }
}
