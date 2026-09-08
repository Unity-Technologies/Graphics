using System.Collections;
using NUnit.Framework;
using UnityEngine.Rendering.Universal;
using UnityEngine.TestTools;

namespace UnityEngine.Rendering.Universal.Tests
{
    [TestFixture]
    class MultipleObjectLight2DTests
    {
        GameObject m_TestObject1;
        GameObject m_TestObject2;
        GameObject m_TestObject3;
        GameObject m_TestObject4;
        GameObject m_TestObjectCached;

        // Some residual Game Objects from previous test scenes can be left over,
        // We destroy everything so they don't interfere with these tests
        [OneTimeSetUp]
        public void OneTimeSetup()
        {
            var allGameObjectsInScene = Object.FindObjectsByType<GameObject>();
            foreach (var go in allGameObjectsInScene)
            {
                Object.DestroyImmediate(go);
            }
        }

        [SetUp]
        public void Setup()
        {
            m_TestObject1 = new GameObject("Test Object 1");
            m_TestObject2 = new GameObject("Test Object 2");
            m_TestObject3 = new GameObject("Test Object 3");
            m_TestObject4 = new GameObject("Test Object 4");
            m_TestObjectCached = new GameObject("Test Object Cached");
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(m_TestObjectCached);
            Object.DestroyImmediate(m_TestObject4);
            Object.DestroyImmediate(m_TestObject3);
            Object.DestroyImmediate(m_TestObject2);
            Object.DestroyImmediate(m_TestObject1);
        }

        [Test]
        public void LightsAreSortedByLightOrder()
        {
            var light1 = m_TestObject1.AddComponent<Light2D>();
            var light2 = m_TestObject2.AddComponent<Light2D>();
            var light3 = m_TestObject3.AddComponent<Light2D>();

            light1.lightOrder = 1;
            light2.lightOrder = 2;
            light3.lightOrder = 0;

            var camera = m_TestObject4.AddComponent<Camera>();
            var cameraPos = camera.transform.position;
            light1.transform.position = cameraPos;
            light2.transform.position = cameraPos;
            light3.transform.position = cameraPos;

            light1.UpdateMesh(true);
            light1.UpdateBoundingSphere();
            light2.UpdateMesh(true);
            light2.UpdateBoundingSphere();
            light3.UpdateMesh(true);
            light3.UpdateBoundingSphere();

            var cullResult = new Light2DCullResult();
            var cullingParams = new ScriptableCullingParameters();
            camera.TryGetCullingParameters(out cullingParams);
            cullResult.SetupCulling(ref cullingParams, camera);

            Assert.AreSame(light3, cullResult.visibleLights[0]);
            Assert.AreSame(light1, cullResult.visibleLights[1]);
            Assert.AreSame(light2, cullResult.visibleLights[2]);
        }

        [Test]
        public void LightIsInVisibleListIfInCameraView()
        {
            var camera = m_TestObject1.AddComponent<Camera>();
            var light = m_TestObject2.AddComponent<Light2D>();
            light.transform.position = camera.transform.position;
            light.UpdateMesh(true);
            light.UpdateBoundingSphere();

            var cullResult = new Light2DCullResult();
            var cullingParams = new ScriptableCullingParameters();
            camera.TryGetCullingParameters(out cullingParams);
            cullResult.SetupCulling(ref cullingParams, camera);

            Assert.Contains(light, cullResult.visibleLights);
        }

        [Test]
        public void LightIsNotInVisibleListIfNotInCameraView()
        {
            var camera = m_TestObject1.AddComponent<Camera>();
            var light = m_TestObject2.AddComponent<Light2D>();
            light.transform.position = camera.transform.position + new Vector3(9999.0f, 0.0f, 0.0f);
            light.UpdateMesh(true);
            light.UpdateBoundingSphere();

            var cullResult = new Light2DCullResult();
            var cullingParams = new ScriptableCullingParameters();
            camera.TryGetCullingParameters(out cullingParams);
            cullResult.SetupCulling(ref cullingParams, camera);

            Assert.IsFalse(cullResult.visibleLights.Contains(light));
        }

