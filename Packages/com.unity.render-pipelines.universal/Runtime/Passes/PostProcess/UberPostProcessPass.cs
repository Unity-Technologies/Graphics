using System;
using UnityEngine.Rendering.RenderGraphModule;
using System.Runtime.CompilerServices; // AggressiveInlining

namespace UnityEngine.Rendering.Universal
{
    internal sealed class UberPostProcessPass : PostProcessPass
    {
        Material m_Material;
        Texture2D[] m_FilmGrainTextures;

        Texture m_DitherTexture;
        RTHandle m_UserLut;
        HDROutputUtils.Operation m_HdrOperations;
        bool m_IsValid;
        bool m_IsFinalPass;
        bool m_RequireSRGBConversionBlit;
        bool m_UseFastSRGBLinearConversion;
        bool m_RenderOverlayUI;

        public UberPostProcessPass(Shader shader, Texture2D[] filmGrainTextures)
        {
            this.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing - 1;
            this.profilingSampler = new ProfilingSampler("Blit Post Processing");

            m_Material = PostProcessUtils.LoadShader(shader, passName);
            m_IsValid = m_Material != null;
            m_FilmGrainTextures = filmGrainTextures;
        }

        public override void Dispose()
        {
            m_UserLut?.Release();
            CoreUtils.Destroy(m_Material);
            m_IsValid = false;
        }

        public void Setup(Texture ditherTexture,
            HDROutputUtils.Operation hdrOperations,
            bool requireSRGBConversionBlit,
            bool useFastSRGBLinearConversion,
            bool isFinalPass,
            bool renderOverlayUI)
        {
            m_DitherTexture = ditherTexture;
            m_HdrOperations = hdrOperations;
            m_RequireSRGBConversionBlit = requireSRGBConversionBlit;
            m_UseFastSRGBLinearConversion = useFastSRGBLinearConversion;
            m_IsFinalPass = isFinalPass;
            m_RenderOverlayUI = renderOverlayUI;
        }

        private class UberPostPassData
        {
            internal TextureHandle destinationTexture;
            internal TextureHandle sourceTexture;
            internal TextureHandle internalLutTexture;

            internal Material material; // NOTE: material is a ref, pass instance is not re-entrant within a frame!
            internal UniversalCameraData cameraData;

            internal Tonemapping tonemapping;
            internal HDROutputUtils.Operation hdrOperations;
            internal bool isHdrGrading;

            internal LutParams lut;
            internal BloomParams bloom;
            internal LensDistortionParams lensDistortion;
            internal ChromaticAberrationParams chromaticAberration;
            internal VignetteParams vignette;
            internal FilmGrainParams filmGrain;
            internal DitheringParams dither;

            internal bool isFinalPass;
            internal bool useFastSRGBLinearConversion;
            internal bool requireSRGBConversionBlit;
        }

        const string _CameraColorAfterPostProcessingName = "_CameraColorAfterPostProcessing";

        /// <inheritdoc />
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            if(!m_IsValid)
                return;

            var colorLookup = volumeStack.GetComponent<ColorLookup>();
            var colorAdjustments = volumeStack.GetComponent<ColorAdjustments>();
            var tonemapping = volumeStack.GetComponent<Tonemapping>();
            var bloom = volumeStack.GetComponent<Bloom>();
            var lensDistortion = volumeStack.GetComponent<LensDistortion>();
            var chromaticAberration = volumeStack.GetComponent<ChromaticAberration>();
            var vignette = volumeStack.GetComponent<Vignette>();
            var filmGrain = volumeStack.GetComponent<FilmGrain>();

            var cameraData = frameData.Get<UniversalCameraData>();
            var postProcessingData = frameData.Get<UniversalPostProcessingData>();
            var resourceData = frameData.Get<UniversalResourceData>();

            var sourceTexture = resourceData.cameraColor;
            var overlayUITexture = resourceData.overlayUITexture;
            var internalColorLut = resourceData.internalColorLut;
            var userColorLut = TryGetCachedUserLutTextureHandle(renderGraph, colorLookup);
            var bloomTexture = resourceData.bloom;

