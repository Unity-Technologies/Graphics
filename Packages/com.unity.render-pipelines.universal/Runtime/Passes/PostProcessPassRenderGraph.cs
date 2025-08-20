using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using System;
using System.Runtime.CompilerServices;

namespace UnityEngine.Rendering.Universal
{
    internal class PostProcessPassRenderGraph
    {
        // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
        // TODO: move into post-process passes.
        internal static class ShaderConstants
        {
            public static readonly int _CameraDepthTextureID = Shader.PropertyToID("_CameraDepthTexture");

            public static readonly int _StencilRef = Shader.PropertyToID("_StencilRef");
            public static readonly int _StencilMask = Shader.PropertyToID("_StencilMask");

            public static readonly int _ColorTexture = Shader.PropertyToID("_ColorTexture");
            public static readonly int _Params = Shader.PropertyToID("_Params");
            public static readonly int _Params2 = Shader.PropertyToID("_Params2");

            public static readonly int _ViewProjM = Shader.PropertyToID("_ViewProjM");
            public static readonly int _PrevViewProjM = Shader.PropertyToID("_PrevViewProjM");
            public static readonly int _ViewProjMStereo = Shader.PropertyToID("_ViewProjMStereo");
            public static readonly int _PrevViewProjMStereo = Shader.PropertyToID("_PrevViewProjMStereo");

            public static readonly int _FullscreenProjMat = Shader.PropertyToID("_FullscreenProjMat");

            // DoF
            public static readonly int _FullCoCTexture = Shader.PropertyToID("_FullCoCTexture");
            public static readonly int _HalfCoCTexture = Shader.PropertyToID("_HalfCoCTexture");
            public static readonly int _DofTexture = Shader.PropertyToID("_DofTexture");
            public static readonly int _CoCParams = Shader.PropertyToID("_CoCParams");
            public static readonly int _BokehKernel = Shader.PropertyToID("_BokehKernel");
            public static readonly int _BokehConstants = Shader.PropertyToID("_BokehConstants");
            public static readonly int _DownSampleScaleFactor = Shader.PropertyToID("_DownSampleScaleFactor");

            // SMAA
            public static readonly int _Metrics = Shader.PropertyToID("_Metrics");
            public static readonly int _AreaTexture = Shader.PropertyToID("_AreaTexture");
            public static readonly int _SearchTexture = Shader.PropertyToID("_SearchTexture");
            public static readonly int _BlendTexture = Shader.PropertyToID("_BlendTexture");
            //public static readonly int _EdgeTexture = Shader.PropertyToID("_EdgeTexture");

            // Bloom
            public static readonly int _SourceTexLowMip = Shader.PropertyToID("_SourceTexLowMip");
            public static readonly int _Bloom_Params = Shader.PropertyToID("_Bloom_Params");
            public static readonly int _Bloom_Texture = Shader.PropertyToID("_Bloom_Texture");
            public static readonly int _LensDirt_Texture = Shader.PropertyToID("_LensDirt_Texture");
            public static readonly int _LensDirt_Params = Shader.PropertyToID("_LensDirt_Params");
            public static readonly int _LensDirt_Intensity = Shader.PropertyToID("_LensDirt_Intensity");

            // Uber
            public static readonly int _Distortion_Params1 = Shader.PropertyToID("_Distortion_Params1");
            public static readonly int _Distortion_Params2 = Shader.PropertyToID("_Distortion_Params2");
            public static readonly int _Chroma_Params = Shader.PropertyToID("_Chroma_Params");
            public static readonly int _Vignette_Params1 = Shader.PropertyToID("_Vignette_Params1");
            public static readonly int _Vignette_Params2 = Shader.PropertyToID("_Vignette_Params2");
            public static readonly int _Vignette_ParamsXR = Shader.PropertyToID("_Vignette_ParamsXR");

            // Color Lookup-table
            public static readonly int _InternalLut = Shader.PropertyToID("_InternalLut");
            public static readonly int _Lut_Params = Shader.PropertyToID("_Lut_Params");
            public static readonly int _UserLut = Shader.PropertyToID("_UserLut");
            public static readonly int _UserLut_Params = Shader.PropertyToID("_UserLut_Params");
        }

        // TODO: move into post-process passes.
        internal static class Constants
        {
            // Bloom
            public const int k_MaxPyramidSize = 16;

            // DoF
            public const int k_GaussianDoFPassComputeCoc = 0;
            public const int k_GaussianDoFPassDownscalePrefilter = 1;
            public const int k_GaussianDoFPassBlurH = 2;
            public const int k_GaussianDoFPassBlurV = 3;
            public const int k_GaussianDoFPassComposite = 4;

            public const int k_BokehDoFPassComputeCoc = 0;
            public const int k_BokehDoFPassDownscalePrefilter = 1;
            public const int k_BokehDoFPassBlur = 2;
            public const int k_BokehDoFPassPostFilter = 3;
            public const int k_BokehDoFPassComposite = 4;
        }

        PostProcessMaterialLibrary m_Materials;

        // Builtin effects settings (VolumeComponents)
        DepthOfField m_DepthOfField;
        MotionBlur m_MotionBlur;
        PaniniProjection m_PaniniProjection;
        Bloom m_Bloom;
        ScreenSpaceLensFlare m_LensFlareScreenSpace;
        LensDistortion m_LensDistortion;
        ChromaticAberration m_ChromaticAberration;
        Vignette m_Vignette;
        ColorLookup m_ColorLookup;
        ColorAdjustments m_ColorAdjustments;
        Tonemapping m_Tonemapping;
        FilmGrain m_FilmGrain;

        // Targets
        string[] m_BloomMipDownName;
        string[] m_BloomMipUpName;
        TextureHandle[] _BloomMipUp;
        TextureHandle[] _BloomMipDown;

        RTHandle m_UserLut;
        RTHandle m_InternalLut;

        // SMAA misc.
        readonly GraphicsFormat m_SMAAEdgeFormat;

        // Bloom misc.
        readonly GraphicsFormat m_BloomColorFormat;

        // Cached bloom params from previous frame to avoid unnecessary material updates
        BloomMaterialParams m_BloomParamsPrev;

        // DoF misc.
        readonly GraphicsFormat m_GaussianCoCFormat;
        readonly GraphicsFormat m_GaussianDoFColorFormat;

        // Bokeh DoF misc.
        Vector4[] m_BokehKernel;
        int m_BokehHash;
        // Needed if the device changes its render target width/height (ex, Mobile platform allows change of orientation)
        float m_BokehMaxRadius;
        float m_BokehRCPAspect;

        // Lens Flare misc.
        readonly GraphicsFormat m_LensFlareScreenSpaceColorFormat;

        // Uber misc.
        int m_DitheringTextureIndex;    // 8-bit dithering

        // If there's a final post process pass after this pass.
        // If yes, Film Grain and Dithering are setup in the final pass, otherwise they are setup in this pass.
        bool m_HasFinalPass;

        // Some Android devices do not support sRGB backbuffer
        // We need to do the conversion manually on those
        // Also if HDR output is active
        bool m_EnableColorEncodingIfNeeded;

        // Use Fast conversions between SRGB and Linear
        bool m_UseFastSRGBLinearConversion;

