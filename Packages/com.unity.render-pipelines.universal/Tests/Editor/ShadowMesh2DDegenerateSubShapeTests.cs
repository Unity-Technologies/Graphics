using System.Reflection;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tests
{
    /// <summary>
    /// UUM-152375: with the Enhanced (Unity.SoftShadow) generator, an outline that holds a sub-shape with
    /// fewer than three edges must be built as if the sub-shape were not there. The generator used to size
    /// its buffers from every ring but skip the small ones when emitting, leaving uninitialized indices
    /// that made the mesh upload throw "Index buffer element is out of bounds".
    /// </summary>
    [TestFixture]
    class ShadowMesh2DDegenerateSubShapeTests
    {
        string m_PreviousOverride;

        [SetUp]
        public void SetUp()
        {
            m_PreviousOverride = Shadow2DGeometry.globalGeneratorOverride;
            Shadow2DGeometry.globalGeneratorOverride = SoftShadowGeometryGenerator.k_Id;
        }

        [TearDown]
        public void TearDown()
        {
            Shadow2DGeometry.globalGeneratorOverride = m_PreviousOverride;
        }

        static readonly Vector3[] k_SquareVertices =
        {
            new Vector3(-1, -1, 0), new Vector3(-1, 1, 0), new Vector3(1, 1, 0), new Vector3(1, -1, 0),
        };
        static readonly int[] k_SquareIndices = { 0, 1, 1, 2, 2, 3, 3, 0 };

        // The ticket's DegenerateSubShape: the square plus a closed 2-edge stub.
        static readonly Vector3[] k_StubVertices = { new Vector3(2.5f, 0, 0), new Vector3(3.5f, 0, 0) };
        static readonly int[] k_StubIndices = { 4, 5, 5, 4 };

        static void Build(ShadowMesh2D shadowMesh, Vector3[] vertices, int[] indices)
        {
            using var nativeVertices = new NativeArray<Vector3>(vertices, Allocator.Temp);
            using var nativeIndices = new NativeArray<int>(indices, Allocator.Temp);
            shadowMesh.SetShape(nativeVertices, nativeIndices, ShadowShape2D.OutlineTopology.Lines,
                ShadowShape2D.WindingOrder.Clockwise, false, true, false);
        }

        const BindingFlags k_Private = BindingFlags.Instance | BindingFlags.NonPublic;

        static NativeArray<int> Indices(ShadowMesh2D shadowMesh) =>
            (NativeArray<int>)typeof(ShadowMesh2D).GetField("m_NativeIndices", k_Private).GetValue(shadowMesh);

        static int VertexCount(ShadowMesh2D shadowMesh) =>
            (int)typeof(ShadowMesh2D).GetField("m_CustomVertexCount", k_Private).GetValue(shadowMesh);

        static Bounds LocalBounds(ShadowMesh2D shadowMesh) =>
            (Bounds)typeof(ShadowMesh2D).GetField("m_LocalBounds", k_Private).GetValue(shadowMesh);

        static T[] Concat<T>(T[] a, T[] b)
        {
            var result = new T[a.Length + b.Length];
            a.CopyTo(result, 0);
            b.CopyTo(result, a.Length);
            return result;
        }

        static void AssertIndicesInRange(ShadowMesh2D shadowMesh)
        {
            var indices = Indices(shadowMesh);
            if (!indices.IsCreated)
                return;

            int vertexCount = VertexCount(shadowMesh);
            for (int i = 0; i < indices.Length; i++)
            {
                int index = indices[i];
                Assert.That(index >= 0 && index < vertexCount,
                    $"Index buffer element #{i} (value {index}) is out of bounds; mesh only has {vertexCount} vertices.");
            }
        }

        // Builds the square alone and the square with a stub, and checks the stub changed nothing.
        static void AssertStubIsSkipped(Vector3[] vertices, int[] indices)
        {
            var control = new ShadowMesh2D();
            var degenerate = new ShadowMesh2D();
            try
            {
                Build(control, k_SquareVertices, k_SquareIndices);
                Assert.DoesNotThrow(() => Build(degenerate, vertices, indices));

                Assert.IsTrue(Indices(degenerate).IsCreated, "The degenerate sub-shape cleared the whole caster.");
                AssertIndicesInRange(degenerate);
                Assert.DoesNotThrow(() => { var _ = degenerate.mesh; });

                Assert.AreEqual(VertexCount(control), VertexCount(degenerate),
                    "The 2-edge sub-shape should add no vertices.");
                Assert.AreEqual(Indices(control).Length, Indices(degenerate).Length,
                    "The 2-edge sub-shape should add no indices.");
                Assert.AreEqual(LocalBounds(control), LocalBounds(degenerate),
                    "The 2-edge sub-shape should not grow the bounds.");
            }
            finally
            {
                control.Clear();
                control.DestroyMesh();
                degenerate.Clear();
                degenerate.DestroyMesh();
            }
        }

        [Test]
        public void SubShapeWithFewerThanThreeEdges_IsSkippedWithoutCorruptingTheMesh()
        {
            AssertStubIsSkipped(Concat(k_SquareVertices, k_StubVertices), Concat(k_SquareIndices, k_StubIndices));
        }

        [Test]
        public void SubShapeWithFewerThanThreeEdges_BeforeTheSquare_IsSkipped()
        {
            // Stub first, so the square's ring starts after the skipped one.
            AssertStubIsSkipped(Concat(k_StubVertices, k_SquareVertices),
                new[] { 0, 1, 1, 0, 2, 3, 3, 4, 4, 5, 5, 2 });
        }

        [Test]
        public void OnlySubShapesWithFewerThanThreeEdges_ClearsTheCaster()
        {
            // Two 2-edge stubs: four edges in total, but no ring is large enough to emit.
            Vector3[] vertices =
            {
                new Vector3(0, 0, 0), new Vector3(1, 0, 0),
                new Vector3(0, 2, 0), new Vector3(1, 2, 0),
            };
            int[] indices = { 0, 1, 1, 0, 2, 3, 3, 2 };

            var shadowMesh = new ShadowMesh2D();
            try
            {
                Assert.DoesNotThrow(() => Build(shadowMesh, vertices, indices));
                Assert.DoesNotThrow(() => { var _ = shadowMesh.mesh; });
                Assert.IsFalse(Indices(shadowMesh).IsCreated, "A caster with no emittable ring should be cleared.");
                Assert.AreEqual(new Bounds(), LocalBounds(shadowMesh));
            }
            finally
            {
                shadowMesh.Clear();
                shadowMesh.DestroyMesh();
            }
        }
    }
}
