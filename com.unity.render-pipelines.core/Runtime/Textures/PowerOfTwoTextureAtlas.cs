using System.Collections.Generic;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Texture atlas with rectangular power of two size.
    /// </summary>
    public class PowerOfTwoTextureAtlas : Texture2DAtlas
    {
        readonly int m_MipPadding;
        const float k_MipmapFactorApprox = 1.33f;

        private Dictionary<int, Vector2Int> m_RequestedTextures = new Dictionary<int, Vector2Int>();

        /// <summary>
        /// Create a new texture atlas, must have power of two size.
        /// </summary>
        /// <param name="size">The size of the atlas in pixels. Must be power of two.</param>
        /// <param name="mipPadding">Amount of mip padding in power of two.</param>
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

        /// <summary>
        /// Used mipmap padding size in power of two.
        /// </summary>
        public int mipPadding => m_MipPadding;

        int GetTexturePadding() => (int)Mathf.Pow(2, m_MipPadding) * 2;

        /// <summary>
        /// Get location of the actual texture data without padding in the atlas.
        /// </summary>
        /// <param name="texture">The source texture cached in the atlas.</param>
        /// <param name="scaleOffset">Cached atlas location (scale and offset) for the source texture.</param>
        /// <returns>Scale and offset for the source texture without padding.</returns>
        public Vector4 GetPayloadScaleOffset(Texture texture, in Vector4 scaleOffset)
        {
            int pixelPadding = GetTexturePadding();
            Vector2 paddingSize = Vector2.one * pixelPadding;
            Vector2 textureSize = GetPowerOfTwoTextureSize(texture);
            return GetPayloadScaleOffset(textureSize, paddingSize, scaleOffset);
        }

        /// <summary>
        /// Get location of the actual texture data without padding in the atlas.
        /// </summary>
        /// <param name="textureSize">Size of the source texture</param>
        /// <param name="paddingSize">Padding size used for the source texture. </param>
        /// <param name="scaleOffset">Cached atlas location (scale and offset) for the source texture.</param>
        /// <returns>Scale and offset for the source texture without padding.</returns>
        static public Vector4 GetPayloadScaleOffset(in Vector2 textureSize, in Vector2 paddingSize, in Vector4 scaleOffset)
        {
            // Scale, Offset is a padded atlas sub-texture rectangle.
            // Actual texture data (payload) is inset, i.e. padded inwards.
            Vector2 subTexScale = new Vector2(scaleOffset.x, scaleOffset.y);
            Vector2 subTexOffset = new Vector2(scaleOffset.z, scaleOffset.w);

            // NOTE: Should match Blit() padding calculations.
            Vector2 scalePadding = ((textureSize + paddingSize) / textureSize);            // Size of padding (sampling) rectangle relative to the payload texture.
            Vector2 offsetPadding = (paddingSize / 2.0f) / (textureSize + paddingSize);    // Padding offset in the padding rectangle

            Vector2 insetScale = subTexScale / scalePadding;                 // Size of payload rectangle in sub-tex
            Vector2 insetOffset = subTexOffset + subTexScale * offsetPadding; // Offset of payload rectangle in sub-tex

            return new Vector4(insetScale.x, insetScale.y, insetOffset.x, insetOffset.y);
        }

        private enum BlitType
        {
            Padding,
            PaddingMultiply,
            OctahedralPadding,
            OctahedralPaddingMultiply,
        }

        private void Blit2DTexture(CommandBuffer cmd, Vector4 scaleOffset, Texture texture, Vector4 sourceScaleOffset, bool blitMips, BlitType blitType)
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
                    switch (blitType)
                    {
                        case BlitType.Padding: Blitter.BlitQuadWithPadding(cmd, texture, textureSize, sourceScaleOffset, scaleOffset, mipLevel, bilinear, pixelPadding); break;
                        case BlitType.PaddingMultiply: Blitter.BlitQuadWithPaddingMultiply(cmd, texture, textureSize, sourceScaleOffset, scaleOffset, mipLevel, bilinear, pixelPadding); break;
                        case BlitType.OctahedralPadding: Blitter.BlitOctahedralWithPadding(cmd, texture, textureSize, sourceScaleOffset, scaleOffset, mipLevel, bilinear, pixelPadding); break;
                        case BlitType.OctahedralPaddingMultiply: Blitter.BlitOctahedralWithPaddingMultiply(cmd, texture, textureSize, sourceScaleOffset, scaleOffset, mipLevel, bilinear, pixelPadding); break;
                    }
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
                Blit2DTexture(cmd, scaleOffset, texture, sourceScaleOffset, blitMips, BlitType.Padding);
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
                Blit2DTexture(cmd, scaleOffset, texture, sourceScaleOffset, blitMips, BlitType.PaddingMultiply);
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
                Blit2DTexture(cmd, scaleOffset, texture, sourceScaleOffset, blitMips, BlitType.OctahedralPadding);
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
                Blit2DTexture(cmd, scaleOffset, texture, sourceScaleOffset, blitMips, BlitType.OctahedralPaddingMultiply);
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
        /// Reserves the space on the texture atlas
        /// </summary>
        /// <param name="texture">The source texture</param>
        /// <returns>True if the space is reserved</returns>
        public bool ReserveSpace(Texture texture) => ReserveSpace(texture, texture.width, texture.height);

        /// <summary>
        /// Reserves the space on the texture atlas
        /// </summary>
        /// <param name="texture">The source texture</param>
        /// <param name="width">The width</param>
        /// <param name="height">The height</param>
        /// <returns>True if the space is reserved</returns>
        public bool ReserveSpace(Texture texture, int width, int height)
            => ReserveSpace(GetTextureID(texture), width, height);

        /// <summary>
        /// Reserves the space on the texture atlas
        /// </summary>
        /// <param name="textureA">The source texture A</param>
        /// <param name="textureB">The source texture B</param>
        /// <param name="width">The width</param>
        /// <param name="height">The height</param>
        /// <returns>True if the space is reserved</returns>
        public bool ReserveSpace(Texture textureA, Texture textureB, int width, int height)
            => ReserveSpace(GetTextureID(textureA, textureB), width, height);

        /// <summary>
        /// Reserves the space on the texture atlas
        /// </summary>
        /// <param name="id">The id</param>
        /// <param name="width">The width</param>
        /// <param name="height">The height</param>
        /// <returns>True if the space is reserved</returns>
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
            entries.Sort((c1, c2) =>
            {
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
