using System;
using System.ComponentModel;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class containing texture resources used in URP.
    /// </summary>
    [Serializable]
    [HideInInspector]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Category("Resources/Runtime Textures")]
    public class UniversalRenderPipelineRuntimeTextures : IRenderPipelineResources
    {
        /// <summary>
        ///  Version of the Texture resources
        /// </summary>
        public int version => 0;

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
    }
}
