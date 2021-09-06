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

        [Serializable, ReloadGroup]
        public sealed class ShaderResources
        {
            [Reload("Shaders/Utils/Blit.shader")]
            public Shader blitPS;

            [Reload("Shaders/Utils/CopyDepth.shader")]
            public Shader copyDepthPS;

            [Obsolete("Obsolete, this feature will be supported by new 'ScreenSpaceShadows' renderer feature")]
            public Shader screenSpaceShadowPS;

            [Reload("Shaders/Utils/Sampling.shader")]
            public Shader samplingPS;

            [Reload("Shaders/Utils/StencilDeferred.shader")]
            public Shader stencilDeferredPS;

            [Reload("Shaders/Utils/FallbackError.shader")]
            public Shader fallbackErrorPS;

            [Reload("Shaders/Utils/MaterialError.shader")]
            public Shader materialErrorPS;

            // Core blitter shaders, adapted from HDRP
            // TODO: move to core and share with HDRP
            [Reload("Shaders/Utils/CoreBlit.shader"), SerializeField]
            internal Shader coreBlitPS;
            [Reload("Shaders/Utils/CoreBlitColorAndDepth.shader"), SerializeField]
            internal Shader coreBlitColorAndDepthPS;


            [Reload("Shaders/CameraMotionVectors.shader")]
            public Shader cameraMotionVector;

            [Reload("Shaders/ObjectMotionVectors.shader")]
            public Shader objectMotionVector;
        }

        public ShaderResources shaders;

        public PostProcessData postProcessData;

#if ENABLE_VR && ENABLE_XR_MODULE
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

        protected override ScriptableRenderer Create()
        {
            Debug.LogWarning($"Forward Renderer Data has been deprecated, {name} will be upgraded to a {nameof(UniversalRendererData)}.");
            return null;
        }

        public LayerMask opaqueLayerMask
        {
            get { throw new NotSupportedException(k_ErrorMessage); }
            set { throw new NotSupportedException(k_ErrorMessage); }
        }

        public LayerMask transparentLayerMask
        {
            get { throw new NotSupportedException(k_ErrorMessage); }
            set { throw new NotSupportedException(k_ErrorMessage); }
        }

        public StencilStateData defaultStencilState
        {
            get { throw new NotSupportedException(k_ErrorMessage); }
            set { throw new NotSupportedException(k_ErrorMessage); }
        }

        public bool shadowTransparentReceive
        {
            get { throw new NotSupportedException(k_ErrorMessage); }
            set { throw new NotSupportedException(k_ErrorMessage); }
        }

        public RenderingMode renderingMode
        {
            get { throw new NotSupportedException(k_ErrorMessage); }
            set { throw new NotSupportedException(k_ErrorMessage); }
        }

        public bool accurateGbufferNormals
        {
            get { throw new NotSupportedException(k_ErrorMessage); }
            set { throw new NotSupportedException(k_ErrorMessage); }
        }
    }
}
