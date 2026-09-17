using NUnit.Framework;

namespace UnityEngine.Rendering.Universal.Tests
{
    class ShadowUtilsTests
    {
        const float k_Tolerance = 1e-6f;

        [Description("The legacy mode always uses the fixed 2.5 slope, whatever the bias value or kernel radius. The slope-scale mode maps the normalized bias to a slope between 0 and 5 and scales it by the soft shadow kernel radius. The 7.0 case covers values authored in the legacy mode, where the range goes up to 10. They saturate at the maximum slope instead of scaling past it.")]
        [TestCase(ShadowDepthBiasMode.Legacy, 0.0f, 1.0f, 2.5f)]
        [TestCase(ShadowDepthBiasMode.Legacy, 0.5f, 1.0f, 2.5f)]
        [TestCase(ShadowDepthBiasMode.Legacy, 7.0f, 3.5f, 2.5f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 0.0f, 1.0f, 0.0f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 0.5f, 1.0f, 2.5f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 1.0f, 1.0f, 5.0f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 7.0f, 1.0f, 5.0f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, -1.0f, 1.0f, 0.0f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 1.0f, 3.5f, 17.5f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 1.0f, 2.5f, 12.5f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 1.0f, 1.5f, 7.5f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 0.25f, 3.5f, 4.375f)]
        public void GetSlopeScaleDepthBias_MapsModeBiasAndKernelRadiusToRasterizerSlope(ShadowDepthBiasMode depthBiasMode, float normalizedSlopeScale, float softShadowKernelRadius, float expected)
        {
            Assert.That(ShadowUtils.GetSlopeScaleDepthBias(normalizedSlopeScale, depthBiasMode, softShadowKernelRadius), Is.EqualTo(expected).Within(k_Tolerance));
        }

        [Description("Each soft shadow quality maps to the radius of its PCF kernel. The shadow biases scale with this radius to compensate for the wider filter.")]
        [TestCase(SoftShadowQuality.Low, 1.5f)]
        [TestCase(SoftShadowQuality.Medium, 2.5f)]
        [TestCase(SoftShadowQuality.High, 3.5f)]
        public void GetSoftShadowKernelRadius_MapsSoftShadowQualityToKernelRadius(SoftShadowQuality softShadowQuality, float expected)
        {
            Assert.That(ShadowUtils.GetSoftShadowKernelRadius(softShadowQuality), Is.EqualTo(expected).Within(k_Tolerance));
        }

