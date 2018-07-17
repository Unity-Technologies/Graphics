using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEngine.Experimental.Rendering
{
    public class PowerOfTwoTextureAtlas : Texture2DAtlas
    {
        int m_MipPadding;

        public PowerOfTwoTextureAtlas(int size, int mipPadding, RenderTextureFormat format, bool generateMipMaps = true, FilterMode filterMode = FilterMode.Point)
            : base(size, size, format, generateMipMaps, filterMode, true)
        {
            m_MipPadding = mipPadding;

            // Check if size is a power of two
            if ((size & (size - 1)) != 0)
                Debug.Assert(false, "Power of two atlas was constructed with non power of two size: " + size);
        }

        int GetTexturePadding(int mipCount)
        {
            return (int)Mathf.Pow(2, Mathf.Min(mipCount, m_MipPadding)) * 2;
        }
        
        void BlitCubemap(CommandBuffer cmd, Vector4 scaleBias, Texture texture)
        {
            int mipCount = GetTextureMipmapCount(texture);
            int padding = GetTexturePadding(mipCount);
            
            for (int mipLevel = 0; mipLevel < mipCount; mipLevel++)
            {
                cmd.SetRenderTarget(m_AtlasTexture, mipLevel);
                HDUtils.BlitCubeLatlong(cmd, texture, new Vector4(1, 1, 0 ,0), scaleBias, mipLevel, true, padding);
            }
        }

        void Blit2DTextureRepeat(CommandBuffer cmd, Vector4 scaleBias, Texture texture)
        {
            int mipCount = GetTextureMipmapCount(texture);
            int padding = GetTexturePadding(mipCount);
            
            for (int mipLevel = 0; mipLevel < mipCount; mipLevel++)
            {
                cmd.SetRenderTarget(m_AtlasTexture, mipLevel);
                HDUtils.BlitPaddedQuad(cmd, texture, new Vector4(1, 1, 0, 0), scaleBias, mipLevel, true, padding);
            }
        }

        bool IsCubemap(Texture texture)
        {
            CustomRenderTexture crt = texture as CustomRenderTexture;

            return (texture is Cubemap || (crt != null && crt.dimension == TextureDimension.Cube));
        }

        protected override void BlitTexture(CommandBuffer cmd, Vector4 scaleBias, Texture texture)
        {
            // We handle ourself the 2D blit because cookies needs padding for trilinear filtering
            if (Is2D(texture))
                Blit2DTextureRepeat(cmd, scaleBias, texture);

            if (IsCubemap(texture))
                BlitCubemap(cmd, scaleBias, texture);
        }

        protected override bool AllocateTexture(CommandBuffer cmd, ref Vector4 scaleBias, Texture texture, int width, int height)
        {
            // This atlas only supports square textures
            if (height != width)
                return false;

            // Change the width and height of the texture to be power of two
            width = Mathf.NextPowerOfTwo(width);
            height = Mathf.NextPowerOfTwo(height);

            if (IsCubemap(texture))
            {
                // For the cubemap, faces are organized like this:
                // +-----+
                // |3|4|5|
                // +-----+
                // |0|1|2|
                // +-----+
                // Correct Latlong texture size
                width *= 2;
                height *= 2;
            }

            return base.AllocateTexture(cmd, ref scaleBias, texture, width, height);
        }
        
        public override bool AddTexture(CommandBuffer cmd, ref Vector4 scaleBias, Texture texture)
        {
            // If the texture is 2D or already chached we have nothing to do in this function
            if (base.AddTexture(cmd, ref scaleBias, texture))
                return true;
            
            // We only accept cubemaps and 2D textures for this atlas
            if (!IsCubemap(texture))
                return false;

            bool b = AllocateTexture(cmd, ref scaleBias, texture, texture.width, texture.height);

            return b;
        }
    }
}