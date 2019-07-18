using UnityEngine.Experimental.Rendering;

namespace UnityEngine.Rendering.Universal
{
    // Note: this pass can't be done at the same time as post-processing as it needs to be done in
    // advance in case we're doing on-tile color grading.
    internal class ColorGradingLutPass : ScriptableRenderPass
    {
        const string m_ProfilerTag = "Color Grading LUT";

        readonly Material m_LutBuilderLdr;
        readonly Material m_LutBuilderHdr;
        readonly GraphicsFormat m_HdrLutFormat;
        readonly GraphicsFormat m_LdrLutFormat;

        RenderTargetHandle m_InternalLut;

        public ColorGradingLutPass(RenderPassEvent evt, PostProcessData data)
        {
            renderPassEvent = evt;
            overrideCameraTarget = true;

            Material Load(Shader shader)
            {
                if (shader == null)
                {
                    Debug.LogErrorFormat($"Missing shader. {GetType().DeclaringType.Name} render pass will not execute. Check for missing reference in the renderer resources.");
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
                m_HdrLutFormat = GraphicsFormat.B10G11R11_UFloatPack32;
            else
                // Obviously using this for log lut encoding is a very bad idea for precision but we
                // need it for compatibility reasons and avoid black screens on platforms that don't
                // support floating point formats. Expect banding and posterization artifact if this
                // ends up being used.
                m_HdrLutFormat = GraphicsFormat.R8G8B8A8_UNorm;

            m_LdrLutFormat = GraphicsFormat.R8G8B8A8_UNorm;
        }

        public void Setup(in RenderTargetHandle internalLut)
        {
            m_InternalLut = internalLut;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get(m_ProfilerTag);

            // Fetch all color grading settings
            var stack = VolumeManager.instance.stack;
            var m_ChannelMixer              = stack.GetComponent<ChannelMixer>();
            var m_ColorAdjustments          = stack.GetComponent<ColorAdjustments>();
            var m_Curves                    = stack.GetComponent<ColorCurves>();
            var m_LiftGammaGain             = stack.GetComponent<LiftGammaGain>();
            var m_ShadowsMidtonesHighlights = stack.GetComponent<ShadowsMidtonesHighlights>();
            var m_SplitToning               = stack.GetComponent<SplitToning>();
            var m_Tonemapping               = stack.GetComponent<Tonemapping>();
            var m_WhiteBalance              = stack.GetComponent<WhiteBalance>();

            ref var postProcessingData = ref renderingData.postProcessingData;
            bool hdr = postProcessingData.gradingMode == ColorGradingMode.HighDynamicRange;

            // Prepare texture & material
            int lutHeight = postProcessingData.lutSize;
            int lutWidth = lutHeight * lutHeight;
            var format = hdr ? m_HdrLutFormat : m_LdrLutFormat;
            var material = hdr ? m_LutBuilderHdr : m_LutBuilderLdr;
            var desc = new RenderTextureDescriptor(lutWidth, lutHeight, format, 0);
            desc.vrUsage = VRTextureUsage.None; // We only need one for both eyes in VR
            cmd.GetTemporaryRT(m_InternalLut.id, desc, FilterMode.Bilinear);

            // Prepare data
            var lmsColorBalance = ColorUtils.ColorBalanceToLMSCoeffs(m_WhiteBalance.temperature.value, m_WhiteBalance.tint.value);
            var hueSatCon = new Vector4(m_ColorAdjustments.hueShift.value / 360f, m_ColorAdjustments.saturation.value / 100f + 1f, m_ColorAdjustments.contrast.value / 100f + 1f, 0f);
            var channelMixerR = new Vector4(m_ChannelMixer.redOutRedIn.value / 100f, m_ChannelMixer.redOutGreenIn.value / 100f, m_ChannelMixer.redOutBlueIn.value / 100f, 0f);
            var channelMixerG = new Vector4(m_ChannelMixer.greenOutRedIn.value / 100f, m_ChannelMixer.greenOutGreenIn.value / 100f, m_ChannelMixer.greenOutBlueIn.value / 100f, 0f);
            var channelMixerB = new Vector4(m_ChannelMixer.blueOutRedIn.value / 100f, m_ChannelMixer.blueOutGreenIn.value / 100f, m_ChannelMixer.blueOutBlueIn.value / 100f, 0f);

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

            // Fill in constants
            material.SetVector(ShaderConstants._Lut_Params, lutParameters);
            material.SetVector(ShaderConstants._ColorBalance, lmsColorBalance);
            material.SetVector(ShaderConstants._ColorFilter, m_ColorAdjustments.colorFilter.value.linear);
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
            material.SetTexture(ShaderConstants._CurveMaster, m_Curves.master.value.GetTexture());
            material.SetTexture(ShaderConstants._CurveRed, m_Curves.red.value.GetTexture());
            material.SetTexture(ShaderConstants._CurveGreen, m_Curves.green.value.GetTexture());
            material.SetTexture(ShaderConstants._CurveBlue, m_Curves.blue.value.GetTexture());

            // Secondary curves
            material.SetTexture(ShaderConstants._CurveHueVsHue, m_Curves.hueVsHue.value.GetTexture());
            material.SetTexture(ShaderConstants._CurveHueVsSat, m_Curves.hueVsSat.value.GetTexture());
            material.SetTexture(ShaderConstants._CurveLumVsSat, m_Curves.lumVsSat.value.GetTexture());
            material.SetTexture(ShaderConstants._CurveSatVsSat, m_Curves.satVsSat.value.GetTexture());

            // Tonemapping (baked into the lut for HDR)
            if (hdr)
            {
                material.shaderKeywords = null;

                switch (m_Tonemapping.mode.value)
                {
                    case TonemappingMode.Neutral: material.EnableKeyword(ShaderKeywordStrings.TonemapNeutral); break;
                    case TonemappingMode.ACES: material.EnableKeyword(ShaderKeywordStrings.TonemapACES); break;
                    default: break; // None
                }
            }

            // Render the lut
            Blit(cmd, m_InternalLut.id, m_InternalLut.id, material);

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void FrameCleanup(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(m_InternalLut.id);
        }

        // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
        static class ShaderConstants
        {
            public static readonly int _Lut_Params        = Shader.PropertyToID("_Lut_Params");
            public static readonly int _ColorBalance      = Shader.PropertyToID("_ColorBalance");
            public static readonly int _ColorFilter       = Shader.PropertyToID("_ColorFilter");
            public static readonly int _ChannelMixerRed   = Shader.PropertyToID("_ChannelMixerRed");
            public static readonly int _ChannelMixerGreen = Shader.PropertyToID("_ChannelMixerGreen");
            public static readonly int _ChannelMixerBlue  = Shader.PropertyToID("_ChannelMixerBlue");
            public static readonly int _HueSatCon         = Shader.PropertyToID("_HueSatCon");
            public static readonly int _Lift              = Shader.PropertyToID("_Lift");
            public static readonly int _Gamma             = Shader.PropertyToID("_Gamma");
            public static readonly int _Gain              = Shader.PropertyToID("_Gain");
            public static readonly int _Shadows           = Shader.PropertyToID("_Shadows");
            public static readonly int _Midtones          = Shader.PropertyToID("_Midtones");
            public static readonly int _Highlights        = Shader.PropertyToID("_Highlights");
            public static readonly int _ShaHiLimits       = Shader.PropertyToID("_ShaHiLimits");
            public static readonly int _SplitShadows      = Shader.PropertyToID("_SplitShadows");
            public static readonly int _SplitHighlights   = Shader.PropertyToID("_SplitHighlights");
            public static readonly int _CurveMaster       = Shader.PropertyToID("_CurveMaster");
            public static readonly int _CurveRed          = Shader.PropertyToID("_CurveRed");
            public static readonly int _CurveGreen        = Shader.PropertyToID("_CurveGreen");
            public static readonly int _CurveBlue         = Shader.PropertyToID("_CurveBlue");
            public static readonly int _CurveHueVsHue     = Shader.PropertyToID("_CurveHueVsHue");
            public static readonly int _CurveHueVsSat     = Shader.PropertyToID("_CurveHueVsSat");
            public static readonly int _CurveLumVsSat     = Shader.PropertyToID("_CurveLumVsSat");
            public static readonly int _CurveSatVsSat     = Shader.PropertyToID("_CurveSatVsSat");
        }
    }
}
