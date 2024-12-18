using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// A graphics settings container for settings related to the Render Graph for all Scriptable Render Pipelines.
    /// </summary>
    /// <remarks>
    /// To change those settings, go to Editor > Project Settings in the Graphics tab.
    /// Changing this through the API is only allowed in the Editor. In the Player, this raises an error.
    /// </remarks>
    /// <seealso cref="IRenderPipelineGraphicsSettings"/>
    /// <example>
    /// <para> This example demonstrates how to determine if your project uses RenderGraph's compilation caching. </para>
    /// <code>
    /// using UnityEngine.Rendering;
    /// 
    /// public static class RenderGraphHelper
    /// {
    ///     public static bool enableCompilationCaching
    ///     {
    ///         get
    ///         {
    ///             var gs = GraphicsSettings.GetRenderPipelineSettings&lt;RenderGraphGlobalSettings&gt;();
    ///             if (gs == null) //not in SRP
    ///                 return false;
    ///             return gs.enableCompilationCaching;
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    [Serializable] 
    [SupportedOnRenderPipeline] 
    [Categorization.CategoryInfo(Name = "Render Graph", Order = 50)]
    [Categorization.ElementInfo(Order = 0)]
    public class RenderGraphGlobalSettings : IRenderPipelineGraphicsSettings
    {
        enum Version
        {
            Initial,
            Count,
            Last = Count - 1
        }

        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        [SerializeField, HideInInspector]
        private Version m_version = Version.Last;
        int IRenderPipelineGraphicsSettings.version => (int)m_version;

        [RecreatePipelineOnChange , SerializeField, Tooltip("Enable caching of render graph compilation from one frame to another.")]
        private bool m_EnableCompilationCaching = true;

        /// <summary>Enable Compilation caching for render graph.</summary>
        public bool enableCompilationCaching
        {
            get => m_EnableCompilationCaching;
            set => this.SetValueAndNotify(ref m_EnableCompilationCaching, value);
        }

        [RecreatePipelineOnChange , SerializeField, Tooltip("Enable validity checks of render graph in Editor and Development mode. Always disabled in Release build.")]
        private bool m_EnableValidityChecks = true;

        /// <summary>Enable validity checks for render graph. Always disabled in Release mode.</summary>
        public bool enableValidityChecks
        {
            get => m_EnableValidityChecks;
            set => this.SetValueAndNotify(ref m_EnableValidityChecks, value);
        }
    }
}
