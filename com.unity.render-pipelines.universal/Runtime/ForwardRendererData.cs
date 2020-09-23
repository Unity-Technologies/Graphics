#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.ProjectWindowCallback;
#endif
using System;
using UnityEngine.Scripting.APIUpdating;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Deprecated, kept for backward compatibility with existing ForwardRendererData asset files.
    /// Use StandardRendererData instead.
    /// </summary>
    [System.Obsolete("ForwardRendererData has been deprecated. Use StandardRendererData instead (UnityUpgradable) -> StandardRendererData", true)]
    [Serializable, ReloadGroup, ExcludeFromPreset]
    [MovedFrom("UnityEngine.Rendering.LWRP")]
    public class ForwardRendererData : ScriptableRendererData
    {
        public sealed class ShaderResources
        {
            public Shader blitPS;

            public Shader copyDepthPS;

            public Shader screenSpaceShadowPS;

            public Shader samplingPS;

            public Shader tileDepthInfoPS;

            public Shader tileDeferredPS;

            public Shader stencilDeferredPS;

            public Shader fallbackErrorPS;
        }

        public PostProcessData postProcessData = null;

#if ENABLE_VR && ENABLE_XR_MODULE
        public XRSystemData xrSystemData = null;
#endif

        public ShaderResources shaders = null;

        protected override ScriptableRenderer Create()
        {
            throw new NotSupportedException("ForwardRendererData has been deprecated. Use StandardRendererData instead");
        }

        public LayerMask opaqueLayerMask
        {
        	get { throw new NotSupportedException("ForwardRendererData has been deprecated. Use StandardRendererData instead"); }
        	set { throw new NotSupportedException("ForwardRendererData has been deprecated. Use StandardRendererData instead"); }
        }

        public LayerMask transparentLayerMask
        {
        	get { throw new NotSupportedException("ForwardRendererData has been deprecated. Use StandardRendererData instead"); }
        	set { throw new NotSupportedException("ForwardRendererData has been deprecated. Use StandardRendererData instead"); }
        }

        public StencilStateData defaultStencilState
        {
        	get { throw new NotSupportedException("ForwardRendererData has been deprecated. Use StandardRendererData instead"); }
        	set { throw new NotSupportedException("ForwardRendererData has been deprecated. Use StandardRendererData instead"); }
        }

        public bool shadowTransparentReceive
        {
        	get { throw new NotSupportedException("ForwardRendererData has been deprecated. Use StandardRendererData instead"); }
        	set { throw new NotSupportedException("ForwardRendererData has been deprecated. Use StandardRendererData instead"); }
        }

        public RenderingMode renderingMode
        {
        	get { throw new NotSupportedException("ForwardRendererData has been deprecated. Use StandardRendererData instead"); }
        	set { throw new NotSupportedException("ForwardRendererData has been deprecated. Use StandardRendererData instead"); }
        }

		public bool accurateGbufferNormals
		{
        	get { throw new NotSupportedException("ForwardRendererData has been deprecated. Use StandardRendererData instead"); }
        	set { throw new NotSupportedException("ForwardRendererData has been deprecated. Use StandardRendererData instead"); }
		}
    }
}