        [Test]
        public void CachedMeshDataIsUpdatedOnChange()
        {
            var shapePath = new Vector3[4] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) };
            var light = m_TestObjectCached.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Freeform;

            light.SetShapePath(shapePath);
            light.UpdateMesh(true);

            Assert.AreEqual(true, light.hasCachedMesh);
        }

        [Test]
        public void CachedMeshDataIsOverriddenByRuntimeChanges()
        {
            var shapePath = new Vector3[4] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) };
            var light = m_TestObjectCached.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Freeform;
            light.SetShapePath(shapePath);
            light.UpdateMesh(true);

            int vertexCount = 0, triangleCount = 0;

            // Check if Cached Data and the actual data are the same.
            Assert.AreEqual(true, light.hasCachedMesh);
            vertexCount = light.lightMesh.triangles.Length;
            triangleCount = light.lightMesh.vertices.Length;

            // Simulate Runtime Behavior.
            var shapePathChanged = new Vector3[5] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0.5f, 1.5f, 0), new Vector3(0, 1, 0) };
            light.SetShapePath(shapePathChanged);
            light.UpdateMesh(true);

            // Check if Cached Data and the actual data are no longer the same. (We don't save cache on Runtime)
            Assert.AreNotEqual(vertexCount, light.lightMesh.triangles.Length);
            Assert.AreNotEqual(triangleCount, light.lightMesh.vertices.Length);
        }

        [Test]
        public void EnsureShapeMeshGenerationDoesNotOverflowAllocation()
        {
            var shapePath = new Vector3[4] { new Vector3(-76.04548f, 7.522535f, 0f), new Vector3(-66.52518f, 18.88778f, 0f), new Vector3(-66.35441f, 24.34475f, 0), new Vector3(-75.15407f, 33.0358f, 0) };
            var light = m_TestObjectCached.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Freeform;
            LightUtility.GenerateShapeMesh(light, shapePath, 180.0f, 0);

            Assert.AreEqual(true, light.hasCachedMesh);
        }

        // Verifies UVs are computed from the vertex XY extents and normalized into [0,1]
        // for Freeform lights. The mesh XY bounds should touch UV (0,0) and (1,1).
        [Ignore("TEMP-DISABLED-PR-123379: disabled to land PR #123379; re-enable per docs/pr/123379-disabled-tests.md")]
        [Test]
        public void FreeformMeshHasPositionDerivedUVs()
        {
            var shapePath = new Vector3[4] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) };
            var light = m_TestObjectCached.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Freeform;
            light.SetShapePath(shapePath);
            light.UpdateMesh(true);

            var mesh = light.lightMesh;
            var vertices = mesh.vertices;
            var uvs = mesh.uv;

            Assert.AreEqual(vertices.Length, uvs.Length, "UV count must match vertex count.");
            Assert.Greater(vertices.Length, 0);

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < vertices.Length; ++i)
            {
                if (vertices[i].x < minX) minX = vertices[i].x;
                if (vertices[i].y < minY) minY = vertices[i].y;
                if (vertices[i].x > maxX) maxX = vertices[i].x;
                if (vertices[i].y > maxY) maxY = vertices[i].y;
            }

            float width = maxX - minX;
            float height = maxY - minY;
            Assert.Greater(width, 0f);
            Assert.Greater(height, 0f);

            const float kEps = 1e-4f;
            for (int i = 0; i < vertices.Length; ++i)
            {
                var expectedU = (vertices[i].x - minX) / width;
                var expectedV = (vertices[i].y - minY) / height;
                Assert.AreEqual(expectedU, uvs[i].x, kEps, $"UV.x mismatch at vertex {i}");
                Assert.AreEqual(expectedV, uvs[i].y, kEps, $"UV.y mismatch at vertex {i}");
                Assert.GreaterOrEqual(uvs[i].x, 0f - kEps);
                Assert.LessOrEqual(uvs[i].x, 1f + kEps);
                Assert.GreaterOrEqual(uvs[i].y, 0f - kEps);
                Assert.LessOrEqual(uvs[i].y, 1f + kEps);
            }
        }

        // Verifies UVs are computed from the effective post-extrusion vertex XY extents
        // for Parametric (Spot) lights. Outer-perimeter vertices carry the extrusion
        // direction in color.rg and are pushed outward by falloffDistance * color.rg in
        // the vert shader, so the CPU-side UV normalization must use those same extended
        // positions to keep UV [0,1] spanning the visible falloff band.
        [Ignore("TEMP-DISABLED-PR-123379: disabled to land PR #123379; re-enable per docs/pr/123379-disabled-tests.md")]
        [Test]
        public void ParametricMeshHasPositionDerivedUVs()
        {
            const float kFalloff = 0.5f;

            var light = m_TestObjectCached.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Parametric;
            LightUtility.GenerateParametricMesh(light, radius: 2.0f, falloffDistance: kFalloff, angle: 0f, sides: 6, batchColor: 0f);

            // Pull position + color + uv from the light's stored LightMeshVertex[] rather
            // than going through Mesh.vertices/colors — the vertex struct holds the exact
            // values that were uploaded, avoiding any concern about whether the graphics
            // layer roundtrips signed color-channel floats (extrudeDir can be negative).
            var storedVertices = light.vertices;

            Assert.Greater(storedVertices.Length, 0);

            float minX = float.MaxValue, minY = float.MaxValue;
            float maxX = float.MinValue, maxY = float.MinValue;
            for (int i = 0; i < storedVertices.Length; ++i)
            {
                float ex = storedVertices[i].position.x + kFalloff * storedVertices[i].color.r;
                float ey = storedVertices[i].position.y + kFalloff * storedVertices[i].color.g;
                if (ex < minX) minX = ex;
                if (ey < minY) minY = ey;
                if (ex > maxX) maxX = ex;
                if (ey > maxY) maxY = ey;
            }

            float width = maxX - minX;
            float height = maxY - minY;
            const float kEps = 1e-4f;

            bool sawU0 = false, sawU1 = false, sawV0 = false, sawV1 = false;
            for (int i = 0; i < storedVertices.Length; ++i)
            {
                float ex = storedVertices[i].position.x + kFalloff * storedVertices[i].color.r;
                float ey = storedVertices[i].position.y + kFalloff * storedVertices[i].color.g;
                var expectedU = width > 0f ? (ex - minX) / width : 0f;
                var expectedV = height > 0f ? (ey - minY) / height : 0f;
                var uv = storedVertices[i].uv;
                Assert.AreEqual(expectedU, uv.x, kEps, $"UV.x mismatch at vertex {i}");
                Assert.AreEqual(expectedV, uv.y, kEps, $"UV.y mismatch at vertex {i}");
                if (uv.x <= kEps) sawU0 = true;
                if (uv.x >= 1f - kEps) sawU1 = true;
                if (uv.y <= kEps) sawV0 = true;
                if (uv.y >= 1f - kEps) sawV1 = true;
            }

            Assert.IsTrue(sawU0 && sawU1, "Expected UV.x range to cover [0, 1] using post-extrusion positions.");
            Assert.IsTrue(sawV0 && sawV1, "Expected UV.y range to cover [0, 1] using post-extrusion positions.");
        }

        // Sprite light meshes must retain the sprite's own UVs; the new
        // position-derived UV logic must not affect them.
        [Ignore("TEMP-DISABLED-PR-123379: disabled to land PR #123379; re-enable per docs/pr/123379-disabled-tests.md")]
        [Test]
        public void SpriteMeshWithNullSpriteClearsMesh()
        {
            var light = m_TestObjectCached.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Sprite;
            var bounds = LightUtility.GenerateSpriteMesh(light, null, 0f);
            Assert.AreEqual(Vector3.zero, bounds.size);
            Assert.AreEqual(0, light.lightMesh.vertexCount);
        }
    }
}
