using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using System;

namespace UnityEngine.Rendering
{
    class AtlasAllocator
    {
        private class AtlasNode
        {
            public AtlasNode m_RightChild = null;
            public AtlasNode m_BottomChild = null;
            public Vector4 m_Rect = new Vector4(0, 0, 0, 0); // x,y is width and height (scale) z,w offset into atlas (offset)

            public AtlasNode Allocate(ref ObjectPool<AtlasNode> pool, int width, int height, bool powerOfTwoPadding)
            {
                // not a leaf node, try children
                if (m_RightChild != null)
                {
                    AtlasNode node = m_RightChild.Allocate(ref pool, width, height, powerOfTwoPadding);
                    if (node == null)
                    {
                        node = m_BottomChild.Allocate(ref pool, width, height, powerOfTwoPadding);
                    }
                    return node;
                }

                int wPadd = 0;
                int hPadd = 0;

                if (powerOfTwoPadding)
                {
                    wPadd = (int)m_Rect.x % width;
                    hPadd = (int)m_Rect.y % height;
                }

                //leaf node, check for fit
                if ((width <= m_Rect.x - wPadd) && (height <= m_Rect.y - hPadd))
                {
                    // perform the split
                    m_RightChild = pool.Get();
                    m_BottomChild = pool.Get();

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

            public void Release(ref ObjectPool<AtlasNode> pool)
            {
                if (m_RightChild != null)
                {
                    m_RightChild.Release(ref pool);
                    m_BottomChild.Release(ref pool);
                    pool.Release(m_RightChild);
                    pool.Release(m_BottomChild);
                }

                m_RightChild = null;
                m_BottomChild = null;
                m_Rect = Vector4.zero;
            }
        }

        private AtlasNode m_Root;
        private int m_Width;
        private int m_Height;
        private bool powerOfTwoPadding;
        private ObjectPool<AtlasNode> m_NodePool;

        public AtlasAllocator(int width, int height, bool potPadding)
        {
            m_Root = new AtlasNode();
            m_Root.m_Rect.Set(width, height, 0, 0);
            m_Width = width;
            m_Height = height;
            powerOfTwoPadding = potPadding;
            m_NodePool = new ObjectPool<AtlasNode>(_ => { }, _ => { });
        }

        public bool Allocate(ref Vector4 result, int width, int height)
        {
            AtlasNode node = m_Root.Allocate(ref m_NodePool, width, height, powerOfTwoPadding);
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

        public void Reset()
        {
            m_Root.Release(ref m_NodePool);
            m_Root.m_Rect.Set(m_Width, m_Height, 0, 0);
        }
    }

    /// <summary>
    /// A generic Atlas texture of 2D textures.
    /// An atlas texture is a texture collection that collects multiple sub-textures into a single big texture.
    /// Sub-texture allocation for Texture2DAtlas is static and will not change after initial allocation.
    /// Does not add mipmap padding for sub-textures.
    /// </summary>
    public class Texture2DAtlas
    {
        private enum BlitType
        {
            Default,
            CubeTo2DOctahedral,
            SingleChannel,
            CubeTo2DOctahedralSingleChannel,
        }

        /// <summary>
        /// Texture is not on the GPU or is not up to date.
        /// </summary>
        private protected const int kGPUTexInvalid = 0;
        /// <summary>
        /// Texture Mip0 is on the GPU and up to date.
        /// </summary>
        private protected const int kGPUTexValidMip0 = 1;
        /// <summary>
        /// Texture and all mips are on the GPU and up to date.
        /// </summary>
        private protected const int kGPUTexValidMipAll = 2;

        /// <summary>
        /// The texture for the atlas.
        /// </summary>
        private protected RTHandle m_AtlasTexture = null;
        /// <summary>
        /// Width of the atlas.
        /// </summary>
        private protected int m_Width;
        /// <summary>
        /// Height of the atlas.
        /// </summary>
        private protected int m_Height;
        /// <summary>
        /// Format of the atlas.
        /// </summary>
        private protected GraphicsFormat m_Format;
        /// <summary>
        /// Atlas uses mip maps.
        /// </summary>
        private protected bool m_UseMipMaps;
        bool m_IsAtlasTextureOwner = false;
        private AtlasAllocator m_AtlasAllocator = null;
        private Dictionary<int, (Vector4 scaleOffset, Vector2Int size)> m_AllocationCache = new Dictionary<int, (Vector4, Vector2Int)>();
        private Dictionary<int, int> m_IsGPUTextureUpToDate = new Dictionary<int, int>();
        private Dictionary<int, int> m_TextureHashes = new Dictionary<int, int>();

        static readonly Vector4 fullScaleOffset = new Vector4(1, 1, 0, 0);

        // Maximum mip padding that can be applied to the textures in the atlas (1 << 10 = 1024 pixels)
        static readonly int s_MaxMipLevelPadding = 10;

        /// <summary>
        /// Maximum mip padding (pow2) that can be applied to the textures in the atlas
        /// </summary>
        public static int maxMipLevelPadding => s_MaxMipLevelPadding;

        /// <summary>
        /// Handle to the texture of the atlas.
        /// </summary>
        public RTHandle AtlasTexture
        {
            get
            {
                return m_AtlasTexture;
            }
        }

        /// <summary>
        /// Creates a new empty texture atlas.
        /// </summary>
        /// <param name="width">Width of the atlas in pixels.</param>
        /// <param name="height">Height of atlas in pixels.</param>
        /// <param name="format">GraphicsFormat of the atlas.</param>
        /// <param name="filterMode">Filtering mode of the atlas.</param>
        /// <param name="powerOfTwoPadding">Power of two padding.</param>
        /// <param name="name">Name of the atlas</param>
        /// <param name="useMipMap">Use mip maps</param>
        public Texture2DAtlas(int width, int height, GraphicsFormat format, FilterMode filterMode = FilterMode.Point, bool powerOfTwoPadding = false, string name = "", bool useMipMap = true)
        {
            m_Width = width;
            m_Height = height;
            m_Format = format;
            m_UseMipMaps = useMipMap;
            m_AtlasTexture = RTHandles.Alloc(
                width: m_Width,
                height: m_Height,
                filterMode: filterMode,
                colorFormat: m_Format,
                wrapMode: TextureWrapMode.Clamp,
                useMipMap: useMipMap,
                autoGenerateMips: false,
                name: name
            );
            m_IsAtlasTextureOwner = true;

            // We clear on create to avoid garbage data to be present in the atlas
            int mipCount = useMipMap ? GetTextureMipmapCount(m_Width, m_Height) : 1;
            for (int mipIdx = 0; mipIdx < mipCount; ++mipIdx)
            {
                Graphics.SetRenderTarget(m_AtlasTexture, mipIdx);
                GL.Clear(false, true, Color.clear);
            }

            m_AtlasAllocator = new AtlasAllocator(width, height, powerOfTwoPadding);
        }

        /// <summary>
        /// Release atlas resources.
        /// </summary>
        public void Release()
        {
            ResetAllocator();
            if (m_IsAtlasTextureOwner) { RTHandles.Release(m_AtlasTexture); }
        }

        /// <summary>
        /// Clear atlas sub-texture allocations.
        /// </summary>
        public void ResetAllocator()
        {
            m_AtlasAllocator.Reset();
            m_AllocationCache.Clear();

            m_IsGPUTextureUpToDate.Clear(); // mark all GPU textures as invalid.
        }

        /// <summary>
        /// Clear atlas texture.
        /// <param name="cmd">Target command buffer for graphics commands.</param>
        /// </summary>
        /// <param name="cmd">Target command buffer for graphics commands.</param>
        public void ClearTarget(CommandBuffer cmd)
        {
            int mipCount = (m_UseMipMaps) ? GetTextureMipmapCount(m_Width, m_Height) : 1;

            // clear the atlas by blitting a black texture at every mips
            for (int mipLevel = 0; mipLevel < mipCount; mipLevel++)
            {
                cmd.SetRenderTarget(m_AtlasTexture, mipLevel);
                Blitter.BlitQuad(cmd, Texture2D.blackTexture, fullScaleOffset, fullScaleOffset, mipLevel, true);
            }

            m_IsGPUTextureUpToDate.Clear(); // mark all GPU textures as invalid.
        }

        /// <summary>
        /// Return texture mip map count based on the width and height.
        /// </summary>
        /// <param name="width">The texture width in pixels.</param>
        /// <param name="height">The texture height in pixels.</param>
        /// <returns>The number of mip maps.</returns>
        private protected int GetTextureMipmapCount(int width, int height)
        {
            if (!m_UseMipMaps)
                return 1;

            // We don't care about the real mipmap count in the texture because they are generated by the atlas
            float maxSize = Mathf.Max(width, height);
            return Mathf.FloorToInt(Mathf.Log(maxSize, 2)) + 1;
        }

        /// <summary>
        /// Test if a texture is a 2D texture.
        /// </summary>
        /// <param name="texture">Source texture.</param>
        /// <returns>True if texture is 2D, false otherwise.</returns>
        private protected bool Is2D(Texture texture)
        {
            RenderTexture rt = texture as RenderTexture;

            return (texture is Texture2D || rt?.dimension == TextureDimension.Tex2D);
        }

        /// <summary>
        /// Checks if single/multi/single channel format conversion is required.
        /// </summary>
        /// <param name="source">Blit source texture</param>
        /// <param name="destination">Blit destination texture</param>
        /// <returns>true on single channel conversion false otherwise</returns>
        private protected bool IsSingleChannelBlit(Texture source, Texture destination)
        {
            var srcCount = GraphicsFormatUtility.GetComponentCount(source.graphicsFormat);
            var dstCount = GraphicsFormatUtility.GetComponentCount(destination.graphicsFormat);
            if (srcCount == 1 || dstCount == 1)
            {
                // One to many, many to one
                if (srcCount != dstCount)
                    return true;

                // Single channel swizzle
                var srcSwizzle =
                    ((1 << ((int)GraphicsFormatUtility.GetSwizzleA(source.graphicsFormat) & 0x7)) << 24) |
                    ((1 << ((int)GraphicsFormatUtility.GetSwizzleB(source.graphicsFormat) & 0x7)) << 16) |
                    ((1 << ((int)GraphicsFormatUtility.GetSwizzleG(source.graphicsFormat) & 0x7)) << 8) |
                    ((1 << ((int)GraphicsFormatUtility.GetSwizzleR(source.graphicsFormat) & 0x7)));
                var dstSwizzle =
                    ((1 << ((int)GraphicsFormatUtility.GetSwizzleA(destination.graphicsFormat) & 0x7)) << 24) |
                    ((1 << ((int)GraphicsFormatUtility.GetSwizzleB(destination.graphicsFormat) & 0x7)) << 16) |
                    ((1 << ((int)GraphicsFormatUtility.GetSwizzleG(destination.graphicsFormat) & 0x7)) << 8) |
                    ((1 << ((int)GraphicsFormatUtility.GetSwizzleR(destination.graphicsFormat) & 0x7)));
                if (srcSwizzle != dstSwizzle)
                    return true;
            }

            return false;
        }

        private void Blit2DTexture(CommandBuffer cmd, Vector4 scaleOffset, Texture texture, Vector4 sourceScaleOffset, bool blitMips, BlitType blitType)
        {
            int mipCount = GetTextureMipmapCount(texture.width, texture.height);

            if (!blitMips)
                mipCount = 1;

            for (int mipLevel = 0; mipLevel < mipCount; mipLevel++)
            {
                cmd.SetRenderTarget(m_AtlasTexture, mipLevel);
                switch (blitType)
                {
                    case BlitType.Default: Blitter.BlitQuad(cmd, texture, sourceScaleOffset, scaleOffset, mipLevel, true); break;
                    case BlitType.CubeTo2DOctahedral: Blitter.BlitCubeToOctahedral2DQuad(cmd, texture, scaleOffset, mipLevel); break;
                    case BlitType.SingleChannel: Blitter.BlitQuadSingleChannel(cmd, texture, sourceScaleOffset, scaleOffset, mipLevel); break;
                    case BlitType.CubeTo2DOctahedralSingleChannel: Blitter.BlitCubeToOctahedral2DQuadSingleChannel(cmd, texture, scaleOffset, mipLevel); break;
                }
            }
        }

        /// <summary>
        /// Mark texture valid on the GPU.
        /// </summary>
        /// <param name="instanceId">Texture instance ID.</param>
        /// <param name="mipAreValid">Texture has valid mip maps.</param>
        private protected void MarkGPUTextureValid(int instanceId, bool mipAreValid = false)
        {
            m_IsGPUTextureUpToDate[instanceId] = (mipAreValid) ? kGPUTexValidMipAll : kGPUTexValidMip0;
        }

        /// <summary>
        /// Mark texture invalid on the GPU.
        /// </summary>
        /// <param name="instanceId">Texture instance ID.</param>
        private protected void MarkGPUTextureInvalid(int instanceId) => m_IsGPUTextureUpToDate[instanceId] = kGPUTexInvalid;

        /// <summary>
        /// Blit 2D texture into the atlas.
        /// </summary>
        /// <param name="cmd">Target command buffer for graphics commands.</param>
        /// <param name="scaleOffset">Destination scale (.xy) and offset (.zw)</param>
        /// <param name="texture">Source Texture</param>
        /// <param name="sourceScaleOffset">Source scale (.xy) and offset(.zw).</param>
        /// <param name="blitMips">Blit mip maps.</param>
        /// <param name="overrideInstanceID">Override texture instance ID.</param>
        public virtual void BlitTexture(CommandBuffer cmd, Vector4 scaleOffset, Texture texture, Vector4 sourceScaleOffset, bool blitMips = true, int overrideInstanceID = -1)
        {
            // This atlas only support 2D texture so we only blit 2D textures
            if (Is2D(texture))
            {
                BlitType blitType = BlitType.Default;
                if (IsSingleChannelBlit(texture, m_AtlasTexture.m_RT))
                    blitType = BlitType.SingleChannel;

                Blit2DTexture(cmd, scaleOffset, texture, sourceScaleOffset, blitMips, blitType);
                var instanceID = overrideInstanceID != -1 ? overrideInstanceID : GetTextureID(texture);
                MarkGPUTextureValid(instanceID, blitMips);
                m_TextureHashes[instanceID] = CoreUtils.GetTextureHash(texture);
            }
        }

        /// <summary>
        /// Blit octahedral texture into the atlas.
        /// </summary>
        /// <param name="cmd">Target command buffer for graphics commands.</param>
        /// <param name="scaleOffset">Destination scale (.xy) and offset (.zw)</param>
        /// <param name="texture">Source Texture</param>
        /// <param name="sourceScaleOffset">Source scale (.xy) and offset(.zw).</param>
        /// <param name="blitMips">Blit mip maps.</param>
        /// <param name="overrideInstanceID">Override texture instance ID.</param>
        public virtual void BlitOctahedralTexture(CommandBuffer cmd, Vector4 scaleOffset, Texture texture, Vector4 sourceScaleOffset, bool blitMips = true, int overrideInstanceID = -1)
        {
            // Default implementation. No padding in Texture2DAtlas, no need to handle specially.
            BlitTexture(cmd, scaleOffset, texture, sourceScaleOffset, blitMips, overrideInstanceID);
        }

        /// <summary>
        /// Blit and project Cube texture into a 2D texture in the atlas.
        /// </summary>
        /// <param name="cmd">Target command buffer for graphics commands.</param>
        /// <param name="scaleOffset">Destination scale (.xy) and offset (.zw)</param>
        /// <param name="texture">Source Texture</param>
        /// <param name="blitMips">Blit mip maps.</param>
        /// <param name="overrideInstanceID">Override texture instance ID.</param>
        public virtual void BlitCubeTexture2D(CommandBuffer cmd, Vector4 scaleOffset, Texture texture, bool blitMips = true, int overrideInstanceID = -1)
        {
            Debug.Assert(texture.dimension == TextureDimension.Cube);

            // This atlas only support 2D texture so we map Cube into set of 2D textures
            if (texture.dimension == TextureDimension.Cube)
            {
                BlitType blitType = BlitType.CubeTo2DOctahedral;
                if (IsSingleChannelBlit(texture, m_AtlasTexture.m_RT))
                    blitType = BlitType.CubeTo2DOctahedralSingleChannel;

                // By default blit cube into a single octahedral 2D texture quad
                Blit2DTexture(cmd, scaleOffset, texture, new Vector4(1.0f, 1.0f, 0.0f, 0.0f), blitMips, blitType);

                var instanceID = overrideInstanceID != -1 ? overrideInstanceID : GetTextureID(texture);
                MarkGPUTextureValid(instanceID, blitMips);
                m_TextureHashes[instanceID] = CoreUtils.GetTextureHash(texture);
            }
        }

        /// <summary>
        /// Allocate space from the atlas for a texture and copy texture contents into the atlas.
        /// </summary>
        /// <param name="cmd">Target command buffer for graphics commands.</param>
        /// <param name="scaleOffset">Destination scale (.xy) and offset (.zw)</param>
        /// <param name="texture">Source Texture</param>
        /// <param name="width">Request width in pixels.</param>
        /// <param name="height">Request height in pixels.</param>
        /// <param name="overrideInstanceID">Override texture instance ID.</param>
        /// <returns></returns>
        public virtual bool AllocateTexture(CommandBuffer cmd, ref Vector4 scaleOffset, Texture texture, int width, int height, int overrideInstanceID = -1)
        {
            var instanceID = overrideInstanceID != -1 ? overrideInstanceID : GetTextureID(texture);
            bool allocated = AllocateTextureWithoutBlit(instanceID, width, height, ref scaleOffset);

            if (allocated)
            {
                if (Is2D(texture))
                    BlitTexture(cmd, scaleOffset, texture, fullScaleOffset);
                else
                    BlitCubeTexture2D(cmd, scaleOffset, texture, true);

                // texture is up to date
                MarkGPUTextureValid(instanceID, true);
                m_TextureHashes[instanceID] = CoreUtils.GetTextureHash(texture);
            }

            return allocated;
        }

        /// <summary>
        /// Allocate space from the atlas for a texture.
        /// </summary>
        /// <param name="texture">Source texture.</param>
        /// <param name="width">Request width in pixels.</param>
        /// <param name="height">Request height in pixels.</param>
        /// <param name="scaleOffset">Allocated scale (.xy) and offset (.zw).</param>
        /// <returns>True on success, false otherwise.</returns>
        public bool AllocateTextureWithoutBlit(Texture texture, int width, int height, ref Vector4 scaleOffset)
            => AllocateTextureWithoutBlit(texture.GetInstanceID(), width, height, ref scaleOffset);

        /// <summary>
        /// Allocate space from the atlas for a texture.
        /// </summary>
        /// <param name="instanceId">Source texture instance ID.</param>
        /// <param name="width">Request width in pixels.</param>
        /// <param name="height">Request height in pixels.</param>
        /// <param name="scaleOffset">Allocated scale (.xy) and offset (.zw).</param>
        /// <returns>True on success, false otherwise.</returns>
        public virtual bool AllocateTextureWithoutBlit(int instanceId, int width, int height, ref Vector4 scaleOffset)
        {
            scaleOffset = Vector4.zero;

            if (m_AtlasAllocator.Allocate(ref scaleOffset, width, height))
            {
                scaleOffset.Scale(new Vector4(1.0f / m_Width, 1.0f / m_Height, 1.0f / m_Width, 1.0f / m_Height));
                m_AllocationCache[instanceId] = (scaleOffset, new Vector2Int(width, height));
                MarkGPUTextureInvalid(instanceId); // the texture data haven't been uploaded
                m_TextureHashes[instanceId] = -1;
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Compute hash from texture properties.
        /// </summary>
        /// <param name="textureA">Source texture A.</param>
        /// <param name="textureB">Source texture B.</param>
        /// <returns>Hash of texture porperties.</returns>
        private protected int GetTextureHash(Texture textureA, Texture textureB)
        {
            int hash = CoreUtils.GetTextureHash(textureA) + 23 * CoreUtils.GetTextureHash(textureB);
            return hash;
        }

        /// <summary>
        /// Get sub-texture ID for the atlas.
        /// </summary>
        /// <param name="texture">Source texture.</param>
        /// <returns>Texture instance ID.</returns>
        public int GetTextureID(Texture texture)
        {
            return texture.GetInstanceID();
        }

        /// <summary>
        /// Get sub-texture ID for the atlas.
        /// </summary>
        /// <param name="textureA">Source texture A.</param>
        /// <param name="textureB">Source texture B.</param>
        /// <returns>Combined texture instance ID.</returns>
        public int GetTextureID(Texture textureA, Texture textureB)
        {
            return GetTextureID(textureA) + 23 * GetTextureID(textureB);
        }

        /// <summary>
        /// Check if the atlas contains the textures.
        /// </summary>
        /// <param name="scaleOffset">Texture scale (.xy) and offset (.zw).</param>
        /// <param name="textureA">Source texture A.</param>
        /// <param name="textureB">Source texture B.</param>
        /// <returns>True if the texture is in the atlas, false otherwise.</returns>
        public bool IsCached(out Vector4 scaleOffset, Texture textureA, Texture textureB)
            => IsCached(out scaleOffset, GetTextureID(textureA, textureB));

        /// <summary>
        /// Check if the atlas contains the textures.
        /// </summary>
        /// <param name="scaleOffset">Texture scale (.xy) and offset (.zw).</param>
        /// <param name="texture">Source texture</param>
        /// <returns>True if the texture is in the atlas, false otherwise.</returns>
        public bool IsCached(out Vector4 scaleOffset, Texture texture)
            => IsCached(out scaleOffset, GetTextureID(texture));

        /// <summary>
        /// Check if the atlas contains the texture.
        /// </summary>
        /// <param name="scaleOffset">Texture scale (.xy) and offset (.zw).</param>
        /// <param name="id">Source texture instance ID.</param>
        /// <returns></returns>
        public bool IsCached(out Vector4 scaleOffset, int id)
        {
            bool cached = m_AllocationCache.TryGetValue(id, out var value);
            scaleOffset = value.scaleOffset;
            return cached;
        }

        /// <summary>
        /// Get cached texture size.
        /// </summary>
        /// <param name="id">Source texture instance ID.</param>
        /// <returns>Texture size.</returns>
        internal Vector2Int GetCachedTextureSize(int id)
        {
            m_AllocationCache.TryGetValue(id, out var value);
            return value.size;
        }

        /// <summary>
        /// Check if contents of a texture needs to be updated in the atlas.
        /// </summary>
        /// <param name="texture">Source texture.</param>
        /// <param name="needMips">Texture uses mips.</param>
        /// <returns>True if texture needs update, false otherwise.</returns>
        public virtual bool NeedsUpdate(Texture texture, bool needMips = false)
        {
            RenderTexture rt = texture as RenderTexture;
            int key = GetTextureID(texture);
            int textureHash = CoreUtils.GetTextureHash(texture);

            // Update the render texture if needed
            if (rt != null)
            {
                int updateCount;
                if (m_IsGPUTextureUpToDate.TryGetValue(key, out updateCount))
                {
                    if (rt.updateCount != updateCount)
                    {
                        m_IsGPUTextureUpToDate[key] = (int)rt.updateCount;
                        return true;
                    }
                }
                else
                {
                    m_IsGPUTextureUpToDate[key] = (int)rt.updateCount;
                }
            }
            // In case the texture settings/import settings have changed, we need to update it
            else if (m_TextureHashes.TryGetValue(key, out int hash) && hash != textureHash)
            {
                m_TextureHashes[key] = textureHash;
                return true;
            }
            // For regular textures, values == 0 means that their GPU data needs to be updated (either because
            // the atlas have been re-layouted or the texture have never been uploaded. We also check if the mips
            // are valid for the texture if we need them
            else if (m_IsGPUTextureUpToDate.TryGetValue(key, out var value))
                return value == kGPUTexInvalid || (needMips && value == kGPUTexValidMip0);

            return false;
        }

        /// <summary>
        /// Check if contents of a texture needs to be updated in the atlas.
        /// </summary>
        /// <param name="textureA">Source texture A.</param>
        /// <param name="textureB">Source texture B.</param>
        /// <param name="needMips">Texture uses mips.</param>
        /// <returns>True if texture needs update, false otherwise.</returns>
        public virtual bool NeedsUpdate(Texture textureA, Texture textureB, bool needMips = false)
        {
            RenderTexture rtA = textureA as RenderTexture;
            RenderTexture rtB = textureB as RenderTexture;
            int key = GetTextureID(textureA, textureB);
            int textureHash = GetTextureHash(textureA, textureB);

            // Update the render texture if needed
            if (rtA != null || rtB != null)
            {
                int updateCount;
                if (m_IsGPUTextureUpToDate.TryGetValue(key, out updateCount))
                {
                    if (rtA != null && rtB != null && Math.Min(rtA.updateCount, rtB.updateCount) != updateCount)
                    {
                        m_IsGPUTextureUpToDate[key] = (int)Math.Min(rtA.updateCount, rtB.updateCount);
                        return true;
                    }
                    else if (rtA != null && rtA.updateCount != updateCount)
                    {
                        m_IsGPUTextureUpToDate[key] = (int)rtA.updateCount;
                        return true;
                    }
                    else if (rtB != null && rtB.updateCount != updateCount)
                    {
                        m_IsGPUTextureUpToDate[key] = (int)rtB.updateCount;
                        return true;
                    }
                }
                else
                {
                    m_IsGPUTextureUpToDate[key] = textureHash;
                }
            }
            // In case the texture settings/import settings have changed, we need to update it
            else if (m_TextureHashes.TryGetValue(key, out int hash) && hash != textureHash)
            {
                m_TextureHashes[key] = key;
                return true;
            }
            // For regular textures, values == 0 means that their GPU data needs to be updated (either because
            // the atlas have been re-layouted or the texture have never been uploaded. We also check if the mips
            // are valid for the texture if we need them
            else if (m_IsGPUTextureUpToDate.TryGetValue(key, out var value))
                return value == kGPUTexInvalid || (needMips && value == kGPUTexValidMip0);

            return false;
        }

        /// <summary>
        /// Add a texture into the atlas.
        /// </summary>
        /// <param name="cmd">Command buffer used for texture copy.</param>
        /// <param name="scaleOffset">Sub-texture rectangle for the added texture. Scale in .xy, offset int .zw</param>
        /// <param name="texture">The texture to be added.</param>
        /// <returns>True if the atlas contains the texture, false otherwise.</returns>
        public virtual bool AddTexture(CommandBuffer cmd, ref Vector4 scaleOffset, Texture texture)
        {
            if (IsCached(out scaleOffset, texture))
                return true;

            return AllocateTexture(cmd, ref scaleOffset, texture, texture.width, texture.height);
        }

        /// <summary>
        /// Update a texture in the atlas.
        /// </summary>
        /// <param name="cmd">Target command buffer for graphics commands.</param>
        /// <param name="oldTexture">Texture in atlas.</param>
        /// <param name="newTexture">Replacement source texture.</param>
        /// <param name="scaleOffset">Destination scale (.xy) and offset (.zw)</param>
        /// <param name="sourceScaleOffset">Source scale (.xy) and offset(.zw).</param>
        /// <param name="updateIfNeeded">Enable texture blit.</param>
        /// <param name="blitMips">Blit mip maps.</param>
        /// <returns>True on success, false otherwise.</returns>
        public virtual bool UpdateTexture(CommandBuffer cmd, Texture oldTexture, Texture newTexture, ref Vector4 scaleOffset, Vector4 sourceScaleOffset, bool updateIfNeeded = true, bool blitMips = true)
        {
            // In case the old texture is here, we Blit the new one at the scale offset of the old one
            if (IsCached(out scaleOffset, oldTexture))
            {
                if (updateIfNeeded && NeedsUpdate(newTexture))
                {
                    if (Is2D(newTexture))
                        BlitTexture(cmd, scaleOffset, newTexture, sourceScaleOffset, blitMips);
                    else
                        BlitCubeTexture2D(cmd, scaleOffset, newTexture, blitMips);
                    MarkGPUTextureValid(GetTextureID(newTexture), blitMips); // texture is up to date
                }
                return true;
            }
            else // else we try to allocate the updated texture
            {
                return AllocateTexture(cmd, ref scaleOffset, newTexture, newTexture.width, newTexture.height);
            }
        }

        /// <summary>
        /// Update a texture in the atlas.
        /// </summary>
        /// <param name="cmd">Target command buffer for graphics commands.</param>
        /// <param name="texture">Texture in atlas.</param>
        /// <param name="scaleOffset">Destination scale (.xy) and offset (.zw)</param>
        /// <param name="updateIfNeeded">Enable texture blit.</param>
        /// <param name="blitMips">Blit mip maps.</param>
        /// <returns>True on success, false otherwise.</returns>
        public virtual bool UpdateTexture(CommandBuffer cmd, Texture texture, ref Vector4 scaleOffset, bool updateIfNeeded = true, bool blitMips = true)
            => UpdateTexture(cmd, texture, texture, ref scaleOffset, fullScaleOffset, updateIfNeeded, blitMips);

        internal bool EnsureTextureSlot(out bool isUploadNeeded, ref Vector4 scaleBias, int key, int width, int height)
        {
            isUploadNeeded = false;
            if (m_AllocationCache.TryGetValue(key, out var value))
            {
                scaleBias = value.scaleOffset;
                return true;
            }
            if (!m_AtlasAllocator.Allocate(ref scaleBias, width, height))
                return false;
            isUploadNeeded = true;
            scaleBias.Scale(new Vector4(1.0f / m_Width, 1.0f / m_Height, 1.0f / m_Width, 1.0f / m_Height));
            m_AllocationCache.Add(key, (scaleBias, new Vector2Int(width, height)));
            return true;
        }
    }
}
