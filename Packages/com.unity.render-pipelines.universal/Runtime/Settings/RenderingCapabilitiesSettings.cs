using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A graphics settings container for rendering capabilities settings for <see cref="UniversalRenderPipeline"/>.
    /// </summary>
    /// <remarks>
    /// To change those settings, go to Editor > Project Settings in the Graphics tab (URP).
    /// URP reads them every frame, so they can also change at runtime, in the Editor and in the Player.
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
        [Tooltip("URP binds its global shader variables as a persistent constant buffer instead of letting the engine fill it from loose uniforms. Scripts can then no longer change these variables with SetGlobal*, Material.Set*, a MaterialPropertyBlock or ComputeShader.Set*. OpenGL ES 3 outside WebGL and OpenGL Core keep loose uniforms.")]
        bool m_UsePersistentGlobalConstantBuffer = false;
        #endregion

        #region Data Accessors

        /// <summary>
        /// The Use Persistent Global Constant Buffer setting.
        /// When enabled, URP binds its global constant buffer itself (persistent mode), and scripts can no longer change its
        /// variables with SetGlobal*, Material.Set*, a MaterialPropertyBlock or ComputeShader.Set*. Otherwise the engine
        /// fills it from URP's loose uniforms (transient mode). Both modes run the same shaders, so changing it recompiles
        /// no shader. OpenGL ES 3 outside WebGL and OpenGL Core keep loose uniforms.
        /// </summary>
        public bool useGlobalConstantBuffer
        {
            get => m_UsePersistentGlobalConstantBuffer;
            set
            {
#if UNITY_EDITOR
                this.SetValueAndNotify(ref m_UsePersistentGlobalConstantBuffer, value);
#else
                m_UsePersistentGlobalConstantBuffer = value;
#endif
            }
        }

        #endregion
    }
}
