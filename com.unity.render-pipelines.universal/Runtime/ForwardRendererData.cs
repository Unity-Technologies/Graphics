#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
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
        }

        public ShaderResources shaders = null;

        public PostProcessData postProcessData = null;

#if ENABLE_VR && ENABLE_XR_MODULE
        [Reload("Runtime/Data/XRSystemData.asset")]
        public XRSystemData xrSystemData = null;
#endif

        protected override ScriptableRenderer Create()
        {
            throw new NotSupportedException(k_ErrorMessage);
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
