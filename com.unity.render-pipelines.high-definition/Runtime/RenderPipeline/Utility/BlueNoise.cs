using UnityEngine.Assertions;
using UnityEngine.Rendering;

namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    // Blue noise texture bank
    public sealed class BlueNoise
    {
        public Texture2D[] textures16L { get { return m_Textures16L; } }
        public Texture2D[] textures16RGB { get { return m_Textures16RGB; } }

        public Texture2DArray textureArray16L { get { return m_TextureArray16L; } }
        public Texture2DArray textureArray16RGB { get { return m_TextureArray16RGB; } }

        readonly Texture2D[] m_Textures16L;
        readonly Texture2D[] m_Textures16RGB;

        Texture2DArray m_TextureArray16L;
        Texture2DArray m_TextureArray16RGB;

        public BlueNoise(HDRenderPipelineAsset asset)
        {
            var resources = asset.renderPipelineResources.textures;

            InitTextures(16, TextureFormat.Alpha8, resources.blueNoise16LTex, out m_Textures16L, out m_TextureArray16L);
            InitTextures(16, TextureFormat.RGB24, resources.blueNoise16RGBTex, out m_Textures16RGB, out m_TextureArray16RGB);
        }

        public void Cleanup()
        {
            CoreUtils.Destroy(m_TextureArray16L);
            CoreUtils.Destroy(m_TextureArray16RGB);

            m_TextureArray16L = null;
            m_TextureArray16RGB = null;
        }

        public Texture2D GetRandom16L()
        {
            return textures16L[(int)(Random.value * (textures16L.Length - 1))];
        }

        public Texture2D GetRandom16RGB()
        {
            return textures16RGB[(int)(Random.value * (textures16RGB.Length - 1))];
        }

        static void InitTextures(int size, TextureFormat format, Texture2D[] sourceTextures, out Texture2D[] destination, out Texture2DArray destinationArray)
        {
            Assert.IsNotNull(sourceTextures);

            int len = sourceTextures.Length;

            Assert.IsTrue(len > 0);

            destination = new Texture2D[len];
            destinationArray = new Texture2DArray(size, size, len, format, false, true);
            destinationArray.hideFlags = HideFlags.HideAndDontSave;

            for (int i = 0; i < len; i++)
            {
                var noiseTex = sourceTextures[i];

                // Fail safe; should never happen unless the resources asset is broken
                if (noiseTex == null)
                {
                    destination[i] = Texture2D.whiteTexture;
                    continue;
                }

                destination[i] = noiseTex;
                Graphics.CopyTexture(noiseTex, 0, 0, destinationArray, i, 0);
            }
        }
    }
}
