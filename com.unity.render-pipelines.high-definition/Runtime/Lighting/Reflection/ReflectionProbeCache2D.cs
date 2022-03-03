using System;
using System.Linq;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    class ReflectionProbeCache2D
    {
        const int k_CurrentFrameLRUIndex = 0;
        const int k_PreviousFrameLRUIndex = 1;

        IBLFilterBSDF[] m_IBLFiltersBSDF;

        int m_Resolution;
        GraphicsFormat m_Format;

        PowerOfTwoTextureAtlas m_TextureAtlas;

        int m_FrameFetchIndex;
        //@ Is this dictionary required at all?
        Dictionary<int, uint> m_TextureHashes = new Dictionary<int, uint>();
        Dictionary<int, uint> m_TextureLRU = new Dictionary<int, uint>();

        Material m_ConvertTextureMaterial;

        public ReflectionProbeCache2D(HDRenderPipelineRuntimeResources defaultResources, IBLFilterBSDF[] iblFiltersBSDF, int resolution, GraphicsFormat format)
        {
            Debug.Assert(format == GraphicsFormat.B10G11R11_UFloatPack32 || format == GraphicsFormat.R16G16B16A16_SFloat, "Reflection Probe Cache format for HDRP can only be FP16 or R11G11B10.");

            m_IBLFiltersBSDF = iblFiltersBSDF;
            m_Resolution = resolution;
            m_Format = format;

            //@ Real size must be larger as octahedral area must be equal to cube map area.
            int mipPadding = 0;
            m_TextureAtlas = new PowerOfTwoTextureAtlas(resolution, mipPadding, format, FilterMode.Trilinear, "ReflectionProbeCache2D Atlas", true, m_IBLFiltersBSDF.Length);

            m_ConvertTextureMaterial = CoreUtils.CreateEngineMaterial(defaultResources.shaders.blitCubeTextureFacePS);
        }

        private static uint GetTextureUpdateHash(Texture texture)
        {
            uint textureHash = texture.updateCount;
#if UNITY_EDITOR
            textureHash += (uint)texture.imageContentsHash.GetHashCode();
#endif
            return textureHash;
        }

        private static int GetTextureID(Texture texture)
        {
            return texture.GetInstanceID();
        }

        private bool NeedsUpdate(Texture texture)
        {
            uint currentTextureHash = GetTextureUpdateHash(texture);
            int textureId = GetTextureID(texture);

            bool needsUpdate = false;

            uint savedTextureHash;

            if (!m_TextureHashes.TryGetValue(textureId, out savedTextureHash) || savedTextureHash != currentTextureHash)
            {
                m_TextureHashes[textureId] = currentTextureHash;
                needsUpdate = true;
            }

            return needsUpdate;
        }

        private RenderTexture ConvolveProbeTexture(CommandBuffer cmd, Texture texture)
        {
            RenderTexture renderTexture = texture as RenderTexture;
            Cubemap cubemap = texture as Cubemap;

            Debug.Assert((renderTexture && renderTexture.dimension == TextureDimension.Cube) || cubemap);

            RenderTexture convolvedTextureArrayTemp;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ConvolveReflectionProbe)))
            {
                RenderTexture convertedTextureTemp = null;

                if (renderTexture)
                {
                    cmd.GenerateMips(renderTexture);
                }
                else if (GraphicsFormatUtility.GetGraphicsFormat(cubemap.format, false) != m_Format || cubemap.mipmapCount == 1)
                {
                    convertedTextureTemp = RenderTexture.GetTemporary(texture.width, texture.height, 0, m_Format);
                    convertedTextureTemp.hideFlags = HideFlags.HideAndDontSave;
                    convertedTextureTemp.dimension = TextureDimension.Cube;
                    convertedTextureTemp.useMipMap = true;
                    convertedTextureTemp.autoGenerateMips = false;
                    convertedTextureTemp.name = CoreUtils.GetRenderTargetAutoName(texture.width, texture.height, 1, m_Format, "ConvertedReflectionProbeTemp", mips: true);
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

                    texture = convertedTextureTemp;
                }

                convolvedTextureArrayTemp = RenderTexture.GetTemporary(texture.width, texture.height, 0, m_Format);
                convolvedTextureArrayTemp.hideFlags = HideFlags.HideAndDontSave;
                convolvedTextureArrayTemp.volumeDepth = 6 * m_IBLFiltersBSDF.Length;
                convolvedTextureArrayTemp.dimension = TextureDimension.CubeArray;
                convolvedTextureArrayTemp.useMipMap = true;
                convolvedTextureArrayTemp.autoGenerateMips = false;
                convolvedTextureArrayTemp.anisoLevel = 0;
                convolvedTextureArrayTemp.name = CoreUtils.GetRenderTargetAutoName(texture.width, texture.height, 1, m_Format, "ConvolvedReflectionProbeTemp", mips: true);
                convolvedTextureArrayTemp.Create();

                for (int filterIndex = 0; filterIndex < m_IBLFiltersBSDF.Length; ++filterIndex)
                    m_IBLFiltersBSDF[filterIndex].FilterCubemap(cmd, texture, convolvedTextureArrayTemp, filterIndex);

                RenderTexture.ReleaseTemporary(convertedTextureTemp);
            }

            return convolvedTextureArrayTemp;
        }

        private bool TryAllocateTextureWithoutBlit(int textureId, int width, int height, ref Vector4 scaleOffset)
        {
            // The first attempt.
            if (m_TextureAtlas.AllocateTextureWithoutBlit(textureId, width, height, ref scaleOffset))
                return true;

            m_TextureAtlas.ResetRequestedTexture();

            // If no space try to remove least recently used entries and allocate again.
            // The better way could be to put subtree min LRU index in atlas node.
            // And remove nodes based on min subtree LRU index in the whole tree.
            // Otherwise smaller entries can be far apart from each other and we will have useless removes.
            var texturesLRUSorted = m_TextureLRU.OrderByDescending(x => x.Value);

            foreach (var pair in texturesLRUSorted)
            {
                if(m_TextureAtlas.IsCached(out _, pair.Key))
                {
                    // Don't touch current and previous frame cached entries.
                    if (pair.Value > k_PreviousFrameLRUIndex)
                    {
                        if (m_TextureAtlas.RemoveTexture(pair.Key))
                        {
                            if (m_TextureAtlas.AllocateTextureWithoutBlit(textureId, width, height, ref scaleOffset))
                                return true;
                        }
                    }
                    else
                    {
                        m_TextureAtlas.ReserveSpace(pair.Key);
                    }
                }
            }

            m_TextureAtlas.ReserveSpace(textureId, width, height);

            Debug.LogWarning("Reflection probe atlas re-layout. Try to increase the size of the Reflection Probe Atlas in the HDRP settings for better performance.");

            if(m_TextureAtlas.RelayoutEntries())
            {
                if (m_TextureAtlas.IsCached(out scaleOffset, textureId))
                    return true;
            }

            return false;
        }

        private bool UpdateTexture(CommandBuffer cmd, Texture texture, ref Vector4 scaleOffset)
        {
            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.UpdateReflectionProbeAtlas)))
            {
                RenderTexture convolvedTextureArrayTemp = ConvolveProbeTexture(cmd, texture);

                if (convolvedTextureArrayTemp == null)
                    return false;

                bool success = true;

                int textureId = GetTextureID(texture);

                if (m_TextureAtlas.IsCached(out scaleOffset, textureId))
                {
                    if (m_TextureAtlas.NeedsUpdate(texture, false))
                        m_TextureAtlas.BlitCubeTexture2D(cmd, scaleOffset, convolvedTextureArrayTemp, true, textureId);
                }
                else
                {
                    // In theory we should multiply by sqrt(6) to match the area.
                    int octahedralWidth = convolvedTextureArrayTemp.width * 2;
                    int octahedralHeight = convolvedTextureArrayTemp.height * 2;

                    if (TryAllocateTextureWithoutBlit(textureId, octahedralWidth, octahedralHeight, ref scaleOffset))
                        m_TextureAtlas.BlitCubeTexture2D(cmd, scaleOffset, convolvedTextureArrayTemp, true, textureId);
                    else
                        success = false;
                }

                RenderTexture.ReleaseTemporary(convolvedTextureArrayTemp);

                return success;
            }
        }

        public Vector4 GetAtlasDatas()
        {
            float padding = Mathf.Pow(2.0f, m_TextureAtlas.mipPadding) * 2.0f;
            return new Vector4(m_Resolution, padding / m_Resolution, 0.0f, 0.0f);
        }

        public Texture GetTexCache()
        {
            return m_TextureAtlas.AtlasTexture;
        }

        public int GetEnvSliceSize()
        {
            return m_IBLFiltersBSDF.Length;
        }

        public void Release()
        {
            m_IBLFiltersBSDF = null;
            m_TextureAtlas.Release();
            m_TextureHashes = null;
            m_ConvertTextureMaterial = null;
        }

        public Vector4 FetchSlice(CommandBuffer cmd, Texture texture, out int fetchIndex)
        {
            Debug.Assert(texture.dimension == TextureDimension.Cube);

            Vector4 scaleOffset = Vector4.zero;
            int textureId = GetTextureID(texture);

            bool updateTexture = true;

            if (m_TextureAtlas.IsCached(out scaleOffset, textureId))
                updateTexture = NeedsUpdate(texture);

            if (updateTexture)
            {
                if(!UpdateTexture(cmd, texture, ref scaleOffset))
                    Debug.LogError("No more space in the reflection probe atlas. To solve this issue, increase the size of the Reflection Probe Atlas in the HDRP settings.");
            }

            m_TextureLRU[textureId] = k_CurrentFrameLRUIndex;

            fetchIndex = m_FrameFetchIndex++;

            return scaleOffset;
        }

        public void NewFrame()
        {
            m_FrameFetchIndex = 0;

            m_TextureLRU.Keys.ToList().ForEach(key =>
            {
                if (!m_TextureAtlas.IsCached(out _, key))
                    m_TextureLRU.Remove(key);
                else
                    m_TextureLRU[key]++;
            });
        }

        public void ClearAtlasAllocator()
        {
            m_TextureAtlas.ResetAllocator();
        }

        public void Clear(CommandBuffer cmd)
        {
            m_TextureAtlas.ResetAllocator();
            m_TextureAtlas.ClearTarget(cmd);
        }
    }
}

