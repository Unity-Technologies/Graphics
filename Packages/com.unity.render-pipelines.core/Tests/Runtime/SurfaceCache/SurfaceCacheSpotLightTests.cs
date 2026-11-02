using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.Rendering.UnifiedRayTracing;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.Tests
{
    [TestFixture("Compute")]
    [TestFixture("Hardware")]
    internal class SurfaceCacheSpotLightTests
    {
        const uint k_SampleCount = 256;
        const int k_FrameCount = 128;

        readonly RayTracingBackend m_Backend;

        public SurfaceCacheSpotLightTests(string backendAsString)
        {
            m_Backend = Enum.Parse<RayTracingBackend>(backendAsString);
        }

        [SetUp]
        public void SetUp()
        {
            TestHarness.EnsureSupportedOrIgnore(m_Backend);
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            return TestHarness.DrainDisposedGpuMemory();
        }

        SHRGBL1 EstimateTwoSpotLightBounce(uint punctualLightCandidateCount)
        {
            var harness = new TestHarness(m_Backend, sampleCount: k_SampleCount, punctualLightCandidateCount: punctualLightCandidateCount);
            var scene = new TestScene(harness.World);
            try
            {
                scene.AddSpotLight(
                    position: new Vector3(-0.5f, 0.75f, 0.0f),
                    direction: Vector3.down,
                    linearColor: new Vector3(1.2f, 0.7f, 0.4f),
                    range: 20.0f,
                    outerSpotAngle: 90.0f,
                    innerSpotAngle: 60.0f);
                scene.AddSpotLight(
                    position: new Vector3(0.5f, 0.75f, 0.0f),
                    direction: Vector3.down,
                    linearColor: new Vector3(0.3f, 0.6f, 1.5f),
                    range: 20.0f,
                    outerSpotAngle: 70.0f,
                    innerSpotAngle: 70.0f);
                scene.AddPlane(
                    albedo: new Vector3(0.9f, 0.6f, 0.3f),
                    halfSize: 5.0f,
                    center: Vector3.zero,
                    normal: Vector3.up);
                harness.SetPatches(
                    worldPositions: new[] { new Vector3(0.0f, 0.25f, 0.0f) },
                    worldNormals:   new[] { Vector3.down },
                    cellIndices:    new uint[] { 0 },
                    irradiances:    new SHRGBL1[1]);

                harness.BeginFrame();
                harness.CommitWorld();
                harness.EndFrame();

                for (int i = 0; i < k_FrameCount; ++i)
                {
                    harness.BeginFrame();
                    harness.Estimate();
                    harness.EndFrame();
                }

                return harness.ReadPatchIrradiance(patchIndex: 0);
            }
            finally
            {
                harness.Dispose();
                scene.Dispose();
            }
        }

        [Test]
        public void Spot_WhenTwoLightsBounceOffPlane_IrradianceIsIndependentOfRISCandidateCount([Values(2u, 8u, 32u)] uint punctualLightCandidateCount)
        {
            SHRGBL1 expected = EstimateTwoSpotLightBounce(punctualLightCandidateCount: 1);
            SHRGBL1 actual = EstimateTwoSpotLightBounce(punctualLightCandidateCount);

            Assert.AreNotEqual(default(SHRGBL1), expected);
            TestHarness.AssertSphericalHarmonicsApproximatelyEqual(expected, actual, epsilon: 0.05f * expected.L0 + Vector3.one * 1e-3f);
        }
    }
}
