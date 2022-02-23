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

        Material m_ConvertTextureMaterial;

        public ReflectionProbeCache2D(HDRenderPipelineRuntimeResources defaultResources, IBLFilterBSDF[] iblFiltersBSDF, int resolution, GraphicsFormat format)
        {
            Debug.Assert(format == GraphicsFormat.RGB_BC6H_SFloat || format == GraphicsFormat.B10G11R11_UFloatPack32 || format == GraphicsFormat.R16G16B16A16_SFloat,
                "Reflection Probe Cache format for HDRP can only be BC6H, FP16 or R11G11B10.");

            m_IBLFiltersBSDF = iblFiltersBSDF;
            m_Resolution = resolution;
            m_Format = format;

            int mipPadding = 0;
            m_TextureAtlas = new PowerOfTwoTextureAtlas(resolution, mipPadding, format, FilterMode.Trilinear, "ReflectionProbeCache2D Atlas", true);

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

            RenderTexture convolvedTextureTemp;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(HDProfileId.ConvolveReflectionProbe)))
            {
                RenderTexture convertedTextureTemp = null;

                if (renderTexture)
                {
                    cmd.GenerateMips(renderTexture);
                }
                else if (GraphicsFormatUtility.GetGraphicsFormat(cubemap.format, false) != m_Format || cubemap.mipmapCount == 1)
                {
                    //@ We can get rid of most of conversions if we replace CopyTexture.
                    //@ Inside FilterCubemap with Blit.

                    //@ Replace with cmb.GetTemporaryRT
                    convertedTextureTemp = RenderTexture.GetTemporary(texture.width, texture.height, 1, m_Format);
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

                convolvedTextureTemp = RenderTexture.GetTemporary(texture.width, texture.height, 1, m_Format);
                convolvedTextureTemp.hideFlags = HideFlags.HideAndDontSave;
                convolvedTextureTemp.dimension = TextureDimension.Cube;
                convolvedTextureTemp.useMipMap = true;
                convolvedTextureTemp.autoGenerateMips = false;
                convolvedTextureTemp.name = CoreUtils.GetRenderTargetAutoName(texture.width, texture.height, 1, m_Format, "ConvolvedReflectionProbeTemp", mips: true);
                convolvedTextureTemp.Create();

                //@ All filters 
                m_IBLFiltersBSDF[0].FilterCubemap(cmd, texture, convolvedTextureTemp);

                RenderTexture.ReleaseTemporary(convertedTextureTemp);
            }

            return convolvedTextureTemp;
        }

        private bool UpdateTexture(CommandBuffer cmd, Texture texture, ref Vector4 scaleOffset)
        {
            bool success = false;

            m_ProbeBakingState[scaleOffset] = ProbeFilteringState.Convolving;

            RenderTexture convolvedTextureTemp = ConvolveProbeTexture(cmd, texture);

            if (convolvedTextureTemp == null)
                return false;

            //@ Add compression
            //@ Get octahedral 2D texture and convert it in BC6H the same way as in EncodeBC6H.DefaultInstance.EncodeFastCubemap
            if (m_Format == GraphicsFormat.RGB_BC6H_SFloat) 
            {
                Debug.Assert(false, "Not supported for now.");
            }

            if (m_TextureAtlas.IsCached(out scaleOffset, GetTextureID(texture)))
            {
                if (m_TextureAtlas.NeedsUpdate(texture, false))
                    m_TextureAtlas.BlitCubeTexture2D(cmd, scaleOffset, convolvedTextureTemp, true, GetTextureID(texture));
            }
            else
            {
                if (!m_TextureAtlas.AllocateTextureWithoutBlit(GetTextureID(texture), convolvedTextureTemp.width, convolvedTextureTemp.height, ref scaleOffset))
                    return false;

                m_TextureAtlas.BlitCubeTexture2D(cmd, scaleOffset, convolvedTextureTemp, true, GetTextureID(texture));

                success = true;
            }

            RenderTexture.ReleaseTemporary(convolvedTextureTemp);

            m_ProbeBakingState[scaleOffset] = ProbeFilteringState.Ready;

            return success;
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

