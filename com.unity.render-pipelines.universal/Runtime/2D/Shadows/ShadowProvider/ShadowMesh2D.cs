using System.Collections.Generic;
using System.Collections.Specialized;
using Unity.Collections;
using UnityEngine;
using UnityEngine.U2D;
using System;

namespace UnityEngine.Rendering.Universal
{
    [Serializable]
    internal class ShadowMesh2D : IShadowShape2DProvider.ShadowShapes2D
    {
        const int k_CapsuleCircleSegments = 8;

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

        private void AddCircle(Vector3 center, float r, int startIndex, int endIndex, bool flip, NativeArray<Vector3> generatedVertices, NativeArray<int> generatedIndices, ref int vertexWritePos, ref int indexWritePos)
        {
            Vector3 startPt = generatedVertices[startIndex];
            Vector3 endPt = generatedVertices[endIndex];

            Vector3 startDir = (startPt - center).normalized;
            Vector3 endDir = (endPt - center).normalized;

            float startAngle;
            float endAngle;

            if (!flip)
            {
                // draw from bigger angle to smaller angle
                startAngle = Mathf.Acos(Vector3.Dot(startDir, new Vector3(1, 0, 0))) * (Vector3.Dot(startDir, new Vector3(0, 1, 0)) < 0 ? -1f : 1f);
                endAngle = Mathf.Acos(Vector3.Dot(endDir, new Vector3(1, 0, 0))) * (Vector3.Dot(endDir, new Vector3(0, 1, 0)) < 0 ? -1f : 1f);
            }
            else
            {
                // draws from smaller angle to bigger angle
                startAngle = Mathf.Abs((Vector3.Dot(startDir, new Vector3(0, 1, 0)) < 0 ? 2f * Mathf.PI : 0) - Mathf.Acos(Vector3.Dot(startDir, new Vector3(1, 0, 0))));
                endAngle = Mathf.Abs((Vector3.Dot(endDir, new Vector3(0, 1, 0)) < 0 ? 2f * Mathf.PI : 0) - Mathf.Acos(Vector3.Dot(endDir, new Vector3(1, 0, 0))));
            }

            float segments = k_CapsuleCircleSegments;
            if (startAngle == endAngle)
            {
                endAngle = startAngle + 2 * Mathf.PI;
                segments = 2 * k_CapsuleCircleSegments;
            }

            float deltaAngle = endAngle - startAngle;

            // Connect to the first line
            int prevIndex = startIndex;
            for (int i=1; i< segments;i++)
            {
                float angle = (deltaAngle * (float)i / (float)segments) + startAngle;
                float x = r * Mathf.Cos(angle) + center.x;
                float y = r * Mathf.Sin(angle) + center.y;
                generatedIndices[indexWritePos++] = prevIndex;
                generatedIndices[indexWritePos++] = vertexWritePos;
                generatedVertices[vertexWritePos] = new Vector3(x, y, 0);
                prevIndex = vertexWritePos++;
            }

            // Write out a segement to the next line
            generatedIndices[indexWritePos++] = prevIndex;
            generatedIndices[indexWritePos++] = endIndex;
        }

        private void AddCapsule(Vector3 pt0, Vector3 pt1, float r0, float r1, NativeArray<Vector3> generatedVertices, NativeArray<int> generatedIndices, ref int vertexWritePos, ref int indexWritePos)
        {
            // Add Straight Segments
            Vector3 delta = (pt1 - pt0).normalized;
            Vector3 relOffset0 = new Vector3(delta.y, -delta.x, 0);
            Vector3 relOffset1 = new Vector3(-delta.y, delta.x, 0);

            int c0Start = vertexWritePos + 1;
            int c0End = vertexWritePos + 2;
            int c1Start = vertexWritePos + 3;
            int c1End = vertexWritePos;

            if(pt1.x < pt0.x)
            {
                Vector3 temp = pt0;
                pt0 = pt1;
                pt1 = temp;
            }

            generatedIndices[indexWritePos++] = vertexWritePos;
            generatedIndices[indexWritePos++] = vertexWritePos + 1;
            generatedIndices[indexWritePos++] = vertexWritePos + 2;
            generatedIndices[indexWritePos++] = vertexWritePos + 3;

            generatedVertices[vertexWritePos++] = pt1 + r1 * relOffset0;
            generatedVertices[vertexWritePos++] = pt0 + r0 * relOffset0;
            generatedVertices[vertexWritePos++] = pt0 + r0 * relOffset1;
            generatedVertices[vertexWritePos++] = pt1 + r1 * relOffset1;

            // Add circles
            AddCircle(pt0, r0, c0Start, c0End, true, generatedVertices, generatedIndices, ref vertexWritePos, ref indexWritePos);
            AddCircle(pt1, r1, c1Start, c1End, false, generatedVertices, generatedIndices, ref vertexWritePos, ref indexWritePos);
        }

