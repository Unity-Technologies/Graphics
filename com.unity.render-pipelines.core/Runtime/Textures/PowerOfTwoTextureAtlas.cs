using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Texture atlas with rectangular power of two size.
    /// </summary>
    public class PowerOfTwoTextureAtlas : Texture2DAtlas
    {
        int m_MipPadding;
        const float k_MipmapFactorApprox = 1.33f;

        private Dictionary<int, Vector2Int> m_RequestedTextures = new Dictionary<int, Vector2Int>();

        /// <summary>
        /// Create a new texture atlas, must have power of two size.
        /// </summary>
        /// <param name="size">The size of the atlas in pixels. Must be power of two.</param>
        /// <param name="mipPadding">Amount of mip padding.</param>
        /// <param name="format">Atlas texture format</param>
        /// <param name="filterMode">Atlas texture filter mode.</param>
        /// <param name="name">Name of the atlas</param>
        /// <param name="useMipMap">Use mip maps</param>
        public PowerOfTwoTextureAtlas(int size, int mipPadding, GraphicsFormat format, FilterMode filterMode = FilterMode.Point, string name = "", bool useMipMap = true)
            : base(size, size, format, filterMode, true, name, useMipMap)
        {
            this.m_MipPadding = mipPadding;

            // Check if size is a power of two
            if ((size & (size - 1)) != 0)
                Debug.Assert(false, "Power of two atlas was constructed with non power of two size: " + size);
        }

        public int mipPadding => m_MipPadding;

        int GetTexturePadding() => (int)Mathf.Pow(2, m_MipPadding) * 2;

        void Blit2DTexturePadding(CommandBuffer cmd, Vector4 scaleOffset, Texture texture, Vector4 sourceScaleOffset, bool blitMips = true)
        {
            int mipCount = GetTextureMipmapCount(texture.width, texture.height);
            int pixelPadding = GetTexturePadding();
            Vector2 textureSize = GetPowerOfTwoTextureSize(texture);
            bool bilinear = texture.filterMode != FilterMode.Point;

            if (!blitMips)
                mipCount = 1;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(CoreProfileId.BlitTextureInPotAtlas)))
            {
                for (int mipLevel = 0; mipLevel < mipCount; mipLevel++)
                {
                    cmd.SetRenderTarget(m_AtlasTexture, mipLevel);
                    Blitter.BlitQuadWithPadding(cmd, texture, textureSize, sourceScaleOffset, scaleOffset, mipLevel, bilinear, pixelPadding);
                }
            }
        }

        void Blit2DTexturePaddingMultiply(CommandBuffer cmd, Vector4 scaleOffset, Texture texture, Vector4 sourceScaleOffset, bool blitMips = true)
        {
            int mipCount = GetTextureMipmapCount(texture.width, texture.height);
            int pixelPadding = GetTexturePadding();
            Vector2 textureSize = GetPowerOfTwoTextureSize(texture);
            bool bilinear = texture.filterMode != FilterMode.Point;

            if (!blitMips)
                mipCount = 1;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(CoreProfileId.BlitTextureInPotAtlas)))
            {
                for (int mipLevel = 0; mipLevel < mipCount; mipLevel++)
                {
                    cmd.SetRenderTarget(m_AtlasTexture, mipLevel);
                    Blitter.BlitQuadWithPaddingMultiply(cmd, texture, textureSize, sourceScaleOffset, scaleOffset, mipLevel, bilinear, pixelPadding);
                }
            }
        }

        void BlitOctahedralTexturePadding(CommandBuffer cmd, Vector4 scaleOffset, Texture texture, Vector4 sourceScaleOffset, bool blitMips = true)
        {
            int mipCount = GetTextureMipmapCount(texture.width, texture.height);
            int pixelPadding = GetTexturePadding();
            Vector2 textureSize = GetPowerOfTwoTextureSize(texture);
            bool bilinear = texture.filterMode != FilterMode.Point;

            if (!blitMips)
                mipCount = 1;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(CoreProfileId.BlitTextureInPotAtlas)))
            {
                for (int mipLevel = 0; mipLevel < mipCount; mipLevel++)
                {
                    cmd.SetRenderTarget(m_AtlasTexture, mipLevel);
                    Blitter.BlitOctahedralWithPadding(cmd, texture, textureSize, sourceScaleOffset, scaleOffset, mipLevel, bilinear, pixelPadding);
                }
            }
        }

        void BlitOctahedralTexturePaddingMultiply(CommandBuffer cmd, Vector4 scaleOffset, Texture texture, Vector4 sourceScaleOffset, bool blitMips = true)
        {
            int mipCount = GetTextureMipmapCount(texture.width, texture.height);
            int pixelPadding = GetTexturePadding();
            Vector2 textureSize = GetPowerOfTwoTextureSize(texture);
            bool bilinear = texture.filterMode != FilterMode.Point;

            if (!blitMips)
                mipCount = 1;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(CoreProfileId.BlitTextureInPotAtlas)))
            {
                for (int mipLevel = 0; mipLevel < mipCount; mipLevel++)
                {
                    cmd.SetRenderTarget(m_AtlasTexture, mipLevel);
                    Blitter.BlitOctahedralWithPaddingMultiply(cmd, texture, textureSize, sourceScaleOffset, scaleOffset, mipLevel, bilinear, pixelPadding);
                }
            }
        }

        /// <summary>
        /// Blit texture into the atlas with padding.
        /// </summary>
        /// <param name="cmd">Target command buffer for graphics commands.</param>
        /// <param name="scaleOffset">Destination scale (.xy) and offset (.zw)</param>
        /// <param name="texture">Source Texture</param>
        /// <param name="sourceScaleOffset">Source scale (.xy) and offset(.zw).</param>
        /// <param name="blitMips">Blit mip maps.</param>
        /// <param name="overrideInstanceID">Override texture instance ID.</param>
        public override void BlitTexture(CommandBuffer cmd, Vector4 scaleOffset, Texture texture, Vector4 sourceScaleOffset, bool blitMips = true, int overrideInstanceID = -1)
        {
            // We handle ourself the 2D blit because cookies needs mipPadding for trilinear filtering
            if (Is2D(texture))
            {
                Blit2DTexturePadding(cmd, scaleOffset, texture, sourceScaleOffset, blitMips);
                MarkGPUTextureValid(overrideInstanceID != -1 ? overrideInstanceID : texture.GetInstanceID(), blitMips);
            }
        }

        /// <summary>
        /// Blit texture into the atlas with padding and blending.
        /// </summary>
        /// <param name="cmd">Target command buffer for graphics commands.</param>
        /// <param name="scaleOffset">Destination scale (.xy) and offset (.zw)</param>
        /// <param name="texture">Source Texture</param>
        /// <param name="sourceScaleOffset">Source scale (.xy) and offset(.zw).</param>
        /// <param name="blitMips">Blit mip maps.</param>
        /// <param name="overrideInstanceID">Override texture instance ID.</param>
        public void BlitTextureMultiply(CommandBuffer cmd, Vector4 scaleOffset, Texture texture, Vector4 sourceScaleOffset, bool blitMips = true, int overrideInstanceID = -1)
        {
            // We handle ourself the 2D blit because cookies needs mipPadding for trilinear filtering
            if (Is2D(texture))
            {
                Blit2DTexturePaddingMultiply(cmd, scaleOffset, texture, sourceScaleOffset, blitMips);
                MarkGPUTextureValid(overrideInstanceID != -1 ? overrideInstanceID : texture.GetInstanceID(), blitMips);
            }
        }

        /// <summary>
        /// Blit octahedral texture into the atlas with padding.
        /// </summary>
        /// <param name="cmd">Target command buffer for graphics commands.</param>
        /// <param name="scaleOffset">Destination scale (.xy) and offset (.zw)</param>
        /// <param name="texture">Source Texture</param>
        /// <param name="sourceScaleOffset">Source scale (.xy) and offset(.zw).</param>
        /// <param name="blitMips">Blit mip maps.</param>
        /// <param name="overrideInstanceID">Override texture instance ID.</param>
        public override void BlitOctahedralTexture(CommandBuffer cmd, Vector4 scaleOffset, Texture texture, Vector4 sourceScaleOffset, bool blitMips = true, int overrideInstanceID = -1)
        {
            // We handle ourself the 2D blit because cookies needs mipPadding for trilinear filtering
            if (Is2D(texture))
            {
                BlitOctahedralTexturePadding(cmd, scaleOffset, texture, sourceScaleOffset, blitMips);
                MarkGPUTextureValid(overrideInstanceID != -1 ? overrideInstanceID : texture.GetInstanceID(), blitMips);
            }
        }

        /// <summary>
        /// Blit octahedral texture into the atlas with padding.
        /// </summary>
        /// <param name="cmd">Target command buffer for graphics commands.</param>
        /// <param name="scaleOffset">Destination scale (.xy) and offset (.zw)</param>
        /// <param name="texture">Source Texture</param>
        /// <param name="sourceScaleOffset">Source scale (.xy) and offset(.zw).</param>
        /// <param name="blitMips">Blit mip maps.</param>
        /// <param name="overrideInstanceID">Override texture instance ID.</param>
        public void BlitOctahedralTextureMultiply(CommandBuffer cmd, Vector4 scaleOffset, Texture texture, Vector4 sourceScaleOffset, bool blitMips = true, int overrideInstanceID = -1)
        {
            // We handle ourself the 2D blit because cookies needs mipPadding for trilinear filtering
            if (Is2D(texture))
            {
                BlitOctahedralTexturePaddingMultiply(cmd, scaleOffset, texture, sourceScaleOffset, blitMips);
                MarkGPUTextureValid(overrideInstanceID != -1 ? overrideInstanceID : texture.GetInstanceID(), blitMips);
            }
        }

        void TextureSizeToPowerOfTwo(Texture texture, ref int width, ref int height)
        {
            // Change the width and height of the texture to be power of two
            width = Mathf.NextPowerOfTwo(width);
            height = Mathf.NextPowerOfTwo(height);
        }

        Vector2 GetPowerOfTwoTextureSize(Texture texture)
        {
            int width = texture.width, height = texture.height;

            TextureSizeToPowerOfTwo(texture, ref width, ref height);
            return new Vector2(width, height);
        }

        // Override the behavior when we add a texture so all non-pot textures are blitted to a pot target zone
        /// <summary>
        /// Allocate space from the atlas for a texture and copy texture contents into the atlas.
        /// </summary>
        /// <param name="cmd">Target command buffer for graphics commands.</param>
        /// <param name="scaleOffset">Allocated scale (.xy) and offset (.zw)</param>
        /// <param name="texture">Source Texture</param>
        /// <param name="width">Request width in pixels.</param>
        /// <param name="height">Request height in pixels.</param>
        /// <param name="overrideInstanceID">Override texture instance ID.</param>
        /// <returns>True on success, false otherwise.</returns>
        public override bool AllocateTexture(CommandBuffer cmd, ref Vector4 scaleOffset, Texture texture, int width, int height, int overrideInstanceID = -1)
        {
            // This atlas only supports square textures
            if (height != width)
            {
                Debug.LogError("Can't place " + texture + " in the atlas " + m_AtlasTexture.name + ": Only squared texture are allowed in this atlas.");
                return false;
            }

            TextureSizeToPowerOfTwo(texture, ref height, ref width);

            return base.AllocateTexture(cmd, ref scaleOffset, texture, width, height);
        }

        /// <summary>
        /// Clear tracked requested textures.
        /// </summary>
        public void ResetRequestedTexture() => m_RequestedTextures.Clear();

        /// <summary>
        /// Reserve space from atlas for a texture.
        /// </summary>
        /// <param name="texture">Source texture.</param>
        /// <returns>True on success, false otherwise.</returns>
        public bool ReserveSpace(Texture texture) => ReserveSpace(texture, texture.width, texture.height);

        /// <summary>
        /// Reserve space from atlas for a texture.
        /// </summary>
        /// <param name="texture">Source texture.</param>
        /// <param name="width">Request width in pixels.</param>
        /// <param name="height">Request height in pixels.</param>
        /// <returns>True on success, false otherwise.</returns>
        public bool ReserveSpace(Texture texture, int width, int height)
            => ReserveSpace(GetTextureID(texture), width, height);


        /// <summary>
        /// Reserve space from atlas for a texture.
        /// Pass width and height for CubeMap (use 2*width) & Texture2D (use width).
        /// </summary>
        /// <param name="textureA">Source texture A.</param>
        /// <param name="textureB">Source texture B.</param>
        /// <param name="width">Request width in pixels.</param>
        /// <param name="height">Request height in pixels.</param>
        /// <returns>True on success, false otherwise.</returns>
        public bool ReserveSpace(Texture textureA, Texture textureB, int width, int height)
            => ReserveSpace(GetTextureID(textureA, textureB), width, height);

        /// <summary>
        /// Reserve space from atlas for a texture.
        /// </summary>
        /// <param name="id">Source texture ID.</param>
        /// <param name="width">Request width in pixels.</param>
        /// <param name="height">Request height in pixels.</param>
        /// <returns>True on success, false otherwise.</returns>
        bool ReserveSpace(int id, int width, int height)
        {
            m_RequestedTextures[id] = new Vector2Int(width, height);

            // Cookie texture resolution changing between frame is a special case, so we handle it here.
            // The texture will be re-allocated and may cause holes in the atlas texture, which is fine
            // because when it doesn't have any more space, it will re-layout the texture correctly.
            var cachedSize = GetCachedTextureSize(id);
            if (!IsCached(out _, id) || cachedSize.x != width || cachedSize.y != height)
            {
                Vector4 scaleBias = Vector4.zero;
                if (!AllocateTextureWithoutBlit(id, width, height, ref scaleBias))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// sort all the requested allocation from biggest to smallest and re-insert them.
        /// This function does not moves the textures in the atlas, it only changes their coordinates
        /// </summary>
        /// <returns>True if all textures have successfully been re-inserted in the atlas</returns>
        public bool RelayoutEntries()
        {
            var entries = new List<(int instanceId, Vector2Int size)>();

            foreach (var entry in m_RequestedTextures)
                entries.Add((entry.Key, entry.Value));
            ResetAllocator();

            // Sort entries from biggest to smallest
            entries.Sort((c1, c2) => {
                return c2.size.magnitude.CompareTo(c1.size.magnitude);
            });

            bool success = true;
            Vector4 newScaleOffset = Vector4.zero;
            foreach (var e in entries)
                success &= AllocateTextureWithoutBlit(e.instanceId, e.size.x, e.size.y, ref newScaleOffset);

            return success;
        }

        /// <summary>
        /// Get cache size in bytes.
        /// </summary>
        /// <param name="nbElement"></param>
        /// <param name="resolution">Atlas resolution (square).</param>
        /// <param name="hasMipmap">Atlas uses mip maps.</param>
        /// <param name="format">Atlas format.</param>
        /// <returns></returns>
        public static long GetApproxCacheSizeInByte(int nbElement, int resolution, bool hasMipmap, GraphicsFormat format)
            => (long)(nbElement * resolution * resolution * (double)((hasMipmap ? k_MipmapFactorApprox : 1.0f) * GraphicsFormatUtility.GetBlockSize(format)));

        /// <summary>
        /// Compute the max size of a power of two atlas for a given size in byte (weight).
        /// </summary>
        /// <param name="weight">Atlas size in bytes.</param>
        /// <param name="hasMipmap">Atlas uses mip maps.</param>
        /// <param name="format">Atlas format.</param>
        /// <returns></returns>
        public static int GetMaxCacheSizeForWeightInByte(int weight, bool hasMipmap, GraphicsFormat format)
        {
            float bytePerPixel = (float)GraphicsFormatUtility.GetBlockSize(format) * (hasMipmap ? k_MipmapFactorApprox : 1.0f);
            var maxAtlasSquareSize = Mathf.Sqrt((float)weight / bytePerPixel);
            return CoreUtils.PreviousPowerOfTwo((int)maxAtlasSquareSize);
        }
    }
}
