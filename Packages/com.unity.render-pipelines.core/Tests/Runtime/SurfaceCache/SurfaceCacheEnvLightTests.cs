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

    [TestFixture("Compute")]
    [TestFixture("Hardware")]
    internal class SurfaceCacheEnvironmentBounceTests
    {
        readonly RayTracingBackend m_Backend;
        TestHarness m_Harness;
        TestScene m_Scene;

        public SurfaceCacheEnvironmentBounceTests(string backendAsString)
            => m_Backend = Enum.Parse<RayTracingBackend>(backendAsString);

        [SetUp]
        public void SetUp()
        {
            TestHarness.EnsureSupportedOrIgnore(m_Backend);

            m_Harness = new TestHarness(
                m_Backend,
                sampleCount: 128,
                multiBounce: true,
                bouncePatchAllocation: true,
                volumeSize: 10f,
                volumeResolution: 32);
            m_Scene = new TestScene(m_Harness.World);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            m_Harness?.Dispose();
            m_Scene?.Dispose();

            for (int i = 0; i < 8; ++i)
                yield return null;
        }

        [Test]
        public void Env_WhenBouncedOffPlane_ThenIrradianceIsAlbedoTimesRadianceTimesPiToThe3Over2()
        {
            const int frameCount = 64;
            const float relativeTolerance = 0.03f;
            const float absoluteTolerance = 1e-3f;

            Vector3 albedo = new Vector3(0.8f, 0.3f, 0.55f);
            var envRadiance = new Vector3(1.5f, 0.4f, 0.9f);
            float planeHalfSize = 5f;
            float planeHeight = 0.0625f;

            m_Harness.World.SetEnvironmentColor(new Color(envRadiance.x, envRadiance.y, envRadiance.z));
            m_Scene.AddPlane(albedo, planeHalfSize, center: new Vector3(0f, planeHeight, 0f), normal: Vector3.down);
            m_Harness.SetPatches(
                worldPositions: new[] { Vector3.zero },
                worldNormals:   new[] { Vector3.up },
                cellIndices:    new uint[] { 0 },
                irradiances:    new SHRGBL1[1]);

            m_Harness.BeginFrame();
            m_Harness.CommitWorld();
            m_Harness.Estimate();
            m_Harness.EndFrame();

            for (int i = 1; i < frameCount; ++i)
            {
                m_Harness.BeginFrame();
                m_Harness.Estimate();
                m_Harness.EndFrame();
            }

            // Assumptions:
            // - The patch is at position x_p below the plane, with normal pointing up.
            // - The plane is infinite size, with normal pointing down, and albedo 'c'.
            // - The environment map has constant outgoing radiance 'I'.
            //
            // The outgoing radiance at any point on the plane is:
            //
            //   L_plane(x) = ∫_H(x) c/π L(x <- ω) n(x)•ω dω
            //              = c/π I ∫_H(x) n(x)•ω dω
            //              = c/π I π
            //              = c I
            //
            // So the L0 coefficient for radiance is:
            //
            //   L_0 = ∫_H(x_p) Y_0 L(x <- ω) dω
            //       = c I Y_0 ∫_H(x_p) 1 dω
            //       = c I Y_0 2π
            //       = c I sqrt(π)
            //
            // Similarly for L_10 (other L1 coefficients are 0 by symmetry):
            //
            //   L_10 = ∫_H(x_p) Y_10 L(x <- ω) dω
            //        = c I ∫_H(x_p) Y_10 dω
            //        = c I sqrt(3π)/2
            //
            // Convert to irradiance using Ramamoorthi's technique
            // (https://cseweb.ucsd.edu/~ravir/papers/envmap/envmap.pdf):
            //
            //   E_0 = c I sqrt(π) π = c I π^(3/2)
            //   E_10 = c I (sqrt(3π)/2) (2π/3) = c I π^(3/2)/sqrt(3)
            //
            Vector3 expectedL0 = Vector3.Scale(albedo, envRadiance) * Mathf.Pow(Mathf.PI, 1.5f);
            var expected = new SHRGBL1
            {
                L0  = expectedL0,
                L10 = expectedL0 / Mathf.Sqrt(3.0f),
                L11 = Vector3.zero,
                L12 = Vector3.zero,
            };
            SHRGBL1 actual = m_Harness.ReadPatchIrradiance(patchIndex: 0);
            Vector3 tolerance = relativeTolerance * expectedL0 + absoluteTolerance * Vector3.one;
            TestHarness.AssertSphericalHarmonicsApproximatelyEqual(expected, actual, tolerance);
        }

        [Test]
        public void Env_WhenPatchFullyEnclosed_ThenNoContribution()
        {
            const int frameCount = 16;
            const float absoluteTolerance = 1e-3f;

            Vector3 albedo = new Vector3(0.8f, 0.3f, 0.55f);
            var envRadiance = new Vector3(1.5f, 0.4f, 0.9f);
            float boxHalfExtent = 1f;
            float faceHalfSize = 2f * boxHalfExtent;

            m_Harness.World.SetEnvironmentColor(new Color(envRadiance.x, envRadiance.y, envRadiance.z));

            m_Scene.AddPlane(albedo, faceHalfSize, new Vector3(0f,  boxHalfExtent, 0f), Vector3.down);
            m_Scene.AddPlane(albedo, faceHalfSize, new Vector3(0f, -boxHalfExtent, 0f), Vector3.up);
            m_Scene.AddPlane(albedo, faceHalfSize, new Vector3( boxHalfExtent, 0f, 0f), Vector3.left);
            m_Scene.AddPlane(albedo, faceHalfSize, new Vector3(-boxHalfExtent, 0f, 0f), Vector3.right);
            m_Scene.AddPlane(albedo, faceHalfSize, new Vector3(0f, 0f,  boxHalfExtent), Vector3.back);
            m_Scene.AddPlane(albedo, faceHalfSize, new Vector3(0f, 0f, -boxHalfExtent), Vector3.forward);
            m_Harness.SetPatches(
                worldPositions: new[] { Vector3.zero },
                worldNormals:   new[] { Vector3.up },
                cellIndices:    new uint[] { 0 },
                irradiances:    new SHRGBL1[1]);

            m_Harness.BeginFrame();
            m_Harness.CommitWorld();
            m_Harness.Estimate();
            m_Harness.EndFrame();

            for (int i = 1; i < frameCount; ++i)
            {
                m_Harness.BeginFrame();
                m_Harness.Estimate();
                m_Harness.EndFrame();
            }

            SHRGBL1 actual = m_Harness.ReadPatchIrradiance(patchIndex: 0);
            TestHarness.AssertSphericalHarmonicsApproximatelyEqual(default, actual, absoluteTolerance * Vector3.one);
        }
    }
}
