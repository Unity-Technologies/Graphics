using NUnit.Framework;

namespace UnityEngine.Rendering.Universal.Tests
{
    class ShadowUtilsTests
    {
        const float k_Tolerance = 1e-6f;

        [Description("Pins the mode-to-rasterizer-slope mapping. The legacy mode always uses the fixed 2.5 slope regardless of the bias value or kernel radius, and the slope-scale mode scales the normalized bias to [0..5] before the soft shadow kernel compensation. The 7.0 case guards the migration clamp: values authored in the legacy mode (range [0..10]) must saturate at the maximum slope instead of scaling past it.")]
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

        [Description("Pins the PCF kernel radii the soft shadow qualities compensate the shadow biases with.")]
        [TestCase(SoftShadowQuality.Low, 1.5f)]
        [TestCase(SoftShadowQuality.Medium, 2.5f)]
        [TestCase(SoftShadowQuality.High, 3.5f)]
        public void GetSoftShadowKernelRadius_MapsSoftShadowQualityToKernelRadius(SoftShadowQuality softShadowQuality, float expected)
        {
            Assert.That(ShadowUtils.GetSoftShadowKernelRadius(softShadowQuality), Is.EqualTo(expected).Within(k_Tolerance));
        }

        [Description("The equivalence the two modes are designed around: a slope-scale bias of 0.5 produces exactly the fixed legacy slope, so a legacy project at bias 0 and a slope-scale project at bias 0.5 render identical shadow maps for hard shadows.")]
        [Test]
        public void GetSlopeScaleDepthBias_SlopeScaleAtHalf_EqualsLegacySlope()
        {
            float legacySlope = ShadowUtils.GetSlopeScaleDepthBias(123.0f, ShadowDepthBiasMode.Legacy, 1.0f);
            float slopeScaleAtHalf = ShadowUtils.GetSlopeScaleDepthBias(0.5f, ShadowDepthBiasMode.SlopeScale, 1.0f);
            Assert.That(slopeScaleAtHalf, Is.EqualTo(legacySlope));
        }
    }
}
