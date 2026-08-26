using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.Rendering.UnifiedRayTracing;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.Tests
{
    [TestFixture("Compute")]
    [TestFixture("Hardware")]
    internal class SurfaceCacheEmissiveTests
    {
        const uint k_SampleCount = 128;
        const int k_FrameCount = 16;

        static readonly SHRGBL1 k_ZeroSH = default;

        readonly RayTracingBackend m_Backend;
        TestHarness m_Harness;
        TestScene m_Scene;

        public SurfaceCacheEmissiveTests(string backendAsString)
        {
            m_Backend = Enum.Parse<RayTracingBackend>(backendAsString);
        }

        [SetUp]
        public void SetUp()
        {
            TestHarness.EnsureSupportedOrIgnore(m_Backend);

            m_Harness = new TestHarness(m_Backend, sampleCount: k_SampleCount);
            m_Scene = new TestScene(m_Harness.World);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            m_Harness?.Dispose();
            m_Scene?.Dispose();
            return TestHarness.DrainDisposedGpuMemory();
        }

        void CreateObserverPatch()
        {
            m_Harness.SetPatches(
                worldPositions: new[] { Vector3.zero },
                worldNormals:   new[] { Vector3.up },
                cellIndices:    new uint[] { 0 },
                irradiances:    new SHRGBL1[1]);
        }

        void Estimate()
        {
            m_Harness.BeginFrame();
            m_Harness.CommitWorld();
            m_Harness.EndFrame();

            for (int i = 0; i < k_FrameCount; ++i)
            {
                m_Harness.BeginFrame();
                m_Harness.Estimate();
                m_Harness.EndFrame();
            }
        }

        [Test]
        public void Emissive_OnlyWhenPatchSeesBackface_ThenZeroIrradiance([Values(true, false)] bool backfacing)
        {
            m_Scene.AddEmissiveQuad(
                emission: new Vector3(1.0f, 0.6f, 0.3f),
                halfSize: 1.0f,
                center: new Vector3(0.0f, 1.0f, 0.0f),
                emissiveFacesUp: backfacing);
            CreateObserverPatch();
            Estimate();

            SHRGBL1 actual = m_Harness.ReadPatchIrradiance(patchIndex: 0);
            if (backfacing)
                Assert.AreEqual(k_ZeroSH, actual);
            else
                Assert.AreNotEqual(k_ZeroSH, actual);
        }

        // Emissive quad above the patch with normal pointing up. Patch below quad with normal pointing up.
        // The quad's backface emits light only when double sided GI is enabled.
        [Test]
        public void Emissive_OnlyWhenEmitterHasDoubleSidedGI_EmitsLightFromBackface([Values(true, false)] bool doubleSided)
        {
            m_Scene.AddEmissiveQuad(
                emission: new Vector3(1.0f, 0.6f, 0.3f),
                halfSize: 1.0f,
                center: new Vector3(0.0f, 1.0f, 0.0f),
                emissiveFacesUp: true,
                doubleSidedGI: doubleSided);
            CreateObserverPatch();
            Estimate();

            SHRGBL1 actual = m_Harness.ReadPatchIrradiance(patchIndex: 0);
            if (doubleSided)
                Assert.AreNotEqual(k_ZeroSH, actual);
            else
                Assert.AreEqual(k_ZeroSH, actual);
        }

        // Emissive quad facing down. Infinite occluding plane with normal pointing up, between quad and patch.
        // Patch below plane with normal pointing up, with pre-seeded irradiance. Rays from the patch hit backfaces.
        // With double sided GI, that results in black. Without, the samples never contribute, so the patch
        // keeps its existing value (see IsValidSample() in PathTracing.hlsl).
        [Test]
        public void Emissive_OnlyWhenOccluderHasDoubleSidedGI_SamplesOccludedByBackfaceContribute([Values(true, false)] bool doubleSided)
        {
            var seeded = new SHRGBL1 { L0 = new Vector3(0.5f, 0.5f, 0.5f) };

            m_Scene.AddEmissiveQuad(
                emission: new Vector3(1.0f, 0.6f, 0.3f),
                halfSize: 1.0f,
                center: new Vector3(0.0f, 2.0f, 0.0f),
                emissiveFacesUp: false);
            m_Scene.AddPlane(
                albedo: Vector3.one,
                halfSize: 100.0f,
                center: new Vector3(0.0f, 1.0f, 0.0f),
                normal: Vector3.up,
                doubleSidedGI: doubleSided);
            m_Harness.SetPatches(
                worldPositions: new[] { Vector3.zero },
                worldNormals:   new[] { Vector3.up },
                cellIndices:    new uint[] { 0 },
                irradiances:    new[] { seeded });
            Estimate();

            SHRGBL1 actual = m_Harness.ReadPatchIrradiance(patchIndex: 0);
            if (doubleSided)
                TestHarness.AssertSphericalHarmonicsApproximatelyEqual(k_ZeroSH, actual, epsilon: Vector3.one * 1e-3f);
            else
                Assert.AreEqual(seeded, actual, "An invalid sample should leave the patch untouched.");
        }

        [Test]
        public void Emissive_OnlyWhenEmitterBehindPatch_ThenZeroIrradiance([Values(true, false)] bool behindPatch)
        {
            m_Scene.AddEmissiveQuad(
                emission: new Vector3(1.0f, 0.6f, 0.3f),
                halfSize: 1.0f,
                center: new Vector3(0.0f, behindPatch ? -1.0f : 1.0f, 0.0f),
                emissiveFacesUp: false);
            CreateObserverPatch();
            Estimate();

            SHRGBL1 actual = m_Harness.ReadPatchIrradiance(patchIndex: 0);
            if (behindPatch)
                Assert.AreEqual(k_ZeroSH, actual);
            else
                Assert.AreNotEqual(k_ZeroSH, actual);
        }

        [Test]
        public void Emissive_OnlyWhenEmitterOccluded_ThenZeroIrradiance([Values(true, false)] bool occluded)
        {
            m_Scene.AddEmissiveQuad(
                emission: new Vector3(1.0f, 0.6f, 0.3f),
                halfSize: 1.0f,
                center: new Vector3(0.0f, 1.0f, 0.0f),
                emissiveFacesUp: false);
            if (occluded)
            {
                m_Scene.AddPlane(
                    albedo: Vector3.one,
                    halfSize: 2.0f,
                    center: new Vector3(0.0f, 0.5f, 0.0f),
                    normal: Vector3.down);
            }
            CreateObserverPatch();
            Estimate();

            SHRGBL1 actual = m_Harness.ReadPatchIrradiance(patchIndex: 0);
            if (occluded)
                Assert.AreEqual(k_ZeroSH, actual);
            else
                Assert.AreNotEqual(k_ZeroSH, actual);
        }

        [Test]
        public void Emissive_WhenFacingEmitter_IrradianceIsPositiveAndChannelProportional()
        {
            Vector3 emission = new Vector3(1.0f, 0.6f, 0.3f);
            m_Scene.AddEmissiveQuad(
                emission,
                halfSize: 1.0f,
                center: new Vector3(0.0f, 1.0f, 0.0f),
                emissiveFacesUp: false);
            CreateObserverPatch();
            Estimate();

            SHRGBL1 actual = m_Harness.ReadPatchIrradiance(patchIndex: 0);

            // Non-zero
            Assert.AreNotEqual(k_ZeroSH, actual);

            // Proportional
            float greenMultiplier = 1.0f / emission[1];
            float blueMultiplier = 1.0f / emission[2];
            Assert.AreEqual(actual.L0[0],  actual.L0[1]  * greenMultiplier, 1e-2f);
            Assert.AreEqual(actual.L0[0],  actual.L0[2]  * blueMultiplier,  1e-2f);
            Assert.AreEqual(actual.L10[0], actual.L10[1] * greenMultiplier, 1e-2f);
            Assert.AreEqual(actual.L10[0], actual.L10[2] * blueMultiplier,  1e-2f);
            Assert.AreEqual(actual.L11[0], actual.L11[1] * greenMultiplier, 1e-2f);
            Assert.AreEqual(actual.L11[0], actual.L11[2] * blueMultiplier,  1e-2f);
            Assert.AreEqual(actual.L12[0], actual.L12[1] * greenMultiplier, 1e-2f);
            Assert.AreEqual(actual.L12[0], actual.L12[2] * blueMultiplier,  1e-2f);
        }

        [Test]
        public void Emissive_WhenMultipleEmitters_AllEmittersContributeToIrradiance()
        {
            m_Scene.AddEmissiveQuad(
                emission: new Vector3(1.0f, 0.0f, 0.0f), // red
                halfSize: 0.5f,
                center: new Vector3(-1.0f, 1.0f, 0.0f),
                emissiveFacesUp: false);
            m_Scene.AddEmissiveQuad(
                emission: new Vector3(0.0f, 1.0f, 0.0f), // green
                halfSize: 0.5f,
                center: new Vector3(0.0f, 1.0f, 0.0f),
                emissiveFacesUp: false);
            m_Scene.AddEmissiveQuad(
                emission: new Vector3(0.0f, 0.0f, 1.0f), // blue
                halfSize: 0.5f,
                center: new Vector3(1.0f, 1.0f, 0.0f),
                emissiveFacesUp: false);
            CreateObserverPatch();
            Estimate();

            SHRGBL1 actual = m_Harness.ReadPatchIrradiance(patchIndex: 0);
            Assert.Greater(actual.L0[0], 0.25f); // significantly above 0
            Assert.Greater(actual.L0[1], 0.25f);
            Assert.Greater(actual.L0[2], 0.25f);
        }

        [Test]
        public void Emissive_WhenEmitterIsTiny_ContributesToIrradiance()
        {
            m_Scene.AddEmissiveQuad(
                emission: Vector3.one * 10000f,
                halfSize: 0.01f,
                center: new Vector3(-1.0f, 1.0f, 0.0f),
                emissiveFacesUp: false);
            CreateObserverPatch();
            Estimate();

            SHRGBL1 actual = m_Harness.ReadPatchIrradiance(patchIndex: 0);
            Assert.Greater(actual.L0[0], 0.25f); // significantly above 0
            Assert.Greater(actual.L0[1], 0.25f);
            Assert.Greater(actual.L0[2], 0.25f);
        }

        // Double sided emissive quad above the patch, large enough to fill its hemisphere, facing either up or down.
        // Patch below quad with normal pointing up.
        // Both faces of the quad emit, so flipping which way it faces must not change the irradiance. The two
        // emitter sampling strategies must agree on the quad's cosine, or their MIS weights stop summing to one.
        [Test]
        public void Emissive_WhenDoubleSidedFillsHemisphere_IrradianceIsFaceOrientationIndependent([Values(true, false)] bool emissiveFacesUp)
        {
            Vector3 emission = new Vector3(1.0f, 0.6f, 0.3f);
            m_Scene.AddEmissiveQuad(
                emission: emission,
                halfSize: 100000.0f,
                center: new Vector3(0.0f, 1.0f, 0.0f),
                emissiveFacesUp: emissiveFacesUp,
                doubleSidedGI: true);
            CreateObserverPatch();
            Estimate();

            // Same analytical result as Emissive_WhenFillsHemisphere_IrradianceMatchesAnalyticalResult; see its
            // derivation. A double-sided emitter must produce it from either orientation.
            SHRGBL1 expected = new SHRGBL1
            {
                L0 = Mathf.PI * Mathf.Sqrt(Mathf.PI) * emission,
                L10 = Mathf.Pow(Mathf.PI, 3.0f / 2.0f) / Mathf.Sqrt(3.0f) * emission,
                L11 = Vector3.zero,
                L12 = Vector3.zero,
            };
            SHRGBL1 actual = m_Harness.ReadPatchIrradiance(patchIndex: 0);
            TestHarness.AssertSphericalHarmonicsApproximatelyEqual(expected, actual, epsilon: Vector3.one * 1e-2f);
        }

        [Test]
        public void Emissive_WhenFillsHemisphere_IrradianceMatchesAnalyticalResult()
        {
            Vector3 emission = new Vector3(1.0f, 0.6f, 0.3f);
            m_Scene.AddEmissiveQuad(
                emission: emission,
                halfSize: 100000.0f,
                center: new Vector3(0.0f, 1.0f, 0.0f),
                emissiveFacesUp: false);
            CreateObserverPatch();
            Estimate();

            // Radiance L0 and L1 coefficient:
            //
            //   L_0 = ∫_Ω Y_0(ω) * L_e dω
            //       = ∫_Ω 1/2 * sqrt(1/pi) * L_e dω
            //       = sqrt(pi) * L_e
            //
            //   L_10 = ∫_Ω Y_10(ω) * L_e dω
            //        = ∫_Ω 1/2 * sqrt(3/pi) * y * L_e dω
            //        = 1/2 * sqrt(3/pi) * L_e * ∫_Ω y dω    <-- pull out constants
            //        = 1/2 * sqrt(3/pi) * pi * L_e          <-- solve integral using y=cos(θ)
            //        = sqrt(3*pi)/2 * L_e
            //
            //   Other L_1 coefficients are 0 by symmetry.
            //
            // Convert to irradiance using Ramamoorthi's technique
            // (https://cseweb.ucsd.edu/~ravir/papers/envmap/envmap.pdf):
            //
            //   E_0 = pi * sqrt(pi) * L_e
            //   E_10 = 2pi/3 * sqrt(3*pi)/2 * L_e = pi^(3/2) / sqrt(3) * L_e

            SHRGBL1 actual = m_Harness.ReadPatchIrradiance(patchIndex: 0);
            SHRGBL1 expected = new SHRGBL1
            {
                L0 = Mathf.PI * Mathf.Sqrt(Mathf.PI) * emission,
                L10 = Mathf.Pow(Mathf.PI, 3.0f/2.0f) / Mathf.Sqrt(3.0f) * emission,
                L11 = Vector3.zero, // Nothing in Z direction
                L12 = Vector3.zero, // Nothing in X direction
            };
            TestHarness.AssertSphericalHarmonicsApproximatelyEqual(expected, actual, epsilon: Vector3.one * 1e-2f);
        }
    }
}
