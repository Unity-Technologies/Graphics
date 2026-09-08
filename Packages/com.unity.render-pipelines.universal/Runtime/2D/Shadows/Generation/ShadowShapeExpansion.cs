using Unity.Collections;

namespace UnityEngine.Rendering.Universal
{
    /// <summary>
    /// Expands an outline described by points plus per-point radii into a plain line loop:
    /// zero-radius runs stay as line segments, non-zero radii become circles or capsules.
    /// </summary>
    /// <remarks>
    /// Moved verbatim out of <see cref="ShadowMesh2D"/> so that any generation path can reuse it
    /// rather than reimplement capsule expansion. This is input normalisation, not shadow
    /// geometry: it produces an outline, and what a path does with that outline is up to the path.
    /// </remarks>
    internal static class ShadowShapeExpansion
    {
        internal const int k_CapsuleCapSegments = 8;

        internal static void AddCircle(Vector3 center, float r, NativeArray<Vector3> generatedVertices, NativeArray<int> generatedIndices, bool reverseWindingOrder, ref int vertexWritePos, ref int indexWritePos)
        {
            float direction = reverseWindingOrder ? 1 : -1;

            // Special case a full circle
            float segments = 2 * k_CapsuleCapSegments;
            float angle;
            int startWritePos = vertexWritePos;
            for (int i = 0; i < segments; i++)
            {
                angle = direction * (2 * Mathf.PI * (float)i / (float)segments);
                float x = r * Mathf.Cos(angle) + center.x;
                float y = r * Mathf.Sin(angle) + center.y;
                generatedIndices[indexWritePos++] = vertexWritePos;
                generatedIndices[indexWritePos++] = i + 1 < segments ? vertexWritePos + 1 : startWritePos;
                generatedVertices[vertexWritePos++] = new Vector3(x, y, 0);
            }
        }

        internal static void AddCapsuleCap(Vector3 center, float r, Vector3 otherCenter, NativeArray<Vector3> generatedVertices, NativeArray<int> generatedIndices, bool reverseWindingOrder, ref int vertexWritePos, ref int indexWritePos)
        {
            float startAngle;
            float endAngle;

            // Special case a full circle
            float segments = k_CapsuleCapSegments;
            Vector3 otherCenterDir = (otherCenter - center).normalized;
            float absCenterAngle = Mathf.Acos(Vector3.Dot(otherCenterDir, new Vector3(1, 0, 0)));
            float angleSign = Vector3.Dot(otherCenterDir, new Vector3(0, 1, 0)) < 0 ? -1f : 1f;
            float centerAngle = absCenterAngle * angleSign;

            // This is hard coded for a half circle
            if (reverseWindingOrder)
            {
                float HalfPI = 0.5f * Mathf.PI;
                startAngle = centerAngle + HalfPI;
                endAngle = startAngle + Mathf.PI;
            }
            else
            {
                float ThreeHalfsPI = 1.5f * Mathf.PI;
                startAngle = centerAngle + ThreeHalfsPI;
                endAngle = startAngle - Mathf.PI;
            }

            float deltaAngle = endAngle - startAngle;
            float angle;

            for (int i = 0; i < segments; i++)
            {
                angle = (deltaAngle * (float)i / (float)segments) + startAngle;
                float x = r * Mathf.Cos(angle) + center.x;
                float y = r * Mathf.Sin(angle) + center.y;
                generatedIndices[indexWritePos++] = vertexWritePos;
                generatedIndices[indexWritePos++] = vertexWritePos + 1;
                generatedVertices[vertexWritePos++] = new Vector3(x, y, 0);
            }
            angle = deltaAngle + startAngle;
            generatedVertices[vertexWritePos++] = new Vector3(r * Mathf.Cos(angle) + center.x, r * Mathf.Sin(angle) + center.y, 0);
        }

        internal static void AddCapsule(Vector3 pt0, Vector3 pt1, float r0, float r1, NativeArray<Vector3> generatedVertices, NativeArray<int> generatedIndices, bool reverseWindingOrder, ref int vertexWritePos, ref int indexWritePos)
        {
            // Add Straight Segments
            Vector3 delta = (pt1 - pt0).normalized;
            Vector3 relOffset0 = new Vector3(delta.y, -delta.x, 0);
            Vector3 relOffset1 = new Vector3(-delta.y, delta.x, 0);

            if (pt1.x < pt0.x)
            {
                Vector3 temp = pt0;
                pt0 = pt1;
                pt1 = temp;
            }

            int circle0Start = vertexWritePos;

            // Add circles
            AddCapsuleCap(pt0, r0, pt1, generatedVertices, generatedIndices, reverseWindingOrder, ref vertexWritePos, ref indexWritePos);
            generatedIndices[indexWritePos++] = vertexWritePos - 1;
            generatedIndices[indexWritePos++] = vertexWritePos;
            AddCapsuleCap(pt1, r1, pt0, generatedVertices, generatedIndices, reverseWindingOrder, ref vertexWritePos, ref indexWritePos);
            generatedIndices[indexWritePos++] = vertexWritePos - 1;
            generatedIndices[indexWritePos++] = circle0Start;
        }

