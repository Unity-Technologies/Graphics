using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// A graphics settings container for settings related to additional <see cref="RenderPipelineAsset"/> inclusion at build time.
    /// </summary>
    /// <remarks>
    /// These settings are not editable through the editor's UI but can be changed through the API for advanced usage. 
    /// Changing this through the API is only allowed in the Editor. In the Player, this raises an error. 
    /// 
    /// By default, only RPAsset, in Quality Settings, is embedded in the build. This allows you to add assets.
    /// Add any render pipeline assets you use in your project either through this or directly in Quality Settings. They contain data listing what resources need to be embedded in the build.
    /// It is highly recommended not to change it unless you know what you are doing. Otherwise, this may lead to unexpected changes in your Player.
    /// </remarks>
    /// <seealso cref="IRenderPipelineGraphicsSettings"/>
    /// <example>
    /// <para> Here is an example of how to determine what label to use to embed additional assets. </para>
    /// <code>
    /// using UnityEngine.Rendering;
    /// 
    /// public static class RPAssetIncludedHelper
    /// {
    ///     public static string label
    ///     {
    ///         get
    ///         {
    ///             var gs = GraphicsSettings.GetRenderPipelineSettings&lt;IncludeAdditionalRPAssets&gt;();
    ///             if (gs == null) //not in SRP
    ///                 return null;
    ///             if (!gs.includeAssetsByLabel)
    ///                 return null;
    ///             return gs.labelToInclude;
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    [Serializable]
    [SupportedOnRenderPipeline]
    [Categorization.CategoryInfo(Name = "H: RP Assets Inclusion", Order = 990), HideInInspector]
    public class IncludeAdditionalRPAssets : IRenderPipelineGraphicsSettings
    {
        enum Version
        {
            Initial,

            Count,
            Last = Count - 1
        }
        [SerializeField, HideInInspector]
        private Version m_version = Version.Last;
        int IRenderPipelineGraphicsSettings.version => (int)m_version;

        [SerializeField]
        private bool m_IncludeReferencedInScenes;

        /// <summary> Additionaly include RPAsset referenced in Scene. </summary>
        public bool includeReferencedInScenes
        {
            get => m_IncludeReferencedInScenes;
            set => this.SetValueAndNotify(ref m_IncludeReferencedInScenes, value, nameof(m_IncludeReferencedInScenes));
        }
        
        [SerializeField]
        private bool m_IncludeAssetsByLabel;
        
        /// <summary> Additionaly include RPAsset that have a specific label. </summary>
        public bool includeAssetsByLabel
        {
            get => m_IncludeAssetsByLabel;
            set => this.SetValueAndNotify(ref m_IncludeAssetsByLabel, value, nameof(m_IncludeAssetsByLabel));
        }
        
        [SerializeField]
        private string m_LabelToInclude;
        
        /// <summary> Label to use when including RPAsset by label. </summary>
        public string labelToInclude
        {
            get => m_LabelToInclude;
            set => this.SetValueAndNotify(ref m_LabelToInclude, value, nameof(m_LabelToInclude));
        }
    }
}
