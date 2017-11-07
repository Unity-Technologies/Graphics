using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    public class ReflectionProbeCache
    {
        enum ProbeFilteringState
        {
            Baking,
            Ready
        }

        int                     m_ProbeSize;
        int                     m_CacheSize;
        IBLFilterGGX            m_IBLFilterGGX;
        TextureCacheCubemap     m_TextureCache;
        RenderTexture           m_TempRenderTexture;
        RenderTexture           m_TempRenderTexture2;
        ProbeFilteringState[]   m_ProbeBakingState;

        public ReflectionProbeCache(IBLFilterGGX iblFilter, int cacheSize, int probeSize, TextureFormat probeFormat, bool isMipmaped)
        {
            m_ProbeSize = probeSize;
            m_CacheSize = cacheSize;
            m_TextureCache = new TextureCacheCubemap();
            m_TextureCache.AllocTextureArray(cacheSize, probeSize, probeFormat, isMipmaped);
            m_IBLFilterGGX = iblFilter;

            InitializeProbeBakingStates();
        }

        void Initialize()
        {
            if(m_TempRenderTexture == null)
            {
                // Temporary RT used for convolution and compression
                m_TempRenderTexture = new RenderTexture(m_ProbeSize, m_ProbeSize, 1, RenderTextureFormat.ARGBHalf);
                m_TempRenderTexture.dimension = TextureDimension.Cube;
                m_TempRenderTexture.useMipMap = true;
                m_TempRenderTexture.autoGenerateMips = false;
                m_TempRenderTexture.Create();

                m_TempRenderTexture2 = new RenderTexture(m_ProbeSize, m_ProbeSize, 1, RenderTextureFormat.ARGBHalf);
                m_TempRenderTexture2.dimension = TextureDimension.Cube;
                m_TempRenderTexture2.useMipMap = true;
                m_TempRenderTexture2.autoGenerateMips = false;
                m_TempRenderTexture2.Create();

                InitializeProbeBakingStates();
            }
        }

        void InitializeProbeBakingStates()
        {
            m_ProbeBakingState = new ProbeFilteringState[m_CacheSize];
            for (int i = 0; i < m_CacheSize; ++i)
                m_ProbeBakingState[i] = ProbeFilteringState.Baking;
        }

        public void Release()
        {
            m_TextureCache.Release();
            m_TextureCache = null;
            m_TempRenderTexture.Release();
            m_TempRenderTexture = null;
            m_ProbeBakingState = null;
        }

        public void NewFrame()
        {
            Initialize();
            m_TextureCache.NewFrame();
        }

        public int FetchSlice(CommandBuffer cmd, Texture texture)
        {
            bool needUpdate;
            var sliceIndex = m_TextureCache.ReserveSlice(texture, out needUpdate);
            if (sliceIndex != -1)
            {
                if(needUpdate || m_ProbeBakingState[sliceIndex] != ProbeFilteringState.Ready)
                {
                    using (new ProfilingSample(cmd, "Convolve Reflection Probe"))
                    {
                        // For now baking is done directly but will be time sliced in the future. Just preparing the code here.
                        m_ProbeBakingState[sliceIndex] = ProbeFilteringState.Baking;

                        // Probes can be either Cubemaps (for baked probes) or RenderTextures (for realtime probes)
                        Cubemap cubeTexture = texture as Cubemap;
                        RenderTexture renderTexture = texture as RenderTexture;

                        Texture convolutionSourceTexture = null;
                        RenderTexture convolutionTargetTexture = m_TempRenderTexture2;
                        if (cubeTexture != null)
                        {
                            // TODO: Write a shader to copy the texture, because this will not work if input is compressed.
                            // Ideally if input is not compressed and has mipmaps, don't do anything here. Problem is, we can't know if mips have been already convolved offline...
                            if (cubeTexture.format != TextureFormat.RGBAHalf)
                                return -1;
                            for (int f = 0; f < 6; f++)
                            {
                                cmd.CopyTexture(cubeTexture, f, 0, m_TempRenderTexture, f, 0);
                            }
                            cmd.GenerateMips(m_TempRenderTexture);
                            convolutionSourceTexture = m_TempRenderTexture;
                        }
                        else
                        {
                            Debug.Assert(renderTexture != null);
                            cmd.GenerateMips(renderTexture);
                            convolutionSourceTexture = renderTexture;
                        }

                        m_IBLFilterGGX.FilterCubemap(cmd, convolutionSourceTexture, convolutionTargetTexture);
                        m_TextureCache.UpdateSlice(cmd, sliceIndex, convolutionTargetTexture, m_TextureCache.GetTextureUpdateCount(texture)); // Be careful to provide the update count from the input texture, not the temporary one used for baking.

                        m_ProbeBakingState[sliceIndex] = ProbeFilteringState.Ready;
                    }
                }
            }

            return sliceIndex;
        }

        public Texture GetTexCache()
        {
            return m_TextureCache.GetTexCache();
        }
    }
}
