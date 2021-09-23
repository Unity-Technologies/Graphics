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

        [SerializeField] Mesh m_Mesh;
        [SerializeField] BoundingSphere m_BoundingSphere;

        public  Mesh mesh { get => m_Mesh; }
        public  BoundingSphere boundingSphere { get => m_BoundingSphere; }


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

            m_BoundingSphere = ShadowUtility.GenerateShadowMesh(m_Mesh, vertices, edges, shapeStartingIndices, shapeIsClosedArray, allowContraction, outlineTopology);
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
