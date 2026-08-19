using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.Rendering.UnifiedRayTracing;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.Tests
{
    [TestFixture("Compute")]
    [TestFixture("Hardware")]
    internal class SurfaceCacheGlobalProbeTests
    {
        readonly RayTracingBackend m_Backend;
        TestHarness m_Harness;

        public SurfaceCacheGlobalProbeTests(string backendAsString)
            => m_Backend = Enum.Parse<RayTracingBackend>(backendAsString);

        [SetUp]
        public void SetUp()
        {
            TestHarness.EnsureSupportedOrIgnore(m_Backend);

            m_Harness = new TestHarness(m_Backend);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            m_Harness?.Dispose();
            return TestHarness.DrainDisposedGpuMemory();
        }

        // Each probe bin projects the radiance over its hemisphere onto SH. For a uniform
        // environment radiance L_env the L0 coefficient is
        // L_0 = ∫_H Y_0(ω) L_env dω
        //     = 1/2 1/sqrt(π) L_env 2π
        //     = sqrt(π) L_env.
        // The probe stores irradiance using Ramamoorthi's technique
        // (https://cseweb.ucsd.edu/~ravir/papers/envmap/envmap.pdf),
        // E_0 = A_hat_0 L_0 = π sqrt(π) L_env = π^(3/2) L_env.
        // The estimator's noise around this value depends on the sampling strategy, which the
        // test deliberately doesn't assume; the assertion tolerance absorbs it.
        static SHRGBL1 ExpectedUniformEnvProbeBin(Color radiance) => new SHRGBL1
        {
            L0 = new Vector3(radiance.r, radiance.g, radiance.b) * Mathf.Pow(Mathf.PI, 1.5f)
        };

        [Test]
        public void GlobalProbe_WhenUniformEnv_ThenEveryBinL0IsRadianceTimesPiToThe3Over2()
        {
            var radiance = new Color(0.7f, 2.0f, 0.3f);
            m_Harness.SetDistanceFallback(true);
            m_Harness.World.SetEnvironmentColor(radiance);
            m_Harness.SetPatches(
                worldPositions: new[] { Vector3.zero },
                worldNormals:   new[] { Vector3.up },
                cellIndices:    new uint[] { 0 },
                irradiances:    new SHRGBL1[1]);

            m_Harness.BeginFrame();
            m_Harness.CommitWorld();
            m_Harness.Estimate();
            m_Harness.EndFrame();

            // The environment cubemap hash advances when the world commit executes, so the probe
            // dispatch that observes the final hash is the one recorded on the following frame.
            m_Harness.BeginFrame();
            m_Harness.Estimate();
            m_Harness.EndFrame();

            SHRGBL1 expected = ExpectedUniformEnvProbeBin(radiance);
            SHRGBL1[] probe = m_Harness.ReadGlobalProbe();
            int expectedBinCount = (int)(SurfaceCacheVolume.AngularResolution * SurfaceCacheVolume.AngularResolution);
            Assert.AreEqual(expectedBinCount, probe.Length, "Global probe bin count");
            for (int binIdx = 0; binIdx < probe.Length; ++binIdx)
                TestHarness.AssertSphericalHarmonicsL0ApproximatelyEqual(expected, probe[binIdx], epsilon: 1e-3f);
        }

        [Test]
        public void GlobalProbe_WhenDistanceFallbackEnabledAfterEnvChange_ThenProbeIsUpdated()
        {
            var radiance = new Color(1.3f, 0.25f, 0.9f);
            m_Harness.World.SetEnvironmentColor(radiance);
            m_Harness.SetPatches(
                worldPositions: new[] { Vector3.zero },
                worldNormals:   new[] { Vector3.up },
                cellIndices:    new uint[] { 0 },
                irradiances:    new SHRGBL1[1]);

            // Frame 1: Estimate() reads the environment hash at recording time, before the commit
            // pass has executed, so it sees the stale value. The new hash only exists after this
            // frame's EndFrame().
            m_Harness.BeginFrame();
            m_Harness.CommitWorld();
            m_Harness.Estimate();
            m_Harness.EndFrame();

            // Frame 2: the first Estimate() that observes the changed hash, still with the probe
            // dispatch off — the frame where the unfixed code updated the hash and lost the
            // pending update.
            m_Harness.BeginFrame();
            m_Harness.Estimate();
            m_Harness.EndFrame();

            // Enable the fallback only now, after the environment change has been observed.
            m_Harness.SetDistanceFallback(true);

            // Frame 3: the first Estimate() with the fallback probe dispatch on. It must schedule a
            // probe update for the environment change frame 2 saw; a probe left at its zeroed
            // initial contents would silently darken everything outside the cascade volume.
            m_Harness.BeginFrame();
            m_Harness.Estimate();
            m_Harness.EndFrame();

            SHRGBL1 expected = ExpectedUniformEnvProbeBin(radiance);
            SHRGBL1[] probe = m_Harness.ReadGlobalProbe();
            for (int binIdx = 0; binIdx < probe.Length; ++binIdx)
                TestHarness.AssertSphericalHarmonicsL0ApproximatelyEqual(expected, probe[binIdx], epsilon: 1e-3f);
        }
    }
}
