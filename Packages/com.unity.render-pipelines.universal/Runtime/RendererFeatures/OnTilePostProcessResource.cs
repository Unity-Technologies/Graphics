using System;
using UnityEngine;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class containing shader resources used for On-Tile Post Processing Feature.
    /// </summary>
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "R: On Tile Post Process Resources", Order = 1000), HideInInspector]
    class OnTilePostProcessResource : IRenderPipelineResources
    {
        [SerializeField, HideInInspector]
        int m_Version = 0;

        /// <summary>Current version of the resource container. Used only for upgrading a project.</summary>
        public int version => m_Version;

        /// <summary>
        /// Indicates whether the resource is available in a player build.
        /// </summary>
        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        [SerializeField]
        [ResourcePath("Runtime/RendererFeatures/OnTileUberPost.shader")]
        Shader m_UberPostShader;

        /// <summary>
        /// Shader resources used for On Tile Post Processing.
        /// </summary>
        public Shader uberPostShader
        {
            get => m_UberPostShader;
            set => this.SetValueAndNotify(ref m_UberPostShader, value, nameof(m_UberPostShader));
        }
    }
}
