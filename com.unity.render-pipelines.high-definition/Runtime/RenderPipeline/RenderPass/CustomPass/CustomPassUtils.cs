using System;
using UnityEngine.Rendering;
using UnityEngine.Experimental.Rendering;
using System.Collections.Generic;

namespace UnityEngine.Rendering.HighDefinition
{
    /// <summary>
    /// A set of custom pass utility function to help you build your effects
    /// </summary>
    public static class CustomPassUtils
    {
        /// <summary>
        /// Fullscreen scale and bias values, it is the default for functions that have scale and bias overloads.
        /// </summary>
        /// <returns>x: scaleX, y: scaleY, z: biasX, w: biasY</returns>
        public static Vector4 fullScreenScaleBias = new Vector4(1, 1, 0, 0);

        static ShaderTagId[] litForwardTags = { HDShaderPassNames.s_ForwardOnlyName, HDShaderPassNames.s_ForwardName, HDShaderPassNames.s_SRPDefaultUnlitName };
        static ShaderTagId[] depthTags = { HDShaderPassNames.s_DepthForwardOnlyName, HDShaderPassNames.s_DepthOnlyName };

        static ProfilingSampler downSampleSampler = new ProfilingSampler("DownSample");
        static ProfilingSampler verticalBlurSampler = new ProfilingSampler("Vertical Blur");
        static ProfilingSampler horizontalBlurSampler = new ProfilingSampler("Horizontal Blur");
        static ProfilingSampler gaussianblurSampler = new ProfilingSampler("Gaussian Blur");
        static ProfilingSampler copySampler = new ProfilingSampler("Copy");

        static MaterialPropertyBlock    propertyBlock = new MaterialPropertyBlock();
        static Material                 customPassUtilsMaterial;

        static Dictionary<int, ComputeBuffer> gaussianWeightsCache = new Dictionary<int, ComputeBuffer>();

        static int downSamplePassIndex;
        static int verticalBlurPassIndex;
        static int horizontalBlurPassIndex;
        static int copyPassIndex;

        internal static void Initialize()
        {
            customPassUtilsMaterial = CoreUtils.CreateEngineMaterial(HDRenderPipeline.defaultAsset.renderPipelineResources.shaders.customPassUtils);
            downSamplePassIndex = customPassUtilsMaterial.FindPass("Downsample");
            verticalBlurPassIndex = customPassUtilsMaterial.FindPass("VerticalBlur");
            horizontalBlurPassIndex = customPassUtilsMaterial.FindPass("HorizontalBlur");
            copyPassIndex = customPassUtilsMaterial.FindPass("Copy");
        }

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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="sourceScaleBias">Scale and bias to apply when sampling the source buffer</param>
        /// <param name="destScaleBias">Scale and bias to apply when writing into the destination buffer. It's scale is relative to the destination buffer, so if you want an half res downsampling into a fullres buffer you need to specify a scale of 0.5;0,5. If your buffer is already half res Then 1;1 scale works.</param>
        /// <param name="sourceMip"></param>
        /// <param name="destMip"></param>
        public static void DownSample(in CustomPassContext ctx, RTHandle source, RTHandle destination, Vector4 sourceScaleBias, Vector4 destScaleBias, int sourceMip = 0, int destMip = 0)
        {
            // Check if the texture provided is at least half of the size of source.
            if (destination.rt.width < source.rt.width / 2 || destination.rt.height < source.rt.height / 2)
                Debug.LogError("Destination for DownSample is too small, it needs to be at least half as big as source.");

            using (new ProfilingScope(ctx.cmd, downSampleSampler))
            {
                SetRenderTargetWithScaleBias(ctx, propertyBlock, destination, destScaleBias, ClearFlag.None, destMip);

                propertyBlock.SetTexture(HDShaderIDs._Source, source);
                propertyBlock.SetVector(HDShaderIDs._SourceScaleBias, sourceScaleBias);
                SetSourceSize(propertyBlock, source);
                ctx.cmd.DrawProcedural(Matrix4x4.identity, customPassUtilsMaterial, downSamplePassIndex, MeshTopology.Quads, 4, 1, propertyBlock);
            }
        }

