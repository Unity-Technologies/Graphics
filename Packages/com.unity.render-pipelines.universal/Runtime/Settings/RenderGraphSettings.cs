using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Build;
#endif

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A graphics settings container for settings related to Render Graph for <see cref="UniversalRenderPipeline"/>.
    /// </summary>
    /// <remarks>
    /// To change those settings, go to Editor > Project Settings in the Graphics tab (URP).
    /// Changing this through the API is only allowed in the Editor. In the Player, this raises an error.
    /// </remarks>
    /// <seealso cref="IRenderPipelineGraphicsSettings"/>
    /// <example>
    /// <para> This example demonstrates how to determine whether your project uses RenderGraph in URP. </para>
    /// <code>
    /// using UnityEngine.Rendering;
    /// using UnityEngine.Rendering.Universal;
    /// 
    /// public static class URPRenderGraphHelper
    /// {
    ///     public static bool enabled
    ///     {
    ///         get
    ///         {
    ///             var gs = GraphicsSettings.GetRenderPipelineSettings&lt;RenderGraphSettings&gt;();
    ///             if (gs == null) //not in URP
    ///                 return false;
    ///             return !gs.enableRenderCompatibilityMode;
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "Render Graph", Order = 50)]
    [Categorization.ElementInfo(Order = -10)]
    public class RenderGraphSettings : IRenderPipelineGraphicsSettings
    {
        #region Version
        internal enum Version : int
        {
            Initial = 0,
        }

        [SerializeField][HideInInspector]
        private Version m_Version;

        /// <summary>Current version of the settings container. Used only for upgrading a project.</summary>
        public int version => (int)m_Version;
        #endregion

        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        #region SerializeFields

        [SerializeField]
        [Tooltip("When enabled, URP does not use the Render Graph API to construct and execute the frame. Use this option only for compatibility purposes.")]
        [RecreatePipelineOnChange]
        private bool m_EnableRenderCompatibilityMode;
        #endregion

        #region Data Accessors

        /// <summary>
        /// When enabled, Universal Rendering Pipeline will not use Render Graph API to construct and execute the frame.
        /// </summary>
        public bool enableRenderCompatibilityMode
#if URP_COMPATIBILITY_MODE
        {
            get
            {
                if (RenderGraphGraphicsAutomatedTests.forceRenderGraphState.HasValue)
                    return !RenderGraphGraphicsAutomatedTests.forceRenderGraphState.Value;
                return m_EnableRenderCompatibilityMode;
            }
            set
            {
                this.SetValueAndNotify(ref m_EnableRenderCompatibilityMode, value, nameof(m_EnableRenderCompatibilityMode));
            }
        }
#else
        {
            //Temporarilly keep this boolean for all third parties support
            get => false;
            [Obsolete("Compatibility Mode is being removed. This setter is not accessible without the define URP_COMPATIBILITY_MODE. #from(6000.3) #breakingFrom(6000.3)", true)] set { }
        }
#endif

        #endregion
        
        #region Upgrade and checks
        
        internal void SetCompatibilityModeFromUpgrade(bool value) => m_EnableRenderCompatibilityMode = value;
        
#if UNITY_EDITOR
        internal bool GetSerializedCompatibilityModeForBuildCheck() => m_EnableRenderCompatibilityMode;

        internal void AddCompatibilityModeDefineForCurrentPlateform()
        {
            //There is no public API to iterate on all installed target plateform. Only updating current one.
            var activeBuildTarget = EditorUserBuildSettings.activeBuildTarget;
            var activeBuildTargetGroup = BuildPipeline.GetBuildTargetGroup(activeBuildTarget);
            var namedBuildTarget = NamedBuildTarget.FromBuildTargetGroup(activeBuildTargetGroup);

            string defineSymbols = PlayerSettings.GetScriptingDefineSymbols(namedBuildTarget);
            if (!string.IsNullOrEmpty(defineSymbols))
                defineSymbols += ";";
            defineSymbols += $"URP_COMPATIBILITY_MODE";
            PlayerSettings.SetScriptingDefineSymbols(namedBuildTarget, defineSymbols);

            AssetDatabase.SaveAssets(); //saving is required to recompile after the define is added.
        }
#endif

        #endregion
    }
}
