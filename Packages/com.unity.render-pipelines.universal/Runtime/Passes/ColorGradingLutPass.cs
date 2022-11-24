using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.Universal.Internal
{
    // Note: this pass can't be done at the same time as post-processing as it needs to be done in
    // advance in case we're doing on-tile color grading.
    /// <summary>
    /// Renders a color grading LUT texture.
    /// </summary>
    public class ColorGradingLutPass : ScriptableRenderPass
    {
        readonly Material m_LutBuilderLdr;
        readonly Material m_LutBuilderHdr;
        internal readonly GraphicsFormat m_HdrLutFormat;
        internal readonly GraphicsFormat m_LdrLutFormat;
        PassData m_PassData;
        RTHandle m_InternalLut;

        bool m_AllowColorGradingACESHDR = true;

        /// <summary>
        /// Creates a new <c>ColorGradingLutPass</c> instance.
        /// </summary>
        /// <param name="evt">The <c>RenderPassEvent</c> to use.</param>
        /// <param name="data">The <c>PostProcessData</c> resources to use.</param>
        /// <seealso cref="RenderPassEvent"/>
        /// <seealso cref="PostProcessData"/>
        public ColorGradingLutPass(RenderPassEvent evt, PostProcessData data)
        {
            base.profilingSampler = new ProfilingSampler(nameof(ColorGradingLutPass));
            renderPassEvent = evt;
            overrideCameraTarget = true;

            Material Load(Shader shader)
            {
                if (shader == null)
                {
                    Debug.LogError($"Missing shader. {GetType().DeclaringType.Name} render pass will not execute. Check for missing reference in the renderer resources.");
                    return null;
                }

                return CoreUtils.CreateEngineMaterial(shader);
            }

            m_LutBuilderLdr = Load(data.shaders.lutBuilderLdrPS);
            m_LutBuilderHdr = Load(data.shaders.lutBuilderHdrPS);

            // Warm up lut format as IsFormatSupported adds GC pressure...
            const FormatUsage kFlags = FormatUsage.Linear | FormatUsage.Render;
            if (SystemInfo.IsFormatSupported(GraphicsFormat.R16G16B16A16_SFloat, kFlags))
                m_HdrLutFormat = GraphicsFormat.R16G16B16A16_SFloat;
            else if (SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32, kFlags))
                // Precision can be too low, if FP16 primary renderTarget is requested by the user.
                // But it's better than falling back to R8G8B8A8_UNorm in the worst case.
                m_HdrLutFormat = GraphicsFormat.B10G11R11_UFloatPack32;
            else
                // Obviously using this for log lut encoding is a very bad idea for precision but we
                // need it for compatibility reasons and avoid black screens on platforms that don't
                // support floating point formats. Expect banding and posterization artifact if this
                // ends up being used.
                m_HdrLutFormat = GraphicsFormat.R8G8B8A8_UNorm;

            m_LdrLutFormat = GraphicsFormat.R8G8B8A8_UNorm;
            base.useNativeRenderPass = false;

            if (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 && Graphics.minOpenGLESVersion <= OpenGLESVersion.OpenGLES30 && SystemInfo.graphicsDeviceName.StartsWith("Adreno (TM) 3"))
                m_AllowColorGradingACESHDR = false;

            m_PassData = new PassData();
        }

        /// <summary>
        /// Sets up the pass.
        /// </summary>
        /// <param name="internalLut">The RTHandle to use to render to.</param>
        /// <seealso cref="RTHandle"/>
        public void Setup(in RTHandle internalLut)
        {
            m_InternalLut = internalLut;
        }

        /// <summary>
        /// Get a descriptor and filter mode for the required texture for this pass
        /// </summary>
        /// <param name="postProcessingData"></param>
        /// <param name="descriptor"></param>
        /// <param name="filterMode"></param>
        public void ConfigureDescriptor(in PostProcessingData postProcessingData, out RenderTextureDescriptor descriptor, out FilterMode filterMode)
        {
            bool hdr = postProcessingData.gradingMode == ColorGradingMode.HighDynamicRange;
            int lutHeight = postProcessingData.lutSize;
            int lutWidth = lutHeight * lutHeight;
            var format = hdr ? m_HdrLutFormat : m_LdrLutFormat;
            descriptor = new RenderTextureDescriptor(lutWidth, lutHeight, format, 0);
            descriptor.vrUsage = VRTextureUsage.None; // We only need one for both eyes in VR

            filterMode = FilterMode.Bilinear;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            m_PassData.lutBuilderLdr = m_LutBuilderLdr;
            m_PassData.lutBuilderHdr = m_LutBuilderHdr;
            m_PassData.allowColorGradingACESHDR = m_AllowColorGradingACESHDR;

            CoreUtils.SetRenderTarget(renderingData.commandBuffer, m_InternalLut, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, ClearFlag.None, Color.clear);
            ExecutePass(context, m_PassData, ref renderingData, m_InternalLut);
        }

        private class PassData
        {
            internal RenderingData renderingData;
            internal Material lutBuilderLdr;
            internal Material lutBuilderHdr;
            internal bool allowColorGradingACESHDR;
            internal TextureHandle internalLut;
        }

        private static void ExecutePass(ScriptableRenderContext context, PassData passData, ref RenderingData renderingData, RTHandle internalLutTarget)
        {
            var cmd = renderingData.commandBuffer;
            var lutBuilderLdr = passData.lutBuilderLdr;
            var lutBuilderHdr = passData.lutBuilderHdr;
            var allowColorGradingACESHDR = passData.allowColorGradingACESHDR;

            using (new ProfilingScope(cmd, ProfilingSampler.Get(URPProfileId.ColorGradingLUT)))
            {
                // Fetch all color grading settings
                var stack = VolumeManager.instance.stack;
                var channelMixer = stack.GetComponent<ChannelMixer>();
                var colorAdjustments = stack.GetComponent<ColorAdjustments>();
                var curves = stack.GetComponent<ColorCurves>();
                var liftGammaGain = stack.GetComponent<LiftGammaGain>();
                var shadowsMidtonesHighlights = stack.GetComponent<ShadowsMidtonesHighlights>();
                var splitToning = stack.GetComponent<SplitToning>();
                var tonemapping = stack.GetComponent<Tonemapping>();
                var whiteBalance = stack.GetComponent<WhiteBalance>();

                ref var postProcessingData = ref renderingData.postProcessingData;
                bool hdr = postProcessingData.gradingMode == ColorGradingMode.HighDynamicRange;

                // Prepare texture & material
                var material = hdr ? lutBuilderHdr : lutBuilderLdr;

                // Prepare data
                var lmsColorBalance = ColorUtils.ColorBalanceToLMSCoeffs(whiteBalance.temperature.value, whiteBalance.tint.value);
                var hueSatCon = new Vector4(colorAdjustments.hueShift.value / 360f, colorAdjustments.saturation.value / 100f + 1f, colorAdjustments.contrast.value / 100f + 1f, 0f);
                var channelMixerR = new Vector4(channelMixer.redOutRedIn.value / 100f, channelMixer.redOutGreenIn.value / 100f, channelMixer.redOutBlueIn.value / 100f, 0f);
                var channelMixerG = new Vector4(channelMixer.greenOutRedIn.value / 100f, channelMixer.greenOutGreenIn.value / 100f, channelMixer.greenOutBlueIn.value / 100f, 0f);
                var channelMixerB = new Vector4(channelMixer.blueOutRedIn.value / 100f, channelMixer.blueOutGreenIn.value / 100f, channelMixer.blueOutBlueIn.value / 100f, 0f);

                var shadowsHighlightsLimits = new Vector4(
                    shadowsMidtonesHighlights.shadowsStart.value,
                    shadowsMidtonesHighlights.shadowsEnd.value,
                    shadowsMidtonesHighlights.highlightsStart.value,
                    shadowsMidtonesHighlights.highlightsEnd.value
                );

                var (shadows, midtones, highlights) = ColorUtils.PrepareShadowsMidtonesHighlights(
                    shadowsMidtonesHighlights.shadows.value,
                    shadowsMidtonesHighlights.midtones.value,
                    shadowsMidtonesHighlights.highlights.value
                );

                var (lift, gamma, gain) = ColorUtils.PrepareLiftGammaGain(
                    liftGammaGain.lift.value,
                    liftGammaGain.gamma.value,
                    liftGammaGain.gain.value
                );

                var (splitShadows, splitHighlights) = ColorUtils.PrepareSplitToning(
                    splitToning.shadows.value,
                    splitToning.highlights.value,
                    splitToning.balance.value
                );

                int lutHeight = postProcessingData.lutSize;
                int lutWidth = lutHeight * lutHeight;
                var lutParameters = new Vector4(lutHeight, 0.5f / lutWidth, 0.5f / lutHeight,
                    lutHeight / (lutHeight - 1f));

                // Fill in constants
                material.SetVector(ShaderConstants._Lut_Params, lutParameters);
                material.SetVector(ShaderConstants._ColorBalance, lmsColorBalance);
                material.SetVector(ShaderConstants._ColorFilter, colorAdjustments.colorFilter.value.linear);
                material.SetVector(ShaderConstants._ChannelMixerRed, channelMixerR);
                material.SetVector(ShaderConstants._ChannelMixerGreen, channelMixerG);
                material.SetVector(ShaderConstants._ChannelMixerBlue, channelMixerB);
                material.SetVector(ShaderConstants._HueSatCon, hueSatCon);
                material.SetVector(ShaderConstants._Lift, lift);
                material.SetVector(ShaderConstants._Gamma, gamma);
                material.SetVector(ShaderConstants._Gain, gain);
                material.SetVector(ShaderConstants._Shadows, shadows);
                material.SetVector(ShaderConstants._Midtones, midtones);
                material.SetVector(ShaderConstants._Highlights, highlights);
                material.SetVector(ShaderConstants._ShaHiLimits, shadowsHighlightsLimits);
                material.SetVector(ShaderConstants._SplitShadows, splitShadows);
                material.SetVector(ShaderConstants._SplitHighlights, splitHighlights);

                // YRGB curves
                material.SetTexture(ShaderConstants._CurveMaster, curves.master.value.GetTexture());
                material.SetTexture(ShaderConstants._CurveRed, curves.red.value.GetTexture());
                material.SetTexture(ShaderConstants._CurveGreen, curves.green.value.GetTexture());
                material.SetTexture(ShaderConstants._CurveBlue, curves.blue.value.GetTexture());

                // Secondary curves
                material.SetTexture(ShaderConstants._CurveHueVsHue, curves.hueVsHue.value.GetTexture());
                material.SetTexture(ShaderConstants._CurveHueVsSat, curves.hueVsSat.value.GetTexture());
                material.SetTexture(ShaderConstants._CurveLumVsSat, curves.lumVsSat.value.GetTexture());
                material.SetTexture(ShaderConstants._CurveSatVsSat, curves.satVsSat.value.GetTexture());

                // Tonemapping (baked into the lut for HDR)
                if (hdr)
                {
                    material.shaderKeywords = null;

                    switch (tonemapping.mode.value)
                    {
                        case TonemappingMode.Neutral: material.EnableKeyword(ShaderKeywordStrings.TonemapNeutral); break;
                        case TonemappingMode.ACES: material.EnableKeyword(allowColorGradingACESHDR ? ShaderKeywordStrings.TonemapACES : ShaderKeywordStrings.TonemapNeutral); break;
                        default: break; // None
                    }

                    // HDR output is active
                    if (renderingData.cameraData.isHDROutputActive)
                    {
                        Vector4 hdrOutputLuminanceParams;
                        Vector4 hdrOutputGradingParams;
                        UniversalRenderPipeline.GetHDROutputLuminanceParameters(tonemapping, out hdrOutputLuminanceParams);
                        UniversalRenderPipeline.GetHDROutputGradingParameters(tonemapping, out hdrOutputGradingParams);

                        material.SetVector(ShaderPropertyId.hdrOutputLuminanceParams, hdrOutputLuminanceParams);
                        material.SetVector(ShaderPropertyId.hdrOutputGradingParams, hdrOutputGradingParams);

                        HDROutputUtils.ConfigureHDROutput(material, HDROutputSettings.main.displayColorGamut, HDROutputUtils.Operation.ColorConversion);
                    }
                }

                renderingData.cameraData.xr.StopSinglePass(cmd);


                if (renderingData.cameraData.xr.supportsFoveatedRendering)
                    cmd.SetFoveatedRenderingMode(FoveatedRenderingMode.Disabled);

                // Render the lut
                Blitter.BlitCameraTexture(cmd, internalLutTarget, internalLutTarget, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, material, 0);

                renderingData.cameraData.xr.StartSinglePass(cmd);
            }
        }

        internal void Render(RenderGraph renderGraph, out TextureHandle internalColorLut, ref RenderingData renderingData)
        {
            using (var builder = renderGraph.AddRenderPass<PassData>("Color Lut Pass", out var passData, base.profilingSampler))
            {
                this.ConfigureDescriptor(in renderingData.postProcessingData, out var lutDesc, out var filterMode);
                internalColorLut = UniversalRenderer.CreateRenderGraphTexture(renderGraph, lutDesc, "_InternalGradingLut", true);

                passData.internalLut = builder.UseColorBuffer(internalColorLut, 0);
                passData.lutBuilderLdr = m_LutBuilderLdr;
                passData.lutBuilderHdr = m_LutBuilderHdr;
                passData.renderingData = renderingData;
                passData.allowColorGradingACESHDR = m_AllowColorGradingACESHDR;

                //  TODO RENDERGRAPH: culling? force culling off for testing
                builder.AllowPassCulling(false);

                builder.SetRenderFunc((PassData data, RenderGraphContext context) =>
                {
                    ExecutePass(context.renderContext, data, ref data.renderingData, data.internalLut);
                });

                return;
            }
        }

        /// <summary>
        /// Cleans up resources used by the pass.
        /// </summary>
        public void Cleanup()
        {
            CoreUtils.Destroy(m_LutBuilderLdr);
            CoreUtils.Destroy(m_LutBuilderHdr);
        }

        // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
        static class ShaderConstants
        {
            public static readonly int _Lut_Params = Shader.PropertyToID("_Lut_Params");
            public static readonly int _ColorBalance = Shader.PropertyToID("_ColorBalance");
            public static readonly int _ColorFilter = Shader.PropertyToID("_ColorFilter");
            public static readonly int _ChannelMixerRed = Shader.PropertyToID("_ChannelMixerRed");
            public static readonly int _ChannelMixerGreen = Shader.PropertyToID("_ChannelMixerGreen");
            public static readonly int _ChannelMixerBlue = Shader.PropertyToID("_ChannelMixerBlue");
            public static readonly int _HueSatCon = Shader.PropertyToID("_HueSatCon");
            public static readonly int _Lift = Shader.PropertyToID("_Lift");
            public static readonly int _Gamma = Shader.PropertyToID("_Gamma");
            public static readonly int _Gain = Shader.PropertyToID("_Gain");
            public static readonly int _Shadows = Shader.PropertyToID("_Shadows");
            public static readonly int _Midtones = Shader.PropertyToID("_Midtones");
            public static readonly int _Highlights = Shader.PropertyToID("_Highlights");
            public static readonly int _ShaHiLimits = Shader.PropertyToID("_ShaHiLimits");
            public static readonly int _SplitShadows = Shader.PropertyToID("_SplitShadows");
            public static readonly int _SplitHighlights = Shader.PropertyToID("_SplitHighlights");
            public static readonly int _CurveMaster = Shader.PropertyToID("_CurveMaster");
            public static readonly int _CurveRed = Shader.PropertyToID("_CurveRed");
            public static readonly int _CurveGreen = Shader.PropertyToID("_CurveGreen");
            public static readonly int _CurveBlue = Shader.PropertyToID("_CurveBlue");
            public static readonly int _CurveHueVsHue = Shader.PropertyToID("_CurveHueVsHue");
            public static readonly int _CurveHueVsSat = Shader.PropertyToID("_CurveHueVsSat");
            public static readonly int _CurveLumVsSat = Shader.PropertyToID("_CurveLumVsSat");
            public static readonly int _CurveSatVsSat = Shader.PropertyToID("_CurveSatVsSat");
        }
    }
}
