using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using System;
using UnityEngine.Rendering.Universal.Internal;

namespace UnityEngine.Rendering.Universal
{
    internal partial class PostProcessPass : ScriptableRenderPass
    {
        static readonly int s_CameraDepthTextureID = Shader.PropertyToID("_CameraDepthTexture");

        private class UpdateCameraResolutionPassData
        {
            internal Vector2Int newCameraTargetSize;
        }

        // Updates render target descriptors and shader constants to reflect a new render size
        // This should be called immediately after the resolution changes mid-frame (typically after an upscaling operation).
        void UpdateCameraResolution(RenderGraph renderGraph, UniversalCameraData cameraData, Vector2Int newCameraTargetSize)
        {
            // Update the camera data descriptor to reflect post-upscaled sizes
            cameraData.cameraTargetDescriptor.width = newCameraTargetSize.x;
            cameraData.cameraTargetDescriptor.height = newCameraTargetSize.y;

            // Update the shader constants to reflect the new camera resolution
            using (var builder = renderGraph.AddUnsafePass<UpdateCameraResolutionPassData>("Update Camera Resolution", out var passData))
            {
                passData.newCameraTargetSize = newCameraTargetSize;

                // This pass only modifies shader constants
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc(static (UpdateCameraResolutionPassData data, UnsafeGraphContext ctx) =>
                {
                    ctx.cmd.SetGlobalVector(
                        ShaderPropertyId.screenSize,
                        new Vector4(
                            data.newCameraTargetSize.x,
                            data.newCameraTargetSize.y,
                            1.0f / data.newCameraTargetSize.x,
                            1.0f / data.newCameraTargetSize.y
                        )
                    );
                });
            }
        }

        internal static TextureHandle CreateCompatibleTexture(RenderGraph renderGraph, in TextureHandle source, string name, bool clear, FilterMode filterMode)
        {
            var desc = source.GetDescriptor(renderGraph);
            MakeCompatible(ref desc);
            desc.name = name;
            desc.clearBuffer = clear;
            desc.filterMode = filterMode;
            return renderGraph.CreateTexture(desc);
        }

        internal static TextureHandle CreateCompatibleTexture(RenderGraph renderGraph, in TextureDesc desc, string name, bool clear, FilterMode filterMode)
        {
            var descCompatible = GetCompatibleDescriptor(desc);
            descCompatible.name = name;
            descCompatible.clearBuffer = clear;
            descCompatible.filterMode = filterMode;
            return renderGraph.CreateTexture(descCompatible);
        }

        internal static TextureDesc GetCompatibleDescriptor(TextureDesc desc, int width, int height, GraphicsFormat format)
        {
            desc.width = width;
            desc.height = height;
            desc.format = format;

            MakeCompatible(ref desc);

            return desc;
        }

        internal static TextureDesc GetCompatibleDescriptor(TextureDesc desc)
        {
            MakeCompatible(ref desc);

            return desc;
        }

        internal static void MakeCompatible(ref TextureDesc desc)
        {
            desc.msaaSamples = MSAASamples.None;
            desc.useMipMap = false;
            desc.autoGenerateMips = false;
            desc.anisoLevel = 0;
            desc.discardBuffer = false;
        }

        #region StopNaNs
        private class StopNaNsPassData
        {
            internal TextureHandle stopNaNTarget;
            internal TextureHandle sourceTexture;
            internal Material stopNaN;
        }

        public void RenderStopNaN(RenderGraph renderGraph, in TextureHandle activeCameraColor, out TextureHandle stopNaNTarget)
        {
            stopNaNTarget = CreateCompatibleTexture(renderGraph, activeCameraColor, "_StopNaNsTarget", true, FilterMode.Bilinear);

            using (var builder = renderGraph.AddRasterRenderPass<StopNaNsPassData>("Stop NaNs", out var passData,
                       ProfilingSampler.Get(URPProfileId.RG_StopNaNs)))
            {
                passData.stopNaNTarget = stopNaNTarget;
                builder.SetRenderAttachment(stopNaNTarget, 0, AccessFlags.ReadWrite);
                passData.sourceTexture = activeCameraColor;
                builder.UseTexture(activeCameraColor, AccessFlags.Read);
                passData.stopNaN = m_Materials.stopNaN;
                builder.SetRenderFunc(static (StopNaNsPassData data, RasterGraphContext context) =>
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
            internal TextureHandle sourceTexture;
            internal TextureHandle depthStencilTexture;
            internal TextureHandle blendTexture;
            internal Material material;
        }

        public void RenderSMAA(RenderGraph renderGraph, UniversalResourceData resourceData, AntialiasingQuality antialiasingQuality, in TextureHandle source, out TextureHandle SMAATarget)
        {
            var destDesc = renderGraph.GetTextureDesc(source);

            SMAATarget = CreateCompatibleTexture(renderGraph, destDesc, "_SMAATarget", true, FilterMode.Bilinear);

            destDesc.clearColor = Color.black;
            destDesc.clearColor.a = 0.0f;

            var edgeTextureDesc = destDesc;
            edgeTextureDesc.format = m_SMAAEdgeFormat;
            var edgeTexture = CreateCompatibleTexture(renderGraph, edgeTextureDesc, "_EdgeStencilTexture", true, FilterMode.Bilinear);

            var edgeTextureStencilDesc = destDesc;
            edgeTextureStencilDesc.format = GraphicsFormatUtility.GetDepthStencilFormat(24);
            var edgeTextureStencil = CreateCompatibleTexture(renderGraph, edgeTextureStencilDesc, "_EdgeTexture", true, FilterMode.Bilinear);

            var blendTextureDesc = destDesc;
            blendTextureDesc.format = GraphicsFormat.R8G8B8A8_UNorm;
            var blendTexture = CreateCompatibleTexture(renderGraph, blendTextureDesc, "_BlendTexture", true, FilterMode.Point);

            // Anti-aliasing
            var material = m_Materials.subpixelMorphologicalAntialiasing;

            using (var builder = renderGraph.AddRasterRenderPass<SMAASetupPassData>("SMAA Material Setup", out var passData, ProfilingSampler.Get(URPProfileId.RG_SMAAMaterialSetup)))
            {
                const int kStencilBit = 64;
                // TODO RENDERGRAPH: handle dynamic scaling
                passData.metrics = new Vector4(1f / destDesc.width, 1f / destDesc.height, destDesc.width, destDesc.height);
                passData.areaTexture = m_Data.textures.smaaAreaTex;
                passData.searchTexture = m_Data.textures.smaaSearchTex;
                passData.stencilRef = (float)kStencilBit;
                passData.stencilMask = (float)kStencilBit;
                passData.antialiasingQuality = antialiasingQuality;
                passData.material = material;

                builder.AllowPassCulling(false);

                builder.SetRenderFunc(static (SMAASetupPassData data, RasterGraphContext context) =>
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
                builder.SetRenderAttachment(edgeTexture, 0, AccessFlags.Write);
                passData.depthStencilTexture = edgeTextureStencil;
                builder.SetRenderAttachmentDepth(edgeTextureStencil, AccessFlags.Write);
                passData.sourceTexture = source;
                builder.UseTexture(source, AccessFlags.Read);
                builder.UseTexture(resourceData.cameraDepth ,AccessFlags.Read);
                passData.material = material;

                builder.SetRenderFunc(static (SMAAPassData data, RasterGraphContext context) =>
                {
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
                builder.SetRenderAttachment(blendTexture, 0, AccessFlags.Write);
                passData.depthStencilTexture = edgeTextureStencil;
                builder.SetRenderAttachmentDepth(edgeTextureStencil, AccessFlags.Read);
                passData.sourceTexture = edgeTexture;
                builder.UseTexture(edgeTexture, AccessFlags.Read);
                passData.material = material;

                builder.SetRenderFunc(static (SMAAPassData data, RasterGraphContext context) =>
                {
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
                builder.SetRenderAttachment(SMAATarget, 0, AccessFlags.Write);
                passData.sourceTexture = source;
                builder.UseTexture(source, AccessFlags.Read);
                passData.blendTexture = blendTexture;
                builder.UseTexture(blendTexture, AccessFlags.Read);
                passData.material = material;

                builder.SetRenderFunc(static (SMAAPassData data, RasterGraphContext context) =>
                {
                    var SMAAMaterial = data.material;
                    var cmd = context.cmd;
                    RTHandle sourceTextureHdl = data.sourceTexture;

                    // Pass 3: Neighborhood blending
                    SMAAMaterial.SetTexture(ShaderConstants._BlendTexture, data.blendTexture);
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
            internal Vector4 dirtScaleOffset;
            internal float dirtIntensity;
            internal Texture dirtTexture;
            internal bool highQualityFilteringValue;
            internal TextureHandle bloomTexture;
            internal Material uberMaterial;
        }

        public void UberPostSetupBloomPass(RenderGraph rendergraph, Material uberMaterial, in TextureDesc srcDesc)
        {
            using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_UberPostSetupBloomPass)))
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
                float screenRatio = srcDesc.width / (float)srcDesc.height;
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

                var highQualityFilteringValue = m_Bloom.highQualityFiltering.value;

                uberMaterial.SetVector(ShaderConstants._Bloom_Params, bloomParams);
                uberMaterial.SetVector(ShaderConstants._LensDirt_Params, dirtScaleOffset);
                uberMaterial.SetFloat(ShaderConstants._LensDirt_Intensity, dirtIntensity);
                uberMaterial.SetTexture(ShaderConstants._LensDirt_Texture, dirtTexture);

                // Keyword setup - a bit convoluted as we're trying to save some variants in Uber...
                if (highQualityFilteringValue)
                    uberMaterial.EnableKeyword(dirtIntensity > 0f ? ShaderKeywordStrings.BloomHQDirt : ShaderKeywordStrings.BloomHQ);
                else
                    uberMaterial.EnableKeyword(dirtIntensity > 0f ? ShaderKeywordStrings.BloomLQDirt : ShaderKeywordStrings.BloomLQ);
            }
        }

        private class BloomPassData
        {
            internal int mipCount;

            internal Material material;
            internal Material[] upsampleMaterials;

            internal TextureHandle sourceTexture;

            internal TextureHandle[] bloomMipUp;
            internal TextureHandle[] bloomMipDown;
        }

        internal struct BloomMaterialParams
        {
            internal Vector4 parameters;
            internal bool highQualityFiltering;
            internal bool enableAlphaOutput;

            internal bool Equals(ref BloomMaterialParams other)
            {
                return parameters == other.parameters &&
                       highQualityFiltering == other.highQualityFiltering &&
                       enableAlphaOutput == other.enableAlphaOutput;
            }
        }

        public void RenderBloomTexture(RenderGraph renderGraph, in TextureHandle source, out TextureHandle destination, bool enableAlphaOutput)
        {
            var srcDesc = source.GetDescriptor(renderGraph);

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

            //We should set the limit the downres result to ensure we dont turn 1x1 textures, which should technically be valid
            //into 0x0 textures which will be invalid
            int tw = Mathf.Max(1, srcDesc.width >> downres);
            int th = Mathf.Max(1, srcDesc.height >> downres);

            // Determine the iteration count
            int maxSize = Mathf.Max(tw, th);
            int iterations = Mathf.FloorToInt(Mathf.Log(maxSize, 2f) - 1);
            int mipCount = Mathf.Clamp(iterations, 1, m_Bloom.maxIterations.value);

            // Setup
            using(new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_BloomSetup)))
            {
                // Pre-filtering parameters
                float clamp = m_Bloom.clamp.value;
                float threshold = Mathf.GammaToLinearSpace(m_Bloom.threshold.value);
                float thresholdKnee = threshold * 0.5f; // Hardcoded soft knee

                // Material setup
                float scatter = Mathf.Lerp(0.05f, 0.95f, m_Bloom.scatter.value);

                BloomMaterialParams bloomParams = new BloomMaterialParams();
                bloomParams.parameters = new Vector4(scatter, clamp, threshold, thresholdKnee);
                bloomParams.highQualityFiltering = m_Bloom.highQualityFiltering.value;
                bloomParams.enableAlphaOutput = enableAlphaOutput;

                // Setting keywords can be somewhat expensive on low-end platforms.
                // Previous params are cached to avoid setting the same keywords every frame.
                var material = m_Materials.bloom;
                bool bloomParamsDirty = !m_BloomParamsPrev.Equals(ref bloomParams);
                bool isParamsPropertySet = material.HasProperty(ShaderConstants._Params);
                if (bloomParamsDirty || !isParamsPropertySet)
                {
                    material.SetVector(ShaderConstants._Params, bloomParams.parameters);
                    CoreUtils.SetKeyword(material, ShaderKeywordStrings.BloomHQ, bloomParams.highQualityFiltering);
                    CoreUtils.SetKeyword(material, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, bloomParams.enableAlphaOutput);

                    // These materials are duplicate just to allow different bloom blits to use different textures.
                    for (uint i = 0; i < k_MaxPyramidSize; ++i)
                    {
                        var materialPyramid = m_Materials.bloomUpsample[i];
                        materialPyramid.SetVector(ShaderConstants._Params, bloomParams.parameters);
                        CoreUtils.SetKeyword(materialPyramid, ShaderKeywordStrings.BloomHQ, bloomParams.highQualityFiltering);
                        CoreUtils.SetKeyword(materialPyramid, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, bloomParams.enableAlphaOutput);
                    }

                    m_BloomParamsPrev = bloomParams;
                }

                // Create bloom mip pyramid textures
                {
                    var desc = GetCompatibleDescriptor(srcDesc, tw, th, m_DefaultColorFormat);
                    _BloomMipDown[0] = CreateCompatibleTexture(renderGraph, desc, m_BloomMipDownName[0], false, FilterMode.Bilinear);
                    _BloomMipUp[0] = CreateCompatibleTexture(renderGraph, desc, m_BloomMipUpName[0], false, FilterMode.Bilinear);

                    for (int i = 1; i < mipCount; i++)
                    {
                        tw = Mathf.Max(1, tw >> 1);
                        th = Mathf.Max(1, th >> 1);
                        ref TextureHandle mipDown = ref _BloomMipDown[i];
                        ref TextureHandle mipUp = ref _BloomMipUp[i];

                        desc.width = tw;
                        desc.height = th;

                        mipDown = CreateCompatibleTexture(renderGraph, desc, m_BloomMipDownName[i], false, FilterMode.Bilinear);
                        mipUp = CreateCompatibleTexture(renderGraph, desc, m_BloomMipUpName[i], false, FilterMode.Bilinear);
                    }
                }
            }

            using (var builder = renderGraph.AddUnsafePass<BloomPassData>("Blit Bloom Mipmaps", out var passData, ProfilingSampler.Get(URPProfileId.Bloom)))
            {
                passData.mipCount = mipCount;
                passData.material = m_Materials.bloom;
                passData.upsampleMaterials = m_Materials.bloomUpsample;
                passData.sourceTexture = source;
                passData.bloomMipDown = _BloomMipDown;
                passData.bloomMipUp = _BloomMipUp;

                // TODO RENDERGRAPH: properly setup dependencies between passes
                builder.AllowPassCulling(false);

                builder.UseTexture(source, AccessFlags.Read);
                for (int i = 0; i < mipCount; i++)
                {
                    builder.UseTexture(_BloomMipDown[i], AccessFlags.ReadWrite);
                    builder.UseTexture(_BloomMipUp[i], AccessFlags.ReadWrite);
                }

                builder.SetRenderFunc(static (BloomPassData data, UnsafeGraphContext context) =>
                {
                    // TODO: can't call BlitTexture with unsafe command buffer
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
                    var material = data.material;
                    int mipCount = data.mipCount;

                    var loadAction = RenderBufferLoadAction.DontCare;   // Blit - always write all pixels
                    var storeAction = RenderBufferStoreAction.Store;    // Blit - always read by then next Blit

                    // Prefilter
                    using(new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.RG_BloomPrefilter)))
                    {
                        Blitter.BlitCameraTexture(cmd, data.sourceTexture, data.bloomMipDown[0], loadAction, storeAction, material, 0);
                    }

                    // Downsample - gaussian pyramid
                    // Classic two pass gaussian blur - use mipUp as a temporary target
                    //   First pass does 2x downsampling + 9-tap gaussian
                    //   Second pass does 9-tap gaussian using a 5-tap filter + bilinear filtering
                    using(new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.RG_BloomDownsample)))
                    {
                        TextureHandle lastDown = data.bloomMipDown[0];
                        for (int i = 1; i < mipCount; i++)
                        {
                            TextureHandle mipDown = data.bloomMipDown[i];
                            TextureHandle mipUp = data.bloomMipUp[i];

                            Blitter.BlitCameraTexture(cmd, lastDown, mipUp, loadAction, storeAction, material, 1);
                            Blitter.BlitCameraTexture(cmd, mipUp, mipDown, loadAction, storeAction, material, 2);

                            lastDown = mipDown;
                        }
                    }

                    using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.RG_BloomUpsample)))
                    {
                        // Upsample (bilinear by default, HQ filtering does bicubic instead
                        for (int i = mipCount - 2; i >= 0; i--)
                        {
                            TextureHandle lowMip = (i == mipCount - 2) ? data.bloomMipDown[i + 1] : data.bloomMipUp[i + 1];
                            TextureHandle highMip = data.bloomMipDown[i];
                            TextureHandle dst = data.bloomMipUp[i];

                            // We need a separate material for each upsample pass because setting the low texture mip source
                            // gets overriden by the time the render func is executed.
                            // Material is a reference, so all the blits would share the same material state in the cmdbuf.
                            // NOTE: another option would be to use cmd.SetGlobalTexture().
                            var upMaterial = data.upsampleMaterials[i];
                            upMaterial.SetTexture(ShaderConstants._SourceTexLowMip, lowMip);

                            Blitter.BlitCameraTexture(cmd, highMip, dst, loadAction, storeAction, upMaterial, 3);
                        }
                    }
                });

