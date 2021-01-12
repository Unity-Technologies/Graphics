using UnityEngine.Experimental.Rendering;
using UnityEngine.Experimental.Rendering.RenderGraphModule;

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class PostProcessSystem
    {
        ColorGradingParameters SensorSDKPrepareColorGradingParameters(ComputeShader cs, int kernel, HDCamera camera)
        {
            var parameters = new ColorGradingParameters();

            parameters.tonemappingMode = m_TonemappingFS ? m_Tonemapping.mode.value : TonemappingMode.None;

            parameters.builderCS = cs;
            parameters.builderKernel = kernel;
            parameters.camera = camera;

            // Setup lut builder compute & grab the kernel we need
            parameters.builderCS.shaderKeywords = null;

            if (m_Tonemapping.IsActive() && m_TonemappingFS)
            {
                switch (parameters.tonemappingMode)
                {
                    case TonemappingMode.Neutral: parameters.builderCS.EnableKeyword("TONEMAPPING_NEUTRAL"); break;
                    case TonemappingMode.ACES: parameters.builderCS.EnableKeyword("TONEMAPPING_ACES"); break;
                    case TonemappingMode.Custom: parameters.builderCS.EnableKeyword("TONEMAPPING_CUSTOM"); break;
                    case TonemappingMode.External: parameters.builderCS.EnableKeyword("TONEMAPPING_EXTERNAL"); break;
                }
            }
            else
            {
                parameters.builderCS.EnableKeyword("TONEMAPPING_NONE");
            }

            parameters.lutSize = m_LutSize;

            //parameters.colorFilter;
            parameters.lmsColorBalance = GetColorBalanceCoeffs(m_WhiteBalance.temperature.value, m_WhiteBalance.tint.value);
            parameters.hueSatCon = new Vector4(m_ColorAdjustments.hueShift.value / 360f, m_ColorAdjustments.saturation.value / 100f + 1f, m_ColorAdjustments.contrast.value / 100f + 1f, 0f);
            parameters.channelMixerR = new Vector4(m_ChannelMixer.redOutRedIn.value / 100f, m_ChannelMixer.redOutGreenIn.value / 100f, m_ChannelMixer.redOutBlueIn.value / 100f, 0f);
            parameters.channelMixerG = new Vector4(m_ChannelMixer.greenOutRedIn.value / 100f, m_ChannelMixer.greenOutGreenIn.value / 100f, m_ChannelMixer.greenOutBlueIn.value / 100f, 0f);
            parameters.channelMixerB = new Vector4(m_ChannelMixer.blueOutRedIn.value / 100f, m_ChannelMixer.blueOutGreenIn.value / 100f, m_ChannelMixer.blueOutBlueIn.value / 100f, 0f);

            ComputeShadowsMidtonesHighlights(out parameters.shadows, out parameters.midtones, out parameters.highlights, out parameters.shadowsHighlightsLimits);
            ComputeLiftGammaGain(out parameters.lift, out parameters.gamma, out parameters.gain);
            ComputeSplitToning(out parameters.splitShadows, out parameters.splitHighlights);

            // Be careful, if m_Curves is modified between preparing the render pass and executing it, result will be wrong.
            // However this should be fine for now as all updates should happen outisde rendering.
            parameters.curves = m_Curves;

            if (parameters.tonemappingMode == TonemappingMode.Custom)
            {
                parameters.hableCurve = m_HableCurve;
                parameters.hableCurve.Init(
                        m_Tonemapping.toeStrength.value,
                        m_Tonemapping.toeLength.value,
                        m_Tonemapping.shoulderStrength.value,
                        m_Tonemapping.shoulderLength.value,
                        m_Tonemapping.shoulderAngle.value,
                        m_Tonemapping.gamma.value
                    );
            }
            else if (parameters.tonemappingMode == TonemappingMode.External)
            {
                parameters.externalLuT = m_Tonemapping.lutTexture.value;
                parameters.lutContribution = m_Tonemapping.lutContribution.value;
            }

            parameters.colorFilter = m_ColorAdjustments.colorFilter.value.linear;
            parameters.miscParams = new Vector4(m_ColorGradingFS ? 1f : 0f, 0f, 0f, 0f);

            return parameters;
        }

        public void GenerateColorGrading(CommandBuffer cmd, ComputeShader cs, int kernel, HDCamera camera)
        {
            var parameters = SensorSDKPrepareColorGradingParameters(cs, kernel, camera);
            DoColorGrading(parameters, m_InternalLogLut, cmd);
        }
    }
}
