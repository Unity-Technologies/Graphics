using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.LWRP
{
    // TODO: xmldoc
    public interface IPostProcessComponent
    {
        bool IsActive();
        bool IsTileCompatible();
    }

    // TODO: Anti-aliasing
    // TODO: Motion blur
    // TODO: Depth of Field
    // TODO: Final pass dithering
    internal class PostProcessPass : ScriptableRenderPass
    {
        RenderTargetHandle m_Source;
        RenderTargetHandle m_Destination;
        RenderTextureDescriptor m_Descriptor;
        
        const string k_RenderPostProcessingTag = "Render PostProcessing Effects";

        MaterialLibrary m_Materials;

        // Builtin effects settings
        PaniniProjection m_PaniniProjection;
        Bloom m_Bloom;
        LensDistortion m_LensDistortion;
        ChromaticAberration m_ChromaticAberration;
        Vignette m_Vignette;
        ColorLookup m_ColorLookup;
        ChannelMixer m_ChannelMixer;
        ColorAdjustments m_ColorAdjustments;
        ColorCurves m_Curves;
        LiftGammaGain m_LiftGammaGain;
        ShadowsMidtonesHighlights m_ShadowsMidtonesHighlights;
        SplitToning m_SplitToning;
        Tonemapping m_Tonemapping;
        WhiteBalance m_WhiteBalance;
        FilmGrain m_FilmGrain;

        // Misc
        readonly GraphicsFormat m_HdrLutFormat;
        readonly GraphicsFormat m_LdrLutFormat;
        readonly RenderTargetHandle m_InternalLut;

        const int k_MaxPyramidSize = 16;
        readonly GraphicsFormat m_BloomFormat;

        public PostProcessPass(RenderPassEvent evt, ForwardRendererData data)
        {
            renderPassEvent = evt;
            m_Materials = new MaterialLibrary(data);

            m_InternalLut = new RenderTargetHandle();
            m_InternalLut.Init("_InternalGradingLut");

            // Texture format pre-lookup
            unsafe
            {
                var asset = LightweightRenderPipeline.asset;
                var hdr = asset != null && asset.supportsHDR;

                // Lut
                var lutFormats = stackalloc GraphicsFormat[3]
                {
                    GraphicsFormat.R16G16B16A16_SFloat,
                    GraphicsFormat.B10G11R11_UFloatPack32,

                    // Obviously using this for log lut encoding is a very bad idea for precision
                    // but we need it for compatibility reasons and avoid black screens on platforms
                    // that don't support floating point formats. Expect banding and posterization
                    // artifact if this ends up being used.
                    GraphicsFormat.R8G8B8A8_UNorm
                };

                m_HdrLutFormat = PickSupportedFormat(lutFormats, 3);
                m_LdrLutFormat = GraphicsFormat.R8G8B8A8_UNorm;

                // Bloom
                var bloomFormats = stackalloc GraphicsFormat[2]
                {
                    GraphicsFormat.B10G11R11_UFloatPack32,
                    GraphicsFormat.R8G8B8A8_UNorm
                };

                m_BloomFormat = hdr
                    ? PickSupportedFormat(bloomFormats, 2)
                    : GraphicsFormat.R8G8B8A8_UNorm;
            }

            // Bloom pyramid shader ids - can't use a simple stackalloc in the bloom function as we
            // unfortunately need to allocate strings
            ShaderConstants._BloomMipUp = new int[k_MaxPyramidSize];
            ShaderConstants._BloomMipDown = new int[k_MaxPyramidSize];

            for (int i = 0; i < k_MaxPyramidSize; i++)
            {
                ShaderConstants._BloomMipUp[i] = Shader.PropertyToID("_BloomMipUp" + i);
                ShaderConstants._BloomMipDown[i] = Shader.PropertyToID("_BloomMipDown" + i);
            }
        }

        // No Span<T> in Unity yet
        static unsafe GraphicsFormat PickSupportedFormat(GraphicsFormat* formats, int count, FormatUsage flags = FormatUsage.Linear | FormatUsage.Render)
        {
            for (int i = 0; i < count; i++)
            {
                if (SystemInfo.IsFormatSupported(formats[i], flags))
                    return formats[i];
            }

            return GraphicsFormat.None;
        }

        public void Setup(RenderTextureDescriptor baseDescriptor, RenderTargetHandle sourceHandle, RenderTargetHandle destinationHandle)
        {
            m_Descriptor = baseDescriptor;
            m_Source = sourceHandle;
            m_Destination = destinationHandle;
        }

        public bool CanRunOnTile()
        {
            // Check builtin & user effects here
            return false;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // Start by pre-fetching all builtin effect settings
            var stack = VolumeManager.instance.stack;
            m_PaniniProjection            = stack.GetComponent<PaniniProjection>();
            m_Bloom                       = stack.GetComponent<Bloom>();
            m_LensDistortion              = stack.GetComponent<LensDistortion>();
            m_ChromaticAberration         = stack.GetComponent<ChromaticAberration>();
            m_Vignette                    = stack.GetComponent<Vignette>();
            m_ColorLookup                 = stack.GetComponent<ColorLookup>();
            m_ChannelMixer                = stack.GetComponent<ChannelMixer>();
            m_ColorAdjustments            = stack.GetComponent<ColorAdjustments>();
            m_Curves                      = stack.GetComponent<ColorCurves>();
            m_LiftGammaGain               = stack.GetComponent<LiftGammaGain>();
            m_ShadowsMidtonesHighlights   = stack.GetComponent<ShadowsMidtonesHighlights>();
            m_SplitToning                 = stack.GetComponent<SplitToning>();
            m_Tonemapping                 = stack.GetComponent<Tonemapping>();
            m_WhiteBalance                = stack.GetComponent<WhiteBalance>();
            m_FilmGrain                   = stack.GetComponent<FilmGrain>();

            if (CanRunOnTile())
            {
                // TODO: Add a fast render path if only on-tile compatible effects are used and we're actually running on a platform that supports it
                // Note: we can still work on-tile if FXAA is enabled, it'd be part of the final pass
            }
            else
            {
                // Regular render path (not on-tile) - we do everything in a single command buffer as it
                // makes it easier to manage temporary targets' lifetime
                var cmd = CommandBufferPool.Get(k_RenderPostProcessingTag);
                Render(cmd, ref renderingData);
                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }
        }

        void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ref var cameraData = ref renderingData.cameraData;

            // Don't use these directly unless you have a good reason to, use GetSource() and
            // GetDestination() instead
            int source = m_Source.id;
            int destination = -1;

            // Utilities to simplify intermediate target management
            int GetSource() => source;

            int GetDestination()
            {
                if (destination == -1)
                {
                    cmd.GetTemporaryRT(
                        ShaderConstants._TempTarget, m_Descriptor.width, m_Descriptor.height,
                        0, FilterMode.Bilinear, m_Descriptor.graphicsFormat
                    );

                    destination = ShaderConstants._TempTarget;
                }

                return destination;
            }

            void Swap() => CoreUtils.Swap(ref source, ref destination);

            // Optional NaN killer before post-processing kicks in
            if (cameraData.isStopNaNEnabled)
            {
                using (new ProfilingSample(cmd, "Stop NaN"))
                {
                    cmd.Blit(GetSource(), GetDestination(), m_Materials.stopNaN);
                    Swap();
                }
            }

            // Panini projection is done as a fullscreen pass after all depth-based effects are done
            // and before bloom kicks in
            if (m_PaniniProjection.IsActive() && !cameraData.isSceneViewCamera)
            {
                using (new ProfilingSample(cmd, "Panini Projection"))
                {
                    DoPaniniProjection(cameraData.camera, cmd, GetSource(), GetDestination());
                    Swap();
                }
            }

            // Combined post-processing stack
            using (new ProfilingSample(cmd, "Uber"))
            {
                // Reset uber keywords
                m_Materials.uber.shaderKeywords = null;

                // Bloom goes first
                bool bloomActive = m_Bloom.IsActive();
                if (bloomActive)
                {
                    using (new ProfilingSample(cmd, "Bloom"))
                        SetupBloom(cameraData.camera, cmd, GetSource(), m_Materials.uber);
                }

                // Setup other effects constants
                SetupLensDistortion(m_Materials.uber, cameraData.isSceneViewCamera);
                SetupChromaticAberration(m_Materials.uber);
                SetupVignette(cameraData.camera, m_Materials.uber);
                SetupColorGrading(cmd, ref renderingData, m_Materials.uber);
                SetupGrain(cameraData.camera, m_Materials.uber, false);

                // Done with Uber, blit it
                Blit(cmd, GetSource(), m_Destination.Identifier(), m_Materials.uber);

                // Cleanup
                cmd.ReleaseTemporaryRT(m_InternalLut.id);

                if (bloomActive)
                    cmd.ReleaseTemporaryRT(ShaderConstants._BloomMipUp[0]);

                if (destination != -1)
                    cmd.ReleaseTemporaryRT(destination);
            }
        }

        #region Panini Projection

        // Back-ported & adapted from the work of the Stockholm demo team - thanks Lasse!
        void DoPaniniProjection(Camera camera, CommandBuffer cmd, int source, int destination)
        {
            float distance = m_PaniniProjection.distance.value;
            var viewExtents = CalcViewExtents(camera);
            var cropExtents = CalcCropExtents(camera, distance);

            float scaleX = cropExtents.x / viewExtents.x;
            float scaleY = cropExtents.y / viewExtents.y;
            float scaleF = Mathf.Min(scaleX, scaleY);

            float paniniD = distance;
            float paniniS = Mathf.Lerp(1f, Mathf.Clamp01(scaleF), m_PaniniProjection.cropToFit.value);

            var material = m_Materials.paniniProjection;
            material.SetVector(ShaderConstants._Params, new Vector4(viewExtents.x, viewExtents.y, paniniD, paniniS));
            material.EnableKeyword(
                1f - Mathf.Abs(paniniD) > float.Epsilon
                ? "GENERIC" : "UNIT_DISTANCE"
            );

            cmd.Blit(source, destination, material);
        }

        static Vector2 CalcViewExtents(Camera camera)
        {
            float fovY = camera.fieldOfView * Mathf.Deg2Rad;
            float aspect = camera.pixelWidth / (float)camera.pixelHeight;

            float viewExtY = Mathf.Tan(0.5f * fovY);
            float viewExtX = aspect * viewExtY;

            return new Vector2(viewExtX, viewExtY);
        }

        static Vector2 CalcCropExtents(Camera camera, float d)
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

            var projPos = CalcViewExtents(camera);
            var projHyp = Mathf.Sqrt(projPos.x * projPos.x + 1f);

            float cylDistMinusD = 1f / projHyp;
            float cylDist = cylDistMinusD + d;
            var cylPos = projPos * cylDistMinusD;

            return cylPos * (viewDist / cylDist);
        }

        #endregion

        #region Bloom

        // TODO: RGBM support when not HDR as right now it's pretty much useless in LDR
        void SetupBloom(Camera camera, CommandBuffer cmd, int source, Material uberMaterial)
        {
            // Start at half-res
            int tw = camera.pixelWidth >> 1;
            int th = camera.pixelHeight >> 1;

            // Determine the iteration count
            int maxSize = Mathf.Max(tw, th);
            int iterations = Mathf.FloorToInt(Mathf.Log(maxSize, 2f) - 1);
            int mipCount = Mathf.Clamp(iterations, 1, k_MaxPyramidSize);

            // Pre-filtering parameters
            float clamp = m_Bloom.clamp.value;
            float threshold = Mathf.GammaToLinearSpace(m_Bloom.threshold.value);
            float thresholdKnee = threshold * 0.5f; // Hardcoded soft knee

            // Material setup
            float scatter = Mathf.Lerp(0.05f, 0.95f, m_Bloom.scatter.value);
            var bloomMaterial = m_Materials.bloom;
            bloomMaterial.SetVector(ShaderConstants._Params, new Vector4(scatter, clamp, threshold, thresholdKnee));
            CoreUtils.SetKeyword(bloomMaterial, "FILTERING_HQ", m_Bloom.highQualityFiltering.value);

            // Prefilter
            cmd.GetTemporaryRT(ShaderConstants._BloomMipDown[0], tw, th, 0, FilterMode.Bilinear, m_BloomFormat);
            cmd.GetTemporaryRT(ShaderConstants._BloomMipUp[0], tw, th, 0, FilterMode.Bilinear, m_BloomFormat);
            cmd.Blit(source, ShaderConstants._BloomMipDown[0], bloomMaterial, 0);

            // Downsample - gaussian pyramid
            int lastDown = ShaderConstants._BloomMipDown[0];
            for (int i = 1; i < mipCount; i++)
            {
                tw = Mathf.Max(1, tw >> 1);
                th = Mathf.Max(1, th >> 1);
                int mipDown = ShaderConstants._BloomMipDown[i];
                int mipUp = ShaderConstants._BloomMipUp[i];

                cmd.GetTemporaryRT(mipDown, tw, th, 0, FilterMode.Bilinear, m_BloomFormat);
                cmd.GetTemporaryRT(mipUp, tw, th, 0, FilterMode.Bilinear, m_BloomFormat);

                // Classic two pass gaussian blur - use mipUp as a temporary target
                //   First pass does 2x downsampling + 9-tap gaussian
                //   Second pass does 9-tap gaussian using a 5-tap filter + bilinear filtering
                cmd.Blit(lastDown, mipUp, bloomMaterial, 1);
                cmd.Blit(mipUp, mipDown, bloomMaterial, 2);
                lastDown = mipDown;
            }

            // Upsample (bilinear by default, HQ filtering does bicubic instead
            for (int i = mipCount - 2; i >= 0; i--)
            {
                int lowMip = (i == mipCount - 2) ? ShaderConstants._BloomMipDown[i + 1] : ShaderConstants._BloomMipUp[i + 1];
                int highMip = ShaderConstants._BloomMipDown[i];
                int dst = ShaderConstants._BloomMipUp[i];

                cmd.SetGlobalTexture(ShaderConstants._MainTexLowMip, lowMip);
                cmd.Blit(highMip, dst, bloomMaterial, 3);
            }

            // Cleanup
            for (int i = 0; i < mipCount; i++)
            {
                cmd.ReleaseTemporaryRT(ShaderConstants._BloomMipDown[i]);
                if (i > 0) cmd.ReleaseTemporaryRT(ShaderConstants._BloomMipUp[i]);
            }

            // Setup bloom on uber
            var tint = m_Bloom.tint.value.linear;
            var luma = ColorUtils.Luminance(tint);
            tint = luma > 0f ? tint * (1f / luma) : Color.white;

            var bloomParams = new Vector4(m_Bloom.intensity.value, tint.r, tint.g, tint.b);
            uberMaterial.SetVector(ShaderConstants._Bloom_Params, bloomParams);

            cmd.SetGlobalTexture(ShaderConstants._Bloom_Texture, ShaderConstants._BloomMipUp[0]);

            // Setup lens dirtiness on uber
            // Keep the aspect ratio correct & center the dirt texture, we don't want it to be
            // stretched or squashed
            var dirtTexture = m_Bloom.dirtTexture.value == null ? Texture2D.blackTexture : m_Bloom.dirtTexture.value;
            float dirtRatio = dirtTexture.width / (float)dirtTexture.height;
            float screenRatio = camera.pixelWidth / (float)camera.pixelHeight;
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

            uberMaterial.SetVector(ShaderConstants._LensDirt_Params, dirtScaleOffset);
            uberMaterial.SetFloat(ShaderConstants._LensDirt_Intensity, dirtIntensity);
            uberMaterial.SetTexture(ShaderConstants._LensDirt_Texture, dirtTexture);

            // Keyword setup - a bit convoluted as we're trying to save some variants in Uber...
            if (m_Bloom.highQualityFiltering.value)
                uberMaterial.EnableKeyword(dirtIntensity > 0f ? "BLOOM_HQ_DIRT" : "BLOOM_HQ");
            else
                uberMaterial.EnableKeyword(dirtIntensity > 0f ? "BLOOM_LQ_DIRT" : "BLOOM_LQ");
        }

        #endregion

        #region Lens Distortion

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
                material.EnableKeyword("DISTORTION");
        }

        #endregion

        #region Chromatic Aberration

        void SetupChromaticAberration(Material material)
        {
            material.SetFloat(ShaderConstants._Chroma_Params, m_ChromaticAberration.intensity.value * 0.05f);

            if (m_ChromaticAberration.IsActive())
                material.EnableKeyword("CHROMATIC_ABERRATION");
        }

        #endregion

        #region Vignette

        void SetupVignette(Camera camera, Material material)
        {
            var color = m_Vignette.color.value;
            var center = m_Vignette.center.value;

            var v1 = new Vector4(
                color.r, color.g, color.b,
                m_Vignette.rounded.value ? camera.pixelWidth / (float)camera.pixelHeight : 1f
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

        #region Color Grading

        void SetupColorGrading(CommandBuffer cmd, ref RenderingData renderingData, Material srcMaterial)
        {
            ref var postProcessingData = ref renderingData.postProcessingData;
            bool hdr = postProcessingData.gradingMode == ColorGradingMode.HighDynamicRange;

            // Prepare texture & material
            int lutHeight = postProcessingData.lutSize;
            int lutWidth = lutHeight * lutHeight;
            var format = hdr ? m_HdrLutFormat : m_LdrLutFormat;
            var lutMaterial = hdr ? m_Materials.lutBuilderHdr : m_Materials.lutBuilderLdr;

            cmd.GetTemporaryRT(m_InternalLut.id, lutWidth, lutHeight, 0, FilterMode.Bilinear, format);

            // Prepare data
            var lmsColorBalance = ColorUtils.ColorBalanceToLMSCoeffs(m_WhiteBalance.temperature.value, m_WhiteBalance.tint.value);
            var hueSatCon = new Vector4(m_ColorAdjustments.hueShift / 360f, m_ColorAdjustments.saturation / 100f + 1f, m_ColorAdjustments.contrast / 100f + 1f, 0f);
            var channelMixerR = new Vector4(m_ChannelMixer.redOutRedIn / 100f, m_ChannelMixer.redOutGreenIn / 100f, m_ChannelMixer.redOutBlueIn / 100f, 0f);
            var channelMixerG = new Vector4(m_ChannelMixer.greenOutRedIn / 100f, m_ChannelMixer.greenOutGreenIn / 100f, m_ChannelMixer.greenOutBlueIn / 100f, 0f);
            var channelMixerB = new Vector4(m_ChannelMixer.blueOutRedIn / 100f, m_ChannelMixer.blueOutGreenIn / 100f, m_ChannelMixer.blueOutBlueIn / 100f, 0f);

            var shadowsHighlightsLimits = new Vector4(
                m_ShadowsMidtonesHighlights.shadowsStart.value,
                m_ShadowsMidtonesHighlights.shadowsEnd.value,
                m_ShadowsMidtonesHighlights.highlightsStart.value,
                m_ShadowsMidtonesHighlights.highlightsEnd.value
            );

            var (shadows, midtones, highlights) = ColorUtils.PrepareShadowsMidtonesHighlights(
                m_ShadowsMidtonesHighlights.shadows.value,
                m_ShadowsMidtonesHighlights.midtones.value,
                m_ShadowsMidtonesHighlights.highlights.value
            );

            var (lift, gamma, gain) = ColorUtils.PrepareLiftGammaGain(
                m_LiftGammaGain.lift.value,
                m_LiftGammaGain.gamma.value,
                m_LiftGammaGain.gain.value
            );

            var (splitShadows, splitHighlights) = ColorUtils.PrepareSplitToning(
                m_SplitToning.shadows.value,
                m_SplitToning.highlights.value,
                m_SplitToning.balance.value
            );

            var lutParameters = new Vector4(lutHeight, 0.5f / lutWidth, 0.5f / lutHeight, lutHeight / (lutHeight - 1f));
            float postExposureLinear = Mathf.Pow(2f, m_ColorAdjustments.postExposure.value);

            // Fill in constants
            lutMaterial.SetVector(ShaderConstants._Lut_Params, lutParameters);
            lutMaterial.SetVector(ShaderConstants._ColorBalance, lmsColorBalance);
            lutMaterial.SetVector(ShaderConstants._ColorFilter, m_ColorAdjustments.colorFilter.value.linear);
            lutMaterial.SetVector(ShaderConstants._ChannelMixerRed, channelMixerR);
            lutMaterial.SetVector(ShaderConstants._ChannelMixerGreen, channelMixerG);
            lutMaterial.SetVector(ShaderConstants._ChannelMixerBlue, channelMixerB);
            lutMaterial.SetVector(ShaderConstants._HueSatCon, hueSatCon);
            lutMaterial.SetVector(ShaderConstants._Lift, lift);
            lutMaterial.SetVector(ShaderConstants._Gamma, gamma);
            lutMaterial.SetVector(ShaderConstants._Gain, gain);
            lutMaterial.SetVector(ShaderConstants._Shadows, shadows);
            lutMaterial.SetVector(ShaderConstants._Midtones, midtones);
            lutMaterial.SetVector(ShaderConstants._Highlights, highlights);
            lutMaterial.SetVector(ShaderConstants._ShaHiLimits, shadowsHighlightsLimits);
            lutMaterial.SetVector(ShaderConstants._SplitShadows, splitShadows);
            lutMaterial.SetVector(ShaderConstants._SplitHighlights, splitHighlights);

            // YRGB curves
            lutMaterial.SetTexture(ShaderConstants._CurveMaster, m_Curves.master.value.GetTexture());
            lutMaterial.SetTexture(ShaderConstants._CurveRed, m_Curves.red.value.GetTexture());
            lutMaterial.SetTexture(ShaderConstants._CurveGreen, m_Curves.green.value.GetTexture());
            lutMaterial.SetTexture(ShaderConstants._CurveBlue, m_Curves.blue.value.GetTexture());

            // Secondary curves
            lutMaterial.SetTexture(ShaderConstants._CurveHueVsHue, m_Curves.hueVsHue.value.GetTexture());
            lutMaterial.SetTexture(ShaderConstants._CurveHueVsSat, m_Curves.hueVsSat.value.GetTexture());
            lutMaterial.SetTexture(ShaderConstants._CurveLumVsSat, m_Curves.lumVsSat.value.GetTexture());
            lutMaterial.SetTexture(ShaderConstants._CurveSatVsSat, m_Curves.satVsSat.value.GetTexture());

            // Tonemapping (baked into the lut for HDR)
            if (hdr)
            {
                lutMaterial.shaderKeywords = null;

                switch (m_Tonemapping.mode.value)
                {
                    case TonemappingMode.Neutral: lutMaterial.EnableKeyword("TONEMAP_NEUTRAL"); break;
                    case TonemappingMode.ACES: lutMaterial.EnableKeyword("TONEMAP_ACES"); break;
                    default: break; // None
                }
            }

            // Render the lut
            cmd.Blit(null, m_InternalLut.id, lutMaterial);

            // Source material setup
            cmd.SetGlobalTexture(ShaderConstants._InternalLut, m_InternalLut.Identifier());
            srcMaterial.SetVector(ShaderConstants._Lut_Params, new Vector4(1f / lutWidth, 1f / lutHeight, lutHeight - 1f, postExposureLinear));
            srcMaterial.SetTexture(ShaderConstants._UserLut, m_ColorLookup.texture);
            srcMaterial.SetVector(ShaderConstants._UserLut_Params, !m_ColorLookup.IsActive()
                ? Vector4.zero
                : new Vector4(1f / m_ColorLookup.texture.value.width,
                              1f / m_ColorLookup.texture.value.height,
                              m_ColorLookup.texture.value.height - 1f,
                              m_ColorLookup.contribution.value)
            );

            if (hdr)
            {
                srcMaterial.EnableKeyword("HDR_GRADING");
            }
            else
            {
                switch (m_Tonemapping.mode.value)
                {
                    case TonemappingMode.Neutral: srcMaterial.EnableKeyword("TONEMAP_NEUTRAL"); break;
                    case TonemappingMode.ACES: srcMaterial.EnableKeyword("TONEMAP_ACES"); break;
                    default: break; // None
                }
            }
        }

        #endregion

        #region Film Grain

        void SetupGrain(Camera camera, Material material, bool onTile)
        {
            var texture = m_FilmGrain.texture.value;

            #if LWRP_DEBUG_STATIC_POSTFX
            float offsetX = 0f;
            float offsetY = 0f;
            #else
            float offsetX = Random.value;
            float offsetY = Random.value;
            #endif

            var tilingParams = texture == null
                ? Vector4.zero
                : new Vector4(camera.pixelWidth / (float)texture.width, camera.pixelHeight / (float)texture.height, offsetX, offsetY);

            material.SetTexture(ShaderConstants._Grain_Texture, texture);
            material.SetVector(ShaderConstants._Grain_Params, new Vector2(m_FilmGrain.intensity.value * 4f, m_FilmGrain.response.value));
            material.SetVector(ShaderConstants._Grain_TilingParams, tilingParams);

            if (!onTile && m_FilmGrain.IsActive())
                material.EnableKeyword("GRAIN");
        }

        #endregion

        #region Internal utilities

        class MaterialLibrary
        {
            public readonly Material stopNaN;
            public readonly Material paniniProjection;
            public readonly Material lutBuilderLdr;
            public readonly Material lutBuilderHdr;
            public readonly Material bloom;
            public readonly Material uber;

            public MaterialLibrary(ForwardRendererData data)
            {
                stopNaN = Load(data.stopNaNShader);
                paniniProjection = Load(data.paniniProjectionShader);
                lutBuilderLdr = Load(data.lutBuilderLdrShader);
                lutBuilderHdr = Load(data.lutBuilderHdrShader);
                bloom = Load(data.bloomShader);
                uber = Load(data.uberPostShader);
            }

            Material Load(Shader shader)
            {
                if (shader == null)
                {
                    Debug.LogErrorFormat($"Missing shader. {GetType().DeclaringType.Name} render pass will not execute. Check for missing reference in the renderer resources.");
                    return null;
                }

                var material = CoreUtils.CreateEngineMaterial(shader);

                return material;
            }
        }

        // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
        static class ShaderConstants
        {
            public static readonly int _TempTarget         = Shader.PropertyToID("_TempTarget");

            public static readonly int _Params             = Shader.PropertyToID("_Params");
            public static readonly int _MainTexLowMip      = Shader.PropertyToID("_MainTexLowMip");
            public static readonly int _Bloom_Params       = Shader.PropertyToID("_Bloom_Params");
            public static readonly int _Bloom_Texture      = Shader.PropertyToID("_Bloom_Texture");
            public static readonly int _LensDirt_Texture   = Shader.PropertyToID("_LensDirt_Texture");
            public static readonly int _LensDirt_Params    = Shader.PropertyToID("_LensDirt_Params");
            public static readonly int _LensDirt_Intensity = Shader.PropertyToID("_LensDirt_Intensity");
            public static readonly int _Distortion_Params1 = Shader.PropertyToID("_Distortion_Params1");
            public static readonly int _Distortion_Params2 = Shader.PropertyToID("_Distortion_Params2");
            public static readonly int _Chroma_Params      = Shader.PropertyToID("_Chroma_Params");
            public static readonly int _Vignette_Params1   = Shader.PropertyToID("_Vignette_Params1");
            public static readonly int _Vignette_Params2   = Shader.PropertyToID("_Vignette_Params2");
            public static readonly int _Lut_Params         = Shader.PropertyToID("_Lut_Params");
            public static readonly int _UserLut_Params     = Shader.PropertyToID("_UserLut_Params");
            public static readonly int _ColorBalance       = Shader.PropertyToID("_ColorBalance");
            public static readonly int _ColorFilter        = Shader.PropertyToID("_ColorFilter");
            public static readonly int _ChannelMixerRed    = Shader.PropertyToID("_ChannelMixerRed");
            public static readonly int _ChannelMixerGreen  = Shader.PropertyToID("_ChannelMixerGreen");
            public static readonly int _ChannelMixerBlue   = Shader.PropertyToID("_ChannelMixerBlue");
            public static readonly int _HueSatCon          = Shader.PropertyToID("_HueSatCon");
            public static readonly int _Lift               = Shader.PropertyToID("_Lift");
            public static readonly int _Gamma              = Shader.PropertyToID("_Gamma");
            public static readonly int _Gain               = Shader.PropertyToID("_Gain");
            public static readonly int _Shadows            = Shader.PropertyToID("_Shadows");
            public static readonly int _Midtones           = Shader.PropertyToID("_Midtones");
            public static readonly int _Highlights         = Shader.PropertyToID("_Highlights");
            public static readonly int _ShaHiLimits        = Shader.PropertyToID("_ShaHiLimits");
            public static readonly int _SplitShadows       = Shader.PropertyToID("_SplitShadows");
            public static readonly int _SplitHighlights    = Shader.PropertyToID("_SplitHighlights");
            public static readonly int _CurveMaster        = Shader.PropertyToID("_CurveMaster");
            public static readonly int _CurveRed           = Shader.PropertyToID("_CurveRed");
            public static readonly int _CurveGreen         = Shader.PropertyToID("_CurveGreen");
            public static readonly int _CurveBlue          = Shader.PropertyToID("_CurveBlue");
            public static readonly int _CurveHueVsHue      = Shader.PropertyToID("_CurveHueVsHue");
            public static readonly int _CurveHueVsSat      = Shader.PropertyToID("_CurveHueVsSat");
            public static readonly int _CurveLumVsSat      = Shader.PropertyToID("_CurveLumVsSat");
            public static readonly int _CurveSatVsSat      = Shader.PropertyToID("_CurveSatVsSat");
            public static readonly int _InternalLut        = Shader.PropertyToID("_InternalLut");
            public static readonly int _UserLut            = Shader.PropertyToID("_UserLut");
            public static readonly int _Grain_Texture      = Shader.PropertyToID("_Grain_Texture");
            public static readonly int _Grain_Params       = Shader.PropertyToID("_Grain_Params");
            public static readonly int _Grain_TilingParams = Shader.PropertyToID("_Grain_TilingParams");

            public static int[] _BloomMipUp;
            public static int[] _BloomMipDown;
        }

        #endregion
    }
}
