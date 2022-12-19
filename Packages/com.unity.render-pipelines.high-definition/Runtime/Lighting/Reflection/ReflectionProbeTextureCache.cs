using System;
using System.Linq;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace UnityEngine.Rendering.HighDefinition
{
    class ReflectionProbeTextureCache
    {
        IBLFilterBSDF[] m_IBLFiltersBSDF;

        int m_AtlasWidth;
        int m_AtlasHeight;
        GraphicsFormat m_AtlasFormat;
        int m_AtlasMipCount;
        int m_AtlasSlicesCount;

        RTHandle m_AtlasTexture;
        Texture2DAtlasDynamic m_Atlas;

        int m_CubeMipPadding;
        int m_CubeTexelPadding;
        int m_PlanarMipPadding;
        int m_PlanarTexelPadding;

        bool m_DecreaseResToFit;

        int m_CubeFrameFetchIndex;
        int m_PlanarFrameFetchIndex;

        Dictionary<int, (uint, uint)> m_TextureLRUAndHash = new Dictionary<int, (uint, uint)>();
        List<(int, uint)> m_TextureLRUSorted = new List<(int, uint)>();

        Material m_ConvertTextureMaterial;

        uint m_CurrentRender;

        bool m_NoMoreSpaceErrorLogged;

        RenderTexture m_ConvolvedPlanarReflectionTexture;

        public ReflectionProbeTextureCache(HDRenderPipelineRuntimeResources defaultResources, IBLFilterBSDF[] iblFiltersBSDF, int width, int height, GraphicsFormat format,
            bool decreaseResToFit, int lastValidCubeMip, int lastValidPlanarMip)
        {
            Assert.IsTrue(Mathf.IsPowerOfTwo(width) && Mathf.IsPowerOfTwo(height));
            Assert.IsTrue(width <= (int)ReflectionProbeTextureCacheResolution.Resolution16384x16384);
            Assert.IsTrue(height <= (int)ReflectionProbeTextureCacheResolution.Resolution16384x16384);
            Assert.IsTrue(format == GraphicsFormat.B10G11R11_UFloatPack32 || format == GraphicsFormat.R16G16B16A16_SFloat, "Reflection Probe Cache format for HDRP can only be FP16 or R11G11B10.");
            Assert.IsTrue(iblFiltersBSDF[0] is IBLFilterGGX);

            m_IBLFiltersBSDF = iblFiltersBSDF;

            m_AtlasWidth = width;
            m_AtlasHeight = height;
            m_AtlasFormat = format;
            m_AtlasMipCount = Mathf.FloorToInt(Mathf.Log(Math.Max(m_AtlasWidth, m_AtlasHeight), 2)) + 1;
            m_AtlasSlicesCount = m_IBLFiltersBSDF.Length;

            m_AtlasTexture = RTHandles.Alloc(
                width: width,
                height: height,
                slices: m_AtlasSlicesCount,
                dimension: TextureDimension.Tex2DArray,
                filterMode: FilterMode.Trilinear,
                colorFormat: format,
                wrapMode: TextureWrapMode.Clamp,
                useMipMap: true,
                autoGenerateMips: false,
                name: "ReflectionProbeCacheTextureAtlas"
            );

            const int k_MaxTexturesInAtlas = 2048;

            m_Atlas = new Texture2DAtlasDynamic(width, height, k_MaxTexturesInAtlas, m_AtlasTexture);

            m_CubeMipPadding = Mathf.Clamp(lastValidCubeMip, 0, (int)EnvConstants.ConvolutionMipCount - 1);
            m_CubeTexelPadding = (1 << m_CubeMipPadding) * 2;
            m_PlanarMipPadding = Mathf.Clamp(lastValidPlanarMip, 0, (int)EnvConstants.ConvolutionMipCount - 1);
            m_PlanarTexelPadding = (1 << m_PlanarMipPadding) * 2;

            m_DecreaseResToFit = decreaseResToFit;

            m_ConvertTextureMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.blitCubeTextureFacePS);
        }

        private static int GetTextureID(Texture texture)
        {
            return texture.GetInstanceID();
        }

        private static int GetTextureSizeInAtlas(Texture texture)
        {
            int textureSize = texture.width;

            if (texture.dimension == TextureDimension.Cube)
            {
                textureSize = GetReflectionProbeSizeInAtlas(textureSize);
            }

            return textureSize;
        }

        public static int GetReflectionProbeSizeInAtlas(int textureSize)
        {
            // Last mip level should be at least 4 texels wide for the prober octahedral border.
            textureSize = Mathf.Max(textureSize, 32);

            // In theory we should multiply by sqrt(6) to match the area but we can't do that due to pow of two constraint.
            // So for cube resolutions less than 512 we multiply by 4 as otherwise quality suffers.
            // But for resolutions more or equal than 512 it is enough to multiply by 2 as the difference is barely noticeable.
            if (textureSize < 512)
                textureSize *= 4;
            else
                textureSize *= 2;

            return textureSize;
        }

        private static Vector2 GetTextureSizeWithoutPadding(int textureWidth, int textureHeight, int texelPadding)
        {
            int textureWidthWithoutPadding = Mathf.Max(textureWidth - texelPadding, 1);
            int textureHeightWithoutPadding = Mathf.Max(textureHeight - texelPadding, 1);
            return new Vector2(textureWidthWithoutPadding, textureHeightWithoutPadding);
        }

        internal static long GetApproxCacheSizeInByte(int elementsCount, int width, int height, GraphicsFormat format)
        {
            const double mipmapFactorApprox = 1.33;
            return (long)(elementsCount * width * height * mipmapFactorApprox * GraphicsFormatUtility.GetBlockSize(format));
        }

        private RenderTexture EnsureConvolvedPlanarReflectionTexture(int textureSize)
        {
            if (m_ConvolvedPlanarReflectionTexture == null || m_ConvolvedPlanarReflectionTexture.width < textureSize)
            {
                m_ConvolvedPlanarReflectionTexture?.Release();

                m_ConvolvedPlanarReflectionTexture = new RenderTexture(textureSize, textureSize, 0, m_AtlasFormat);
                m_ConvolvedPlanarReflectionTexture.hideFlags = HideFlags.HideAndDontSave;
                m_ConvolvedPlanarReflectionTexture.dimension = TextureDimension.Tex2D;
                m_ConvolvedPlanarReflectionTexture.useMipMap = true;
                m_ConvolvedPlanarReflectionTexture.autoGenerateMips = false;
                m_ConvolvedPlanarReflectionTexture.filterMode = FilterMode.Point;
                m_ConvolvedPlanarReflectionTexture.name = CoreUtils.GetRenderTargetAutoName(textureSize, textureSize, 0, m_AtlasFormat, "ConvolvedPlanarReflectionTexture", mips: true);
                m_ConvolvedPlanarReflectionTexture.enableRandomWrite = true;
                m_ConvolvedPlanarReflectionTexture.Create();
            }

            return m_ConvolvedPlanarReflectionTexture;
        }

        private void LogErrorNoMoreSpaceOnce()
        {
            if (!m_NoMoreSpaceErrorLogged)
            {
                m_NoMoreSpaceErrorLogged = true;

                Debug.LogError("No more space in Reflection Probe Atlas. To solve this issue, increase the size of the Reflection Probe Atlas in the HDRP settings.");
            }
        }

        private bool NeedsUpdate(Texture texture, uint textureHash, ref Vector4 scaleOffset)
        {
            int textureId = GetTextureID(texture);

            bool needsUpdate = false;

            if (!m_Atlas.IsCached(out scaleOffset, textureId))
                needsUpdate = true;
            else if (!m_TextureLRUAndHash.TryGetValue(textureId, out (uint, uint hash) entry) || entry.hash != textureHash)
                needsUpdate = true;

            m_TextureLRUAndHash[textureId] = (m_CurrentRender, textureHash);

            return needsUpdate;
        }

        private RenderTexture PrepareCubeReflectionProbeTexture(CommandBuffer cmd, Texture texture)
        {
            RenderTexture renderTexture = texture as RenderTexture;
            Cubemap cubemap = texture as Cubemap;

            Assert.IsTrue((renderTexture && renderTexture.dimension == TextureDimension.Cube) || cubemap, "Cube Reflection Probe should always be a Cubemap Texture.");

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ConvertReflectionProbe)))
            {
                int cubeSize = Math.Max(texture.width, (int)Mathf.Pow(2, (int)EnvConstants.ConvolutionMipCount - 1));

                bool conversionRequired = texture.graphicsFormat != m_AtlasFormat;
                conversionRequired |= (cubemap && cubemap.mipmapCount == 1);
                conversionRequired |= (renderTexture && !renderTexture.useMipMap);
                conversionRequired |= texture.width < cubeSize;

                if (conversionRequired)
                {
                    RenderTexture convertedTextureTemp = RenderTexture.GetTemporary(cubeSize, cubeSize, 0, m_AtlasFormat);
                    convertedTextureTemp.dimension = TextureDimension.Cube;
                    convertedTextureTemp.filterMode = texture.filterMode;
                    convertedTextureTemp.useMipMap = true;
                    convertedTextureTemp.autoGenerateMips = false;
                    convertedTextureTemp.name = CoreUtils.GetRenderTargetAutoName(cubeSize, cubeSize, 0, m_AtlasFormat, "ConvertedReflectionProbeTemp", mips: true);
                    convertedTextureTemp.Create();

                    MaterialPropertyBlock convertTextureProps = new MaterialPropertyBlock();
                    convertTextureProps.SetTexture(HDShaderIDs._InputTex, texture);
                    convertTextureProps.SetFloat(HDShaderIDs._LoD, 0.0f);

                    for (int f = 0; f < 6; ++f)
                    {
                        convertTextureProps.SetFloat(HDShaderIDs._FaceIndex, f);
                        CoreUtils.SetRenderTarget(cmd, convertedTextureTemp, ClearFlag.None, Color.black, 0, (CubemapFace)f);
                        CoreUtils.DrawFullScreen(cmd, m_ConvertTextureMaterial, convertTextureProps);
                    }

                    cmd.GenerateMips(convertedTextureTemp);

                    return convertedTextureTemp;
                }
                else if (renderTexture && !renderTexture.autoGenerateMips)
                {
                    cmd.GenerateMips(renderTexture);
                }
            }

            return null;
        }

        private RenderTexture ConvolveCubeReflectionProbeTexture(CommandBuffer cmd, Texture texture, IBLFilterBSDF filter)
        {
            RenderTexture renderTexture = texture as RenderTexture;

            Assert.IsTrue((renderTexture && renderTexture.dimension == TextureDimension.Cube), "Cube Reflection Probe should always be a Cubemap Texture.");

            RenderTexture convolvedTextureTemp = RenderTexture.GetTemporary(texture.width, texture.height, 0, m_AtlasFormat);
            convolvedTextureTemp.dimension = TextureDimension.Cube;
            convolvedTextureTemp.filterMode = texture.filterMode;
            convolvedTextureTemp.useMipMap = true;
            convolvedTextureTemp.autoGenerateMips = false;
            convolvedTextureTemp.anisoLevel = 0;
            convolvedTextureTemp.name = "ConvolvedReflectionProbeTemp";
            convolvedTextureTemp.Create();

            filter.FilterCubemap(cmd, texture, convolvedTextureTemp);

            return convolvedTextureTemp;
        }

        private RenderTexture ConvolvePlanarReflectionProbeTexture(CommandBuffer cmd, Texture texture, ref IBLFilterBSDF.PlanarTextureFilteringParameters planarTextureFilteringParameters)
        {
            RenderTexture renderTexture = texture as RenderTexture;

            Assert.IsTrue(renderTexture && renderTexture.dimension == TextureDimension.Tex2D, "Planar Reflection Probe must be a 2D RenderTexture.");
            Assert.IsTrue(texture.graphicsFormat == m_AtlasFormat, "Planar Reflection Probe format and the cache format must be the same.");

            RenderTexture convolvedPlanarReflectionTexture = EnsureConvolvedPlanarReflectionTexture(texture.width);

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ConvolvePlanarReflectionProbe)))
            {
                // For planar reflection we only convolve with the GGX filter, otherwise it would be too expensive.
                IBLFilterGGX IBLFiltersBSDF = (IBLFilterGGX)m_IBLFiltersBSDF[0];
                IBLFiltersBSDF.FilterPlanarTexture(cmd, renderTexture, ref planarTextureFilteringParameters, convolvedPlanarReflectionTexture);
            }

            return convolvedPlanarReflectionTexture;
        }

        private void BlitTextureCube(CommandBuffer cmd, Vector4 scaleOffset, Texture texture, int arraySlice)
        {
            Assert.IsTrue(texture.dimension == TextureDimension.Cube);

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.BlitTextureToReflectionProbeAtlas)))
            {
                int texelPadding = m_CubeTexelPadding;
                int textureWidthInAtlas = Mathf.CeilToInt(scaleOffset.x * m_AtlasWidth);
                int textureHeightInAtlas = Mathf.CeilToInt(scaleOffset.y * m_AtlasHeight);
                Assert.IsTrue(textureWidthInAtlas == textureHeightInAtlas);

                Vector2 textureSizeWithoutPadding = GetTextureSizeWithoutPadding(textureWidthInAtlas, textureHeightInAtlas, texelPadding);
                bool bilinear = texture.filterMode != FilterMode.Point;

                for (int mipLevel = 0; mipLevel < m_AtlasMipCount; ++mipLevel)
                {
                    // Every octahedral mip has at least one texel padding. This improves octahedral mapping as it always should have border for every mip.
                    if (mipLevel > m_CubeMipPadding)
                        texelPadding *= 2;

                    cmd.SetRenderTarget(m_AtlasTexture, mipLevel, CubemapFace.Unknown, arraySlice);
                    Blitter.BlitCubeToOctahedral2DQuadWithPadding(cmd, texture, textureSizeWithoutPadding, scaleOffset, mipLevel, bilinear, texelPadding);
                }
            }
        }

        private void BlitTexture2D(CommandBuffer cmd, Vector4 scaleOffset, Vector4 sourceScaleOffset, Texture texture, int arraySlice)
        {
            Assert.IsTrue(texture.dimension == TextureDimension.Tex2D);

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.BlitTextureToReflectionProbeAtlas)))
            {
                int texelPadding = m_PlanarTexelPadding;
                int textureWidthInAtlas = Mathf.CeilToInt(scaleOffset.x * m_AtlasWidth);
                int textureHeightInAtlas = Mathf.CeilToInt(scaleOffset.y * m_AtlasHeight);
                Assert.IsTrue(textureWidthInAtlas == textureHeightInAtlas);

                Vector2 textureSizeWithoutPadding = GetTextureSizeWithoutPadding(textureWidthInAtlas, textureHeightInAtlas, texelPadding);
                bool bilinear = texture.filterMode != FilterMode.Point;

                for (int mipLevel = 0; mipLevel < m_AtlasMipCount; ++mipLevel)
                {
                    cmd.SetRenderTarget(m_AtlasTexture, mipLevel, CubemapFace.Unknown, arraySlice);
                    Blitter.BlitQuadWithPadding(cmd, texture, textureSizeWithoutPadding, sourceScaleOffset, scaleOffset, mipLevel, bilinear, texelPadding);
                }
            }
        }

        private bool RelayoutTextureAtlas()
        {
            var atlasEntries = new List<(int textureId, Vector4 scaleOffset)>();
            atlasEntries.Capacity = m_TextureLRUAndHash.Count;

            foreach (var pair in m_TextureLRUAndHash)
            {
                if (m_Atlas.IsCached(out Vector4 scaleOffset, pair.Key))
                    atlasEntries.Add((pair.Key, scaleOffset));
            }

            atlasEntries.Sort((a, b) => { return b.scaleOffset.x.CompareTo(a.scaleOffset.x); });

            m_Atlas.ResetAllocator();

            bool success = true;

            foreach (var entry in atlasEntries)
            {
                int textureWidth = Mathf.CeilToInt(entry.scaleOffset.x * m_AtlasWidth);
                int textureHeight = Mathf.CeilToInt(entry.scaleOffset.y * m_AtlasHeight);

                if (m_Atlas.EnsureTextureSlot(out _, out Vector4 scaleOffset, entry.textureId, textureWidth, textureHeight))
                {
                    var texturePos = new Vector2Int(Mathf.FloorToInt(entry.scaleOffset.z * m_AtlasWidth), Mathf.FloorToInt(entry.scaleOffset.w * m_AtlasHeight));
                    var newTexturePos = new Vector2Int(Mathf.FloorToInt(scaleOffset.z * m_AtlasWidth), Mathf.FloorToInt(scaleOffset.w * m_AtlasHeight));

                    // Invalidate texture only if its position actually changed after re-layout.
                    bool invalidateTexture = texturePos != newTexturePos;

                    if (invalidateTexture)
                    {
                        var LRUAndHash = m_TextureLRUAndHash[entry.textureId];
                        LRUAndHash.Item2 = 0;
                        m_TextureLRUAndHash[entry.textureId] = LRUAndHash;
                    }
                }
                else
                {
                    m_TextureLRUAndHash.Remove(entry.textureId);

                    success = false;
                }
            }

            return success;
        }

        private bool TryAllocateTexture(int textureId, int textureSize, ref Vector4 scaleOffset)
        {
            Assert.IsTrue(Mathf.IsPowerOfTwo(textureSize));
            Assert.IsTrue(!m_Atlas.IsCached(out _, textureId));

            // 1.
            // The first direct attempt to find space.
            if (m_Atlas.EnsureTextureSlot(out _, out scaleOffset, textureId, textureSize, textureSize))
                return true;

            // 2.
            // If no space try to remove least recently used entries and find space again.
            for (int textureIndex = m_TextureLRUSorted.Count - 1; textureIndex >= 0; --textureIndex)
            {
                var textureLRU = m_TextureLRUSorted[textureIndex];

                const int k_PreviousRender = 1;

                // Preserve current and previous frame cached entries.
                if (m_CurrentRender - textureLRU.Item2 > k_PreviousRender)
                {
                    m_Atlas.ReleaseTextureSlot(textureLRU.Item1);
                    m_TextureLRUAndHash.Remove(textureLRU.Item1);
                    m_TextureLRUSorted.RemoveAt(textureIndex);

                    if (m_Atlas.EnsureTextureSlot(out _, out scaleOffset, textureId, textureSize, textureSize))
                        return true;
                }
                else
                {
                    break;
                }
            }

            // 3.
            // Try to downscale texture and find space.
            if (m_DecreaseResToFit)
            {
                if (m_Atlas.EnsureTextureSlot(out _, out scaleOffset, textureId, textureSize / 2, textureSize / 2))
                    return true;
            }

            m_TextureLRUAndHash.Remove(textureId);

            return false;
        }

        private bool UpdateTexture(CommandBuffer cmd, Texture texture, ref Vector4 scaleOffset)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.UpdateReflectionProbeAtlas)))
            {
                int textureId = GetTextureID(texture);

                if (!m_Atlas.IsCached(out scaleOffset, textureId))
                {
                    int textureSize = GetTextureSizeInAtlas(texture);

                    if (!TryAllocateTexture(textureId, textureSize, ref scaleOffset))
                        return false;
                }

                RenderTexture convertedTextureTemp = PrepareCubeReflectionProbeTexture(cmd, texture);

                for (int filterIndex = 0; filterIndex < m_IBLFiltersBSDF.Length; ++filterIndex)
                {
                    RenderTexture convolvedTextureTemp = ConvolveCubeReflectionProbeTexture(cmd, convertedTextureTemp ? convertedTextureTemp : texture, m_IBLFiltersBSDF[filterIndex]);
                    BlitTextureCube(cmd, scaleOffset, convolvedTextureTemp, filterIndex);
                    RenderTexture.ReleaseTemporary(convolvedTextureTemp);
                }

                RenderTexture.ReleaseTemporary(convertedTextureTemp);

                return true;
            }
        }

        private bool UpdateTexture(CommandBuffer cmd, Texture texture, ref IBLFilterBSDF.PlanarTextureFilteringParameters planarTextureFilteringParameters, ref Vector4 scaleOffset)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.UpdateReflectionProbeAtlas)))
            {
                int textureId = GetTextureID(texture);

                if (!m_Atlas.IsCached(out scaleOffset, textureId))
                {
                    int textureSize = texture.width;

                    if (!TryAllocateTexture(textureId, textureSize, ref scaleOffset))
                        return false;
                }

                RenderTexture convolvedPlanarReflectionTexture = ConvolvePlanarReflectionProbeTexture(cmd, texture, ref planarTextureFilteringParameters);

                float sourceScale = (float)texture.width / convolvedPlanarReflectionTexture.width;
                Vector4 sourceScaleOffset = new Vector4(sourceScale, sourceScale, 0.0f, 0.0f);

                BlitTexture2D(cmd, scaleOffset, sourceScaleOffset, convolvedPlanarReflectionTexture, 0);

                return true;
            }
        }

        public Vector4 GetTextureAtlasCubeData()
        {
            return new Vector4((float)m_CubeTexelPadding / m_AtlasWidth, (float)m_CubeTexelPadding / m_AtlasHeight, m_CubeMipPadding, 0.0f);
        }

        public Vector4 GetTextureAtlasPlanarData()
        {
            return new Vector4((float)m_PlanarTexelPadding / m_AtlasWidth, (float)m_PlanarTexelPadding / m_AtlasHeight, 1.0f / m_AtlasWidth, 1.0f / m_AtlasHeight);
        }

        public Texture GetAtlasTexture()
        {
            return m_AtlasTexture;
        }

        public int GetEnvSliceSize()
        {
            return m_AtlasSlicesCount;
        }

        public void Release()
        {
            foreach (var IBLFiltersBSDF in m_IBLFiltersBSDF)
                IBLFiltersBSDF.Cleanup();
            m_IBLFiltersBSDF = null;

            m_AtlasTexture.Release();
            m_AtlasTexture = null;
            m_Atlas.Release();
            m_Atlas = null;

            m_TextureLRUAndHash = null;

            m_ConvertTextureMaterial = null;

            m_ConvolvedPlanarReflectionTexture?.Release();
        }

        public Vector4 FetchCubeReflectionProbe(CommandBuffer cmd, HDProbe probe, out int fetchIndex)
        {
            Texture texture = probe.texture;
            Assert.IsTrue(texture.width == texture.height);
            Assert.IsTrue(texture.dimension == TextureDimension.Cube);

            fetchIndex = m_CubeFrameFetchIndex++;

            Vector4 scaleOffset = Vector4.zero;

            if (NeedsUpdate(texture, probe.GetTextureHash(), ref scaleOffset))
            {
                if(!UpdateTexture(cmd, texture, ref scaleOffset))
                    LogErrorNoMoreSpaceOnce();
            }

            return scaleOffset;
        }

        public Vector4 FetchPlanarReflectionProbe(CommandBuffer cmd, HDProbe probe, ref IBLFilterBSDF.PlanarTextureFilteringParameters planarTextureFilteringParameters, out int fetchIndex)
        {
            Texture texture = probe.texture;
            Assert.IsTrue(texture.width == texture.height);
            Assert.IsTrue(texture.dimension == TextureDimension.Tex2D);

            fetchIndex = m_PlanarFrameFetchIndex++;

            Vector4 scaleOffset = Vector4.zero;

            if (NeedsUpdate(texture, probe.GetTextureHash(), ref scaleOffset))
            {
                if (!UpdateTexture(cmd, texture, ref planarTextureFilteringParameters, ref scaleOffset))
                    LogErrorNoMoreSpaceOnce();
            }

            return scaleOffset;
        }

        public void ReserveReflectionProbeSlot(HDProbe probe)
        {
            Texture texture = probe.texture;
            Assert.IsTrue(texture.width == texture.height);
            Assert.IsTrue(texture.dimension == TextureDimension.Tex2D || texture.dimension == TextureDimension.Cube);

            int textureId = GetTextureID(texture);

            if (!m_Atlas.IsCached(out _, textureId))
            {
                int textureSize = GetTextureSizeInAtlas(texture);

                Vector4 scaleOffset = Vector4.zero;

                if (!TryAllocateTexture(textureId, textureSize, ref scaleOffset))
                {
                    if (RelayoutTextureAtlas())
                        TryAllocateTexture(textureId, textureSize, ref scaleOffset);
                }
            }
        }

        public void NewFrame()
        {
            m_CubeFrameFetchIndex = 0;
            m_PlanarFrameFetchIndex = 0;
        }

        public void NewRender()
        {
            m_NoMoreSpaceErrorLogged = false;

            ++m_CurrentRender;

            m_TextureLRUSorted.Clear();

            foreach (var pair in m_TextureLRUAndHash)
                m_TextureLRUSorted.Add((pair.Key, pair.Value.Item1));

            m_TextureLRUSorted.Sort((a, b) => { return b.Item2.CompareTo(a.Item2); });
        }

        public void ClearAtlasAllocator()
        {
            m_Atlas.ResetAllocator();
            m_TextureLRUAndHash.Clear();
            m_TextureLRUSorted.Clear();
        }

        public void Clear(CommandBuffer cmd)
        {
            ClearAtlasAllocator();

            for (int sliceIndex = 0; sliceIndex < m_AtlasSlicesCount; ++sliceIndex)
            {
                for (int mipLevel = 0; mipLevel < m_AtlasMipCount; ++mipLevel)
                {
                    cmd.SetRenderTarget(m_AtlasTexture, mipLevel, CubemapFace.Unknown, sliceIndex);
                    Blitter.BlitQuad(cmd, Texture2D.blackTexture, new Vector4(1.0f, 1.0f, 0.0f, 0.0f), new Vector4(1.0f, 1.0f, 0.0f, 0.0f), mipLevel, true);
                }
            }
        }
    }
}
