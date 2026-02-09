using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A resource container for shaders used for terrains in <see cref="UniversalRenderPipeline"/>.
    /// </summary>
    /// <remarks>
    /// You cannot edit these resources through the editor's UI; use the API for advanced changes.
    /// Changing this through the API is only allowed in the Editor. In the Player, this raises an error.
    /// </remarks>
    /// <seealso cref="IRenderPipelineResources"/>
    /// <example>
    /// <para> Here is an example of how to get the terrain detail lit shader used by URP. </para>
    /// <code>
    /// using UnityEngine.Rendering;
    /// using UnityEngine.Rendering.Universal;
    ///
    /// public static class URPUniversalRendererRuntimeShadersHelper
    /// {
    ///     public static Shader terrainDetailLit
    ///     {
    ///         get
    ///         {
    ///             if (GraphicsSettings.TryGetRenderPipelineSettings&lt;UniversalRenderPipelineRuntimeTerrainShaders&gt;(
    ///                 out var shadersResources))
    ///             {
    ///                 return shadersResources.terrainDetailLitShader;
    ///             }
    ///             return null;
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "R: Runtime Shaders", Order = 1000), HideInInspector]
    public class UniversalRenderPipelineRuntimeTerrainShaders : IRenderPipelineResources
    {
        [SerializeField][HideInInspector] private int m_Version = 0;

        /// <summary>The current version of the resource container. Used only for upgrading a project.</summary>
        public int version => m_Version;

        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild
        {
            get
            {
                // Check if the setting exists and is enabled
                if (GraphicsSettings.TryGetRenderPipelineSettings<URPTerrainShaderSetting>(out var settings))
                {
                    return settings.includeTerrainShaders;
                }
                return false; // Default to not including if settings don't exist
            }
        }

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
    }
}
