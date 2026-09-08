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
        [Tooltip("Developer Mode only, work in progress. Declares URP's global shader variables as a constant buffer instead of loose uniforms, on all graphics APIs. Warning, this currently costs more memory and triggers a full shaders re-compilation.")]
        bool m_UseGlobalConstantBuffer = false;
        #endregion

        #region Data Accessors

        /// <summary>
        /// When enabled, URP declares its global shader variables as a constant buffer instead of loose uniforms,
        /// by adding the URP_GLOBAL_CONSTANT_BUFFER shader define. The define is global and applies to every
        /// graphics API. Work in progress.
        /// </summary>
        // KNOWN LIMITATION: the value is serialized in the project but read through a per-machine preference, so
        // leaving Developer Mode strips a committed define and reimports every shader, and re-entering adds it back.
        // Tolerable while nothing reads useGlobalConstantBuffer, but the gate has to go before the upload path lands:
        // it is #if UNITY_EDITOR, so a player reads the field directly and a non-dev-mode build would ship
        // loose-uniform shaders against a cbuffer-pushing runtime.
        public bool useGlobalConstantBuffer
        {
            get
            {
#if UNITY_EDITOR
                // Outside of Developer Mode the feature is hidden and treated as off,
                // so a value set in dev mode doesn't accidentally affect normal editor sessions.
                // Must be Unsupported.IsDeveloperMode(), the same source the inspector uses to decide whether to
                // ignore HideInInspector: EditorPrefs.GetBool("DeveloperMode") defaults to false, while the native
                // side defaults it to "is this a source build", so on a source build the toggle would show but read off.
                if (!UnityEditor.Unsupported.IsDeveloperMode())
                    return false;
#endif
                return m_UseGlobalConstantBuffer;
            }
            set => this.SetValueAndNotify(ref m_UseGlobalConstantBuffer, value);
        }

        #endregion
    }
}
