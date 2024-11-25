using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Class that stores the Variable Rate Shading common global resources
    /// </summary>
    [Serializable]
    [SupportedOnRenderPipeline]
    [Categorization.CategoryInfo(Name = "R: VRS - Runtime Resources", Order = 1000), HideInInspector]
    public sealed class VrsRenderPipelineRuntimeResources : IRenderPipelineResources
    {
        /// <summary>
        /// Version of the Vrs Resources
        /// </summary>
        public int version => 0;

        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        [SerializeField]
        [ResourcePath("Runtime/Vrs/Shaders/VrsTexture.compute")]
        ComputeShader m_TextureComputeShader;

        /// <summary>
        /// General Vrs compute shader.
        /// </summary>
        public ComputeShader textureComputeShader
        {
            get => m_TextureComputeShader;
            set => this.SetValueAndNotify(ref m_TextureComputeShader, value, nameof(m_TextureComputeShader));
        }

        [SerializeField]
        [ResourcePath("Runtime/Vrs/Shaders/VrsVisualization.shader")]
        Shader m_VisualizationShader;

        /// <summary>
        /// Show resource shader.
        /// </summary>
        public Shader visualizationShader
        {
            get => m_VisualizationShader;
            set => this.SetValueAndNotify(ref m_VisualizationShader, value, nameof(m_VisualizationShader));
        }

        [SerializeField]
        [Tooltip("Colors to visualize the shading rates")]
        VrsLut m_VisualizationLookupTable = VrsLut.CreateDefault();

        /// <summary>
        /// Shading rate visualization lookup table.
        /// </summary>
        public VrsLut visualizationLookupTable
        {
            get => m_VisualizationLookupTable;
            set => this.SetValueAndNotify(ref m_VisualizationLookupTable, value, nameof(m_VisualizationLookupTable));
        }

        [SerializeField]
        [Tooltip("Colors to convert between shading rates and textures")]
        VrsLut m_ConversionLookupTable = VrsLut.CreateDefault();

        /// <summary>
        /// texture to/from Shading rate conversion lookup table.
        /// </summary>
        public VrsLut conversionLookupTable
        {
            get => m_ConversionLookupTable;
            set => this.SetValueAndNotify(ref m_ConversionLookupTable, value, nameof(m_ConversionLookupTable));
        }
    }
}
