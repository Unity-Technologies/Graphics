using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A resource container for shaders used for <see cref="UniversalRenderPipeline"/>.
    /// </summary>
    /// <remarks>
    /// You cannot edit these resources through the editor's UI; use the API for advanced changes.
    /// Changing this through the API is only allowed in the Editor. In the Player, this raises an error.
    /// </remarks>
    /// <seealso cref="IRenderPipelineResources"/>
    /// <example>
    /// <para> Here is an example of how to get the blit shader used by URP. </para>
    /// <code>
    /// using UnityEngine.Rendering;
    /// using UnityEngine.Rendering.Universal;
    ///
    /// public static class URPUniversalRendererRuntimeShadersHelper
    /// {
    ///     public static Shader blit
    ///     {
    ///         get
    ///         {
    ///             var gs = GraphicsSettings.GetRenderPipelineSettings&lt;UniversalRenderPipelineRuntimeShaders&gt;();
    ///             if (gs == null) //not in URP
    ///                 return null;
    ///             return gs.coreBlitPS;
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "R: Runtime Shaders", Order = 1000), HideInInspector]
    public class UniversalRenderPipelineRuntimeShaders : IRenderPipelineResources
    {
        [SerializeField][HideInInspector] private int m_Version = 0;

        /// <summary>Current version of the resource container. Used only for upgrading a project.</summary>
        public int version => m_Version;
        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        [SerializeField, ResourcePath("Shaders/Utils/FallbackError.shader")]
        Shader m_FallbackErrorShader;

        /// <summary>
        /// Fallback shader used when error happens.
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
        /// Blit shader used for HDR Overlay.
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
        /// Default blit shader used for blit operation.
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
        /// Blit shader used for both Color And Depth blit operation.
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
        /// Shader used when sampling is required.
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