        // Support Screen Space Lens Flare post process effect
        bool m_SupportScreenSpaceLensFlare;

        // Support Data Driven Lens Flare post process effect
        bool m_SupportDataDrivenLensFlare;


        /// <summary>
        /// Creates a new <c>PostProcessPass</c> instance.
        /// </summary>
        /// <param name="data">The <c>PostProcessData</c> resources to use.</param>
        /// <param name="requestPostProColorFormat">Requested <c>GraphicsFormat</c> for postprocess rendering.</param>
        /// <seealso cref="RenderPassEvent"/>
        /// <seealso cref="PostProcessData"/>
        /// <seealso cref="PostProcessParams"/>
        /// <seealso cref="GraphicsFormat"/>
        public PostProcessPassRenderGraph(PostProcessData data, GraphicsFormat requestPostProColorFormat)
        {
            Assertions.Assert.IsNotNull(data, "PostProcessData and resources cannot be null.");

            m_Materials = new PostProcessMaterialLibrary(data);

            // Arrays for Bloom pyramid TextureHandle names.
            m_BloomMipDownName = new string[Constants.k_MaxPyramidSize];
            m_BloomMipUpName = new string[Constants.k_MaxPyramidSize];

            for (int i = 0; i < Constants.k_MaxPyramidSize; i++)
            {
                m_BloomMipUpName[i] = "_BloomMipUp" + i;
                m_BloomMipDownName[i] = "_BloomMipDown" + i;
            }

            // Arrays for Bloom pyramid TextureHandles.
            _BloomMipUp = new TextureHandle[Constants.k_MaxPyramidSize];
            _BloomMipDown = new TextureHandle[Constants.k_MaxPyramidSize];

            // NOTE: Request color format is the back-buffer color format. It can be HDR or SDR (when HDR disabled).
            // Request color might have alpha or might not have alpha.
            // The actual post-process target can be different. A RenderTexture with a custom format. Not necessarily a back-buffer.
            // A RenderTexture with a custom format can have an alpha channel, regardless of the back-buffer setting,
            // so the post-processing should just use the current target format/alpha to toggle alpha output.
            //
            // However, we want to filter out the alpha shader variants when not used (common case).
            // The rule is that URP post-processing format follows the back-buffer format setting.

            bool requestHDR = IsHDRFormat(requestPostProColorFormat);
            //bool requestAlpha = IsAlphaFormat(postProcessParams.requestColorFormat);
            GraphicsFormat defaultFormat = GraphicsFormat.None;

            // Texture format pre-lookup
            // UUM-41070: We require `Linear | Render` but with the deprecated FormatUsage this was checking `Blend`
            // For now, we keep checking for `Blend` until the performance hit of doing the correct checks is evaluated
            if (requestHDR)
            {
                const GraphicsFormatUsage usage = GraphicsFormatUsage.Blend;
                if (SystemInfo.IsFormatSupported(requestPostProColorFormat, usage))    // Typically, RGBA16Float.
                {
                    defaultFormat = requestPostProColorFormat;
                }
                else if (SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32, usage)) // HDR fallback
                {
                    // NOTE: Technically request format can be with alpha, however if it's not supported and we fall back here
                    // , we assume no alpha. Post-process default format follows the back buffer format.
                    // If support failed, it must have failed for back buffer too.
                    defaultFormat = GraphicsFormat.B10G11R11_UFloatPack32;
                }
                else
                {
                    defaultFormat = QualitySettings.activeColorSpace == ColorSpace.Linear
                        ? GraphicsFormat.R8G8B8A8_SRGB
                        : GraphicsFormat.R8G8B8A8_UNorm;
                }
            }
            else // SDR
            {
                defaultFormat = QualitySettings.activeColorSpace == ColorSpace.Linear
                    ? GraphicsFormat.R8G8B8A8_SRGB
                    : GraphicsFormat.R8G8B8A8_UNorm;
            }

            // Bloom
            m_BloomColorFormat = defaultFormat;

            // SMAA
            // Only two components are needed for edge render texture, but on some vendors four components may be faster.
            if (SystemInfo.IsFormatSupported(GraphicsFormat.R8G8_UNorm, GraphicsFormatUsage.Render) && SystemInfo.graphicsDeviceVendor.ToLowerInvariant().Contains("arm"))
                m_SMAAEdgeFormat = GraphicsFormat.R8G8_UNorm;
            else
                m_SMAAEdgeFormat = GraphicsFormat.R8G8B8A8_UNorm;

            // Depth of Field
            //
            // CoC
            // UUM-41070: We require `Linear | Render` but with the deprecated FormatUsage this was checking `Blend`
            // For now, we keep checking for `Blend` until the performance hit of doing the correct checks is evaluated
            if (SystemInfo.IsFormatSupported(GraphicsFormat.R16_UNorm, GraphicsFormatUsage.Blend))
                m_GaussianCoCFormat = GraphicsFormat.R16_UNorm;
            else if (SystemInfo.IsFormatSupported(GraphicsFormat.R16_SFloat, GraphicsFormatUsage.Blend))
                m_GaussianCoCFormat = GraphicsFormat.R16_SFloat;
            else // Expect CoC banding
                m_GaussianCoCFormat = GraphicsFormat.R8_UNorm;

            m_GaussianDoFColorFormat = defaultFormat;

            // LensFlare
            m_LensFlareScreenSpaceColorFormat = defaultFormat;
        }

        public void Cleanup()
        {
            m_Materials.Cleanup();
            Dispose();
        }

        /// <summary>
        /// Disposes used resources.
        /// </summary>
        public void Dispose()
        {
            m_UserLut?.Release();
        }

        // NOTE: Duplicate in compatibility mode
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsHDRFormat(GraphicsFormat format)
        {
            return format == GraphicsFormat.B10G11R11_UFloatPack32 ||
                   GraphicsFormatUtility.IsHalfFormat(format) ||
                   GraphicsFormatUtility.IsFloatFormat(format);
        }

        // NOTE: Duplicate in compatibility mode
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool IsAlphaFormat(GraphicsFormat format)
        {
            return GraphicsFormatUtility.HasAlphaChannel(format);
        }

        // NOTE: Duplicate in compatibility mode
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        bool RequireSRGBConversionBlitToBackBuffer(bool requireSrgbConversion)
        {
            return requireSrgbConversion && m_EnableColorEncodingIfNeeded;
        }

        // NOTE: Duplicate in compatibility mode
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static bool RequireHDROutput(UniversalCameraData cameraData)
        {
            // If capturing, don't convert to HDR.
            // If not last in the stack, don't convert to HDR.
            return cameraData.isHDROutputActive && cameraData.captureActions == null;
        }

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

        internal static RenderTextureDescriptor GetCompatibleDescriptor(RenderTextureDescriptor desc, int width, int height, GraphicsFormat format, GraphicsFormat depthStencilFormat = GraphicsFormat.None)
        {
            desc.depthStencilFormat = depthStencilFormat;
            desc.msaaSamples = 1;
            desc.width = width;
            desc.height = height;
            desc.graphicsFormat = format;
            return desc;
        }

