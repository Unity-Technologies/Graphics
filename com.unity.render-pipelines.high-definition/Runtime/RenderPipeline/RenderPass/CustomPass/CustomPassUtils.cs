using System;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A set of custom pass utility function to help you build your effects
    /// </summary>
    public class CustomPassUtils
    {
        /// <summary>
        /// Fullscreen scale and bias values, it is the default for functions that have scale and bias overloads.
        /// </summary>
        /// <returns>x: scaleX, y: scaleY, z: biasX, w: biasY</returns>
        public static Vector4 fullScreenScaleBias = new Vector4(1, 1, 0, 0);

        static ShaderTagId[] litForwardTags = {
            HDShaderPassNames.s_ForwardOnlyName, HDShaderPassNames.s_ForwardName, HDShaderPassNames.s_SRPDefaultUnlitName
        };
        static ShaderTagId[] depthTags = { HDShaderPassNames.s_DepthForwardOnlyName, HDShaderPassNames.s_DepthOnlyName };

        static ProfilingSampler downSampleSampler = new ProfilingSampler("DownSample");
        static ProfilingSampler verticalBlurSampler = new ProfilingSampler("Vertical Blur");
        static ProfilingSampler horizontalBlurSampler = new ProfilingSampler("Horizontal Blur");
        static ProfilingSampler gaussianblurSampler = new ProfilingSampler("Gaussian Blur");

        static MaterialPropertyBlock    blurPropertyBlock = new MaterialPropertyBlock();
        static Material                 customPassUtilsMaterial = new Material(HDRenderPipeline.defaultAsset.renderPipelineResources.shaders.customPassUtils);

        static Dictionary<int, float[]> gaussianWeightsCache = new Dictionary<int, float[]>();

        static int downSamplePassIndex = customPassUtilsMaterial.FindPass("Downsample");

        /// <summary>
        /// Convert the source buffer to an half resolution buffer and output it to the destination buffer.
        /// </summary>
        /// <param name="ctx">Custom Pass Context</param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="sourceMip"></param>
        /// <param name="destMip"></param>
        public static void DownSample(in CustomPassContext ctx, RTHandle source, RTHandle destination, int sourceMip = 0, int destMip = 0)
            => DownSample(ctx, source, destination, fullScreenScaleBias, fullScreenScaleBias, sourceMip, destMip);

        public static void DownSample(in CustomPassContext ctx, RTHandle source, RTHandle destination, Vector4 sourceScaleBias, Vector4 destScaleBias, int sourceMip = 0, int destMip = 0)
        {
            // Check if the texture provided is at least half of the size of source.
            if (destination.rt.width < source.rt.width / 2 || destination.rt.height < source.rt.height)
                Debug.LogError("Destination for DownSample is too small, it needs to be at least half as big as source.");

            using (new ProfilingScope(ctx.cmd, downSampleSampler))
            {
                CoreUtils.SetRenderTarget(ctx.cmd, destination, ClearFlag.None, miplevel: destMip);

                Vector2Int scaledViewportSize = destination.GetScaledSize(destination.rtHandleProperties.currentViewportSize);
                ctx.cmd.SetViewport(new Rect(0.0f, 0.0f, scaledViewportSize.x, scaledViewportSize.y));

                blurPropertyBlock.SetTexture(HDShaderIDs._Source, source);
                blurPropertyBlock.SetVector(HDShaderIDs._SourceScaleBias, sourceScaleBias);
                ctx.cmd.DrawProcedural(Matrix4x4.identity, customPassUtilsMaterial, downSamplePassIndex, MeshTopology.Quads, 4, 1, blurPropertyBlock);
            }
        }

        // Do we provide an upsample function ?
        // public static void UpSample(CustomPassContext ctx, RTHandle source, RTHandle destination)
        // {
        //     Debug.Log("TODO");
        // }

        public static void Copy(in CustomPassContext ctx, RTHandle source, RTHandle destination, int sourceMip = 0, int destMip = 0, bool bilinear = false)
            => Copy(ctx, source, destination, fullScreenScaleBias, fullScreenScaleBias, sourceMip, destMip, bilinear);

        public static void Copy(in CustomPassContext ctx, RTHandle source, RTHandle destination, Vector4 sourceScaleBias, Vector4 destScaleBias, int sourceMip = 0, int destMip = 0, bool bilinear = false)
        {
            CoreUtils.SetRenderTarget(ctx.cmd, destination, ClearFlag.None, Color.black, destMip);
            HDUtils.BlitQuad(ctx.cmd, source, sourceScaleBias, fullScreenScaleBias, sourceMip, bilinear);
        }

        public static void GaussianBlurVertical(in CustomPassContext ctx, RTHandle source, RTHandle destination, int sampleCount = 8, int sourceMip = 0, int destMip = 0)
            => GaussianBlurVertical(ctx, source, destination, fullScreenScaleBias, fullScreenScaleBias, sampleCount, sourceMip, destMip);

        public static void GaussianBlurVertical(in CustomPassContext ctx, RTHandle source, RTHandle destination, Vector4 sourceScaleBias, Vector4 destScaleBias, int sampleCount = 8, int sourceMip = 0, int destMip = 0)
        {
            using (new ProfilingScope(ctx.cmd, verticalBlurSampler))
            {
                
            }
        }

        public static void GaussianBlurHorizontal(in CustomPassContext ctx, RTHandle source, RTHandle destination, int sampleCount = 8, int sourceMip = 0, int destMip = 0)
            => GaussianBlurHorizontal(ctx, source, destination, fullScreenScaleBias, fullScreenScaleBias, sampleCount, sourceMip, destMip);

        public static void GaussianBlurHorizontal(in CustomPassContext ctx, RTHandle source, RTHandle destination, Vector4 sourceScaleBias, Vector4 destScaleBias, int sampleCount = 8, int sourceMip = 0, int destMip = 0)
        {
            using (new ProfilingScope(ctx.cmd, horizontalBlurSampler))
            {
                
            }
        }

        public static void GaussianBlur(in CustomPassContext ctx, RTHandle source, RTHandle destination, RTHandle tempTarget, int sampleCount = 8, float radius = 1, int sourceMip = 0, int destMip = 0)
            => GaussianBlur(ctx, source, destination, tempTarget, fullScreenScaleBias, fullScreenScaleBias, sampleCount, radius, sourceMip, destMip);

        public static void GaussianBlur(in CustomPassContext ctx, RTHandle source, RTHandle destination, RTHandle tempTarget, Vector4 sourceScaleBias, Vector4 destScaleBias, int sampleCount = 8, float radius = 1, int sourceMip = 0, int destMip = 0, bool downSample = true)
        {
            using (new ProfilingScope(ctx.cmd, gaussianblurSampler))
            {
                if (downSample)
                {
                    // Downsample to half res
                    DownSample(ctx, source, tempTarget, sourceScaleBias, destScaleBias, sourceMip, destMip);
                    // Vertical blur
                    GaussianBlurVertical(ctx, tempTarget, destination);
                    // Instead of allocating a new buffer on the fly, we copy the data.
                    // We will be able to allocate it when rendergraph lands
                    Copy(ctx, destination, tempTarget, fullScreenScaleBias, destScaleBias);
                    // Horizontal blur and upsample
                    GaussianBlurHorizontal(ctx, tempTarget, destination);
                }
                else
                {
                    // Vertical blur
                    GaussianBlurVertical(ctx, source, tempTarget);
                    // Horizontal blur and upsample
                    GaussianBlurHorizontal(ctx, tempTarget, destination);
                }
            }
        }

        public static void DrawRenderers(in CustomPassContext ctx, LayerMask layerMask, CustomPass.RenderQueueType renderQueueFilter = CustomPass.RenderQueueType.All, Material overrideMaterial = null, int overideMaterialIndex = 0)
        {
            var result = new RendererListDesc(litForwardTags, ctx.cullingResults, ctx.hdCamera.camera)
            {
                rendererConfiguration = PerObjectData.None,
                renderQueueRange = GetRenderQueueRangeFromRenderQueueType(renderQueueFilter),
                sortingCriteria = SortingCriteria.BackToFront,
                excludeObjectMotionVectors = false,
                layerMask = layerMask,
                stateBlock = new RenderStateBlock(RenderStateMask.Depth){ depthState = new DepthState(true, CompareFunction.LessEqual)},
            };

            HDUtils.DrawRendererList(ctx.renderContext, ctx.cmd, RendererList.Create(result));
        }

        public static void DrawShadowMap(in CustomPassContext ctx, RTHandle destination, Camera view, LayerMask layerMask)
        {
            var result = new RendererListDesc(litForwardTags, ctx.cullingResults, ctx.hdCamera.camera)
            {
                rendererConfiguration = PerObjectData.None,
                renderQueueRange = RenderQueueRange.all,
                sortingCriteria = SortingCriteria.BackToFront,
                excludeObjectMotionVectors = false,
                layerMask = layerMask,
                // stateBlock = new RenderStateBlock(RenderStateMask.Depth){ depthState = new DepthState(true, CompareFunction.LessEqual)},
            };

            var hdrp = HDRenderPipeline.currentPipeline;
            // ctx.hdCamera.SetupGlobalParams(ctx.cmd, hdrp.m_)

            CoreUtils.SetRenderTarget(ctx.cmd, destination, ClearFlag.Depth);
            HDUtils.DrawRendererList(ctx.renderContext, ctx.cmd, RendererList.Create(result));
        }

        // Render objects from the view in parameter
        public static void RenderFrom(in CustomPassContext ctx, RTHandle destination, Camera view, LayerMask layerMask)
        {
            
        }

        /// <summary>
        /// Generate gaussian weights for a given number of samples
        /// </summary>
        /// <param name="weightCount"></param>
        /// <returns></returns>
        internal static float[] GetGaussianWeights(int weightCount)
		{
            float[] weights;

            if (gaussianWeightsCache.TryGetValue(weightCount, out weights))
                return weights;
            
            weights = new float[weightCount];
			float p = 0;
			float integrationBound = 3;
			for (int i = 0; i < weightCount; i++)
			{
				float w = (Gaussian(p) / (float)weightCount) * integrationBound;
				p += 1.0f / (float)weightCount * integrationBound;
                weights[i] = w;
			}
            gaussianWeightsCache[weightCount] = weights;

			// Gaussian function
			float Gaussian(float x, float sigma = 1)
			{
				float a = 1.0f / Mathf.Sqrt(2 * Mathf.PI * sigma * sigma);
				float b = Mathf.Exp(-(x * x) / (2 * sigma * sigma));
				return a * b;
			}

            return weights;
		}

        public static RenderQueueRange GetRenderQueueRangeFromRenderQueueType(CustomPass.RenderQueueType type)
        {
            switch (type)
            {
                case CustomPass.RenderQueueType.OpaqueNoAlphaTest: return HDRenderQueue.k_RenderQueue_OpaqueNoAlphaTest;
                case CustomPass.RenderQueueType.OpaqueAlphaTest: return HDRenderQueue.k_RenderQueue_OpaqueAlphaTest;
                case CustomPass.RenderQueueType.AllOpaque: return HDRenderQueue.k_RenderQueue_AllOpaque;
                case CustomPass.RenderQueueType.AfterPostProcessOpaque: return HDRenderQueue.k_RenderQueue_AfterPostProcessOpaque;
                case CustomPass.RenderQueueType.PreRefraction: return HDRenderQueue.k_RenderQueue_PreRefraction;
                case CustomPass.RenderQueueType.Transparent: return HDRenderQueue.k_RenderQueue_Transparent;
                case CustomPass.RenderQueueType.LowTransparent: return HDRenderQueue.k_RenderQueue_LowTransparent;
                case CustomPass.RenderQueueType.AllTransparent: return HDRenderQueue.k_RenderQueue_AllTransparent;
                case CustomPass.RenderQueueType.AllTransparentWithLowRes: return HDRenderQueue.k_RenderQueue_AllTransparentWithLowRes;
                case CustomPass.RenderQueueType.AfterPostProcessTransparent: return HDRenderQueue.k_RenderQueue_AfterPostProcessTransparent;
                case CustomPass.RenderQueueType.All:
                default:
                    return HDRenderQueue.k_RenderQueue_All;
            }
        }
    }
}