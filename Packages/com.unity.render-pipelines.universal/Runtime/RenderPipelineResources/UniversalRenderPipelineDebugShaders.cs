using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A resource container for debug shaders used for <see cref="UniversalRenderPipeline"/>.
    /// </summary>
    /// <remarks>
    /// You cannot edit these resources through the editor's UI; use the API for advanced changes.
    /// Changing this through the API is only allowed in the Editor. In the Player, this raises an error.
    /// 
    /// This container is removed for non-development build.
    /// </remarks>
    /// <seealso cref="IRenderPipelineResources"/>
    /// <example>
    /// <para> Here is an example of how to get the replacement pixel shader used by URP. </para>
    /// <code>
    /// using UnityEngine.Rendering;
    /// using UnityEngine.Rendering.Universal;
    /// 
    /// public static class URPUniversalRendererDebugShadersHelper
    /// {
    ///     public static Shader replacementPS
    ///     {
    ///         get
    ///         {
    ///             var gs = GraphicsSettings.GetRenderPipelineSettings&lt;UniversalRenderPipelineDebugShaders&gt;();
    ///             if (gs == null) //not in URP or not in development build
    ///                 return null;
    ///             return gs.debugReplacementPS;
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "R: Debug Shaders", Order = 1000), HideInInspector]
    public class UniversalRenderPipelineDebugShaders : IRenderPipelineResources
    {
        /// <summary>
        /// Current version of this resource container. Used only for upgrading a project.
        /// </summary>
        public int version => 0;

        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild =>
#if DEVELOPMENT_BUILD || UNITY_EDITOR
            true;
#else
            false;
#endif

        [SerializeField]
        [ResourcePath("Shaders/Debug/DebugReplacement.shader")]
        Shader m_DebugReplacementPS;

        /// <summary>
        /// Debug shader used to output interpolated vertex attributes.
        /// </summary>
        public Shader debugReplacementPS
        {
            get => m_DebugReplacementPS;
            set => this.SetValueAndNotify(ref m_DebugReplacementPS, value, nameof(m_DebugReplacementPS));
        }

        [SerializeField]
        [ResourcePath("Shaders/Debug/HDRDebugView.shader")]
        Shader m_HdrDebugViewPS;

        /// <summary>
        /// Debug shader used to output HDR Chromacity mapping.
        /// </summary>
        public Shader hdrDebugViewPS
        {
            get => m_HdrDebugViewPS;
            set => this.SetValueAndNotify(ref m_HdrDebugViewPS, value, nameof(m_HdrDebugViewPS));
        }

        [SerializeField]
        [ResourcePath("Shaders/Debug/ProbeVolumeSamplingDebugPositionNormal.compute")]
        ComputeShader m_ProbeVolumeSamplingDebugComputeShader;

        /// <summary>
        /// Debug shader used to output world position and world normal for the pixel under the cursor.
        /// </summary>
        public ComputeShader probeVolumeSamplingDebugComputeShader
        {
            get => m_ProbeVolumeSamplingDebugComputeShader;
            set => this.SetValueAndNotify(ref m_ProbeVolumeSamplingDebugComputeShader, value, nameof(m_ProbeVolumeSamplingDebugComputeShader));
        }
    }
}
