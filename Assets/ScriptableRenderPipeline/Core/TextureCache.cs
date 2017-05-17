using UnityEngine;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace UnityEngine.Experimental.Rendering
{
    public class TextureCache2D : TextureCache
    {
        private Texture2DArray m_Cache;

        public override void TransferToSlice(int sliceIndex, Texture texture)
        {
            var mismatch = (m_Cache.width != texture.width) || (m_Cache.height != texture.height);

            if (texture is Texture2D)
            {
                mismatch |= (m_Cache.format != (texture as Texture2D).format);
            }

            if (mismatch)
            {
                if (!Graphics.ConvertTexture(texture, 0, m_Cache, sliceIndex))
                {
                    Debug.LogErrorFormat(texture, "Unable to convert texture \"{0}\" to match renderloop settings ({1}x{2} {3})",
                        texture.name, m_Cache.width, m_Cache.height, m_Cache.format);
                }
            }
            else
            {
                Graphics.CopyTexture(texture, 0, m_Cache, sliceIndex);
            }
        }

        public override Texture GetTexCache()
        {
            return m_Cache;
        }

        public bool AllocTextureArray(int numTextures, int width, int height, TextureFormat format, bool isMipMapped)
        {
            var res = AllocTextureArray(numTextures);
            m_NumMipLevels = GetNumMips(width, height);

            m_Cache = new Texture2DArray(width, height, numTextures, format, isMipMapped)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Clamp
            };

            return res;
        }

        public void Release()
        {
            Texture.DestroyImmediate(m_Cache);      // do I need this?
        }
    }

    public class TextureCacheCubemap : TextureCache
    {
        private CubemapArray m_Cache;

        // the member variables below are only in use when TextureCache.supportsCubemapArrayTextures is false
        private Texture2DArray m_CacheNoCubeArray;
        private RenderTexture[] m_StagingRTs;
        private int m_NumPanoMipLevels;
        private Material m_CubeBlitMaterial;
        private int m_CubeMipLevelPropName;
        private int m_cubeSrcTexPropName;

        public override void TransferToSlice(int sliceIndex, Texture texture)
        {
            if (!TextureCache.supportsCubemapArrayTextures)
                TransferToPanoCache(sliceIndex, texture);
            else
            {
                var mismatch = (m_Cache.width != texture.width) || (m_Cache.height != texture.height);

                if (texture is Cubemap)
                {
                    mismatch |= (m_Cache.format != (texture as Cubemap).format);
                }

                if (mismatch)
                {
                    bool failed = false;

                    for (int f = 0; f < 6; f++)
                    {
                        if (!Graphics.ConvertTexture(texture, f, m_Cache, 6 * sliceIndex + f))
                        {
                            failed = true;
                            break;
                        }
                    }

                    if (failed)
                    {
                        Debug.LogErrorFormat(texture, "Unable to convert texture \"{0}\" to match renderloop settings ({1}x{2} {3})",
                            texture.name, m_Cache.width, m_Cache.height, m_Cache.format);
                    }
                }
                else
                {
                    for (int f = 0; f < 6; f++)
                        Graphics.CopyTexture(texture, f, m_Cache, 6 * sliceIndex + f);
                }
            }
        }

        public override Texture GetTexCache()
        {
            return !TextureCache.supportsCubemapArrayTextures ? (Texture)m_CacheNoCubeArray : m_Cache;
        }

        public bool AllocTextureArray(int numCubeMaps, int width, TextureFormat format, bool isMipMapped)
        {
            var res = AllocTextureArray(numCubeMaps);
            m_NumMipLevels = GetNumMips(width, width);      // will calculate same way whether we have cube array or not

            if (!TextureCache.supportsCubemapArrayTextures)
            {
                if (!m_CubeBlitMaterial) m_CubeBlitMaterial = new Material(Shader.Find("Hidden/CubeToPano"));

                int panoWidthTop = 4 * width;
                int panoHeightTop = 2 * width;

                // create panorama 2D array. Hardcoding the render target for now. No convenient way atm to
                // map from TextureFormat to RenderTextureFormat and don't want to deal with sRGB issues for now.
                m_CacheNoCubeArray = new Texture2DArray(panoWidthTop, panoHeightTop, numCubeMaps, TextureFormat.RGBAHalf, isMipMapped)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Repeat,
                    wrapModeV = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Trilinear,
                    anisoLevel = 0
                };

                m_NumPanoMipLevels = isMipMapped ? GetNumMips(panoWidthTop, panoHeightTop) : 1;
                m_StagingRTs = new RenderTexture[m_NumPanoMipLevels];
                for (int m = 0; m < m_NumPanoMipLevels; m++)
                {
                    m_StagingRTs[m] = new RenderTexture(Mathf.Max(1, panoWidthTop >> m), Mathf.Max(1, panoHeightTop >> m), 0, RenderTextureFormat.ARGBHalf);
                }

                if (m_CubeBlitMaterial)
                {
                    m_CubeMipLevelPropName = Shader.PropertyToID("_cubeMipLvl");
                    m_cubeSrcTexPropName = Shader.PropertyToID("_srcCubeTexture");
                }
            }
            else
            {
                m_Cache = new CubemapArray(width, numCubeMaps, format, isMipMapped)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Trilinear,
                    anisoLevel = 0 // It is important to set 0 here, else unity force anisotropy filtering
                };
            }

            return res;
        }

        public void Release()
        {
            if (m_CacheNoCubeArray)
            {
                Texture.DestroyImmediate(m_CacheNoCubeArray);
                for (int m = 0; m < m_NumPanoMipLevels; m++)
                {
                    m_StagingRTs[m].Release();
                }
                m_StagingRTs = null;
                if (m_CubeBlitMaterial) Material.DestroyImmediate(m_CubeBlitMaterial);
            }
            if (m_Cache)
                Texture.DestroyImmediate(m_Cache);
        }

        private void TransferToPanoCache(int sliceIndex, Texture texture)
        {
            m_CubeBlitMaterial.SetTexture(m_cubeSrcTexPropName, texture);
            for (int m = 0; m < m_NumPanoMipLevels; m++)
            {
                m_CubeBlitMaterial.SetInt(m_CubeMipLevelPropName, Mathf.Min(m_NumMipLevels - 1, m));
                Graphics.SetRenderTarget(m_StagingRTs[m]);
                Graphics.Blit(null, m_CubeBlitMaterial, 0);
            }

            for (int m = 0; m < m_NumPanoMipLevels; m++)
                Graphics.CopyTexture(m_StagingRTs[m], 0, 0, m_CacheNoCubeArray, sliceIndex, m);
        }
    }


    public abstract class TextureCache
    {
        protected int m_NumMipLevels;

        static int s_GlobalTextureCacheVersion = 0;
        int m_TextureCacheVersion = 0;

    #if UNITY_EDITOR
        internal class AssetReloader : UnityEditor.AssetPostprocessor
        {
            void OnPostprocessTexture(Texture texture)
            {
                s_GlobalTextureCacheVersion++;
            }
        }
    #endif

        public static bool isMobileBuildTarget
        {
            get
            {
    #if UNITY_EDITOR
                switch (EditorUserBuildSettings.activeBuildTarget)
                {
                    case BuildTarget.iOS:
                    case BuildTarget.Android:
                    case BuildTarget.Tizen:
                    case BuildTarget.WSAPlayer:
                        // Note: We return true on purpose even if Windows Store Apps are running on Desktop.
                        return true;
                    default:
                        return false;
                }
    #else
                return Application.isMobilePlatform;
    #endif
            }
        }

        public static TextureFormat GetPreferredHdrCompressedTextureFormat
        {
            get
            {
                var format = TextureFormat.RGBAHalf;

                var probeFormat = TextureFormat.BC6H;

                // On editor the texture is uncompressed when operating against mobile build targets
//#if UNITY_2017_2_OR_NEWER
                if (SystemInfo.SupportsTextureFormat(probeFormat) && !UnityEngine.Rendering.GraphicsSettings.HasShaderDefine(UnityEngine.Rendering.BuiltinShaderDefine.UNITY_NO_DXT5nm))
                    format = probeFormat;
//#else
//                if (SystemInfo.SupportsTextureFormat(probeFormat) && !TextureCache.isMobileBuildTarget)
//                    format = probeFormat;
//#endif

                return format;
            }
        }

        public static bool supportsCubemapArrayTextures
        {
            get
            {
//#if UNITY_2017_2_OR_NEWER
                return !UnityEngine.Rendering.GraphicsSettings.HasShaderDefine(UnityEngine.Rendering.BuiltinShaderDefine.UNITY_NO_CUBEMAP_ARRAY);
//#else
//                return (SystemInfo.supportsCubemapArrayTextures && !TextureCache.isMobileBuildTarget);
//#endif
            }
        }

        private struct SSliceEntry
        {
            public uint texId;
            public uint countLRU;
        };

        private int m_NumTextures;
        private int[] m_SortedIdxArray;
        private SSliceEntry[] m_SliceArray;

        Dictionary<uint, int> m_LocatorInSliceArray;

        private static uint g_MaxFrameCount = unchecked((uint)(-1));
        private static uint g_InvalidTexID = (uint)0;

        public int FetchSlice(Texture texture, bool forceReinject=false)
        {
            var sliceIndex = -1;

            if (texture == null)
                return sliceIndex;

            var texId = (uint)texture.GetInstanceID();

            //assert(TexID!=g_InvalidTexID);
            if (texId == g_InvalidTexID) return 0;

            var bSwapSlice = forceReinject;
            var bFoundAvailOrExistingSlice = false;

            // search for existing copy
            if (m_LocatorInSliceArray.ContainsKey(texId))
            {
                sliceIndex = m_LocatorInSliceArray[texId];
                bFoundAvailOrExistingSlice = true;
#if UNITY_EDITOR
                if(m_TextureCacheVersion!=s_GlobalTextureCacheVersion)
                {
                    m_TextureCacheVersion++;
                    Debug.Assert(m_TextureCacheVersion <= s_GlobalTextureCacheVersion);
                    bSwapSlice = true;  // force a reinject.
                }
#endif
                //assert(m_SliceArray[sliceIndex].TexID==TexID);
            }

            // If no existing copy found in the array
            if (!bFoundAvailOrExistingSlice)
            {
                // look for first non zero entry. Will by the least recently used entry
                // since the array was pre-sorted (in linear time) in NewFrame()
                var bFound = false;
                int j = 0, idx = 0;
                while ((!bFound) && j < m_NumTextures)
                {
                    idx = m_SortedIdxArray[j];
                    if (m_SliceArray[idx].countLRU == 0) ++j;       // if entry already snagged by a new texture in this frame then ++j
                    else bFound = true;
                }

                if (bFound)
                {
                    // if we are replacing an existing entry delete it from m_locatorInSliceArray.
                    if (m_SliceArray[idx].texId != g_InvalidTexID)
                    {
                        m_LocatorInSliceArray.Remove(m_SliceArray[idx].texId);
                    }

                    m_LocatorInSliceArray.Add(texId, idx);
                    m_SliceArray[idx].texId = texId;

                    sliceIndex = idx;
                    bFoundAvailOrExistingSlice = true;
                    bSwapSlice = true;
                }
            }


            // wrap up
            //assert(bFoundAvailOrExistingSlice);
            if (bFoundAvailOrExistingSlice)
            {
                m_SliceArray[sliceIndex].countLRU = 0;      // mark slice as in use this frame

                if (bSwapSlice) // if this was a miss
                {
                    // transfer new slice to sliceIndex from source texture
                    TransferToSlice(sliceIndex, texture);
                }
            }

            return sliceIndex;
        }

        public void NewFrame()
        {
            var numNonZeros = 0;
            var tmpBuffer = new int[m_NumTextures];
            for (int i = 0; i < m_NumTextures; i++)
            {
                tmpBuffer[i] = m_SortedIdxArray[i];     // copy buffer
                if (m_SliceArray[m_SortedIdxArray[i]].countLRU != 0) ++numNonZeros;
            }
            int nonZerosBase = 0, zerosBase = 0;
            for (int i = 0; i < m_NumTextures; i++)
            {
                if (m_SliceArray[tmpBuffer[i]].countLRU == 0)
                {
                    m_SortedIdxArray[zerosBase + numNonZeros] = tmpBuffer[i];
                    ++zerosBase;
                }
                else
                {
                    m_SortedIdxArray[nonZerosBase] = tmpBuffer[i];
                    ++nonZerosBase;
                }
            }

            for (int i = 0; i < m_NumTextures; i++)
            {
                if (m_SliceArray[i].countLRU < g_MaxFrameCount) ++m_SliceArray[i].countLRU;     // next frame
            }

            //for(int q=1; q<m_numTextures; q++)
            //    assert(m_SliceArray[m_SortedIdxArray[q-1]].CountLRU>=m_SliceArray[m_SortedIdxArray[q]].CountLRU);
        }

        protected TextureCache()
        {
            m_NumTextures = 0;
            m_NumMipLevels = 0;
        }

        public virtual void TransferToSlice(int sliceIndex, Texture texture)
        {
        }

        public virtual Texture GetTexCache()
        {
            return null;
        }

        protected bool AllocTextureArray(int numTextures)
        {
            if (numTextures > 0)
            {
                m_SliceArray = new SSliceEntry[numTextures];
                m_SortedIdxArray = new int[numTextures];
                m_LocatorInSliceArray = new Dictionary<uint, int>();

                m_NumTextures = numTextures;
                for (int i = 0; i < m_NumTextures; i++)
                {
                    m_SliceArray[i].countLRU = g_MaxFrameCount;         // never used before
                    m_SliceArray[i].texId = g_InvalidTexID;
                    m_SortedIdxArray[i] = i;
                }
            }

            //return m_SliceArray != NULL && m_SortedIdxArray != NULL && numTextures > 0;
            return numTextures > 0;
        }

        // should not really be used in general. Assuming lights are culled properly entries will automatically be replaced efficiently.
        public void RemoveEntryFromSlice(Texture texture)
        {
            var texId = (uint)texture.GetInstanceID();

            //assert(TexID!=g_InvalidTexID);
            if (texId == g_InvalidTexID) return;

            // search for existing copy
            if (!m_LocatorInSliceArray.ContainsKey(texId))
                return;

            var sliceIndex = m_LocatorInSliceArray[texId];

            //assert(m_SliceArray[sliceIndex].TexID==TexID);

            // locate entry sorted by uCountLRU in m_pSortedIdxArray
            var foundIdxSortLRU = false;
            var i = 0;
            while ((!foundIdxSortLRU) && i < m_NumTextures)
            {
                if (m_SortedIdxArray[i] == sliceIndex) foundIdxSortLRU = true;
                else ++i;
            }

            if (!foundIdxSortLRU)
                return;

            // relocate sliceIndex to front of m_pSortedIdxArray since uCountLRU will be set to maximum.
            for (int j = 0; j < i; j++)
            {
                m_SortedIdxArray[j + 1] = m_SortedIdxArray[j];
            }
            m_SortedIdxArray[0] = sliceIndex;

            // delete from m_locatorInSliceArray and m_pSliceArray.
            m_LocatorInSliceArray.Remove(texId);
            m_SliceArray[sliceIndex].countLRU = g_MaxFrameCount;            // never used before
            m_SliceArray[sliceIndex].texId = g_InvalidTexID;
        }

        protected int GetNumMips(int width, int height)
        {
            return GetNumMips(width > height ? width : height);
        }

        protected int GetNumMips(int dim)
        {
            var uDim = (uint)dim;
            var iNumMips = 0;
            while (uDim > 0)
            { ++iNumMips; uDim >>= 1; }
            return iNumMips;
        }
    }
}
