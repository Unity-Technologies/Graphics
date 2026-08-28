using System;
using NUnit.Framework;

namespace UnityEngine.Rendering.Universal.Tests
{
    // Unit tests for the adjustable AgX tone curve, which lives in HLSL (AgxAdjustableCurve in Color.hlsl).
    // They lock the curve contract (mid-grey exactness, continuity at the crossover, monotonicity,
    // boundedness, HDR shoulder retarget) via a C# mirror, so a future edit that breaks an invariant fails
    // here rather than only showing up as a subtle image regression.
    class AgXTonemappingTests
    {
        const float k_InCrossover = 0.18f; // input middle grey (toe/shoulder split), matches Color.hlsl
        const float k_White = 16.29f;      // input white point (~2^4.026), matches Color.hlsl

        // The curve body without the white-point clamp, so the tests can check that clamping the input is
        // equivalent to clamping the output (the property the clamp in Color.hlsl relies on).
        static double CurveUnclamped(double x, double outputMax, float contrast, float midGrey)
        {
            Tonemapping.GetAgXCurveConstants(contrast, midGrey, out float toeAf, out float slopeF);
            double toeA = toeAf, slope = slopeF;

            if (x >= k_InCrossover)
            {
                double shoulderMax = outputMax - midGrey;
                double w = ((k_White - k_InCrossover) * (k_White - k_InCrossover) / shoulderMax) * slope;
                double s = x - k_InCrossover;
                double slopeS = slope * s;
                return slopeS * (1.0 + s / w) / (1.0 + slopeS / shoulderMax) + midGrey;
            }
            double t = Math.Pow(x, contrast);
            return t / (t + toeA);
        }

        // C# mirror of AgxAdjustableCurve (Color.hlsl), fed by the real Tonemapping.GetAgXCurveConstants.
        static double Curve(double x, double outputMax, float contrast, float midGrey)
            => CurveUnclamped(Math.Min(x, k_White), outputMax, contrast, midGrey);

        // (contrast, midGrey) covering the shipping default plus the slider extremes.
        static readonly (float contrast, float midGrey)[] k_Params =
        {
            (1.193f, 0.18f),   // shipping default
            (1.0f, 0.2145f),
            (1.25f, 0.18f),
            (0.5f, 0.10f),     // slider extremes
            (3.0f, 0.30f),
        };

        // Output headroom values spanning SDR through a wide HDR retarget.
        static readonly double[] k_OutputMaxes = { 1.0, 2.0, 4.0, 16.0 };

        // The shoulder-hits-outputMax identity is exact in exact arithmetic, but this mirror inherits the
        // shader's float constants: s is (x - k_InCrossover) in double, while the w term uses
        // (k_White - k_InCrossover) constant-folded in float. Those differ by ~3e-7, so the cancellation
        // leaves a residual that grows with outputMax (~3e-7 absolute at outputMax = 16). Scale the tolerance
        // with outputMax accordingly — still ~5 orders tighter than any behavioural break in the clamp.
        static double IdentityTolerance(double outputMax) => outputMax * 1e-6;

        [Test]
        public void MidGrey_MapsToOutCrossover_Exactly([ValueSource(nameof(k_Params))] (float contrast, float midGrey) p)
        {
            // Input mid-grey (0.18) must map to the requested output mid-grey — the defining property of the
            // "Mid Gray" control. Uses the real precomputed constants.
            double y = Curve(k_InCrossover, 1.0, p.contrast, p.midGrey);
            Assert.That(y, Is.EqualTo(p.midGrey).Within(1e-5), "mid-grey did not map to the mid-grey control value");
        }

        [Test]
        public void Curve_IsContinuous_AtCrossover([ValueSource(nameof(k_Params))] (float contrast, float midGrey) p)
        {
            // Toe and shoulder must agree at the crossover (no visible kink).
            double toe = Curve(k_InCrossover - 1e-4, 1.0, p.contrast, p.midGrey);
            double shoulder = Curve(k_InCrossover + 1e-4, 1.0, p.contrast, p.midGrey);
            Assert.That(toe, Is.EqualTo(shoulder).Within(1e-3), "toe and shoulder disagree at the crossover");
        }

