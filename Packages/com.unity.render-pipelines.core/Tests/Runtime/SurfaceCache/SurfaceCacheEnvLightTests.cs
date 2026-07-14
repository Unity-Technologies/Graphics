using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.Rendering.UnifiedRayTracing;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.Tests
{
    [TestFixture("Compute")]
    [TestFixture("Hardware")]
    internal class SurfaceCacheUniformEnvLightTests
    {
        readonly RayTracingBackend m_Backend;
        TestHarness m_Harness;

        public SurfaceCacheUniformEnvLightTests(string backendAsString)
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

            // This is not elegant but we need it: Dispose only queues GPU memory for release, and on Metal it is
            // reclaimed later, at frame boundaries. Synchronous tests never yield, so per-test allocations stack
            // up, which on iOS exhausts the per-process memory cap.
            for (int i = 0; i < 8; ++i)
                yield return null;
        }

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
            TestHarness.AssertSphericalHarmonicsL0ApproximatelyEqual(
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
            TestHarness.AssertSphericalHarmonicsL0ApproximatelyEqual(expected, actual, epsilon: 1e-2f);
        }
    }
}