        public override void SetShapeFromCapsules(NativeArray<Vector3> vertices, NativeArray<int> indices, NativeArray<float> radii, bool ignoreLocalTransform)
        {
            if (m_Mesh == null)
                m_Mesh = new Mesh();

            int capsuleCount = 0;
            for (int i = 0; i < indices.Length; i += 2)
            {
                int index0 = indices[i];
                int index1 = indices[i + 1];

                if (radii[index0] > 0 || radii[index1] > 0)
                    capsuleCount++;
            }

            int capsuleStraightSegments = capsuleCount * 2;
            int capsuleCircleSegments = capsuleCount * k_CapsuleCircleSegments;  // This can be refined later

            int lineCount = (indices.Length >> 1) - capsuleCount;
            int indexCount = 2 * (lineCount + capsuleStraightSegments + 2* capsuleCircleSegments);
            int vertexCount = indexCount;  // Keep this simple for now

            NativeArray<Vector3> generatedVertices = new NativeArray<Vector3>(vertexCount, Allocator.Temp);
            NativeArray<int> generatedIndices = new NativeArray<int>(indexCount, Allocator.Temp);

            int vertexWritePos = 0;
            int indexWritePos = 0;
            for(int i=0;i<indices.Length;i+=2)
            {
                int v0 = indices[i];
                int v1 = indices[i + 1];
                Vector3 pt0 = vertices[v0];
                Vector3 pt1 = vertices[v1];
                float r0 = radii[v0];
                float r1 = radii[v1];

                if (radii[v0] > 0 || radii[v1] > 0)
                {
                    if (v0 != v1)
                        AddCapsule(pt0, pt1, r0, r1, generatedVertices, generatedIndices, ref vertexWritePos, ref indexWritePos);
                    else
                    {
                        int startIndex = vertexWritePos;
                        generatedVertices[vertexWritePos++] = pt0 + new Vector3(r0, 0, 0);
                        AddCircle(pt0, r0, startIndex, startIndex, true, generatedVertices, generatedIndices, ref vertexWritePos, ref indexWritePos);
                    }
                }
                else
                {
                    // Add line segment
                    generatedIndices[indexWritePos++] = vertexWritePos;
                    generatedIndices[indexWritePos++] = vertexWritePos + 1;
                    generatedVertices[vertexWritePos++] = pt0;
                    generatedVertices[vertexWritePos++] = pt1;
                    generatedVertices[vertexWritePos++] = pt1;
                }
            }

            NativeArray<ShadowEdge> calculatedEdges;
            NativeArray<int> calculatedStartingEdges;
            NativeArray<bool> calculatedIsClosedArray;
            ShadowUtility.CalculateEdgesFromLines(generatedIndices, out calculatedEdges, out calculatedStartingEdges, out calculatedIsClosedArray);

            if (m_EdgeProcessing == EdgeProcessing.Clipping)
            {
                NativeArray<Vector3> clippedVertices;
                NativeArray<ShadowEdge> clippedEdges;
                NativeArray<int> clippedStartingIndices;

                ShadowUtility.ClipEdges(generatedVertices, calculatedEdges, calculatedStartingEdges, calculatedIsClosedArray, contractEdge, out clippedVertices, out clippedEdges, out clippedStartingIndices);

                m_BoundingSphere = ShadowUtility.GenerateShadowMesh(m_Mesh, clippedVertices, clippedEdges, clippedStartingIndices, calculatedIsClosedArray, false, IShadowShape2DProvider.OutlineTopology.Lines);

                clippedVertices.Dispose();
                clippedEdges.Dispose();
                clippedStartingIndices.Dispose();
            }
            else
            {
                m_BoundingSphere = ShadowUtility.GenerateShadowMesh(m_Mesh, generatedVertices, calculatedEdges, calculatedStartingEdges, calculatedIsClosedArray, true, IShadowShape2DProvider.OutlineTopology.Lines);
            }

            generatedVertices.Dispose();
            generatedIndices.Dispose();
            calculatedEdges.Dispose();
            calculatedIsClosedArray.Dispose();
            calculatedStartingEdges.Dispose();
        }

        public override void SetShape(NativeArray<Vector3> vertices, NativeArray<int> indices, IShadowShape2DProvider.OutlineTopology outlineTopology, bool allowContraction = true)
        {
            NativeArray<ShadowEdge> edges;
            NativeArray<int> shapeStartingIndices;
            NativeArray<bool> shapeIsClosedArray;

            if (m_Mesh == null)
                m_Mesh = new Mesh();

            if (outlineTopology == IShadowShape2DProvider.OutlineTopology.Triangles)
            {
                ShadowUtility.CalculateEdgesFromTriangles(vertices, indices, true, out edges, out shapeStartingIndices, out shapeIsClosedArray);
            }
            else // if (outlineTopology == IShadowShape2DProvider.OutlineTopology.Lines)
            {
                ShadowUtility.CalculateEdgesFromLines(indices, out edges, out shapeStartingIndices, out shapeIsClosedArray);
            }

            // It would be better if we don't have to rerun SetShape after a contractEdge change.
            if (m_EdgeProcessing == EdgeProcessing.Clipping)
            {
                NativeArray<Vector3> clippedVertices;
                NativeArray<ShadowEdge> clippedEdges;
                NativeArray<int> clippedStartingIndices;

                ShadowUtility.ClipEdges(vertices, edges, shapeStartingIndices, shapeIsClosedArray, contractEdge, out clippedVertices, out clippedEdges, out clippedStartingIndices );

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

        public void SetShapeWithLines(NativeArray<Vector3> vertices, NativeArray<int> indices)
        {
            SetShape(vertices, indices, IShadowShape2DProvider.OutlineTopology.Lines);
        }

        public override void UpdateVertices(NativeArray<Vector3> vertices)
        {
            ShadowUtility.UpdateShadowMeshVertices(m_Mesh, vertices);
        }
    }
}
