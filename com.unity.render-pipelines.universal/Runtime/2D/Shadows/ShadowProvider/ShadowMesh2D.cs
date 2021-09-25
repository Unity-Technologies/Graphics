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