        [Description("A light set to UsePipelineSettings takes its soft shadow quality from the pipeline asset. The bias scaling and the shader filtering then agree on the kernel.")]
        [TestCase(SoftShadowQuality.Low)]
        [TestCase(SoftShadowQuality.Medium)]
        [TestCase(SoftShadowQuality.High)]
        public void ResolveSoftShadowQuality_UsePipelineSettings_ResolvesToAssetQuality(SoftShadowQuality assetQuality)
        {
            var asset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            try
            {
                asset.softShadowQuality = assetQuality;
                Assert.That(ShadowUtils.ResolveSoftShadowQuality(SoftShadowQuality.UsePipelineSettings, asset), Is.EqualTo(assetQuality));
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Description("An explicit per-light soft shadow quality is used as is.")]
        [TestCase(SoftShadowQuality.Low)]
        [TestCase(SoftShadowQuality.High)]
        public void ResolveSoftShadowQuality_ExplicitQuality_PassesThrough(SoftShadowQuality softShadowQuality)
        {
            Assert.That(ShadowUtils.ResolveSoftShadowQuality(softShadowQuality, null), Is.EqualTo(softShadowQuality));
        }

        [Description("If the asset's own quality is also UsePipelineSettings, the resolved quality falls back to Low. A value below Low would reach the shader as zero, which reads as soft shadows turned off.")]
        [Test]
        public void ResolveSoftShadowQuality_AssetQualityIsUsePipelineSettings_ClampsToLow()
        {
            var asset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            try
            {
                asset.softShadowQuality = SoftShadowQuality.UsePipelineSettings;
                Assert.That(ShadowUtils.ResolveSoftShadowQuality(SoftShadowQuality.UsePipelineSettings, asset), Is.EqualTo(SoftShadowQuality.Low));
            }
            finally
            {
                Object.DestroyImmediate(asset);
            }
        }

        [Description("Without a pipeline asset the quality falls back to Medium, the default of a new asset.")]
        [Test]
        public void ResolveSoftShadowQuality_UsePipelineSettingsWithoutAsset_FallsBackToMedium()
        {
            Assert.That(ShadowUtils.ResolveSoftShadowQuality(SoftShadowQuality.UsePipelineSettings, null), Is.EqualTo(SoftShadowQuality.Medium));
        }

        [Description("A light without a UniversalAdditionalLightData component resolves its soft shadow quality from the pipeline asset, so the shader filter agrees with the kernel radius path.")]
        [Test]
        public void GetShadowStrengthAndSoftShadows_LightWithoutAdditionalData_ResolvesFromAsset()
        {
            var asset = ScriptableObject.CreateInstance<UniversalRenderPipelineAsset>();
            var go = new GameObject();
            try
            {
                asset.softShadowQuality = SoftShadowQuality.High;
                var light = go.AddComponent<Light>();
                light.shadowStrength = 0.5f;

                ShadowUtils.GetShadowStrengthAndSoftShadows(light, null, true, asset, out float shadowStrength, out float softShadows);

                Assert.That(softShadows, Is.EqualTo((int)SoftShadowQuality.High));
                Assert.That(shadowStrength, Is.EqualTo(0.5f));
            }
            finally
            {
                Object.DestroyImmediate(go);
                Object.DestroyImmediate(asset);
            }
        }

        [Description("A slope-scale bias of 0.5 produces exactly the fixed slope of the legacy mode, so the two modes render identical hard shadow maps at that value.")]
        [Test]
        public void GetSlopeScaleDepthBias_SlopeScaleAtHalf_EqualsLegacySlope()
        {
            float legacySlope = ShadowUtils.GetSlopeScaleDepthBias(123.0f, ShadowDepthBiasMode.Legacy, 1.0f);
            float slopeScaleAtHalf = ShadowUtils.GetSlopeScaleDepthBias(0.5f, ShadowDepthBiasMode.SlopeScale, 1.0f);
            Assert.That(slopeScaleAtHalf, Is.EqualTo(legacySlope));
        }

        [Description("The receiver-side offset is 0 in the caster mode, where the normal bias is applied to the caster geometry instead. In the receiver mode the sampling position moves by the bias scaled by the world-space texel size and the soft shadow kernel radius.")]
        [TestCase(ShadowNormalBiasMode.Caster, 1.0f, 1.0f, 1.0f, 0.0f)]
        [TestCase(ShadowNormalBiasMode.Caster, 7.0f, 0.5f, 3.5f, 0.0f)]
        [TestCase(ShadowNormalBiasMode.Receiver, 0.0f, 1.0f, 1.0f, 0.0f)]
        [TestCase(ShadowNormalBiasMode.Receiver, 1.0f, 1.0f, 1.0f, 1.0f)]
        [TestCase(ShadowNormalBiasMode.Receiver, 1.0f, 0.25f, 1.0f, 0.25f)]
        [TestCase(ShadowNormalBiasMode.Receiver, 1.0f, 1.0f, 3.5f, 3.5f)]
        [TestCase(ShadowNormalBiasMode.Receiver, 0.5f, 0.03f, 2.5f, 0.0375f)]
        public void GetReceiverNormalBias_MapsModeBiasTexelSizeAndKernelRadiusToWorldSpaceOffset(ShadowNormalBiasMode normalBiasMode, float normalBias, float texelSize, float softShadowKernelRadius, float expected)
        {
            Assert.That(ShadowUtils.GetReceiverNormalBias(normalBias, normalBiasMode, texelSize, softShadowKernelRadius), Is.EqualTo(expected).Within(k_Tolerance));
        }

        [Description("In the caster mode the caster vertices move against the normal by bias times world-space texel size, so the sign is negative. Point lights are kept at 0 to match the Built-in pipeline. In the receiver mode the channel is zeroed because the offset moves to the sampling side.")]
        [TestCase(ShadowNormalBiasMode.Caster, 1.0f, 1.0f, LightType.Directional, -1.0f)]
        [TestCase(ShadowNormalBiasMode.Caster, 2.0f, 0.5f, LightType.Spot, -1.0f)]
        [TestCase(ShadowNormalBiasMode.Caster, 1.0f, 1.0f, LightType.Point, 0.0f)]
        [TestCase(ShadowNormalBiasMode.Receiver, 1.0f, 1.0f, LightType.Directional, 0.0f)]
        [TestCase(ShadowNormalBiasMode.Receiver, 1.0f, 1.0f, LightType.Point, 0.0f)]
        public void GetCasterNormalBias_MapsModeBiasAndLightTypeToCasterOffset(ShadowNormalBiasMode normalBiasMode, float normalBias, float texelSize, LightType lightType, float expected)
        {
            Assert.That(ShadowUtils.GetCasterNormalBias(normalBias, normalBiasMode, texelSize, lightType), Is.EqualTo(expected).Within(k_Tolerance));
        }

        [Description("Point lights never get a caster-side normal bias, matching the Built-in pipeline. The receiver channel has no such exception, so a point light's normal bias only takes effect in the receiver mode.")]
        [Test]
        public void NormalBias_PointLight_OnlyTakesEffectInReceiverMode()
        {
            Assert.That(ShadowUtils.GetCasterNormalBias(1.0f, ShadowNormalBiasMode.Caster, 1.0f, LightType.Point), Is.EqualTo(0.0f).Within(k_Tolerance));
            Assert.That(ShadowUtils.GetCasterNormalBias(1.0f, ShadowNormalBiasMode.Receiver, 1.0f, LightType.Point), Is.EqualTo(0.0f).Within(k_Tolerance));
            Assert.That(ShadowUtils.GetReceiverNormalBias(1.0f, ShadowNormalBiasMode.Receiver, 1.0f, 2.5f), Is.EqualTo(2.5f).Within(k_Tolerance));
        }

        [Description("For a non-point light the receiver offset is the caster offset with the sign flipped, soft shadow kernel compensation included. Switching modes keeps the authored bias at the same strength.")]
        [Test]
        public void NormalBias_ModeSwitch_MovesTheSameOffsetBetweenChannels()
        {
            float softShadowKernelRadius = ShadowUtils.GetSoftShadowKernelRadius(SoftShadowQuality.High);
            float casterNormalBias = ShadowUtils.GetCasterNormalBias(0.7f, ShadowNormalBiasMode.Caster, 0.42f, LightType.Directional) * softShadowKernelRadius;
            float receiverNormalBias = ShadowUtils.GetReceiverNormalBias(0.7f, ShadowNormalBiasMode.Receiver, 0.42f, softShadowKernelRadius);
            Assert.That(receiverNormalBias, Is.EqualTo(-casterNormalBias).Within(k_Tolerance));
        }
    }
}
