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
        [SerializeField] bool m_AllowContraction;

        public  Mesh mesh { get => m_Mesh; }
        public  BoundingSphere boundingSphere { get => m_BoundingSphere; }


        public override void SetShape(NativeArray<Vector3> vertices, NativeArray<int> indices, IShadowShape2DProvider.OutlineTopology outlineTopology, bool allowContraction = true)
        {
            NativeArray<ShadowEdge> edges;
            NativeArray<int> shapeStartingIndices;

            m_AllowContraction = allowContraction;

            if (m_Mesh == null)
                m_Mesh = new Mesh();

            if (outlineTopology == IShadowShape2DProvider.OutlineTopology.Triangles)
            {
                ShadowUtility.CalculateEdgesFromTriangles(vertices, indices, out edges, out shapeStartingIndices);
                //FixWindingOrder(vertices, shapeStartingIndices, edges);

                m_BoundingSphere = ShadowUtility.GenerateShadowMesh(m_Mesh, vertices, edges, shapeStartingIndices);
                edges.Dispose();
                shapeStartingIndices.Dispose();
            }
            else if (outlineTopology == IShadowShape2DProvider.OutlineTopology.Lines)
            {
                

            }
            else if (outlineTopology == IShadowShape2DProvider.OutlineTopology.LineStrip)
            {
                if (indices.Length == 0)
                    ShadowUtility.CalculateEdgesForSimpleLineStrip(vertices.Length, out edges, out shapeStartingIndices);
                else
                    ShadowUtility.CalculateEdgesFromLineStrip(indices, out edges, out shapeStartingIndices);

                ShadowUtility.FixWindingOrder(vertices, shapeStartingIndices, edges);
                m_BoundingSphere = ShadowUtility.GenerateShadowMesh(m_Mesh, vertices, edges, shapeStartingIndices);
                edges.Dispose();
                shapeStartingIndices.Dispose();
            }
        }

        public override void UpdateVertices(NativeArray<Vector3> vertices)
        {
            ShadowUtility.UpdateShadowMeshVertices(m_Mesh, vertices);
        }
    }
}