            TextureHandle destinationTexture;
            if (resourceData.isActiveTargetBackBuffer)
            {
                destinationTexture = resourceData.backBufferColor;
            }
            else
            {
                //Due to camera stacking we could be rendering to a persistent texture that is not the backbuffer
                destinationTexture = resourceData.destinationCameraColor.IsValid() ?
                    resourceData.destinationCameraColor
                    //TODO here we don't seem to apply PostProcessUtils.CreateCompatibleTexture. This seems completely out of sync with the other post process passes.
                    //However, changing it, breaks some tests because rendering with the color and depth after this pass when MSAA is on leads to mismatch.
                    //It seems completely arbitrary that we continue to write out color with MSAA if no other pp pass has been applied earlier.
                    : renderGraph.CreateTexture(sourceTexture, _CameraColorAfterPostProcessingName);
                resourceData.destinationCameraColor = TextureHandle.nullHandle;
            }

            using (var builder = renderGraph.AddRasterRenderPass<UberPostPassData>(passName, out var passData, profilingSampler))
            {
                var srcDesc = sourceTexture.GetDescriptor(renderGraph);

#if ENABLE_VR && ENABLE_XR_MODULE
                if (cameraData.xr.enabled)
                {
                    bool passSupportsFoveation = cameraData.xrUniversal.canFoveateIntermediatePasses || resourceData.isActiveTargetBackBuffer;
                    // This is a screen-space pass, make sure foveated rendering is disabled for non-uniform renders
                    passSupportsFoveation &= !Experimental.Rendering.XRSystem.foveatedRenderingCaps.HasFlag(FoveatedRenderingCaps.NonUniformRaster);
                    builder.EnableFoveatedRasterization(cameraData.xr.supportsFoveatedRendering && passSupportsFoveation);

                    // Apply MultiviewRenderRegionsCompatible flag only to the peripheral view in Quad Views
                    if (cameraData.xr.multipassId == 0)
                    {
                        builder.SetExtendedFeatureFlags(ExtendedFeatureFlags.MultiviewRenderRegionsCompatible);
                    }
                }
#endif
                builder.AllowGlobalStateModification(true);
                passData.destinationTexture = destinationTexture;
                builder.SetRenderAttachment(destinationTexture, 0, AccessFlags.Write);
                passData.sourceTexture = sourceTexture;
                builder.UseTexture(sourceTexture, AccessFlags.Read);

                if(m_RenderOverlayUI)
                    builder.UseTexture(overlayUITexture, AccessFlags.Read);

                builder.UseTexture(internalColorLut, AccessFlags.Read);
                if(userColorLut.IsValid())
                    builder.UseTexture(userColorLut, AccessFlags.Read);

                if(bloomTexture.IsValid())
                    builder.UseTexture(bloomTexture, AccessFlags.Read);

                passData.material = m_Material;
                passData.cameraData = cameraData;
                passData.useFastSRGBLinearConversion = m_UseFastSRGBLinearConversion;
                passData.requireSRGBConversionBlit = m_RequireSRGBConversionBlit;

                // HDR
                passData.tonemapping = tonemapping;
                passData.hdrOperations = m_HdrOperations;
                passData.isHdrGrading = postProcessingData.gradingMode == ColorGradingMode.HighDynamicRange;

                passData.lut.Setup(colorAdjustments, colorLookup, postProcessingData.lutSize, internalColorLut, userColorLut);
                passData.bloom.Setup(bloom, in srcDesc, bloomTexture);
                passData.lensDistortion.Setup(lensDistortion, cameraData.isSceneViewCamera);
                passData.chromaticAberration.Setup(chromaticAberration);
                passData.vignette.Setup(vignette, srcDesc.width, srcDesc.height, cameraData.xr);

                // Final pass effects
                if (m_IsFinalPass)
                {
                    passData.filmGrain.Setup(filmGrain, m_FilmGrainTextures, cameraData.pixelWidth, cameraData.pixelHeight);
                    passData.dither.Setup(m_DitherTexture, cameraData.pixelWidth, cameraData.pixelHeight);
                }
                passData.isFinalPass = m_IsFinalPass;

                builder.SetRenderFunc(static (UberPostPassData data, RasterGraphContext context) =>
                {
                    var cmd = context.cmd;
                    var cameraData = data.cameraData;
                    var material = data.material;
                    RTHandle sourceTextureHdl = data.sourceTexture;

                    // Reset keywords
                    material.shaderKeywords = null;

                    data.lut.Apply(material);

                    if (data.bloom.IsActive())
                        data.bloom.Apply(material);

                    if(data.lensDistortion.IsActive())
                        data.lensDistortion.Apply(material);

                    if(data.chromaticAberration.IsActive())
                        data.chromaticAberration.Apply(material);

                    data.vignette.Apply(material, cameraData.xr);

                    if(data.filmGrain.IsActive())
                        data.filmGrain.Apply(material);

                    if(data.dither.IsActive())
                        data.dither.Apply(material);

                    if (data.requireSRGBConversionBlit)
                        material.EnableKeyword(ShaderKeywordStrings.LinearToSRGBConversion);

                    if (data.useFastSRGBLinearConversion)
                        material.EnableKeyword(ShaderKeywordStrings.UseFastSRGBLinearConversion);

                    if (cameraData.isAlphaOutputEnabled)
                        material.EnableKeyword(ShaderKeywordStrings._ENABLE_ALPHA_OUTPUT);

                    if (data.isHdrGrading)
                    {
                        material.EnableKeyword(ShaderKeywordStrings.HDRGrading);
                    }
                    else
                    {
                        switch (data.tonemapping.mode.value)
                        {
                            case TonemappingMode.Neutral: material.EnableKeyword(ShaderKeywordStrings.TonemapNeutral); break;
                            case TonemappingMode.ACES: material.EnableKeyword(ShaderKeywordStrings.TonemapACES); break;
                            default: break; // None
                        }
                    }

                    if(PostProcessUtils.RequireHDROutput(cameraData))
                        PostProcessUtils.SetupHDROutput(material, cameraData.hdrDisplayInformation, cameraData.hdrDisplayColorGamut, data.tonemapping, data.hdrOperations, cameraData.rendersOverlayUI);

                    // Done with Uber, blit it
#if ENABLE_VR && ENABLE_XR_MODULE
                    if (cameraData.xr.enabled && cameraData.xr.hasValidVisibleMesh)
                        PostProcessUtils.ScaleViewportAndDrawVisibilityMesh(context, data.sourceTexture, data.destinationTexture, data.cameraData, material, data.isFinalPass);
                    else
#endif
                        PostProcessUtils.ScaleViewportAndBlit(context, data.sourceTexture, data.destinationTexture, data.cameraData, material, data.isFinalPass);

                });
            }

