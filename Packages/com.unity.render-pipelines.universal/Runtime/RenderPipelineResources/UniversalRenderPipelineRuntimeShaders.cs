using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class containing shader resources used in URP.
    /// </summary>
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "R: Runtime Shaders", Order = 1000), HideInInspector]
    public class UniversalRenderPipelineRuntimeShaders : IRenderPipelineResources
    {
        [SerializeField][HideInInspector] private int m_Version = 0;

        /// <summary>Version of the resource. </summary>
        public int version => m_Version;
        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        [SerializeField, ResourcePath("Shaders/Utils/FallbackError.shader")]
        Shader m_FallbackErrorShader;

        /// <summary>
        /// Fallback error shader
        /// </summary>
        public Shader fallbackErrorShader
        {
            get => m_FallbackErrorShader;
            set => this.SetValueAndNotify(ref m_FallbackErrorShader, value, nameof(m_FallbackErrorShader));
        }


        [SerializeField]
        [ResourcePath("Shaders/Utils/BlitHDROverlay.shader")]
        internal Shader m_BlitHDROverlay;

        /// <summary>
        /// Blit HDR Overlay shader.
        /// </summary>
        public Shader blitHDROverlay
        {
            get => m_BlitHDROverlay;
            set => this.SetValueAndNotify(ref m_BlitHDROverlay, value, nameof(m_BlitHDROverlay));
        }

        [SerializeField]
        [ResourcePath("Shaders/Utils/CoreBlit.shader")]
        internal Shader m_CoreBlitPS;

        /// <summary>
        /// Core Blit shader.
        /// </summary>
        public Shader coreBlitPS
        {
            get => m_CoreBlitPS;
            set => this.SetValueAndNotify(ref m_CoreBlitPS, value, nameof(m_CoreBlitPS));
        }

        [SerializeField]
        [ResourcePath("Shaders/Utils/CoreBlitColorAndDepth.shader")]
        internal Shader m_CoreBlitColorAndDepthPS;

        /// <summary>
        /// Core Blit Color And Depth shader.
        /// </summary>
        public Shader coreBlitColorAndDepthPS
        {
            get => m_CoreBlitColorAndDepthPS;
            set => this.SetValueAndNotify(ref m_CoreBlitColorAndDepthPS, value, nameof(m_CoreBlitColorAndDepthPS));
        }

        [SerializeField]
        [ResourcePath("Shaders/Utils/Sampling.shader")]
        private Shader m_SamplingPS;

        /// <summary>
        /// Sampling shader.
        /// </summary>
        public Shader samplingPS
        {
            get => m_SamplingPS;
            set => this.SetValueAndNotify(ref m_SamplingPS, value, nameof(m_SamplingPS));
        }

        #region Terrain
        [Header("Terrain")]
        [SerializeField]
        [ResourcePath("Shaders/Terrain/TerrainDetailLit.shader")]
        private Shader m_TerrainDetailLit;

        /// <summary>
        /// Returns the terrain detail lit shader that this asset uses.
        /// </summary>
        public Shader terrainDetailLitShader
        {
            get => m_TerrainDetailLit;
            set => this.SetValueAndNotify(ref m_TerrainDetailLit, value);
        }

        [SerializeField]
        [ResourcePath("Shaders/Terrain/WavingGrassBillboard.shader")]
        private Shader m_TerrainDetailGrassBillboard;

        /// <summary>
        /// Returns the terrain detail grass billboard shader that this asset uses.
        /// </summary>
        public Shader terrainDetailGrassBillboardShader
        {
            get => m_TerrainDetailGrassBillboard;
            set => this.SetValueAndNotify(ref m_TerrainDetailGrassBillboard, value);
        }

        [SerializeField]
        [ResourcePath("Shaders/Terrain/WavingGrass.shader")]
        private Shader m_TerrainDetailGrass;

        /// <summary>
        /// Returns the terrain detail grass shader that this asset uses.
        /// </summary>
        public Shader terrainDetailGrassShader
        {
            get => m_TerrainDetailGrass;
            set => this.SetValueAndNotify(ref m_TerrainDetailGrass, value);
        }
        #endregion
    }
}
