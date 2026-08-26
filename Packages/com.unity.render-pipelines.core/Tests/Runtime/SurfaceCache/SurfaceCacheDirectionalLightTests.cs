using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.Rendering.UnifiedRayTracing;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.Tests
{
    [TestFixture("Compute")]
    [TestFixture("Hardware")]
    internal class SurfaceCacheDirectionalLightTests
    {
        // Large enough that the observer's hemisphere is filled by the plane (coverage ~ 1).
        const float k_PlaneHalfSize = 5f;
        const float k_ObserverHeight = 0.0625f;
        const uint k_SampleCount = 64;
        const int k_FrameCount = 16;
        const float k_RelativeTolerance = 0.02f;
        const float k_AbsoluteTolerance = 1e-3f;

        readonly RayTracingBackend m_Backend;
        TestHarness m_Harness;
        TestScene m_Scene;

        public SurfaceCacheDirectionalLightTests(string backendAsString)
            => m_Backend = Enum.Parse<RayTracingBackend>(backendAsString);

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

        // Observer patch just above the origin, looking straight down at the plane.
        void SeedObserver()
        {
            m_Harness.SetPatches(
                worldPositions: new[] { new Vector3(0f, k_ObserverHeight, 0f) },
                worldNormals:   new[] { Vector3.down },
                cellIndices:    new uint[] { 0 },
                irradiances:    new SHRGBL1[1]);
        }

        void Estimate()
        {
            // Warm-up commit builds the material atlas, acceleration structure, and environment before estimating.
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

        static void AssertObserverIrradiance(Vector3 expectedL0, SHRGBL1 actual)
        {
            // Assuming a patch that is pointing down onto an infinitely large plane with outgoing radiance L, we get
            // E_0 = A_hat0 ∫_H L Y_0 dω = π L √π = L π^(3/2)
            // where E_0 is irradiance L0 coefficient and A_hat0 is the radiance to irradiance conversion factor [1].
            // Similarly for the irradiance y L1 coefficient:
            // E_{1,y} = A_hat1 ∫_H L Y_1 dω = A_hat1 L √(3/π)/2 ∫_H y dω = 2π / 3 L √(3/π)/2 (-π) = -π^(3/2)L/√3.
            // We can substitute in E_0 to see
            // E_{0,1} = -1/√3 L.
            // [1] An Efficient Representation for Irradiance Environment Maps (https://cseweb.ucsd.edu/~ravir/papers/envmap/envmap.pdf)
            Vector3 expectedL1y = expectedL0 * (-1f / Mathf.Sqrt(3f));
            var expected = new SHRGBL1
            {
                L0  = expectedL0,
                L10 = expectedL1y,
                L11 = Vector3.zero,
                L12 = Vector3.zero,
            };
            // Per-channel tolerance scaled by that channel's L0, so the near-zero L1 x/z are judged against the channel's signal.
            Vector3 epsilon = k_RelativeTolerance * expectedL0 + k_AbsoluteTolerance * Vector3.one;
            TestHarness.AssertSphericalHarmonicsApproximatelyEqual(expected, actual, epsilon);
        }

        [Test]
        public void Directional_WhenNormalIncidence_ThenIrradianceScalesWithAlbedoAndIntensity(
            [Values(0.0f, 0.42f, 1.0f)] float albedoScale,
            [Values(0.0f, 0.5f, 1.0f, 42.0f)] float intensity)
        {
            Vector3 albedo = albedoScale * new Vector3(1.0f, 0.7f, 0.4f);
            Vector3 lightColor = intensity * new Vector3(1.0f, 0.5f, 0.25f);
            m_Scene.AddDirectionalLight(Vector3.down, lightColor);
            m_Scene.AddPlane(albedo, k_PlaneHalfSize, Vector3.zero, Vector3.up);
            SeedObserver();
            Estimate();

            // Normal incidence (n•ω_light = 1); special case of Directional_WhenObliqueIncidence_ThenCosineWeighted, see its derivation.
            Vector3 expected = Mathf.Sqrt(Mathf.PI) * Vector3.Scale(albedo, lightColor);
            SHRGBL1 actual = m_Harness.ReadPatchIrradiance(patchIndex: 0);
            AssertObserverIrradiance(expected, actual);
        }

        [Test]
        public void Directional_WhenObliqueIncidence_ThenCosineWeighted()
        {
            // Channel proportions differ from the other tests, so a shared albedo-to-light ratio cannot hide a per-channel bug.
            Vector3 albedo = new Vector3(0.4f, 0.9f, 0.6f);
            Vector3 lightColor = new Vector3(0.5f, 1.0f, 2.0f);

            Vector3 lightDir = new Vector3(0.1f, -0.8f, 0.2f).normalized; // shining down, mostly
            float lightDirDotPlaneNormal = Vector3.Dot(-lightDir, Vector3.up);
            m_Scene.AddDirectionalLight(lightDir, lightColor);
            m_Scene.AddPlane(albedo, k_PlaneHalfSize, Vector3.zero, Vector3.up);
            SeedObserver();
            Estimate();

            // Assuming an infinitely large Lambertian plane with albedo ρ illuminated by a directional light modelled
            // as a delta distribution from direction ω_light with intensity I_light, we derive the bounced radiance from
            // the plane:
            // L_p = ∫_H L_sun(ω) ρ/π (n•ω) dω = ρ/π I_light (n•ω_light).
            // We derive the L0 radiance coefficient for a downwards-pointing patch positioned just above the plane:
            // L_0 = ∫ Y_0 L_p dω = ρ / √π I_light (n•ω_light).
            // Finally, we convert this to irradiance [1]:
            // E_0 = A_hat_0 L_0 = π ρ / √π I_light (n•ω_light) = ρ √π I_light (n•ω_light).
            // [1] An Efficient Representation for Irradiance Environment Maps (https://cseweb.ucsd.edu/~ravir/papers/envmap/envmap.pdf)
            Vector3 expected = Mathf.Sqrt(Mathf.PI) * lightDirDotPlaneNormal * Vector3.Scale(albedo, lightColor);
            SHRGBL1 actual = m_Harness.ReadPatchIrradiance(patchIndex: 0);
            AssertObserverIrradiance(expected, actual);
        }

        [Test]
        public void Directional_WhenGrazing_ThenZero()
        {
            // Light travels horizontally (to the left), parallel to the plane: cos theta = 0, the < 0 guard boundary.
            m_Scene.AddDirectionalLight(Vector3.left, new Vector3(0.3f, 1.0f, 0.6f));
            m_Scene.AddPlane(albedo: new Vector3(0.9f, 0.4f, 0.7f), k_PlaneHalfSize, Vector3.zero, Vector3.up);
            SeedObserver();
            Estimate();

            SHRGBL1 actual = m_Harness.ReadPatchIrradiance(patchIndex: 0);
            AssertObserverIrradiance(Vector3.zero, actual);
        }

        [Test]
        public void Directional_WhenBackFacing_ThenZero()
        {
            // Plane front face points down, so the observer above hits the culled back face.
            m_Scene.AddDirectionalLight(Vector3.down, new Vector3(0.8f, 0.4f, 1.0f));
            m_Scene.AddPlane(albedo: new Vector3(0.5f, 0.9f, 0.3f), k_PlaneHalfSize, Vector3.zero, Vector3.down);
            SeedObserver();
            Estimate();

            SHRGBL1 actual = m_Harness.ReadPatchIrradiance(patchIndex: 0);
            AssertObserverIrradiance(Vector3.zero, actual);
        }

        // Plane with normal pointing down. Directional light pointing down. Patch above plane with normal pointing down.
        // Directional light bounces off plane, into patch, only when double sided GI is enabled.
        [Test]
        public void Directional_OnlyWhenPlaneHasDoubleSidedGI_BouncesOffPlane([Values(true, false)] bool doubleSided)
        {
            Vector3 albedo = new Vector3(0.5f, 0.9f, 0.3f);
            Vector3 lightColor = new Vector3(0.8f, 0.4f, 1.0f);
            m_Scene.AddDirectionalLight(Vector3.down, lightColor);
            m_Scene.AddPlane(albedo, k_PlaneHalfSize, Vector3.zero, Vector3.down, doubleSidedGI: doubleSided);
            SeedObserver();
            Estimate();

            // Derivation from Directional_WhenObliqueIncidence_ThenCosineWeighted
            Vector3 expected = doubleSided
                ? Mathf.Sqrt(Mathf.PI) * Vector3.Scale(albedo, lightColor)
                : Vector3.zero;
            SHRGBL1 actual = m_Harness.ReadPatchIrradiance(patchIndex: 0);
            AssertObserverIrradiance(expected, actual);
        }

        // Plane with normal pointing down. Directional light pointing up. Patch above plane with normal pointing down.
        // The light hits the underside of the plane, so no light arrives at the patch above, with or without double sided GI.
        [Test]
        public void Directional_IsOccludedByPlane_RegardlessOfDoubleSidedGIMode([Values(true, false)] bool doubleSided)
        {
            m_Scene.AddDirectionalLight(Vector3.up, new Vector3(0.8f, 0.4f, 1.0f));
            m_Scene.AddPlane(albedo: new Vector3(0.5f, 0.9f, 0.3f), k_PlaneHalfSize, Vector3.zero, Vector3.down, doubleSidedGI: doubleSided);
            SeedObserver();
            Estimate();

            SHRGBL1 actual = m_Harness.ReadPatchIrradiance(patchIndex: 0);
            AssertObserverIrradiance(Vector3.zero, actual);
        }

        [Test]
        public void Directional_WhenLightBelowPlane_ThenZero()
        {
            // Light travels up: the plane's lit front face points away from it, so nothing bounces.
            m_Scene.AddDirectionalLight(Vector3.up, new Vector3(1.0f, 0.6f, 0.3f));
            m_Scene.AddPlane(albedo: new Vector3(0.4f, 0.7f, 1.0f), k_PlaneHalfSize, Vector3.zero, Vector3.up);
            SeedObserver();
            Estimate();

            SHRGBL1 actual = m_Harness.ReadPatchIrradiance(patchIndex: 0);
            AssertObserverIrradiance(Vector3.zero, actual);
        }

        [Test]
        public void Directional_WhenOccluded_ThenZero()
        {
            const float occluderHeight = 1f;
            m_Scene.AddDirectionalLight(Vector3.down, new Vector3(0.6f, 1.0f, 0.4f));
            m_Scene.AddPlane(albedo: new Vector3(1.0f, 0.5f, 0.8f), k_PlaneHalfSize, Vector3.zero, Vector3.up);
            // Opaque plane above, between light and ground; only its silhouette matters, so any albedo works.
            m_Scene.AddPlane(albedo: Vector3.one, k_PlaneHalfSize, new Vector3(0f, occluderHeight, 0f), Vector3.up);
            SeedObserver();
            Estimate();

            SHRGBL1 actual = m_Harness.ReadPatchIrradiance(patchIndex: 0);
            AssertObserverIrradiance(Vector3.zero, actual);
        }
    }
}
