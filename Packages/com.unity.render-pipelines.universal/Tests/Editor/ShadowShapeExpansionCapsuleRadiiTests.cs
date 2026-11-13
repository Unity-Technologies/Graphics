using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UnityEditor.Rendering.Universal.Tests
{
    /// <summary>
    /// UUM-161210: a capsule edge must keep each end's radius on its own end, whatever the edge's direction.
    /// AddCapsule swapped the endpoints when the edge ran right to left but kept the radii in place,
    /// so each cap got the other end's radius.
    /// </summary>
    [TestFixture]
    class ShadowShapeExpansionCapsuleRadiiTests
    {
        const float k_Tolerance = 1e-4f;

        // Measures the cap radius around each end: the largest distance from that end
        // of any generated vertex that sits closer to it than to the other end.
        static void MeasureCapRadii(Vector3 pt0, Vector3 pt1, float r0, float r1, bool reverseWindingOrder, out float measured0, out float measured1)
        {
            using var vertices = new NativeArray<Vector3>(new[] { pt0, pt1 }, Allocator.Temp);
            using var indices = new NativeArray<int>(new[] { 0, 1 }, Allocator.Temp);
            using var radii = new NativeArray<float>(new[] { r0, r1 }, Allocator.Temp);

            ShadowShapeExpansion.GenerateShapeWithRadii(vertices, indices, radii, reverseWindingOrder, out var genVerts, out var genIndices);
            using (genVerts)
            using (genIndices)
            {
                measured0 = 0;
                measured1 = 0;
                var capVertexCount = 2 * (ShadowShapeExpansion.k_CapsuleCapSegments + 1);
                for (int i = 0; i < capVertexCount; i++)
                {
                    var d0 = Vector3.Distance(genVerts[i], pt0);
                    var d1 = Vector3.Distance(genVerts[i], pt1);
                    if (d0 < d1)
                        measured0 = Mathf.Max(measured0, d0);
                    else
                        measured1 = Mathf.Max(measured1, d1);
                }
            }
        }

        [TestCase(0f, 0f, 1f, 0f, false, TestName = "CapsuleKeepsEndRadii_LeftToRight")]
        [TestCase(0f, 0f, 0f, 1f, false, TestName = "CapsuleKeepsEndRadii_BottomToTop")]
        [TestCase(1f, 0f, 0f, 0f, false, TestName = "CapsuleKeepsEndRadii_RightToLeft")]
        [TestCase(0f, 1f, 0f, 0f, false, TestName = "CapsuleKeepsEndRadii_TopToBottom")]
        [TestCase(1f, 1f, 0f, 0f, false, TestName = "CapsuleKeepsEndRadii_DiagonalRightToLeft")]
        [TestCase(0f, 0f, 1f, 0f, true, TestName = "CapsuleKeepsEndRadii_LeftToRight_ReverseWinding")]
        [TestCase(1f, 0f, 0f, 0f, true, TestName = "CapsuleKeepsEndRadii_RightToLeft_ReverseWinding")]
        public void CapsuleKeepsEndRadii(float x0, float y0, float x1, float y1, bool reverseWindingOrder)
        {
            var pt0 = new Vector3(x0, y0, 0);
            var pt1 = new Vector3(x1, y1, 0);
            const float r0 = 0.5f;
            const float r1 = 0.1f;

            MeasureCapRadii(pt0, pt1, r0, r1, reverseWindingOrder, out var measured0, out var measured1);

            Assert.AreEqual(r0, measured0, k_Tolerance, $"Cap at {pt0} should have radius {r0}.");
            Assert.AreEqual(r1, measured1, k_Tolerance, $"Cap at {pt1} should have radius {r1}.");
        }

        // Shoelace sum over the generated line list. Positive is counterclockwise.
        static float SignedArea(Vector3 pt0, Vector3 pt1, float r0, float r1, bool reverseWindingOrder)
        {
            using var vertices = new NativeArray<Vector3>(new[] { pt0, pt1 }, Allocator.Temp);
            using var indices = new NativeArray<int>(new[] { 0, 1 }, Allocator.Temp);
            using var radii = new NativeArray<float>(new[] { r0, r1 }, Allocator.Temp);

            ShadowShapeExpansion.GenerateShapeWithRadii(vertices, indices, radii, reverseWindingOrder, out var genVerts, out var genIndices);
            using (genVerts)
            using (genIndices)
            {
                float area = 0;
                for (int i = 0; i < genIndices.Length; i += 2)
                {
                    var a = genVerts[genIndices[i]];
                    var b = genVerts[genIndices[i + 1]];
                    area += a.x * b.y - b.x * a.y;
                }
                return 0.5f * area;
            }
        }

        // Without the endpoint swap, an edge and its reverse must still give the same outline and winding.
        [TestCase(false, TestName = "CapsuleOutlineIndependentOfEdgeDirection")]
        [TestCase(true, TestName = "CapsuleOutlineIndependentOfEdgeDirection_ReverseWinding")]
        public void CapsuleOutlineIndependentOfEdgeDirection(bool reverseWindingOrder)
        {
            var a = new Vector3(0, 0, 0);
            var b = new Vector3(1, 0.25f, 0);
            const float ra = 0.5f;
            const float rb = 0.1f;

            var forward = SignedArea(a, b, ra, rb, reverseWindingOrder);
            var backward = SignedArea(b, a, rb, ra, reverseWindingOrder);

            Assert.AreEqual(reverseWindingOrder, forward > 0, "Capsule winding should follow reverseWindingOrder.");
            Assert.AreEqual(forward, backward, k_Tolerance, "Reversing the edge should not change the capsule outline.");
        }
    }
}
