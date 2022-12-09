#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
using UnityEditor.Rendering.Universal;
#endif
using System;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Deprecated, kept for backward compatibility with existing ForwardRendererData asset files.
    /// Use UniversalRendererData instead.
    /// </summary>
    [System.Obsolete("ForwardRendererData has been deprecated (UnityUpgradable) -> UniversalRendererData", true)]
    [Serializable, ReloadGroup, ExcludeFromPreset]
    public class ForwardRendererData : ScriptableRendererData
    {
        private const string k_ErrorMessage = "ForwardRendererData has been deprecated. Use UniversalRendererData instead";

        /// <summary>
        /// Class containing shader resources used in URP.
        /// </summary>
        [Serializable, ReloadGroup]
        public sealed class ShaderResources
        {
            /// <summary>
            /// Blit shader.
            /// </summary>
            [Reload("Shaders/Utils/Blit.shader")]
            public Shader blitPS;

            /// <summary>
            /// Copy depth shader.
            /// </summary>
            [Reload("Shaders/Utils/CopyDepth.shader")]
            public Shader copyDepthPS;

            /// <summary>
            /// Screen space shadows shader.
            /// </summary>
            [Obsolete("Obsolete, this feature will be supported by new 'ScreenSpaceShadows' renderer feature")]
            public Shader screenSpaceShadowPS;

            /// <summary>
            /// Sampling shader.
            /// </summary>
            [Reload("Shaders/Utils/Sampling.shader")]
            public Shader samplingPS;

            /// <summary>
            /// Stencil deferred shader.
            /// </summary>
            [Reload("Shaders/Utils/StencilDeferred.shader")]
            public Shader stencilDeferredPS;

            /// <summary>
            /// Fallback error shader.
            /// </summary>
            [Reload("Shaders/Utils/FallbackError.shader")]
            public Shader fallbackErrorPS;

            /// <summary>
            /// Fallback loading shader.
            /// </summary>
            [Reload("Shaders/Utils/FallbackLoading.shader")]
            public Shader fallbackLoadingPS;

            /// <summary>
            /// Material error shader.
            /// </summary>
            [Obsolete("Use fallbackErrorPS instead")]
            [Reload("Shaders/Utils/MaterialError.shader")]
            public Shader materialErrorPS;

            // Core blitter shaders, adapted from HDRP
            // TODO: move to core and share with HDRP
            [Reload("Shaders/Utils/CoreBlit.shader"), SerializeField]
            internal Shader coreBlitPS;
            [Reload("Shaders/Utils/CoreBlitColorAndDepth.shader"), SerializeField]
            internal Shader coreBlitColorAndDepthPS;

            /// <summary>
            /// Camera motion vectors shader.
            /// </summary>
            [Reload("Shaders/CameraMotionVectors.shader")]
            public Shader cameraMotionVector;

            /// <summary>
            /// Object motion vectors shader.
            /// </summary>
            [Reload("Shaders/ObjectMotionVectors.shader")]
            public Shader objectMotionVector;
        }

        /// <summary>
        /// Shader resources used in URP.
        /// </summary>
        public ShaderResources shaders;

        /// <summary>
        /// Resources needed for post processing.
        /// </summary>
        public PostProcessData postProcessData;

#if ENABLE_VR && ENABLE_XR_MODULE
        /// <summary>
        /// Shader resources needed in URP for XR.
        /// </summary>
        [Reload("Runtime/Data/XRSystemData.asset")]
        public XRSystemData xrSystemData;
#endif

        [SerializeField] LayerMask m_OpaqueLayerMask;
        [SerializeField] LayerMask m_TransparentLayerMask;
        [SerializeField] StencilStateData m_DefaultStencilState; // This default state is compatible with deferred renderer.
        [SerializeField] bool m_ShadowTransparentReceive;
        [SerializeField] RenderingMode m_RenderingMode;
        [SerializeField] DepthPrimingMode m_DepthPrimingMode; // Default disabled because there are some outstanding issues with Text Mesh rendering.
        [SerializeField] bool m_AccurateGbufferNormals;
        [SerializeField] bool m_ClusteredRendering;
        [SerializeField] TileSize m_TileSize;

        /// <inheritdoc/>
        protected override ScriptableRenderer Create()
        {
            Debug.LogWarning($"Forward Renderer Data has been deprecated, {name} will be upgraded to a {nameof(UniversalRendererData)}.");
            return null;
        }

        /// <summary>
        /// Use this to configure how to filter opaque objects.
        /// </summary>
        public LayerMask opaqueLayerMask
        {
            get { throw new NotSupportedException(k_ErrorMessage); }
            set { throw new NotSupportedException(k_ErrorMessage); }
        }

        /// <summary>
        /// Use this to configure how to filter transparent objects.
        /// </summary>
        public LayerMask transparentLayerMask
        {
            get { throw new NotSupportedException(k_ErrorMessage); }
            set { throw new NotSupportedException(k_ErrorMessage); }
        }

        /// <summary>
        /// The default stencil state settings.
        /// </summary>
        public StencilStateData defaultStencilState
        {
            get { throw new NotSupportedException(k_ErrorMessage); }
            set { throw new NotSupportedException(k_ErrorMessage); }
        }

        /// <summary>
        /// True if transparent objects receive shadows.
        /// </summary>
        public bool shadowTransparentReceive
        {
            get { throw new NotSupportedException(k_ErrorMessage); }
            set { throw new NotSupportedException(k_ErrorMessage); }
        }

        /// <summary>
        /// Rendering mode.
        /// </summary>
        public RenderingMode renderingMode
        {
            get { throw new NotSupportedException(k_ErrorMessage); }
            set { throw new NotSupportedException(k_ErrorMessage); }
        }

        /// <summary>
        /// Use Octahedron normal vector encoding for gbuffer normals.
        /// The overhead is negligible from desktop GPUs, while it should be avoided for mobile GPUs.
        /// </summary>
        public bool accurateGbufferNormals
        {
            get { throw new NotSupportedException(k_ErrorMessage); }
            set { throw new NotSupportedException(k_ErrorMessage); }
        }
    }
}
