using System;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Specifies the logging level for shader variants
    /// </summary>
    public enum ShaderVariantLogLevel
    {
        /// <summary>Disable all log for Shader Variant</summary>
        [Tooltip("No shader variants are logged")]
        Disabled,

        /// <summary>Only logs SRP Shaders when logging Shader Variant</summary>
        [Tooltip("Only shaders that are compatible with SRPs (e.g., URP, HDRP) are logged")]
        OnlySRPShaders,

        /// <summary>Logs all Shader Variant</summary>
        [Tooltip("All shader variants are logged")]
        AllShaders,
    }

    /// <summary>
    /// This is a Graphics Settings container for settings related to shader stripping for all scriptable render pipelines.
    /// </summary>
    /// <remarks>
    /// To change those settings, go to  Editor > Project Settings in the Graphics tab.
    /// Changing this through the API is only allowed in the Editor. In the Player, this raises an error.
    /// </remarks>
    /// <seealso cref="IRenderPipelineGraphicsSettings"/>
    /// <example>
    /// <para> Here is an example of how to check if your project strips variant shaders when building a Player using URP. </para>
    /// <code>
    /// using UnityEngine.Rendering;
    /// 
    /// public static class ShaderStrippingHelper
    /// {
    ///     public static bool exportLog
    ///     {
    ///         get
    ///         {
    ///             var gs = GraphicsSettings.GetRenderPipelineSettings&lt;ShaderStrippingSetting&gt;();
    ///             if (gs == null) //not in any SRP
    ///                 return false;
    ///             return gs.exportShaderVariants;
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    [Serializable]
    [SupportedOnRenderPipeline]
    [Categorization.CategoryInfo(Name = "Additional Shader Stripping Settings", Order = 40)]
    [Categorization.ElementInfo(Order = 0)]
    public class ShaderStrippingSetting : IRenderPipelineGraphicsSettings
    {
        #region Version
        internal enum Version : int
        {
            Initial = 0,
        }

        [SerializeField] [HideInInspector]
        private Version m_Version = Version.Initial;

        /// <summary>Current version of the settings container. Used only for project upgrades.</summary>
        public int version => (int)m_Version;
        #endregion

        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        #region SerializeFields
        [SerializeField]
        [Tooltip("Controls whether to output shader variant information to a file.")]
        private bool m_ExportShaderVariants = true;

        [SerializeField]
        [Tooltip("Controls the level of logging of shader variant information outputted during the build process. Information appears in the Unity Console when the build finishes.")]
        private ShaderVariantLogLevel m_ShaderVariantLogLevel = ShaderVariantLogLevel.Disabled;

        [SerializeField]
        [Tooltip("When enabled, all debug display shader variants are removed when you build for the Unity Player. This decreases build time, but prevents the use of most Rendering Debugger features in Player builds.")]
        private bool m_StripRuntimeDebugShaders = true;
        #endregion

        #region Data Accessors
        /// <summary>
        /// Controls whether to output shader variant information to a file.
        /// </summary>
        public bool exportShaderVariants
        {
            get => m_ExportShaderVariants;
            set => this.SetValueAndNotify(ref m_ExportShaderVariants, value);
        }

        /// <summary>
        /// Controls the level of logging of shader variant information outputted during the build process.
        /// Information appears in the Unity Console when the build finishes.
        /// </summary>
        public ShaderVariantLogLevel shaderVariantLogLevel
        {
            get => m_ShaderVariantLogLevel;
            set => this.SetValueAndNotify(ref m_ShaderVariantLogLevel, value);
        }

        /// <summary>
        /// When enabled, all debug display shader variants are removed when you build for the Unity Player.
        /// This decreases build time, but prevents the use of most Rendering Debugger features in Player builds.
        /// </summary>
        public bool stripRuntimeDebugShaders
        {
            get => m_StripRuntimeDebugShaders;
            set => this.SetValueAndNotify(ref m_StripRuntimeDebugShaders, value);
        }
        #endregion
    }
}
