using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class containing texture resources used in URP.
    /// </summary>
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "R: Runtime Textures", Order = 1000), HideInInspector]
    public class UniversalRenderPipelineRuntimeTextures : IRenderPipelineResources
    {
        [SerializeField][HideInInspector] private int m_Version = 1;

        /// <summary>
        ///  Version of the Texture resources
        /// </summary>
        public int version => m_Version;

        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        [SerializeField]
        [ResourcePath("Textures/BlueNoise64/L/LDR_LLL1_0.png")]
        private Texture2D m_BlueNoise64LTex;

        /// <summary>
        /// Pre-baked blue noise textures.
        /// </summary>
        public Texture2D blueNoise64LTex
        {
            get => m_BlueNoise64LTex;
            set => this.SetValueAndNotify(ref m_BlueNoise64LTex, value, nameof(m_BlueNoise64LTex));
        }

        [SerializeField]
        [ResourcePath("Textures/BayerMatrix.png")]
        private Texture2D m_BayerMatrixTex;

        /// <summary>
        /// Bayer matrix texture.
        /// </summary>
        public Texture2D bayerMatrixTex
        {
            get => m_BayerMatrixTex;
            set => this.SetValueAndNotify(ref m_BayerMatrixTex, value, nameof(m_BayerMatrixTex));
        }

        [SerializeField]
        [ResourcePath("Textures/DebugFont.tga")]
        private Texture2D m_DebugFontTex;

        /// <summary>
        /// Debug font texture.
        /// </summary>
        public Texture2D debugFontTexture
        {
            get => m_DebugFontTex;
            set => this.SetValueAndNotify(ref m_DebugFontTex, value, nameof(m_DebugFontTex));
        }

        private Texture2D m_StencilDitherTex = null;

        /// <summary>
        /// stencil-based lod texture.
        /// </summary>
        public Texture2D stencilDitherTex
        {
            get
            {
                if (!m_StencilDitherTex)
                {
                    m_StencilDitherTex = new Texture2D(width: 2, height: 2, textureFormat: TextureFormat.Alpha8, mipChain: false, linear: true);

                    // keep in sync with StencilDitherMaskSeed.shader
                    m_StencilDitherTex.SetPixel(0, 0, Color.red * 0.25f);
                    m_StencilDitherTex.SetPixel(1, 1, Color.red * 0.5f);
                    m_StencilDitherTex.SetPixel(0, 1, Color.red * 0.75f);
                    m_StencilDitherTex.SetPixel(1, 0, Color.red * 1.0f);
                    m_StencilDitherTex.Apply();
                }
                return m_StencilDitherTex;
            }
        }


    }
}