        [Test]
        public void Curve_IsMonotonicAndBounded_OverInputRange([ValueSource(nameof(k_Params))] (float contrast, float midGrey) p)
        {
            const double outputMax = 1.0; // SDR
            double prev = -1.0;
            const int steps = 4096;
            for (int i = 0; i <= steps; i++)
            {
                double x = (double)i / steps * k_White; // 0 .. white point
                double y = Curve(x, outputMax, p.contrast, p.midGrey);

                Assert.That(y, Is.GreaterThanOrEqualTo(prev - 1e-6), $"curve not monotonic at x={x}");
                Assert.That(y, Is.GreaterThanOrEqualTo(-1e-6).And.LessThanOrEqualTo(outputMax + 1e-4), $"curve out of [0, outputMax] at x={x}");
                prev = y;
            }
        }

        [Test]
        public void Curve_RetargetsShoulder_ToHdrHeadroom()
        {
            // On the HDR path outputMax > 1: the shoulder must reach above SDR white while the dark-to-mid toe
            // stays anchored (unchanged) — the property that makes the curve SDR/HDR/EDR-stable.
            const float contrast = 1.193f, midGrey = 0.18f;
            double sdrTop = Curve(k_White, 1.0, contrast, midGrey);
            double hdrTop = Curve(k_White, 4.0, contrast, midGrey);
            Assert.That(hdrTop, Is.GreaterThan(sdrTop), "HDR shoulder did not extend above SDR white");

            // Toe (below mid-grey) is independent of outputMax.
            double toeSdr = Curve(0.05, 1.0, contrast, midGrey);
            double toeHdr = Curve(0.05, 4.0, contrast, midGrey);
            Assert.That(toeHdr, Is.EqualTo(toeSdr).Within(1e-6), "toe changed with output headroom");
        }

        [Test]
        public void Shoulder_ReachesOutputMax_AtWhitePoint(
            [ValueSource(nameof(k_Params))] (float contrast, float midGrey) p,
            [ValueSource(nameof(k_OutputMaxes))] double outputMax)
        {
            // The shoulder hits outputMax at the white point, for any outputMax — the shoulderMax terms
            // cancel. This is what licenses the min(x, white) clamp in AgxAdjustableCurve: without it the
            // clamp would visibly cap highlights below the display ceiling.
            double y = CurveUnclamped(k_White, outputMax, p.contrast, p.midGrey);
            Assert.That(y, Is.EqualTo(outputMax).Within(IdentityTolerance(outputMax)), "shoulder did not reach outputMax at the white point");
        }

        [Test]
        public void InputClamp_IsEquivalent_ToClampingTheOutput(
            [ValueSource(nameof(k_Params))] (float contrast, float midGrey) p,
            [ValueSource(nameof(k_OutputMaxes))] double outputMax)
        {
            // Above the white point the curve keeps growing (linearly), so it used to be the min(outputMax, ..)
            // in AgxTonemap that flattened it. Clamping the input to the white point instead must produce the
            // identical result — that equivalence is the whole reason the fp16 overflow fix is free.
            foreach (double x in new[] { (double)k_White, 20.0, 41.0, 1.0e3, 6.5e4 })
            {
                double clampedInput = Curve(x, outputMax, p.contrast, p.midGrey);
                double clampedOutput = Math.Min(outputMax, CurveUnclamped(x, outputMax, p.contrast, p.midGrey));
                double tolerance = IdentityTolerance(outputMax);
                Assert.That(clampedInput, Is.EqualTo(clampedOutput).Within(tolerance), $"input and output clamping disagree at x={x}");
                Assert.That(clampedInput, Is.EqualTo(outputMax).Within(tolerance), $"curve did not sit at outputMax above the white point at x={x}");
            }
        }
    }
}