        internal static int AddShape(NativeArray<Vector3> vertices, NativeArray<int> indices, int indicesProcessed, NativeArray<Vector3> generatedVertices, NativeArray<int> generatedIndices, ref int vertexWritePos, ref int indexWritePos)
        {
            int indexToProcess = indicesProcessed;
            int prevIndex = indices[indexToProcess];
            int startIndex = indices[indexToProcess];
            int startWriteIndex = vertexWritePos;

            generatedVertices[vertexWritePos++] = vertices[prevIndex];

            bool continueProcessing = true;
            while (indexToProcess < indices.Length  && continueProcessing)
            {
                int index0 = indices[indexToProcess++];
                int index1 = indices[indexToProcess++];

                generatedIndices[indexWritePos++] = vertexWritePos - 1;

                if (index1 != startIndex)
                {
                    generatedIndices[indexWritePos++] = vertexWritePos;
                    generatedVertices[vertexWritePos++] = vertices[index1];
                    continueProcessing = index0 == prevIndex;
                }
                else
                {
                    generatedIndices[indexWritePos++] = startWriteIndex;
                    continueProcessing = false;
                }

                prevIndex = index1;
            }

            return indexToProcess;
        }

        /// <summary>
        /// Expands the supplied points, indices and radii into a line-topology outline. Callers
        /// handle the empty-input case and dispose the returned <c>Allocator.Temp</c> arrays.
        /// </summary>
        internal static void GenerateShapeWithRadii(NativeArray<Vector3> vertices, NativeArray<int> indices, NativeArray<float> radii, bool reverseWindingOrder, out NativeArray<Vector3> generatedVertices, out NativeArray<int> generatedIndices)
        {
            int circleCount = 0;
            int capsuleCount = 0;
            for (int i = 0; i < indices.Length; i += 2)
            {
                int index0 = indices[i];
                int index1 = indices[i + 1];

                if (radii[index0] > 0 || radii[index1] > 0)
                {
                    if (index0 == index1)
                        circleCount++;
                    else
                        capsuleCount++;
                }
            }

            int capsuleStraightSegments = capsuleCount * 2;
            int capsuleCapSegments = capsuleCount * k_CapsuleCapSegments;  // This can be refined later
            int circleSegments = circleCount * 2 * k_CapsuleCapSegments;

            int lineCount = (indices.Length >> 1) - (capsuleCount + circleCount);
            int indexCount = 2 * (lineCount + capsuleStraightSegments + (2 * capsuleCapSegments) + circleSegments);
            int vertexCount = indexCount;  // Keep this simple for now

            generatedVertices = new NativeArray<Vector3>(vertexCount, Allocator.Temp);
            generatedIndices = new NativeArray<int>(indexCount, Allocator.Temp);

            int vertexWritePos = 0;
            int indexWritePos = 0;
            int indicesProcessed = 0;
            while (indicesProcessed < indices.Length)
            {
                int v0 = indices[indicesProcessed];
                int v1 = indices[indicesProcessed + 1];

                float r0 = radii[v0];
                float r1 = radii[v1];

                if (radii[v0] > 0 || radii[v1] > 0)
                {
                    Vector3 pt0 = vertices[v0];
                    Vector3 pt1 = vertices[v1];

                    if (vertices[v0].x == vertices[v1].x && vertices[v0].y == vertices[v1].y)
                        AddCircle(pt0, r0, generatedVertices, generatedIndices, reverseWindingOrder, ref vertexWritePos, ref indexWritePos);
                    else
                        AddCapsule(pt0, pt1, r0, r1, generatedVertices, generatedIndices, reverseWindingOrder, ref vertexWritePos, ref indexWritePos);

                    indicesProcessed += 2;
                }
                else
                {
                    // Will add edges or polygons
                    indicesProcessed = AddShape(vertices, indices, indicesProcessed, generatedVertices, generatedIndices, ref vertexWritePos, ref indexWritePos);
                }
            }
        }
    }
}
