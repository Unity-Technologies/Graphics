using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
 	class TextureCache2D : TextureCache
    {
        private RenderTexture m_Cache;

        public TextureCache2D(string cacheName = "")
            : base(cacheName)
        {
        }

        bool TextureHasMipmaps(Texture texture)
        {
            // Either the texture
            if (texture is Texture2D)
                return ((Texture2D)texture).mipmapCount > 1;
            else
                return ((RenderTexture)texture).useMipMap;
        }
        override public bool IsCreated()
        {
            return m_Cache.IsCreated();
        }
        protected override bool TransferToSlice(CommandBuffer cmd, int sliceIndex, Texture[] textureArray)
        {
            // Make sure the array is not null or empty and that the first texture is a render-texture or a texture2D
            if(textureArray == null || textureArray.Length == 0  && (!(textureArray[0] is RenderTexture) && !(textureArray[0] is Texture2D)))
            {
                return false;
            }

            // First check here is to check if all the sub-texture have the same size
            for(int texIDx = 1; texIDx < textureArray.Length; ++texIDx)
            {
                // We cannot update if the textures if they don't have the same size or not the right type
                if (textureArray[texIDx].width != textureArray[0].width || textureArray[texIDx].height != textureArray[0].height || (!(textureArray[0] is RenderTexture) && !(textureArray[0] is Texture2D)))
                {
                    Debug.LogWarning("All the sub-textures should have the same dimensions to be handled by the texture cache.");
                    return false;
                }
            }

            // Do we have a mismatch ?
            var mismatch = (m_Cache.width != textureArray[0].width) || (m_Cache.height != textureArray[0].height);

            if (textureArray[0] is Texture2D)
            {
                mismatch |= (m_Cache.graphicsFormat != (textureArray[0] as Texture2D).graphicsFormat);
            }

            for (int texIDx = 0; texIDx < textureArray.Length; ++texIDx)
            {
                if (!TextureHasMipmaps(textureArray[texIDx]))
                    Debug.LogWarning("The texture '" + textureArray[texIDx] + "' should have mipmaps to be handled by the cookie texture array");

                if (mismatch)
                {
                    cmd.Blit(textureArray[texIDx], m_Cache, 0, m_SliceSize * sliceIndex + texIDx);
                }
                else
                {
                    cmd.CopyTexture(textureArray[texIDx], 0, m_Cache, m_SliceSize * sliceIndex + texIDx);
                }
            }
            return true;
        }

        public override Texture GetTexCache()
        {
            return m_Cache;
        }

        public bool AllocTextureArray(int numTextures, int width, int height, GraphicsFormat format, bool isMipMapped)
        {
            var res = AllocTextureArray(numTextures);
            m_NumMipLevels = GetNumMips(width, height);

            var desc = new RenderTextureDescriptor(width, height, format, 0)
            {
                // autoGenerateMips is true by default
                dimension = TextureDimension.Tex2DArray,
                volumeDepth = numTextures,
                useMipMap = isMipMapped,
                msaaSamples = 1,
            };

            m_Cache = new RenderTexture(desc)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp,
                name = CoreUtils.GetTextureAutoName(width, height, format, TextureDimension.Tex2DArray, depth: numTextures, name: m_CacheName)
            };

            // We need to clear the content in case it is read on first frame, since on console we have no guarantee that
            // the content won't be NaN
            ClearCache();
            m_Cache.Create();

            return res;
        }

        internal void ClearCache()
        {
            var desc = m_Cache.descriptor;
            bool isMipped = desc.useMipMap;
            int mipCount = isMipped ? GetNumMips(desc.width, desc.height) : 1;
            for (int depthSlice = 0; depthSlice < desc.volumeDepth; ++depthSlice)
            {
                for (int mipIdx = 0; mipIdx < mipCount; ++mipIdx)
                {
                    Graphics.SetRenderTarget(m_Cache, mipIdx, CubemapFace.Unknown, depthSlice);
                    GL.Clear(false, true, Color.clear);
                }
            }
        }

        public void Release()
        {
            CoreUtils.Destroy(m_Cache);
        }

        internal static long GetApproxCacheSizeInByte(int nbElement, int resolution, int sliceSize)
        {
            return (long)((long)nbElement * resolution * resolution * k_FP16SizeInByte * k_NbChannel * k_MipmapFactorApprox * sliceSize);
        }

        internal static int GetMaxCacheSizeForWeightInByte(int weight, int resolution, int sliceSize)
        {
            int theoricalResult = Mathf.FloorToInt(weight / ((long)resolution * resolution * k_FP16SizeInByte * k_NbChannel * k_MipmapFactorApprox * sliceSize));
            return Mathf.Clamp(theoricalResult, 1, k_MaxSupported);
        }
    }
}
