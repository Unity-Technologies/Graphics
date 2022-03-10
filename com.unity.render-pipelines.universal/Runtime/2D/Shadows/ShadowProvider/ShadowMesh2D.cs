using System.Collections.Generic;
using System.Collections.Specialized;
using Unity.Collections;
using UnityEngine;
using UnityEngine.U2D;
using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    internal class ShadowMesh2D : ShadowShape2D
    {
        const int k_CapsuleCapSegments = 8;

        public enum EdgeProcessing
        {
            None,
            Clipping,
        }

        [SerializeField] Mesh m_Mesh;
        [SerializeField] BoundingSphere m_BoundingSphere;
        [SerializeField] EdgeProcessing m_EdgeProcessing = EdgeProcessing.Clipping;
        [SerializeField] float m_ContractEdge;

        public  Mesh mesh { get => m_Mesh; }
        public  BoundingSphere boundingSphere { get => m_BoundingSphere; }
        public  EdgeProcessing edgeProcessing { get { return m_EdgeProcessing; } set { m_EdgeProcessing = value; } }
        public float contractEdge { get { return m_ContractEdge; } set { m_ContractEdge = value; } }

        private void AddCircle(Vector3 center, float r, NativeArray<Vector3> generatedVertices, NativeArray<int> generatedIndices, bool reverseWindingOrder, ref int vertexWritePos, ref int indexWritePos)
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

        private void AddCapsuleCap(Vector3 center, float r, Vector3 otherCenter, NativeArray<Vector3> generatedVertices, NativeArray<int> generatedIndices, bool reverseWindingOrder, ref int vertexWritePos, ref int indexWritePos)
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

        private void AddCapsule(Vector3 pt0, Vector3 pt1, float r0, float r1, NativeArray<Vector3> generatedVertices, NativeArray<int> generatedIndices, bool reverseWindingOrder, ref int vertexWritePos, ref int indexWritePos)
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

        int AddShape(NativeArray<Vector3> vertices, NativeArray<int> indices, int indicesProcessed, NativeArray<Vector3> generatedVertices, NativeArray<int> generatedIndices, ref int vertexWritePos, ref int indexWritePos)
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

        public override void SetShapeFromCapsules(NativeArray<Vector3> vertices, NativeArray<int> indices, NativeArray<float> radii, ShadowShape2D.WindingOrder windingOrder = ShadowShape2D.WindingOrder.Clockwise, bool allowContraction = true)        {
            if (m_Mesh == null)
                m_Mesh = new Mesh();

            if (indices.Length == 0)
            {
                m_Mesh.Clear();
                return;
            }

            bool reverseWindingOrder = windingOrder == ShadowShape2D.WindingOrder.CounterClockwise;


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

            NativeArray<Vector3> generatedVertices = new NativeArray<Vector3>(vertexCount, Allocator.Temp);
            NativeArray<int> generatedIndices = new NativeArray<int>(indexCount, Allocator.Temp);

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

            NativeArray<ShadowEdge> calculatedEdges;
            NativeArray<int> calculatedStartingEdges;
            NativeArray<bool> calculatedIsClosedArray;

            ShadowUtility.CalculateEdgesFromLines(generatedIndices, out calculatedEdges, out calculatedStartingEdges, out calculatedIsClosedArray);

            if(reverseWindingOrder)
                ShadowUtility.ReverseWindingOrder(calculatedStartingEdges, calculatedEdges);

            if (m_EdgeProcessing == EdgeProcessing.Clipping)
            {
                NativeArray<Vector3> clippedVertices;
                NativeArray<ShadowEdge> clippedEdges;
                NativeArray<int> clippedStartingIndices;

                ShadowUtility.ClipEdges(generatedVertices, calculatedEdges, calculatedStartingEdges, calculatedIsClosedArray, contractEdge, out clippedVertices, out clippedEdges, out clippedStartingIndices);

                if (clippedStartingIndices.Length > 0)
                    m_BoundingSphere = ShadowUtility.GenerateShadowMesh(m_Mesh, clippedVertices, clippedEdges, clippedStartingIndices, calculatedIsClosedArray, true, ShadowShape2D.OutlineTopology.Lines);
                else
                {
                    m_BoundingSphere = new BoundingSphere();
                    m_Mesh.Clear();
                }

                clippedVertices.Dispose();
                clippedEdges.Dispose();
                clippedStartingIndices.Dispose();
            }
            else
            {
                m_BoundingSphere = ShadowUtility.GenerateShadowMesh(m_Mesh, generatedVertices, calculatedEdges, calculatedStartingEdges, calculatedIsClosedArray, true, ShadowShape2D.OutlineTopology.Lines);
            }

            generatedVertices.Dispose();
            generatedIndices.Dispose();
            calculatedEdges.Dispose();
            calculatedIsClosedArray.Dispose();
            calculatedStartingEdges.Dispose();
        }

        public override void SetShape(NativeArray<Vector3> vertices, NativeArray<int> indices, ShadowShape2D.OutlineTopology outlineTopology, ShadowShape2D.WindingOrder windingOrder = ShadowShape2D.WindingOrder.Clockwise, bool allowContraction = true)
        {
            NativeArray<ShadowEdge> edges;
            NativeArray<int> shapeStartingIndices;
            NativeArray<bool> shapeIsClosedArray;

            if (m_Mesh == null)
                m_Mesh = new Mesh();

            if (indices.Length == 0)
            {
                m_Mesh.Clear();
                return;
            }

            if (outlineTopology == ShadowShape2D.OutlineTopology.Triangles)
            {
                ShadowUtility.CalculateEdgesFromTriangles(vertices, indices, true, out edges, out shapeStartingIndices, out shapeIsClosedArray);
            }
            else // if (outlineTopology == ShadowShape2D.OutlineTopology.Lines)
            {
                ShadowUtility.CalculateEdgesFromLines(indices, out edges, out shapeStartingIndices, out shapeIsClosedArray);
            }

            if (windingOrder == ShadowShape2D.WindingOrder.CounterClockwise)
                ShadowUtility.ReverseWindingOrder(shapeStartingIndices, edges);

            // It would be better if we don't have to rerun SetShape after a contractEdge change.
            if (m_EdgeProcessing == EdgeProcessing.Clipping && allowContraction)
            {
                NativeArray<Vector3> clippedVertices;
                NativeArray<ShadowEdge> clippedEdges;
                NativeArray<int> clippedStartingIndices;

                ShadowUtility.ClipEdges(vertices, edges, shapeStartingIndices, shapeIsClosedArray, contractEdge, out clippedVertices, out clippedEdges, out clippedStartingIndices);

                m_BoundingSphere = ShadowUtility.GenerateShadowMesh(m_Mesh, clippedVertices, clippedEdges, clippedStartingIndices, shapeIsClosedArray, allowContraction, outlineTopology);

                clippedVertices.Dispose();
                clippedEdges.Dispose();
                clippedStartingIndices.Dispose();
            }
            else
            {
                m_BoundingSphere = ShadowUtility.GenerateShadowMesh(m_Mesh, vertices, edges, shapeStartingIndices, shapeIsClosedArray, allowContraction, outlineTopology);
            }

            edges.Dispose();
            shapeStartingIndices.Dispose();
        }

        public void SetShapeWithLines(NativeArray<Vector3> vertices, NativeArray<int> indices, bool allowContraction)
        {
            SetShape(vertices, indices, ShadowShape2D.OutlineTopology.Lines, allowContraction: allowContraction);
        }

        public override void UpdateVertices(NativeArray<Vector3> vertices)
        {
            ShadowUtility.UpdateShadowMeshVertices(m_Mesh, vertices);
        }
    }
}
