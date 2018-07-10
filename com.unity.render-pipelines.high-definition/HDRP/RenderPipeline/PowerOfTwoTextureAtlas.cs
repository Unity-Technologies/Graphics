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
            : base(size, size, format, generateMipMaps, filterMode) {}
        
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

        bool IsCubemap(Texture texture)
        {
            CustomRenderTexture crt = texture as CustomRenderTexture;

            return (texture is Cubemap || (crt != null && crt.dimension == TextureDimension.Cube));
        }

        protected override void BlitTexture(CommandBuffer cmd, Vector4 scaleBias, Texture texture)
        {
            base.BlitTexture(cmd, scaleBias, texture);

            // 2D textures are handled by base.BlitTexture so here we blit CubeMaps
            if (IsCubemap(texture))
                BlitCubemap(cmd, scaleBias, texture);
        }

        protected override bool AllocateTexture(CommandBuffer cmd, ref Vector4 scaleBias, Texture texture, int width, int height)
        {
            // Change the width and height of the texture to bepower of two

            return base.AllocateTexture(cmd, ref scaleBias, texture, width, height);
        }
        
        public override bool AddTexture(CommandBuffer cmd, ref Vector4 scaleBias, Texture texture)
        {
            // If the texture is 2D or already chached we have nothing to do in this function
            if (base.AddTexture(cmd, ref scaleBias, texture))
                return true;
            
            if (!IsCubemap(texture))
                return false;
            
            // For the cubemap, faces are organized like this:
            // +-----+
            // |3|4|5|
            // +-----+
            // |0|1|2|
            // +-----+
            return AllocateTexture(cmd, ref scaleBias, texture, texture.width * 3, texture.height * 2);
        }
    }
}