using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A Graphics Settings container for settings related to terrain shaders in <see cref="UniversalRenderPipeline"/>.
    /// </summary>
    /// <remarks>
    /// To change those settings, go to Editor > Project Settings in the Graphics tab (URP).
    /// Changing those settings through the API is only allowed in the Editor. In the Player, this raises an error.
    /// 
    /// Unity removes this container from Players at build time.
    /// </remarks>
    /// <seealso cref="IRenderPipelineGraphicsSettings"/>
    /// <example>
    /// <para> Here is an example of how to determine if your project has included terrain shaders when building a Player with URP. </para>
    /// <code>
    /// using UnityEngine.Rendering;
    /// using UnityEngine.Rendering.Universal;
    /// 
    /// public static class URPTerrainShaderHelper
    /// {
    ///     public static bool enabled
    ///     {
    ///         get
    ///         {
    ///             var gs = GraphicsSettings.GetRenderPipelineSettings&lt;URPTerrainShaderSetting&gt;();
    ///             if (gs == null) //not in URP or in a Player
    ///                 return false;
    ///             return gs.includeTerrainShaders;
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "Terrain Shader Inclusion Settings", Order = 50)]
    [Categorization.ElementInfo(Order = 10)]
    public class URPTerrainShaderSetting : IRenderPipelineGraphicsSettings
    {
        #region Version
        internal enum Version : int
        {
            Initial = 0,
        }

        [SerializeField][HideInInspector]
        private Version m_Version;

        /// <summary>Indicates the current version of this settings container. Used exclusively for project upgrades.</summary>
        public int version => (int)m_Version;
        #endregion

        #region Settings
        [SerializeField]
        [Tooltip("Include terrain shaders in build even if not referenced.")]
        bool m_IncludeTerrainShaders = true;

        /// <summary>
        /// Controls whether terrain shaders are included in the build.
        /// </summary>
        public bool includeTerrainShaders
        {
            get => m_IncludeTerrainShaders;
            set => this.SetValueAndNotify(ref m_IncludeTerrainShaders, value);
        }
        #endregion
    }
}
