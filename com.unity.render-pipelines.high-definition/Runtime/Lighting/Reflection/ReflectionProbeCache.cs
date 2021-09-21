using System;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    class ReflectionProbeCache
    {
        enum ProbeFilteringState
        {
            Convolving,
            Ready
        }

        int m_ProbeSize;
        int m_CacheSize;
        IBLFilterBSDF[] m_IBLFilterBSDF;
        TextureCacheCubemap m_TextureCache;
        RenderTexture m_TempRenderTexture;
        RenderTexture[] m_ConvolutionTargetTextureArray;
        ProbeFilteringState[] m_ProbeBakingState;
        Material m_ConvertTextureMaterial;
        Material m_CubeToPano;
        MaterialPropertyBlock m_ConvertTextureMPB;
        bool m_PerformBC6HCompression;

        GraphicsFormat m_ProbeFormat;

        public ReflectionProbeCache(HDRenderPipelineRuntimeResources defaultResources, IBLFilterBSDF[] iblFilterBSDFArray, int cacheSize, int probeSize, GraphicsFormat probeFormat, bool isMipmaped)
        {
            m_ConvertTextureMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.blitCubeTextureFacePS);
            m_ConvertTextureMPB = new MaterialPropertyBlock();
            m_CubeToPano = CoreUtils.CreateEngineMaterial(defaultResources.shaders.cubeToPanoPS);

            Debug.Assert(probeFormat == GraphicsFormat.RGB_BC6H_SFloat || probeFormat == GraphicsFormat.B10G11R11_UFloatPack32 || probeFormat == GraphicsFormat.R16G16B16A16_SFloat,
                "Reflection Probe Cache format for HDRP can only be BC6H, FP16 or R11G11B10.");
            m_ProbeFormat = probeFormat;

            m_ProbeSize = probeSize;
            m_CacheSize = cacheSize;
            m_TextureCache = new TextureCacheCubemap("ReflectionProbe", iblFilterBSDFArray.Length);
            m_TextureCache.AllocTextureArray(cacheSize, probeSize, probeFormat, isMipmaped, m_CubeToPano);
            m_IBLFilterBSDF = iblFilterBSDFArray;

            m_PerformBC6HCompression = probeFormat == GraphicsFormat.RGB_BC6H_SFloat;

            InitializeProbeBakingStates();
        }

        void Initialize()
        {
            if (m_TempRenderTexture == null)
            {
                // Temporary RT used for convolution and compression
                m_TempRenderTexture = new RenderTexture(m_ProbeSize, m_ProbeSize, 1, m_ProbeFormat);
                m_TempRenderTexture.hideFlags = HideFlags.HideAndDontSave;
                m_TempRenderTexture.dimension = TextureDimension.Cube;
                m_TempRenderTexture.useMipMap = true;
                m_TempRenderTexture.autoGenerateMips = false;
                m_TempRenderTexture.name = CoreUtils.GetRenderTargetAutoName(m_ProbeSize, m_ProbeSize, 1, m_ProbeFormat, "ReflectionProbeTemp", mips: true);
                m_TempRenderTexture.Create();

                m_ConvolutionTargetTextureArray = new RenderTexture[m_IBLFilterBSDF.Length];
                for (int bsdfIdx = 0; bsdfIdx < m_IBLFilterBSDF.Length; ++bsdfIdx)
                {
                    m_ConvolutionTargetTextureArray[bsdfIdx] = new RenderTexture(m_ProbeSize, m_ProbeSize, 1, m_ProbeFormat);
                    m_ConvolutionTargetTextureArray[bsdfIdx].hideFlags = HideFlags.HideAndDontSave;
                    m_ConvolutionTargetTextureArray[bsdfIdx].dimension = TextureDimension.Cube;
                    m_ConvolutionTargetTextureArray[bsdfIdx].useMipMap = true;
                    m_ConvolutionTargetTextureArray[bsdfIdx].autoGenerateMips = false;
                    m_ConvolutionTargetTextureArray[bsdfIdx].name = CoreUtils.GetRenderTargetAutoName(m_ProbeSize, m_ProbeSize, 1, m_ProbeFormat, "ReflectionProbeConvolution_" + bsdfIdx.ToString(), mips: true);
                    m_ConvolutionTargetTextureArray[bsdfIdx].Create();
                }
            }

            InitializeProbeBakingStates();
        }

        void InitializeProbeBakingStates()
        {
            if (m_ProbeBakingState == null || m_ProbeBakingState.Length != m_CacheSize)
            {
                Array.Resize(ref m_ProbeBakingState, m_CacheSize);
                for (var i = 0; i < m_CacheSize; ++i)
                    m_ProbeBakingState[i] = ProbeFilteringState.Convolving;
            }
        }

        public void Release()
        {
            m_TextureCache.Release();
            CoreUtils.Destroy(m_TempRenderTexture);

            if (m_ConvolutionTargetTextureArray != null)
            {
                for (int bsdfIdx = 0; bsdfIdx < m_IBLFilterBSDF.Length; ++bsdfIdx)
                {
                    if (m_ConvolutionTargetTextureArray[bsdfIdx] != null)
                    {
                        CoreUtils.Destroy(m_ConvolutionTargetTextureArray[bsdfIdx]);
                        m_ConvolutionTargetTextureArray[bsdfIdx] = null;
                    }
                }
            }

            m_ProbeBakingState = null;

            CoreUtils.Destroy(m_ConvertTextureMaterial);
            CoreUtils.Destroy(m_CubeToPano);
        }

        public void NewFrame()
        {
            Initialize();
            m_TextureCache.NewFrame();
        }

        // This method is used to convert inputs that are either compressed or not of the right size.
        // We can't use Graphics.ConvertTexture here because it does not work with a RenderTexture as destination.
        void ConvertTexture(CommandBuffer cmd, Texture input, RenderTexture target)
        {
            m_ConvertTextureMPB.SetTexture(HDShaderIDs._InputTex, input);
            m_ConvertTextureMPB.SetFloat(HDShaderIDs._LoD, 0.0f); // We want to convert mip 0 to whatever the size of the destination cache is.
            for (int f = 0; f < 6; ++f)
            {
                m_ConvertTextureMPB.SetFloat(HDShaderIDs._FaceIndex, (float)f);
                CoreUtils.SetRenderTarget(cmd, target, ClearFlag.None, Color.black, 0, (CubemapFace)f);
                CoreUtils.DrawFullScreen(cmd, m_ConvertTextureMaterial, m_ConvertTextureMPB);
            }
        }

        Texture[] ConvolveProbeTexture(CommandBuffer cmd, Texture texture)
        {
            // Probes can be either Cubemaps (for baked probes) or RenderTextures (for realtime probes)
            Cubemap cubeTexture = texture as Cubemap;
            RenderTexture renderTexture = texture as RenderTexture;

            RenderTexture convolutionSourceTexture = null;
            if (cubeTexture != null)
            {
                // if the size if different from the cache probe size or if the input texture format is compressed, we need to convert it
                // 1) to a format for which we can generate mip maps
                // 2) to the proper reflection probe cache size
                bool sizeMismatch = cubeTexture.width != m_ProbeSize || cubeTexture.height != m_ProbeSize;
                bool formatMismatch = (GraphicsFormatUtility.GetGraphicsFormat(cubeTexture.format, false) != m_TempRenderTexture.graphicsFormat);
                if (formatMismatch || sizeMismatch)
                {
                    // We comment the following warning as they have no impact on the result but spam the console, it is just that we waste offline time and a bit of quality for nothing.
                    if (sizeMismatch)
                    {
                        // Debug.LogWarningFormat("Baked Reflection Probe {0} does not match HDRP Reflection Probe Cache size of {1}. Consider baking it at the same size for better loading performance.", texture.name, m_ProbeSize);
                    }
                    else if (cubeTexture.graphicsFormat == GraphicsFormat.RGB_BC6H_UFloat || cubeTexture.graphicsFormat == GraphicsFormat.RGB_BC6H_SFloat)
                    {
                        // Debug.LogWarningFormat("Baked Reflection Probe {0} is compressed but the HDRP Reflection Probe Cache is not. Consider removing compression from the input texture for better quality.", texture.name);
                    }
                    ConvertTexture(cmd, cubeTexture, m_TempRenderTexture);
                }
                else
                {
                    for (int f = 0; f < 6; f++)
                    {
                        cmd.CopyTexture(cubeTexture, f, 0, m_TempRenderTexture, f, 0);
                    }
                }

                // Ideally if input is not compressed and has mipmaps, don't do anything here. Problem is, we can't know if mips have been already convolved offline...
                cmd.GenerateMips(m_TempRenderTexture);
                convolutionSourceTexture = m_TempRenderTexture;
            }
            else
            {
                Debug.Assert(renderTexture != null);
                if (renderTexture.dimension != TextureDimension.Cube)
                {
                    Debug.LogError("Realtime reflection probe should always be a Cube RenderTexture.");
                    return null;
                }

                // TODO: Do a different case for downsizing, in this case, instead of doing ConvertTexture just use the relevant mipmaps.
                bool sizeMismatch = renderTexture.width != m_ProbeSize || renderTexture.height != m_ProbeSize;
                bool formatMismatch = (renderTexture.graphicsFormat != m_ProbeFormat);

                if (formatMismatch || sizeMismatch)
                {
                    ConvertTexture(cmd, renderTexture, m_TempRenderTexture);
                    convolutionSourceTexture = m_TempRenderTexture;
                }
                else
                {
                    convolutionSourceTexture = renderTexture;
                }
                // Generate unfiltered mipmaps as a base for convolution
                // TODO: Make sure that we don't first convolve everything on the GPU with the legacy code path executed after rendering the probe.
                cmd.GenerateMips(convolutionSourceTexture);
            }

            for (int bsdfIdx = 0; bsdfIdx < m_IBLFilterBSDF.Length; ++bsdfIdx)
            {
                m_IBLFilterBSDF[bsdfIdx].FilterCubemap(cmd, convolutionSourceTexture, m_ConvolutionTargetTextureArray[bsdfIdx]);
            }

            return m_ConvolutionTargetTextureArray;
        }

        public int FetchSlice(CommandBuffer cmd, Texture texture)
        {
            bool needUpdate;
            var sliceIndex = m_TextureCache.ReserveSlice(texture, out needUpdate);
            if (sliceIndex != -1)
            {
                if (needUpdate || m_ProbeBakingState[sliceIndex] != ProbeFilteringState.Ready)
                {
                    using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ConvolveReflectionProbe)))
                    {
                        // For now baking is done directly but will be time sliced in the future. Just preparing the code here.
                        m_ProbeBakingState[sliceIndex] = ProbeFilteringState.Convolving;

                        Texture[] result = ConvolveProbeTexture(cmd, texture);
                        if (result == null)
                            return -1;

                        if (m_PerformBC6HCompression)
                        {
                            cmd.BC6HEncodeFastCubemap(
                                result[0], m_ProbeSize, m_TextureCache.GetTexCache(),
                                0, int.MaxValue, sliceIndex);
                            m_TextureCache.SetSliceHash(sliceIndex, m_TextureCache.GetTextureHash(texture));
                        }
                        else
                        {
                            m_TextureCache.UpdateSlice(cmd, sliceIndex, result, m_TextureCache.GetTextureHash(texture)); // Be careful to provide the update count from the input texture, not the temporary one used for convolving.
                        }

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

        internal static long GetApproxCacheSizeInByte(int nbElement, int resolution, int sliceSize)
        {
            return TextureCacheCubemap.GetApproxCacheSizeInByte(nbElement, resolution, sliceSize);
        }

        internal static int GetMaxCacheSizeForWeightInByte(int weight, int resolution, int sliceSize)
        {
            return TextureCacheCubemap.GetMaxCacheSizeForWeightInByte(weight, resolution, sliceSize);
        }

        public int GetEnvSliceSize()
        {
            return m_IBLFilterBSDF.Length;
        }
    }
}