                destination = passData.bloomMipUp[0];
            }
        }
        #endregion

        #region DoF
        public void RenderDoF(RenderGraph renderGraph, UniversalResourceData resourceData, UniversalCameraData cameraData, in TextureHandle source, out TextureHandle destination)
        {
            var dofMaterial = m_DepthOfField.mode.value == DepthOfFieldMode.Gaussian ? m_Materials.gaussianDepthOfField : m_Materials.bokehDepthOfField;

            destination = CreateCompatibleTexture(renderGraph, source, "_DoFTarget", true, FilterMode.Bilinear);

            CoreUtils.SetKeyword(dofMaterial, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, cameraData.isAlphaOutputEnabled);

            if (m_DepthOfField.mode.value == DepthOfFieldMode.Gaussian)
            {
                RenderDoFGaussian(renderGraph, resourceData, cameraData, source, destination, ref dofMaterial);
            }
            else if (m_DepthOfField.mode.value == DepthOfFieldMode.Bokeh)
            {
                RenderDoFBokeh(renderGraph, resourceData, cameraData, source, destination, ref dofMaterial);
            }
        }

        private class DoFGaussianPassData
        {
            // Setup
            internal int downsample;
            internal RenderingData renderingData;
            internal Vector3 cocParams;
            internal bool highQualitySamplingValue;
            // Inputs
            internal TextureHandle sourceTexture;
            internal TextureHandle depthTexture;
            internal Material material;
            internal Material materialCoC;
            // Pass textures
            internal TextureHandle halfCoCTexture;
            internal TextureHandle fullCoCTexture;
            internal TextureHandle pingTexture;
            internal TextureHandle pongTexture;
            internal RenderTargetIdentifier[] multipleRenderTargets = new RenderTargetIdentifier[2];
            // Output textures
            internal TextureHandle destination;
        };

        public void RenderDoFGaussian(RenderGraph renderGraph, UniversalResourceData resourceData, UniversalCameraData cameraData, in TextureHandle source, TextureHandle destination, ref Material dofMaterial)
        {
            var srcDesc = source.GetDescriptor(renderGraph);

            var material = dofMaterial;
            int downSample = 2;
            int wh = srcDesc.width / downSample;
            int hh = srcDesc.height / downSample;

            // Pass Textures
            var fullCoCTextureDesc = GetCompatibleDescriptor(srcDesc, srcDesc.width, srcDesc.height, m_GaussianCoCFormat);
            var fullCoCTexture = CreateCompatibleTexture(renderGraph, fullCoCTextureDesc, "_FullCoCTexture", true, FilterMode.Bilinear);
            var halfCoCTextureDesc = GetCompatibleDescriptor(srcDesc, wh, hh, m_GaussianCoCFormat);
            var halfCoCTexture = CreateCompatibleTexture(renderGraph, halfCoCTextureDesc, "_HalfCoCTexture", true, FilterMode.Bilinear);
            var pingTextureDesc = GetCompatibleDescriptor(srcDesc, wh, hh, m_DefaultColorFormat);
            var pingTexture = CreateCompatibleTexture(renderGraph, pingTextureDesc, "_PingTexture", true, FilterMode.Bilinear);
            var pongTextureDesc = GetCompatibleDescriptor(srcDesc, wh, hh, m_DefaultColorFormat);
            var pongTexture = CreateCompatibleTexture(renderGraph, pongTextureDesc, "_PongTexture", true, FilterMode.Bilinear);

            using (var builder = renderGraph.AddUnsafePass<DoFGaussianPassData>("Depth of Field - Gaussian", out var passData))
            {
                // Setup
                float farStart = m_DepthOfField.gaussianStart.value;
                float farEnd = Mathf.Max(farStart, m_DepthOfField.gaussianEnd.value);

                // Assumes a radius of 1 is 1 at 1080p
                // Past a certain radius our gaussian kernel will look very bad so we'll clamp it for
                // very high resolutions (4K+).
                float maxRadius = m_DepthOfField.gaussianMaxRadius.value * (wh / 1080f);
                maxRadius = Mathf.Min(maxRadius, 2f);

                passData.downsample = downSample;
                passData.cocParams = new Vector3(farStart, farEnd, maxRadius);
                passData.highQualitySamplingValue = m_DepthOfField.highQualitySampling.value;

                passData.material = material;
                passData.materialCoC = m_Materials.gaussianDepthOfFieldCoC;

                // Inputs
                passData.sourceTexture = source;
                builder.UseTexture(source, AccessFlags.Read);

                passData.depthTexture = resourceData.cameraDepthTexture;
                builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);

                // Pass Textures
                passData.fullCoCTexture = fullCoCTexture;
                builder.UseTexture(fullCoCTexture, AccessFlags.ReadWrite);

                passData.halfCoCTexture = halfCoCTexture;
                builder.UseTexture(halfCoCTexture, AccessFlags.ReadWrite);

                passData.pingTexture = pingTexture;
                builder.UseTexture(pingTexture, AccessFlags.ReadWrite);

                passData.pongTexture = pongTexture;
                builder.UseTexture(pongTexture, AccessFlags.ReadWrite);

                // Outputs
                passData.destination = destination;
                builder.UseTexture(destination, AccessFlags.Write);

                builder.SetRenderFunc(static (DoFGaussianPassData data, UnsafeGraphContext context) =>
                {
                    var dofMat = data.material;
                    var dofMaterialCoC = data.materialCoC;
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);

                    RTHandle sourceTextureHdl = data.sourceTexture;
                    RTHandle dstHdl = data.destination;

                    // Setup
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_SetupDoF)))
                    {
                        dofMat.SetVector(ShaderConstants._CoCParams, data.cocParams);
                        CoreUtils.SetKeyword(dofMat, ShaderKeywordStrings.HighQualitySampling,
                            data.highQualitySamplingValue);

                        dofMaterialCoC.SetVector(ShaderConstants._CoCParams, data.cocParams);
                        CoreUtils.SetKeyword(dofMaterialCoC, ShaderKeywordStrings.HighQualitySampling,
                            data.highQualitySamplingValue);

                        PostProcessUtils.SetSourceSize(cmd, data.sourceTexture);
                        dofMat.SetVector(ShaderConstants._DownSampleScaleFactor,
                            new Vector4(1.0f / data.downsample, 1.0f / data.downsample, data.downsample,
                                data.downsample));
                    }

                    // Compute CoC
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_DOFComputeCOC)))
                    {
                        dofMat.SetTexture(s_CameraDepthTextureID, data.depthTexture);
                        Blitter.BlitCameraTexture(cmd, data.sourceTexture, data.fullCoCTexture, data.materialCoC, k_GaussianDoFPassComputeCoc);
                    }

                    // Downscale & prefilter color + CoC
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_DOFDownscalePrefilter)))
                    {
                        dofMat.SetTexture(ShaderConstants._FullCoCTexture, data.fullCoCTexture);

                        // Handle packed shader output
                        data.multipleRenderTargets[0] = data.halfCoCTexture;
                        data.multipleRenderTargets[1] = data.pingTexture;
                        CoreUtils.SetRenderTarget(cmd, data.multipleRenderTargets, data.halfCoCTexture);

                        Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                        Blitter.BlitTexture(cmd, data.sourceTexture, viewportScale, dofMat, k_GaussianDoFPassDownscalePrefilter);
                    }

                    // Blur H
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_DOFBlurH)))
                    {
                        dofMat.SetTexture(ShaderConstants._HalfCoCTexture, data.halfCoCTexture);
                        Blitter.BlitCameraTexture(cmd, data.pingTexture, data.pongTexture, dofMat, k_GaussianDoFPassBlurH);
                    }

                    // Blur V
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_DOFBlurV)))
                    {
                        Blitter.BlitCameraTexture(cmd, data.pongTexture, data.pingTexture, dofMat, k_GaussianDoFPassBlurV);
                    }

                    // Composite
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_DOFComposite)))
                    {
                        dofMat.SetTexture(ShaderConstants._ColorTexture, data.pingTexture);
                        dofMat.SetTexture(ShaderConstants._FullCoCTexture, data.fullCoCTexture);
                        Blitter.BlitCameraTexture(cmd, sourceTextureHdl, dstHdl, dofMat, k_GaussianDoFPassComposite);
                    }
                });
            }
        }

        private class DoFBokehPassData
        {
            // Setup
            internal Vector4[] bokehKernel;
            internal int downSample;
            internal float uvMargin;
            internal Vector4 cocParams;
            internal bool useFastSRGBLinearConversion;
            // Inputs
            internal TextureHandle sourceTexture;
            internal TextureHandle depthTexture;
            internal Material material;
            internal Material materialCoC;
            // Pass textures
            internal TextureHandle halfCoCTexture;
            internal TextureHandle fullCoCTexture;
            internal TextureHandle pingTexture;
            internal TextureHandle pongTexture;
            // Output texture
            internal TextureHandle destination;
        };

        public void RenderDoFBokeh(RenderGraph renderGraph, UniversalResourceData resourceData, UniversalCameraData cameraData, in TextureHandle source, in TextureHandle destination, ref Material dofMaterial)
        {
            var srcDesc = source.GetDescriptor(renderGraph);

            int downSample = 2;
            var material = dofMaterial;
            int wh = srcDesc.width / downSample;
            int hh = srcDesc.height / downSample;

            // Pass Textures
            var fullCoCTextureDesc = GetCompatibleDescriptor(srcDesc, srcDesc.width, srcDesc.height, GraphicsFormat.R8_UNorm);
            var fullCoCTexture = CreateCompatibleTexture(renderGraph, fullCoCTextureDesc, "_FullCoCTexture", true, FilterMode.Bilinear);
            var pingTextureDesc = GetCompatibleDescriptor(srcDesc, wh, hh, GraphicsFormat.R16G16B16A16_SFloat);
            var pingTexture = CreateCompatibleTexture(renderGraph, pingTextureDesc, "_PingTexture", true, FilterMode.Bilinear);
            var pongTextureDesc = GetCompatibleDescriptor(srcDesc, wh, hh, GraphicsFormat.R16G16B16A16_SFloat);
            var pongTexture = CreateCompatibleTexture(renderGraph, pongTextureDesc, "_PongTexture", true, FilterMode.Bilinear);

            using (var builder = renderGraph.AddUnsafePass<DoFBokehPassData>("Depth of Field - Bokeh", out var passData))
            {
                // Setup
                // "A Lens and Aperture Camera Model for Synthetic Image Generation" [Potmesil81]
                float F = m_DepthOfField.focalLength.value / 1000f;
                float A = m_DepthOfField.focalLength.value / m_DepthOfField.aperture.value;
                float P = m_DepthOfField.focusDistance.value;
                float maxCoC = (A * F) / (P - F);
                float maxRadius = GetMaxBokehRadiusInPixels(srcDesc.height);
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
                float uvMargin = (1.0f / srcDesc.height) * downSample;

                passData.bokehKernel = m_BokehKernel;
                passData.downSample = downSample;
                passData.uvMargin = uvMargin;
                passData.cocParams = new Vector4(P, maxCoC, maxRadius, rcpAspect);
                passData.useFastSRGBLinearConversion = m_UseFastSRGBLinearConversion;

                // Inputs
                passData.sourceTexture = source;
                builder.UseTexture(source, AccessFlags.Read);

                passData.depthTexture = resourceData.cameraDepthTexture;
                builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);

                passData.material = material;
                passData.materialCoC = m_Materials.bokehDepthOfFieldCoC;

                // Pass Textures
                passData.fullCoCTexture = fullCoCTexture;
                builder.UseTexture(fullCoCTexture, AccessFlags.ReadWrite);
                passData.pingTexture = pingTexture;
                builder.UseTexture(pingTexture, AccessFlags.ReadWrite);
                passData.pongTexture = pongTexture;
                builder.UseTexture(pongTexture, AccessFlags.ReadWrite);

                // Outputs
                passData.destination = destination;
                builder.UseTexture(destination, AccessFlags.Write);

                // TODO RENDERGRAPH: properly setup dependencies between passes
                builder.SetRenderFunc(static (DoFBokehPassData data, UnsafeGraphContext context) =>
                {
                    var dofMat = data.material;
                    var dofMaterialCoC = data.materialCoC;
                    var cmd = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
                    RTHandle sourceTextureHdl = data.sourceTexture;
                    RTHandle dst = data.destination;

                    // Setup
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_SetupDoF)))
                    {
                        CoreUtils.SetKeyword(dofMat, ShaderKeywordStrings.UseFastSRGBLinearConversion,
                            data.useFastSRGBLinearConversion);
                        CoreUtils.SetKeyword(dofMaterialCoC, ShaderKeywordStrings.UseFastSRGBLinearConversion,
                            data.useFastSRGBLinearConversion);

                        dofMat.SetVector(ShaderConstants._CoCParams, data.cocParams);
                        dofMat.SetVectorArray(ShaderConstants._BokehKernel, data.bokehKernel);
                        dofMat.SetVector(ShaderConstants._DownSampleScaleFactor,
                            new Vector4(1.0f / data.downSample, 1.0f / data.downSample, data.downSample,
                                data.downSample));
                        dofMat.SetVector(ShaderConstants._BokehConstants,
                            new Vector4(data.uvMargin, data.uvMargin * 2.0f));
                        PostProcessUtils.SetSourceSize(cmd, data.sourceTexture);
                    }

                    // Compute CoC
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_DOFComputeCOC)))
                    {
                        dofMat.SetTexture(s_CameraDepthTextureID, data.depthTexture);
                        Blitter.BlitCameraTexture(cmd, sourceTextureHdl, data.fullCoCTexture, dofMat, k_BokehDoFPassComputeCoc);
                    }

                    // Downscale and Prefilter Color + CoC
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_DOFDownscalePrefilter)))
                    {
                        dofMat.SetTexture(ShaderConstants._FullCoCTexture, data.fullCoCTexture);
                        Blitter.BlitCameraTexture(cmd, sourceTextureHdl, data.pingTexture, dofMat, k_BokehDoFPassDownscalePrefilter);
                    }

                    // Blur
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_DOFBlurBokeh)))
                    {
                        Blitter.BlitCameraTexture(cmd, data.pingTexture, data.pongTexture, dofMat, k_BokehDoFPassBlur);
                    }

                    // Post Filtering
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_DOFPostFilter)))
                    {
                        Blitter.BlitCameraTexture(cmd, data.pongTexture, data.pingTexture, dofMat, k_BokehDoFPassPostFilter);
                    }

                    // Composite
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_DOFComposite)))
                    {
                        dofMat.SetTexture(ShaderConstants._DofTexture, data.pingTexture);
                        Blitter.BlitCameraTexture(cmd, sourceTextureHdl, dst, dofMat, k_BokehDoFPassComposite);
                    }
                });
            }
        }
        #endregion

        #region Panini
        private class PaniniProjectionPassData
        {
            internal TextureHandle destinationTexture;
            internal TextureHandle sourceTexture;
            internal Material material;
            internal Vector4 paniniParams;
            internal bool isPaniniGeneric;
        }

        public void RenderPaniniProjection(RenderGraph renderGraph, Camera camera, in TextureHandle source, out TextureHandle destination)
        {
            destination = CreateCompatibleTexture(renderGraph, source, "_PaniniProjectionTarget", true, FilterMode.Bilinear);

            // Use source width/height for aspect ratio which can be different from camera aspect. (e.g. viewport)
            var desc = source.GetDescriptor(renderGraph);
            float distance = m_PaniniProjection.distance.value;
            var viewExtents = CalcViewExtents(camera, desc.width, desc.height);
            var cropExtents = CalcCropExtents(camera, distance, desc.width, desc.height);

            float scaleX = cropExtents.x / viewExtents.x;
            float scaleY = cropExtents.y / viewExtents.y;
            float scaleF = Mathf.Min(scaleX, scaleY);

            float paniniD = distance;
            float paniniS = Mathf.Lerp(1f, Mathf.Clamp01(scaleF), m_PaniniProjection.cropToFit.value);

            using (var builder = renderGraph.AddRasterRenderPass<PaniniProjectionPassData>("Panini Projection", out var passData, ProfilingSampler.Get(URPProfileId.PaniniProjection)))
            {
                builder.AllowGlobalStateModification(true);
                passData.destinationTexture = destination;
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                passData.sourceTexture = source;
                builder.UseTexture(source, AccessFlags.Read);
                passData.material = m_Materials.paniniProjection;
                passData.paniniParams = new Vector4(viewExtents.x, viewExtents.y, paniniD, paniniS);
                passData.isPaniniGeneric = 1f - Mathf.Abs(paniniD) > float.Epsilon;

                builder.SetRenderFunc(static (PaniniProjectionPassData data, RasterGraphContext context) =>
                {
                    var cmd = context.cmd;
                    RTHandle sourceTextureHdl = data.sourceTexture;

                    cmd.SetGlobalVector(ShaderConstants._Params, data.paniniParams);
                    data.material.EnableKeyword(data.isPaniniGeneric ? ShaderKeywordStrings.PaniniGeneric : ShaderKeywordStrings.PaniniUnitDistance);

                    Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(cmd, sourceTextureHdl, viewportScale, data.material, 0);
                });

                return;
            }
        }
        #endregion

        #region TemporalAA

        private const string _TemporalAATargetName = "_TemporalAATarget";
        private void RenderTemporalAA(RenderGraph renderGraph, UniversalResourceData resourceData, UniversalCameraData cameraData, ref TextureHandle source, out TextureHandle destination)
        {
            destination = CreateCompatibleTexture(renderGraph, source, _TemporalAATargetName, false, FilterMode.Bilinear);

            TextureHandle cameraDepth = resourceData.cameraDepth;
            TextureHandle motionVectors = resourceData.motionVectorColor;

            Debug.Assert(motionVectors.IsValid(), "MotionVectors are invalid. TAA requires a motion vector texture.");

            TemporalAA.Render(renderGraph, m_Materials.temporalAntialiasing, cameraData, ref source, ref cameraDepth, ref motionVectors, ref destination);
        }
        #endregion

        #region STP

        private const string _UpscaledColorTargetName = "_CameraColorUpscaledSTP";

        private void RenderSTP(RenderGraph renderGraph, UniversalResourceData resourceData, UniversalCameraData cameraData, ref TextureHandle source, out TextureHandle destination)
        {
            TextureHandle cameraDepth = resourceData.cameraDepthTexture;
            TextureHandle motionVectors = resourceData.motionVectorColor;

            Debug.Assert(motionVectors.IsValid(), "MotionVectors are invalid. STP requires a motion vector texture.");

            var srcDesc = source.GetDescriptor(renderGraph);

            var destDesc = GetCompatibleDescriptor(srcDesc,
                cameraData.pixelWidth,
                cameraData.pixelHeight,
                // Avoid enabling sRGB because STP works with compute shaders which can't output sRGB automatically.
                GraphicsFormatUtility.GetLinearFormat(srcDesc.format));

            // STP uses compute shaders so all render textures must enable random writes
            destDesc.enableRandomWrite = true;

            destination = CreateCompatibleTexture(renderGraph, destDesc, _UpscaledColorTargetName, false, FilterMode.Bilinear);

            int frameIndex = Time.frameCount;
            var noiseTexture = m_Data.textures.blueNoise16LTex[frameIndex & (m_Data.textures.blueNoise16LTex.Length - 1)];

            StpUtils.Execute(renderGraph, resourceData, cameraData, source, cameraDepth, motionVectors, destination, noiseTexture);

            // Update the camera resolution to reflect the upscaled size
            UpdateCameraResolution(renderGraph, cameraData, new Vector2Int(destDesc.width, destDesc.height));
        }
        #endregion

        #region MotionBlur
        private class MotionBlurPassData
        {
            internal TextureHandle sourceTexture;
            internal TextureHandle motionVectors;
            internal Material material;
            internal int passIndex;
            internal Camera camera;
            internal XRPass xr;
            internal float intensity;
            internal float clamp;
            internal bool enableAlphaOutput;
        }

        public void RenderMotionBlur(RenderGraph renderGraph, UniversalResourceData resourceData, UniversalCameraData cameraData, in TextureHandle source, out TextureHandle destination)
        {
            var material = m_Materials.cameraMotionBlur;

            destination = CreateCompatibleTexture(renderGraph, source, "_MotionBlurTarget", true, FilterMode.Bilinear);

            TextureHandle motionVectorColor = resourceData.motionVectorColor;
            TextureHandle cameraDepthTexture = resourceData.cameraDepthTexture;

            var mode = m_MotionBlur.mode.value;
            int passIndex = (int)m_MotionBlur.quality.value;
            passIndex += (mode == MotionBlurMode.CameraAndObjects) ? 3 : 0;

            using (var builder = renderGraph.AddRasterRenderPass<MotionBlurPassData>("Motion Blur", out var passData, ProfilingSampler.Get(URPProfileId.RG_MotionBlur)))
            {
                builder.AllowGlobalStateModification(true);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                passData.sourceTexture = source;
                builder.UseTexture(source, AccessFlags.Read);

                if (mode == MotionBlurMode.CameraAndObjects)
                {
                    Debug.Assert(ScriptableRenderer.current.SupportsMotionVectors(), "Current renderer does not support motion vectors.");
                    Debug.Assert(motionVectorColor.IsValid(), "Motion vectors are invalid. Per-object motion blur requires a motion vector texture.");

                    passData.motionVectors = motionVectorColor;
                    builder.UseTexture(motionVectorColor, AccessFlags.Read);
                }
                else
                {
                    passData.motionVectors = TextureHandle.nullHandle;
                }

                Debug.Assert(cameraDepthTexture.IsValid(), "Camera depth texture is invalid. Per-camera motion blur requires a depth texture.");
                builder.UseTexture(cameraDepthTexture, AccessFlags.Read);
                passData.material = material;
                passData.passIndex = passIndex;
                passData.camera = cameraData.camera;
                passData.xr = cameraData.xr;
                passData.enableAlphaOutput = cameraData.isAlphaOutputEnabled;
                passData.intensity = m_MotionBlur.intensity.value;
                passData.clamp = m_MotionBlur.clamp.value;
                builder.SetRenderFunc(static (MotionBlurPassData data, RasterGraphContext context) =>
                {
                    var cmd = context.cmd;
                    RTHandle sourceTextureHdl = data.sourceTexture;

                    UpdateMotionBlurMatrices(ref data.material, data.camera, data.xr);

                    data.material.SetFloat("_Intensity", data.intensity);
                    data.material.SetFloat("_Clamp", data.clamp);
                    CoreUtils.SetKeyword(data.material, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, data.enableAlphaOutput);

                    PostProcessUtils.SetSourceSize(cmd, data.sourceTexture);
                    Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;
                    Blitter.BlitTexture(cmd, sourceTextureHdl, viewportScale, data.material, data.passIndex);
                });

                return;
            }
        }
