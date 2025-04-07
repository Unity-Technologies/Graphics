using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A resource container for textures used for <see cref="UniversalRenderPipeline"/>.
    /// </summary>
    /// <remarks>
    /// You cannot edit these resources through the editor's UI; use the API for advanced changes.
    /// Changing this through the API is only allowed in the Editor. In the Player, this raises an error.
    /// 
    /// This container is removed for non-XR builds.
    /// </remarks>
    /// <seealso cref="IRenderPipelineResources"/>
    /// <example>
    /// <para> Here is an example of how to get the MotionVector shader used by URP for XR. </para>
    /// <code>
    /// using UnityEngine.Rendering;
    /// using UnityEngine.Rendering.Universal;
    /// 
    /// public static class URPUniversalRendererRuntimeXRResourcesHelper
    /// {
    ///     public static Shader motionVector
    ///     {
    ///         get
    ///         {
    ///             var gs = GraphicsSettings.GetRenderPipelineSettings&lt;UniversalRenderPipelineRuntimeXRResources&gt;();
    ///             if (gs == null) //not in URP or XR not enabled
    ///                 return null;
    ///             return gs.xrMotionVector;
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "R: Runtime XR", Order = 1000), HideInInspector]
    public class UniversalRenderPipelineRuntimeXRResources : IRenderPipelineResources
    {
        /// <summary>
        /// Current version of the resource container. Used only for upgrading a project.
        /// </summary>
        public int version => 0;

        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        [SerializeField]
        [ResourcePath("Shaders/XR/XROcclusionMesh.shader")]
        private Shader m_xrOcclusionMeshPS;

        /// <summary>
        /// XR Occlusion mesh shader.
        /// </summary>
        public Shader xrOcclusionMeshPS
        {
            get => m_xrOcclusionMeshPS;
            set => this.SetValueAndNotify(ref m_xrOcclusionMeshPS, value, nameof(m_xrOcclusionMeshPS));
        }

        [SerializeField]
        [ResourcePath("Shaders/XR/XRMirrorView.shader")]
        private Shader m_xrMirrorViewPS;

        /// <summary>
        /// XR Mirror View shader.
        /// </summary>
        public Shader xrMirrorViewPS
        {
            get => m_xrMirrorViewPS;
            set => this.SetValueAndNotify(ref m_xrMirrorViewPS, value, nameof(m_xrMirrorViewPS));
        }

        [SerializeField]
        [ResourcePath("Shaders/XR/XRMotionVector.shader")]
        private Shader m_xrMotionVector;

        /// <summary>
        /// XR MotionVector shader.
        /// </summary>
        public Shader xrMotionVector
        {
            get => m_xrMotionVector;
            set => this.SetValueAndNotify(ref m_xrMotionVector, value, nameof(m_xrMotionVector));
        }

        internal bool valid
        {
            get
            {
                if (xrOcclusionMeshPS == null)
                    return false;

                if (xrMirrorViewPS == null)
                    return false;

                if (m_xrMotionVector == null)
                    return false;

                return true;
            }
        }
    }
}
