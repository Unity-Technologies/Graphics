using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering.HDPipeline;

namespace UnityEngine.Experimental.Rendering
{
    public class PowerOfTwoTextureAtlas : Texture2DAtlas
    {
        public PowerOfTwoTextureAtlas(int size, RenderTextureFormat format, bool generateMipMaps = true, FilterMode filterMode = FilterMode.Point)
            : base(size, size, format, generateMipMaps, filterMode, true)
        {
            // Check if size is a power of two
            if ((size & (size - 1)) != 0)
                Debug.Assert(false, "Power of two atlas was constructed with non power of two size: " + size);
        }
        
        void BlitCubemap(CommandBuffer cmd, Vector4 scaleBias, Texture texture)
        {
            int mipCount = GetTextureMipmapCount(texture);

            scaleBias.x /= 3;
            scaleBias.y /= 2;
            for (int mipLevel = 0; mipLevel < mipCount; mipLevel++)
            {
                cmd.SetRenderTarget(m_AtlasTexture, mipLevel);
                HDUtils.BlitCube(cmd, texture, new Vector4(1, 1, 0, 0), scaleBias, mipLevel, false);
            }
        }

        void Blit2DTextureRepeat(CommandBuffer cmd, Vector4 scaleBias, Texture texture)
        {
            int mipCount = GetTextureMipmapCount(texture);
            
            for (int mipLevel = 0; mipLevel < mipCount; mipLevel++)
            {
                cmd.SetRenderTarget(m_AtlasTexture, mipLevel);
                HDUtils.BlitPaddedQuad(cmd, texture, new Vector4(1, 1, 0, 0), scaleBias, mipLevel, true);
            }
        }

        bool IsCubemap(Texture texture)
        {
            CustomRenderTexture crt = texture as CustomRenderTexture;

            return (texture is Cubemap || (crt != null && crt.dimension == TextureDimension.Cube));
        }

        protected override void BlitTexture(CommandBuffer cmd, Vector4 scaleBias, Texture texture)
        {
            if (Is2D(texture))
            {
                // If the texture is in repeat mode, we blit it with a repeat padding to handle border filtering
                if (texture.wrapMode == TextureWrapMode.Repeat)
                    Blit2DTextureRepeat(cmd, scaleBias, texture);
                else
                    Blit2DTexture(cmd, scaleBias, texture);
            }

            // 2D textures are handled by base.BlitTexture so here we blit CubeMaps
            if (IsCubemap(texture))
                BlitCubemap(cmd, scaleBias, texture);
        }

        protected override bool AllocateTexture(CommandBuffer cmd, ref Vector4 scaleBias, Texture texture, int width, int height)
        {
            // This atlas only supports square textures
            if (height != width)
                return false;

            if (texture.wrapMode == TextureWrapMode.Repeat)
            {
                width += 8;
                height += 8;
            }

            // Compute the next highest power of two (ref https://graphics.stanford.edu/~seander/bithacks.html#RoundUpPowerOf2)
            int p = height;
            p--;
            p |= p >> 1;
            p |= p >> 2;
            p |= p >> 4;
            p |= p >> 8;
            p |= p >> 16;
            p++;

            // Change the width and height of the texture to be power of two
            width = p;
            height = p;

            if (IsCubemap(texture))
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