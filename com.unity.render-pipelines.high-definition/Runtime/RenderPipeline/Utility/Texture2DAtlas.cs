using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;
using System.Linq;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Rendering.HighDefinition
{
    class AtlasAllocator
    {
        private class AtlasNode
        {
            public AtlasNode m_RightChild = null;
            public AtlasNode m_BottomChild = null;
            public Vector4 m_Rect = new Vector4(0, 0, 0, 0); // x,y is width and height (scale) z,w offset into atlas (offset)

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
                    wPadd = (int)m_Rect.x % width;
                    hPadd = (int)m_Rect.y % height;
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

        public void Reset()
        {
            m_Root.Release();
            m_Root.m_Rect.Set(m_Width, m_Height, 0, 0);
        }
    }

    class Texture2DAtlas
    {
        protected RTHandle m_AtlasTexture = null;
        internal bool m_IsAtlasTextureOwner = false;
        protected int m_Width;
        protected int m_Height;
        protected bool m_UseMipMaps;
        protected GraphicsFormat m_Format;
        private AtlasAllocator m_AtlasAllocator = null;
        private Dictionary<int, Vector4> m_AllocationCache = new Dictionary<int, Vector4>();
        private Dictionary<int, int> m_IsGPUTextureUpToDate = new Dictionary<int, int>();
        private Dictionary<int, int> m_TextureHashes = new Dictionary<int, int>();

        static readonly Vector4 fullScaleOffset = new Vector4(1, 1, 0, 0);

        // Maximum mip padding that can be applied to the textures in the atlas (1 << 10 = 1024 pixels)
        public static readonly int maxMipLevelPadding = 10;

        public RTHandle AtlasTexture
        {
            get
            {
                return m_AtlasTexture;
            }
        }

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

        public void Release()
        {
            ResetAllocator();
            if (m_IsAtlasTextureOwner) { RTHandles.Release(m_AtlasTexture); }
        }

        public void ResetAllocator()
        {
            m_AtlasAllocator.Reset();
            m_AllocationCache.Clear();

            m_IsGPUTextureUpToDate.Clear(); // mark all GPU textures as invalid.
        }

        public void ClearTarget(CommandBuffer cmd)
        {
            int mipCount = (m_UseMipMaps) ? GetTextureMipmapCount(m_Width, m_Height) : 1;

            // clear the atlas by blitting a black texture at every mips
            for (int mipLevel = 0; mipLevel < mipCount; mipLevel++)
            {
                cmd.SetRenderTarget(m_AtlasTexture, mipLevel);
                HDUtils.BlitQuad(cmd, Texture2D.blackTexture, fullScaleOffset, fullScaleOffset, mipLevel, true);
            }

            m_IsGPUTextureUpToDate.Clear(); // mark all GPU textures as invalid.
        }

        protected int GetTextureMipmapCount(int width, int height)
        {
            if (!m_UseMipMaps)
                return 1;

            // We don't care about the real mipmap count in the texture because they are generated by the atlas
            float maxSize = Mathf.Max(width, height);
            return Mathf.FloorToInt(Mathf.Log(maxSize, 2)) + 1;
        }

        protected bool Is2D(Texture texture)
        {
            RenderTexture rt = texture as RenderTexture;

            return (texture is Texture2D || rt?.dimension == TextureDimension.Tex2D);
        }

        protected void Blit2DTexture(CommandBuffer cmd, Vector4 scaleOffset, Texture texture, Vector4 sourceScaleOffset, bool blitMips = true)
        {
            int mipCount = GetTextureMipmapCount(texture.width, texture.height);

            if (!blitMips)
                mipCount = 1;

            for (int mipLevel = 0; mipLevel < mipCount; mipLevel++)
            {
                cmd.SetRenderTarget(m_AtlasTexture, mipLevel);
                HDUtils.BlitQuad(cmd, texture, sourceScaleOffset, scaleOffset, mipLevel, true);
            }
        }

        protected void MarkGPUTextureValid(int instanceId, bool mipAreValid = false)
        {
            m_IsGPUTextureUpToDate[instanceId] = (mipAreValid) ? 2 : 1;
        }

        protected void MarkGPUTextureInvalid(int instanceId) => m_IsGPUTextureUpToDate[instanceId] = 0;

        public virtual void BlitTexture(CommandBuffer cmd, Vector4 scaleOffset, Texture texture, Vector4 sourceScaleOffset, bool blitMips = true, int overrideInstanceID = -1)
        {
            // This atlas only support 2D texture so we only blit 2D textures
            if (Is2D(texture))
                Blit2DTexture(cmd, scaleOffset, texture, sourceScaleOffset, blitMips);
        }

        public virtual void BlitOctahedralTexture(CommandBuffer cmd, Vector4 scaleOffset, Texture texture, Vector4 sourceScaleOffset, bool blitMips = true, int overrideInstanceID = -1)
        {
            // This atlas only support 2D texture so we only blit 2D textures
            if (Is2D(texture))
                BlitOctahedralTexture(cmd, scaleOffset, texture, sourceScaleOffset, blitMips);
        }

        public virtual bool AllocateTexture(CommandBuffer cmd, ref Vector4 scaleOffset, Texture texture, int width, int height, int overrideInstanceID = -1)
        {
            bool allocated = AllocateTextureWithoutBlit(texture, width, height, ref scaleOffset);

            if (allocated)
            {
                BlitTexture(cmd, scaleOffset, texture, fullScaleOffset);
                MarkGPUTextureValid(overrideInstanceID != -1 ? overrideInstanceID : GetTextureID(texture), true); // texture is up to date
            }

            return allocated;
        }

        public bool AllocateTextureWithoutBlit(Texture texture, int width, int height, ref Vector4 scaleOffset)
            => AllocateTextureWithoutBlit(texture.GetInstanceID(), width, height, ref scaleOffset);

        public virtual bool AllocateTextureWithoutBlit(int instanceId, int width, int height, ref Vector4 scaleOffset)
        {
            scaleOffset = Vector4.zero;

            if (m_AtlasAllocator.Allocate(ref scaleOffset, width, height))
            {
                scaleOffset.Scale(new Vector4(1.0f / m_Width, 1.0f / m_Height, 1.0f / m_Width, 1.0f / m_Height));
                m_AllocationCache.Add(instanceId, scaleOffset);
                MarkGPUTextureInvalid(instanceId); // the texture data haven't been uploaded
                m_TextureHashes[instanceId] = -1;
                return true;
            }
            else
            {
                return false;
            }
        }

        protected int GetTextureHash(Texture texture)
        {
            int hash = texture.GetHashCode();

            unchecked
            {
#if UNITY_EDITOR
                hash = 23 * hash + texture.imageContentsHash.GetHashCode();
#endif
                hash = 23*hash + texture.GetInstanceID().GetHashCode();
                hash = 23*hash + texture.graphicsFormat.GetHashCode();
                hash = 23*hash + texture.wrapMode.GetHashCode();
                hash = 23*hash + texture.width.GetHashCode();
                hash = 23*hash + texture.height.GetHashCode();
                hash = 23*hash + texture.filterMode.GetHashCode();
                hash = 23*hash + texture.anisoLevel.GetHashCode();
                hash = 23*hash + texture.mipmapCount.GetHashCode();
            }

            return hash;
        }

        protected int GetTextureHash(Texture textureA, Texture textureB)
        {
            int hash = GetTextureHash(textureA) + 23 * GetTextureHash(textureB);
            return hash;
        }


        public int GetTextureID(Texture texture)
        {
            return texture.GetInstanceID();
        }

        public int GetTextureID(Texture textureA, Texture textureB)
        {
            return GetTextureID(textureA) + 23* GetTextureID(textureB);
        }

        public bool IsCached(out Vector4 scaleOffset, Texture textureA, Texture textureB)
            => IsCached(out scaleOffset, GetTextureID(textureA, textureB));

        public bool IsCached(out Vector4 scaleOffset, Texture texture)
            => IsCached(out scaleOffset, GetTextureID(texture));

        public bool IsCached(out Vector4 scaleOffset, int id)
            => m_AllocationCache.TryGetValue(id, out scaleOffset);

        public virtual bool NeedsUpdate(Texture texture, bool needMips = false)
        {
            RenderTexture   rt = texture as RenderTexture;
            int             key = GetTextureID(texture);
            int             textureHash = GetTextureHash(texture);

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
                return value == 0 || (needMips && value == 1);

            return false;
        }

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
                    else if (rtB.updateCount != updateCount) // implicitly rtB != null
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
                return value == 0 || (needMips && value == 1);

            return false;
        }

        public virtual bool AddTexture(CommandBuffer cmd, ref Vector4 scaleOffset, Texture texture)
        {
            if (IsCached(out scaleOffset, texture))
                return true;

            // We only support 2D texture in this class, support for other textures are provided by child classes (ex: PowerOfTwoTextureAtlas)
            if (!Is2D(texture))
                return false;

            return AllocateTexture(cmd, ref scaleOffset, texture, texture.width, texture.height);
        }

        public virtual bool UpdateTexture(CommandBuffer cmd, Texture oldTexture, Texture newTexture, ref Vector4 scaleOffset, Vector4 sourceScaleOffset, bool updateIfNeeded = true, bool blitMips = true)
        {
            // In case the old texture is here, we Blit the new one at the scale offset of the old one
            if (IsCached(out scaleOffset, oldTexture))
            {
                if (updateIfNeeded && NeedsUpdate(newTexture))
                {
                    BlitTexture(cmd, scaleOffset, newTexture, sourceScaleOffset, blitMips);
                    MarkGPUTextureValid(GetTextureID(newTexture), blitMips); // texture is up to date
                }
                return true;
            }
            else // else we try to allocate the updated texture
            {
                return AllocateTexture(cmd, ref scaleOffset, newTexture, newTexture.width, newTexture.height);
            }
        }

        public virtual bool UpdateTexture(CommandBuffer cmd, Texture texture, ref Vector4 scaleOffset, bool updateIfNeeded = true, bool blitMips = true)
            => UpdateTexture(cmd, texture, texture, ref scaleOffset, fullScaleOffset, updateIfNeeded, blitMips);
        internal bool EnsureTextureSlot(out bool isUploadNeeded, ref Vector4 scaleBias, int key, int width, int height)
        {
            isUploadNeeded = false;
            if (m_AllocationCache.TryGetValue(key, out scaleBias)) { return true; }
            if (!m_AtlasAllocator.Allocate(ref scaleBias, width, height)) { return false; }
            isUploadNeeded = true;
            scaleBias.Scale(new Vector4(1.0f / m_Width, 1.0f / m_Height, 1.0f / m_Width, 1.0f / m_Height));
            m_AllocationCache.Add(key, scaleBias);
            return true;
        }
    }
}
