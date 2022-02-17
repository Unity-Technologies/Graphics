using System;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    class ReflectionProbeCache2D
    {
        enum ProbeFilteringState
        {
            Convolving,
            Ready
        }

        IBLFilterBSDF[] m_IBLFiltersBSDF;

        int m_Resolution;
        GraphicsFormat m_Format;

        PowerOfTwoTextureAtlas m_TextureAtlas;

        int m_FrameProbeIndex;
        Dictionary<int, uint> m_TextureHashes = new Dictionary<int, uint>();
        Dictionary<Vector4, ProbeFilteringState> m_ProbeBakingState = new Dictionary<Vector4, ProbeFilteringState>();

        public ReflectionProbeCache2D(HDRenderPipelineRuntimeResources defaultResources, IBLFilterBSDF[] iblFiltersBSDF, int resolution, GraphicsFormat format)
        {
            m_IBLFiltersBSDF = iblFiltersBSDF;
            m_Resolution = resolution;
            m_Format = format;

            int mipPadding = 0;
            m_TextureAtlas = new PowerOfTwoTextureAtlas(resolution, mipPadding, format, FilterMode.Trilinear, "ReflectionProbeCache2D Atlas", true);
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
            Debug.Assert(renderTexture);

            RenderTexture convolvedTexture;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ConvolveReflectionProbe)))
            {
                //@ check size format mismatch
                cmd.GenerateMips(renderTexture);

                //@ Replace with static rt of fixes size
                convolvedTexture = RenderTexture.GetTemporary(renderTexture.width, renderTexture.height, 1, m_Format);
                convolvedTexture.hideFlags = HideFlags.HideAndDontSave;
                convolvedTexture.dimension = TextureDimension.Cube;
                convolvedTexture.useMipMap = true;
                convolvedTexture.autoGenerateMips = false;
                convolvedTexture.name = CoreUtils.GetRenderTargetAutoName(renderTexture.width, renderTexture.height, 1, m_Format, "ConvolvedReflectionProbeTemp", mips: true);
                convolvedTexture.Create();

                //@ All filters
                m_IBLFiltersBSDF[0].FilterCubemap(cmd, renderTexture, convolvedTexture);
            }

            return convolvedTexture;
        }

        private bool UpdateTexture(CommandBuffer cmd, Texture texture, ref Vector4 scaleOffset)
        {
            bool success = false;

            m_ProbeBakingState[scaleOffset] = ProbeFilteringState.Convolving;

            RenderTexture convolvedTexture = ConvolveProbeTexture(cmd, texture);

            if (convolvedTexture == null)
                return false;

            if (m_TextureAtlas.IsCached(out scaleOffset, GetTextureID(texture)))
            {
                if (m_TextureAtlas.NeedsUpdate(texture, false))
                    m_TextureAtlas.BlitCubeTexture2D(cmd, scaleOffset, convolvedTexture, true, GetTextureID(texture));
            }
            else
            {
                if (!m_TextureAtlas.AllocateTextureWithoutBlit(GetTextureID(texture), convolvedTexture.width, convolvedTexture.height, ref scaleOffset))
                    return false;

                m_TextureAtlas.BlitCubeTexture2D(cmd, scaleOffset, convolvedTexture, true, GetTextureID(texture));

                success = true;
            }

            RenderTexture.ReleaseTemporary(convolvedTexture);

            m_ProbeBakingState[scaleOffset] = ProbeFilteringState.Ready;

            return success;
        }

        public Vector4 GetAtlasDatas()
        {
            float padding = Mathf.Pow(2.0f, m_TextureAtlas.mipPadding) * 2.0f;
            return new Vector4(m_Resolution, padding / m_Resolution, 0, 0);
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
            m_ProbeBakingState = null;
        }

        public Vector4 FetchSlice(CommandBuffer cmd, Texture texture, out int fetchIndex)
        {
            Debug.Assert(texture.dimension == TextureDimension.Cube);

            fetchIndex = m_FrameProbeIndex++;

            bool updateTexture;

            Vector4 scaleOffset = Vector4.zero;

            if (m_TextureAtlas.IsCached(out scaleOffset, GetTextureID(texture)))
                updateTexture = NeedsUpdate(texture) || m_ProbeBakingState[scaleOffset] != ProbeFilteringState.Ready;
            else
                updateTexture = true;

            if (updateTexture)
            {
                if(!UpdateTexture(cmd, texture, ref scaleOffset))
                {
                    //@ We should have eviction mechanism.
                    Debug.LogError("No more space in the reflection probe atlas. To solve this issue, increase the size of the Reflection Probe Atlas in the HDRP settings.");
                }
            }

            return scaleOffset;
        }

        public void NewFrame()
        {
        }

        public void ClearAtlasAllocator()
        {
            //@ Cache must be persistent.
            m_FrameProbeIndex = 0;
            m_TextureAtlas.ResetAllocator();
        }
    }
}

