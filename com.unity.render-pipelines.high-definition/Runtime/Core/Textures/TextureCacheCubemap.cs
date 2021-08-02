using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class TextureCacheCubemap : TextureCache
    {
        private RenderTexture m_Cache;

        const int k_NbFace = 6;

        // the member variables below are only in use when TextureCache.supportsCubemapArrayTextures is false
        Texture2DArray m_CacheNoCubeArray;
        RenderTexture[] m_StagingRTs;
        int m_NumPanoMipLevels;
        Material m_CubeBlitMaterial;
        int m_CubeMipLevelPropName;
        int m_cubeSrcTexPropName;
        Material m_BlitCubemapFaceMaterial;
        MaterialPropertyBlock m_BlitCubemapFaceProperties;

        public TextureCacheCubemap(string cacheName = "", int sliceSize = 1)
            : base(cacheName, sliceSize)
        {
            var res = HDRenderPipeline.defaultAsset.renderPipelineResources;
            m_BlitCubemapFaceMaterial = CoreUtils.CreateEngineMaterial(res.shaders.blitCubeTextureFacePS);
            m_BlitCubemapFaceProperties = new MaterialPropertyBlock();
        }

        override public bool IsCreated()
        {
            return m_Cache.IsCreated();
        }

        override protected bool TransferToSlice(CommandBuffer cmd, int sliceIndex, Texture[] textureArray)
        {
            if (!TextureCache.supportsCubemapArrayTextures)
                return TransferToPanoCache(cmd, sliceIndex, textureArray);
            else
            {
                // Make sure the array is not null or empty and that the first texture is a render-texture or a texture2D
                if (textureArray == null || textureArray.Length == 0)
                {
                    return false;
                }

                // First check here is to check if all the sub-texture have the same size
                for (int texIDx = 1; texIDx < textureArray.Length; ++texIDx)
                {
                    // We cannot update if the textures if they don't have the same size or not the right type
                    if (textureArray[texIDx].width != textureArray[0].width || textureArray[texIDx].height != textureArray[0].height)
                    {
                        Debug.LogWarning("All the sub-textures should have the same dimensions to be handled by the texture cache.");
                        return false;
                    }
                }

                var mismatch = (m_Cache.width != textureArray[0].width) || (m_Cache.height != textureArray[0].height);

                if (textureArray[0] is Cubemap)
                {
                    mismatch |= (m_Cache.graphicsFormat != (textureArray[0] as Cubemap).graphicsFormat);
                }

                for (int texIDx = 0; texIDx < textureArray.Length; ++texIDx)
                {
                    if (mismatch)
                    {
                        m_BlitCubemapFaceProperties.SetTexture(HDShaderIDs._InputTex, textureArray[texIDx]);
                        m_BlitCubemapFaceProperties.SetFloat(HDShaderIDs._LoD, 0);
                        for (int f = 0; f < 6; f++)
                        {
                            m_BlitCubemapFaceProperties.SetFloat(HDShaderIDs._FaceIndex, (float)f);
                            CoreUtils.SetRenderTarget(cmd, m_Cache, ClearFlag.None, Color.black, depthSlice: 6 * (m_SliceSize * sliceIndex + texIDx) + f);
                            CoreUtils.DrawFullScreen(cmd, m_BlitCubemapFaceMaterial, m_BlitCubemapFaceProperties);
                        }
                    }
                    else
                    {
                        for (int f = 0; f < 6; f++)
                            cmd.CopyTexture(textureArray[texIDx], f, m_Cache, 6 * (m_SliceSize * sliceIndex + texIDx) + f);
                    }
                }

                return true;
            }
        }

        public override Texture GetTexCache()
        {
            return !TextureCache.supportsCubemapArrayTextures ? (Texture)m_CacheNoCubeArray : m_Cache;
        }

        public bool AllocTextureArray(int numCubeMaps, int width, GraphicsFormat format, bool isMipMapped, Material cubeBlitMaterial)
        {
            var res = AllocTextureArray(numCubeMaps);
            m_NumMipLevels = GetNumMips(width, width);      // will calculate same way whether we have cube array or not

            if (!TextureCache.supportsCubemapArrayTextures)
            {
                m_CubeBlitMaterial = cubeBlitMaterial;

                int panoWidthTop = 4 * width;
                int panoHeightTop = 2 * width;

                // create panorama 2D array. Hardcoding the render target for now. No convenient way atm to
                // map from TextureFormat to RenderTextureFormat and don't want to deal with sRGB issues for now.
                m_CacheNoCubeArray = new Texture2DArray(panoWidthTop, panoHeightTop, numCubeMaps, format, isMipMapped ? TextureCreationFlags.MipChain : TextureCreationFlags.None)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Repeat,
                    wrapModeV = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Trilinear,
                    anisoLevel = 0,
                    name = CoreUtils.GetTextureAutoName(panoWidthTop, panoHeightTop, format, TextureDimension.Tex2DArray, depth: numCubeMaps, name: m_CacheName)
                };

                m_NumPanoMipLevels = isMipMapped ? GetNumMips(panoWidthTop, panoHeightTop) : 1;
                m_StagingRTs = new RenderTexture[m_NumPanoMipLevels];
                for (int m = 0; m < m_NumPanoMipLevels; m++)
                {
                    m_StagingRTs[m] = new RenderTexture(Mathf.Max(1, panoWidthTop >> m), Mathf.Max(1, panoHeightTop >> m), 0, format) { hideFlags = HideFlags.HideAndDontSave };
                    m_StagingRTs[m].name = CoreUtils.GetRenderTargetAutoName(Mathf.Max(1, panoWidthTop >> m), Mathf.Max(1, panoHeightTop >> m), 1, format, String.Format("PanaCache{0}", m));
                }

                if (m_CubeBlitMaterial)
                {
                    m_CubeMipLevelPropName = Shader.PropertyToID("_cubeMipLvl");
                    m_cubeSrcTexPropName = Shader.PropertyToID("_srcCubeTexture");
                }
            }
            else
            {
                var desc = new RenderTextureDescriptor(width, width, format, 0)
                {
                    dimension = TextureDimension.CubeArray,
                    volumeDepth = numCubeMaps * 6, // We need to multiply by the face count of a cubemap here
                    autoGenerateMips = false,
                    useMipMap = isMipMapped,
                    msaaSamples = 1,
                };

                m_Cache = new RenderTexture(desc)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Trilinear,
                    anisoLevel = 0, // It is important to set 0 here, else unity force anisotropy filtering
                    name = CoreUtils.GetTextureAutoName(width, width, format, desc.dimension, depth: numCubeMaps, name: m_CacheName, mips: isMipMapped)
                };

                // We need to clear the content in case it is read on first frame, since on console we have no guarantee that
                // the content won't be NaN
                ClearCache();
                m_Cache.Create();
            }

            return res;
        }

        internal void ClearCache()
        {
            var desc = m_Cache.descriptor;
            bool isMipped = desc.useMipMap;
            int mipCount = isMipped ? GetNumMips(desc.width, desc.height) : 1;
            for (int mipIdx = 0; mipIdx < mipCount; ++mipIdx)
            {
                Graphics.SetRenderTarget(m_Cache, mipIdx, CubemapFace.Unknown, -1);
                GL.Clear(false, true, Color.clear);
            }
        }

        public void Release()
        {
            if (m_CacheNoCubeArray)
            {
                CoreUtils.Destroy(m_CacheNoCubeArray);
                for (int m = 0; m < m_NumPanoMipLevels; m++)
                {
                    m_StagingRTs[m].Release();
                }
                m_StagingRTs = null;
                CoreUtils.Destroy(m_CubeBlitMaterial);
            }

            CoreUtils.Destroy(m_BlitCubemapFaceMaterial);

            CoreUtils.Destroy(m_Cache);
        }

        private bool TransferToPanoCache(CommandBuffer cmd, int sliceIndex, Texture[] textureArray)
        {
            for(int texIdx = 0; texIdx < textureArray.Length; ++texIdx)
            {
                m_CubeBlitMaterial.SetTexture(m_cubeSrcTexPropName, textureArray[texIdx]);
                for (int m = 0; m < m_NumPanoMipLevels; m++)
                {
                    m_CubeBlitMaterial.SetInt(m_CubeMipLevelPropName, Mathf.Min(m_NumMipLevels - 1, m));
                    cmd.Blit(null, m_StagingRTs[m], m_CubeBlitMaterial, 0);
                }

                for (int m = 0; m < m_NumPanoMipLevels; m++)
                    cmd.CopyTexture(m_StagingRTs[m], 0, 0, m_CacheNoCubeArray, m_SliceSize * sliceIndex + texIdx, m);
            }
            return true;
        }

        internal static long GetApproxCacheSizeInByte(int nbElement, int resolution, int sliceSize)
        {
            return (long)((long)nbElement * resolution * resolution * k_NbFace * k_FP16SizeInByte * k_NbChannel * k_MipmapFactorApprox * sliceSize);
        }

        internal static int GetMaxCacheSizeForWeightInByte(long weight, int resolution, int sliceSize)
        {
            int theoricalResult = Mathf.FloorToInt(weight / ((long)resolution * resolution * k_NbFace * k_FP16SizeInByte * k_NbChannel * k_MipmapFactorApprox * sliceSize));
            return Mathf.Clamp(theoricalResult, 1, k_MaxSupported);
        }
    }
}
