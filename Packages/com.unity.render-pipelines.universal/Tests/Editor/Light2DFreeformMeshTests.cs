using NUnit.Framework;
using UnityEditor.Rendering.Universal;
using UnityEngine.Rendering.Universal;

namespace UnityEngine.Rendering.Tests
{
    class Light2DFreeformMeshTests
    {
        GameObject m_GameObject;

        [SetUp]
        public void Setup()
        {
            m_GameObject = new GameObject("Light2DFreeformMeshTests") { hideFlags = HideFlags.HideAndDontSave };
        }

        [TearDown]
        public void Cleanup()
        {
            Object.DestroyImmediate(m_GameObject);
        }

        // UUM-157159: without the 2D Common package a Freeform light renders only its falloff edge,
        // so the inspector must show an error for Freeform lights, and only for them.
        [TestCase(Light2D.LightType.Parametric)]
        [TestCase(Light2D.LightType.Point)]
        [TestCase(Light2D.LightType.Sprite)]
        [TestCase(Light2D.LightType.Global)]
        [TestCase(Light2D.LightType.Provider)]
        public void Missing2DCommonErrorNeverShowsForNonFreeformLights(Light2D.LightType type)
        {
            Assert.IsFalse(Light2DEditorUtility.ShouldShowMissing2DCommonError(type));
        }

        [Test]
        public void Missing2DCommonErrorShowsForFreeformOnlyWhenTessellatorIsMissing()
        {
            // Reads the runtime assembly's define, not the package registry: an installed 2D Common
            // below the runtime asmdef's version threshold still leaves the tessellator compiled out.
            if (LightUtility.canTessellateFreeformInterior)
                Assert.IsFalse(Light2DEditorUtility.ShouldShowMissing2DCommonError(Light2D.LightType.Freeform));
            else
                Assert.IsTrue(Light2DEditorUtility.ShouldShowMissing2DCommonError(Light2D.LightType.Freeform));
        }

        // With 2D Common, UTess fills the inside of the Freeform light.
        [Test]
        public void FreeformLightMeshHasInteriorTriangles()
        {
            if (!LightUtility.canTessellateFreeformInterior)
                Assert.Ignore("Needs com.unity.2d.common to triangulate the Freeform light interior.");

            var light = m_GameObject.AddComponent<Light2D>();
            light.lightType = Light2D.LightType.Freeform;
            light.SetShapePath(new[] { new Vector3(-1, -1), new Vector3(1, -1), new Vector3(1, 1), new Vector3(-1, 1) });
            light.UpdateMesh(true);

            var vertices = light.vertices;
            var indices = light.indices;
            Assert.That(indices, Is.Not.Null.And.Not.Empty, "Freeform light has no mesh indices.");

            // Interior vertices have color.a == 1, falloff edge vertices have color.a == 0.
            var interiorTriangles = 0;
            for (var i = 0; i + 2 < indices.Length; i += 3)
            {
                if (vertices[indices[i]].color.a > 0.5f &&
                    vertices[indices[i + 1]].color.a > 0.5f &&
                    vertices[indices[i + 2]].color.a > 0.5f)
                    interiorTriangles++;
            }

            Assert.That(interiorTriangles, Is.GreaterThan(0),
                $"Freeform light mesh has no interior triangles ({indices.Length / 3} triangles, all in the falloff ring).");
        }
    }
}
