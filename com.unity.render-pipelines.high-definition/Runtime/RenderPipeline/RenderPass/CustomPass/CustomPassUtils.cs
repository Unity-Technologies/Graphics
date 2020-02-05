using System;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A set of custom pass utility function to help you build your effects
    /// </summary>
    public class CustomPassUtils
    {
        public static Vector4 fullScreenScaleBias = new Vector4(1, 1, 0, 0);

        static ShaderTagId[] litForwardTags = {
            HDShaderPassNames.s_ForwardOnlyName, HDShaderPassNames.s_ForwardName, HDShaderPassNames.s_SRPDefaultUnlitName
        };
        static ShaderTagId[] depthTags = { HDShaderPassNames.s_DepthForwardOnlyName, HDShaderPassNames.s_DepthOnlyName };
        
        static ProfilingSampler downSampleSampler = new ProfilingSampler("DownSample");
        static ProfilingSampler verticalBlurSampler = new ProfilingSampler("Vertical Blur");
        static ProfilingSampler horizontalBlurSampler = new ProfilingSampler("Horizontal Blur");
        static ProfilingSampler gaussianblurSampler = new ProfilingSampler("Gaussian Blur");

        /// <summary>
        /// Convert the source buffer to an half resolution buffer and output it to the destination buffer.
        /// </summary>
        /// <param name="ctx">Custom Pass Context</param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="sampleCount"></param>
        /// <param name="sourceMip"></param>
        /// <param name="destMip"></param>
        public static void DownSample(CustomPassContext ctx, RTHandle source, RTHandle destination, int sampleCount = 1, int sourceMip = 0, int destMip = 0)
        {
            Debug.Log("TODO");
        }

        // Do we provide an upsample function ?
        // public static void UpSample(CustomPassContext ctx, RTHandle source, RTHandle destination)
        // {
        //     Debug.Log("TODO");
        // }

        public static void Copy(CustomPassContext ctx, RTHandle source, RTHandle destination, int sourceMip = 0, int destMip = 0, bool bilinear = true)
            => Copy(ctx, source, destination, fullScreenScaleBias, fullScreenScaleBias, sourceMip, destMip, bilinear);

        public static void Copy(CustomPassContext ctx, RTHandle source, RTHandle destination, Vector4 sourceScaleBias, Vector4 destScaleBias, int sourceMip = 0, int destMip = 0, bool bilinear = true)
        {
            CoreUtils.SetRenderTarget(ctx.cmd, destination, ClearFlag.None, Color.black, destMip);
            HDUtils.BlitQuad(ctx.cmd, source, sourceScaleBias, fullScreenScaleBias, sourceMip, bilinear);
        }

        public static void GaussianBlurVertical(CustomPassContext ctx, RTHandle source, RTHandle destination, int sampleCount = 8, int sourceMip = 0, int destMip = 0)
            => GaussianBlurVertical(ctx, source, destination, fullScreenScaleBias, fullScreenScaleBias, sampleCount, sourceMip, destMip);

        public static void GaussianBlurVertical(CustomPassContext ctx, RTHandle source, RTHandle destination, Vector4 sourceScaleBias, Vector4 destScaleBias, int sampleCount = 8, int sourceMip = 0, int destMip = 0)
        {
            using (new ProfilingScope(ctx.cmd, verticalBlurSampler))
            {
                
            }
        }

        public static void GaussianBlurHorizontal(CustomPassContext ctx, RTHandle source, RTHandle destination, int sampleCount = 8, int sourceMip = 0, int destMip = 0)
            => GaussianBlurHorizontal(ctx, source, destination, fullScreenScaleBias, fullScreenScaleBias, sampleCount, sourceMip, destMip);

        public static void GaussianBlurHorizontal(CustomPassContext ctx, RTHandle source, RTHandle destination, Vector4 sourceScaleBias, Vector4 destScaleBias, int sampleCount = 8, int sourceMip = 0, int destMip = 0)
        {
            using (new ProfilingScope(ctx.cmd, horizontalBlurSampler))
            {
                
            }
        }

        public static void GaussianBlur(CustomPassContext ctx, RTHandle source, RTHandle destination, RTHandle tempTarget, int sampleCount = 8, float radius = 1, int sourceMip = 0, int destMip = 0)
            => GaussianBlur(ctx, source, destination, tempTarget, sampleCount);

        public static void GaussianBlur(CustomPassContext ctx, RTHandle source, RTHandle destination, RTHandle tempTarget, Vector4 sourceScaleBias, Vector4 destScaleBias, int sampleCount = 8, float radius = 1, int sourceMip = 0, int destMip = 0)
        {
            using (new ProfilingScope(ctx.cmd, gaussianblurSampler))
            {
                // Downsample to half res
                DownSample(ctx, source, destination, 1, sourceMip, destMip);
                // Vertical blur
                GaussianBlurVertical(ctx, destination, tempTarget);
                // Horizontal blur
                GaussianBlurHorizontal(ctx, tempTarget, destination);
            }
        }

        public static void DrawRenderers(CustomPassContext ctx, LayerMask layerMask, Material overrideMaterial = null, int overideMaterialIndex = 0)
        {
            var result = new RendererListDesc(litForwardTags, ctx.cullingResult, ctx.hdCamera.camera)
            {
                rendererConfiguration = PerObjectData.None,
                renderQueueRange = RenderQueueRange.all,
                sortingCriteria = SortingCriteria.BackToFront,
                excludeObjectMotionVectors = false,
                layerMask = layerMask,
                stateBlock = new RenderStateBlock(RenderStateMask.Depth){ depthState = new DepthState(true, CompareFunction.LessEqual)},
            };

            HDUtils.DrawRendererList(ctx.renderContext, ctx.cmd, RendererList.Create(result));
        }

        public static void DrawShadow(CustomPassContext ctx, RTHandle destination, Camera view, LayerMask layerMask)
        {
            var result = new RendererListDesc(litForwardTags, ctx.cullingResult, ctx.hdCamera.camera)
            {
                rendererConfiguration = PerObjectData.None,
                renderQueueRange = RenderQueueRange.all,
                sortingCriteria = SortingCriteria.BackToFront,
                excludeObjectMotionVectors = false,
                layerMask = layerMask,
                // stateBlock = new RenderStateBlock(RenderStateMask.Depth){ depthState = new DepthState(true, CompareFunction.LessEqual)},
            };

            var hdrp = HDRenderPipeline.currentPipeline;
            ctx.hdCamera.SetupGlobalParams(ctx.cmd, hdrp.m_)

            CoreUtils.SetRenderTarget(ctx.cmd, destination, ClearFlag.Depth);
            HDUtils.DrawRendererList(ctx.renderContext, ctx.cmd, RendererList.Create(result));
        }
    }
}