        #region StopNaNs
        private class StopNaNsPassData
        {
            internal TextureHandle sourceTexture;
            internal Material stopNaN;
        }

        public void RenderStopNaN(RenderGraph renderGraph, in TextureHandle activeCameraColor, out TextureHandle stopNaNTarget)
        {
            stopNaNTarget = CreateCompatibleTexture(renderGraph, activeCameraColor, "_StopNaNsTarget", true, FilterMode.Bilinear);

            using (var builder = renderGraph.AddRasterRenderPass<StopNaNsPassData>("Stop NaNs", out var passData,
                       ProfilingSampler.Get(URPProfileId.RG_StopNaNs)))
            {
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
                passData.areaTexture = m_Materials.resources.textures.smaaAreaTex;
                passData.searchTexture = m_Materials.resources.textures.smaaSearchTex;
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
            internal Vector4 parameters2;
            internal BloomFilterMode bloomFilter;
            internal bool highQualityFiltering;
            internal bool enableAlphaOutput;

            internal bool Equals(ref BloomMaterialParams other)
            {
                return parameters == other.parameters &&
                       parameters2 == other.parameters2 &&
                       highQualityFiltering == other.highQualityFiltering &&
                       enableAlphaOutput == other.enableAlphaOutput &&
                       bloomFilter == other.bloomFilter;
            }
        }

        public Vector2Int CalcBloomResolution(Bloom bloom, in TextureDesc bloomSourceDesc)
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

            //We should set the limit the downres result to ensure we dont turn 1x1 textures, which should technically be valid
            //into 0x0 textures which will be invalid
            int tw = Mathf.Max(1, bloomSourceDesc.width >> downres);
            int th = Mathf.Max(1, bloomSourceDesc.height >> downres);

            return new Vector2Int(tw, th);
        }

        public int CalcBloomMipCount(Bloom bloom, Vector2Int bloomResolution)
        {
            // Determine the iteration count
            int maxSize = Mathf.Max(bloomResolution.x, bloomResolution.y);
            int iterations = Mathf.FloorToInt(Mathf.Log(maxSize, 2f) - 1);
            int mipCount = Mathf.Clamp(iterations, 1, m_Bloom.maxIterations.value);
            return mipCount;
        }

