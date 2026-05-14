using NUnit.Framework;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.Tests
{
    [TestFixture]
    [PrebuildSetup(typeof(TestPrebuildSetup))]
    internal class SurfaceCacheUniformEnvLightTests
    {
        TestHarness m_Harness;

        [SetUp]
        public void SetUp()
        {
            // Assert.Ignore (not Assume.That) — UTR maps Inconclusive into the failure bucket;
            // Ignore is reported as Skipped, like the [Ignore] attribute.
            if (SystemInfo.renderingThreadingMode == RenderingThreadingMode.NativeGraphicsJobsSplitThreading)
                Assert.Ignore("Skipping under NativeGraphicsJobsSplitThreading; tracked in GFXLIGHT-2292.");
            m_Harness = new TestHarness();
        }

        [TearDown]
        public void TearDown() => m_Harness?.Dispose();

        [Test]
        public void Env_WhenNoLights_ThenZeroIrradiance()
        {
            m_Harness.SetPatches(
                worldPositions: new[] { Vector3.zero },
                worldNormals:   new[] { Vector3.up },
                cellIndices:    new uint[] { 0 },
                irradiances:    new SHRGBL1[1]);

            m_Harness.BeginFrame();
            m_Harness.CommitWorld();
            m_Harness.Estimate();
            m_Harness.EndFrame();

            for (uint i = 1; i < 16; i++)
            {
                m_Harness.BeginFrame();
                m_Harness.Estimate();
                m_Harness.EndFrame();
            }

            SHRGBL1 actual = m_Harness.ReadPatchIrradiance(patchIndex: 0);
            TestHarness.AssertL0IrradianceApproximatelyEqual(
                expected: default, actual, epsilon: 1e-4f);
        }

        [Test]
        public void Env_WhenArbitraryNormalAndIntensity_ThenL0EqualsRadianceTimesPiToThe3Over2()
        {
            var radiance = new Color(0.7f, 2.0f, 0.3f);
            m_Harness.World.SetEnvironmentColor(radiance);

            Vector3 arbitraryNormal = new Vector3(0.42f, 0.7f, 0.39f).normalized;
            m_Harness.SetPatches(
                worldPositions: new[] { Vector3.zero },
                worldNormals:   new[] { arbitraryNormal },
                cellIndices:    new uint[] { 0 },
                irradiances:    new SHRGBL1[1]);

            m_Harness.BeginFrame();
            m_Harness.CommitWorld();
            m_Harness.Estimate();
            m_Harness.EndFrame();

            for (uint i = 1; i < 16; i++)
            {
                m_Harness.BeginFrame();
                m_Harness.Estimate();
                m_Harness.EndFrame();
            }

            // The radiance SHL0 coefficient should be
            // L_0 = ∫_H Y_0(w) L_env dw
            //     = 1/2 1/sqrt(π) L_env ∫_H dw
            //     = 1/2 1/sqrt(π) L_env 2π
            //     = sqrt(π) L_env
            // where H is any hemisphere and L_env is uniform environment radiance.
            // This value is converted to irradiance using Ramamoorthi's technique
            // (https://cseweb.ucsd.edu/~ravir/papers/envmap/envmap.pdf),
            // E_0 = A_hat_0 L_0 = π sqrt(π) L_env = π^(3/2) L_env,
            // where E_0 denotes the L0 irradiance term.
            float k = Mathf.Pow(Mathf.PI, 1.5f);
            var expected = new SHRGBL1
            {
                L0  = new Vector3(radiance.r, radiance.g, radiance.b) * k,
                L10 = Vector3.zero,
                L11 = Vector3.zero,
                L12 = Vector3.zero,
            };
            SHRGBL1 actual = m_Harness.ReadPatchIrradiance(patchIndex: 0);
            TestHarness.AssertL0IrradianceApproximatelyEqual(expected, actual, epsilon: 1e-2f);
        }
    }
}
