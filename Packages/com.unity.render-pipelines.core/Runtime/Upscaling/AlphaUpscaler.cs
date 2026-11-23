using System;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering
{
    /// <summary>
    /// Utility pass that rebuilds the alpha channel of an image produced by a color-only upscaler.
    /// </summary>
    public static class AlphaUpscaler
    {
        /// <summary>
        /// Input textures and parameters for the alpha upscale pass.
        /// </summary>
        public struct Config
        {
            /// <summary>Color at rendering resolution, before upscaling. Only the alpha channel is read.</summary>
            public TextureHandle preUpscaleAlpha;

            /// <summary>Upscaled color whose alpha channel is undefined. Only the RGB channels are read.</summary>
            public TextureHandle upscaledColor;

            /// <summary>Size of the rendered viewport of preUpscaleAlpha, in pixels.</summary>
            public Vector2Int preUpscaleSize;

            /// <summary>
            /// Physical (allocation) size of preUpscaleAlpha when it is larger than preUpscaleSize,
            /// e.g. with viewport-based dynamic resolution inside a larger texture. Leave zero when
            /// the texture is allocated at exactly preUpscaleSize.
            /// </summary>
            public Vector2Int preUpscalePhysicalSize;

            /// <summary>Size of the upscaled output, in pixels.</summary>
            public Vector2Int postUpscaleSize;

            /// <summary>Sub-pixel camera jitter applied to the pre-upscale frame, in pixels at the rendering resolution.</summary>
            public Vector2 jitter;

            /// <summary>Indicates whether the textures are 2D texture arrays, as used by XR single-pass rendering.</summary>
            public bool enableTexArray;

            /// <summary>Number of views to process.</summary>
            public int viewCount;

            /// <summary>
            /// Motion vectors at rendering resolution, in viewport UV space. Along with the depth and
            /// the two history textures, this enables the temporal accumulation; leave invalid for a
            /// spatial-only upsample.
            /// </summary>
            public TextureHandle motionVectors;

            /// <summary>Depth at rendering resolution, for the motion vector dilation at silhouettes.</summary>
            public TextureHandle cameraDepth;

            /// <summary>
            /// Reactive mask at rendering resolution, or leave invalid to disable.
            /// </summary>
            public TextureHandle reactiveMask;

            /// <summary>Stencil value that marks the excluded pixels when reactiveMask is a stencil buffer.</summary>
            public int stencilExcludeBit;

            /// <summary>Single-channel alpha history accumulated over the previous frames.</summary>
            public TextureHandle prevAlphaHistory;

            /// <summary>Single-channel alpha history the pass writes this frame.</summary>
            public TextureHandle nextAlphaHistory;

            /// <summary>
            /// Blend weight of the reprojected history. Use 0 when the history content is invalid (e.g. on a
            /// history reset): the frame falls back to the spatial upsample but still initializes the history.
            /// </summary>
            public float historyBlendFactor;
        }

        /// <summary>Default history blend weight of the temporal alpha accumulation.</summary>
        public const float defaultHistoryBlendFactor = 0.85f;

        /// <summary>
        /// Contains the compute shader used by the alpha upscale pass
        /// </summary>
        [Serializable]
        [SupportedOnRenderPipeline]
        [Categorization.CategoryInfo(Name = "R: Alpha Upscaler", Order = 1000)]
        [Categorization.ElementInfo(Order = 0), HideInInspector]
        internal class RuntimeResources : IRenderPipelineResources
        {
            public int version => 0;

            bool IRenderPipelineGraphicsSettings.isAvailableInPlayerBuild => true;

            [SerializeField, ResourcePath("Runtime/Upscaling/AlphaUpscale.compute")]
            private ComputeShader m_AlphaUpscaleCS;

            public ComputeShader alphaUpscaleCS
            {
                get => m_AlphaUpscaleCS;
                set => this.SetValueAndNotify(ref m_AlphaUpscaleCS, value);
            }
        }

        static class ShaderResources
        {
            public static readonly int _UpscaledColor = Shader.PropertyToID("_UpscaledColor");
            public static readonly int _PreUpscaleAlpha = Shader.PropertyToID("_PreUpscaleAlpha");
            public static readonly int _MotionVectorTexture = Shader.PropertyToID("_MotionVectorTexture");
            public static readonly int _DepthTexture = Shader.PropertyToID("_DepthTexture");
            public static readonly int _StencilTexture = Shader.PropertyToID("_StencilTexture");
            public static readonly int _ExcludeTAABit = Shader.PropertyToID("_ExcludeTAABit");
            public static readonly int _ReactiveMaskTexture = Shader.PropertyToID("_ReactiveMaskTexture");
            public static readonly int _AlphaHistory = Shader.PropertyToID("_AlphaHistory");
            public static readonly int _AlphaHistoryOutput = Shader.PropertyToID("_AlphaHistoryOutput");
            public static readonly int _OutputTexture = Shader.PropertyToID("_OutputTexture");
            public static readonly int _AlphaUpscaleParams0 = Shader.PropertyToID("_AlphaUpscaleParams0");
            public static readonly int _AlphaUpscaleParams1 = Shader.PropertyToID("_AlphaUpscaleParams1");
            public static readonly int _AlphaUpscaleParams2 = Shader.PropertyToID("_AlphaUpscaleParams2");
        }

        static class ShaderKeywords
        {
            public static readonly string DisableTexture2DXArray = "DISABLE_TEXTURE2D_X_ARRAY";
            public static readonly string DirectStencilSample = "DIRECT_STENCIL_SAMPLE";
            public static readonly string ReactiveMask = "REACTIVE_MASK";
        }

        enum ReactiveMaskMode
        {
            None,
            Stencil,
            Color
        }

        class PassData
        {
            public ComputeShader cs;
            public int kernelIndex;
            public Vector2Int dispatchSize;
            public int viewCount;
            public Vector2Int preUpscaleSize;
            public Vector2Int preUpscalePhysicalSize;
            public Vector4 params1;
            public float historyBlend;
            public bool useHistory;
            public ReactiveMaskMode reactiveMaskMode;
            public int stencilExcludeBit;

            public TextureHandle preUpscaleAlpha;
            public TextureHandle upscaledColor;
            public TextureHandle motionVectors;
            public TextureHandle cameraDepth;
            public TextureHandle reactiveMask;
            public TextureHandle prevAlphaHistory;
            public TextureHandle nextAlphaHistory;
            public TextureHandle destination;
        }

        static readonly ProfilingSampler k_AlphaUpscale = new ProfilingSampler("AlphaUpscale");

        /// <summary>
        /// Executes the alpha upscale pass using the provided configuration in the target render graph
        /// </summary>
        /// <param name="renderGraph">render graph to execute the pass within</param>
        /// <param name="config">configuration parameters for the pass</param>
        /// <returns>Texture handle that contains the upscaled color with a valid alpha channel</returns>
        public static TextureHandle Execute(RenderGraph renderGraph, ref Config config)
        {
            var runtimeResources = GraphicsSettings.GetRenderPipelineSettings<RuntimeResources>();

            using (var builder = renderGraph.AddComputePass<PassData>("Upscale Alpha And Merge Color", out var passData, k_AlphaUpscale))
            {
                passData.cs = runtimeResources.alphaUpscaleCS;
                passData.cs.shaderKeywords = null;

                if (!config.enableTexArray)
                    passData.cs.EnableKeyword(ShaderKeywords.DisableTexture2DXArray);

                var maskMode = ReactiveMaskMode.None;
                if (config.reactiveMask.IsValid())
                    maskMode = config.stencilExcludeBit != 0 ? ReactiveMaskMode.Stencil : ReactiveMaskMode.Color;
                passData.reactiveMaskMode = maskMode;

                if (maskMode != ReactiveMaskMode.None)
                {
                    passData.cs.EnableKeyword(maskMode == ReactiveMaskMode.Stencil
                        ? ShaderKeywords.DirectStencilSample : ShaderKeywords.ReactiveMask);
                    passData.stencilExcludeBit = config.stencilExcludeBit;
                    passData.reactiveMask = config.reactiveMask;
                    builder.UseTexture(passData.reactiveMask, AccessFlags.Read);
                }

                passData.kernelIndex = passData.cs.FindKernel("UpscaleAlphaAndMergeColor");
                passData.viewCount = config.viewCount;

                // Must match GROUP_SIZE in AlphaUpscale.compute
                const int groupSize = 16;
                passData.dispatchSize = new Vector2Int(
                    CoreUtils.DivRoundUp(config.postUpscaleSize.x, groupSize),
                    CoreUtils.DivRoundUp(config.postUpscaleSize.y, groupSize)
                );

                passData.preUpscaleSize = config.preUpscaleSize;
                passData.preUpscalePhysicalSize = (config.preUpscalePhysicalSize.x > 0 && config.preUpscalePhysicalSize.y > 0)
                    ? config.preUpscalePhysicalSize : config.preUpscaleSize;
                passData.params1 = new Vector4(config.postUpscaleSize.x, config.postUpscaleSize.y, config.jitter.x, config.jitter.y);

                passData.preUpscaleAlpha = config.preUpscaleAlpha;
                builder.UseTexture(passData.preUpscaleAlpha, AccessFlags.Read);
                passData.upscaledColor = config.upscaledColor;
                builder.UseTexture(passData.upscaledColor, AccessFlags.Read);

                var destinationDesc = config.upscaledColor.GetDescriptor(renderGraph);
                destinationDesc.name = "UpscaledColorWithAlpha";
                destinationDesc.enableRandomWrite = true;
                destinationDesc.clearBuffer = false;
                passData.destination = renderGraph.CreateTexture(destinationDesc);
                builder.UseTexture(passData.destination, AccessFlags.WriteAll);

                bool useHistory = config.motionVectors.IsValid() && config.cameraDepth.IsValid() && config.prevAlphaHistory.IsValid() && config.nextAlphaHistory.IsValid();
                passData.useHistory = useHistory;
                passData.historyBlend = config.historyBlendFactor;

                if (useHistory)
                {
                    passData.motionVectors = config.motionVectors;
                    passData.cameraDepth = config.cameraDepth;
                    passData.prevAlphaHistory = config.prevAlphaHistory;
                    passData.nextAlphaHistory = config.nextAlphaHistory;
                }
                else
                {
                    // The kernel never touches the history in this mode, but the bindings must still be valid.
                    passData.motionVectors = renderGraph.defaultResources.blackTextureXR;
                    passData.cameraDepth = renderGraph.defaultResources.blackTextureXR;
                    passData.prevAlphaHistory = renderGraph.defaultResources.blackTextureXR;
                    passData.nextAlphaHistory = renderGraph.CreateTexture(new TextureDesc(1, 1, false, config.enableTexArray)
                    {
                        name = "AlphaHistoryUnused",
                        format = GraphicsFormat.R16_SFloat,
                        enableRandomWrite = true
                    });
                }

                builder.UseTexture(passData.motionVectors, AccessFlags.Read);
                builder.UseTexture(passData.cameraDepth, AccessFlags.Read);
                builder.UseTexture(passData.prevAlphaHistory, AccessFlags.Read);
                builder.UseTexture(passData.nextAlphaHistory, AccessFlags.Write);

                builder.SetRenderFunc(
                    static (PassData data, ComputeGraphContext ctx) =>
                    {
                        var params0 = new Vector4(data.preUpscaleSize.x, data.preUpscaleSize.y, 1.0f / data.preUpscalePhysicalSize.x, 1.0f / data.preUpscalePhysicalSize.y);

                        // zw: the history RTHandle can be larger than the post-upscale viewport,
                        // so the kernel samples it with the physical texture size.
                        var params2 = Vector4.zero;
                        if (data.useHistory)
                        {
                            var historyTexture = (RenderTexture)data.nextAlphaHistory;
                            params2 = new Vector4(data.historyBlend, 1.0f, 1.0f / historyTexture.width, 1.0f / historyTexture.height);
                        }

                        ctx.cmd.SetComputeVectorParam(data.cs, ShaderResources._AlphaUpscaleParams0, params0);
                        ctx.cmd.SetComputeVectorParam(data.cs, ShaderResources._AlphaUpscaleParams1, data.params1);
                        ctx.cmd.SetComputeVectorParam(data.cs, ShaderResources._AlphaUpscaleParams2, params2);

                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._PreUpscaleAlpha, data.preUpscaleAlpha);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._UpscaledColor, data.upscaledColor);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._MotionVectorTexture, data.motionVectors);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._DepthTexture, data.cameraDepth);

                        if (data.reactiveMaskMode == ReactiveMaskMode.Stencil)
                        {
                            ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._StencilTexture, data.reactiveMask, 0, RenderTextureSubElement.Stencil);
                            ctx.cmd.SetComputeIntParam(data.cs, ShaderResources._ExcludeTAABit, data.stencilExcludeBit);
                        }
                        else if (data.reactiveMaskMode == ReactiveMaskMode.Color)
                        {
                            ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._ReactiveMaskTexture, data.reactiveMask);
                        }
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._AlphaHistory, data.prevAlphaHistory);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._AlphaHistoryOutput, data.nextAlphaHistory);
                        ctx.cmd.SetComputeTextureParam(data.cs, data.kernelIndex, ShaderResources._OutputTexture, data.destination);

                        ctx.cmd.DispatchCompute(data.cs, data.kernelIndex, data.dispatchSize.x, data.dispatchSize.y, data.viewCount);
                    });

                return passData.destination;
            }
        }
    }
}
