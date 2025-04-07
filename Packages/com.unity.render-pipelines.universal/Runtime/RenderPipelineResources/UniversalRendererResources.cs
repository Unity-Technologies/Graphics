using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// A resource container for resource used for <see cref="UniversalRenderPipeline"/>.
    /// </summary>
    /// <remarks>
    /// You cannot edit these resources through the editor's UI; use the API for advanced changes.
    /// Changing this through the API is only allowed in the Editor. In the Player, this raises an error.
    /// </remarks>
    /// <seealso cref="IRenderPipelineResources"/>
    /// <example>
    /// <para> Here is an example of how to get the MotionVector shader used by URP's Universal Renderer. </para>
    /// <code>
    /// using UnityEngine.Rendering;
    /// using UnityEngine.Rendering.Universal;
    /// 
    /// public static class URPUniversalRendererResourcesHelper
    /// {
    ///     public static Shader motionVector
    ///     {
    ///         get
    ///         {
    ///             var gs = GraphicsSettings.GetRenderPipelineSettings&lt;UniversalRendererResources&gt;();
    ///             if (gs == null) //not in URP or not with UniversalRenderer
    ///                 return null;
    ///             return gs.cameraMotionVector;
    ///         }
    ///     }
    /// }
    /// </code>
    /// </example>
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "R: Universal Renderer Shaders", Order = 1000), HideInInspector]
    public class UniversalRendererResources : IRenderPipelineResources
    {
        [SerializeField][HideInInspector] private int m_Version = 0;
        
        /// <summary>
        /// Current version of the resource container. Used only for upgrading a project.
        /// </summary>
        public int version => m_Version;
        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        [SerializeField]
        [ResourcePath("Shaders/Utils/CopyDepth.shader")]
        private Shader m_CopyDepthPS;

        /// <summary>
        /// Shader used to copy the depth buffer.
        /// </summary>
        public Shader copyDepthPS
        {
            get => m_CopyDepthPS;
            set => this.SetValueAndNotify(ref m_CopyDepthPS, value, nameof(m_CopyDepthPS));
        }

        [SerializeField]
        [ResourcePath("Shaders/CameraMotionVectors.shader")]
        private Shader m_CameraMotionVector;

        /// <summary>
        /// Shader used to compute Motion Vectors from the Camera.
        /// </summary>
        public Shader cameraMotionVector
        {
            get => m_CameraMotionVector;
            set => this.SetValueAndNotify(ref m_CameraMotionVector, value, nameof(m_CameraMotionVector));
        }

        [SerializeField]
        [ResourcePath("Shaders/Utils/StencilDeferred.shader")]
        private Shader m_StencilDeferredPS;

        /// <summary>
        /// Shader used to write stencil operations in deferred mode.
        /// </summary>
        public Shader stencilDeferredPS
        {
            get => m_StencilDeferredPS;
            set => this.SetValueAndNotify(ref m_StencilDeferredPS, value, nameof(m_StencilDeferredPS));
        }

        [SerializeField]
        [ResourcePath("Shaders/Utils/ClusterDeferred.shader")]
        private Shader m_ClusterDeferred;

        /// <summary>
        /// Cluster Deferred shader.
        /// </summary>
        public Shader clusterDeferred
        {
            get => m_ClusterDeferred;
            set => this.SetValueAndNotify(ref m_ClusterDeferred, value, nameof(m_ClusterDeferred));
        }

        [SerializeField]
        [ResourcePath("Shaders/Utils/StencilDitherMaskSeed.shader")]
        private Shader m_StencilDitherMaskSeedPS;

        /// <summary>
        /// Shader used to write stencils for the dither mask.
        /// </summary>
        public Shader stencilDitherMaskSeedPS
        {
            get => m_StencilDitherMaskSeedPS;
            set => this.SetValueAndNotify(ref m_StencilDitherMaskSeedPS, value, nameof(m_StencilDitherMaskSeedPS));
        }

        [Header("Decal Renderer Feature Specific")]
        [SerializeField]
        [ResourcePath("Runtime/Decal/DBuffer/DBufferClear.shader")]
        private Shader m_DBufferClear;

        /// <summary>
        /// Shader used to clear the Decal DBuffer.
        /// </summary>
        public Shader decalDBufferClear
        {
            get => m_DBufferClear;
            set => this.SetValueAndNotify(ref m_DBufferClear, value, nameof(m_DBufferClear));
        }
    }
}
