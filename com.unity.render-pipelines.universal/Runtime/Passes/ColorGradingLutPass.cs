using UnityEngine.Experimental.Rendering;

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
        readonly GraphicsFormat m_HdrLutFormat;
        readonly GraphicsFormat m_LdrLutFormat;

        RTHandle m_InternalLut;

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
                m_HdrLutFormat = GraphicsFormat.B10G11R11_UFloatPack32;
            else
                // Obviously using this for log lut encoding is a very bad idea for precision but we
                // need it for compatibility reasons and avoid black screens on platforms that don't
                // support floating point formats. Expect banding and posterization artifact if this
                // ends up being used.
                m_HdrLutFormat = GraphicsFormat.R8G8B8A8_UNorm;

            m_LdrLutFormat = GraphicsFormat.R8G8B8A8_UNorm;
            base.useNativeRenderPass = false;
        }

        public void Setup(in RTHandle internalLut)
        {
            m_InternalLut = internalLut;
        }

        /// <inheritdoc/>
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            var cmd = CommandBufferPool.Get();
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
                int lutHeight = postProcessingData.lutSize;
                int lutWidth = lutHeight * lutHeight;
                var format = hdr ? m_HdrLutFormat : m_LdrLutFormat;
                var material = hdr ? m_LutBuilderHdr : m_LutBuilderLdr;
                var desc = new RenderTextureDescriptor(lutWidth, lutHeight, format, 0);
                desc.vrUsage = VRTextureUsage.None; // We only need one for both eyes in VR
                cmd.GetTemporaryRT(Shader.PropertyToID(m_InternalLut.name), desc, FilterMode.Bilinear);

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

                var(shadows, midtones, highlights) = ColorUtils.PrepareShadowsMidtonesHighlights(
                    shadowsMidtonesHighlights.shadows.value,
                    shadowsMidtonesHighlights.midtones.value,
                    shadowsMidtonesHighlights.highlights.value
                );

                var(lift, gamma, gain) = ColorUtils.PrepareLiftGammaGain(
                    liftGammaGain.lift.value,
                    liftGammaGain.gamma.value,
                    liftGammaGain.gain.value
                );

                var(splitShadows, splitHighlights) = ColorUtils.PrepareSplitToning(
                    splitToning.shadows.value,
                    splitToning.highlights.value,
                    splitToning.balance.value
                );

                var lutParameters = new Vector4(lutHeight, 0.5f / lutWidth, 0.5f / lutHeight,
                    lutHeight / (lutHeight - 1f));

                // Fill in constants
                material.SetVector(URPShaderIDs._Lut_Params, lutParameters);
                material.SetVector(URPShaderIDs._ColorBalance, lmsColorBalance);
                material.SetVector(URPShaderIDs._ColorFilter, colorAdjustments.colorFilter.value.linear);
                material.SetVector(URPShaderIDs._ChannelMixerRed, channelMixerR);
                material.SetVector(URPShaderIDs._ChannelMixerGreen, channelMixerG);
                material.SetVector(URPShaderIDs._ChannelMixerBlue, channelMixerB);
                material.SetVector(URPShaderIDs._HueSatCon, hueSatCon);
                material.SetVector(URPShaderIDs._Lift, lift);
                material.SetVector(URPShaderIDs._Gamma, gamma);
                material.SetVector(URPShaderIDs._Gain, gain);
                material.SetVector(URPShaderIDs._Shadows, shadows);
                material.SetVector(URPShaderIDs._Midtones, midtones);
                material.SetVector(URPShaderIDs._Highlights, highlights);
                material.SetVector(URPShaderIDs._ShaHiLimits, shadowsHighlightsLimits);
                material.SetVector(URPShaderIDs._SplitShadows, splitShadows);
                material.SetVector(URPShaderIDs._SplitHighlights, splitHighlights);

                // YRGB curves
                material.SetTexture(URPShaderIDs._CurveMaster, curves.master.value.GetTexture());
                material.SetTexture(URPShaderIDs._CurveRed, curves.red.value.GetTexture());
                material.SetTexture(URPShaderIDs._CurveGreen, curves.green.value.GetTexture());
                material.SetTexture(URPShaderIDs._CurveBlue, curves.blue.value.GetTexture());

                // Secondary curves
                material.SetTexture(URPShaderIDs._CurveHueVsHue, curves.hueVsHue.value.GetTexture());
                material.SetTexture(URPShaderIDs._CurveHueVsSat, curves.hueVsSat.value.GetTexture());
                material.SetTexture(URPShaderIDs._CurveLumVsSat, curves.lumVsSat.value.GetTexture());
                material.SetTexture(URPShaderIDs._CurveSatVsSat, curves.satVsSat.value.GetTexture());

                // Tonemapping (baked into the lut for HDR)
                if (hdr)
                {
                    material.shaderKeywords = null;

                    switch (tonemapping.mode.value)
                    {
                        case TonemappingMode.Neutral: material.EnableKeyword(ShaderKeywordStrings.TonemapNeutral); break;
                        case TonemappingMode.ACES: material.EnableKeyword(ShaderKeywordStrings.TonemapACES); break;
                        default: break; // None
                    }
                }

                renderingData.cameraData.xr.StopSinglePass(cmd);

                // Render the lut
                cmd.Blit(null, m_InternalLut, material);

                renderingData.cameraData.xr.StartSinglePass(cmd);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        /// <inheritdoc/>
        public override void OnFinishCameraStackRendering(CommandBuffer cmd)
        {
            cmd.ReleaseTemporaryRT(Shader.PropertyToID(m_InternalLut.name));
        }

        public void Cleanup()
        {
            CoreUtils.Destroy(m_LutBuilderLdr);
            CoreUtils.Destroy(m_LutBuilderHdr);
        }
    }
}
