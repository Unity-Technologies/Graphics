using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A graphics settings container for rendering capabilities settings for <see cref="UniversalRenderPipeline"/>.
    /// </summary>
    /// <remarks>
    /// To change those settings, go to Editor > Project Settings in the Graphics tab (URP).
    /// Changing this through the API is only allowed in the Editor. In the Player, this raises an error.
    /// </remarks>
    /// <seealso cref="IRenderPipelineGraphicsSettings"/>
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "Rendering Capabilities", Order = 30), HideInInspector]
    [Categorization.ElementInfo(Order = 0)]
    public class RenderingCapabilitiesSettings : IRenderPipelineGraphicsSettings
    {
        #region Version
        internal enum Version : int
        {
            Initial = 0,
        }

        [SerializeField, HideInInspector]
        private Version m_Version;

        /// <summary>Indicates the current version of this settings container. Used exclusively for project upgrades.</summary>
        public int version => (int)m_Version;
        #endregion

        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        #region SerializeFields
        [SerializeField]
        [Tooltip("When enabled, URP uploads global shader variables as persistent GPU constant buffers instead of rebuilding them from loose uniforms before every draw call. " +
                 "This reduces CPU overhead on native graphics APIs. Has no effect on GLES3 (constant buffers are not supported on that backend).")]
        bool m_UsePersistentConstantBuffers = false;
        #endregion

        #region Data Accessors

        /// <summary>
        /// When enabled, URP uploads global shader variables as persistent GPU constant buffers,
        /// reducing CPU overhead on native graphics APIs (D3D11/12, Metal, Vulkan, NVN, GNM).
        /// </summary>
        public bool usePersistentConstantBuffers
        {
            get
            {
#if UNITY_EDITOR
                // Outside of Developer Mode the feature is hidden and treated as off,
                // so a value set in dev mode doesn't accidentally affect normal editor sessions.
                if (!UnityEditor.EditorPrefs.GetBool("DeveloperMode"))
                    return false;
#endif
                return m_UsePersistentConstantBuffers;
            }
            set => this.SetValueAndNotify(ref m_UsePersistentConstantBuffers, value);
        }

        #endregion
    }
}