        // Do we provide an upsample function ?

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="sourceMip"></param>
        /// <param name="destMip"></param>
        public static void Copy(in CustomPassContext ctx, RTHandle source, RTHandle destination, int sourceMip = 0, int destMip = 0)
            => Copy(ctx, source, destination, fullScreenScaleBias, fullScreenScaleBias, sourceMip, destMip);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="sourceScaleBias"></param>
        /// <param name="destScaleBias"></param>
        /// <param name="sourceMip"></param>
        /// <param name="destMip"></param>
        public static void Copy(in CustomPassContext ctx, RTHandle source, RTHandle destination, Vector4 sourceScaleBias, Vector4 destScaleBias, int sourceMip = 0, int destMip = 0)
        {
            if (source == destination)
                Debug.LogError("Can't copy the buffer. Source has to be different from the destination.");

            using (new ProfilingScope(ctx.cmd, copySampler))
            {
                SetRenderTargetWithScaleBias(ctx, propertyBlock, destination, destScaleBias, ClearFlag.None, destMip);

                propertyBlock.SetTexture(HDShaderIDs._Source, source);
                propertyBlock.SetVector(HDShaderIDs._SourceScaleBias, sourceScaleBias);
                SetSourceSize(propertyBlock, source);
                ctx.cmd.DrawProcedural(Matrix4x4.identity, customPassUtilsMaterial, copyPassIndex, MeshTopology.Quads, 4, 1, propertyBlock);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="sampleCount"></param>
        /// <param name="radius"></param>
        /// <param name="sourceMip"></param>
        /// <param name="destMip"></param>
        public static void VerticalGaussianBlur(in CustomPassContext ctx, RTHandle source, RTHandle destination, int sampleCount = 8, float radius = 5, int sourceMip = 0, int destMip = 0)
            => VerticalGaussianBlur(ctx, source, destination, fullScreenScaleBias, fullScreenScaleBias, sampleCount, radius, sourceMip, destMip);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="sourceScaleBias"></param>
        /// <param name="destScaleBias"></param>
        /// <param name="sampleCount"></param>
        /// <param name="radius"></param>
        /// <param name="sourceMip"></param>
        /// <param name="destMip"></param>
        public static void VerticalGaussianBlur(in CustomPassContext ctx, RTHandle source, RTHandle destination, Vector4 sourceScaleBias, Vector4 destScaleBias, int sampleCount = 8, float radius = 5, int sourceMip = 0, int destMip = 0)
        {
            if (source == destination)
                Debug.LogError("Can't blur the buffer. Source has to be different from the destination.");

            using (new ProfilingScope(ctx.cmd, verticalBlurSampler))
            {
                SetRenderTargetWithScaleBias(ctx, propertyBlock, destination, destScaleBias, ClearFlag.None, destMip);

                propertyBlock.SetTexture(HDShaderIDs._Source, source);
                propertyBlock.SetVector(HDShaderIDs._SourceScaleBias, sourceScaleBias);
                propertyBlock.SetBuffer(HDShaderIDs._GaussianWeights, GetGaussianWeights(sampleCount));
                propertyBlock.SetFloat(HDShaderIDs._SampleCount, sampleCount);
                propertyBlock.SetFloat(HDShaderIDs._Radius, radius);
                SetSourceSize(propertyBlock, source);
                ctx.cmd.DrawProcedural(Matrix4x4.identity, customPassUtilsMaterial, verticalBlurPassIndex, MeshTopology.Quads, 4, 1, propertyBlock);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="sampleCount"></param>
        /// <param name="radius"></param>
        /// <param name="sourceMip"></param>
        /// <param name="destMip"></param>
        public static void HorizontalGaussianBlur(in CustomPassContext ctx, RTHandle source, RTHandle destination, int sampleCount = 8, float radius = 5, int sourceMip = 0, int destMip = 0)
            => HorizontalGaussianBlur(ctx, source, destination, fullScreenScaleBias, fullScreenScaleBias, sampleCount, radius, sourceMip, destMip);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="sourceScaleBias"></param>
        /// <param name="destScaleBias"></param>
        /// <param name="sampleCount"></param>
        /// <param name="radius"></param>
        /// <param name="sourceMip"></param>
        /// <param name="destMip"></param>
        public static void HorizontalGaussianBlur(in CustomPassContext ctx, RTHandle source, RTHandle destination, Vector4 sourceScaleBias, Vector4 destScaleBias, int sampleCount = 8, float radius = 5, int sourceMip = 0, int destMip = 0)
        {
            if (source == destination)
                Debug.LogError("Can't blur the buffer. Source has to be different from the destination.");

            using (new ProfilingScope(ctx.cmd, horizontalBlurSampler))
            {
                SetRenderTargetWithScaleBias(ctx, propertyBlock, destination, destScaleBias, ClearFlag.None, destMip);

                propertyBlock.SetTexture(HDShaderIDs._Source, source);
                propertyBlock.SetVector(HDShaderIDs._SourceScaleBias, sourceScaleBias);
                propertyBlock.SetBuffer(HDShaderIDs._GaussianWeights, GetGaussianWeights(sampleCount));
                propertyBlock.SetFloat(HDShaderIDs._SampleCount, sampleCount);
                propertyBlock.SetFloat(HDShaderIDs._Radius, radius);
                SetSourceSize(propertyBlock, source);
                ctx.cmd.DrawProcedural(Matrix4x4.identity, customPassUtilsMaterial, horizontalBlurPassIndex, MeshTopology.Quads, 4, 1, propertyBlock);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="tempTarget"></param>
        /// <param name="sampleCount"></param>
        /// <param name="radius"></param>
        /// <param name="sourceMip"></param>
        /// <param name="destMip"></param>
        /// <param name="downSample"></param>
        public static void GaussianBlur(in CustomPassContext ctx, RTHandle source, RTHandle destination, RTHandle tempTarget, int sampleCount = 9, float radius = 5, int sourceMip = 0, int destMip = 0, bool downSample = true)
            => GaussianBlur(ctx, source, destination, tempTarget, fullScreenScaleBias, fullScreenScaleBias, sampleCount, radius, sourceMip, destMip, downSample);

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="source"></param>
        /// <param name="destination"></param>
        /// <param name="tempTarget"></param>
        /// <param name="sourceScaleBias"></param>
        /// <param name="destScaleBias"></param>
        /// <param name="sampleCount"></param>
        /// <param name="radius"></param>
        /// <param name="sourceMip"></param>
        /// <param name="destMip"></param>
        /// <param name="downSample"></param>
        public static void GaussianBlur(in CustomPassContext ctx, RTHandle source, RTHandle destination, RTHandle tempTarget, Vector4 sourceScaleBias, Vector4 destScaleBias, int sampleCount = 9, float radius = 5, int sourceMip = 0, int destMip = 0, bool downSample = true)
        {
            if (source == tempTarget || destination == tempTarget)
                Debug.LogError("Can't blur the buffer. tempTarget has to be different from both source or destination.");
            if (tempTarget.scaleFactor.x != tempTarget.scaleFactor.y || (tempTarget.scaleFactor.x != 0.5f && tempTarget.scaleFactor.x != 1.0f))
                Debug.LogError($"Can't blur the buffer. Only a scaleFactor of 0.5 or 1.0 is supported on tempTarget. Current scaleFactor: {tempTarget.scaleFactor}");

            // Gaussian blur doesn't like even numbers
            if (sampleCount % 2 == 0)
                sampleCount++;

            using (new ProfilingScope(ctx.cmd, gaussianblurSampler))
            {
                if (downSample)
                {
                    // Downsample to half res in mip 0 of temp target (in case temp target doesn't have any mipmap we use 0)
                    DownSample(ctx, source, tempTarget, sourceScaleBias, destScaleBias, sourceMip, 0);
                    // Vertical blur
                    VerticalGaussianBlur(ctx, tempTarget, destination, sourceScaleBias, destScaleBias, sampleCount, radius, 0, destMip);
                    // Instead of allocating a new buffer on the fly, we copy the data.
                    // We will be able to allocate it when rendergraph lands
                    Copy(ctx, destination, tempTarget, sourceScaleBias, destScaleBias, 0, destMip);
                    // Horizontal blur and upsample
                    HorizontalGaussianBlur(ctx, tempTarget, destination, sourceScaleBias, destScaleBias, sampleCount, radius, sourceMip, destMip);
                }
                else
                {
                    // Vertical blur
                    VerticalGaussianBlur(ctx, source, tempTarget, sourceScaleBias, destScaleBias, sampleCount, radius, sourceMip, destMip);
                    // Horizontal blur and upsample
                    HorizontalGaussianBlur(ctx, tempTarget, destination, sourceScaleBias, destScaleBias, sampleCount, radius, sourceMip, destMip);
                }
            }
        }

        /// <summary>
        /// Simpler version of ScriptableRenderContext.DrawRenderers to draw HDRP materials.
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="layerMask"></param>
        /// <param name="renderQueueFilter"></param>
        /// <param name="overrideMaterial"></param>
        /// <param name="overideMaterialIndex"></param>
        public static void DrawRenderers(in CustomPassContext ctx, LayerMask layerMask, CustomPass.RenderQueueType renderQueueFilter = CustomPass.RenderQueueType.All, Material overrideMaterial = null, int overideMaterialIndex = 0)
        {
            PerObjectData renderConfig = ctx.hdCamera.frameSettings.IsEnabled(FrameSettingsField.Shadowmask) ? HDUtils.k_RendererConfigurationBakedLightingWithShadowMask : HDUtils.k_RendererConfigurationBakedLighting;

            var result = new RendererListDesc(litForwardTags, ctx.cullingResults, ctx.hdCamera.camera)
            {
                rendererConfiguration = renderConfig,
                renderQueueRange = GetRenderQueueRangeFromRenderQueueType(renderQueueFilter),
                sortingCriteria = SortingCriteria.BackToFront,
                excludeObjectMotionVectors = false,
                layerMask = layerMask,
                stateBlock = new RenderStateBlock(RenderStateMask.Depth){ depthState = new DepthState(true, CompareFunction.LessEqual)},
            };

            HDUtils.DrawRendererList(ctx.renderContext, ctx.cmd, RendererList.Create(result));
        }

        /// <summary>
        /// Generate gaussian weights for a given number of samples
        /// </summary>
        /// <param name="weightCount">number of weights you want to generate</param>
        /// <returns>a GPU compute buffer containing the weights</returns>
        internal static ComputeBuffer GetGaussianWeights(int weightCount)
		{
            float[] weights;
            ComputeBuffer gpuWeights;

            if (gaussianWeightsCache.TryGetValue(weightCount, out gpuWeights))
                return gpuWeights;

            weights = new float[weightCount];
			float integrationBound = 3;
			float p = -integrationBound;
            float c = 0;
            float step = (1.0f / (float)weightCount) * integrationBound * 2;
			for (int i = 0; i < weightCount; i++)
			{
				float w = (Gaussian(p) / (float)weightCount) * integrationBound * 2;
                weights[i] = w;
				p += step;
                c += w;
			}

			// Gaussian function
			float Gaussian(float x, float sigma = 1)
			{
				float a = 1.0f / Mathf.Sqrt(2 * Mathf.PI * sigma * sigma);
				float b = Mathf.Exp(-(x * x) / (2 * sigma * sigma));
				return a * b;
			}

            gpuWeights = new ComputeBuffer(weights.Length, sizeof(float));
            gpuWeights.SetData(weights);
            gaussianWeightsCache[weightCount] = gpuWeights;

            return gpuWeights;
		}

        /// <summary>
        /// Convert a Custom Pass render queue type to a RenderQueueRange that can be used in DrawRenderers
        /// </summary>
        /// <param name="type">The type of render queue</param>
        /// <returns>The converted render queue range</returns>
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

        // TODO when rendergraph is available: a PostProcess pass which does the copy with a temp target

        internal static void Cleanup()
        {
            foreach (var gaussianWeights in gaussianWeightsCache)
                gaussianWeights.Value.Release();
            gaussianWeightsCache.Clear();
        }

        internal static void SetRenderTargetWithScaleBias(in CustomPassContext ctx, MaterialPropertyBlock block, RTHandle destination, Vector4 destScaleBias, ClearFlag clearFlag, int miplevel)
        {
            // viewport with RT handle scale and scale factor:
            Rect viewport = new Rect();
            Vector2 destSize = viewport.size = destination.GetScaledSize(destination.rtHandleProperties.currentViewportSize);
            viewport.position = new Vector2(viewport.size.x * destScaleBias.z, viewport.size.y * destScaleBias.w);
            viewport.size *= new Vector2(destScaleBias.x, destScaleBias.y);

            CoreUtils.SetRenderTarget(ctx.cmd, destination, clearFlag, Color.black, miplevel);
            ctx.cmd.SetViewport(viewport);

            block.SetVector(HDShaderIDs._ViewPortSize, new Vector4(destSize.x, destSize.y, 1.0f / destSize.x, 1.0f / destSize.y));
            block.SetVector(HDShaderIDs._ViewportScaleBias, new Vector4(1.0f / destScaleBias.x, 1.0f / destScaleBias.y, destScaleBias.z, destScaleBias.w));
        }
        
        static void SetSourceSize(MaterialPropertyBlock block, RTHandle source)
        {
            Vector2 sourceSize = source.GetScaledSize(source.rtHandleProperties.currentViewportSize);
            block.SetVector(HDShaderIDs._SourceSize, new Vector4(sourceSize.x, sourceSize.y, 1.0f / sourceSize.x, 1.0f / sourceSize.y));
            block.SetVector(HDShaderIDs._SourceScaleFactor, new Vector4(source.scaleFactor.x, source.scaleFactor.y, 1.0f / source.scaleFactor.x, 1.0f / source.scaleFactor.y));
        }
    }
}