using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Class containing shader resources used in URP.
    /// </summary>
    [Serializable]
    [SupportedOnRenderPipeline(typeof(UniversalRenderPipelineAsset))]
    [Categorization.CategoryInfo(Name = "R: Universal Renderer Shaders", Order = 1000), HideInInspector]
    public class UniversalRendererResources : IRenderPipelineResources
    {
        [SerializeField][HideInInspector] private int m_Version = 0;

        /// <summary>Version of the resource. </summary>
        public int version => m_Version;
        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        [SerializeField]
        [ResourcePath("Shaders/Utils/CopyDepth.shader")]
        private Shader m_CopyDepthPS;

        /// <summary>
        /// Copy Depth shader.
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
        /// Camera Motion Vectors shader.
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
        /// Stencil Deferred shader.
        /// </summary>
        public Shader stencilDeferredPS
        {
            get => m_StencilDeferredPS;
            set => this.SetValueAndNotify(ref m_StencilDeferredPS, value, nameof(m_StencilDeferredPS));
        }

        [Header("Decal Renderer Feature Specific")]
        [SerializeField]
        [ResourcePath("Runtime/Decal/DBuffer/DBufferClear.shader")]
        private Shader m_DBufferClear;

        /// <summary>
        /// Decal DBuffer Shader
        /// </summary>
        public Shader decalDBufferClear
        {
            get => m_DBufferClear;
            set => this.SetValueAndNotify(ref m_DBufferClear, value, nameof(m_DBufferClear));
        }
    }
}
