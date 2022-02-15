using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class TextureCacheOctmap : TextureCache
    {
        Texture2DArray m_OctTexCache;
        RenderTexture[] m_StagingRTs;

        int m_OctNumMipLevels;

        Material m_CubeToOctMaterial;

        int m_CubeSrcTexPropName;
        int m_CubeMipLevelPropName;
        MaterialPropertyBlock m_CubeToOctMaterialProps;

        public TextureCacheOctmap(string cacheName = "", int sliceSize = 1) : base(cacheName, sliceSize)
        {
            m_CubeToOctMaterial = CoreUtils.CreateEngineMaterial(HDRenderPipelineGlobalSettings.instance.renderPipelineResources.shaders.cubeToOctPS);
            m_CubeSrcTexPropName = Shader.PropertyToID("_CubeTexture");
            m_CubeMipLevelPropName = Shader.PropertyToID("_CubeMipLevel");
            m_CubeToOctMaterialProps = new MaterialPropertyBlock();
        }

        internal static long GetApproxCacheSizeInByte(int nbElement, int resolution, int sliceSize)
        {
            return (long)((long)nbElement * resolution * resolution * k_FP16SizeInByte * k_NbChannel * k_MipmapFactorApprox * sliceSize);
        }

        internal static int GetMaxCacheSizeForWeightInByte(long weight, int resolution, int sliceSize)
        {
            int theoricalResult = Mathf.FloorToInt(weight / ((long)resolution * resolution * k_FP16SizeInByte * k_NbChannel * k_MipmapFactorApprox * sliceSize));
            return Mathf.Clamp(theoricalResult, 1, k_MaxSupported);
        }

        protected override bool TransferToSlice(CommandBuffer cmd, int sliceIndex, Texture[] textureArray)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ConvertReflectionProbeCubeToOct)))
            {
                for (int texIdx = 0; texIdx < textureArray.Length; ++texIdx)
                {
                    m_CubeToOctMaterialProps.SetTexture(m_CubeSrcTexPropName, textureArray[texIdx]);

                    for (int m = 0; m < m_OctNumMipLevels; m++)
                    {
                        m_CubeToOctMaterialProps.SetFloat(m_CubeMipLevelPropName, Mathf.Min(m_NumMipLevels - 1, m));

                        CoreUtils.SetRenderTarget(cmd, m_StagingRTs[m], ClearFlag.None, Color.black);
                        CoreUtils.DrawFullScreen(cmd, m_CubeToOctMaterial, m_CubeToOctMaterialProps);
                    }

                    for (int m = 0; m < m_OctNumMipLevels; m++)
                        cmd.CopyTexture(m_StagingRTs[m], 0, 0, m_OctTexCache, m_SliceSize * sliceIndex + texIdx, m);
                }
            }

            return true;
        }

        public override bool IsCreated()
        {
            return true;
        }

        public override Texture GetTexCache()
        {
            return m_OctTexCache;
        }

        public bool AllocTextureArray(int numCubeMaps, int width, GraphicsFormat format, bool isMipMapped, Material cubeBlitMaterial)
        {
            var res = AllocTextureArray(numCubeMaps);

            m_NumMipLevels = GetNumMips(width, width);

            int panoWidthTop = 4 * width;
            int panoHeightTop = 2 * width;

            m_OctTexCache = new Texture2DArray(panoWidthTop, panoHeightTop, numCubeMaps, format, isMipMapped ? TextureCreationFlags.MipChain : TextureCreationFlags.None)
            {
                hideFlags = HideFlags.HideAndDontSave,
                wrapMode = TextureWrapMode.Repeat,
                wrapModeV = TextureWrapMode.Clamp,
                filterMode = FilterMode.Trilinear,
                anisoLevel = 0,
                name = CoreUtils.GetTextureAutoName(panoWidthTop, panoHeightTop, format, TextureDimension.Tex2DArray, depth: numCubeMaps, name: m_CacheName)
            };

            m_OctNumMipLevels = isMipMapped ? GetNumMips(panoWidthTop, panoHeightTop) : 1;
            m_StagingRTs = new RenderTexture[m_OctNumMipLevels];

            for (int m = 0; m < m_OctNumMipLevels; ++m)
            {
                m_StagingRTs[m] = new RenderTexture(Mathf.Max(1, panoWidthTop >> m), Mathf.Max(1, panoHeightTop >> m), 0, format) { hideFlags = HideFlags.HideAndDontSave };
                m_StagingRTs[m].name = CoreUtils.GetRenderTargetAutoName(Mathf.Max(1, panoWidthTop >> m), Mathf.Max(1, panoHeightTop >> m), 1, format, String.Format("PanaCache{0}", m));
            }

            return res;
        }

        public void Release()
        {
            CoreUtils.Destroy(m_OctTexCache);

            for (int m = 0; m < m_OctNumMipLevels; m++)
                m_StagingRTs[m].Release();

            m_StagingRTs = null;

            CoreUtils.Destroy(m_CubeToOctMaterial);

            m_CubeToOctMaterialProps = null;
        }
    }
}
