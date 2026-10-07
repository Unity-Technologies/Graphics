using NUnit.Framework;

namespace UnityEngine.Rendering.Universal.Tests
{
    class ShadowUtilsTests
    {
        const float k_Tolerance = 1e-6f;

        [Description("The legacy mode always uses the fixed 2.5 slope, whatever the bias value or kernel radius. The slope-scale mode maps the normalized bias to a slope between the fixed legacy slope and 5 times the kernel radius. The 7.0 case covers values authored in the legacy mode, where the range goes up to 10. They saturate at the maximum slope instead of scaling past it.")]
        [TestCase(ShadowDepthBiasMode.Legacy, 0.0f, 1.0f, 2.5f)]
        [TestCase(ShadowDepthBiasMode.Legacy, 0.5f, 1.0f, 2.5f)]
        [TestCase(ShadowDepthBiasMode.Legacy, 7.0f, 3.5f, 2.5f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 0.0f, 1.0f, 2.5f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 0.5f, 1.0f, 3.75f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 1.0f, 1.0f, 5.0f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 7.0f, 1.0f, 5.0f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, -1.0f, 1.0f, 2.5f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 0.0f, 1.5f, 2.5f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 0.0f, 2.5f, 2.5f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 0.0f, 3.5f, 2.5f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 1.0f, 3.5f, 17.5f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 1.0f, 2.5f, 12.5f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 1.0f, 1.5f, 7.5f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 0.25f, 3.5f, 6.25f)]
        public void GetSlopeScaleDepthBias_MapsModeBiasAndKernelRadiusToRasterizerSlope(ShadowDepthBiasMode depthBiasMode, float normalizedSlopeScale, float softShadowKernelRadius, float expected)
        {
            Assert.That(ShadowUtils.GetSlopeScaleDepthBias(normalizedSlopeScale, depthBiasMode, softShadowKernelRadius), Is.EqualTo(expected).Within(k_Tolerance));
        }

        [Description("The legacy mode pushes casters away from the light by the bias value in texels. The slope-scale mode keeps a fixed 0.05 texel push instead, because the slope term and the caster normal bias both vanish on receivers facing the light head-on.")]
        [TestCase(ShadowDepthBiasMode.Legacy, 1.0f, 2.0f, -2.0f)]
        [TestCase(ShadowDepthBiasMode.Legacy, 0.05f, 2.0f, -0.1f)]
        [TestCase(ShadowDepthBiasMode.Legacy, 0.0f, 2.0f, 0.0f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 0.0f, 2.0f, -0.1f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 1.0f, 2.0f, -0.1f)]
        [TestCase(ShadowDepthBiasMode.SlopeScale, 0.5f, 0.4f, -0.02f)]
        public void GetCasterDepthBias_MapsModeAndTexelSizeToCasterOffset(ShadowDepthBiasMode depthBiasMode, float depthBias, float texelSize, float expected)
        {
            Assert.That(ShadowUtils.GetCasterDepthBias(depthBias, depthBiasMode, texelSize), Is.EqualTo(expected).Within(k_Tolerance));
        }

        [Description("Pins the PCF kernel radii the soft shadow qualities compensate the shadow biases with.")]
        [TestCase(SoftShadowQuality.Low, 1.5f)]
        [TestCase(SoftShadowQuality.Medium, 2.5f)]
        [TestCase(SoftShadowQuality.High, 3.5f)]
        public void GetSoftShadowKernelRadius_MapsSoftShadowQualityToKernelRadius(SoftShadowQuality softShadowQuality, float expected)
        {
            Assert.That(ShadowUtils.GetSoftShadowKernelRadius(softShadowQuality), Is.EqualTo(expected).Within(k_Tolerance));
        }

        [Description("A slope-scale bias of 0 produces exactly the fixed slope of the legacy mode, so switching an existing project to the slope-scale mode never renders a shadow map with less slope bias than before.")]
        [Test]
        public void GetSlopeScaleDepthBias_SlopeScaleAtZero_EqualsLegacySlope()
        {
            float legacySlope = ShadowUtils.GetSlopeScaleDepthBias(123.0f, ShadowDepthBiasMode.Legacy, 1.0f);
            float slopeScaleAtZero = ShadowUtils.GetSlopeScaleDepthBias(0.0f, ShadowDepthBiasMode.SlopeScale, 1.0f);
            Assert.That(slopeScaleAtZero, Is.EqualTo(legacySlope));
        }

        [Description("For every soft shadow kernel the slope-scale floor equals the legacy slope, so no shadow map gets less slope bias than the legacy mode at any slider value.")]
        [TestCase(1.0f)]
        [TestCase(1.5f)]
        [TestCase(2.5f)]
        [TestCase(3.5f)]
        public void GetSlopeScaleDepthBias_FloorNeverBelowLegacySlope(float softShadowKernelRadius)
        {
            float legacySlope = ShadowUtils.GetSlopeScaleDepthBias(0.0f, ShadowDepthBiasMode.Legacy, softShadowKernelRadius);
            float slopeScaleFloor = ShadowUtils.GetSlopeScaleDepthBias(0.0f, ShadowDepthBiasMode.SlopeScale, softShadowKernelRadius);
            Assert.That(slopeScaleFloor, Is.EqualTo(legacySlope).Within(k_Tolerance));
        }
    }
}
