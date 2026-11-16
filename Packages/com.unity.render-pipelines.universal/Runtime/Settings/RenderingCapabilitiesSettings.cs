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
        [Tooltip("Developer Mode only, work in progress. Declares URP's global shader variables as a constant buffer instead of loose uniforms, on all graphics APIs except OpenGL ES 3. Warning, this currently costs more memory and triggers a full shaders re-compilation.")]
        bool m_UseGlobalConstantBuffer = false;
        #endregion

        #region Data Accessors

        /// <summary>
        /// When enabled, URP declares its global shader variables as a constant buffer instead of loose uniforms,
        /// by adding the URP_GLOBAL_CONSTANT_BUFFER shader define. The define is global and applies to every
        /// graphics API, but OpenGL ES 3 keeps loose uniforms. Work in progress.
        /// </summary>
        // Only the inspector depends on Developer Mode (HideInInspector), not the value: the editor and the player both
        // read the serialized field. GlobalConstantBufferShaderDefine derives the shader define from this value, so the
        // shaders of a build always match what the runtime pushes, whoever builds the project. A Developer Mode gate here
        // made an editor outside Developer Mode strip the define while its players still pushed the constant buffer.
        public bool useGlobalConstantBuffer
        {
            get => m_UseGlobalConstantBuffer;
            set => this.SetValueAndNotify(ref m_UseGlobalConstantBuffer, value);
        }

        #endregion
    }
}
