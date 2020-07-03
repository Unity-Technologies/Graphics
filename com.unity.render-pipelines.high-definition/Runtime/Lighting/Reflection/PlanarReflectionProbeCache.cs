using System;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    class PlanarReflectionProbeCache
    {
        internal static readonly int s_InputTexID = Shader.PropertyToID("_InputTex");
        internal static readonly int s_LoDID = Shader.PropertyToID("_LoD");
        internal static readonly int s_FaceIndexID = Shader.PropertyToID("_FaceIndex");

        enum ProbeFilteringState
        {
            Convolving,
            Ready
        }

        int                     m_ProbeSize;
        IBLFilterGGX            m_IBLFilterGGX;
        PowerOfTwoTextureAtlas  m_TextureAtlas;
        RenderTexture           m_TempRenderTexture = null;
        RenderTexture           m_ConvolutionTargetTexture;
        Dictionary<Vector4, ProbeFilteringState> m_ProbeBakingState = new Dictionary<Vector4, ProbeFilteringState>();
        Material                m_ConvertTextureMaterial;
        MaterialPropertyBlock   m_ConvertTextureMPB;
        bool                    m_PerformBC6HCompression;
        Dictionary<int, uint>   m_TextureHashes = new Dictionary<int, uint>();
        int                     m_FrameProbeIndex;

        public PlanarReflectionProbeCache(RenderPipelineResources defaultResources, IBLFilterGGX iblFilter, int atlasResolution, GraphicsFormat probeFormat, bool isMipmaped)
        {
            m_ConvertTextureMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.blitCubeTextureFacePS);
            m_ConvertTextureMPB = new MaterialPropertyBlock();

            // BC6H requires CPP feature not yet available
            probeFormat = GraphicsFormat.R16G16B16A16_SFloat;

            Debug.Assert(probeFormat == GraphicsFormat.RGB_BC6H_SFloat || probeFormat == GraphicsFormat.R16G16B16A16_SFloat, "Reflection Probe Cache format for HDRP can only be BC6H or FP16.");

            m_ProbeSize = atlasResolution;
            m_TextureAtlas = new PowerOfTwoTextureAtlas(atlasResolution, 0, probeFormat, useMipMap: isMipmaped, name: "PlanarReflectionProbe Atlas");
            m_IBLFilterGGX = iblFilter;

            m_PerformBC6HCompression = probeFormat == GraphicsFormat.RGB_BC6H_SFloat;
        }

        void Initialize()
        {
            if (m_ConvolutionTargetTexture == null)
            {
                // Temporary RT used for convolution and compression

                // Note: Temporarily disabled because planar probe baking is currently disabled so we avoid allocating unused targets
                // m_TempRenderTexture = new RenderTexture(m_ProbeSize, m_ProbeSize, 1, RenderTextureFormat.ARGBHalf);
                // m_TempRenderTexture.hideFlags = HideFlags.HideAndDontSave;
                // m_TempRenderTexture.dimension = TextureDimension.Tex2D;
                // m_TempRenderTexture.useMipMap = true;
                // m_TempRenderTexture.autoGenerateMips = false;
                // m_TempRenderTexture.name = CoreUtils.GetRenderTargetAutoName(m_ProbeSize, m_ProbeSize, 1, RenderTextureFormat.ARGBHalf, "PlanarReflectionTemp", mips: true);
                // m_TempRenderTexture.Create();

                m_ConvolutionTargetTexture = new RenderTexture(m_ProbeSize, m_ProbeSize, 0, RenderTextureFormat.ARGBHalf);
                m_ConvolutionTargetTexture.hideFlags = HideFlags.HideAndDontSave;
                m_ConvolutionTargetTexture.dimension = TextureDimension.Tex2D;
                m_ConvolutionTargetTexture.useMipMap = true;
                m_ConvolutionTargetTexture.autoGenerateMips = false;
                m_ConvolutionTargetTexture.filterMode = FilterMode.Point;
                m_ConvolutionTargetTexture.name = CoreUtils.GetRenderTargetAutoName(m_ProbeSize, m_ProbeSize, 0, RenderTextureFormat.ARGBHalf, "PlanarReflectionConvolution", mips: true);
                m_ConvolutionTargetTexture.enableRandomWrite = true;
                m_ConvolutionTargetTexture.Create();

                // Clear to avoid garbage in the convolution texture.
                int mipCount = Mathf.FloorToInt(Mathf.Log(m_ProbeSize, 2)) + 1;
                for (int mipIdx = 0; mipIdx < mipCount; ++mipIdx)
                {
                    Graphics.SetRenderTarget(m_ConvolutionTargetTexture, mipIdx, CubemapFace.Unknown);
                    GL.Clear(false, true, Color.clear);
                }

            }

            m_FrameProbeIndex = 0;
        }

        public void Release()
        {
            m_TextureAtlas.Release();
            CoreUtils.Destroy(m_TempRenderTexture);
            CoreUtils.Destroy(m_ConvolutionTargetTexture);

            m_ProbeBakingState = null;

            CoreUtils.Destroy(m_ConvertTextureMaterial);
        }

        public void NewFrame()
        {
            Initialize();
        }

        // This method is used to convert inputs that are either compressed or not of the right size.
        // We can't use Graphics.ConvertTexture here because it does not work with a RenderTexture as destination.
        void ConvertTexture(CommandBuffer cmd, Texture input, RenderTexture target)
        {
            m_ConvertTextureMPB.SetTexture(s_InputTexID, input);
            m_ConvertTextureMPB.SetFloat(s_LoDID, 0.0f); // We want to convert mip 0 to whatever the size of the destination cache is.
            CoreUtils.SetRenderTarget(cmd, target, ClearFlag.None, Color.black, 0, 0);
            CoreUtils.DrawFullScreen(cmd, m_ConvertTextureMaterial, m_ConvertTextureMPB);
        }

        Texture ConvolveProbeTexture(CommandBuffer cmd, Texture texture, out Vector4 sourceScaleOffset)
        {
            // Probes can be either Cubemaps (for baked probes) or RenderTextures (for realtime probes)
            Texture2D texture2D = texture as Texture2D;
            RenderTexture renderTexture = texture as RenderTexture;

            RenderTexture convolutionSourceTexture = null;

            // Disabled code path because planar reflection probe baking is currently disabled
            if (texture2D != null && false)
            {
                // if the size if different from the cache probe size or if the input texture format is compressed, we need to convert it
                // 1) to a format for which we can generate mip maps
                // 2) to the proper reflection probe cache size
                var sizeMismatch = texture2D.width != m_ProbeSize || texture2D.height != m_ProbeSize;
                var formatMismatch = texture2D.format != TextureFormat.RGBAHalf; // Temporary RT for convolution is always FP16
                if (formatMismatch || sizeMismatch)
                {
                    if (sizeMismatch)
                    {
                        Debug.LogWarningFormat("Baked Planar Reflection Probe {0} does not match HDRP Planar Reflection Probe Cache size of {1}. Consider baking it at the same size for better loading performance.", texture.name, m_ProbeSize);
                    }
                    else if (texture2D.format == TextureFormat.BC6H)
                    {
                        Debug.LogWarningFormat("Baked Planar Reflection Probe {0} is compressed but the HDRP Planar Reflection Probe Cache is not. Consider removing compression from the input texture for better quality.", texture.name);
                    }
                    ConvertTexture(cmd, texture2D, m_TempRenderTexture);
                }
                else
                    cmd.CopyTexture(texture2D, 0, 0, m_TempRenderTexture, 0, 0);

                convolutionSourceTexture = m_TempRenderTexture;
            }
            else
            {
                Debug.Assert(renderTexture != null);
                if (renderTexture.dimension != TextureDimension.Tex2D)
                {
                    Debug.LogError("Planar Realtime reflection probe should always be a 2D RenderTexture.");
                    sourceScaleOffset = Vector4.zero;
                    return null;
                }

                convolutionSourceTexture = renderTexture;
            }

            float scaleX = (float)texture.width / m_ConvolutionTargetTexture.width;
            float scaleY = (float)texture.height / m_ConvolutionTargetTexture.height;
            sourceScaleOffset = new Vector4(scaleX, scaleY, 0, 0);
            m_IBLFilterGGX.FilterPlanarTexture(cmd, convolutionSourceTexture, m_ConvolutionTargetTexture);

            return m_ConvolutionTargetTexture;
        }

        public Vector4 FetchSlice(CommandBuffer cmd, Texture texture, out int fetchIndex)
        {
            Vector4 scaleOffset = Vector4.zero;
            fetchIndex = m_FrameProbeIndex++;

            if (m_TextureAtlas.IsCached(out scaleOffset, texture))
            {
                // If the texture is already in the atlas, we update it only if needed
                if (NeedsUpdate(texture) || m_ProbeBakingState[scaleOffset] != ProbeFilteringState.Ready)
                    if (!UpdatePlanarTexture(cmd, texture, ref scaleOffset))
                        Debug.LogError("Can't convolve or update the planar reflection render target");
            }
            else // Either we add it to the atlas
                if (!UpdatePlanarTexture(cmd, texture, ref scaleOffset))
                    Debug.LogError("No more space in the planar reflection probe atlas. To solve this issue, increase the size of the Planar Reflection Probe Atlas in the HDRP settings.");

            return scaleOffset;
        }

        bool UpdatePlanarTexture(CommandBuffer cmd, Texture texture, ref Vector4 scaleOffset)
        {
            bool    success = false;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ConvolvePlanarReflectionProbe)))
            {
                // For now baking is done directly but will be time sliced in the future. Just preparing the code here.
                m_ProbeBakingState[scaleOffset] = ProbeFilteringState.Convolving;

                Vector4 sourceScaleOffset;
                Texture convolvedTexture = ConvolveProbeTexture(cmd, texture, out sourceScaleOffset);
                if (convolvedTexture == null)
                    return false;

                if (m_PerformBC6HCompression)
                {
                    throw new NotImplementedException("BC6H Support not implemented for PlanarReflectionProbeCache");
                }
                else
                {
                    if (m_TextureAtlas.IsCached(out scaleOffset, texture))
                    {
                        success = m_TextureAtlas.UpdateTexture(cmd, texture, convolvedTexture, ref scaleOffset, sourceScaleOffset);
                    }
                    else
                    {
                        // Reserve space for the rendertarget and then blit the result of the convolution at this
                        // location, we don't use the UpdateTexture because it will keep the reference to the
                        // temporary target used to convolve the result of the probe rendering.
                        if (!m_TextureAtlas.AllocateTextureWithoutBlit(texture, texture.width, texture.height, ref scaleOffset))
                            return false;
                        m_TextureAtlas.BlitTexture(cmd, scaleOffset, convolvedTexture, sourceScaleOffset);
                        success = true;
                    }
                }

                m_ProbeBakingState[scaleOffset] = ProbeFilteringState.Ready;
            }

            return success;
        }

        public uint GetTextureHash(Texture texture)
        {
            uint textureHash  = texture.updateCount;
            // For baked probes in the editor we need to factor in the actual hash of texture because we can't increment the update count of a texture that's baked on the disk.
#if UNITY_EDITOR
            textureHash += (uint)texture.imageContentsHash.GetHashCode();
#endif
            return textureHash;
        }

        bool NeedsUpdate(Texture texture)
        {
            uint savedTextureHash;
            uint currentTextureHash = GetTextureHash(texture);
            int instanceId = texture.GetInstanceID();
            bool needsUpdate = false;

            if (!m_TextureHashes.TryGetValue(instanceId, out savedTextureHash) || savedTextureHash != currentTextureHash)
            {
                m_TextureHashes[instanceId] = currentTextureHash;
                needsUpdate = true;
            }

            return needsUpdate;
        }

        public Texture GetTexCache() => m_TextureAtlas.AtlasTexture;


        public void Clear(CommandBuffer cmd)
        {
            m_TextureAtlas.ResetAllocator();
            m_TextureAtlas.ClearTarget(cmd);
        }

        public void ClearAtlasAllocator() => m_TextureAtlas.ResetAllocator();

        internal static long GetApproxCacheSizeInByte(int nbElement, int atlasResolution, GraphicsFormat format)
            => PowerOfTwoTextureAtlas.GetApproxCacheSizeInByte(nbElement, atlasResolution, true, format);

        internal static int GetMaxCacheSizeForWeightInByte(int weight, GraphicsFormat format)
            => PowerOfTwoTextureAtlas.GetMaxCacheSizeForWeightInByte(weight, true, format);
        
        internal Vector4 GetAtlasDatas()
        {
            float padding = Mathf.Pow(2.0f, m_TextureAtlas.mipPadding) * 2.0f;

            return new Vector4(
                m_TextureAtlas.AtlasTexture.rt.width,
                padding / (float)m_TextureAtlas.AtlasTexture.rt.width,
                0,
                0
            );
        }
    }
}
