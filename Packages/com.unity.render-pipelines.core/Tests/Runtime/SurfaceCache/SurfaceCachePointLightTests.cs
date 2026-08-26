using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine.Rendering.UnifiedRayTracing;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.Tests
{
    [TestFixture("Compute")]
    [TestFixture("Hardware")]
    internal class SurfaceCachePointLightTests
    {
        const float k_PlaneHalfSize = 5f;
        const float k_AbsoluteTolerance = 1e-3f;

        const uint k_SampleCount = 256;
        const int k_FrameCount = 128;
        const float k_RelativeTolerance = 0.08f;

        const float k_Height = 0.25f;
        const float k_LightRange = 20f;

        static readonly Vector3 k_BaseAlbedo = new Vector3(0.9f, 0.6f, 0.3f);
        static readonly Vector3 k_LightColor = new Vector3(1.2f, 0.7f, 0.4f);

        readonly RayTracingBackend m_Backend;
        TestHarness m_Harness;
        TestScene m_Scene;

        public SurfaceCachePointLightTests(string backendAsString)
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

        void SeedObserver()
        {
            m_Harness.SetPatches(
                worldPositions: new[] { new Vector3(0f, k_Height, 0f) },
                worldNormals:   new[] { Vector3.down },
                cellIndices:    new uint[] { 0 },
                irradiances:    new SHRGBL1[1]);
        }

        void AddColocatedPointLights(int lightCount)
        {
            for (int i = 0; i < lightCount; ++i)
                m_Scene.AddPointLight(new Vector3(0f, k_Height, 0f), k_LightColor, k_LightRange);
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

        // We want to derive the irradiance SH coefficients for a patch due to lighting from a point light
        // that bounces off of a plane.
        // Suppose the patch is located at position x_p with a normal pointing down. The plane is located
        // below the patch, it is infinitely large with normal pointing up, and it is lambertian with
        // albedo c. To make the derivation simpler we will assume that the point light is positioned at
        // x_l = x_p, h_l units above the plane. The point light has intensity I.
        // We can derive the outgoing radiance from a point on the plane by applying the rendering equation
        // in area form such that we can exploit that the point light has a delta distribution:
        // L_plane(x) = ∫_H(x) c/π L(x <- ω) n(x)•ω dω
        //            = c/π ∫_M L(x <- x') n(x)•d(x->x') n(x')•d(x'->x) / ||x-x'||^2 dA(x')
        //            = c/π ∫_M I δ_{x_l}(x') n(x)•d(x->x') n(x')•d(x'->x) / ||x-x'||^2 dA(x')
        //            = c/π I n(x)•d(x->x_l) / ||x-x_l||^2
        // where H is the hemisphere, M is the scene surface (which here includes the infinitely small
        // point light), n is a function that returns the normal for a surface point, and d is a function
        // that returns direction between two positions.
        // Next, we calculate the radiance SH L0 term for the patch:
        // L_0 = ∫_H(x_p) Y_0 L(x <- ω) dω = Y_0 ∫_H(x_p) L_plane(t(x_p, ω)) dω
        //     = Y_0 c/π I ∫ n(t(x_p, ω)) • d(t(x_p, ω) -> x_l) / || t(x_p, ω) - x_l ||^2 dω
        // where t is an intersection trace function.
        // By analyzing the triangle formed by the patch position, the ray hit position, and the position
        // on the plane straight down from the patch, and by exploiting that x_p=x_l, basic trigonometry
        // shows us that
        // n(t(x_p, ω)) • d(t(x_p, ω) -> x_l) / || t(x_p, ω) - x_l ||^2 = (n(x_p)•ω)^3/h_l^2,
        // so
        // L_0 = Y_0 c/π I ∫ (n(x_p)•ω)^3/h_l^2 dω = Y_0 c/π I / h_l^2 ∫ (n(x_p)•ω)^3 dω
        // This integral is quite standard and is equal to π/2, so
        // L_0 = Y_0 c/π I / h_l^2 π/2 = c I / (4 sqrt(π) h_l^2).
        // Finally, we convert to irradiance[1]:
        // E_0 = L_0 A_hat0 = c I / (4 sqrt(π) h_l^2) π = sqrt(π) c I / (4 h_l^2).
        // [1] An Efficient Representation for Irradiance Environment Maps, Ramamoorthi
        // https://cseweb.ucsd.edu/~ravir/papers/envmap/envmap.pdf
        static void AssertBounceIrradiance(int lightCount, Vector3 albedo, SHRGBL1 actual)
        {
            Vector3 expectedL0 = lightCount * Mathf.Sqrt(Mathf.PI) / (4f * k_Height * k_Height) * Vector3.Scale(albedo, k_LightColor);
            Vector3 expectedL1y = -8f * Mathf.Sqrt(3f) / 15f * expectedL0;
            var expected = new SHRGBL1
            {
                L0  = expectedL0,
                L10 = expectedL1y,
                L11 = Vector3.zero,
                L12 = Vector3.zero,
            };

            Vector3 epsilon = k_RelativeTolerance * expectedL0 + k_AbsoluteTolerance * Vector3.one;
            TestHarness.AssertSphericalHarmonicsApproximatelyEqual(expected, actual, epsilon);
        }

        [Test]
        public void Point_WhenLightAbovePlane_ThenBounceScalesWithAlbedo([Values(0.0f, 0.42f, 1.0f)] float albedoScale)
        {
            Vector3 albedo = albedoScale * k_BaseAlbedo;
            AddColocatedPointLights(lightCount: 1);
            m_Scene.AddPlane(albedo, k_PlaneHalfSize, Vector3.zero, Vector3.up);
            SeedObserver();
            Estimate();

            SHRGBL1 actual = m_Harness.ReadPatchIrradiance(patchIndex: 0);
            AssertBounceIrradiance(lightCount: 1, albedo, actual);
        }

        // Plane with normal pointing down. Point light above plane. Patch above plane with normal pointing down.
        // Point light bounces off plane, into patch, only when double sided GI is enabled.
        [Test]
        public void Point_OnlyWhenPlaneHasDoubleSidedGI_BouncesOffPlane([Values(true, false)] bool doubleSided)
        {
            m_Scene.AddPointLight(new Vector3(0f, k_Height, 0f), k_LightColor, k_LightRange);
            m_Scene.AddPlane(k_BaseAlbedo, k_PlaneHalfSize, Vector3.zero, Vector3.down, doubleSidedGI: doubleSided);
            SeedObserver();
            Estimate();

            SHRGBL1 actual = m_Harness.ReadPatchIrradiance(patchIndex: 0);
            AssertBounceIrradiance(lightCount: doubleSided ? 1 : 0, k_BaseAlbedo, actual);
        }

        // Plane with normal pointing up. Point light below plane. Patch above plane with normal pointing down.
        // The light hits the underside of the plane, so no light arrives at the patch above, with or without double sided GI.
        [Test]
        public void Point_IsOccludedByPlane_RegardlessOfDoubleSidedGIMode([Values(true, false)] bool doubleSided)
        {
            m_Scene.AddPointLight(new Vector3(0f, -k_Height, 0f), k_LightColor, k_LightRange);
            m_Scene.AddPlane(k_BaseAlbedo, k_PlaneHalfSize, Vector3.zero, Vector3.up, doubleSidedGI: doubleSided);
            SeedObserver();
            Estimate();

            SHRGBL1 actual = m_Harness.ReadPatchIrradiance(patchIndex: 0);
            AssertBounceIrradiance(lightCount: 0, k_BaseAlbedo, actual);
        }

        [Test]
        public void Point_WhenNIdenticalColocatedLights_ThenBounceAdditive([Values(0, 1, 5)] int lightCount)
        {
            Vector3 albedo = new Vector3(0.42f, 0.9f, 0.6f);
            AddColocatedPointLights(lightCount);
            m_Scene.AddPlane(albedo, k_PlaneHalfSize, Vector3.zero, Vector3.up);
            SeedObserver();
            Estimate();

            SHRGBL1 actual = m_Harness.ReadPatchIrradiance(patchIndex: 0);
            AssertBounceIrradiance(lightCount, albedo, actual);
        }
    }
}
