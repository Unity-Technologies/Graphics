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
        static ProfilingSampler renderFromCameraSampler = new ProfilingSampler("Render From Camera");
        static ProfilingSampler renderDepthFromCameraSampler = new ProfilingSampler("Render Depth");
        static ProfilingSampler renderNormalFromCameraSampler = new ProfilingSampler("Render Normal");
        static ProfilingSampler renderTangentFromCameraSampler = new ProfilingSampler("Render Tangent");

        static MaterialPropertyBlock    propertyBlock = new MaterialPropertyBlock();
        static Material                 customPassUtilsMaterial;
        static Material                 customPassRenderersUtilsMaterial;


        static Dictionary<int, ComputeBuffer> gaussianWeightsCache = new Dictionary<int, ComputeBuffer>();

        static int downSamplePassIndex;
        static int verticalBlurPassIndex;
        static int horizontalBlurPassIndex;
        static int copyPassIndex;
        static int depthToColorPassIndex;
        static int depthPassIndex;
        static int normalToColorPassIndex;
        static int tangentToColorPassIndex;

        internal static void Initialize()
        {
            customPassUtilsMaterial = CoreUtils.CreateEngineMaterial(HDRenderPipeline.defaultAsset.renderPipelineResources.shaders.customPassUtils);
            downSamplePassIndex = customPassUtilsMaterial.FindPass("Downsample");
            verticalBlurPassIndex = customPassUtilsMaterial.FindPass("VerticalBlur");
            horizontalBlurPassIndex = customPassUtilsMaterial.FindPass("HorizontalBlur");
            copyPassIndex = customPassUtilsMaterial.FindPass("Copy");

            customPassRenderersUtilsMaterial = CoreUtils.CreateEngineMaterial(HDRenderPipeline.defaultAsset.renderPipelineResources.shaders.customPassRenderersUtils);
            depthToColorPassIndex = customPassRenderersUtilsMaterial.FindPass("DepthToColorPass");
            depthPassIndex = customPassRenderersUtilsMaterial.FindPass("DepthPass");
            normalToColorPassIndex = customPassRenderersUtilsMaterial.FindPass("NormalToColorPass");
            tangentToColorPassIndex = customPassRenderersUtilsMaterial.FindPass("TangentToColorPass");
        }

        /// <summary>
        /// Convert the source buffer to an half resolution buffer and output it to the destination buffer.
        /// </summary>
        /// <param name="ctx">Custom Pass Context.</param>
        /// <param name="source">Source to use for the downsample</param>
        /// <param name="destination">Destination buffer of the downsample</param>
        /// <param name="sourceMip">Source mip level to sample from.</param>
        /// <param name="destMip">Destination mip level to write to.</param>
        public static void DownSample(in CustomPassContext ctx, RTHandle source, RTHandle destination, int sourceMip = 0, int destMip = 0)
            => DownSample(ctx, source, destination, fullScreenScaleBias, fullScreenScaleBias, sourceMip, destMip);

        /// <summary>
        /// Convert the source buffer to an half resolution buffer and output it to the destination buffer.
        /// </summary>
        /// <param name="ctx">Custom Pass Context.</param>
        /// <param name="source">Source to use for the downsample</param>
        /// <param name="destination">Destination buffer of the downsample</param>
        /// <param name="sourceScaleBias">Scale and bias to apply when sampling the source buffer</param>
        /// <param name="destScaleBias">Scale and bias to apply when writing into the destination buffer. It's scale is relative to the destination buffer, so if you want an half res downsampling into a fullres buffer you need to specify a scale of 0.5;0,5. If your buffer is already half res Then 1;1 scale works.</param>
        /// <param name="sourceMip">Source mip level to sample from.</param>
        /// <param name="destMip">Destination mip level to write to.</param>
        public static void DownSample(in CustomPassContext ctx, RTHandle source, RTHandle destination, Vector4 sourceScaleBias, Vector4 destScaleBias, int sourceMip = 0, int destMip = 0)
        {
            // Check if the texture provided is at least half of the size of source.
            if (destination.rt.width < source.rt.width / 2 || destination.rt.height < source.rt.height / 2)
                Debug.LogError("Destination for DownSample is too small, it needs to be at least half as big as source.");
            if (source.rt.antiAliasing > 1 || destination.rt.antiAliasing > 1) 
                Debug.LogError($"DownSample is not supported with MSAA buffers");

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
        /// Copy an RTHandle content to another
        /// </summary>
        /// <param name="ctx">Custom Pass Context.</param>
        /// <param name="source">Source to use for the copy</param>
        /// <param name="destination">Destination buffer of the copy</param>
        /// <param name="sourceMip">Source mip level to sample from.</param>
        /// <param name="destMip">Destination mip level to write to.</param>
        public static void Copy(in CustomPassContext ctx, RTHandle source, RTHandle destination, int sourceMip = 0, int destMip = 0)
            => Copy(ctx, source, destination, fullScreenScaleBias, fullScreenScaleBias, sourceMip, destMip);

        /// <summary>
        /// Copy a region of an RTHandle to another
        /// </summary>
        /// <param name="ctx">Custom Pass Context.</param>
        /// <param name="source">Source to use for the copy</param>
        /// <param name="destination">Destination buffer of the copy</param>
        /// <param name="sourceScaleBias">Scale and bias to apply when sampling the source buffer</param>
        /// <param name="destScaleBias">Scale and bias to apply when writing into the destination buffer.</param>
        /// <param name="sourceMip">Source mip level to sample from.</param>
        /// <param name="destMip">Destination mip level to write to.</param>
        public static void Copy(in CustomPassContext ctx, RTHandle source, RTHandle destination, Vector4 sourceScaleBias, Vector4 destScaleBias, int sourceMip = 0, int destMip = 0)
        {
            if (source == destination)
                Debug.LogError("Can't copy the buffer. Source has to be different from the destination.");
            if (source.rt.antiAliasing > 1 || destination.rt.antiAliasing > 1) 
                Debug.LogError($"Copy is not supported with MSAA buffers");

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
        /// Vertical gaussian blur pass
        /// </summary>
        /// <param name="ctx">Custom Pass Context.</param>
        /// <param name="source">Source to use for the gaussian blur.</param>
        /// <param name="destination">Destination buffer of the gaussian blur.</param>
        /// <param name="sampleCount">Number of samples to use for the gaussian blur. A high number will impact performances.</param>
        /// <param name="radius">Radius in pixel of the gaussian blur.</param>
        /// <param name="sourceMip">Source mip level to sample from.</param>
        /// <param name="destMip">Destination mip level to write to.</param>
        public static void VerticalGaussianBlur(in CustomPassContext ctx, RTHandle source, RTHandle destination, int sampleCount = 8, float radius = 5, int sourceMip = 0, int destMip = 0)
            => VerticalGaussianBlur(ctx, source, destination, fullScreenScaleBias, fullScreenScaleBias, sampleCount, radius, sourceMip, destMip);

        /// <summary>
        /// Vertical gaussian blur pass
        /// </summary>
        /// <param name="ctx">Custom Pass Context.</param>
        /// <param name="source">Source to use for the gaussian blur.</param>
        /// <param name="destination">Destination buffer of the gaussian blur.</param>
        /// <param name="sourceScaleBias">Scale and bias to apply when sampling the source buffer</param>
        /// <param name="destScaleBias">Scale and bias to apply when writing into the destination buffer.</param>
        /// <param name="sampleCount">Number of samples to use for the gaussian blur. A high number will impact performances.</param>
        /// <param name="radius">Radius in pixel of the gaussian blur.</param>
        /// <param name="sourceMip">Source mip level to sample from.</param>
        /// <param name="destMip">Destination mip level to write to.</param>
        public static void VerticalGaussianBlur(in CustomPassContext ctx, RTHandle source, RTHandle destination, Vector4 sourceScaleBias, Vector4 destScaleBias, int sampleCount = 8, float radius = 5, int sourceMip = 0, int destMip = 0)
        {
            if (source == destination)
                Debug.LogError("Can't blur the buffer. Source has to be different from the destination.");
            if (source.rt.antiAliasing > 1 || destination.rt.antiAliasing > 1)
                Debug.LogError($"GaussianBlur is not supported with MSAA buffers");

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
        /// Horizontal gaussian blur pass.
        /// </summary>
        /// <param name="ctx">Custom Pass Context.</param>
        /// <param name="source">Source to use for the gaussian blur.</param>
        /// <param name="destination">Destination buffer of the gaussian blur.</param>
        /// <param name="sampleCount">Number of samples to use for the gaussian blur. A high number will impact performances.</param>
        /// <param name="radius">Radius in pixel of the gaussian blur.</param>
        /// <param name="sourceMip">Source mip level to sample from.</param>
        /// <param name="destMip">Destination mip level to write to.</param>
        public static void HorizontalGaussianBlur(in CustomPassContext ctx, RTHandle source, RTHandle destination, int sampleCount = 8, float radius = 5, int sourceMip = 0, int destMip = 0)
            => HorizontalGaussianBlur(ctx, source, destination, fullScreenScaleBias, fullScreenScaleBias, sampleCount, radius, sourceMip, destMip);

        /// <summary>
        /// Horizontal gaussian blur pass.
        /// </summary>
        /// <param name="ctx">Custom Pass Context.</param>
        /// <param name="source">Source to use for the gaussian blur.</param>
        /// <param name="destination">Destination buffer of the gaussian blur.</param>
        /// <param name="sourceScaleBias">Scale and bias to apply when sampling the source buffer.</param>
        /// <param name="destScaleBias">Scale and bias to apply when writing into the destination buffer.</param>
        /// <param name="sampleCount">Number of samples to use for the gaussian blur. A high number will impact performances.</param>
        /// <param name="radius">Radius in pixel of the gaussian blur.</param>
        /// <param name="sourceMip">Source mip level to sample from.</param>
        /// <param name="destMip">Destination mip level to write to.</param>
        public static void HorizontalGaussianBlur(in CustomPassContext ctx, RTHandle source, RTHandle destination, Vector4 sourceScaleBias, Vector4 destScaleBias, int sampleCount = 8, float radius = 5, int sourceMip = 0, int destMip = 0)
        {
            if (source == destination)
                Debug.LogError("Can't blur the buffer. Source has to be different from the destination.");
            if (source.rt.antiAliasing > 1 || destination.rt.antiAliasing > 1) 
                Debug.LogError($"GaussianBlur is not supported with MSAA buffers");

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
        /// Gaussian Blur pass.
        /// </summary>
        /// <param name="ctx">Custom Pass Context.</param>
        /// <param name="source">Source to use for the gaussian blur.</param>
        /// <param name="destination">Destination buffer of the gaussian blur.</param>
        /// <param name="tempTarget">Temporary render target to provide used internally to swap the result between blur passes. Can be half res if downsample is true.</param>
        /// <param name="sampleCount">Number of samples to use for the gaussian blur. A high number will impact performances.</param>
        /// <param name="radius">Radius in pixel of the gaussian blur.</param>
        /// <param name="sourceMip">Source mip level to sample from.</param>
        /// <param name="destMip">Destination mip level to write to.</param>
        /// <param name="downSample">If true, will execute a downsample pass before the blur. It increases the performances of the blur.</param>
        public static void GaussianBlur(in CustomPassContext ctx, RTHandle source, RTHandle destination, RTHandle tempTarget, int sampleCount = 9, float radius = 5, int sourceMip = 0, int destMip = 0, bool downSample = true)
            => GaussianBlur(ctx, source, destination, tempTarget, fullScreenScaleBias, fullScreenScaleBias, sampleCount, radius, sourceMip, destMip, downSample);

        /// <summary>
        /// Gaussian Blur pass.
        /// </summary>
        /// <param name="ctx">Custom Pass Context.</param>
        /// <param name="source">Source to use for the gaussian blur.</param>
        /// <param name="destination">Destination buffer of the gaussian blur.</param>
        /// <param name="tempTarget">Temporary render target to provide used internally to swap the result between blur passes. Can be half res if downsample is true.</param>
        /// <param name="sourceScaleBias">Scale and bias to apply when sampling the source buffer.</param>
        /// <param name="destScaleBias">Scale and bias to apply when writing into the destination buffer.</param>
        /// <param name="sampleCount">Number of samples to use for the gaussian blur. A high number will impact performances.</param>
        /// <param name="radius">Radius in pixel of the gaussian blur.</param>
        /// <param name="sourceMip">Source mip level to sample from.</param>
        /// <param name="destMip">Destination mip level to write to.</param>
        /// <param name="downSample">If true, will execute a downsample pass before the blur. It increases the performances of the blur.</param>
        public static void GaussianBlur(in CustomPassContext ctx, RTHandle source, RTHandle destination, RTHandle tempTarget, Vector4 sourceScaleBias, Vector4 destScaleBias, int sampleCount = 9, float radius = 5, int sourceMip = 0, int destMip = 0, bool downSample = true)
        {
            if (source == tempTarget || destination == tempTarget)
                Debug.LogError("Can't blur the buffer. tempTarget has to be different from both source or destination.");
            if (tempTarget.scaleFactor.x != tempTarget.scaleFactor.y || (tempTarget.scaleFactor.x != 0.5f && tempTarget.scaleFactor.x != 1.0f))
                Debug.LogError($"Can't blur the buffer. Only a scaleFactor of 0.5 or 1.0 is supported on tempTarget. Current scaleFactor: {tempTarget.scaleFactor}");
            if (source.rt.antiAliasing > 1 || destination.rt.antiAliasing > 1 || tempTarget.rt.antiAliasing > 1)
                Debug.LogError($"GaussianBlur is not supported with MSAA buffers");

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
        /// <param name="ctx">Custom Pass Context.</param>
        /// <param name="layerMask">LayerMask to filter the objects to render.</param>
        /// <param name="renderQueueFilter">Render Queue to filter the type of objects you want to render.</param>
        /// <param name="overrideMaterial">Optional material that will be used to render the objects.</param>
        /// <param name="overrideMaterialIndex">Pass index to use for the override material.</param>
        /// <param name="overrideRenderState">The render states to override when rendering the objects.</param>
        public static void DrawRenderers(in CustomPassContext ctx, LayerMask layerMask, CustomPass.RenderQueueType renderQueueFilter = CustomPass.RenderQueueType.All, Material overrideMaterial = null, int overrideMaterialIndex = 0, RenderStateBlock overrideRenderState = default(RenderStateBlock))
        {
            PerObjectData renderConfig = ctx.hdCamera.frameSettings.IsEnabled(FrameSettingsField.Shadowmask) ? HDUtils.k_RendererConfigurationBakedLightingWithShadowMask : HDUtils.k_RendererConfigurationBakedLighting;

            var result = new RendererListDesc(litForwardTags, ctx.cullingResults, ctx.hdCamera.camera)
            {
                rendererConfiguration = renderConfig,
                renderQueueRange = GetRenderQueueRangeFromRenderQueueType(renderQueueFilter),
                sortingCriteria = SortingCriteria.CommonOpaque,
                overrideMaterial = overrideMaterial,
                overrideMaterialPassIndex = overrideMaterialIndex,
                excludeObjectMotionVectors = false,
                layerMask = layerMask,
                stateBlock = overrideRenderState,
            };

            CoreUtils.DrawRendererList(ctx.renderContext, ctx.cmd, RendererList.Create(result));
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

        /// <summary>
        /// Disable the single-pass rendering (use in XR)
        /// </summary>
        public struct DisableSinglePassRendering : IDisposable
        {
            CustomPassContext m_Context;

            /// <summary>
            /// Disable the single-pass rendering (use in XR)
            /// </summary>
            /// <param name="ctx">Custom Pass Context.</param>
            public DisableSinglePassRendering(in CustomPassContext ctx)
            {
                m_Context = ctx;
                if (ctx.hdCamera.xr.enabled)
                    m_Context.hdCamera.xr.StopSinglePass(ctx.cmd);
            }

            /// <summary>
            /// Re-enable the single-pass rendering if it was enabled
            /// </summary>
            void IDisposable.Dispose()
            {
                if (m_Context.hdCamera.xr.enabled)
                    m_Context.hdCamera.xr.StartSinglePass(m_Context.cmd);
            }
        }

        /// <summary>
        /// Render a list of objects from another camera point of view.
        /// </summary>
        /// <param name="ctx">Custom Pass Context.</param>
        /// <param name="view">The camera from where you want the objects to be rendered.</param>
        /// <param name="layerMask">LayerMask to filter the objects to render.</param>
        /// <param name="renderQueueFilter">Render Queue to filter the type of objects you want to render.</param>
        /// <param name="overrideMaterial">Optional material that will be used to render the objects.</param>
        /// <param name="overrideMaterialIndex">Pass index to use for the override material.</param>
        /// <param name="overrideRenderState">The render states to override when rendering the objects.</param>
        public static void RenderFromCamera(in CustomPassContext ctx, Camera view, LayerMask layerMask, CustomPass.RenderQueueType renderQueueFilter = CustomPass.RenderQueueType.All, Material overrideMaterial = null, int overrideMaterialIndex = 0, RenderStateBlock overrideRenderState = default(RenderStateBlock))
            => RenderFromCamera(ctx, view, null, null, ClearFlag.None, layerMask, renderQueueFilter, overrideMaterial, overrideMaterialIndex, overrideRenderState);

        /// <summary>
        /// Render a list of objects from another camera point of view.
        /// </summary>
        /// <param name="ctx">Custom Pass Context.</param>
        /// <param name="view">The camera from where you want the objects to be rendered.</param>
        /// <param name="targetColor">The render target that will be bound to the color buffer before rendering</param>
        /// <param name="targetDepth">The render target that will be bound to the depth buffer before rendering</param>
        /// <param name="clearFlag">The type of clear to do before binding the render targets.</param>
        /// <param name="layerMask">LayerMask to filter the objects to render.</param>
        /// <param name="renderQueueFilter">Render Queue to filter the type of objects you want to render.</param>
        /// <param name="overrideMaterial">Optional material that will be used to render the objects.</param>
        /// <param name="overrideMaterialIndex">Pass index to use for the override material.</param>
        /// <param name="overrideRenderState">The render states to override when rendering the objects.</param>
        public static void RenderFromCamera(in CustomPassContext ctx, Camera view, RTHandle targetColor, RTHandle targetDepth, ClearFlag clearFlag, LayerMask layerMask, CustomPass.RenderQueueType renderQueueFilter = CustomPass.RenderQueueType.All, Material overrideMaterial = null, int overrideMaterialIndex = 0, RenderStateBlock overrideRenderState = default(RenderStateBlock))
        {
            if (targetColor != null && targetDepth != null)
                CoreUtils.SetRenderTarget(ctx.cmd, targetColor, targetDepth, clearFlag);
            else if (targetColor != null)
                CoreUtils.SetRenderTarget(ctx.cmd, targetColor, clearFlag);
            else if (targetDepth != null)
                CoreUtils.SetRenderTarget(ctx.cmd, targetDepth, clearFlag);

            using (new DisableSinglePassRendering(ctx))
            {
                using (new HDRenderPipeline.OverrideCameraRendering(ctx.cmd, view))
                {
                    using (new ProfilingScope(ctx.cmd, renderFromCameraSampler))
                        DrawRenderers(ctx, layerMask, renderQueueFilter, overrideMaterial, overrideMaterialIndex, overrideRenderState);
                }
            }
        }

        /// <summary>
        /// Render eye space depth of objects from the view point of a camera into the color buffer.
        /// </summary>
        /// <param name="ctx">Custom Pass Context.</param>
        /// <param name="view">The camera from where you want the objects to be rendered.</param>
        /// <param name="layerMask">LayerMask to filter the objects to render.</param>
        /// <param name="renderQueueFilter">Render Queue to filter the type of objects you want to render.</param>
        /// <param name="overrideRenderState">The render states to override when rendering the objects.</param>
        public static void RenderDepthFromCamera(in CustomPassContext ctx, Camera view, LayerMask layerMask, CustomPass.RenderQueueType renderQueueFilter = CustomPass.RenderQueueType.All, RenderStateBlock overrideRenderState = default(RenderStateBlock))
            => RenderDepthFromCamera(ctx, view, null, null, ClearFlag.None, layerMask, renderQueueFilter, overrideRenderState);

        /// <summary>
        /// Render eye space depth of objects from the view point of a camera into the color and depth buffers.
        /// </summary>
        /// <param name="ctx">Custom Pass Context.</param>
        /// <param name="view">The camera from where you want the objects to be rendered.</param>
        /// <param name="targetColor">The render target that will be bound to the color buffer before rendering</param>
        /// <param name="targetDepth">The render target that will be bound to the depth buffer before rendering</param>
        /// <param name="clearFlag">The type of clear to do before binding the render targets.</param>
        /// <param name="layerMask">LayerMask to filter the objects to render.</param>
        /// <param name="renderQueueFilter">Render Queue to filter the type of objects you want to render.</param>
        /// <param name="overrideRenderState">The render states to override when rendering the objects.</param>
        public static void RenderDepthFromCamera(in CustomPassContext ctx, Camera view, RTHandle targetColor, RTHandle targetDepth, ClearFlag clearFlag, LayerMask layerMask, CustomPass.RenderQueueType renderQueueFilter = CustomPass.RenderQueueType.All, RenderStateBlock overrideRenderState = default(RenderStateBlock))
        {
            using (new ProfilingScope(ctx.cmd, renderDepthFromCameraSampler))
            {
                if (targetColor == null && targetDepth != null)
                    RenderFromCamera(ctx, view, targetColor, targetDepth, clearFlag, layerMask, renderQueueFilter, customPassRenderersUtilsMaterial, depthPassIndex, overrideRenderState);
                else
                    RenderFromCamera(ctx, view, targetColor, targetDepth, clearFlag, layerMask, renderQueueFilter, customPassRenderersUtilsMaterial, depthToColorPassIndex, overrideRenderState);
            }
        }

        /// <summary>
        /// Render world space normal of objects from the view point of a camera into the color buffer.
        /// </summary>
        /// <param name="ctx">Custom Pass Context.</param>
        /// <param name="view">The camera from where you want the objects to be rendered.</param>
        /// <param name="layerMask">LayerMask to filter the objects to render.</param>
        /// <param name="renderQueueFilter">Render Queue to filter the type of objects you want to render.</param>
        /// <param name="overrideRenderState">The render states to override when rendering the objects.</param>
        public static void RenderNormalFromCamera(in CustomPassContext ctx, Camera view, LayerMask layerMask, CustomPass.RenderQueueType renderQueueFilter = CustomPass.RenderQueueType.All, RenderStateBlock overrideRenderState = default(RenderStateBlock))
            => RenderNormalFromCamera(ctx, view, null, null, ClearFlag.None, layerMask, renderQueueFilter, overrideRenderState);

        /// <summary>
        /// Render world space normal of objects from the view point of a camera into the color buffer.
        /// </summary>
        /// <param name="ctx">Custom Pass Context.</param>
        /// <param name="view">The camera from where you want the objects to be rendered.</param>
        /// <param name="targetColor">The render target that will be bound to the color buffer before rendering</param>
        /// <param name="targetDepth">The render target that will be bound to the depth buffer before rendering</param>
        /// <param name="clearFlag">The type of clear to do before binding the render targets.</param>
        /// <param name="layerMask">LayerMask to filter the objects to render.</param>
        /// <param name="renderQueueFilter">Render Queue to filter the type of objects you want to render.</param>
        /// <param name="overrideRenderState">The render states to override when rendering the objects.</param>
        public static void RenderNormalFromCamera(in CustomPassContext ctx, Camera view, RTHandle targetColor, RTHandle targetDepth, ClearFlag clearFlag, LayerMask layerMask, CustomPass.RenderQueueType renderQueueFilter = CustomPass.RenderQueueType.All, RenderStateBlock overrideRenderState = default(RenderStateBlock))
        {
            using (new ProfilingScope(ctx.cmd, renderNormalFromCameraSampler))
                RenderFromCamera(ctx, view, targetColor, targetDepth, clearFlag, layerMask, renderQueueFilter, customPassRenderersUtilsMaterial, normalToColorPassIndex, overrideRenderState);
        }

        /// <summary>
        /// Render world space tangent of objects from the view point of a camera into the color buffer.
        /// </summary>
        /// <param name="ctx">Custom Pass Context.</param>
        /// <param name="view">The camera from where you want the objects to be rendered.</param>
        /// <param name="layerMask">LayerMask to filter the objects to render.</param>
        /// <param name="renderQueueFilter">Render Queue to filter the type of objects you want to render.</param>
        /// <param name="overrideRenderState">The render states to override when rendering the objects.</param>
        public static void RenderTangentFromCamera(in CustomPassContext ctx, Camera view, LayerMask layerMask, CustomPass.RenderQueueType renderQueueFilter = CustomPass.RenderQueueType.All, RenderStateBlock overrideRenderState = default(RenderStateBlock))
            => RenderTangentFromCamera(ctx, view, null, null, ClearFlag.None, layerMask, renderQueueFilter, overrideRenderState);

        /// <summary>
        /// Render world space tangent of objects from the view point of a camera into the color buffer
        /// </summary>
        /// <param name="ctx">Custom Pass Context.</param>
        /// <param name="view">The camera from where you want the objects to be rendered.</param>
        /// <param name="targetColor">The render target that will be bound to the color buffer before rendering</param>
        /// <param name="targetDepth">The render target that will be bound to the depth buffer before rendering</param>
        /// <param name="clearFlag">The type of clear to do before binding the render targets.</param>
        /// <param name="layerMask">LayerMask to filter the objects to render.</param>
        /// <param name="renderQueueFilter">Render Queue to filter the type of objects you want to render.</param>
        /// <param name="overrideRenderState">The render states to override when rendering the objects.</param>
        public static void RenderTangentFromCamera(in CustomPassContext ctx, Camera view, RTHandle targetColor, RTHandle targetDepth, ClearFlag clearFlag, LayerMask layerMask, CustomPass.RenderQueueType renderQueueFilter = CustomPass.RenderQueueType.All, RenderStateBlock overrideRenderState = default(RenderStateBlock))
        {
            using (new ProfilingScope(ctx.cmd, renderTangentFromCameraSampler))
                RenderFromCamera(ctx, view, targetColor, targetDepth, clearFlag, layerMask, renderQueueFilter, customPassRenderersUtilsMaterial, tangentToColorPassIndex, overrideRenderState);
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
            if (destination.useScaling)
                viewport.size = destination.GetScaledSize(destination.rtHandleProperties.currentViewportSize);
            else
                viewport.size = new Vector2Int(destination.rt.width, destination.rt.height);
            Vector2 destSize = viewport.size;
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