        public void RenderBloomTexture(RenderGraph renderGraph, in TextureHandle source, out TextureHandle destination, bool enableAlphaOutput)
        {
            var srcDesc = source.GetDescriptor(renderGraph);

            Vector2Int bloomRes = CalcBloomResolution(m_Bloom, in srcDesc);
            int mipCount = CalcBloomMipCount(m_Bloom, bloomRes);
            int tw = bloomRes.x;
            int th = bloomRes.y;

            // Setup
            using(new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_BloomSetup)))
            {
                // Pre-filtering parameters
                float clamp = m_Bloom.clamp.value;
                float threshold = Mathf.GammaToLinearSpace(m_Bloom.threshold.value);
                float thresholdKnee = threshold * 0.5f; // Hardcoded soft knee

                // Material setup
                float scatter = Mathf.Lerp(0.05f, 0.95f, m_Bloom.scatter.value);   // Blend factor between low/hi mip on upsample.
                float kawaseScatter = Mathf.Clamp01(m_Bloom.scatter.value);          // Blend factor between linear and blurred sample. 1.0 for strict Kawase blur.
                float dualScatter = Mathf.Lerp(0.3f, 1.3f, m_Bloom.scatter.value); // Dual upsample filter scale. Scatter default == 0.7 --> 1.0 filter scale.

                BloomMaterialParams bloomParams = new BloomMaterialParams();
                bloomParams.parameters = new Vector4(scatter, clamp, threshold, thresholdKnee);
                bloomParams.parameters2 = new Vector4(0.5f, kawaseScatter, dualScatter, 0.5f * dualScatter);
                bloomParams.bloomFilter = m_Bloom.filter.value;
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
                    material.SetVector(ShaderConstants._Params2, bloomParams.parameters2);
                    CoreUtils.SetKeyword(material, ShaderKeywordStrings.BloomHQ, bloomParams.highQualityFiltering);
                    CoreUtils.SetKeyword(material, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, bloomParams.enableAlphaOutput);

                    // These materials are duplicate just to allow different bloom blits to use different textures.
                    for (uint i = 0; i < Constants.k_MaxPyramidSize; ++i)
                    {
                        var materialPyramid = m_Materials.bloomUpsample[i];
                        materialPyramid.SetVector(ShaderConstants._Params, bloomParams.parameters);
                        CoreUtils.SetKeyword(materialPyramid, ShaderKeywordStrings.BloomHQ, bloomParams.highQualityFiltering);
                        CoreUtils.SetKeyword(materialPyramid, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, bloomParams.enableAlphaOutput);

                        // TODO: investigate suggested quality improvement trick in more detail:
                        // Kawase5: 0, 1, 2, 2, 3
                        // Kawase9: 0, 1, 2, 3, 4, 4, 5, 6, 7
                        // ? -> KawaseN: duplicate pass at N/2 (See, Bandwidth-Efficient Rendering, siggraph2015)
                        float kawaseDist = 0.5f + ((i > mipCount / 2) ? (i - 1) : i);

                        Vector4 params2 = bloomParams.parameters2;
                        params2.x = kawaseDist;
                        materialPyramid.SetVector(ShaderConstants._Params2, params2);
                    }

                    m_BloomParamsPrev = bloomParams;
                }

                // Create bloom mip pyramid textures
                {
                    var desc = GetCompatibleDescriptor(srcDesc, tw, th, m_BloomColorFormat);
                    _BloomMipDown[0] = CreateCompatibleTexture(renderGraph, desc, m_BloomMipDownName[0], false, FilterMode.Bilinear);
                    _BloomMipUp[0] = CreateCompatibleTexture(renderGraph, desc, m_BloomMipUpName[0], false, FilterMode.Bilinear);

                    if (bloomParams.bloomFilter != BloomFilterMode.Kawase)
                    {
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
            }

            switch (m_Bloom.filter.value)
            {
                case BloomFilterMode.Dual:
                    destination = BloomDual(renderGraph, source, mipCount);
                break;
                case BloomFilterMode.Kawase:
                    destination = BloomKawase(renderGraph, source, mipCount);
                break;
                case BloomFilterMode.Gaussian: goto default;
                default:
                    destination = BloomGaussian(renderGraph, source, mipCount);
                break;
            }
        }

        TextureHandle BloomGaussian(RenderGraph renderGraph, TextureHandle source, int mipCount)
        {
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

                    var loadAction = RenderBufferLoadAction.DontCare; // Blit - always write all pixels
                    var storeAction = RenderBufferStoreAction.Store; // Blit - always read by then next Blit

                    // Prefilter
                    using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.RG_BloomPrefilter)))
                    {
                        Blitter.BlitCameraTexture(cmd, data.sourceTexture, data.bloomMipDown[0], loadAction,
                            storeAction, material, 0);
                    }

                    // Downsample - gaussian pyramid
                    // Classic two pass gaussian blur - use mipUp as a temporary target
                    //   First pass does 2x downsampling + 9-tap gaussian
                    //   Second pass does 9-tap gaussian using a 5-tap filter + bilinear filtering
                    using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.RG_BloomDownsample)))
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
                            TextureHandle lowMip =
                                (i == mipCount - 2) ? data.bloomMipDown[i + 1] : data.bloomMipUp[i + 1];
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
                // 1st mip is the prefilter.
                return mipCount == 1 ? passData.bloomMipDown[0] : passData.bloomMipUp[0];
            }
        }

        TextureHandle BloomKawase(RenderGraph renderGraph, TextureHandle source, int mipCount)
        {
            using (var builder = renderGraph.AddUnsafePass<BloomPassData>("Blit Bloom Mipmaps (Kawase)", out var passData, ProfilingSampler.Get(URPProfileId.Bloom)))
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
                builder.UseTexture(_BloomMipDown[0], AccessFlags.ReadWrite);
                builder.UseTexture(_BloomMipUp[0], AccessFlags.ReadWrite);

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

                    // Kawase blur passes
                    using(new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.RG_BloomDownsample)))
                    {
                        for (int i = 0; i < mipCount; i++)
                        {
                            TextureHandle src = ((i & 1) == 0) ? data.bloomMipDown[0] : data.bloomMipUp[0];
                            TextureHandle dst = ((i & 1) == 0) ? data.bloomMipUp[0] : data.bloomMipDown[0];
                            Material mat = data.upsampleMaterials[i];

                            Blitter.BlitCameraTexture(cmd, src, dst, loadAction, storeAction, mat, 4);
                        }
                    }
                });
                return (((mipCount - 1) & 1) == 0) ? _BloomMipUp[0] : _BloomMipDown[0];
            }
        }


        //  Dual Filter, Bandwidth-Efficient Rendering, siggraph2015
        TextureHandle BloomDual(RenderGraph renderGraph, TextureHandle source, int mipCount)
        {
            using (var builder = renderGraph.AddUnsafePass<BloomPassData>("Blit Bloom Mipmaps (Dual)", out var passData, ProfilingSampler.Get(URPProfileId.Bloom)))
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

                    // ARM: Bandwidth-Efficient Rendering, siggraph2015
                    // Downsample - dual pyramid, fixed Kawase0 blur on shrinking targets.
                    using(new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.RG_BloomDownsample)))
                    {
                        TextureHandle lastDown = data.bloomMipDown[0];
                        for (int i = 1; i < mipCount; i++)
                        {
                            TextureHandle src = data.bloomMipDown[i - 1];
                            TextureHandle dst = data.bloomMipDown[i];

                            Blitter.BlitCameraTexture(cmd, src, dst, loadAction, storeAction, material, 5);
                        }
                    }

                    using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.RG_BloomUpsample)))
                    {
                        for (int i = mipCount - 2; i >= 0; i--)
                        {
                            TextureHandle src = (i == mipCount - 2) ? data.bloomMipDown[i + 1] : data.bloomMipUp[i + 1];
                            TextureHandle dst = data.bloomMipUp[i];

                            Blitter.BlitCameraTexture(cmd, src, dst, loadAction, storeAction, material, 6);
                        }
                    }
                });
                // 1st mip is the prefilter.
                return mipCount == 1 ? passData.bloomMipDown[0] : passData.bloomMipUp[0];
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
            var pingTextureDesc = GetCompatibleDescriptor(srcDesc, wh, hh, m_GaussianDoFColorFormat);
            var pingTexture = CreateCompatibleTexture(renderGraph, pingTextureDesc, "_PingTexture", true, FilterMode.Bilinear);
            var pongTextureDesc = GetCompatibleDescriptor(srcDesc, wh, hh, m_GaussianDoFColorFormat);
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
                        dofMat.SetTexture(ShaderConstants._CameraDepthTextureID, data.depthTexture);
                        Blitter.BlitCameraTexture(cmd, data.sourceTexture, data.fullCoCTexture, data.materialCoC, Constants.k_GaussianDoFPassComputeCoc);
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
                        Blitter.BlitTexture(cmd, data.sourceTexture, viewportScale, dofMat, Constants.k_GaussianDoFPassDownscalePrefilter);
                    }

                    // Blur H
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_DOFBlurH)))
                    {
                        dofMat.SetTexture(ShaderConstants._HalfCoCTexture, data.halfCoCTexture);
                        Blitter.BlitCameraTexture(cmd, data.pingTexture, data.pongTexture, dofMat, Constants.k_GaussianDoFPassBlurH);
                    }

                    // Blur V
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_DOFBlurV)))
                    {
                        Blitter.BlitCameraTexture(cmd, data.pongTexture, data.pingTexture, dofMat, Constants.k_GaussianDoFPassBlurV);
                    }

                    // Composite
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_DOFComposite)))
                    {
                        dofMat.SetTexture(ShaderConstants._ColorTexture, data.pingTexture);
                        dofMat.SetTexture(ShaderConstants._FullCoCTexture, data.fullCoCTexture);
                        Blitter.BlitCameraTexture(cmd, sourceTextureHdl, dstHdl, dofMat, Constants.k_GaussianDoFPassComposite);
                    }
                });
            }
        }

        // NOTE: Duplicate in compatibility mode
        void PrepareBokehKernel(float maxRadius, float rcpAspect)
        {
            const int kRings = 4;
            const int kPointsPerRing = 7;

            // Check the existing array
            if (m_BokehKernel == null)
                m_BokehKernel = new Vector4[42];

            // Fill in sample points (concentric circles transformed to rotated N-Gon)
            int idx = 0;
            float bladeCount = m_DepthOfField.bladeCount.value;
            float curvature = 1f - m_DepthOfField.bladeCurvature.value;
            float rotation = m_DepthOfField.bladeRotation.value * Mathf.Deg2Rad;
            const float PI = Mathf.PI;
            const float TWO_PI = Mathf.PI * 2f;

            for (int ring = 1; ring < kRings; ring++)
            {
                float bias = 1f / kPointsPerRing;
                float radius = (ring + bias) / (kRings - 1f + bias);
                int points = ring * kPointsPerRing;

                for (int point = 0; point < points; point++)
                {
                    // Angle on ring
                    float phi = 2f * PI * point / points;

                    // Transform to rotated N-Gon
                    // Adapted from "CryEngine 3 Graphics Gems" [Sousa13]
                    float nt = Mathf.Cos(PI / bladeCount);
                    float dt = Mathf.Cos(phi - (TWO_PI / bladeCount) * Mathf.Floor((bladeCount * phi + Mathf.PI) / TWO_PI));
                    float r = radius * Mathf.Pow(nt / dt, curvature);
                    float u = r * Mathf.Cos(phi - rotation);
                    float v = r * Mathf.Sin(phi - rotation);

                    float uRadius = u * maxRadius;
                    float vRadius = v * maxRadius;
                    float uRadiusPowTwo = uRadius * uRadius;
                    float vRadiusPowTwo = vRadius * vRadius;
                    float kernelLength = Mathf.Sqrt((uRadiusPowTwo + vRadiusPowTwo));
                    float uRCP = uRadius * rcpAspect;

                    m_BokehKernel[idx] = new Vector4(uRadius, vRadius, kernelLength, uRCP);
                    idx++;
                }
            }
        }

        // NOTE: Duplicate in compatibility mode
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static float GetMaxBokehRadiusInPixels(float viewportHeight)
        {
            // Estimate the maximum radius of bokeh (empirically derived from the ring count)
            const float kRadiusInPixels = 14f;
            return Mathf.Min(0.05f, kRadiusInPixels / viewportHeight);
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
                        dofMat.SetTexture(ShaderConstants._CameraDepthTextureID, data.depthTexture);
                        Blitter.BlitCameraTexture(cmd, sourceTextureHdl, data.fullCoCTexture, dofMat, Constants.k_BokehDoFPassComputeCoc);
                    }

                    // Downscale and Prefilter Color + CoC
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_DOFDownscalePrefilter)))
                    {
                        dofMat.SetTexture(ShaderConstants._FullCoCTexture, data.fullCoCTexture);
                        Blitter.BlitCameraTexture(cmd, sourceTextureHdl, data.pingTexture, dofMat, Constants.k_BokehDoFPassDownscalePrefilter);
                    }

                    // Blur
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_DOFBlurBokeh)))
                    {
                        Blitter.BlitCameraTexture(cmd, data.pingTexture, data.pongTexture, dofMat, Constants.k_BokehDoFPassBlur);
                    }

                    // Post Filtering
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_DOFPostFilter)))
                    {
                        Blitter.BlitCameraTexture(cmd, data.pongTexture, data.pingTexture, dofMat, Constants.k_BokehDoFPassPostFilter);
                    }

                    // Composite
                    using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_DOFComposite)))
                    {
                        dofMat.SetTexture(ShaderConstants._DofTexture, data.pingTexture);
                        Blitter.BlitCameraTexture(cmd, sourceTextureHdl, dst, dofMat, Constants.k_BokehDoFPassComposite);
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

        // NOTE: Duplicate in compatibility mode
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector2 CalcViewExtents(Camera camera, int width, int height)
        {
            float fovY = camera.fieldOfView * Mathf.Deg2Rad;
            float aspect = width / (float)height;

            float viewExtY = Mathf.Tan(0.5f * fovY);
            float viewExtX = aspect * viewExtY;

            return new Vector2(viewExtX, viewExtY);
        }

        // NOTE: Duplicate in compatibility mode
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static Vector2 CalcCropExtents(Camera camera, float d, int width, int height)
        {
            // given
            //    S----------- E--X-------
            //    |    `  ~.  /,´
            //    |-- ---    Q
            //    |        ,/    `
            //  1 |      ,´/       `
            //    |    ,´ /         ´
            //    |  ,´  /           ´
            //    |,`   /             ,
            //    O    /
            //    |   /               ,
            //  d |  /
            //    | /                ,
            //    |/                .
            //    P
            //    |              ´
            //    |         , ´
            //    +-    ´
            //
            // have X
            // want to find E

            float viewDist = 1f + d;

            var projPos = CalcViewExtents(camera, width, height);
            var projHyp = Mathf.Sqrt(projPos.x * projPos.x + 1f);

            float cylDistMinusD = 1f / projHyp;
            float cylDist = cylDistMinusD + d;
            var cylPos = projPos * cylDistMinusD;

            return cylPos * (viewDist / cylDist);
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
            var noiseTexture = m_Materials.resources.textures.blueNoise16LTex[frameIndex & (m_Materials.resources.textures.blueNoise16LTex.Length - 1)];

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
            var material = m_Materials.motionBlur;

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

        // NOTE: Duplicate in compatibility mode
        internal static void UpdateMotionBlurMatrices(ref Material material, Camera camera, XRPass xr)
        {
            MotionVectorsPersistentData motionData = null;

            if(camera.TryGetComponent<UniversalAdditionalCameraData>(out var additionalCameraData))
                motionData = additionalCameraData.motionVectorsPersistentData;

            if (motionData == null)
                return;

#if ENABLE_VR && ENABLE_XR_MODULE
            if (xr.enabled && xr.singlePassEnabled)
            {
                // pass maximum of 2 matrices per pass. Need to access into the matrix array
                var viewStartIndex = xr.viewCount * xr.multipassId;
                // Using motionData.stagingMatrixStereo as staging buffer to avoid allocation
                Array.Copy(motionData.previousViewProjectionStereo, viewStartIndex, motionData.stagingMatrixStereo, 0, xr.viewCount);
                material.SetMatrixArray(ShaderConstants._PrevViewProjMStereo, motionData.stagingMatrixStereo);
                Array.Copy(motionData.viewProjectionStereo, viewStartIndex, motionData.stagingMatrixStereo, 0, xr.viewCount);
                material.SetMatrixArray(ShaderConstants._ViewProjMStereo, motionData.stagingMatrixStereo);
            }
            else
#endif
            {
                int viewProjMIdx = 0;
#if ENABLE_VR && ENABLE_XR_MODULE
                if (xr.enabled)
                    viewProjMIdx = xr.multipassId * xr.viewCount;
#endif

                // TODO: These should be part of URP main matrix set. For now, we set them here for motion vector rendering.
                material.SetMatrix(ShaderConstants._PrevViewProjM, motionData.previousViewProjectionStereo[viewProjMIdx]);
                material.SetMatrix(ShaderConstants._ViewProjM, motionData.viewProjectionStereo[viewProjMIdx]);
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

        // NOTE: Duplicate in compatibility mode
        static float GetLensFlareLightAttenuation(Light light, Camera cam, Vector3 wo)
        {
            // Must always be true
            if (light != null)
            {
                switch (light.type)
                {
                    case LightType.Directional:
                        return LensFlareCommonSRP.ShapeAttenuationDirLight(light.transform.forward, cam.transform.forward);
                    case LightType.Point:
                        return LensFlareCommonSRP.ShapeAttenuationPointLight();
                    case LightType.Spot:
                        return LensFlareCommonSRP.ShapeAttenuationSpotConeLight(light.transform.forward, wo, light.spotAngle, light.innerSpotAngle / 180.0f);
                    default:
                        return 1.0f;
                }
            }

            return 1.0f;
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

        public TextureHandle RenderLensFlareScreenSpace(RenderGraph renderGraph, Camera camera, in TextureDesc srcDesc, TextureHandle originalBloomTexture, TextureHandle screenSpaceLensFlareBloomMipTexture, bool sameBloomInputOutputTex)
        {
            var downsample = (int) m_LensFlareScreenSpace.resolution.value;

            int flareRenderWidth = Math.Max( srcDesc.width / downsample, 1);
            int flareRenderHeight = Math.Max( srcDesc.height / downsample, 1);

            var streakTextureDesc = GetCompatibleDescriptor(srcDesc, flareRenderWidth, flareRenderHeight, m_LensFlareScreenSpaceColorFormat);
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
                // Input/Output can be the same texture. There's a temp texture in between. Avoid RG double write error.
                if(!sameBloomInputOutputTex)
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

                builder.AllowGlobalStateModification(true);
                builder.AllowPassCulling(false);

                Material material = m_Materials.scalingSetup;
                material.shaderKeywords = null;

                material.shaderKeywords = null;

                if (settings.isFxaaEnabled)
                    CoreUtils.SetKeyword(material, ShaderKeywordStrings.Fxaa, settings.isFxaaEnabled);

                if (settings.isFsrEnabled)
                    CoreUtils.SetKeyword(material, (settings.hdrOperations.HasFlag(HDROutputUtils.Operation.ColorEncoding) ? ShaderKeywordStrings.Gamma20AndHDRInput : ShaderKeywordStrings.Gamma20), true);

                if (settings.hdrOperations.HasFlag(HDROutputUtils.Operation.ColorEncoding))
                    SetupHDROutput(cameraData.hdrDisplayInformation, cameraData.hdrDisplayColorGamut, material, settings.hdrOperations, cameraData.rendersOverlayUI);

                if (settings.isAlphaOutputEnabled)
                    CoreUtils.SetKeyword(material, ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT, settings.isAlphaOutputEnabled);

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
                builder.AllowPassCulling(false);
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
                    builder.SetExtendedFeatureFlags(ExtendedFeatureFlags.MultiviewRenderRegionsCompatible);
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

                    CoreUtils.SetKeyword(material, ShaderKeywordStrings.Fxaa, isFxaaEnabled);

                    if (isFsrEnabled)
                    {
                        // RCAS
                        // Use the override value if it's available, otherwise use the default.
                        float sharpness = data.cameraData.fsrOverrideSharpness ? data.cameraData.fsrSharpness : FSRUtils.kDefaultSharpnessLinear;

                        // Set up the parameters for the RCAS pass unless the sharpness value indicates that it wont have any effect.
                        if (data.cameraData.fsrSharpness > 0.0f)
                        {
                            // RCAS is performed during the final post blit, but we set up the parameters here for better logical grouping.
                            CoreUtils.SetKeyword(material, (requireHDROutput ? ShaderKeywordStrings.EasuRcasAndHDRInput : ShaderKeywordStrings.Rcas), true);
                            FSRUtils.SetRcasConstantsLinear(cmd, sharpness);
                        }
                    }
                    else if (isRcasEnabled)   // RCAS only
                    {
                        // Reuse RCAS as a standalone sharpening filter for TAA.
                        // If FSR is enabled then it overrides the sharpening/TAA setting and we skip it.
                        CoreUtils.SetKeyword(material, ShaderKeywordStrings.Rcas, true);
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
            m_EnableColorEncodingIfNeeded = enableColorEncodingIfNeeded;

            if (m_FilmGrain.IsActive())
            {
                material.EnableKeyword(ShaderKeywordStrings.FilmGrain);
                PostProcessUtils.ConfigureFilmGrain(
                    m_Materials.resources,
                    m_FilmGrain,
                    upscaledDesc.width, upscaledDesc.height,
                    material
                );
            }

            if (cameraData.isDitheringEnabled)
            {
                material.EnableKeyword(ShaderKeywordStrings.Dithering);
                m_DitheringTextureIndex = PostProcessUtils.ConfigureDithering(
                    m_Materials.resources,
                    m_DitheringTextureIndex,
                    upscaledDesc.width, upscaledDesc.height,
                    material
                );
            }

            if (RequireSRGBConversionBlitToBackBuffer(cameraData.requireSrgbConversion))
                CoreUtils.SetKeyword(material, ShaderKeywordStrings.LinearToSRGBConversion, true);

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
            DebugHandler debugHandler = ScriptableRenderPass.GetActiveDebugHandler(cameraData);
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
            settings.isTaaSharpeningEnabled = (cameraData.IsTemporalAAEnabled() && cameraData.taaSettings.contrastAdaptiveSharpening > 0.0f) && !settings.isFsrEnabled && !cameraData.IsSTPEnabled() && 
#if ENABLE_UPSCALER_FRAMEWORK
                cameraData.upscalingFilter != ImageUpscalingFilter.IUpscaler
#else
                true
#endif
                ;

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
                    builder.SetExtendedFeatureFlags(ExtendedFeatureFlags.MultiviewRenderRegionsCompatible);
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
                passData.userLutTexture = userLutTexture; // This can be null if ColorLookup is not active.
                if (userLutTexture.IsValid())
                    builder.UseTexture(userLutTexture, AccessFlags.Read);

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
                        CoreUtils.SetKeyword(material, ShaderKeywordStrings.HDRGrading, true);
                    }
                    else
                    {
                        switch (data.toneMappingMode)
                        {
                            case TonemappingMode.Neutral: CoreUtils.SetKeyword(material, ShaderKeywordStrings.TonemapNeutral, true); break;
                            case TonemappingMode.ACES: CoreUtils.SetKeyword(material, ShaderKeywordStrings.TonemapACES, true); break;
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

            // Temporal Anti Aliasing / Upscaling
#if ENABLE_UPSCALER_FRAMEWORK
            if (useTemporalAA && postProcessingData.activeUpscaler != null)
            {
                // Create a context item containing upscaling inputs
                UpscalingIO io = frameData.Create<UpscalingIO>();
                io.cameraColor = currentSource;
                io.cameraDepth = resourceData.cameraDepth;
                io.motionVectorColor = resourceData.motionVectorColor;
                io.motionVectorDomain = UpscalingIO.MotionVectorDomain.NDC;
                io.motionVectorDirection = UpscalingIO.MotionVectorDirection.PreviousFrameToCurrentFrame;
                io.jitteredMotionVectors = false; // URP has no jittering in MVs
                // io.exposureTexture; // TODO: set exposure texture when available
                io.preExposureValue = 1.0f; // TODO: set if exposure value is pre-multiplied
                io.hdrDisplayInformation = cameraData.isHDROutputActive ? cameraData.hdrDisplayInformation : new HDROutputUtils.HDRDisplayInformation(-1, -1, -1, 160.0f);
                io.preUpscaleResolution = new Vector2Int(
                    cameraData.cameraTargetDescriptor.width,
                    cameraData.cameraTargetDescriptor.height
                );
                io.previousPreUpscaleResolution = io.preUpscaleResolution; // URP doesn't support Dynamic Resolution Scaling (DRS).
                io.postUpscaleResolution = new Vector2Int(cameraData.pixelWidth, cameraData.pixelHeight);
                io.motionVectorTextureSize = io.preUpscaleResolution;
                io.enableTexArray = cameraData.xr.enabled && cameraData.xr.singlePassEnabled;

                MotionVectorsPersistentData motionData = null;
                {
                    cameraData.camera.TryGetComponent<UniversalAdditionalCameraData>(out var additionalCameraData);
                    Debug.Assert(additionalCameraData != null);
                    motionData = additionalCameraData.motionVectorsPersistentData;
                    Debug.Assert(motionData != null);
                }
                io.cameraInstanceID = cameraData.camera.GetInstanceID();
                io.nearClipPlane = cameraData.camera.nearClipPlane;
                io.farClipPlane = cameraData.camera.farClipPlane;
                io.fieldOfViewDegrees = cameraData.camera.fieldOfView;
                io.invertedDepth = SystemInfo.usesReversedZBuffer;
                io.flippedY = SystemInfo.graphicsUVStartsAtTop;
                io.flippedX = false;
                io.hdrInput = GraphicsFormatUtility.IsHDRFormat(currentSource.GetDescriptor(renderGraph).format);
                io.numActiveViews = cameraData.xr.enabled ? cameraData.xr.viewCount : 1;
                io.eyeIndex = (cameraData.xr.enabled && !cameraData.xr.singlePassEnabled) ? cameraData.xr.multipassId : 0;
                io.worldSpaceCameraPositions = new Vector3[io.numActiveViews];
                io.previousWorldSpaceCameraPositions = new Vector3[io.numActiveViews];
                io.previousPreviousWorldSpaceCameraPositions = new Vector3[io.numActiveViews];
                for (int i = 0; i < io.numActiveViews; i++)
                {
                    io.worldSpaceCameraPositions[i] = motionData.worldSpaceCameraPos;
                    io.previousWorldSpaceCameraPositions[i] = motionData.previousWorldSpaceCameraPos;
                    io.previousPreviousWorldSpaceCameraPositions[i] = motionData.previousPreviousWorldSpaceCameraPos;
                }
                io.projectionMatrices = motionData.projectionStereo;
                io.previousProjectionMatrices = motionData.previousProjectionStereo;
                io.previousPreviousProjectionMatrices = motionData.previousPreviousProjectionStereo;
                io.viewMatrices = motionData.viewStereo;
                io.previousViewMatrices = motionData.previousViewStereo;
                io.previousPreviousViewMatrices = motionData.previousPreviousViewStereo;
                io.resetHistory = cameraData.resetHistory;
                // TODO (Apoorva): Maybe we want to support this?
                // URP supports adding an offset value to the TAA frame index for testing determinism as follows:
                //     io.frameIndex = Time.frameCount + settings.jitterFrameCountOffset;
                io.frameIndex = Time.frameCount;
                io.deltaTime = motionData.deltaTime;
                io.previousDeltaTime = motionData.lastDeltaTime;
                io.blueNoiseTextureSet = m_Materials.resources.textures.blueNoise16LTex;

                // The motion scaling feature is only active outside of test environments. If we allowed it to run
                // during automated graphics tests, the results of each test run would be dependent on system
                // performance.
#if LWRP_DEBUG_STATIC_POSTFX
                io.enableMotionScaling = false;
#else
                io.enableMotionScaling = true;
#endif
                io.enableHwDrs = false; // URP doesn't support hardware dynamic resolution scaling
                // Insert the active upscaler's render graph passes
                postProcessingData.activeUpscaler.RecordRenderGraph(renderGraph, frameData);

                // Update the camera resolution to reflect the upscaled size
                var desc = io.cameraColor.GetDescriptor(renderGraph);
                UpdateCameraResolution(renderGraph, cameraData, new Vector2Int(desc.width, desc.height));

                // Use the output texture of upscaling
                currentSource = io.cameraColor;
            }
            else
#endif
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
                        // We need to take into account how many valid mips the bloom pass produced.
                        int bloomMipCount = CalcBloomMipCount(m_Bloom, CalcBloomResolution(m_Bloom, in srcDesc));
                        int maxBloomMip = Mathf.Clamp(bloomMipCount - 1, 0, m_Bloom.maxIterations.value / 2);
                        int useBloomMip = Mathf.Clamp(m_LensFlareScreenSpace.bloomMip.value, 0, maxBloomMip);

                        TextureHandle bloomMipFlareSource = _BloomMipUp[useBloomMip];
                        bool sameBloomInputOutputTex = false;
                        if(useBloomMip == 0)
                        {
                            // Hierarchical blooms do only the prefilter if there's only 1 mip.
                            if (bloomMipCount == 1 && m_Bloom.filter != BloomFilterMode.Kawase)
                                bloomMipFlareSource = _BloomMipDown[0];

                            // Flare source and Flare target is the same texture. BloomMip[0]
                            sameBloomInputOutputTex = true; 
                        }

                        // Kawase blur does not use the mip pyramid.
                        // It is safe to pass the same texture to both input/output.
                        if (m_Bloom.filter.value == BloomFilterMode.Kawase)
                        {
                            bloomMipFlareSource = bloomTexture;
                            sameBloomInputOutputTex = true;
                        }

                        bloomTexture = RenderLensFlareScreenSpace(renderGraph, cameraData.camera, srcDesc, bloomTexture, bloomMipFlareSource, sameBloomInputOutputTex);
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
                    CoreUtils.SetKeyword(m_Materials.uber, ShaderKeywordStrings.LinearToSRGBConversion, true);

                if (m_UseFastSRGBLinearConversion)
                {
                    CoreUtils.SetKeyword(m_Materials.uber, ShaderKeywordStrings.UseFastSRGBLinearConversion, true);
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

                DebugHandler debugHandler = ScriptableRenderPass.GetActiveDebugHandler(cameraData);
                debugHandler?.UpdateShaderGlobalPropertiesForFinalValidationPass(renderGraph, cameraData, !m_HasFinalPass && !resolveToDebugScreen);

                RenderUberPost(renderGraph, frameData, cameraData, postProcessingData, in currentSource, in postProcessingTarget, in lutTexture, in bloomTexture, in overlayUITexture, requireHDROutput, enableAlphaOutput, resolveToDebugScreen, hasFinalPass);
            }
        }

#region Lens Distortion
        // NOTE: Duplicate in compatibility mode
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SetupLensDistortion(Material material, bool isSceneView)
        {
            float amount = 1.6f * Mathf.Max(Mathf.Abs(m_LensDistortion.intensity.value * 100f), 1f);
            float theta = Mathf.Deg2Rad * Mathf.Min(160f, amount);
            float sigma = 2f * Mathf.Tan(theta * 0.5f);
            var center = m_LensDistortion.center.value * 2f - Vector2.one;
            var p1 = new Vector4(
                center.x,
                center.y,
                Mathf.Max(m_LensDistortion.xMultiplier.value, 1e-4f),
                Mathf.Max(m_LensDistortion.yMultiplier.value, 1e-4f)
            );
            var p2 = new Vector4(
                m_LensDistortion.intensity.value >= 0f ? theta : 1f / theta,
                sigma,
                1f / m_LensDistortion.scale.value,
                m_LensDistortion.intensity.value * 100f
            );

            material.SetVector(ShaderConstants._Distortion_Params1, p1);
            material.SetVector(ShaderConstants._Distortion_Params2, p2);

            if (m_LensDistortion.IsActive() && !isSceneView)
                material.EnableKeyword(ShaderKeywordStrings.Distortion);
        }

#endregion

#region Chromatic Aberration
        // NOTE: Duplicate in compatibility mode
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SetupChromaticAberration(Material material)
        {
            material.SetFloat(ShaderConstants._Chroma_Params, m_ChromaticAberration.intensity.value * 0.05f);

            if (m_ChromaticAberration.IsActive())
                material.EnableKeyword(ShaderKeywordStrings.ChromaticAberration);
        }

#endregion

#region Vignette
        // NOTE: Duplicate in compatibility mode
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SetupVignette(Material material, XRPass xrPass, int width, int height)
        {
            var color = m_Vignette.color.value;
            var center = m_Vignette.center.value;
            var aspectRatio = width / (float)height;


#if ENABLE_VR && ENABLE_XR_MODULE
            if (xrPass != null && xrPass.enabled)
            {
                if (xrPass.singlePassEnabled)
                    material.SetVector(ShaderConstants._Vignette_ParamsXR, xrPass.ApplyXRViewCenterOffset(center));
                else
                    // In multi-pass mode we need to modify the eye center with the values from .xy of the corrected
                    // center since the version of the shader that is not single-pass will use the value in _Vignette_Params2
                    center = xrPass.ApplyXRViewCenterOffset(center);
            }
#endif

            var v1 = new Vector4(
                color.r, color.g, color.b,
                m_Vignette.rounded.value ? aspectRatio : 1f
            );
            var v2 = new Vector4(
                center.x, center.y,
                m_Vignette.intensity.value * 3f,
                m_Vignette.smoothness.value * 5f
            );

            material.SetVector(ShaderConstants._Vignette_Params1, v1);
            material.SetVector(ShaderConstants._Vignette_Params2, v2);
        }

#endregion

#region Film Grain

        // NOTE: Duplicate in compatibility mode
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SetupGrain(UniversalCameraData cameraData, Material material)
        {
            if (!m_HasFinalPass && m_FilmGrain.IsActive())
            {
                material.EnableKeyword(ShaderKeywordStrings.FilmGrain);
                PostProcessUtils.ConfigureFilmGrain(
                    m_Materials.resources,
                    m_FilmGrain,
                    cameraData.pixelWidth, cameraData.pixelHeight,
                    material
                );
            }
        }

#endregion

#region 8-bit Dithering

        // NOTE: Duplicate in compatibility mode
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SetupDithering(UniversalCameraData cameraData, Material material)
        {
            if (!m_HasFinalPass && cameraData.isDitheringEnabled)
            {
                material.EnableKeyword(ShaderKeywordStrings.Dithering);
                m_DitheringTextureIndex = PostProcessUtils.ConfigureDithering(
                    m_Materials.resources,
                    m_DitheringTextureIndex,
                    cameraData.pixelWidth, cameraData.pixelHeight,
                    material
                );
            }
        }

#endregion

#region HDR Output
        // NOTE: Duplicate in compatibility mode
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void SetupHDROutput(HDROutputUtils.HDRDisplayInformation hdrDisplayInformation, ColorGamut hdrDisplayColorGamut, Material material, HDROutputUtils.Operation hdrOperations, bool rendersOverlayUI)
        {
            Vector4 hdrOutputLuminanceParams;
            UniversalRenderPipeline.GetHDROutputLuminanceParameters(hdrDisplayInformation, hdrDisplayColorGamut, m_Tonemapping, out hdrOutputLuminanceParams);
            material.SetVector(ShaderPropertyId.hdrOutputLuminanceParams, hdrOutputLuminanceParams);

            HDROutputUtils.ConfigureHDROutput(material, hdrDisplayColorGamut, hdrOperations);
            CoreUtils.SetKeyword(material, ShaderKeywordStrings.HDROverlay, rendersOverlayUI);
        }
#endregion
    }

    // TODO: move into post-process passes.
    internal class PostProcessMaterialLibrary
    {
        public readonly Material stopNaN;
        public readonly Material subpixelMorphologicalAntialiasing;
        public readonly Material gaussianDepthOfField;
        public readonly Material gaussianDepthOfFieldCoC;
        public readonly Material bokehDepthOfField;
        public readonly Material bokehDepthOfFieldCoC;
        public readonly Material temporalAntialiasing;
        public readonly Material motionBlur;
        public readonly Material paniniProjection;
        public readonly Material bloom;
        public readonly Material[] bloomUpsample;
        public readonly Material lensFlareScreenSpace;
        public readonly Material lensFlareDataDriven;
        public readonly Material uber;
        public readonly Material scalingSetup;
        public readonly Material easu;
        public readonly Material finalPass;

        internal PostProcessData m_Resources;
        public PostProcessData resources => m_Resources;

        public PostProcessMaterialLibrary(PostProcessData data)
        {
            // NOTE NOTE NOTE NOTE NOTE NOTE
            // If you create something here you must also destroy it in Cleanup()
            // or it will leak during enter/leave play mode cycles
            // NOTE NOTE NOTE NOTE NOTE NOTE
            stopNaN = Load(data.shaders.stopNanPS);
            subpixelMorphologicalAntialiasing = Load(data.shaders.subpixelMorphologicalAntialiasingPS);
            gaussianDepthOfField = Load(data.shaders.gaussianDepthOfFieldPS);
            gaussianDepthOfFieldCoC = Load(data.shaders.gaussianDepthOfFieldPS);
            bokehDepthOfField = Load(data.shaders.bokehDepthOfFieldPS);
            bokehDepthOfFieldCoC = Load(data.shaders.bokehDepthOfFieldPS);
            temporalAntialiasing = Load(data.shaders.temporalAntialiasingPS);
            motionBlur = Load(data.shaders.cameraMotionBlurPS);
            paniniProjection = Load(data.shaders.paniniProjectionPS);
            bloom = Load(data.shaders.bloomPS);
            lensFlareScreenSpace = Load(data.shaders.LensFlareScreenSpacePS);
            lensFlareDataDriven = Load(data.shaders.LensFlareDataDrivenPS);
            uber = Load(data.shaders.uberPostPS);
            scalingSetup = Load(data.shaders.scalingSetupPS);
            easu = Load(data.shaders.easuPS);
            finalPass = Load(data.shaders.finalPostPassPS);

            bloomUpsample = new Material[PostProcessPassRenderGraph.Constants.k_MaxPyramidSize];
            for (uint i = 0; i < PostProcessPassRenderGraph.Constants.k_MaxPyramidSize; ++i)
                bloomUpsample[i] = Load(data.shaders.bloomPS);

            m_Resources = data;
        }

        Material Load(Shader shader)
        {
            if (shader == null)
            {
                Debug.LogErrorFormat($"Missing shader. PostProcessing render passes will not execute. Check for missing reference in the renderer resources.");
                return null;
            }
            else if (!shader.isSupported)
            {
                return null;
            }

            return CoreUtils.CreateEngineMaterial(shader);
        }

        internal void Cleanup()
        {
            CoreUtils.Destroy(stopNaN);
            CoreUtils.Destroy(subpixelMorphologicalAntialiasing);
            CoreUtils.Destroy(gaussianDepthOfField);
            CoreUtils.Destroy(gaussianDepthOfFieldCoC);
            CoreUtils.Destroy(bokehDepthOfField);
            CoreUtils.Destroy(bokehDepthOfFieldCoC);
            CoreUtils.Destroy(temporalAntialiasing);
            CoreUtils.Destroy(motionBlur);
            CoreUtils.Destroy(paniniProjection);
            CoreUtils.Destroy(bloom);
            CoreUtils.Destroy(lensFlareScreenSpace);
            CoreUtils.Destroy(lensFlareDataDriven);
            CoreUtils.Destroy(scalingSetup);
            CoreUtils.Destroy(uber);
            CoreUtils.Destroy(easu);
            CoreUtils.Destroy(finalPass);

            for (uint i = 0; i < PostProcessPassRenderGraph.Constants.k_MaxPyramidSize; ++i)
                CoreUtils.Destroy(bloomUpsample[i]);
        }
    }
}
