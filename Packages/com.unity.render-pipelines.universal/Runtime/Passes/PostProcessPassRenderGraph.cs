using System.Runtime.CompilerServices;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;
using System;

namespace UnityEngine.Rendering.Universal
{
    internal partial class PostProcessPass : ScriptableRenderPass
    {
        #region StopNaNs
        private class StopNaNsPassData
        {
            internal TextureHandle stopNaNTarget;
            internal TextureHandle sourceTexture;
            internal Material stopNaN;
        }

        public void RenderStopNaN(RenderGraph renderGraph, in TextureHandle activeCameraColor, out TextureHandle stopNaNTarget, ref RenderingData renderingData)
        {
            ref var cameraData = ref renderingData.cameraData;

            var cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            var desc = PostProcessPass.GetCompatibleDescriptor(cameraTargetDescriptor,
                cameraTargetDescriptor.width,
                cameraTargetDescriptor.height,
                cameraTargetDescriptor.graphicsFormat,
                DepthBits.None);

            stopNaNTarget = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_StopNaNsTarget", true, FilterMode.Bilinear);

            using (var builder = renderGraph.AddRasterRenderPass<StopNaNsPassData>("Stop NaNs", out var passData,
                       ProfilingSampler.Get(URPProfileId.RG_StopNaNs)))
            {
                passData.stopNaNTarget = builder.UseTextureFragment(stopNaNTarget, 0, IBaseRenderGraphBuilder.AccessFlags.ReadWrite);
                passData.sourceTexture = builder.UseTexture(activeCameraColor, IBaseRenderGraphBuilder.AccessFlags.Write);
                passData.stopNaN = m_Materials.stopNaN;
                builder.SetRenderFunc((StopNaNsPassData data, RasterGraphContext context) =>
                {
                    var cmd = context.cmd;
                    RTHandle sourceTextureHdl = data.sourceTexture;
                    Vector2 viewportScale = sourceTextureHdl.useScaling? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(cmd, sourceTextureHdl, viewportScale, data.stopNaN, 0);
                });
            }
        }
        #endregion

        #region SMAA
        private class SMAASetupPassData
        {
            internal Vector4 metrics;
            internal Texture2D areaTexture;
            internal Texture2D searchTexture;
            internal float stencilRef;
            internal float stencilMask;
            internal AntialiasingQuality antialiasingQuality;
            internal Material material;
        }

        private class SMAAPassData
        {
            internal TextureHandle destinationTexture;
            internal TextureHandle sourceTexture;
            internal TextureHandle depthStencilTexture;
            internal TextureHandle blendTexture;
            internal CameraData cameraData;
            internal Material material;
        }

        public void RenderSMAA(RenderGraph renderGraph, in TextureHandle source, out TextureHandle SMAATarget, ref RenderingData renderingData)
        {
            ref var cameraData = ref renderingData.cameraData;

            var desc = PostProcessPass.GetCompatibleDescriptor(m_Descriptor,
                m_Descriptor.width,
                m_Descriptor.height,
                m_Descriptor.graphicsFormat,
                DepthBits.None);
            SMAATarget = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_SMAATarget", true, FilterMode.Bilinear);

            var edgeTextureDesc = PostProcessPass.GetCompatibleDescriptor(m_Descriptor,
                m_Descriptor.width,
                m_Descriptor.height,
                m_SMAAEdgeFormat,
                DepthBits.None);
            var edgeTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, edgeTextureDesc, "_EdgeStencilTexture", true, FilterMode.Bilinear);

            var edgeTextureStencilDesc = PostProcessPass.GetCompatibleDescriptor(m_Descriptor,
                m_Descriptor.width,
                m_Descriptor.height,
                GraphicsFormat.None,
                DepthBits.Depth24);
            var edgeTextureStencil = UniversalRenderer.CreateRenderGraphTexture(renderGraph, edgeTextureStencilDesc, "_EdgeTexture", true, FilterMode.Bilinear);

            var blendTextureDesc = PostProcessPass.GetCompatibleDescriptor(m_Descriptor,
                m_Descriptor.width,
                m_Descriptor.height,
                GraphicsFormat.R8G8B8A8_UNorm,
                DepthBits.None);
            var blendTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, blendTextureDesc, "_BlendTexture", true, FilterMode.Point);

            // Anti-aliasing
            var material = m_Materials.subpixelMorphologicalAntialiasing;