#endregion

#region LensFlareDataDriven
        private class LensFlarePassData
        {
            internal TextureHandle destinationTexture;
            internal UniversalCameraData cameraData;
            internal Material material;
            internal Rect viewport;
            internal float paniniDistance;
            internal float paniniCropToFit;
            internal float width;
            internal float height;
            internal bool usePanini;
        }

        void LensFlareDataDrivenComputeOcclusion(RenderGraph renderGraph, UniversalResourceData resourceData, UniversalCameraData cameraData, in TextureDesc srcDesc)
        {
            if (!LensFlareCommonSRP.IsOcclusionRTCompatible())
                return;

            using (var builder = renderGraph.AddUnsafePass<LensFlarePassData>("Lens Flare Compute Occlusion", out var passData, ProfilingSampler.Get(URPProfileId.LensFlareDataDrivenComputeOcclusion)))
            {
                RTHandle occH = LensFlareCommonSRP.occlusionRT;
                TextureHandle occlusionHandle = renderGraph.ImportTexture(LensFlareCommonSRP.occlusionRT);
                passData.destinationTexture = occlusionHandle;
                builder.UseTexture(occlusionHandle, AccessFlags.Write);
                passData.cameraData = cameraData;
                passData.viewport = cameraData.pixelRect;
                passData.material = m_Materials.lensFlareDataDriven;
                passData.width = (float)srcDesc.width;
                passData.height = (float)srcDesc.height;
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

                builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);

                builder.SetRenderFunc(
                    static (LensFlarePassData data, UnsafeGraphContext ctx) =>
                    {
                        Camera camera = data.cameraData.camera;
                        XRPass xr = data.cameraData.xr;

                        Matrix4x4 nonJitteredViewProjMatrix0;
                        int xrId0;
#if ENABLE_VR && ENABLE_XR_MODULE
                        // Not VR or Multi-Pass
                        if (xr.enabled)
                        {
                            if (xr.singlePassEnabled)
                            {
                                nonJitteredViewProjMatrix0 = GL.GetGPUProjectionMatrix(data.cameraData.GetProjectionMatrixNoJitter(0), true) * data.cameraData.GetViewMatrix(0);
                                xrId0 = 0;
                            }
                            else
                            {
                                var gpuNonJitteredProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
                                nonJitteredViewProjMatrix0 = gpuNonJitteredProj * camera.worldToCameraMatrix;
                                xrId0 = data.cameraData.xr.multipassId;
                            }
                        }
                        else
                        {
                            nonJitteredViewProjMatrix0 = GL.GetGPUProjectionMatrix(data.cameraData.GetProjectionMatrixNoJitter(0), true) * data.cameraData.GetViewMatrix(0);
                            xrId0 = 0;
                        }
#else
                        var gpuNonJitteredProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
                        nonJitteredViewProjMatrix0 = gpuNonJitteredProj * camera.worldToCameraMatrix;
                        xrId0 = xr.multipassId;
#endif

                        LensFlareCommonSRP.ComputeOcclusion(
                            data.material, camera, xr, xr.multipassId,
                            data.width, data.height,
                            data.usePanini, data.paniniDistance, data.paniniCropToFit, true,
                            camera.transform.position,
                            nonJitteredViewProjMatrix0,
                            ctx.cmd,
                            false, false, null, null);


#if ENABLE_VR && ENABLE_XR_MODULE
                        if (xr.enabled && xr.singlePassEnabled)
                        {
                            //ctx.cmd.SetGlobalTexture(m_Depth.name, m_Depth.nameID);

                            for (int xrIdx = 1; xrIdx < xr.viewCount; ++xrIdx)
                            {
                                Matrix4x4 gpuVPXR = GL.GetGPUProjectionMatrix(data.cameraData.GetProjectionMatrixNoJitter(xrIdx), true) * data.cameraData.GetViewMatrix(xrIdx);

                                // Bypass single pass version
                                LensFlareCommonSRP.ComputeOcclusion(
                                    data.material, camera, xr, xrIdx,
                                    data.width, data.height,
                                    data.usePanini, data.paniniDistance, data.paniniCropToFit, true,
                                    camera.transform.position,
                                    gpuVPXR,
                                    ctx.cmd,
                                    false, false, null, null);
                            }
                        }
#endif
                    });
            }
        }

        public void RenderLensFlareDataDriven(RenderGraph renderGraph, UniversalResourceData resourceData, UniversalCameraData cameraData, in TextureHandle destination, in TextureDesc srcDesc)
        {
            using (var builder = renderGraph.AddUnsafePass<LensFlarePassData>("Lens Flare Data Driven Pass", out var passData, ProfilingSampler.Get(URPProfileId.LensFlareDataDriven)))
            {
                // Use WriteTexture here because DoLensFlareDataDrivenCommon will call SetRenderTarget internally.
                // TODO RENDERGRAPH: convert SRP core lens flare to be rendergraph friendly
                passData.destinationTexture = destination;
                builder.UseTexture(destination, AccessFlags.Write);
                passData.cameraData = cameraData;
                passData.material = m_Materials.lensFlareDataDriven;
                passData.width = (float)srcDesc.width;
                passData.height = (float)srcDesc.height;
                passData.viewport.x = 0.0f;
                passData.viewport.y = 0.0f;
                passData.viewport.width = (float)srcDesc.width;
                passData.viewport.height = (float)srcDesc.height;
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
                    builder.UseTexture(occlusionHandle, AccessFlags.Read);
                }
                else
                {
                    builder.UseTexture(resourceData.cameraDepthTexture, AccessFlags.Read);
                }

                builder.SetRenderFunc(static (LensFlarePassData data, UnsafeGraphContext ctx) =>
                {
                    Camera camera = data.cameraData.camera;
                    XRPass xr = data.cameraData.xr;

#if ENABLE_VR && ENABLE_XR_MODULE
                    // Not VR or Multi-Pass
                    if (!xr.enabled ||
                        (xr.enabled && !xr.singlePassEnabled))
#endif
                    {
                        var gpuNonJitteredProj = GL.GetGPUProjectionMatrix(camera.projectionMatrix, true);
                        Matrix4x4 nonJitteredViewProjMatrix0 = gpuNonJitteredProj * camera.worldToCameraMatrix;

                        LensFlareCommonSRP.DoLensFlareDataDrivenCommon(
                            data.material, data.cameraData.camera, data.viewport, xr, data.cameraData.xr.multipassId,
                            data.width, data.height,
                            data.usePanini, data.paniniDistance, data.paniniCropToFit,
                            true,
                            camera.transform.position,
                            nonJitteredViewProjMatrix0,
                            ctx.cmd,
                            false, false, null, null,
                            data.destinationTexture,
                            (Light light, Camera cam, Vector3 wo) => { return GetLensFlareLightAttenuation(light, cam, wo); },
                            false);
                    }
#if ENABLE_VR && ENABLE_XR_MODULE
                    else
                    {
                        for (int xrIdx = 0; xrIdx < xr.viewCount; ++xrIdx)
                        {
                            Matrix4x4 nonJitteredViewProjMatrix_k = GL.GetGPUProjectionMatrix(data.cameraData.GetProjectionMatrixNoJitter(xrIdx), true) * data.cameraData.GetViewMatrix(xrIdx);

                            LensFlareCommonSRP.DoLensFlareDataDrivenCommon(
                                data.material, data.cameraData.camera, data.viewport, xr, data.cameraData.xr.multipassId,
                                data.width, data.height,
                                data.usePanini, data.paniniDistance, data.paniniCropToFit,
                                true,
                                camera.transform.position,
                                nonJitteredViewProjMatrix_k,
                                ctx.cmd,
                                false, false, null, null,
                                data.destinationTexture,
                                (Light light, Camera cam, Vector3 wo) => { return GetLensFlareLightAttenuation(light, cam, wo); },
                                false);
                        }
                    }
#endif
                });
            }
        }