            if (!resourceData.isActiveTargetBackBuffer)
            {
                resourceData.cameraColor = destinationTexture;
            }
        }

#region ColorLut
        TextureHandle TryGetCachedUserLutTextureHandle(RenderGraph renderGraph, ColorLookup colorLookup)
        {
            if (colorLookup.texture.value == null)
            {
                if (m_UserLut != null)
                {
                    m_UserLut.Release();
                    m_UserLut = null;
                }
            }
            else
            {
                if (m_UserLut == null || m_UserLut.externalTexture != colorLookup.texture.value)
                {
                    m_UserLut?.Release();
                    m_UserLut = RTHandles.Alloc(colorLookup.texture.value);
                }
            }
            return m_UserLut != null ? renderGraph.ImportTexture(m_UserLut) : TextureHandle.nullHandle;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CalcColorLutParams(ColorAdjustments colorAdjustments, ColorLookup colorLookup, int lutHeight, out Vector4 internalLutParams, out Vector4 userLutParams)
        {
            Assertions.Assert.IsNotNull(colorAdjustments, "SetupColorLut colorAdjustments cannot be null.");
            Assertions.Assert.IsNotNull(colorLookup, "SetupColorLut colorLookup cannot be null.");

            int lutWidth = lutHeight * lutHeight;

            float postExposureLinear = Mathf.Pow(2f, colorAdjustments.postExposure.value);
            internalLutParams = new Vector4(1f / lutWidth, 1f / lutHeight, lutHeight - 1f, postExposureLinear);

            userLutParams = !colorLookup.IsActive()
                ? Vector4.zero
                : new Vector4(1f / colorLookup.texture.value.width,
                    1f / colorLookup.texture.value.height,
                    colorLookup.texture.value.height - 1f,
                    colorLookup.contribution.value);
        }

        public struct LutParams
        {
            public TextureHandle internalLutTexture;
            public TextureHandle activeUserLutTexture;
            public Vector4 internalLutParams;
            public Vector4 userLutParams;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Setup(ColorAdjustments colorAdjustments, ColorLookup colorLookup, int lutHeight, TextureHandle internalLutTexture, TextureHandle activeUserLutTexture)
            {
                this.internalLutTexture = internalLutTexture;
                this.activeUserLutTexture = activeUserLutTexture;
                CalcColorLutParams(colorAdjustments, colorLookup, lutHeight, out internalLutParams, out userLutParams);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Apply(Material material)
            {
                material.SetTexture(ShaderConstants._InternalLut, internalLutTexture);
                material.SetTexture(ShaderConstants._UserLut, activeUserLutTexture);
                material.SetVector(ShaderConstants._Lut_Params, internalLutParams);
                material.SetVector(ShaderConstants._UserLut_Params, userLutParams);
            }

        }
#endregion

#region Bloom
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void CalcBloomParams(Bloom bloom, in TextureDesc srcDesc, out Vector4 bloomParams, out bool highQualityFiltering, out Texture dirtTexture, out Vector4 dirtScaleOffset, out float dirtIntensity)
        {
            using (new ProfilingScope(ProfilingSampler.Get(URPProfileId.RG_UberPostSetupBloomPass)))
            {
                // Setup bloom on uber
                var tint = bloom.tint.value.linear;
                var luma = ColorUtils.Luminance(tint);
                tint = luma > 0f ? tint * (1f / luma) : Color.white;
                bloomParams = new Vector4(bloom.intensity.value, tint.r, tint.g, tint.b);

                highQualityFiltering = bloom.highQualityFiltering.value;

                // Setup lens dirtiness on uber
                // Keep the aspect ratio correct & center the dirt texture, we don't want it to be
                // stretched or squashed
                dirtTexture = bloom.dirtTexture.value == null ? Texture2D.blackTexture : bloom.dirtTexture.value;
                float dirtRatio = dirtTexture.width / (float)dirtTexture.height;
                float screenRatio = srcDesc.width / (float)srcDesc.height;
                dirtScaleOffset = new Vector4(1f, 1f, 0f, 0f);
                dirtIntensity = bloom.dirtIntensity.value;

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
            }
        }

        public struct BloomParams
        {
            public TextureHandle activeBloomTexture;
            public Vector4 bloomParams;
            public Texture dirtTexture;
            public Vector4 dirtScaleOffset;
            public float dirtIntensity;
            public bool highQualityFiltering;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsActive()
            {
                return activeBloomTexture.IsValid();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Setup(Bloom bloom, in TextureDesc srcDesc, TextureHandle activeBloomTexture)
            {
                this.activeBloomTexture = activeBloomTexture;
                CalcBloomParams(bloom, in srcDesc, out bloomParams, out highQualityFiltering, out dirtTexture, out dirtScaleOffset, out dirtIntensity);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Apply(Material material)
            {
                material.SetTexture(ShaderConstants._Bloom_Texture, activeBloomTexture);
                material.SetVector(ShaderConstants._Bloom_Params, bloomParams);

                material.SetTexture(ShaderConstants._LensDirt_Texture, dirtTexture);
                material.SetVector(ShaderConstants._LensDirt_Params, dirtScaleOffset);
                material.SetFloat(ShaderConstants._LensDirt_Intensity, dirtIntensity);

                // Keyword setup - a bit convoluted as we're trying to save some variants in Uber...
                if (highQualityFiltering)
                    material.EnableKeyword(dirtIntensity > 0f ? ShaderKeywordStrings.BloomHQDirt : ShaderKeywordStrings.BloomHQ);
                else
                    material.EnableKeyword(dirtIntensity > 0f ? ShaderKeywordStrings.BloomLQDirt : ShaderKeywordStrings.BloomLQ);
            }
        }
#endregion

#region Lens Distortion
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void CalcLensDistortionParams(LensDistortion lensDistortion, out Vector4 lensDistortionParams1, out Vector4 lensDistortionParams2)
        {
            float amount = 1.6f * Mathf.Max(Mathf.Abs(lensDistortion.intensity.value * 100f), 1f);
            float theta = Mathf.Deg2Rad * Mathf.Min(160f, amount);
            float sigma = 2f * Mathf.Tan(theta * 0.5f);
            var center = lensDistortion.center.value * 2f - Vector2.one;
            lensDistortionParams1 = new Vector4(
                center.x,
                center.y,
                Mathf.Max(lensDistortion.xMultiplier.value, 1e-4f),
                Mathf.Max(lensDistortion.yMultiplier.value, 1e-4f)
            );
            lensDistortionParams2 = new Vector4(
                lensDistortion.intensity.value >= 0f ? theta : 1f / theta,
                sigma,
                1f / lensDistortion.scale.value,
                lensDistortion.intensity.value * 100f
            );
        }

        public struct LensDistortionParams
        {
            public Vector4 lensDistortionParams1;
            public Vector4 lensDistortionParams2;
            public bool lensDistortionActive;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsActive()
            {
                return lensDistortionActive;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Setup(LensDistortion lensDistortion, bool isSceneViewCamera)
            {
                lensDistortionActive = lensDistortion.IsActive() && !isSceneViewCamera;
                CalcLensDistortionParams(lensDistortion, out lensDistortionParams1, out lensDistortionParams2);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Apply(Material material)
            {
                material.SetVector(ShaderConstants._Distortion_Params1, lensDistortionParams1);
                material.SetVector(ShaderConstants._Distortion_Params2, lensDistortionParams2);

                material.EnableKeyword(ShaderKeywordStrings.Distortion);
            }
        }
#endregion

#region Chromatic Aberration

        public struct ChromaticAberrationParams
        {
            public float chromaticAberrationIntensity;
            public bool chromaticAberrationActive;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsActive()
            {
                return chromaticAberrationActive;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Setup(ChromaticAberration chromaticAberration)
            {
                chromaticAberrationActive = chromaticAberration.IsActive();
                chromaticAberrationIntensity = chromaticAberration.intensity.value;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Apply(Material material)
            {
                material.SetFloat(ShaderConstants._Chroma_Params, chromaticAberrationIntensity * 0.05f);

                material.EnableKeyword(ShaderKeywordStrings.ChromaticAberration);
            }
        }
#endregion

#region Vignette
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void CalcVignetteParams(Vignette vignette, int width, int height, Experimental.Rendering.XRPass xrPass, out Vector4 vignetteParams1, out Vector4 vignetteParams2)
        {
            var color = vignette.color.value;
            var center = vignette.center.value;
            var aspectRatio = width / (float)height;

#if ENABLE_VR && ENABLE_XR_MODULE
            if (xrPass != null && xrPass.enabled && !xrPass.singlePassEnabled)
            {
                // In multi-pass mode we need to modify the eye center with the values from .xy of the corrected
                // center since the version of the shader that is not single-pass will use the value in _Vignette_Params2
                center = xrPass.ApplyXRViewCenterOffset(center);
            }
#endif

            vignetteParams1 = new Vector4(
                color.r, color.g, color.b,
                vignette.rounded.value ? aspectRatio : 1f
            );
            vignetteParams2 = new Vector4(
                center.x, center.y,
                vignette.intensity.value * 3f,
                vignette.smoothness.value * 5f
            );
        }

        public struct VignetteParams
        {
            public Vector4 vignetteParams1;
            public Vector4 vignetteParams2;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Setup(Vignette vignette, int width, int height, Experimental.Rendering.XRPass xrPass)
            {
                CalcVignetteParams(vignette, width, height, xrPass, out vignetteParams1, out vignetteParams2);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Apply(Material material, Experimental.Rendering.XRPass xrPass)
            {
                material.SetVector(ShaderConstants._Vignette_Params1, vignetteParams1);
                material.SetVector(ShaderConstants._Vignette_Params2, vignetteParams2);

#if ENABLE_VR && ENABLE_XR_MODULE
                if (xrPass != null && xrPass.enabled && xrPass.singlePassEnabled)
                {
                    Vector2 center = vignetteParams2;
                    material.SetVector(ShaderConstants._Vignette_ParamsXR, xrPass.ApplyXRViewCenterOffset(center));
                }
#endif
            }
        }
#endregion

#region Film Grain
        // NOTE: Procedural FilmGrain can be done using the custom texture with RenderTexture. No need to import it into the RenderGraph.
        const float k_FilmGrainIntensityScale = 4f;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static public void CalcFilmGrainParams(FilmGrain filmGrain, Texture2D[] filmGrainTextures, out Texture grainTexture, out Vector2 grainParams)
        {
            grainTexture = (filmGrain.type.value == FilmGrainLookup.Custom) ? filmGrain.texture.value : filmGrainTextures[(int)filmGrain.type.value];
            grainParams = new Vector2(filmGrain.intensity.value * k_FilmGrainIntensityScale, filmGrain.response.value);
        }

        public struct FilmGrainParams
        {
            public Texture activeGrainTexture;
            public Vector4 tilingParams;
            public Vector2 grainParams;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsActive()
            {
                return activeGrainTexture != null;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Setup(FilmGrain filmGrain, Texture2D[] filmGrainTextures, int pixelWidth, int pixelHeight)
            {
                activeGrainTexture = null;  // Disable by default
                if (filmGrain.IsActive())
                {
                    CalcFilmGrainParams(filmGrain, filmGrainTextures, out activeGrainTexture, out grainParams);
                    tilingParams = PostProcessUtils.CalcNoiseTextureTilingParams(activeGrainTexture, pixelWidth, pixelHeight, PostProcessUtils.GetRandomOffset2D());
                }
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Apply(Material material)
            {
                PostProcessUtils.ConfigureFilmGrainMaterial(material, activeGrainTexture, grainParams, tilingParams);
                material.EnableKeyword(ShaderKeywordStrings.FilmGrain);
            }
        }
#endregion

#region 8-bit Dithering

        public struct DitheringParams
        {
            public Texture activeDitherTexture;
            public Vector4 tilingParams;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool IsActive()
            {
                return activeDitherTexture != null;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Setup(Texture ditherTexture, int pixelWidth, int pixelHeight)
            {
                activeDitherTexture = ditherTexture;
                tilingParams = PostProcessUtils.CalcNoiseTextureTilingParams(ditherTexture, pixelWidth, pixelHeight, PostProcessUtils.GetRandomOffset2D());
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void Apply(Material material)
            {
                PostProcessUtils.ConfigureDitheringMaterial(material, activeDitherTexture, tilingParams);
                material.EnableKeyword(ShaderKeywordStrings.Dithering);
            }
        }
#endregion

        // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
        internal static class ShaderConstants
        {
            // Uber
            public static readonly int _Distortion_Params1 = Shader.PropertyToID("_Distortion_Params1");
            public static readonly int _Distortion_Params2 = Shader.PropertyToID("_Distortion_Params2");
            public static readonly int _Chroma_Params = Shader.PropertyToID("_Chroma_Params");
            public static readonly int _Vignette_Params1 = Shader.PropertyToID("_Vignette_Params1");
            public static readonly int _Vignette_Params2 = Shader.PropertyToID("_Vignette_Params2");
            public static readonly int _Vignette_ParamsXR = Shader.PropertyToID("_Vignette_ParamsXR");

            // Uber Lut
            public static readonly int _InternalLut = Shader.PropertyToID("_InternalLut");
            public static readonly int _Lut_Params = Shader.PropertyToID("_Lut_Params");
            public static readonly int _UserLut = Shader.PropertyToID("_UserLut");
            public static readonly int _UserLut_Params = Shader.PropertyToID("_UserLut_Params");

            // Uber Bloom
            public static readonly int _Bloom_Texture = Shader.PropertyToID("_Bloom_Texture");
            public static readonly int _Bloom_Params = Shader.PropertyToID("_Bloom_Params");
            public static readonly int _LensDirt_Texture = Shader.PropertyToID("_LensDirt_Texture");
            public static readonly int _LensDirt_Params = Shader.PropertyToID("_LensDirt_Params");
            public static readonly int _LensDirt_Intensity = Shader.PropertyToID("_LensDirt_Intensity");
        }
    }
}