            using (var builder = renderGraph.AddRasterRenderPass<SMAASetupPassData>("SMAA Material Setup", out var passData, ProfilingSampler.Get(URPProfileId.RG_SMAAMaterialSetup)))
            {
                const int kStencilBit = 64;
                // TODO RENDERGRAPH: handle dynamic scaling
                passData.metrics = new Vector4(1f / m_Descriptor.width, 1f / m_Descriptor.height, m_Descriptor.width, m_Descriptor.height);
                passData.areaTexture = m_Data.textures.smaaAreaTex;
                passData.searchTexture = m_Data.textures.smaaSearchTex;
                passData.stencilRef = (float)kStencilBit;
                passData.stencilMask = (float)kStencilBit;
                passData.antialiasingQuality = cameraData.antialiasingQuality;
                passData.material = material;

                builder.AllowPassCulling(false);

                builder.SetRenderFunc((SMAASetupPassData data, RasterGraphContext context) =>
                {
                    // Globals
                    data.material.SetVector(ShaderConstants._Metrics, data.metrics);
                    data.material.SetTexture(ShaderConstants._AreaTexture, data.areaTexture);
                    data.material.SetTexture(ShaderConstants._SearchTexture, data.searchTexture);
                    data.material.SetFloat(ShaderConstants._StencilRef, data.stencilRef);
                    data.material.SetFloat(ShaderConstants._StencilMask, data.stencilMask);

                    // Quality presets
                    data.material.shaderKeywords = null;

                    switch (data.antialiasingQuality)
                    {
                        case AntialiasingQuality.Low:
                            data.material.EnableKeyword(ShaderKeywordStrings.SmaaLow);
                            break;
                        case AntialiasingQuality.Medium:
                            data.material.EnableKeyword(ShaderKeywordStrings.SmaaMedium);
                            break;
                        case AntialiasingQuality.High:
                            data.material.EnableKeyword(ShaderKeywordStrings.SmaaHigh);
                            break;
                    }
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<SMAAPassData>("SMAA Edge Detection", out var passData, ProfilingSampler.Get(URPProfileId.RG_SMAAEdgeDetection)))
            {
                passData.destinationTexture = builder.UseTextureFragment(edgeTexture, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                passData.depthStencilTexture = builder.UseTextureFragmentDepth(edgeTextureStencil, IBaseRenderGraphBuilder.AccessFlags.Write);
                passData.sourceTexture = builder.UseTexture(source, IBaseRenderGraphBuilder.AccessFlags.Read);
                UniversalRenderer renderer = (UniversalRenderer)renderingData.cameraData.renderer;
                builder.UseTexture( renderer.resources.GetTexture(UniversalResource.CameraDepth) ,IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.cameraData = renderingData.cameraData;
                passData.material = material;

                builder.SetRenderFunc((SMAAPassData data, RasterGraphContext context) =>
                {
                    var pixelRect = data.cameraData.pixelRect;
                    var SMAAMaterial = data.material;
                    var cmd = context.cmd;
                    RTHandle sourceTextureHdl = data.sourceTexture;

                    // Pass 1: Edge detection
                    Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(cmd, sourceTextureHdl, viewportScale, SMAAMaterial, 0);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<SMAAPassData>("SMAA Blend weights", out var passData, ProfilingSampler.Get(URPProfileId.RG_SMAABlendWeight)))
            {
                passData.destinationTexture = builder.UseTextureFragment(blendTexture, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                passData.sourceTexture = builder.UseTexture(edgeTexture, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.cameraData = renderingData.cameraData;
                passData.material = material;

                builder.SetRenderFunc((SMAAPassData data, RasterGraphContext context) =>
                {
                    var pixelRect = data.cameraData.pixelRect;
                    var SMAAMaterial = data.material;
                    var cmd = context.cmd;
                    RTHandle sourceTextureHdl = data.sourceTexture;

                    // Pass 2: Blend weights
                    Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(cmd, sourceTextureHdl, viewportScale, SMAAMaterial, 1);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<SMAAPassData>("SMAA Neighborhood blending", out var passData, ProfilingSampler.Get(URPProfileId.RG_SMAANeighborhoodBlend)))
            {
                builder.AllowGlobalStateModification(true);
                passData.destinationTexture = builder.UseTextureFragment(SMAATarget, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                passData.sourceTexture = builder.UseTexture(source, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.blendTexture = builder.UseTexture(blendTexture, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.cameraData = renderingData.cameraData;
                passData.material = material;

                builder.SetRenderFunc((SMAAPassData data, RasterGraphContext context) =>
                {
                    var pixelRect = data.cameraData.pixelRect;
                    var SMAAMaterial = data.material;
                    var cmd = context.cmd;
                    RTHandle sourceTextureHdl = data.sourceTexture;

                    // Pass 3: Neighborhood blending
                    cmd.SetGlobalTexture(ShaderConstants._BlendTexture, data.blendTexture);

                    Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(cmd, sourceTextureHdl, viewportScale, SMAAMaterial, 2);
                });
            }
        }
        #endregion

        #region Bloom
        private class UberSetupBloomPassData
        {
            internal Vector4 bloomParams;
            internal bool useRGBM;
            internal Vector4 dirtScaleOffset;
            internal float dirtIntensity;
            internal Texture dirtTexture;
            internal bool highQualityFilteringValue;
            internal TextureHandle bloomTexture;
            internal Material uberMaterial;
        }

        public void UberPostSetupBloomPass(RenderGraph rendergraph, in TextureHandle bloomTexture, Material uberMaterial)
        {
            using (var builder = rendergraph.AddRasterRenderPass<UberSetupBloomPassData>("UberPost - UberPostSetupBloomPass", out var passData, ProfilingSampler.Get(URPProfileId.RG_UberPostSetupBloomPass)))
            {
                // Setup bloom on uber
                var tint = m_Bloom.tint.value.linear;
                var luma = ColorUtils.Luminance(tint);
                tint = luma > 0f ? tint * (1f / luma) : Color.white;
                var bloomParams = new Vector4(m_Bloom.intensity.value, tint.r, tint.g, tint.b);

                // Setup lens dirtiness on uber
                // Keep the aspect ratio correct & center the dirt texture, we don't want it to be
                // stretched or squashed
                var dirtTexture = m_Bloom.dirtTexture.value == null ? Texture2D.blackTexture : m_Bloom.dirtTexture.value;
                float dirtRatio = dirtTexture.width / (float)dirtTexture.height;
                float screenRatio = m_Descriptor.width / (float)m_Descriptor.height;
                var dirtScaleOffset = new Vector4(1f, 1f, 0f, 0f);
                float dirtIntensity = m_Bloom.dirtIntensity.value;

                if (dirtRatio > screenRatio)
                {
                    dirtScaleOffset.x = screenRatio / dirtRatio;
                    dirtScaleOffset.z = (1f - dirtScaleOffset.x) * 0.5f;
                }
                else if (screenRatio > dirtRatio)
                {
                    dirtScaleOffset.y = dirtRatio / screenRatio;
                    dirtScaleOffset.w = (1f - dirtScaleOffset.y) * 0.5f;
                }

                passData.bloomParams = bloomParams;
                passData.dirtScaleOffset = dirtScaleOffset;
                passData.dirtIntensity = dirtIntensity;
                passData.dirtTexture = dirtTexture;
                passData.highQualityFilteringValue = m_Bloom.highQualityFiltering.value;

                passData.bloomTexture = builder.UseTexture(bloomTexture, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.uberMaterial = uberMaterial;

                // TODO RENDERGRAPH: properly setup dependencies between passes
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((UberSetupBloomPassData data, RasterGraphContext context) =>
                {
                    var uberMaterial = data.uberMaterial;
                    uberMaterial.SetVector(ShaderConstants._Bloom_Params, data.bloomParams);
                    uberMaterial.SetFloat(ShaderConstants._Bloom_RGBM, data.useRGBM ? 1f : 0f);
                    uberMaterial.SetVector(ShaderConstants._LensDirt_Params, data.dirtScaleOffset);
                    uberMaterial.SetFloat(ShaderConstants._LensDirt_Intensity, data.dirtIntensity);
                    uberMaterial.SetTexture(ShaderConstants._LensDirt_Texture, data.dirtTexture);

                    // Keyword setup - a bit convoluted as we're trying to save some variants in Uber...
                    if (data.highQualityFilteringValue)
                        uberMaterial.EnableKeyword(data.dirtIntensity > 0f ? ShaderKeywordStrings.BloomHQDirt : ShaderKeywordStrings.BloomHQ);
                    else
                        uberMaterial.EnableKeyword(data.dirtIntensity > 0f ? ShaderKeywordStrings.BloomLQDirt : ShaderKeywordStrings.BloomLQ);

                    uberMaterial.SetTexture(ShaderConstants._Bloom_Texture, data.bloomTexture);
                });
            }
        }

        private class BloomSetupPassData
        {
            internal Vector4 bloomParams;
            internal bool highQualityFilteringValue;
            internal bool useRGBM;
            internal Material material;
        }

        private class BloomPassData
        {
            internal TextureHandle sourceTexture;
            internal TextureHandle sourceTextureLowMip;
            internal Material material;
        }

        public void RenderBloomTexture(RenderGraph renderGraph, in TextureHandle source, out TextureHandle destination, ref RenderingData renderingData)
        {
            // Start at half-res
            int downres = 1;
            switch (m_Bloom.downscale.value)
            {
                case BloomDownscaleMode.Half:
                    downres = 1;
                    break;
                case BloomDownscaleMode.Quarter:
                    downres = 2;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            int tw = m_Descriptor.width >> downres;
            int th = m_Descriptor.height >> downres;

            // Determine the iteration count
            int maxSize = Mathf.Max(tw, th);
            int iterations = Mathf.FloorToInt(Mathf.Log(maxSize, 2f) - 1);
            int mipCount = Mathf.Clamp(iterations, 1, m_Bloom.maxIterations.value);

            var bloomMaterial = m_Materials.bloom;

            using (var builder = renderGraph.AddRasterRenderPass<BloomSetupPassData>("Bloom - Setup", out var passData, ProfilingSampler.Get(URPProfileId.RG_BloomSetupPass)))
            {
                // Pre-filtering parameters
                float clamp = m_Bloom.clamp.value;
                float threshold = Mathf.GammaToLinearSpace(m_Bloom.threshold.value);
                float thresholdKnee = threshold * 0.5f; // Hardcoded soft knee

                // Material setup
                float scatter = Mathf.Lerp(0.05f, 0.95f, m_Bloom.scatter.value);

                passData.bloomParams = new Vector4(scatter, clamp, threshold, thresholdKnee);
                passData.highQualityFilteringValue = m_Bloom.highQualityFiltering.value;
                passData.useRGBM = m_UseRGBM;
                passData.material = bloomMaterial;

                // TODO RENDERGRAPH: properly setup dependencies between passes
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((BloomSetupPassData data, RasterGraphContext context) =>
                {
                    var bloomMaterial = data.material;

                    bloomMaterial.SetVector(ShaderConstants._Params, data.bloomParams);
                    CoreUtils.SetKeyword(bloomMaterial, ShaderKeywordStrings.BloomHQ, data.highQualityFilteringValue);
                    CoreUtils.SetKeyword(bloomMaterial, ShaderKeywordStrings.UseRGBM, data.useRGBM);
                });
            }

            // Prefilter
            var desc = GetCompatibleDescriptor(tw, th, m_DefaultHDRFormat);
            _BloomMipDown[0] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_BloomMipDown", true, FilterMode.Bilinear);
            _BloomMipUp[0] = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_BloomMipUp", true, FilterMode.Bilinear);
            using (var builder = renderGraph.AddRasterRenderPass<BloomPassData>("Bloom - Prefilter", out var passData, ProfilingSampler.Get(URPProfileId.RG_BloomPrefilter)))
            {
                builder.UseTextureFragment(_BloomMipDown[0], 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                passData.sourceTexture = builder.UseTexture(source, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.material = bloomMaterial;

                builder.SetRenderFunc((BloomPassData data, RasterGraphContext context) =>
                {
                    var material = data.material;
                    var cmd = context.cmd;
                    RTHandle sourceTextureHdl = data.sourceTexture;

                    Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(cmd, sourceTextureHdl, viewportScale, material, 0);
                });
            }

            // Downsample - gaussian pyramid
            TextureHandle lastDown = _BloomMipDown[0];
            for (int i = 1; i < mipCount; i++)
            {
                tw = Mathf.Max(1, tw >> 1);
                th = Mathf.Max(1, th >> 1);
                ref TextureHandle mipDown = ref _BloomMipDown[i];
                ref TextureHandle mipUp = ref _BloomMipUp[i];

                desc.width = tw;
                desc.height = th;

                mipDown = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_BloomMipDown", true, FilterMode.Bilinear);
                mipUp = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_BloomMipUp", true, FilterMode.Bilinear);

                // Classic two pass gaussian blur - use mipUp as a temporary target
                //   First pass does 2x downsampling + 9-tap gaussian
                //   Second pass does 9-tap gaussian using a 5-tap filter + bilinear filtering
                using (var builder = renderGraph.AddRasterRenderPass<BloomPassData>("Bloom - First pass", out var passData, ProfilingSampler.Get(URPProfileId.RG_BloomFirstPass)))
                {
                    builder.UseTextureFragment(mipUp, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                    passData.sourceTexture = builder.UseTexture(lastDown, IBaseRenderGraphBuilder.AccessFlags.Read);
                    passData.material = bloomMaterial;

                    builder.SetRenderFunc((BloomPassData data, RasterGraphContext context) =>
                    {
                        var material = data.material;
                        var cmd = context.cmd;
                        RTHandle sourceTextureHdl = data.sourceTexture;

                        Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                        Blitter.BlitTexture(cmd, sourceTextureHdl, viewportScale, material, 1);
                    });
                }

                using (var builder = renderGraph.AddRasterRenderPass<BloomPassData>("Bloom - Second pass", out var passData, ProfilingSampler.Get(URPProfileId.RG_BloomSecondPass)))
                {
                    builder.UseTextureFragment(mipDown, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                    passData.sourceTexture = builder.UseTexture(mipUp, IBaseRenderGraphBuilder.AccessFlags.Read);
                    passData.material = bloomMaterial;

                    builder.SetRenderFunc((BloomPassData data, RasterGraphContext context) =>
                    {
                        var material = data.material;
                        var cmd = context.cmd;
                        RTHandle sourceTextureHdl = data.sourceTexture;

                        Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                        Blitter.BlitTexture(cmd, sourceTextureHdl, viewportScale, material, 2);
                    });
                }

                lastDown = mipDown;
            }

            // Upsample (bilinear by default, HQ filtering does bicubic instead
            for (int i = mipCount - 2; i >= 0; i--)
            {
                TextureHandle lowMip = (i == mipCount - 2) ? _BloomMipDown[i + 1] : _BloomMipUp[i + 1];
                TextureHandle highMip = _BloomMipDown[i];
                TextureHandle dst = _BloomMipUp[i];

                using (var builder = renderGraph.AddRasterRenderPass<BloomPassData>("Bloom - Upsample", out var passData, ProfilingSampler.Get(URPProfileId.RG_BloomUpsample)))
                {
                    builder.UseTextureFragment(dst, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                    builder.AllowGlobalStateModification(true);
                    passData.sourceTexture = builder.UseTexture(highMip, IBaseRenderGraphBuilder.AccessFlags.Read);
                    passData.sourceTextureLowMip = builder.UseTexture(lowMip, IBaseRenderGraphBuilder.AccessFlags.Read);
                    passData.material = bloomMaterial;

                    builder.SetRenderFunc((BloomPassData data, RasterGraphContext context) =>
                    {
                        var material = data.material;
                        var cmd = context.cmd;
                        RTHandle sourceTextureHdl = data.sourceTexture;

                        cmd.SetGlobalTexture(ShaderConstants._SourceTexLowMip, data.sourceTextureLowMip);

                        Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                        Blitter.BlitTexture(cmd, sourceTextureHdl, viewportScale, material, 3);
                    });
                }
            }

            destination = _BloomMipUp[0];
        }
        #endregion

        #region DoF
        public void RenderDoF(RenderGraph renderGraph, in TextureHandle source, out TextureHandle destination, ref RenderingData renderingData)
        {
            var dofMaterial = m_DepthOfField.mode.value == DepthOfFieldMode.Gaussian ? m_Materials.gaussianDepthOfField : m_Materials.bokehDepthOfField;

            var desc = PostProcessPass.GetCompatibleDescriptor(m_Descriptor,
                m_Descriptor.width,
                m_Descriptor.height,
                m_Descriptor.graphicsFormat,
                DepthBits.None);
            destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_DoFTarget", true, FilterMode.Bilinear);

            if (m_DepthOfField.mode.value == DepthOfFieldMode.Gaussian)
            {
                RenderDoFGaussian(renderGraph, source, destination, ref dofMaterial, ref renderingData);
            }
            else if (m_DepthOfField.mode.value == DepthOfFieldMode.Bokeh)
            {
                RenderDoFBokeh(renderGraph, source, destination, ref dofMaterial, ref renderingData);
            }
        }

        private class DoFGaussianSetupPassData
        {
            internal TextureHandle source;
            internal int downSample;
            internal RenderingData renderingData;
            internal Vector3 cocParams;
            internal bool highQualitySamplingValue;
            internal Material material;
        };

        private class DoFGaussianPassData
        {
            internal TextureHandle cocTexture;
            internal TextureHandle colorTexture;
            internal TextureHandle sourceTexture;
            internal Material material;
        };

        public void RenderDoFGaussian(RenderGraph renderGraph, in TextureHandle source, in TextureHandle destination, ref Material dofMaterial, ref RenderingData renderingData)
        {
            int downSample = 2;
            var material = dofMaterial;
            int wh = m_Descriptor.width / downSample;
            int hh = m_Descriptor.height / downSample;

            using (var builder = renderGraph.AddRasterRenderPass<DoFGaussianSetupPassData>("Setup DoF passes", out var passData, ProfilingSampler.Get(URPProfileId.RG_SetupDoF)))
            {
                float farStart = m_DepthOfField.gaussianStart.value;
                float farEnd = Mathf.Max(farStart, m_DepthOfField.gaussianEnd.value);

                // Assumes a radius of 1 is 1 at 1080p
                // Past a certain radius our gaussian kernel will look very bad so we'll clamp it for
                // very high resolutions (4K+).
                float maxRadius = m_DepthOfField.gaussianMaxRadius.value * (wh / 1080f);
                maxRadius = Mathf.Min(maxRadius, 2f);

                passData.source = source;
                passData.downSample = downSample;
                passData.cocParams = new Vector3(farStart, farEnd, maxRadius);
                passData.highQualitySamplingValue = m_DepthOfField.highQualitySampling.value;
                passData.material = material;

                // TODO RENDERGRAPH: properly setup dependencies between passes
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((DoFGaussianSetupPassData data, RasterGraphContext context) =>
                {
                    var cmd = context.cmd;
                    var dofmaterial = data.material;

                    dofmaterial.SetVector(ShaderConstants._CoCParams, data.cocParams);
                    CoreUtils.SetKeyword(dofmaterial, ShaderKeywordStrings.HighQualitySampling, data.highQualitySamplingValue);
                    PostProcessUtils.SetSourceSize(cmd, data.source);
                    cmd.SetGlobalVector(ShaderConstants._DownSampleScaleFactor, new Vector4(1.0f / data.downSample, 1.0f / data.downSample, data.downSample, data.downSample));
                });
            }

            // Temporary textures
            var fullCoCTextureDesc = PostProcessPass.GetCompatibleDescriptor(m_Descriptor, m_Descriptor.width, m_Descriptor.height, m_GaussianCoCFormat);
            var fullCoCTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, fullCoCTextureDesc, "_FullCoCTexture", true, FilterMode.Bilinear);
            var halfCoCTextureDesc = PostProcessPass.GetCompatibleDescriptor(m_Descriptor, wh, hh, m_GaussianCoCFormat);
            var halfCoCTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, halfCoCTextureDesc, "_HalfCoCTexture", true, FilterMode.Bilinear);
            var pingTextureDesc = PostProcessPass.GetCompatibleDescriptor(m_Descriptor, wh, hh, m_DefaultHDRFormat);
            var pingTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, pingTextureDesc, "_PingTexture", true, FilterMode.Bilinear);
            var pongTextureDesc = PostProcessPass.GetCompatibleDescriptor(m_Descriptor, wh, hh, m_DefaultHDRFormat);
            var pongTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, pongTextureDesc, "_PongTexture", true, FilterMode.Bilinear);

            using (var builder = renderGraph.AddRasterRenderPass<DoFGaussianPassData>("Depth of Field - Compute CoC", out var passData, ProfilingSampler.Get(URPProfileId.RG_DOFComputeCOC)))
            {
                builder.UseTextureFragment(fullCoCTexture, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                passData.sourceTexture = builder.UseTexture(source, IBaseRenderGraphBuilder.AccessFlags.Read);

                UniversalRenderer renderer = (UniversalRenderer)renderingData.cameraData.renderer;
                builder.UseTexture(renderer.resources.GetTexture(UniversalResource.CameraDepthTexture), IBaseRenderGraphBuilder.AccessFlags.Read);

                passData.material = material;
                builder.SetRenderFunc((DoFGaussianPassData data, RasterGraphContext context) =>
                {
                    var dofmaterial = data.material;
                    var cmd = context.cmd;
                    RTHandle sourceTextureHdl = data.sourceTexture;
                    // Compute CoC
                    Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(cmd, sourceTextureHdl, viewportScale, dofmaterial, 0);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<DoFGaussianPassData>("Depth of Field - Downscale & Prefilter Color + CoC", out var passData, ProfilingSampler.Get(URPProfileId.RG_DOFDownscalePrefilter)))
            {
                builder.UseTextureFragment(halfCoCTexture, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.UseTextureFragment(pingTexture, 1, IBaseRenderGraphBuilder.AccessFlags.Write);
                // TODO RENDERGRAPH: investigate - Setting MRTs without a depth buffer is not supported, could we add the support and remove the depth?
                builder.UseTextureFragmentDepth(renderGraph.CreateTexture(halfCoCTexture), IBaseRenderGraphBuilder.AccessFlags.ReadWrite);
                builder.AllowGlobalStateModification(true);
                passData.sourceTexture = builder.UseTexture(source, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.cocTexture = builder.UseTexture(fullCoCTexture, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.material = material;

                builder.SetRenderFunc((DoFGaussianPassData data, RasterGraphContext context) =>
                {
                    var dofmaterial = data.material;
                    var cmd = context.cmd;
                    RTHandle sourceTextureHdl = data.sourceTexture;

                    // Downscale & prefilter color + coc
                    cmd.SetGlobalTexture(ShaderConstants._FullCoCTexture, data.cocTexture);

                    Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(cmd, sourceTextureHdl, viewportScale, dofmaterial, 1);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<DoFGaussianPassData>("Depth of Field - Blur H", out var passData, ProfilingSampler.Get(URPProfileId.RG_DOFBlurH)))
            {
                builder.UseTextureFragment(pongTexture, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.AllowGlobalStateModification(true);
                passData.sourceTexture = builder.UseTexture(pingTexture, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.cocTexture = builder.UseTexture(halfCoCTexture, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.material = material;

                builder.SetRenderFunc((DoFGaussianPassData data, RasterGraphContext context) =>
                {
                    var dofmaterial = data.material;
                    var cmd = context.cmd;
                    RTHandle sourceTexture = data.sourceTexture;

                    // Blur
                    cmd.SetGlobalTexture(ShaderConstants._HalfCoCTexture, data.cocTexture);
                    Vector2 viewportScale = sourceTexture.useScaling ? new Vector2(sourceTexture.rtHandleProperties.rtHandleScale.x, sourceTexture.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(cmd, sourceTexture, viewportScale, dofmaterial, 2);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<DoFGaussianPassData>("Depth of Field - Blur V", out var passData, ProfilingSampler.Get(URPProfileId.RG_DOFBlurV)))
            {
                builder.UseTextureFragment(pingTexture, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.AllowGlobalStateModification(true);
                passData.sourceTexture = builder.UseTexture(pongTexture, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.cocTexture = builder.UseTexture(halfCoCTexture, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.material = material;

                builder.SetRenderFunc((DoFGaussianPassData data, RasterGraphContext context) =>
                {
                    var dofmaterial = data.material;
                    var cmd = context.cmd;
                    RTHandle sourceTextureHdl = data.sourceTexture;

                    // Blur
                    cmd.SetGlobalTexture(ShaderConstants._HalfCoCTexture, data.cocTexture);
                    Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(cmd, sourceTextureHdl, viewportScale, dofmaterial, 3);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<DoFGaussianPassData>("Depth of Field - Composite", out var passData, ProfilingSampler.Get(URPProfileId.RG_DOFComposite)))
            {
                builder.UseTextureFragment(destination, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.AllowGlobalStateModification(true);
                passData.sourceTexture = builder.UseTexture(source, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.cocTexture = builder.UseTexture(fullCoCTexture, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.colorTexture = builder.UseTexture(pingTexture, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.material = material;

                builder.SetRenderFunc((DoFGaussianPassData data, RasterGraphContext context) =>
                {
                    var dofmaterial = data.material;
                    var cmd = context.cmd;
                    RTHandle sourceTextureHdl = data.sourceTexture;

                    // Composite
                    cmd.SetGlobalTexture(ShaderConstants._ColorTexture, data.colorTexture);
                    cmd.SetGlobalTexture(ShaderConstants._FullCoCTexture, data.cocTexture);

                    Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(cmd, sourceTextureHdl, viewportScale, dofmaterial, 4);
                });
            }
        }

        private class DoFBokehSetupPassData
        {
            internal Vector4[] bokehKernel;
            internal TextureHandle source;
            internal int downSample;
            internal float uvMargin;
            internal Vector4 cocParams;
            internal bool useFastSRGBLinearConversion;
            internal Material material;
        };

        private class DoFBokehPassData
        {
            internal TextureHandle cocTexture;
            internal TextureHandle dofTexture;
            internal TextureHandle sourceTexture;
            internal Material material;
        };

        public void RenderDoFBokeh(RenderGraph renderGraph, in TextureHandle source, in TextureHandle destination, ref Material dofMaterial, ref RenderingData renderingData)
        {
            int downSample = 2;
            var material = dofMaterial;
            int wh = m_Descriptor.width / downSample;
            int hh = m_Descriptor.height / downSample;

            using (var builder = renderGraph.AddRasterRenderPass<DoFBokehSetupPassData>("Setup DoF passes", out var passData, ProfilingSampler.Get(URPProfileId.RG_SetupDoF)))
            {
                // "A Lens and Aperture Camera Model for Synthetic Image Generation" [Potmesil81]
                float F = m_DepthOfField.focalLength.value / 1000f;
                float A = m_DepthOfField.focalLength.value / m_DepthOfField.aperture.value;
                float P = m_DepthOfField.focusDistance.value;
                float maxCoC = (A * F) / (P - F);
                float maxRadius = GetMaxBokehRadiusInPixels(m_Descriptor.height);
                float rcpAspect = 1f / (wh / (float)hh);


                // Prepare the bokeh kernel constant buffer
                int hash = m_DepthOfField.GetHashCode();
                if (hash != m_BokehHash || maxRadius != m_BokehMaxRadius || rcpAspect != m_BokehRCPAspect)
                {
                    m_BokehHash = hash;
                    m_BokehMaxRadius = maxRadius;
                    m_BokehRCPAspect = rcpAspect;
                    PrepareBokehKernel(maxRadius, rcpAspect);
                }
                float uvMargin = (1.0f / m_Descriptor.height) * downSample;

                passData.bokehKernel = m_BokehKernel;
                passData.source = source;
                passData.downSample = downSample;
                passData.uvMargin = uvMargin;
                passData.cocParams = new Vector4(P, maxCoC, maxRadius, rcpAspect);
                passData.useFastSRGBLinearConversion = m_UseFastSRGBLinearConversion;
                passData.material = material;

                // TODO RENDERGRAPH: properly setup dependencies between passes
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((DoFBokehSetupPassData data, RasterGraphContext context) =>
                {
                    var dofmaterial = data.material;
                    var cmd = context.cmd;

                    CoreUtils.SetKeyword(dofmaterial, ShaderKeywordStrings.UseFastSRGBLinearConversion, data.useFastSRGBLinearConversion);
                    cmd.SetGlobalVector(ShaderConstants._CoCParams, data.cocParams);
                    cmd.SetGlobalVectorArray(ShaderConstants._BokehKernel, data.bokehKernel);
                    cmd.SetGlobalVector(ShaderConstants._DownSampleScaleFactor, new Vector4(1.0f / data.downSample, 1.0f / data.downSample, data.downSample, data.downSample));
                    cmd.SetGlobalVector(ShaderConstants._BokehConstants, new Vector4(data.uvMargin, data.uvMargin * 2.0f));
                    PostProcessUtils.SetSourceSize(cmd, data.source);
                });
            }

            // Temporary textures
            var fullCoCTextureDesc = PostProcessPass.GetCompatibleDescriptor(m_Descriptor, m_Descriptor.width, m_Descriptor.height, GraphicsFormat.R8_UNorm);
            var fullCoCTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, fullCoCTextureDesc, "_FullCoCTexture", true, FilterMode.Bilinear);
            var pingTextureDesc = PostProcessPass.GetCompatibleDescriptor(m_Descriptor, wh, hh, GraphicsFormat.R16G16B16A16_SFloat);
            var pingTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, pingTextureDesc, "_PingTexture", true, FilterMode.Bilinear);
            var pongTextureDesc = PostProcessPass.GetCompatibleDescriptor(m_Descriptor, wh, hh, GraphicsFormat.R16G16B16A16_SFloat);
            var pongTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, pongTextureDesc, "_PongTexture", true, FilterMode.Bilinear);

            using (var builder = renderGraph.AddRasterRenderPass<DoFBokehPassData>("Depth of Field - Compute CoC", out var passData, ProfilingSampler.Get(URPProfileId.RG_DOFComputeCOC)))
            {
                builder.UseTextureFragment(fullCoCTexture, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                passData.sourceTexture = builder.UseTexture(source, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.material = material;

                UniversalRenderer renderer = (UniversalRenderer)renderingData.cameraData.renderer;
                builder.UseTexture(renderer.resources.GetTexture(UniversalResource.CameraDepthTexture), IBaseRenderGraphBuilder.AccessFlags.Read);

                builder.SetRenderFunc((DoFBokehPassData data, RasterGraphContext context) =>
                {
                    var dofmaterial = data.material;
                    var cmd = context.cmd;
                    RTHandle sourceTextureHdl = data.sourceTexture;

                    // Compute CoC
                    Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(cmd, sourceTextureHdl, viewportScale, dofmaterial, 0);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<DoFBokehPassData>("Depth of Field - Downscale & Prefilter Color + CoC", out var passData, ProfilingSampler.Get(URPProfileId.RG_DOFDownscalePrefilter)))
            {
                builder.UseTextureFragment(pingTexture, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.AllowGlobalStateModification(true);
                passData.sourceTexture = builder.UseTexture(source, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.cocTexture = builder.UseTexture(fullCoCTexture, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.material = material;

                builder.SetRenderFunc((DoFBokehPassData data, RasterGraphContext context) =>
                {
                    var dofmaterial = data.material;
                    var cmd = context.cmd;
                    RTHandle sourceTextureHdl = data.sourceTexture;

                    // Downscale & prefilter color + coc
                    cmd.SetGlobalTexture(ShaderConstants._FullCoCTexture, data.cocTexture);
                    Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(cmd, sourceTextureHdl, viewportScale, dofmaterial, 1);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<DoFBokehPassData>("Depth of Field - Bokeh Blur", out var passData, ProfilingSampler.Get(URPProfileId.RG_DOFBlurBokeh)))
            {
                builder.UseTextureFragment(pongTexture, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                passData.sourceTexture = builder.UseTexture(pingTexture, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.material = material;

                builder.SetRenderFunc((DoFBokehPassData data, RasterGraphContext context) =>
                {
                    var dofmaterial = data.material;
                    var cmd = context.cmd;
                    RTHandle sourceTextureHdl = data.sourceTexture;

                    // Downscale & prefilter color + coc
                    Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(cmd, sourceTextureHdl, viewportScale, dofmaterial, 2);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<DoFBokehPassData>("Depth of Field - Post-filtering", out var passData, ProfilingSampler.Get(URPProfileId.RG_DOFPostFilter)))
            {
                builder.UseTextureFragment(pingTexture, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                passData.sourceTexture = builder.UseTexture(pongTexture, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.material = material;

                builder.SetRenderFunc((DoFBokehPassData data, RasterGraphContext context) =>
                {
                    var dofmaterial = data.material;
                    var cmd = context.cmd;
                    RTHandle sourceTextureHdl = data.sourceTexture;

                    // Post - filtering
                    // TODO RENDERGRAPH: Look into loadstore op in BlitDstDiscardContent
                    Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(cmd, sourceTextureHdl, viewportScale, dofmaterial, 3);
                });
            }

            using (var builder = renderGraph.AddRasterRenderPass<DoFBokehPassData>("Depth of Field - Composite", out var passData, ProfilingSampler.Get(URPProfileId.RG_DOFComposite)))
            {
                builder.UseTextureFragment(destination, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                builder.AllowGlobalStateModification(true);
                passData.sourceTexture = builder.UseTexture(source, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.dofTexture = builder.UseTexture(pingTexture, IBaseRenderGraphBuilder.AccessFlags.Read);
                builder.UseTexture(fullCoCTexture, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.material = material;

                builder.SetRenderFunc((DoFBokehPassData data, RasterGraphContext context) =>
                {
                    var dofmaterial = data.material;
                    var cmd = context.cmd;
                    RTHandle sourceTextureHdl = data.sourceTexture;

                    // Composite
                    // TODO RENDERGRAPH: Look into loadstore op in BlitDstDiscardContent
                    cmd.SetGlobalTexture(ShaderConstants._DofTexture, data.dofTexture);
                    Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(cmd, sourceTextureHdl, viewportScale, dofmaterial, 4);
                });
            }
        }
        #endregion

        #region Panini
        private class PaniniProjectionPassData
        {
            internal TextureHandle destinationTexture;
            internal TextureHandle sourceTexture;
            internal RenderTextureDescriptor sourceTextureDesc;
            internal Material material;
            internal Vector4 paniniParams;
            internal bool isPaniniGeneric;
        }

        public void RenderPaniniProjection(RenderGraph renderGraph, in TextureHandle source, out TextureHandle destination, ref RenderingData renderingData)
        {
            var camera = renderingData.cameraData.camera;

            var desc = PostProcessPass.GetCompatibleDescriptor(m_Descriptor,
                m_Descriptor.width,
                m_Descriptor.height,
                m_Descriptor.graphicsFormat,
                DepthBits.None);

            destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_PaniniProjectionTarget", true, FilterMode.Bilinear);

            float distance = m_PaniniProjection.distance.value;
            var viewExtents = CalcViewExtents(camera);
            var cropExtents = CalcCropExtents(camera, distance);

            float scaleX = cropExtents.x / viewExtents.x;
            float scaleY = cropExtents.y / viewExtents.y;
            float scaleF = Mathf.Min(scaleX, scaleY);

            float paniniD = distance;
            float paniniS = Mathf.Lerp(1f, Mathf.Clamp01(scaleF), m_PaniniProjection.cropToFit.value);

            using (var builder = renderGraph.AddRasterRenderPass<PaniniProjectionPassData>("Panini Projection", out var passData, ProfilingSampler.Get(URPProfileId.PaniniProjection)))
            {
                builder.AllowGlobalStateModification(true);
                passData.destinationTexture = builder.UseTextureFragment(destination, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                passData.sourceTexture = builder.UseTexture(source, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.material = m_Materials.paniniProjection;
                passData.paniniParams = new Vector4(viewExtents.x, viewExtents.y, paniniD, paniniS);
                passData.isPaniniGeneric = 1f - Mathf.Abs(paniniD) > float.Epsilon;
                passData.sourceTextureDesc = m_Descriptor;

                builder.SetRenderFunc((PaniniProjectionPassData data, RasterGraphContext context) =>
                {
                    var cmd = context.cmd;
                    RTHandle sourceTextureHdl = data.sourceTexture;

                    cmd.SetGlobalVector(ShaderConstants._Params, data.paniniParams);
                    cmd.EnableShaderKeyword(data.isPaniniGeneric ? ShaderKeywordStrings.PaniniGeneric : ShaderKeywordStrings.PaniniUnitDistance);

                    Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(cmd, sourceTextureHdl, viewportScale, data.material, 0);

                    cmd.DisableShaderKeyword(data.isPaniniGeneric ? ShaderKeywordStrings.PaniniGeneric : ShaderKeywordStrings.PaniniUnitDistance);
                });

                return;
            }
        }
        #endregion

        #region TemporalAA

        private const string _TemporalAATargetName = "_TemporalAATarget";
        private void RenderTemporalAA(RenderGraph renderGraph, ref TextureHandle source, out TextureHandle destination, ref CameraData cameraData)
        {
            var desc = PostProcessPass.GetCompatibleDescriptor(m_Descriptor,
                m_Descriptor.width,
                m_Descriptor.height,
                m_Descriptor.graphicsFormat,
                DepthBits.None);
            destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, _TemporalAATargetName, false, FilterMode.Bilinear);    // TODO: use a constant for the name

            UniversalRenderer renderer = (UniversalRenderer)cameraData.renderer;
            TextureHandle cameraDepth = renderer.resources.GetTexture(UniversalResource.CameraDepth);
            //TextureHandle motionVectors = renderGraph.ImportBackbuffer(m_MotionVectors.rt);
            TextureHandle motionVectors = renderer.resources.GetTexture(UniversalResource.MotionVectorColor);

            TemporalAA.Render(renderGraph, m_Materials.temporalAntialiasing, ref cameraData, ref source, ref cameraDepth, ref motionVectors, ref destination);
        }
        #endregion

        #region MotionBlur
        private class MotionBlurPassData
        {
            internal TextureHandle destinationTexture;
            internal TextureHandle sourceTexture;
            internal Material material;
            internal int passIndex;
            internal Camera camera;
            internal XRPass xr;
            internal float intensity;
            internal float clamp;
        }

        public void RenderMotionBlur(RenderGraph renderGraph, in TextureHandle source, out TextureHandle destination, ref CameraData cameraData)
        {
            var material = m_Materials.cameraMotionBlur;

            var desc = PostProcessPass.GetCompatibleDescriptor(m_Descriptor,
                m_Descriptor.width,
                m_Descriptor.height,
                m_Descriptor.graphicsFormat,
                DepthBits.None);

            destination = UniversalRenderer.CreateRenderGraphTexture(renderGraph, desc, "_MotionBlurTarget", true, FilterMode.Bilinear);

            using (var builder = renderGraph.AddRasterRenderPass<MotionBlurPassData>("Motion Blur", out var passData, ProfilingSampler.Get(URPProfileId.RG_MotionBlur)))
            {
                builder.AllowGlobalStateModification(true);
                passData.destinationTexture = builder.UseTextureFragment(destination, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                passData.sourceTexture = builder.UseTexture(source, IBaseRenderGraphBuilder.AccessFlags.Read);
                UniversalRenderer renderer = (UniversalRenderer)cameraData.renderer;
                builder.UseTexture( renderer.resources.GetTexture(UniversalResource.CameraDepthTexture), IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.material = material;
                passData.passIndex = (int)m_MotionBlur.quality.value;
                passData.camera = cameraData.camera;
                passData.xr = cameraData.xr;
                passData.intensity = m_MotionBlur.intensity.value;
                passData.clamp = m_MotionBlur.clamp.value;
                builder.SetRenderFunc((MotionBlurPassData data, RasterGraphContext context) =>
                {
                    var cmd = context.cmd;
                    RTHandle sourceTextureHdl = data.sourceTexture;

                    UpdateMotionBlurMatrices(ref data.material, data.camera, data.xr);

                    data.material.SetFloat("_Intensity", data.intensity);
                    data.material.SetFloat("_Clamp", data.clamp);

                    PostProcessUtils.SetSourceSize(cmd, data.sourceTexture);
                    Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(cmd, sourceTextureHdl, viewportScale, data.material, 0);
                });

                return;
            }
        }
#endregion

#region LensFlareDataDriven
        private class LensFlarePassData
        {
            internal TextureHandle destinationTexture;
            internal RenderTextureDescriptor sourceDescriptor;
            internal Camera camera;
            internal Material material;
            internal bool usePanini;
            internal float paniniDistance;
            internal float paniniCropToFit;
        }

        void LensFlareDataDrivenComputeOcclusion(RenderGraph renderGraph, ref RenderingData renderingData)
        {
            if (!LensFlareCommonSRP.IsOcclusionRTCompatible())
                return;

            using (var builder = renderGraph.AddRenderPass<LensFlarePassData>("Lens Flare Compute Occlusion", out var passData, ProfilingSampler.Get(URPProfileId.LensFlareDataDrivenComputeOcclusion)))
            {
                RTHandle occH = LensFlareCommonSRP.occlusionRT;
                TextureHandle occlusionHandle = renderGraph.ImportTexture(LensFlareCommonSRP.occlusionRT);
                passData.destinationTexture = builder.WriteTexture(occlusionHandle);
                passData.camera = renderingData.cameraData.camera;
                passData.material = m_Materials.lensFlareDataDriven;
                if (m_PaniniProjection.IsActive())
                {
                    passData.usePanini = true;
                    passData.paniniDistance = m_PaniniProjection.distance.value;
                    passData.paniniCropToFit = m_PaniniProjection.cropToFit.value;
                }
                else
                {
                    passData.usePanini = false;
                    passData.paniniDistance = 1.0f;
                    passData.paniniCropToFit = 1.0f;
                }

                UniversalRenderer renderer = (UniversalRenderer)renderingData.cameraData.renderer;
                builder.ReadTexture(renderer.resources.GetTexture(UniversalResource.CameraDepthTexture));

                builder.SetRenderFunc(
                    (LensFlarePassData data, RenderGraphContext ctx) =>
                    {
                        var gpuView = data.camera.worldToCameraMatrix;
                        var gpuNonJitteredProj = GL.GetGPUProjectionMatrix(data.camera.projectionMatrix, true);
                        // Zero out the translation component.
                        gpuView.SetColumn(3, new Vector4(0, 0, 0, 1));
                        var gpuVP = gpuNonJitteredProj * data.camera.worldToCameraMatrix;

                        LensFlareCommonSRP.ComputeOcclusion(
                            data.material, data.camera,
                            (float)data.sourceDescriptor.width, (float)data.sourceDescriptor.height,
                            data.usePanini, data.paniniDistance, data.paniniCropToFit, true,
                            data.camera.transform.position,
                            gpuVP,
                            ctx.cmd,
                            false, false, null, null,
                            ShaderConstants._FlareOcclusionTex, -1, ShaderConstants._FlareOcclusionIndex, ShaderConstants._FlareTex, ShaderConstants._FlareColorValue,
                            -1, ShaderConstants._FlareData0, ShaderConstants._FlareData1, ShaderConstants._FlareData2, ShaderConstants._FlareData3, ShaderConstants._FlareData4);
                    });
            }
        }

        public void RenderLensFlareDataDriven(RenderGraph renderGraph, in TextureHandle destination, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<LensFlarePassData>("Lens Flare Data Driven Pass", out var passData, ProfilingSampler.Get(URPProfileId.LensFlareDataDriven)))
            {
                // Use WriteTexture here because DoLensFlareDataDrivenCommon will call SetRenderTarget internally.
                // TODO RENDERGRAPH: convert SRP core lensflare to be rendergraph friendly
                passData.destinationTexture = builder.WriteTexture(destination);
                passData.sourceDescriptor = m_Descriptor;
                passData.camera = renderingData.cameraData.camera;
                passData.material = m_Materials.lensFlareDataDriven;
                if (m_PaniniProjection.IsActive())
                {
                    passData.usePanini = true;
                    passData.paniniDistance = m_PaniniProjection.distance.value;
                    passData.paniniCropToFit = m_PaniniProjection.cropToFit.value;
                }
                else
                {
                    passData.usePanini = false;
                    passData.paniniDistance = 1.0f;
                    passData.paniniCropToFit = 1.0f;
                }
                if (LensFlareCommonSRP.IsOcclusionRTCompatible())
                {
                    TextureHandle occlusionHandle = renderGraph.ImportTexture(LensFlareCommonSRP.occlusionRT);
                    builder.ReadTexture(occlusionHandle);
                }

                builder.SetRenderFunc((LensFlarePassData data, RenderGraphContext context) =>
                {
                    var cmd = context.cmd;
                    var camera = data.camera;

                    var gpuView = camera.worldToCameraMatrix;
                    var gpuNonJitteredProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
                    // Zero out the translation component.
                    gpuView.SetColumn(3, new Vector4(0, 0, 0, 1));
                    var gpuVP = gpuNonJitteredProj * camera.worldToCameraMatrix;

                    LensFlareCommonSRP.DoLensFlareDataDrivenCommon(data.material, camera, (float)data.sourceDescriptor.width, (float)data.sourceDescriptor.height,
                        data.usePanini, data.paniniDistance, data.paniniCropToFit,
                        true,
                        camera.transform.position,
                        gpuVP,
                        cmd,
                        false, false, null, null,
                        data.destinationTexture,
                        (Light light, Camera cam, Vector3 wo) => { return GetLensFlareLightAttenuation(light, cam, wo); },
                        ShaderConstants._FlareOcclusionRemapTex, ShaderConstants._FlareOcclusionTex, ShaderConstants._FlareOcclusionIndex,
                        0, 0,
                        ShaderConstants._FlareTex, ShaderConstants._FlareColorValue, ShaderConstants._FlareData0, ShaderConstants._FlareData1, ShaderConstants._FlareData2, ShaderConstants._FlareData3, ShaderConstants._FlareData4,
                        false);
                });
            }
        }
#endregion

#region LensFlareScreenSpace

        private class LensFlareScreenSpacePassData
        {
            internal TextureHandle destinationTexture;
            internal TextureHandle streakTmpTexture;
            internal TextureHandle streakTmpTexture2;
            internal TextureHandle bloomTexture;
            internal TextureHandle result;
            internal RenderTextureDescriptor sourceDescriptor;
            internal Camera camera;
            internal Material material;
            internal int downsample;
        }

        public TextureHandle RenderLensFlareScreenSpace(RenderGraph renderGraph, in TextureHandle destination, ref RenderingData renderingData, TextureHandle bloomTexture)
        {
            var downsample = (int) m_LensFlareScreenSpace.resolution.value;
            int width = m_Descriptor.width / downsample;
            int height = m_Descriptor.height / downsample;

            var streakTextureDesc = PostProcessPass.GetCompatibleDescriptor(m_Descriptor, width, height, m_DefaultHDRFormat);
            var streakTmpTexture = UniversalRenderer.CreateRenderGraphTexture(renderGraph, streakTextureDesc, "_StreakTmpTexture", true, FilterMode.Bilinear);
            var streakTmpTexture2 = UniversalRenderer.CreateRenderGraphTexture(renderGraph, streakTextureDesc, "_StreakTmpTexture2", true, FilterMode.Bilinear);
            TextureHandle result = renderGraph.defaultResources.blackTextureXR;

            using (var builder = renderGraph.AddRenderPass<LensFlareScreenSpacePassData>("Lens Flare Screen Space Pass", out var passData, ProfilingSampler.Get(URPProfileId.LensFlareScreenSpace)))
            {
                // Use WriteTexture here because DoLensFlareScreenSpaceCommon will call SetRenderTarget internally.
                // TODO RENDERGRAPH: convert SRP core lensflare to be rendergraph friendly
                passData.destinationTexture = builder.WriteTexture(destination);
                passData.streakTmpTexture = builder.ReadWriteTexture(streakTmpTexture);
                passData.streakTmpTexture2 = builder.ReadWriteTexture(streakTmpTexture2);
                passData.bloomTexture = builder.ReadWriteTexture(bloomTexture);
                passData.sourceDescriptor = m_Descriptor;
                passData.camera = renderingData.cameraData.camera;
                passData.material = m_Materials.lensFlareScreenSpace;
                passData.downsample = downsample;
                
                passData.result = builder.WriteTexture(renderGraph.CreateTexture(new TextureDesc(width, height, true)
                    { colorFormat = GraphicsFormat.R16G16B16A16_SFloat, enableRandomWrite = true, useMipMap = false, name = "Lens Flare Screen Space Result" }));

                builder.SetRenderFunc((LensFlareScreenSpacePassData data, RenderGraphContext context) =>
                {
                    var cmd = context.cmd;
                    var camera = data.camera;
                    var ratio = (int) m_LensFlareScreenSpace.resolution.value;

                    LensFlareCommonSRP.DoLensFlareScreenSpaceCommon(
                        m_Materials.lensFlareScreenSpace,
                        camera,
                        (float)data.sourceDescriptor.width,
                        (float)data.sourceDescriptor.height,
                        m_LensFlareScreenSpace.tintColor.value,
                        data.bloomTexture,
                        null, // We don't have any spectral LUT in URP
                        data.streakTmpTexture,
                        data.streakTmpTexture2,
                        new Vector4(
                            m_LensFlareScreenSpace.intensity.value,
                            m_LensFlareScreenSpace.firstFlareIntensity.value,
                            m_LensFlareScreenSpace.secondaryFlareIntensity.value,
                            m_LensFlareScreenSpace.warpedFlareIntensity.value),
                        new Vector4(
                            Mathf.Pow(m_LensFlareScreenSpace.vignetteEffect.value, 0.25f),
                            m_LensFlareScreenSpace.startingPosition.value,
                            m_LensFlareScreenSpace.scale.value,
                            0), // Free slot, not used
                        new Vector4(
                            m_LensFlareScreenSpace.samples.value,
                            m_LensFlareScreenSpace.sampleDimmer.value,
                            m_LensFlareScreenSpace.chromaticAbberationIntensity.value / 20f,
                            0), // No need to pass a chromatic aberration sample count, hardcoded at 3 in shader
                        new Vector4(
                            m_LensFlareScreenSpace.streaksIntensity.value,
                            m_LensFlareScreenSpace.streaksLength.value * 10,
                            m_LensFlareScreenSpace.streaksOrientation.value / 90f,
                            m_LensFlareScreenSpace.streaksThreshold.value),
                        new Vector4(
                            data.downsample,
                            1.0f / m_LensFlareScreenSpace.warpedFlareScale.value.x,
                            1.0f / m_LensFlareScreenSpace.warpedFlareScale.value.y,
                            0), // Free slot, not used
                        cmd,
                        data.result,
                        ShaderConstants._LensFlareScreenSpaceBloomTexture,
                        ShaderConstants._LensFlareScreenSpaceResultTexture,
                        0, // No identifiers for SpectralLut Texture
                        ShaderConstants._LensFlareScreenSpaceStreakTex,
                        ShaderConstants._LensFlareScreenSpaceMipLevel,
                        ShaderConstants._LensFlareScreenSpaceTintColor,
                        ShaderConstants._LensFlareScreenSpaceParams1,
                        ShaderConstants._LensFlareScreenSpaceParams2,
                        ShaderConstants._LensFlareScreenSpaceParams3,
                        ShaderConstants._LensFlareScreenSpaceParams4,
                        ShaderConstants._LensFlareScreenSpaceParams5,
                        false);
                });
                return passData.bloomTexture;   
            }
        }

#endregion

        static private void ScaleViewportAndBlit(RasterCommandBuffer cmd, RTHandle sourceTextureHdl, RTHandle dest, ref CameraData cameraData, Material material)
        {
            Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
            var yflip = cameraData.IsRenderTargetProjectionMatrixFlipped(dest);
            Vector4 scaleBias = !yflip ? new Vector4(viewportScale.x, -viewportScale.y, 0, viewportScale.y) : new Vector4(viewportScale.x, viewportScale.y, 0, 0);
            RenderTargetIdentifier cameraTarget = BuiltinRenderTextureType.CameraTarget;
        #if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
                cameraTarget = cameraData.xr.renderTarget;
        #endif
            if (dest.nameID == cameraTarget || cameraData.targetTexture != null)
                cmd.SetViewport(cameraData.pixelRect);

            Blitter.BlitTexture(cmd, sourceTextureHdl, scaleBias, material, 0);
        }

#region FinalPass
        private class PostProcessingFinalSetupPassData
        {
            internal TextureHandle destinationTexture;
            internal TextureHandle sourceTexture;
            internal Material material;
            internal CameraData cameraData;
        }

        public void RenderFinalSetup(RenderGraph renderGraph, in TextureHandle source, in TextureHandle destination, ref RenderingData renderingData)
        {
            // Scaled FXAA
            UniversalRenderer renderer = (UniversalRenderer)renderingData.cameraData.renderer;

            using (var builder = renderGraph.AddRasterRenderPass<PostProcessingFinalSetupPassData>("Postprocessing Final Setup Pass", out var passData, ProfilingSampler.Get(URPProfileId.RG_FinalSetup)))
            {
                passData.destinationTexture = builder.UseTextureFragment(destination, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                passData.sourceTexture = builder.UseTexture(source, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.cameraData = renderingData.cameraData;
                passData.material = m_Materials.scalingSetup;

                builder.SetRenderFunc((PostProcessingFinalSetupPassData data, RasterGraphContext context) =>
                {
                    var cmd = context.cmd;
                    ref var cameraData = ref data.cameraData;
                    var camera = data.cameraData.camera;
                    var material = data.material;
                    RTHandle sourceTextureHdl = data.sourceTexture;

                    PostProcessUtils.SetSourceSize(cmd, sourceTextureHdl);

                    material.EnableKeyword(ShaderKeywordStrings.Fxaa);

                    ScaleViewportAndBlit(cmd, sourceTextureHdl, data.destinationTexture, ref cameraData, material);
                });
                return;
            }
        }

        private class PostProcessingFinalFSRScalePassData
        {
            internal TextureHandle destinationTexture;
            internal TextureHandle sourceTexture;
            internal Material material;
            internal CameraData cameraData;

        }

        public void RenderFinalFSRScale(RenderGraph renderGraph, in TextureHandle source, in TextureHandle destination, ref RenderingData renderingData)
        {
            // FSR upscale
            UniversalRenderer renderer = (UniversalRenderer)renderingData.cameraData.renderer;
            m_Materials.easu.shaderKeywords = null;

            using (var builder = renderGraph.AddRasterRenderPass<PostProcessingFinalFSRScalePassData>("Postprocessing Final FSR Scale Pass", out var passData, ProfilingSampler.Get(URPProfileId.RG_FinalFSRScale)))
            {
                builder.AllowGlobalStateModification(true);
                passData.destinationTexture = builder.UseTextureFragment(destination, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                passData.sourceTexture = builder.UseTexture(source, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.cameraData = renderingData.cameraData;
                passData.material = m_Materials.easu;

                builder.SetRenderFunc((PostProcessingFinalFSRScalePassData data, RasterGraphContext context) =>
                {
                    var cmd = context.cmd;
                    ref var cameraData = ref data.cameraData;
                    var sourceTex = data.sourceTexture;
                    var destTex = data.destinationTexture;
                    var material = data.material;
                    RTHandle sourceHdl = (RTHandle)sourceTex;
                    RTHandle destHdl = (RTHandle)destTex;

                    var fsrInputSize = new Vector2(sourceHdl.referenceSize.x, sourceHdl.referenceSize.y);
                    var fsrOutputSize = new Vector2(destHdl.referenceSize.x, destHdl.referenceSize.y);
                    FSRUtils.SetEasuConstants(cmd, fsrInputSize, fsrInputSize, fsrOutputSize);

                    Vector2 viewportScale = sourceHdl.useScaling ? new Vector2(sourceHdl.rtHandleProperties.rtHandleScale.x, sourceHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(cmd, sourceHdl, viewportScale, material, 0);
                });
                return;
            }
        }

        private class PostProcessingFinalBlitPassData
        {
            internal TextureHandle destinationTexture;
            internal TextureHandle sourceTexture;
            internal Material material;
            internal CameraData cameraData;
            internal bool isFxaaEnabled;
            internal bool isFsrEnabled;
        }

        public void RenderFinalBlit(RenderGraph renderGraph, in TextureHandle source, ref RenderingData renderingData, bool performFXAA, bool performFsr)
        {
            UniversalRenderer renderer = (UniversalRenderer)renderingData.cameraData.renderer;

            using (var builder = renderGraph.AddRasterRenderPass<PostProcessingFinalBlitPassData>("Postprocessing Final Blit Pass", out var passData, ProfilingSampler.Get(URPProfileId.RG_FinalBlit)))
            {
                builder.AllowGlobalStateModification(true);
                passData.destinationTexture = builder.UseTextureFragment(renderer.resources.GetTexture(UniversalResource.BackBufferColor), 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                passData.sourceTexture = builder.UseTexture(source, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.cameraData = renderingData.cameraData;
                passData.material = m_Materials.finalPass;
                passData.isFxaaEnabled = performFXAA;
                passData.isFsrEnabled = performFsr;

                builder.SetRenderFunc((PostProcessingFinalBlitPassData data, RasterGraphContext context) =>
                {
                    var cmd = context.cmd;
                    ref var cameraData = ref data.cameraData;
                    var material = data.material;
                    var isFxaaEnabled = data.isFxaaEnabled;
                    var isFsrEnabled = data.isFsrEnabled;
                    RTHandle sourceTextureHdl = data.sourceTexture;
                    RTHandle destinationTextureHdl = data.destinationTexture;

                    PostProcessUtils.SetSourceSize(cmd, data.sourceTexture);

                    if (isFxaaEnabled)
                        material.EnableKeyword(ShaderKeywordStrings.Fxaa);

                    if (isFsrEnabled)
                    {
                        // RCAS
                        // Use the override value if it's available, otherwise use the default.
                        float sharpness = cameraData.fsrOverrideSharpness ? cameraData.fsrSharpness : FSRUtils.kDefaultSharpnessLinear;


                        // Set up the parameters for the RCAS pass unless the sharpness value indicates that it wont have any effect.
                        if (cameraData.fsrSharpness > 0.0f)
                        {
                            // RCAS is performed during the final post blit, but we set up the parameters here for better logical grouping.
                            material.EnableKeyword(ShaderKeywordStrings.Rcas);
                            FSRUtils.SetRcasConstantsLinear(cmd, sharpness);
                        }
                    }

                    bool isRenderToBackBufferTarget = !cameraData.isSceneViewCamera;
#if ENABLE_VR && ENABLE_XR_MODULE
                    if (cameraData.xr.enabled)
                        isRenderToBackBufferTarget = destinationTextureHdl == cameraData.xr.renderTarget;
#endif
                    Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;

                    // We y-flip if
                    // 1) we are blitting from render texture to back buffer(UV starts at bottom) and
                    // 2) renderTexture starts UV at top
                    bool yflip = isRenderToBackBufferTarget && cameraData.targetTexture == null && SystemInfo.graphicsUVStartsAtTop;
                    Vector4 scaleBias = yflip ? new Vector4(viewportScale.x, -viewportScale.y, 0, viewportScale.y) : new Vector4(viewportScale.x, viewportScale.y, 0, 0);

                    cmd.SetViewport(cameraData.pixelRect);
                    Blitter.BlitTexture(cmd, sourceTextureHdl, scaleBias, material, 0);
                });

                return;
            }
        }

        public void RenderFinalPassRenderGraph(RenderGraph renderGraph, in TextureHandle source, ref RenderingData renderingData)
        {
            var stack = VolumeManager.instance.stack;
            m_FilmGrain = stack.GetComponent<FilmGrain>();
            m_Tonemapping = stack.GetComponent<Tonemapping>();

            ref var cameraData = ref renderingData.cameraData;
            var material = m_Materials.finalPass;
            var cmd = renderingData.commandBuffer;

            material.shaderKeywords = null;

            // TODO RENDERGRAPH: when we remove the old path we should review the naming of these variables...
            // m_HasFinalPass is used to let FX passes know when they are not being called by the actual final pass, so they can skip any "final work"
            m_HasFinalPass = false;
            // m_IsFinalPass is used by effects called by RenderFinalPassRenderGraph, so we let them know that we are in a final PP pass
            m_IsFinalPass = true;

            if (m_FilmGrain.active)
            {
                material.EnableKeyword(ShaderKeywordStrings.FilmGrain);
                PostProcessUtils.ConfigureFilmGrain(
                    m_Data,
                    m_FilmGrain,
                    cameraData.pixelWidth, cameraData.pixelHeight,
                    material
                );
            }

            if (cameraData.isDitheringEnabled)
            {
                material.EnableKeyword(ShaderKeywordStrings.Dithering);
                m_DitheringTextureIndex = PostProcessUtils.ConfigureDithering(
                    m_Data,
                    m_DitheringTextureIndex,
                    cameraData.pixelWidth, cameraData.pixelHeight,
                    material
                );
            }

            if (RequireSRGBConversionBlitToBackBuffer(ref cameraData))
                material.EnableKeyword(ShaderKeywordStrings.LinearToSRGBConversion);

            GetActiveDebugHandler(ref renderingData)?.UpdateShaderGlobalPropertiesForFinalValidationPass(cmd, ref cameraData, !m_HasFinalPass);

            // TODO: Investigate how to make FXAA and FSR work with HDR output.
            bool outputToHDR = cameraData.isHDROutputActive;
            bool performFxaa = (cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing) && !outputToHDR;
            bool isFsrEnabled = ((cameraData.imageScalingMode == ImageScalingMode.Upscaling) && (cameraData.upscalingFilter == ImageUpscalingFilter.FSR)) && !outputToHDR;
            bool isScaling = cameraData.imageScalingMode != ImageScalingMode.None;

            var tempRtDesc = cameraData.cameraTargetDescriptor;
            tempRtDesc.msaaSamples = 1;
            tempRtDesc.depthBufferBits = 0;

            // Select a UNORM format since we've already performed tonemapping. (Values are in 0-1 range)
            // This improves precision and is required if we want to avoid excessive banding when FSR is in use.
            tempRtDesc.graphicsFormat = UniversalRenderPipeline.MakeUnormRenderTextureGraphicsFormat();

            var scalingSetupTarget = UniversalRenderer.CreateRenderGraphTexture(renderGraph, tempRtDesc, "scalingSetupTarget", true, FilterMode.Point);
            var upscaleRtDesc = tempRtDesc;
            upscaleRtDesc.width = cameraData.pixelWidth;
            upscaleRtDesc.height = cameraData.pixelHeight;
            var upScaleTarget = UniversalRenderer.CreateRenderGraphTexture(renderGraph, upscaleRtDesc, "_UpscaledTexture", true, FilterMode.Point);

            var currentSource = source;
            if (isScaling)
            {
                // When FXAA is needed while scaling is active, we must perform it before the scaling takes place.
                if (performFxaa)
                {
                    RenderFinalSetup(renderGraph, in currentSource, in scalingSetupTarget, ref renderingData);
                    currentSource = scalingSetupTarget;

                    // Indicate that we no longer need to perform FXAA in the final pass since it was already perfomed here.
                    performFxaa = false;
                }

                switch (cameraData.imageScalingMode)
                {
                    case ImageScalingMode.Upscaling:
                    {
                        switch (cameraData.upscalingFilter)
                        {
                            case ImageUpscalingFilter.Point:
                            {
                                material.EnableKeyword(ShaderKeywordStrings.PointSampling);
                                break;
                            }
                            case ImageUpscalingFilter.Linear:
                            {
                                break;
                            }
                            case ImageUpscalingFilter.FSR:
                            {
                                RenderFinalFSRScale(renderGraph, in currentSource, in upScaleTarget, ref renderingData);
                                currentSource = upScaleTarget;
                                break;
                            }
                        }
                        break;
                    }
                    case ImageScalingMode.Downscaling:
                    {
                        break;
                    }
                }
            }

            RenderFinalBlit(renderGraph, in currentSource, ref renderingData, performFxaa, isFsrEnabled);
        }
#endregion

#region UberPost
        private class UberPostPassData
        {
            internal TextureHandle destinationTexture;
            internal TextureHandle sourceTexture;
            internal TextureHandle lutTexture;
            internal Vector4 lutParams;
            internal TextureHandle userLutTexture;
            internal Vector4 userLutParams;
            internal Material material;
            internal CameraData cameraData;
            internal TonemappingMode toneMappingMode;
            internal bool isHdr;
            internal bool isBackbuffer;
        }

        public void RenderUberPost(RenderGraph renderGraph, in TextureHandle sourceTexture, in TextureHandle destTexture, in TextureHandle lutTexture, ref RenderingData renderingData)
        {
            var material = m_Materials.uber;
            ref var postProcessingData = ref renderingData.postProcessingData;
            bool hdr = postProcessingData.gradingMode == ColorGradingMode.HighDynamicRange;
            int lutHeight = postProcessingData.lutSize;
            int lutWidth = lutHeight * lutHeight;

            // Source material setup
            float postExposureLinear = Mathf.Pow(2f, m_ColorAdjustments.postExposure.value);
            Vector4 lutParams = new Vector4(1f / lutWidth, 1f / lutHeight, lutHeight - 1f, postExposureLinear);

            RTHandle userLutRThdl = m_ColorLookup.texture.value ? RTHandles.Alloc(m_ColorLookup.texture.value) : null;
            TextureHandle userLutTexture = renderGraph.ImportTexture(userLutRThdl);
            Vector4 userLutParams = !m_ColorLookup.IsActive()
                ? Vector4.zero
                : new Vector4(1f / m_ColorLookup.texture.value.width,
                    1f / m_ColorLookup.texture.value.height,
                    m_ColorLookup.texture.value.height - 1f,
                    m_ColorLookup.contribution.value);

            using (var builder = renderGraph.AddRasterRenderPass<UberPostPassData>("Postprocessing Uber Post Pass", out var passData, ProfilingSampler.Get(URPProfileId.RG_UberPost)))
            {
                builder.AllowGlobalStateModification(true);
                passData.destinationTexture = builder.UseTextureFragment(destTexture, 0, IBaseRenderGraphBuilder.AccessFlags.Write);
                passData.sourceTexture = builder.UseTexture(sourceTexture, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.lutTexture = builder.UseTexture(lutTexture, IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.lutParams = lutParams;
                if (userLutTexture.IsValid())
                    passData.userLutTexture = builder.UseTexture(userLutTexture, IBaseRenderGraphBuilder.AccessFlags.Read);
                if (m_Bloom.IsActive())
                    builder.UseTexture(_BloomMipUp[0], IBaseRenderGraphBuilder.AccessFlags.Read);
                passData.userLutParams = userLutParams;
                passData.cameraData = renderingData.cameraData;
                passData.material = material;
                passData.toneMappingMode = m_Tonemapping.mode.value;
                passData.isHdr = hdr;

                builder.SetRenderFunc((UberPostPassData data, RasterGraphContext context) =>
                {
                    var cmd = context.cmd;
                    ref var cameraData = ref data.cameraData;
                    var camera = data.cameraData.camera;
                    var material = data.material;
                    RTHandle sourceTextureHdl = data.sourceTexture;

                    material.SetTexture(ShaderConstants._InternalLut, data.lutTexture);
                    material.SetVector(ShaderConstants._Lut_Params, data.lutParams);
                    material.SetTexture(ShaderConstants._UserLut, data.userLutTexture);
                    material.SetVector(ShaderConstants._UserLut_Params, data.userLutParams);

                    if (data.isHdr)
                    {
                        material.EnableKeyword(ShaderKeywordStrings.HDRGrading);
                    }
                    else
                    {
                        switch (data.toneMappingMode)
                        {
                            case TonemappingMode.Neutral: material.EnableKeyword(ShaderKeywordStrings.TonemapNeutral); break;
                            case TonemappingMode.ACES: material.EnableKeyword(ShaderKeywordStrings.TonemapACES); break;
                            default: break; // None
                        }
                    }

                    // Done with Uber, blit it
                    ScaleViewportAndBlit(cmd, sourceTextureHdl, data.destinationTexture, ref cameraData, material);
                });

                return;
            }
        }
#endregion

        private class PostFXSetupPassData { }

        public void RenderPostProcessingRenderGraph(RenderGraph renderGraph, in TextureHandle activeCameraColorTexture, in TextureHandle lutTexture, in TextureHandle postProcessingTarget ,ref RenderingData renderingData, bool hasFinalPass)
        {
            var stack = VolumeManager.instance.stack;
            m_DepthOfField = stack.GetComponent<DepthOfField>();
            m_MotionBlur = stack.GetComponent<MotionBlur>();
            m_PaniniProjection = stack.GetComponent<PaniniProjection>();
            m_Bloom = stack.GetComponent<Bloom>();
            m_LensFlareScreenSpace = stack.GetComponent<ScreenSpaceLensFlare>();
            m_LensDistortion = stack.GetComponent<LensDistortion>();
            m_ChromaticAberration = stack.GetComponent<ChromaticAberration>();
            m_Vignette = stack.GetComponent<Vignette>();
            m_ColorLookup = stack.GetComponent<ColorLookup>();
            m_ColorAdjustments = stack.GetComponent<ColorAdjustments>();
            m_Tonemapping = stack.GetComponent<Tonemapping>();
            m_FilmGrain = stack.GetComponent<FilmGrain>();
            m_UseFastSRGBLinearConversion = renderingData.postProcessingData.useFastSRGBLinearConversion;
            m_SupportDataDrivenLensFlare = renderingData.postProcessingData.supportDataDrivenLensFlare;
            m_SupportScreenSpaceLensFlare = renderingData.postProcessingData.supportScreenSpaceLensFlare;
            // TODO RENDERGRAPH: the descriptor should come from postProcessingTarget, not cameraTarget
            m_Descriptor = renderingData.cameraData.cameraTargetDescriptor;
            m_Descriptor.useMipMap = false;
            m_Descriptor.autoGenerateMips = false;
            m_HasFinalPass = hasFinalPass;

            ref CameraData cameraData = ref renderingData.cameraData;
            ref ScriptableRenderer renderer = ref cameraData.renderer;
            bool isSceneViewCamera = cameraData.isSceneViewCamera;

            //We blit back and forth without msaa untill the last blit.
            bool useStopNan = cameraData.isStopNaNEnabled && m_Materials.stopNaN != null;
            bool useSubPixelMorpAA = cameraData.antialiasing == AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var dofMaterial = m_DepthOfField.mode.value == DepthOfFieldMode.Gaussian ? m_Materials.gaussianDepthOfField : m_Materials.bokehDepthOfField;
            bool useDepthOfField = m_DepthOfField.IsActive() && !isSceneViewCamera && dofMaterial != null;
            bool useLensFlare = !LensFlareCommonSRP.Instance.IsEmpty() && m_SupportDataDrivenLensFlare;
            bool useLensFlareScreenSpace = m_LensFlareScreenSpace.IsActive() && m_SupportScreenSpaceLensFlare;
            bool useMotionBlur = m_MotionBlur.IsActive() && !isSceneViewCamera;
            bool usePaniniProjection = m_PaniniProjection.IsActive() && !isSceneViewCamera;
            bool isFsrEnabled = ((cameraData.imageScalingMode == ImageScalingMode.Upscaling) && (cameraData.upscalingFilter == ImageUpscalingFilter.FSR));

            // Note that enabling jitters uses the same CameraData::IsTemporalAAEnabled(). So if we add any other kind of overrides (like
            // disable useTemporalAA if another feature is disabled) then we need to put it in CameraData::IsTemporalAAEnabled() as opposed
            // to tweaking the value here.
            bool useTemporalAA = cameraData.IsTemporalAAEnabled();
            if (cameraData.antialiasing == AntialiasingMode.TemporalAntiAliasing && !useTemporalAA)
                TemporalAA.ValidateAndWarn(ref cameraData);

            using (var builder = renderGraph.AddRasterRenderPass<PostFXSetupPassData>("Setup PostFX passes", out var passData,
                ProfilingSampler.Get(URPProfileId.RG_SetupPostFX)))
            {
                // TODO RENDERGRAPH: properly setup dependencies between passes
                builder.AllowPassCulling(false);
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc((PostFXSetupPassData data, RasterGraphContext context) =>
                {
                    // Setup projection matrix for cmd.DrawMesh()
                    context.cmd.SetGlobalMatrix(ShaderConstants._FullscreenProjMat, GL.GetGPUProjectionMatrix(Matrix4x4.identity, true));
                });
            }

            TextureHandle currentSource = activeCameraColorTexture;

            // Optional NaN killer before post-processing kicks in
            // stopNaN may be null on Adreno 3xx. It doesn't support full shader level 3.5, but SystemInfo.graphicsShaderLevel is 35.
            if (useStopNan)
            {
                RenderStopNaN(renderGraph, in currentSource, out var stopNaNTarget, ref renderingData);
                currentSource = stopNaNTarget;
            }

            if(useSubPixelMorpAA)
            {
                RenderSMAA(renderGraph, in currentSource, out var SMAATarget, ref renderingData);
                currentSource = SMAATarget;
            }

            // Depth of Field
            // Adreno 3xx SystemInfo.graphicsShaderLevel is 35, but instancing support is disabled due to buggy drivers.
            // DOF shader uses #pragma target 3.5 which adds requirement for instancing support, thus marking the shader unsupported on those devices.
            if (useDepthOfField)
            {
                RenderDoF(renderGraph, in currentSource, out var DoFTarget, ref renderingData);
                currentSource = DoFTarget;
            }

            // Temporal Anti Aliasing
            if (useTemporalAA)
            {
                RenderTemporalAA(renderGraph, ref currentSource, out var TemporalAATarget, ref renderingData.cameraData);
                currentSource = TemporalAATarget;
            }

            if(useMotionBlur)
            {
                RenderMotionBlur(renderGraph, in currentSource, out var MotionBlurTarget, ref renderingData.cameraData);
                currentSource = MotionBlurTarget;
            }

            if(usePaniniProjection)
            {
                RenderPaniniProjection(renderGraph, in currentSource, out var PaniniTarget, ref renderingData);
                currentSource = PaniniTarget;
            }

            // Uberpost
            {
                // Reset uber keywords
                m_Materials.uber.shaderKeywords = null;

                // Bloom goes first
                bool bloomActive = m_Bloom.IsActive();
                //Even if bloom is not active we need the texture if the lensFlareScreenSpace pass is active.
                if (bloomActive || useLensFlareScreenSpace)
                {
                    RenderBloomTexture(renderGraph, currentSource, out var BloomTexture, ref renderingData);

                    if (useLensFlareScreenSpace)
                    {
                        int maxBloomMip = Mathf.Clamp(m_LensFlareScreenSpace.bloomMip.value, 0, m_Bloom.maxIterations.value/2);
                        BloomTexture = RenderLensFlareScreenSpace(renderGraph, in currentSource, ref renderingData, _BloomMipUp[maxBloomMip]);
                    }

                    UberPostSetupBloomPass(renderGraph, in BloomTexture, m_Materials.uber);
                }

                if (useLensFlare)
                {
                    LensFlareDataDrivenComputeOcclusion(renderGraph, ref renderingData);
                    RenderLensFlareDataDriven(renderGraph, in currentSource, ref renderingData);
                }

                // TODO RENDERGRAPH: Once we started removing the non-RG code pass in URP, we should move functions below to renderfunc so that material setup happens at
                // the same timeline of executing the rendergraph. Keep them here for now so we cound reuse non-RG code to reduce maintainance cost.
                SetupLensDistortion(m_Materials.uber, isSceneViewCamera);
                SetupChromaticAberration(m_Materials.uber);
                SetupVignette(m_Materials.uber, cameraData.xr);
                SetupGrain(ref cameraData, m_Materials.uber);
                SetupDithering(ref cameraData, m_Materials.uber);

                if (RequireSRGBConversionBlitToBackBuffer(ref cameraData))
                    m_Materials.uber.EnableKeyword(ShaderKeywordStrings.LinearToSRGBConversion);

                if (m_UseFastSRGBLinearConversion)
                {
                    m_Materials.uber.EnableKeyword(ShaderKeywordStrings.UseFastSRGBLinearConversion);
                }

                if (isFsrEnabled)
                {
                    m_Materials.uber.EnableKeyword(ShaderKeywordStrings.Gamma20);
                }

                GetActiveDebugHandler(ref renderingData)?.UpdateShaderGlobalPropertiesForFinalValidationPass(renderingData.commandBuffer, ref cameraData, !m_HasFinalPass);

                RenderUberPost(renderGraph, in currentSource, in postProcessingTarget, in lutTexture, ref renderingData);
            }
        }
    }
}
