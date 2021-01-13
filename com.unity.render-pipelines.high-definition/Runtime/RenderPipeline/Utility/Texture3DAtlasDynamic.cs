using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using System;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering.HighDefinition
{
    class Atlas3DAllocatorDynamic
    {
        private class Atlas3DNodePool
        {
            public Atlas3DNode[] m_Nodes;
            Int16 m_Next;
            Int16 m_FreelistHead;

            public Atlas3DNodePool(Int16 capacity)
            {
                m_Nodes = new Atlas3DNode[capacity];
                m_Next = 0;
                m_FreelistHead = -1;
            }

            public void Dispose()
            {
                Clear();
                m_Nodes = null;
            }

            public void Clear()
            {
                m_Next = 0;
                m_FreelistHead = -1;
            }

            public Int16 Atlas3DNodeCreate(Int16 parent)
            {
                Debug.Assert((m_Next < m_Nodes.Length) || (m_FreelistHead != -1), "Error: Atlas3DNodePool: Out of memory. Please pre-allocate pool to larger capacity");

                if (m_FreelistHead != -1)
                {
                    Int16 freelistHeadNext = m_Nodes[m_FreelistHead].m_FreelistNext;
                    m_Nodes[m_FreelistHead] = new Atlas3DNode(m_FreelistHead, parent);
                    Int16 res = m_FreelistHead;
                    m_FreelistHead = freelistHeadNext;
                    return res;
                }

                m_Nodes[m_Next] = new Atlas3DNode(m_Next, parent);
                return m_Next++;
            }

            public void Atlas3DNodeFree(Int16 index)
            {
                Debug.Assert(index >= 0 && index < m_Nodes.Length, "Error: Atlas3DNodeFree: index out of range.");
                m_Nodes[index].m_FreelistNext = m_FreelistHead;
                m_FreelistHead = index;
            }
        }

        private struct Atlas3DNode
        {
            private enum Atlas3DNodeFlags : uint
            {
                IsOccupied = 1 << 0
            }

            public Int16 m_Self;
            public Int16 m_Parent;
            public Int16 m_LeftChild;
            public Int16 m_RightChild;
            public Int16 m_FreelistNext;
            public UInt16 m_Flags;
            public Vector3 m_RectSize;
            public Vector3 m_RectOffset;

            public Atlas3DNode(Int16 self, Int16 parent)
            {
                m_Self = self;
                m_Parent = parent;
                m_LeftChild = -1;
                m_RightChild = -1;
                m_Flags = 0;
                m_FreelistNext = -1;
                m_RectSize = Vector3.zero;
                m_RectOffset = Vector3.zero;
            }

            public bool IsOccupied()
            {
                return (m_Flags & (UInt16)Atlas3DNodeFlags.IsOccupied) > 0;
            }

            public void SetIsOccupied()
            {
                UInt16 isOccupiedMask = (UInt16)Atlas3DNodeFlags.IsOccupied;
                m_Flags |= isOccupiedMask;
            }

            public void ClearIsOccupied()
            {
                UInt16 isOccupiedMask = (UInt16)Atlas3DNodeFlags.IsOccupied;
                m_Flags &= (UInt16) ~isOccupiedMask;
            }

            public bool IsLeafNode()
            {
                // Note: Only need to check if m_LeftChild == null, as either both are allocated (split), or none are allocated (leaf).
                return m_LeftChild == -1;
            }

            public Int16 Allocate(Atlas3DNodePool pool, int width, int height, int depth)
            {
                if (Mathf.Min(Mathf.Min(width, height), depth) < 1)
                {
                    // Degenerate allocation requested.
                    Debug.Assert(false, "Error: Texture3DAtlasDynamic: Attempted to allocate a degenerate region. Please ensure width and height are >= 1");
                    return -1;
                }

                // not a leaf node, try children
                // TODO: Rather than always going left, then right, we might want to always attempt to allocate in the smaller child, then larger.
                if (!IsLeafNode())
                {
                    Int16 node = pool.m_Nodes[m_LeftChild].Allocate(pool, width, height, depth);
                    if (node == -1)
                    {
                        node = pool.m_Nodes[m_RightChild].Allocate(pool, width, height, depth);
                    }
                    return node;
                }

                // leaf node, check for fit
                if (IsOccupied()) { return -1; }
                if (width > m_RectSize.x || height > m_RectSize.y || depth > m_RectSize.z) { return -1; }

                // perform the split
                Debug.Assert(m_LeftChild == -1);
                Debug.Assert(m_RightChild == -1);
                m_LeftChild = pool.Atlas3DNodeCreate(m_Self);
                m_RightChild = pool.Atlas3DNodeCreate(m_Self);
                // Debug.Log("m_LeftChild = " + m_LeftChild);
                // Debug.Log("m_RightChild = " + m_RightChild);

                Debug.Assert(m_LeftChild >= 0 && m_LeftChild < pool.m_Nodes.Length);
                Debug.Assert(m_RightChild >= 0 && m_RightChild < pool.m_Nodes.Length);

                // Debug.Log("Rect = {" + m_RectSize.x + ", " + m_RectSize.y + ", " + m_RectSize.z + ", " + m_RectOffset.x + ", " + m_RectOffset.y + "," + m_RectOffset.z + "}");

                float deltaX = m_RectSize.x - width;
                float deltaY = m_RectSize.y - height;
                float deltaZ = m_RectSize.z - depth;
                // Debug.Log("deltaX = " + deltaX);
                // Debug.Log("deltaY = " + deltaY);
                // Debug.Log("deltaZ = " + deltaZ);

                if (deltaX >= deltaY && deltaX >= deltaZ)
                {
                    // Debug.Log("Split X");
                    //
                    //     +--------+------+
                    //    /        /      /|
                    //   /        /      / |
                    //  +--------+------+  |
                    //  |        |      |  |
                    //  |        |      |  +
                    //  |        |      | /
                    //  |        |      |/
                    //  +--------+------+
                    //
                    pool.m_Nodes[m_LeftChild].m_RectSize = new Vector3(width, m_RectSize.y, m_RectSize.z);
                    pool.m_Nodes[m_LeftChild].m_RectOffset = m_RectOffset;

                    pool.m_Nodes[m_RightChild].m_RectSize = new Vector3(deltaX, m_RectSize.y, m_RectSize.z);
                    pool.m_Nodes[m_RightChild].m_RectOffset = new Vector3(m_RectOffset.x + width, m_RectOffset.y, m_RectOffset.z);

                    if (Mathf.Max(deltaY, deltaZ) < 1)
                    {
                        pool.m_Nodes[m_LeftChild].SetIsOccupied();
                        return m_LeftChild;
                    }
                    else
                    {
                        Int16 node = pool.m_Nodes[m_LeftChild].Allocate(pool, width, height, depth);
                        if (node >= 0) { pool.m_Nodes[node].SetIsOccupied(); }
                        return node;
                    }
                }
                else if (deltaY >= deltaX && deltaY >= deltaZ)
                {
                    // Debug.Log("Split Y.");
                    //
                    //     +---------------+
                    //    /               /|
                    //   /               / +
                    //  +---------------+ /|
                    //  |               |/ |
                    //  +---------------+  +
                    //  |               | /
                    //  |               |/
                    //  +---------------+
                    //
                    pool.m_Nodes[m_LeftChild].m_RectSize = new Vector3(m_RectSize.x, height, m_RectSize.z);
                    pool.m_Nodes[m_LeftChild].m_RectOffset = m_RectOffset;

                    pool.m_Nodes[m_RightChild].m_RectSize = new Vector3(m_RectSize.x, deltaY, m_RectSize.z);
                    pool.m_Nodes[m_RightChild].m_RectOffset = new Vector3(m_RectOffset.x, m_RectOffset.y + height, m_RectOffset.z);

                    if (Math.Max(deltaX, deltaZ) < 1)
                    {
                        pool.m_Nodes[m_LeftChild].SetIsOccupied();
                        return m_LeftChild;
                    }
                    else
                    {
                        Int16 node = pool.m_Nodes[m_LeftChild].Allocate(pool, width, height, depth);
                        if (node >= 0) { pool.m_Nodes[node].SetIsOccupied(); }
                        return node;
                    }
                }
                else // deltaZ >= deltaX && deltaZ >= deltaY
                {
                    // Debug.Log("Split Z.");
                    //
                    //     +---------------+
                    //    +---------------+|
                    //   /               /||
                    //  +---------------+ ||
                    //  |               | ||
                    //  |               | |+
                    //  |               | +
                    //  |               |/
                    //  +---------------+
                    //
                    pool.m_Nodes[m_LeftChild].m_RectSize = new Vector3(m_RectSize.x, m_RectSize.y, depth);
                    pool.m_Nodes[m_LeftChild].m_RectOffset = m_RectOffset;

                    pool.m_Nodes[m_RightChild].m_RectSize = new Vector3(m_RectSize.x, m_RectSize.y, deltaZ);
                    pool.m_Nodes[m_RightChild].m_RectOffset = new Vector3(m_RectOffset.x, m_RectOffset.y, m_RectOffset.z + depth);

                    if (Math.Max(deltaX, deltaY) < 1)
                    {
                        pool.m_Nodes[m_LeftChild].SetIsOccupied();
                        return m_LeftChild;
                    }
                    else
                    {
                        Int16 node = pool.m_Nodes[m_LeftChild].Allocate(pool, width, height, depth);
                        if (node >= 0) { pool.m_Nodes[node].SetIsOccupied(); }
                        return node;
                    }
                }
            }

            public void ReleaseChildren(Atlas3DNodePool pool)
            {
                if (IsLeafNode()) { return; }
                pool.m_Nodes[m_LeftChild].ReleaseChildren(pool);
                pool.m_Nodes[m_RightChild].ReleaseChildren(pool);

                pool.Atlas3DNodeFree(m_LeftChild);
                pool.Atlas3DNodeFree(m_RightChild);
                m_LeftChild = -1;
                m_RightChild = -1;
            }

            public void ReleaseAndMerge(Atlas3DNodePool pool)
            {
                Int16 n = m_Self;
                do
                {
                    pool.m_Nodes[n].ReleaseChildren(pool);
                    pool.m_Nodes[n].ClearIsOccupied();
                    n = pool.m_Nodes[n].m_Parent;
                }
                while (n >= 0 && pool.m_Nodes[n].IsMergeNeeded(pool));
            }

            public bool IsMergeNeeded(Atlas3DNodePool pool)
            {
                return pool.m_Nodes[m_LeftChild].IsLeafNode() && (!pool.m_Nodes[m_LeftChild].IsOccupied())
                    && pool.m_Nodes[m_RightChild].IsLeafNode() && (!pool.m_Nodes[m_RightChild].IsOccupied());
            }
        }

        private int m_Width;
        private int m_Height;
        private int m_Depth;
        private Atlas3DNodePool m_Pool;
        private Int16 m_Root;
        private Dictionary<int, Int16> m_NodeFromID;

        public Atlas3DAllocatorDynamic(int width, int height, int depth, int capacityAllocations)
        {
            // In an evenly split binary tree, the nodeCount == leafNodeCount * 2
            int capacityNodes = capacityAllocations * 2;
            Debug.Assert(capacityNodes < (1 << 16), "Error: Atlas3DAllocatorDynamic: Attempted to allocate a capacity of " + capacityNodes + ", which is greater than our 16-bit indices can support. Please request a capacity <=" + (1 << 16));
            m_Pool = new Atlas3DNodePool((Int16)capacityNodes);

            m_NodeFromID = new Dictionary<int, Int16>(capacityAllocations);

            Int16 rootParent = -1;
            m_Root = m_Pool.Atlas3DNodeCreate(rootParent);
            m_Pool.m_Nodes[m_Root].m_RectSize = new Vector3(width, height, depth);
            m_Pool.m_Nodes[m_Root].m_RectOffset = Vector3.zero;
            m_Width = width;
            m_Height = height;
            m_Depth = depth;

            // string debug = "";
            // DebugStringFromNode(ref debug, m_Root);
            // Debug.Log("Allocating atlas = " + debug);
        }

        public bool Allocate(out Vector3 resultSize, out Vector3 resultOffset, int key, int width, int height, int depth)
        {
            Int16 node = m_Pool.m_Nodes[m_Root].Allocate(m_Pool, width, height, depth);
            if (node >= 0)
            {
                resultSize = m_Pool.m_Nodes[node].m_RectSize;
                resultOffset = m_Pool.m_Nodes[node].m_RectOffset;
                m_NodeFromID.Add(key, node);
                return true;
            }
            else
            {
                resultSize = Vector3.zero;
                resultOffset = Vector3.zero;
                return false;
            }
        }

        public void Release(int key)
        {
            if (m_NodeFromID.TryGetValue(key, out Int16 node))
            {
                Debug.Assert(node >= 0 && node < m_Pool.m_Nodes.Length);
                m_Pool.m_Nodes[node].ReleaseAndMerge(m_Pool);
                m_NodeFromID.Remove(key);
                return;
            }
        }

        public void Release()
        {
            m_Pool.Clear();
            m_Root = m_Pool.Atlas3DNodeCreate(-1);
            m_Pool.m_Nodes[m_Root].m_RectSize = new Vector3(m_Width, m_Height, m_Depth);
            m_Pool.m_Nodes[m_Root].m_RectOffset = Vector3.zero;
            m_NodeFromID.Clear();
        }

        public string DebugStringFromRoot(int depthMax = -1)
        {
            string res = "";
            DebugStringFromNode(ref res, m_Root, 0, depthMax);
            return res;
        }

        private void DebugStringFromNode(ref string res, Int16 n, int depthCurrent = 0, int depthMax = -1)
        {
            res += "{[" + depthCurrent + "], isOccupied = " + (m_Pool.m_Nodes[n].IsOccupied() ? "true" : "false") + ", self = " + m_Pool.m_Nodes[n].m_Self + ", " + m_Pool.m_Nodes[n].m_RectSize.x + "," + m_Pool.m_Nodes[n].m_RectSize.y + ", " + m_Pool.m_Nodes[n].m_RectSize.z + ", " + m_Pool.m_Nodes[n].m_RectOffset.x + ", " + m_Pool.m_Nodes[n].m_RectOffset.y + ", " + m_Pool.m_Nodes[n].m_RectOffset.z + "}\n";

            if (depthMax == -1 || depthCurrent < depthMax)
            {
                if (m_Pool.m_Nodes[n].m_LeftChild >= 0)
                {
                    DebugStringFromNode(ref res, m_Pool.m_Nodes[n].m_LeftChild, depthCurrent + 1, depthMax);
                }

                if (m_Pool.m_Nodes[n].m_RightChild >= 0)
                {
                    DebugStringFromNode(ref res, m_Pool.m_Nodes[n].m_RightChild, depthCurrent + 1, depthMax);
                }
            }
        }
    }

    class Texture3DAtlasDynamic
    {
        private RTHandle m_AtlasTexture = null;
        private bool isAtlasTextureOwner = false;
        private int m_Width;
        private int m_Height;
        private int m_Depth;
        private GraphicsFormat m_Format;
        private Atlas3DAllocatorDynamic m_AtlasAllocator = null;
        private Dictionary<int, Texture3DAtlasScaleBias> m_AllocationCache;

        private struct Texture3DAtlasScaleBias
        {
            public Vector3 scale;
            public Vector3 bias;
        }

        public RTHandle AtlasTexture
        {
            get
            {
                return m_AtlasTexture;
            }
        }

        public Texture3DAtlasDynamic(int width, int height, int depth, int capacity, GraphicsFormat format)
        {
            m_Width = width;
            m_Height = height;
            m_Depth = depth;
            m_Format = format;
            m_AtlasTexture = RTHandles.Alloc(
                m_Width,
                m_Height,
                m_Depth,
                DepthBits.None,
                m_Format,
                FilterMode.Point,
                TextureWrapMode.Clamp,
                TextureDimension.Tex3D,
                false,
                true,
                false,
                false,
                1,
                0,
                MSAASamples.None,
                false,
                false
            );
            isAtlasTextureOwner = true;

            m_AtlasAllocator = new Atlas3DAllocatorDynamic(width, height, depth, capacity);
            m_AllocationCache = new Dictionary<int, Texture3DAtlasScaleBias>(capacity);
        }

        public Texture3DAtlasDynamic(int width, int height, int depth, int capacity, RTHandle atlasTexture)
        {
            m_Width = width;
            m_Height = height;
            m_Depth = depth;
            m_Format = atlasTexture.rt.graphicsFormat;
            m_AtlasTexture = atlasTexture;
            isAtlasTextureOwner = false;

            m_AtlasAllocator = new Atlas3DAllocatorDynamic(width, height, depth, capacity);
            m_AllocationCache = new Dictionary<int, Texture3DAtlasScaleBias>(capacity);
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

        public bool TryGetScaleBias(out Vector3 scale, out Vector3 bias, int key)
        {
            if (m_AllocationCache.TryGetValue(key, out Texture3DAtlasScaleBias scaleBias))
            {
                scale = scaleBias.scale;
                bias = scaleBias.bias;
                return true;
            }

            scale = Vector3.zero;
            bias = Vector3.zero;
            return false;
        }

        public bool EnsureTextureSlot(out bool isUploadNeeded, out Vector3 scale, out Vector3 bias, int key, int width, int height, int depth)
        {
            isUploadNeeded = false;
            if (m_AllocationCache.TryGetValue(key, out Texture3DAtlasScaleBias scaleBias))
            {
                scale = scaleBias.scale;
                bias = scaleBias.bias;
                return true;
            }

            // Debug.Log("EnsureTextureSlot Before = " + m_AtlasAllocator.DebugStringFromRoot());
            if (!m_AtlasAllocator.Allocate(out scale, out bias, key, width, height, depth)) { return false; }
            // Debug.Log("EnsureTextureSlot After = " + m_AtlasAllocator.DebugStringFromRoot());

            isUploadNeeded = true;
            scale.Scale(new Vector3(1.0f / m_Width, 1.0f / m_Height, 1.0f / m_Depth));
            bias.Scale(new Vector3(1.0f / m_Width, 1.0f / m_Height, 1.0f / m_Depth));
            m_AllocationCache.Add(key, new Texture3DAtlasScaleBias { scale = scale, bias = bias});
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
