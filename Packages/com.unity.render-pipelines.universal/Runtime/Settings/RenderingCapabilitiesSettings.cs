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
    [Categorization.CategoryInfo(Name = "Rendering Capabilities", Order = 30)]
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
        [Tooltip("When enabled, URP binds its global shader variables as a persistent constant buffer instead of letting the engine fill the buffer from loose uniforms. You can't change these variables with SetGlobal*, Material.Set*, a MaterialPropertyBlock, or ComputeShader.Set*. In render graph passes, use UniversalGlobalShaderData or Universal2DGlobalShaderData instead. OpenGL ES 3 (except on WebGL) and OpenGL Core always use loose uniforms.")]
        bool m_UsePersistentGlobalConstantBuffer = false;
        #endregion

        #region Data Accessors

        /// <summary>
        /// Whether URP binds its global shader variables as a persistent constant buffer (the Use Persistent Global Constant
        /// Buffer setting).
        /// </summary>
        /// <remarks>
        /// When enabled (persistent mode), URP binds its global constant buffer itself, and scripts can't change its variables
        /// with `SetGlobal*`, `Material.Set*`, a `MaterialPropertyBlock`, or `ComputeShader.Set*`. Render graph passes change
        /// them through <see cref="UnityEngine.Experimental.Rendering.UniversalGlobalShaderData"/> or
        /// <see cref="UnityEngine.Experimental.Rendering.Universal2DGlobalShaderData"/>, which work in both modes.
        /// When disabled (transient mode), the engine fills the buffer from URP loose uniforms. Both modes use the same
        /// shaders, so changing this setting doesn't recompile shaders. OpenGL ES 3 (except on WebGL) and OpenGL Core always
        /// use loose uniforms.
        /// </remarks>
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
