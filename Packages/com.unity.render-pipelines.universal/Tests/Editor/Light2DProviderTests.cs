using NUnit.Framework;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Tests
{
    class Light2DProviderTests
    {
        // A minimal provider that keeps the default GetMesh, which returns null.
        class NoMeshLightProvider : Light2DProvider { }

        GameObject m_GameObject;

        [SetUp]
        public void Setup()
        {
            m_GameObject = new GameObject("Light2DProviderTests") { hideFlags = HideFlags.HideAndDontSave };
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(m_GameObject);
        }

        // UUM-153965: the render path reads lightMesh, which allocates a mesh, and then
        // UpdateMesh swaps in the provider's null mesh and reads its bounds.
        [Test]
        public void ProviderLightWithNullMeshDoesNotThrowOnUpdateMesh()
        {
            var light = m_GameObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Provider;
            light.light2DProvider = new NoMeshLightProvider();

            for (var frame = 0; frame < 2; frame++)
            {
                _ = light.lightMesh;
                Assert.DoesNotThrow(() => light.UpdateMesh(false), $"UpdateMesh threw on frame {frame}.");
            }
        }

        // A null provider mesh is replaced by one empty mesh the light owns, made once and kept.
        [Test]
        public void ProviderLightWithNullMeshKeepsOneEmptyMesh()
        {
            var light = m_GameObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Provider;
            light.light2DProvider = new NoMeshLightProvider();

            Mesh firstMesh = null;
            for (var frame = 0; frame < 3; frame++)
            {
                light.UpdateMesh(false);
                var mesh = light.lightMesh;
                if (frame == 0)
                    firstMesh = mesh;

                Assert.AreSame(firstMesh, mesh, $"lightMesh changed on frame {frame}.");
                Assert.AreEqual(0, mesh.vertexCount, $"lightMesh is not empty on frame {frame}.");
            }
        }

        // A provider that goes from mesh A to null and back must get A again, even when lightMesh is not read
        // in between (the light was culled). A belongs to the provider and must not be destroyed.
        [Test]
        public void ProviderLightPicksUpMeshAgainAfterNullWithoutDrawing()
        {
            var meshA = CreateTriangleMesh();
            try
            {
                var provider = new SwitchableMeshLightProvider();
                var light = m_GameObject.AddComponent<Light2D>();
                light.lightType = Light2D.LightType.Provider;
                light.light2DProvider = provider;

                provider.mesh = meshA;
                light.UpdateMesh(false);
                provider.mesh = null;
                light.UpdateMesh(false);
                provider.mesh = meshA;
                light.UpdateMesh(false);

                Assert.IsTrue(meshA != null, "The provider's mesh was destroyed.");
                Assert.AreSame(meshA, light.lightMesh);
                Assert.AreEqual(meshA.bounds, GetLocalBounds(light));
            }
            finally
            {
                Object.DestroyImmediate(meshA);
            }
        }

        // Removing the provider must not clear a mesh that the provider owns.
        [Test]
        public void RemovingProviderDoesNotClearProviderMesh()
        {
            var meshA = CreateTriangleMesh();
            try
            {
                var light = m_GameObject.AddComponent<Light2D>();
                light.lightType = Light2D.LightType.Provider;
                light.light2DProvider = new SwitchableMeshLightProvider { mesh = meshA };
                light.UpdateMesh(false);
                Assert.AreSame(meshA, light.lightMesh);

                light.light2DProvider = null;
                light.UpdateMesh(false);

                Assert.IsTrue(meshA != null, "The provider's mesh was destroyed.");
                Assert.AreEqual(3, meshA.vertexCount, "The provider's mesh was cleared.");
            }
            finally
            {
                Object.DestroyImmediate(meshA);
            }
        }

        class SwitchableMeshLightProvider : Light2DProvider
        {
            public Mesh mesh;
            public override Mesh GetMesh() { return mesh; }
        }

        static Mesh CreateTriangleMesh()
        {
            var mesh = new Mesh { hideFlags = HideFlags.HideAndDontSave };
            mesh.vertices = new[] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(0, 2, 0) };
            mesh.triangles = new[] { 0, 1, 2 };
            mesh.RecalculateBounds();
            return mesh;
        }

        static Bounds GetLocalBounds(Light2D light)
        {
            var field = typeof(Light2D).GetField("m_LocalBounds", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            return (Bounds)field.GetValue(light);
        }
    }
}
