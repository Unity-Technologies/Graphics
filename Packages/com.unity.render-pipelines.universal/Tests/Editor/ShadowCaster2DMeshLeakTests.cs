using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tests
{
    /// <summary>
    /// UUM-152591: a ShadowCaster2D with no shape source builds a shadow Mesh in Awake, from a default
    /// box. That Mesh is runtime-only and unserialized, so unless OnDestroy frees it, it outlives the
    /// caster and UnloadUnusedAssets does not reclaim it.
    /// </summary>
    class ShadowCaster2DMeshLeakTests
    {
        const int k_CasterCount = 25;

        static int LoadedMeshCount()
        {
            System.GC.Collect();
            System.GC.WaitForPendingFinalizers();
            EditorUtility.UnloadUnusedAssetsImmediate();
            System.GC.Collect();
            EditorUtility.UnloadUnusedAssetsImmediate();
            return Resources.FindObjectsOfTypeAll<Mesh>().Length;
        }

        [Test]
        public void DestroyingShapelessShadowCaster2D_ReleasesItsMesh()
        {
            int baseline = LoadedMeshCount();

            var objects = new GameObject[k_CasterCount];
            for (int i = 0; i < k_CasterCount; i++)
            {
                objects[i] = new GameObject("ShapelessShadowCaster2D");
                objects[i].AddComponent<ShadowCaster2D>();
            }

            for (int i = 0; i < k_CasterCount; i++)
                Object.DestroyImmediate(objects[i]);

            Assert.AreEqual(baseline, LoadedMeshCount(),
                $"Creating and destroying {k_CasterCount} shapeless ShadowCaster2D components left meshes behind.");
        }

        [Test]
        public void ShadowMeshAccess_AfterDestroyMesh_RebuildsTheMesh()
        {
            var go = new GameObject("ShapelessShadowCaster2D");
            try
            {
                var caster = go.AddComponent<ShadowCaster2D>();
                Mesh original = caster.mesh;
                Assert.IsNotNull(original);
                int vertexCount = original.vertexCount;

                caster.m_ShadowMesh.DestroyMesh();
                Assert.DoesNotThrow(() => caster.m_ShadowMesh.DestroyMesh(), "A second DestroyMesh call should do nothing.");

                Assert.IsTrue(original == null, "DestroyMesh should destroy the mesh it built.");
                Assert.IsNotNull(caster.mesh, "The mesh should be rebuilt from the geometry buffers.");
                Assert.AreEqual(vertexCount, caster.mesh.vertexCount);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void UndoingTheDestroyOfAShadowCaster2D_RestoresItsMesh()
        {
            var go = new GameObject("ShapelessShadowCaster2D");
            try
            {
                var caster = go.AddComponent<ShadowCaster2D>();
                int vertexCount = caster.mesh.vertexCount;

                Undo.IncrementCurrentGroup();
                Undo.DestroyObjectImmediate(caster);
                Assert.IsNull(go.GetComponent<ShadowCaster2D>());

                Undo.PerformUndo();

                var restored = go.GetComponent<ShadowCaster2D>();
                Assert.IsNotNull(restored, "Undo should bring the caster back.");
                Assert.IsNotNull(restored.mesh, "The restored caster should have a shadow mesh.");
                Assert.AreEqual(vertexCount, restored.mesh.vertexCount);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
