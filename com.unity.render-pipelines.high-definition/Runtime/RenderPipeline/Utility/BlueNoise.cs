using UnityEngine.Assertions;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A bank of pre-generated blue noise textures.
    /// </summary>
    public sealed class BlueNoise
    {
        /// <summary>
        /// Single channel (luminance only) 16x16 textures.
        /// </summary>
        public Texture2D[] textures16L { get { return m_Textures16L; } }

        /// <summary>
        /// Multi-channel (red, gree, blue) 16x16 textures.
        /// </summary>
        public Texture2D[] textures16RGB { get { return m_Textures16RGB; } }

        /// <summary>
        /// A TextureArray containing all the single channel (luminance only) 16x16 textures.
        /// </summary>
        public Texture2DArray textureArray16L { get { return m_TextureArray16L; } }

        /// <summary>
        /// A TextureArray containing all the multi-channel (red, green, blue) 16x16 textures.
        /// </summary>
        public Texture2DArray textureArray16RGB { get { return m_TextureArray16RGB; } }

        readonly Texture2D[] m_Textures16L;
        readonly Texture2D[] m_Textures16RGB;

        Texture2DArray m_TextureArray16L;
        Texture2DArray m_TextureArray16RGB;

        RenderPipelineResources m_RenderPipelineResources;

        static readonly System.Random m_Random = new System.Random();

        /// <summary>
        /// Creates a new instance of the blue noise texture bank.
        /// </summary>
        /// <param name="resources">A reference to the render pipeline resources asset.</param>
        internal BlueNoise(RenderPipelineResources resources)
        {
            m_RenderPipelineResources = resources;
            InitTextures(16, TextureFormat.Alpha8, resources.textures.blueNoise16LTex, out m_Textures16L, out m_TextureArray16L);
            InitTextures(16, TextureFormat.RGB24, resources.textures.blueNoise16RGBTex, out m_Textures16RGB, out m_TextureArray16RGB);
        }

        /// <summary>
        /// Cleanups up internal textures. This method should be called before disposing of this instance.
        /// </summary>
        public void Cleanup()
        {
            CoreUtils.Destroy(m_TextureArray16L);
            CoreUtils.Destroy(m_TextureArray16RGB);

            m_TextureArray16L = null;
            m_TextureArray16RGB = null;
        }

        /// <summary>
        /// Returns a random, single channel (luminance only) 16x16 blue noise texture.
        /// </summary>
        /// <returns>A single channel (luminance only) 16x16 blue noise texture.</returns>
        public Texture2D GetRandom16L()
        {
            return textures16L[(int)(m_Random.NextDouble() * (textures16L.Length - 1))];
        }

        /// <summary>
        /// Returns a random, multi-channel (red, green blue) 16x16 blue noise texture.
        /// </summary>
        /// <returns>A multi-channel (red, green blue) 16x16 blue noise texture.</returns>
        public Texture2D GetRandom16RGB()
        {
            return textures16RGB[(int)(m_Random.NextDouble() * (textures16RGB.Length - 1))];
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

        // Structure that holds all the dithered sampling texture that shall be binded at dispatch time.
        internal struct DitheredTextureSet
        {
            public Texture2D owenScrambled256Tex;
            public Texture2D scramblingTile;
            public Texture2D rankingTile;
            public Texture2D scramblingTex;
        }

        internal void BindDitheredRNGData1SPP(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._OwenScrambledTexture, m_RenderPipelineResources.textures.owenScrambled256Tex);
            cmd.SetGlobalTexture(HDShaderIDs._ScramblingTileXSPP, m_RenderPipelineResources.textures.scramblingTile1SPP);
            cmd.SetGlobalTexture(HDShaderIDs._RankingTileXSPP, m_RenderPipelineResources.textures.rankingTile1SPP);
            cmd.SetGlobalTexture(HDShaderIDs._ScramblingTexture, m_RenderPipelineResources.textures.scramblingTex);
        }

        internal void BindDitheredRNGData8SPP(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._OwenScrambledTexture, m_RenderPipelineResources.textures.owenScrambled256Tex);
            cmd.SetGlobalTexture(HDShaderIDs._ScramblingTileXSPP, m_RenderPipelineResources.textures.scramblingTile8SPP);
            cmd.SetGlobalTexture(HDShaderIDs._RankingTileXSPP, m_RenderPipelineResources.textures.rankingTile8SPP);
            cmd.SetGlobalTexture(HDShaderIDs._ScramblingTexture, m_RenderPipelineResources.textures.scramblingTex);
        }

        internal DitheredTextureSet DitheredTextureSet8SPP()
        {
            DitheredTextureSet ditheredTextureSet = new DitheredTextureSet();
            ditheredTextureSet.owenScrambled256Tex = m_RenderPipelineResources.textures.owenScrambled256Tex;
            ditheredTextureSet.scramblingTile = m_RenderPipelineResources.textures.scramblingTile8SPP;
            ditheredTextureSet.rankingTile = m_RenderPipelineResources.textures.rankingTile8SPP;
            ditheredTextureSet.scramblingTex = m_RenderPipelineResources.textures.scramblingTex;
            return ditheredTextureSet;
        }

        internal DitheredTextureSet DitheredTextureSet256SPP()
        {
            DitheredTextureSet ditheredTextureSet = new DitheredTextureSet();
            ditheredTextureSet.owenScrambled256Tex = m_RenderPipelineResources.textures.owenScrambled256Tex;
            ditheredTextureSet.scramblingTile = m_RenderPipelineResources.textures.scramblingTile256SPP;
            ditheredTextureSet.rankingTile = m_RenderPipelineResources.textures.rankingTile256SPP;
            ditheredTextureSet.scramblingTex = m_RenderPipelineResources.textures.scramblingTex;
            return ditheredTextureSet;
        }

        internal void BindDitheredRNGData256SPP(CommandBuffer cmd)
        {
            cmd.SetGlobalTexture(HDShaderIDs._OwenScrambledTexture, m_RenderPipelineResources.textures.owenScrambled256Tex);
            cmd.SetGlobalTexture(HDShaderIDs._ScramblingTileXSPP, m_RenderPipelineResources.textures.scramblingTile256SPP);
            cmd.SetGlobalTexture(HDShaderIDs._RankingTileXSPP, m_RenderPipelineResources.textures.rankingTile256SPP);
            cmd.SetGlobalTexture(HDShaderIDs._ScramblingTexture, m_RenderPipelineResources.textures.scramblingTex);
        }

        internal static void BindDitheredTextureSet(CommandBuffer cmd, DitheredTextureSet ditheredTextureSet)
        {
            cmd.SetGlobalTexture(HDShaderIDs._OwenScrambledTexture, ditheredTextureSet.owenScrambled256Tex);
            cmd.SetGlobalTexture(HDShaderIDs._ScramblingTileXSPP, ditheredTextureSet.scramblingTile);
            cmd.SetGlobalTexture(HDShaderIDs._RankingTileXSPP, ditheredTextureSet.rankingTile);
            cmd.SetGlobalTexture(HDShaderIDs._ScramblingTexture, ditheredTextureSet.scramblingTex);
        }

    }
}