#endregion

#region LensFlareScreenSpace

        private class LensFlareScreenSpacePassData
        {
            internal TextureHandle streakTmpTexture;
            internal TextureHandle streakTmpTexture2;
            internal TextureHandle originalBloomTexture;
            internal TextureHandle screenSpaceLensFlareBloomMipTexture;
            internal TextureHandle result;
            internal int actualWidth;
            internal int actualHeight;
            internal Camera camera;
            internal Material material;
            internal ScreenSpaceLensFlare lensFlareScreenSpace;
            internal int downsample;
        }

        public TextureHandle RenderLensFlareScreenSpace(RenderGraph renderGraph, Camera camera, in TextureDesc srcDesc, TextureHandle originalBloomTexture, TextureHandle screenSpaceLensFlareBloomMipTexture, bool sameInputOutputTex)
        {
            var downsample = (int) m_LensFlareScreenSpace.resolution.value;

            int flareRenderWidth = Math.Max( srcDesc.width / downsample, 1);
            int flareRenderHeight = Math.Max( srcDesc.height / downsample, 1);

            var streakTextureDesc = GetCompatibleDescriptor(srcDesc, flareRenderWidth, flareRenderHeight, m_DefaultColorFormat);
            var streakTmpTexture = CreateCompatibleTexture(renderGraph, streakTextureDesc, "_StreakTmpTexture", true, FilterMode.Bilinear);
            var streakTmpTexture2 = CreateCompatibleTexture(renderGraph, streakTextureDesc, "_StreakTmpTexture2", true, FilterMode.Bilinear);

            // NOTE: Result texture is the result of the flares/streaks only. Not the final output which is "bloom + flares".
            var resultTexture = CreateCompatibleTexture(renderGraph, streakTextureDesc, "_LensFlareScreenSpace", true, FilterMode.Bilinear);

            using (var builder = renderGraph.AddUnsafePass<LensFlareScreenSpacePassData>("Blit Lens Flare Screen Space", out var passData, ProfilingSampler.Get(URPProfileId.LensFlareScreenSpace)))
            {
                // Use WriteTexture here because DoLensFlareScreenSpaceCommon will call SetRenderTarget internally.
                // TODO RENDERGRAPH: convert SRP core lensflare to be rendergraph friendly
                passData.streakTmpTexture = streakTmpTexture;
                builder.UseTexture(streakTmpTexture, AccessFlags.ReadWrite);
                passData.streakTmpTexture2 = streakTmpTexture2;
                builder.UseTexture(streakTmpTexture2, AccessFlags.ReadWrite);
                passData.screenSpaceLensFlareBloomMipTexture = screenSpaceLensFlareBloomMipTexture;
                builder.UseTexture(screenSpaceLensFlareBloomMipTexture, AccessFlags.ReadWrite);
                passData.originalBloomTexture = originalBloomTexture;
                if(!sameInputOutputTex)
                    builder.UseTexture(originalBloomTexture, AccessFlags.ReadWrite);
                passData.actualWidth = srcDesc.width; 
                passData.actualHeight = srcDesc.height;
                passData.camera = camera;
                passData.material = m_Materials.lensFlareScreenSpace;
                passData.lensFlareScreenSpace = m_LensFlareScreenSpace; // NOTE: reference, assumed constant until executed.
                passData.downsample = downsample;
                passData.result = resultTexture;
                builder.UseTexture(resultTexture, AccessFlags.ReadWrite);

                builder.SetRenderFunc(static (LensFlareScreenSpacePassData data, UnsafeGraphContext context) =>
                {
                    var cmd = context.cmd;
                    var camera = data.camera;
                    var lensFlareScreenSpace = data.lensFlareScreenSpace;

                    LensFlareCommonSRP.DoLensFlareScreenSpaceCommon(
                        data.material,
                        camera,
                        (float)data.actualWidth,
                        (float)data.actualHeight,
                        data.lensFlareScreenSpace.tintColor.value,
                        data.originalBloomTexture,
                        data.screenSpaceLensFlareBloomMipTexture,
                        null, // We don't have any spectral LUT in URP
                        data.streakTmpTexture,
                        data.streakTmpTexture2,
                        new Vector4(
                            lensFlareScreenSpace.intensity.value,
                            lensFlareScreenSpace.firstFlareIntensity.value,
                            lensFlareScreenSpace.secondaryFlareIntensity.value,
                            lensFlareScreenSpace.warpedFlareIntensity.value),
                        new Vector4(
                            lensFlareScreenSpace.vignetteEffect.value,
                            lensFlareScreenSpace.startingPosition.value,
                            lensFlareScreenSpace.scale.value,
                            0), // Free slot, not used
                        new Vector4(
                            lensFlareScreenSpace.samples.value,
                            lensFlareScreenSpace.sampleDimmer.value,
                            lensFlareScreenSpace.chromaticAbberationIntensity.value,
                            0), // No need to pass a chromatic aberration sample count, hardcoded at 3 in shader
                        new Vector4(
                            lensFlareScreenSpace.streaksIntensity.value,
                            lensFlareScreenSpace.streaksLength.value,
                            lensFlareScreenSpace.streaksOrientation.value,
                            lensFlareScreenSpace.streaksThreshold.value),
                        new Vector4(
                            data.downsample,
                            lensFlareScreenSpace.warpedFlareScale.value.x,
                            lensFlareScreenSpace.warpedFlareScale.value.y,
                            0), // Free slot, not used
                        cmd,
                        data.result,
                        false);
                });
            }
            return originalBloomTexture;
        }

        #endregion

        static private void ScaleViewport(RasterCommandBuffer cmd, RTHandle sourceTextureHdl, RTHandle dest, UniversalCameraData cameraData, bool hasFinalPass)
        {
            RenderTargetIdentifier cameraTarget = BuiltinRenderTextureType.CameraTarget;
#if ENABLE_VR && ENABLE_XR_MODULE
            if (cameraData.xr.enabled)
                cameraTarget = cameraData.xr.renderTarget;
#endif
            if (dest.nameID == cameraTarget || cameraData.targetTexture != null)
            {
                if (hasFinalPass || !cameraData.resolveFinalTarget)
                {
                    // Inside the camera stack the target is the shared intermediate target, which can be scaled with render scale.
                    // camera.pixelRect is the viewport of the final target in pixels, so it cannot be used for the intermediate target.
                    // On intermediate target allocation the viewport size is baked into the target size.
                    // Which means the intermediate target does not have a viewport rect. Its offset is always 0 and its size matches viewport size.
                    // The overlay cameras inherit the base viewport, so they cannot have a different viewport,
                    // a necessary limitation since the target covers only the base viewport area.
                    // The offsetting is finally done by the final output viewport-rect to the final target.
                    // Note: effectively this is setting a fullscreen viewport for the intermediate target.
                    var targetWidth = cameraData.cameraTargetDescriptor.width;
                    var targetHeight = cameraData.cameraTargetDescriptor.height;
                    var targetViewportInPixels = new Rect(
                        0,
                        0,
                        targetWidth,
                        targetHeight);
                    cmd.SetViewport(targetViewportInPixels);
                }
                else
                    cmd.SetViewport(cameraData.pixelRect);
            }
        }

        static private void ScaleViewportAndBlit(RasterCommandBuffer cmd, RTHandle sourceTextureHdl, RTHandle dest, UniversalCameraData cameraData, Material material, bool hasFinalPass)
        {
            Vector4 scaleBias = RenderingUtils.GetFinalBlitScaleBias(sourceTextureHdl, dest, cameraData);
            ScaleViewport(cmd, sourceTextureHdl, dest, cameraData, hasFinalPass);

            Blitter.BlitTexture(cmd, sourceTextureHdl, scaleBias, material, 0);
        }

        static private void ScaleViewportAndDrawVisibilityMesh(RasterCommandBuffer cmd, RTHandle sourceTextureHdl, RTHandle dest, UniversalCameraData cameraData, Material material, bool hasFinalPass)
        {
#if ENABLE_VR && ENABLE_XR_MODULE
            Vector4 scaleBias = RenderingUtils.GetFinalBlitScaleBias(sourceTextureHdl, dest, cameraData);
            ScaleViewport(cmd, sourceTextureHdl, dest, cameraData, hasFinalPass);

            // Set property block for blit shader
            MaterialPropertyBlock xrPropertyBlock = XRSystemUniversal.GetMaterialPropertyBlock();
            xrPropertyBlock.SetVector(Shader.PropertyToID("_BlitScaleBias"), scaleBias);
            xrPropertyBlock.SetTexture(Shader.PropertyToID("_BlitTexture"), sourceTextureHdl);
            cameraData.xr.RenderVisibleMeshCustomMaterial(cmd, cameraData.xr.occlusionMeshScale, material, xrPropertyBlock, 1, cameraData.IsRenderTargetProjectionMatrixFlipped(dest));
#endif
        }

        #region FinalPass
        private class PostProcessingFinalSetupPassData
        {
            internal TextureHandle destinationTexture;
            internal TextureHandle sourceTexture;
            internal Material material;
            internal UniversalCameraData cameraData;
        }

        public void RenderFinalSetup(RenderGraph renderGraph, UniversalCameraData cameraData, in TextureHandle source, in TextureHandle destination, ref FinalBlitSettings settings)
        {
            // Scaled FXAA
            using (var builder = renderGraph.AddRasterRenderPass<PostProcessingFinalSetupPassData>("Postprocessing Final Setup Pass", out var passData, ProfilingSampler.Get(URPProfileId.RG_FinalSetup)))
            {
                Material material = m_Materials.scalingSetup;
                material.shaderKeywords = null;

                if (settings.isFxaaEnabled)
                    material.EnableKeyword(ShaderKeywordStrings.Fxaa);

                if (settings.isFsrEnabled)
                    material.EnableKeyword(settings.hdrOperations.HasFlag(HDROutputUtils.Operation.ColorEncoding) ? ShaderKeywordStrings.Gamma20AndHDRInput : ShaderKeywordStrings.Gamma20);

                if (settings.hdrOperations.HasFlag(HDROutputUtils.Operation.ColorEncoding))
                    SetupHDROutput(cameraData.hdrDisplayInformation, cameraData.hdrDisplayColorGamut, material, settings.hdrOperations, cameraData.rendersOverlayUI);

                if (settings.isAlphaOutputEnabled)
                    CoreUtils.SetKeyword(material, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, settings.isAlphaOutputEnabled);

                builder.AllowGlobalStateModification(true);
                passData.destinationTexture = destination;
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                passData.sourceTexture = source;
                builder.UseTexture(source, AccessFlags.Read);
                passData.cameraData = cameraData;
                passData.material = material;

                builder.SetRenderFunc(static (PostProcessingFinalSetupPassData data, RasterGraphContext context) =>
                {
                    var cmd = context.cmd;
                    RTHandle sourceTextureHdl = data.sourceTexture;

                    PostProcessUtils.SetSourceSize(cmd, sourceTextureHdl);

                    bool hasFinalPass = true; // This is a pass just before final pass. Viewport must match intermediate target.
                    ScaleViewportAndBlit(context.cmd, sourceTextureHdl, data.destinationTexture, data.cameraData, data.material, hasFinalPass);
                });
                return;
            }
        }

        private class PostProcessingFinalFSRScalePassData
        {
            internal TextureHandle sourceTexture;
            internal Material material;
            internal bool enableAlphaOutput;
            internal Vector2 fsrInputSize;
            internal Vector2 fsrOutputSize;
        }

        public void RenderFinalFSRScale(RenderGraph renderGraph, in TextureHandle source, in TextureDesc srcDesc, in TextureHandle destination, in TextureDesc dstDesc, bool enableAlphaOutput)
        {
            // FSR upscale
            m_Materials.easu.shaderKeywords = null;

            using (var builder = renderGraph.AddRasterRenderPass<PostProcessingFinalFSRScalePassData>("Postprocessing Final FSR Scale Pass", out var passData, ProfilingSampler.Get(URPProfileId.RG_FinalFSRScale)))
            {
                builder.AllowGlobalStateModification(true);
                builder.SetRenderAttachment(destination, 0, AccessFlags.Write);
                passData.sourceTexture = source;
                builder.UseTexture(source, AccessFlags.Read);
                passData.material = m_Materials.easu;
                passData.enableAlphaOutput = enableAlphaOutput;
                passData.fsrInputSize = new Vector2(srcDesc.width, srcDesc.height);
                passData.fsrOutputSize = new Vector2(dstDesc.width, dstDesc.height);

                builder.SetRenderFunc(static (PostProcessingFinalFSRScalePassData data, RasterGraphContext context) =>
                {
                    var cmd = context.cmd;
                    var sourceTex = data.sourceTexture;
                    var material = data.material;
                    var enableAlphaOutput = data.enableAlphaOutput;
                    RTHandle sourceHdl = (RTHandle)sourceTex;

                    FSRUtils.SetEasuConstants(cmd, data.fsrInputSize, data.fsrInputSize, data.fsrOutputSize);

                    CoreUtils.SetKeyword(material, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, enableAlphaOutput);

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
            internal UniversalCameraData cameraData;
            internal FinalBlitSettings settings;
        }

        /// <summary>
        /// Final blit settings.
        /// </summary>
        public struct FinalBlitSettings
        {
            /// <summary>Is FXAA enabled</summary>
            public bool isFxaaEnabled;
            /// <summary>Is FSR Enabled.</summary>
            public bool isFsrEnabled;
            /// <summary>Is TAA sharpening enabled.</summary>
            public bool isTaaSharpeningEnabled;
            /// <summary>True if final blit requires HDR output.</summary>
            public bool requireHDROutput;
            /// <summary>True if final blit needs to resolve to debug screen.</summary>
            public bool resolveToDebugScreen;
            /// <summary>True if final blit needs to output alpha channel.</summary>
            public bool isAlphaOutputEnabled;

            /// <summary>HDR Operations</summary>
            public HDROutputUtils.Operation hdrOperations;

            /// <summary>
            /// Create FinalBlitSettings
            /// </summary>
            /// <returns>New FinalBlitSettings</returns>
            public static FinalBlitSettings Create()
            {
                FinalBlitSettings s = new FinalBlitSettings();
                s.isFxaaEnabled = false;
                s.isFsrEnabled = false;
                s.isTaaSharpeningEnabled = false;
                s.requireHDROutput = false;
                s.resolveToDebugScreen = false;
                s.isAlphaOutputEnabled = false;

                s.hdrOperations = HDROutputUtils.Operation.None;

                return s;
            }
        };

        public void RenderFinalBlit(RenderGraph renderGraph, UniversalCameraData cameraData, in TextureHandle source, in TextureHandle overlayUITexture, in TextureHandle postProcessingTarget, ref FinalBlitSettings settings)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PostProcessingFinalBlitPassData>("Postprocessing Final Blit Pass", out var passData, ProfilingSampler.Get(URPProfileId.RG_FinalBlit)))
            {
                builder.AllowGlobalStateModification(true);
                passData.destinationTexture = postProcessingTarget;
                builder.SetRenderAttachment(postProcessingTarget, 0, AccessFlags.Write);
                passData.sourceTexture = source;
                builder.UseTexture(source, AccessFlags.Read);
                passData.cameraData = cameraData;
                passData.material = m_Materials.finalPass;
                passData.settings = settings;

                if (settings.requireHDROutput && m_EnableColorEncodingIfNeeded && cameraData.rendersOverlayUI)
                    builder.UseTexture(overlayUITexture, AccessFlags.Read);

#if ENABLE_VR && ENABLE_XR_MODULE
                if (cameraData.xr.enabled)
                {
                    // This is a screen-space pass, make sure foveated rendering is disabled for non-uniform renders
                    bool passSupportsFoveation = !XRSystem.foveatedRenderingCaps.HasFlag(FoveatedRenderingCaps.NonUniformRaster);
                    builder.EnableFoveatedRasterization(cameraData.xr.supportsFoveatedRendering && passSupportsFoveation);
                }
#endif

                builder.SetRenderFunc(static (PostProcessingFinalBlitPassData data, RasterGraphContext context) =>
                {
                    var cmd = context.cmd;
                    var material = data.material;
                    var isFxaaEnabled = data.settings.isFxaaEnabled;
                    var isFsrEnabled = data.settings.isFsrEnabled;
                    var isRcasEnabled = data.settings.isTaaSharpeningEnabled;
                    var requireHDROutput = data.settings.requireHDROutput;
                    var resolveToDebugScreen = data.settings.resolveToDebugScreen;
                    var isAlphaOutputEnabled = data.settings.isAlphaOutputEnabled;
                    RTHandle sourceTextureHdl = data.sourceTexture;
                    RTHandle destinationTextureHdl = data.destinationTexture;

                    PostProcessUtils.SetSourceSize(cmd, data.sourceTexture);

                    if (isFxaaEnabled)
                        material.EnableKeyword(ShaderKeywordStrings.Fxaa);

                    if (isFsrEnabled)
                    {
                        // RCAS
                        // Use the override value if it's available, otherwise use the default.
                        float sharpness = data.cameraData.fsrOverrideSharpness ? data.cameraData.fsrSharpness : FSRUtils.kDefaultSharpnessLinear;

                        // Set up the parameters for the RCAS pass unless the sharpness value indicates that it wont have any effect.
                        if (data.cameraData.fsrSharpness > 0.0f)
                        {
                            // RCAS is performed during the final post blit, but we set up the parameters here for better logical grouping.
                            material.EnableKeyword(requireHDROutput ? ShaderKeywordStrings.EasuRcasAndHDRInput : ShaderKeywordStrings.Rcas);
                            FSRUtils.SetRcasConstantsLinear(cmd, sharpness);
                        }
                    }
                    else if (isRcasEnabled)   // RCAS only
                    {
                        // Reuse RCAS as a standalone sharpening filter for TAA.
                        // If FSR is enabled then it overrides the sharpening/TAA setting and we skip it.
                        material.EnableKeyword(ShaderKeywordStrings.Rcas);
                        FSRUtils.SetRcasConstantsLinear(cmd, data.cameraData.taaSettings.contrastAdaptiveSharpening);
                    }

                    if (isAlphaOutputEnabled)
                        CoreUtils.SetKeyword(material, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, isAlphaOutputEnabled);

                    bool isRenderToBackBufferTarget = !data.cameraData.isSceneViewCamera;
#if ENABLE_VR && ENABLE_XR_MODULE
                    if (data.cameraData.xr.enabled)
                        isRenderToBackBufferTarget = destinationTextureHdl == data.cameraData.xr.renderTarget;
#endif
                    // HDR debug views force-renders to DebugScreenTexture.
                    isRenderToBackBufferTarget &= !resolveToDebugScreen;

                    Vector2 viewportScale = sourceTextureHdl.useScaling ? new Vector2(sourceTextureHdl.rtHandleProperties.rtHandleScale.x, sourceTextureHdl.rtHandleProperties.rtHandleScale.y) : Vector2.one;

                    // We y-flip if
                    // 1) we are blitting from render texture to back buffer(UV starts at bottom) and
                    // 2) renderTexture starts UV at top
                    bool yflip = isRenderToBackBufferTarget && data.cameraData.targetTexture == null && SystemInfo.graphicsUVStartsAtTop;
                    Vector4 scaleBias = yflip ? new Vector4(viewportScale.x, -viewportScale.y, 0, viewportScale.y) : new Vector4(viewportScale.x, viewportScale.y, 0, 0);

                    cmd.SetViewport(data.cameraData.pixelRect);
#if ENABLE_VR && ENABLE_XR_MODULE
                    if (data.cameraData.xr.enabled && data.cameraData.xr.hasValidVisibleMesh)
                    {
                        MaterialPropertyBlock xrPropertyBlock = XRSystemUniversal.GetMaterialPropertyBlock();
                        xrPropertyBlock.SetVector(Shader.PropertyToID("_BlitScaleBias"), scaleBias);
                        xrPropertyBlock.SetTexture(Shader.PropertyToID("_BlitTexture"), sourceTextureHdl);

                        data.cameraData.xr.RenderVisibleMeshCustomMaterial(cmd, data.cameraData.xr.occlusionMeshScale, material, xrPropertyBlock, 1, !yflip);
                    }
                    else
#endif
                        Blitter.BlitTexture(cmd, sourceTextureHdl, scaleBias, material, 0);
                });

                return;
            }
        }

        public void RenderFinalPassRenderGraph(RenderGraph renderGraph, ContextContainer frameData, in TextureHandle source, in TextureHandle overlayUITexture, in TextureHandle postProcessingTarget, bool enableColorEncodingIfNeeded)
        {
            var stack = VolumeManager.instance.stack;
            m_Tonemapping = stack.GetComponent<Tonemapping>();
            m_FilmGrain = stack.GetComponent<FilmGrain>();

            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();

            var material = m_Materials.finalPass;

            material.shaderKeywords = null;

            FinalBlitSettings settings = FinalBlitSettings.Create();

            var srcDesc = renderGraph.GetTextureDesc(source);

            var upscaledDesc = srcDesc;
            upscaledDesc.width = cameraData.pixelWidth;
            upscaledDesc.height = cameraData.pixelHeight;

            // TODO RENDERGRAPH: when we remove the old path we should review the naming of these variables...
            // m_HasFinalPass is used to let FX passes know when they are not being called by the actual final pass, so they can skip any "final work"
            m_HasFinalPass = false;
            // m_IsFinalPass is used by effects called by RenderFinalPassRenderGraph, so we let them know that we are in a final PP pass
            m_IsFinalPass = true;
            m_EnableColorEncodingIfNeeded = enableColorEncodingIfNeeded;

            if (m_FilmGrain.IsActive())
            {
                material.EnableKeyword(ShaderKeywordStrings.FilmGrain);
                PostProcessUtils.ConfigureFilmGrain(
                    m_Data,
                    m_FilmGrain,
                    upscaledDesc.width, upscaledDesc.height,
                    material
                );
            }

            if (cameraData.isDitheringEnabled)
            {
                material.EnableKeyword(ShaderKeywordStrings.Dithering);
                m_DitheringTextureIndex = PostProcessUtils.ConfigureDithering(
                    m_Data,
                    m_DitheringTextureIndex,
                    upscaledDesc.width, upscaledDesc.height,
                    material
                );
            }

            if (RequireSRGBConversionBlitToBackBuffer(cameraData.requireSrgbConversion))
                material.EnableKeyword(ShaderKeywordStrings.LinearToSRGBConversion);

            settings.hdrOperations = HDROutputUtils.Operation.None;
            settings.requireHDROutput = RequireHDROutput(cameraData);
            if (settings.requireHDROutput)
            {
                // If there is a final post process pass, it's always the final pass so do color encoding
                settings.hdrOperations = m_EnableColorEncodingIfNeeded ? HDROutputUtils.Operation.ColorEncoding : HDROutputUtils.Operation.None;
                // If the color space conversion wasn't applied by the uber pass, do it here
                if (!cameraData.postProcessEnabled)
                    settings.hdrOperations |= HDROutputUtils.Operation.ColorConversion;

                SetupHDROutput(cameraData.hdrDisplayInformation, cameraData.hdrDisplayColorGamut, material, settings.hdrOperations, cameraData.rendersOverlayUI);
            }
            DebugHandler debugHandler = GetActiveDebugHandler(cameraData);
            bool resolveToDebugScreen = debugHandler != null && debugHandler.WriteToDebugScreenTexture(cameraData.resolveFinalTarget);
            debugHandler?.UpdateShaderGlobalPropertiesForFinalValidationPass(renderGraph, cameraData, !m_HasFinalPass && !resolveToDebugScreen);

            settings.resolveToDebugScreen = resolveToDebugScreen;
            settings.isAlphaOutputEnabled = cameraData.isAlphaOutputEnabled;
            settings.isFxaaEnabled = (cameraData.antialiasing == AntialiasingMode.FastApproximateAntialiasing);
            settings.isFsrEnabled = ((cameraData.imageScalingMode == ImageScalingMode.Upscaling) && (cameraData.upscalingFilter == ImageUpscalingFilter.FSR));

            // Reuse RCAS pass as an optional standalone post sharpening pass for TAA.
            // This avoids the cost of EASU and is available for other upscaling options.
            // If FSR is enabled then FSR settings override the TAA settings and we perform RCAS only once.
            // If STP is enabled, then TAA sharpening has already been performed inside STP.
            settings.isTaaSharpeningEnabled = (cameraData.IsTemporalAAEnabled() && cameraData.taaSettings.contrastAdaptiveSharpening > 0.0f) && !settings.isFsrEnabled && !cameraData.IsSTPEnabled();

            var tempRtDesc = srcDesc;

            // Select a UNORM format since we've already performed tonemapping. (Values are in 0-1 range)
            // This improves precision and is required if we want to avoid excessive banding when FSR is in use.
            if (!settings.requireHDROutput)
                tempRtDesc.format = UniversalRenderPipeline.MakeUnormRenderTextureGraphicsFormat();

            var scalingSetupTarget = CreateCompatibleTexture(renderGraph, tempRtDesc, "scalingSetupTarget", true, FilterMode.Point);

            var upScaleTarget = CreateCompatibleTexture(renderGraph, upscaledDesc, "_UpscaledTexture", true, FilterMode.Point);

            var currentSource = source;
            if (cameraData.imageScalingMode != ImageScalingMode.None)
            {
                // When FXAA is enabled in scaled renders, we execute it in a separate blit since it's not designed to be used in
                // situations where the input and output resolutions do not match.
                // When FSR is active, we always need an additional pass since it has a very particular color encoding requirement.

                // NOTE: An ideal implementation could inline this color conversion logic into the UberPost pass, but the current code structure would make
                //       this process very complex. Specifically, we'd need to guarantee that the uber post output is always written to a UNORM format render
                //       target in order to preserve the precision of specially encoded color data.
                bool isSetupRequired = (settings.isFxaaEnabled || settings.isFsrEnabled);

                // When FXAA is needed while scaling is active, we must perform it before the scaling takes place.
                if (isSetupRequired)
                {
                    RenderFinalSetup(renderGraph, cameraData, in currentSource, in scalingSetupTarget, ref settings);
                    currentSource = scalingSetupTarget;

                    // Indicate that we no longer need to perform FXAA in the final pass since it was already perfomed here.
                    settings.isFxaaEnabled = false;
                }

                switch (cameraData.imageScalingMode)
                {
                    case ImageScalingMode.Upscaling:
                    {
                        switch (cameraData.upscalingFilter)
                        {
                            case ImageUpscalingFilter.Point:
                            {
                                // TAA post sharpening is an RCAS pass, avoid overriding it with point sampling.
                                if (!settings.isTaaSharpeningEnabled)
                                    material.EnableKeyword(ShaderKeywordStrings.PointSampling);
                                break;
                            }
                            case ImageUpscalingFilter.Linear:
                            {
                                break;
                            }
                            case ImageUpscalingFilter.FSR:
                            {
                                RenderFinalFSRScale(renderGraph, in currentSource, in srcDesc, in upScaleTarget, in upscaledDesc, settings.isAlphaOutputEnabled);
                                currentSource = upScaleTarget;
                                break;
                            }
                        }
                        break;
                    }
                    case ImageScalingMode.Downscaling:
                    {
                        // In the downscaling case, we don't perform any sort of filter override logic since we always want linear filtering
                        // and it's already the default option in the shader.

                        // Also disable TAA post sharpening pass when downscaling.
                        settings.isTaaSharpeningEnabled = false;
                        break;
                    }
                }
            }
            else if (settings.isFxaaEnabled)
            {
                // In unscaled renders, FXAA can be safely performed in the FinalPost shader
                material.EnableKeyword(ShaderKeywordStrings.Fxaa);
            }

            RenderFinalBlit(renderGraph, cameraData, in currentSource, in overlayUITexture, in postProcessingTarget, ref settings);
        }
#endregion

#region UberPost
        private class UberPostPassData
        {
            internal TextureHandle destinationTexture;
            internal TextureHandle sourceTexture;
            internal TextureHandle lutTexture;
            internal TextureHandle bloomTexture;
            internal Vector4 lutParams;
            internal TextureHandle userLutTexture;
            internal Vector4 userLutParams;
            internal Material material;
            internal UniversalCameraData cameraData;
            internal TonemappingMode toneMappingMode;
            internal bool isHdrGrading;
            internal bool isBackbuffer;
            internal bool enableAlphaOutput;
            internal bool hasFinalPass;
        }

        TextureHandle TryGetCachedUserLutTextureHandle(RenderGraph renderGraph)
        {
            if (m_ColorLookup.texture.value == null)
            {
                if (m_UserLut != null)
                {
                    m_UserLut.Release();
                    m_UserLut = null;
                }
            }
            else
            {
                if (m_UserLut == null || m_UserLut.externalTexture != m_ColorLookup.texture.value)
                {
                    m_UserLut?.Release();
                    m_UserLut = RTHandles.Alloc(m_ColorLookup.texture.value);
                }
            }
            return m_UserLut != null ? renderGraph.ImportTexture(m_UserLut) : TextureHandle.nullHandle;
        }

        public void RenderUberPost(RenderGraph renderGraph, ContextContainer frameData, UniversalCameraData cameraData, UniversalPostProcessingData postProcessingData,
            in TextureHandle sourceTexture, in TextureHandle destTexture, in TextureHandle lutTexture, in TextureHandle bloomTexture, in TextureHandle overlayUITexture,
            bool requireHDROutput, bool enableAlphaOutput, bool resolveToDebugScreen, bool hasFinalPass)
        {
            var material = m_Materials.uber;
            bool hdrGrading = postProcessingData.gradingMode == ColorGradingMode.HighDynamicRange;
            int lutHeight = postProcessingData.lutSize;
            int lutWidth = lutHeight * lutHeight;

            // Source material setup
            float postExposureLinear = Mathf.Pow(2f, m_ColorAdjustments.postExposure.value);
            Vector4 lutParams = new Vector4(1f / lutWidth, 1f / lutHeight, lutHeight - 1f, postExposureLinear);

            TextureHandle userLutTexture = TryGetCachedUserLutTextureHandle(renderGraph);
            Vector4 userLutParams = !m_ColorLookup.IsActive()
                ? Vector4.zero
                : new Vector4(1f / m_ColorLookup.texture.value.width,
                    1f / m_ColorLookup.texture.value.height,
                    m_ColorLookup.texture.value.height - 1f,
                    m_ColorLookup.contribution.value);

            using (var builder = renderGraph.AddRasterRenderPass<UberPostPassData>("Blit Post Processing", out var passData, ProfilingSampler.Get(URPProfileId.RG_UberPost)))
            {
                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();

#if ENABLE_VR && ENABLE_XR_MODULE
                if (cameraData.xr.enabled)
                {
                    bool passSupportsFoveation = cameraData.xrUniversal.canFoveateIntermediatePasses || resourceData.isActiveTargetBackBuffer;
                    // This is a screen-space pass, make sure foveated rendering is disabled for non-uniform renders
                    passSupportsFoveation &= !XRSystem.foveatedRenderingCaps.HasFlag(FoveatedRenderingCaps.NonUniformRaster);
                    builder.EnableFoveatedRasterization(cameraData.xr.supportsFoveatedRendering && passSupportsFoveation);
                }
#endif

                builder.AllowGlobalStateModification(true);
                passData.destinationTexture = destTexture;
                builder.SetRenderAttachment(destTexture, 0, AccessFlags.Write);
                passData.sourceTexture = sourceTexture;
                builder.UseTexture(sourceTexture, AccessFlags.Read);
                passData.lutTexture = lutTexture;
                builder.UseTexture(lutTexture, AccessFlags.Read);
                passData.lutParams = lutParams;
                if (userLutTexture.IsValid())
                {
                    passData.userLutTexture = userLutTexture;
                    builder.UseTexture(userLutTexture, AccessFlags.Read);
                }

                if (m_Bloom.IsActive())
                {
                    builder.UseTexture(bloomTexture, AccessFlags.Read);
                    passData.bloomTexture = bloomTexture;
                }

                if (requireHDROutput && m_EnableColorEncodingIfNeeded && overlayUITexture.IsValid())
                    builder.UseTexture(overlayUITexture, AccessFlags.Read);

                passData.userLutParams = userLutParams;
                passData.cameraData = cameraData;
                passData.material = material;
                passData.toneMappingMode = m_Tonemapping.mode.value;
                passData.isHdrGrading = hdrGrading;
                passData.enableAlphaOutput = enableAlphaOutput;
                passData.hasFinalPass = hasFinalPass;

                builder.SetRenderFunc(static (UberPostPassData data, RasterGraphContext context) =>
                {
                    var cmd = context.cmd;
                    var camera = data.cameraData.camera;
                    var material = data.material;
                    RTHandle sourceTextureHdl = data.sourceTexture;

                    material.SetTexture(ShaderConstants._InternalLut, data.lutTexture);
                    material.SetVector(ShaderConstants._Lut_Params, data.lutParams);
                    material.SetTexture(ShaderConstants._UserLut, data.userLutTexture);
                    material.SetVector(ShaderConstants._UserLut_Params, data.userLutParams);

                    if (data.bloomTexture.IsValid())
                    {
                        material.SetTexture(ShaderConstants._Bloom_Texture, data.bloomTexture);
                    }

                    if (data.isHdrGrading)
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

                    CoreUtils.SetKeyword(material, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, data.enableAlphaOutput);

                    // Done with Uber, blit it
#if ENABLE_VR && ENABLE_XR_MODULE
                    if (data.cameraData.xr.enabled && data.cameraData.xr.hasValidVisibleMesh)
                        ScaleViewportAndDrawVisibilityMesh(cmd, sourceTextureHdl, data.destinationTexture, data.cameraData, material, data.hasFinalPass);
                    else
#endif
                        ScaleViewportAndBlit(cmd, sourceTextureHdl, data.destinationTexture, data.cameraData, material, data.hasFinalPass);

                });

                return;
            }
        }
#endregion

        private class PostFXSetupPassData { }

        public void RenderPostProcessingRenderGraph(RenderGraph renderGraph, ContextContainer frameData, in TextureHandle activeCameraColorTexture, in TextureHandle lutTexture, in TextureHandle overlayUITexture, in TextureHandle postProcessingTarget, bool hasFinalPass, bool resolveToDebugScreen, bool enableColorEndingIfNeeded)
        {
            UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
            UniversalRenderingData renderingData = frameData.Get<UniversalRenderingData>();
            UniversalCameraData cameraData = frameData.Get<UniversalCameraData>();
            UniversalPostProcessingData postProcessingData = frameData.Get<UniversalPostProcessingData>();

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
            m_UseFastSRGBLinearConversion = postProcessingData.useFastSRGBLinearConversion;
            m_SupportDataDrivenLensFlare = postProcessingData.supportDataDrivenLensFlare;
            m_SupportScreenSpaceLensFlare = postProcessingData.supportScreenSpaceLensFlare;

            m_HasFinalPass = hasFinalPass;
            m_EnableColorEncodingIfNeeded = enableColorEndingIfNeeded;

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

            // Disable MotionBlur in EditMode, so that editing remains clear and readable.
            // NOTE: HDRP does the same via CoreUtils::AreAnimatedMaterialsEnabled().
            // Disable MotionBlurMode.CameraAndObjects on renderers that do not support motion vectors
            useMotionBlur = useMotionBlur && Application.isPlaying;
            if (useMotionBlur && m_MotionBlur.mode.value == MotionBlurMode.CameraAndObjects)
            {
                useMotionBlur &= renderer.SupportsMotionVectors();
                if (!useMotionBlur)
                {
                    var warning = "Disabling Motion Blur for Camera And Objects because the renderer does not implement motion vectors.";
                    const int warningThrottleFrames = 60 * 1; // 60 FPS * 1 sec
                    if (Time.frameCount % warningThrottleFrames == 0)
                        Debug.LogWarning(warning);
                }
            }

            // Note that enabling jitters uses the same CameraData::IsTemporalAAEnabled(). So if we add any other kind of overrides (like
            // disable useTemporalAA if another feature is disabled) then we need to put it in CameraData::IsTemporalAAEnabled() as opposed
            // to tweaking the value here.
            bool useTemporalAA = cameraData.IsTemporalAAEnabled();

            // STP is only enabled when TAA is enabled and all of its runtime requirements are met.
            // Using IsSTPRequested() vs IsSTPEnabled() for perf reason here, as we already know TAA status
            bool isSTPRequested = cameraData.IsSTPRequested();
            bool useSTP = useTemporalAA && isSTPRequested;

            // Warn users if TAA and STP are disabled despite being requested
            if (!useTemporalAA && cameraData.IsTemporalAARequested())
                TemporalAA.ValidateAndWarn(cameraData, isSTPRequested);

            using (var builder = renderGraph.AddRasterRenderPass<PostFXSetupPassData>("Setup PostFX passes", out var passData,
                ProfilingSampler.Get(URPProfileId.RG_SetupPostFX)))
            {
                builder.AllowGlobalStateModification(true);
                builder.SetRenderFunc(static (PostFXSetupPassData data, RasterGraphContext context) =>
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
                RenderStopNaN(renderGraph, in currentSource, out var stopNaNTarget);
                currentSource = stopNaNTarget;
            }

            if(useSubPixelMorpAA)
            {
                RenderSMAA(renderGraph, resourceData, cameraData.antialiasingQuality, in currentSource, out var SMAATarget);
                currentSource = SMAATarget;
            }

            // Depth of Field
            // Adreno 3xx SystemInfo.graphicsShaderLevel is 35, but instancing support is disabled due to buggy drivers.
            // DOF shader uses #pragma target 3.5 which adds requirement for instancing support, thus marking the shader unsupported on those devices.
            if (useDepthOfField)
            {
                RenderDoF(renderGraph, resourceData, cameraData, in currentSource, out var DoFTarget);
                currentSource = DoFTarget;
            }

            // Temporal Anti Aliasing
            if (useTemporalAA)
            {
                if (useSTP)
                {
                    RenderSTP(renderGraph, resourceData, cameraData, ref currentSource, out var StpTarget);
                    currentSource = StpTarget;
                }
                else
                {
                    RenderTemporalAA(renderGraph, resourceData, cameraData, ref currentSource, out var TemporalAATarget);
                    currentSource = TemporalAATarget;
                }
            }

            if(useMotionBlur)
            {
                RenderMotionBlur(renderGraph, resourceData, cameraData, in currentSource, out var MotionBlurTarget);
                currentSource = MotionBlurTarget;
            }

            if(usePaniniProjection)
            {
                RenderPaniniProjection(renderGraph, cameraData.camera, in currentSource, out var PaniniTarget);
                currentSource = PaniniTarget;
            }

            // Uberpost
            {
                // Reset uber keywords
                m_Materials.uber.shaderKeywords = null;

                var srcDesc = currentSource.GetDescriptor(renderGraph);

                // Bloom goes first
                TextureHandle bloomTexture = TextureHandle.nullHandle;
                bool bloomActive = m_Bloom.IsActive();
                //Even if bloom is not active we need the texture if the lensFlareScreenSpace pass is active.
                if (bloomActive || useLensFlareScreenSpace)
                {
                    RenderBloomTexture(renderGraph, currentSource, out bloomTexture, cameraData.isAlphaOutputEnabled);

                    if (useLensFlareScreenSpace)
                    {
                        int maxBloomMip = Mathf.Clamp(m_LensFlareScreenSpace.bloomMip.value, 0, m_Bloom.maxIterations.value/2);
                        bool sameInputOutputTex = maxBloomMip == 0;
                        bloomTexture = RenderLensFlareScreenSpace(renderGraph, cameraData.camera, srcDesc, bloomTexture, _BloomMipUp[maxBloomMip], sameInputOutputTex);
                    }

                    UberPostSetupBloomPass(renderGraph, m_Materials.uber, srcDesc);
                }

                if (useLensFlare)
                {
                    LensFlareDataDrivenComputeOcclusion(renderGraph, resourceData, cameraData, srcDesc);
                    RenderLensFlareDataDriven(renderGraph, resourceData, cameraData, in currentSource, in srcDesc);
                }

                // TODO RENDERGRAPH: Once we started removing the non-RG code pass in URP, we should move functions below to renderfunc so that material setup happens at
                // the same timeline of executing the rendergraph. Keep them here for now so we cound reuse non-RG code to reduce maintainance cost.
                SetupLensDistortion(m_Materials.uber, isSceneViewCamera);
                SetupChromaticAberration(m_Materials.uber);
                SetupVignette(m_Materials.uber, cameraData.xr, srcDesc.width, srcDesc.height);
                SetupGrain(cameraData, m_Materials.uber);
                SetupDithering(cameraData, m_Materials.uber);

                if (RequireSRGBConversionBlitToBackBuffer(cameraData.requireSrgbConversion))
                    m_Materials.uber.EnableKeyword(ShaderKeywordStrings.LinearToSRGBConversion);

                if (m_UseFastSRGBLinearConversion)
                {
                    m_Materials.uber.EnableKeyword(ShaderKeywordStrings.UseFastSRGBLinearConversion);
                }

                bool requireHDROutput = RequireHDROutput(cameraData);
                if (requireHDROutput)
                {
                    // Color space conversion is already applied through color grading, do encoding if uber post is the last pass
                    // Otherwise encoding will happen in the final post process pass or the final blit pass
                    HDROutputUtils.Operation hdrOperations = !m_HasFinalPass && m_EnableColorEncodingIfNeeded ? HDROutputUtils.Operation.ColorEncoding : HDROutputUtils.Operation.None;

                    SetupHDROutput(cameraData.hdrDisplayInformation, cameraData.hdrDisplayColorGamut, m_Materials.uber, hdrOperations, cameraData.rendersOverlayUI);
                }

                bool enableAlphaOutput = cameraData.isAlphaOutputEnabled;

                DebugHandler debugHandler = GetActiveDebugHandler(cameraData);
                debugHandler?.UpdateShaderGlobalPropertiesForFinalValidationPass(renderGraph, cameraData, !m_HasFinalPass && !resolveToDebugScreen);

                RenderUberPost(renderGraph, frameData, cameraData, postProcessingData, in currentSource, in postProcessingTarget, in lutTexture, in bloomTexture, in overlayUITexture, requireHDROutput, enableAlphaOutput, resolveToDebugScreen, hasFinalPass);
            }
        }
    }
}
