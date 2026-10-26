using System;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering
{
    /// <summary>Resource class that handles the serialization of upscaler reactive mask shaders.</summary>
    [Serializable]
    [SupportedOnRenderPipeline]
    [Categorization.CategoryInfo(Name = "R: Upscaler Reactive Mask", Order = 1000), HideInInspector]
    public sealed class UpscalerReactiveMaskResources : IRenderPipelineResources
    {
        [SerializeField, HideInInspector]
        int m_Version = 0;

        /// <summary>The version number of the resources container.</summary>
        public int version { get => m_Version; }

        bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

        [SerializeField, ResourcePath("Runtime/Upscaling/UpscalerReactiveMask.shader")]
        Shader m_reactiveMaskPS;

        /// <summary>The reactive mask shader.</summary>
        public Shader reactiveMaskPS
        {
            get => m_reactiveMaskPS;
            set => this.SetValueAndNotify(ref m_reactiveMaskPS, value, nameof(m_reactiveMaskPS));
        }
    }

    /// <summary>
    /// Common definitions for reactive mask pass.
    /// </summary>
    public static class UpscalerReactiveMask
    {
        /// <summary>
        /// Render graph pass data for reactive mask pass.
        /// </summary>
        public class PassData
        {
            /// <summary>Material that represents the reactive mask shader.</summary>
            public Material reactiveMaskMaterial;
            /// <summary>Pass ID in the reactive mask shader.</summary>
            public int reactiveMaskPassId;
            /// <summary>Render target width.</summary>
            public int destWidth;
            /// <summary>Render target height.</summary>
            public int destHeight;
            /// <summary>Reactivity scale parameter. Applied after thresholding if active.</summary>
            public float reactiveScale;
            /// <summary>Threshold value applied if thresholding flag is set.</summary>
            public float reactiveThreshold;
            /// <summary>Reactivity value if thresholding is active and passed.</summary>
            public float reactiveBinaryValue;
            /// <summary>Bitflags of <see cref="UpscalerReactiveMask.ReactiveFlags"/>.</summary>
            public uint reactiveFlags;
            /// <summary>Camera color texture before transparency rendering.</summary>
            public TextureHandle cameraColorPreAlpha;
            /// <summary>Camera color texture after transparency rendering.</summary>
            public TextureHandle cameraColorPostAlpha;
        }

        /// <summary>
        /// Shader keyword constants for reactive mask shader.
        /// </summary>
        public static class ShaderKeywords
        {
            /// <summary>Keyword string for texture array input.</summary>
            public static readonly string k_InputTextureArrayKeyword = "_INPUT_TEXTURE_ARRAY";
        }

        /// <summary>Shader constant ids used to set parameters of reactive mask shader.</summary>
        public static class ShaderConstants
        {
            /// <summary>Stencil reference value used when generating stencil reactive mask.</summary>
            public static readonly int _StencilRef           = Shader.PropertyToID("_StencilRef");
            /// <summary>Stencil mask to set when generating stencil reactive mask.</summary>
            public static readonly int _StencilMask          = Shader.PropertyToID("_StencilMask");
            /// <summary>Scale factor for reactive mask values which is applied before thresholding.</summary>
            public static readonly int _ReactiveScale        = Shader.PropertyToID("_ReactiveScale");
            /// <summary>Threshold value when thresholding is enabled via reactive flags.</summary>
            public static readonly int _ReactiveThreshold    = Shader.PropertyToID("_ReactiveThreshold");
            /// <summary>The value that reactive mask will have if it passes the threshold.</summary>
            public static readonly int _ReactiveBinaryValue  = Shader.PropertyToID("_ReactiveBinaryValue");
            /// <summary>Bitflags for reactive mask generation. See <see cref="ReactiveFlags"/>.</summary>
            public static readonly int _ReactiveFlags        = Shader.PropertyToID("_ReactiveFlags");
            /// <summary>Camera color texture before rendering transparency.</summary>
            public static readonly int _ColorPreAlpha        = Shader.PropertyToID("_ColorPreAlpha");
            /// <summary>Camera color texture after rendering transparency.</summary>
            public static readonly int _ColorPostAlpha       = Shader.PropertyToID("_ColorPostAlpha");
        }

        /// <summary>Bitflags for the _ReactiveFlags parameter of reactive mask shader.</summary>
        public static class ReactiveFlags
        {
            /// <summary>Transform HDR color values to LDR values before computing reactivity.</summary>
            public static readonly uint _ApplyTonemap        = 1;
            /// <summary>Transform LDR color values to HDR values before computing reactivity.</summary>
            public static readonly uint _ApplyInverseTonemap = 2;
            /// <summary>Enable thresholding for reactivity values.</summary>
            public static readonly uint _ApplyThreshold      = 4;
            /// <summary>Use max component value instead of length of the delta between color inputs when computing reactivity.</summary>
            public static readonly uint _UseComponentMax     = 8;
        }
    }
}
