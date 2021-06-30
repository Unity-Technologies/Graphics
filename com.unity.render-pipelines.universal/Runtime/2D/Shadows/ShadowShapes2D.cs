using System.Collections.Generic;
using System.Collections.Specialized;
using Unity.Collections;
using UnityEngine;
using UnityEngine.U2D;
using System;
   

namespace UnityEngine.Rendering.Universal
{
    public class ShadowShapes2D : IShadowShapes2DProvider.ShadowShapes2D
    {
        delegate int ValueGetter<T>(ref T data, int index);
        delegate int LengthGetter<T>(ref T data);
        public struct Edge
        {
            public int v0;
            public int v1;
            public int nextEdgeIndex;

            public Edge(int indexA, int indexB)
            {
                v0 = indexA;
                v1 = indexB;
                nextEdgeIndex = -1;
            }
        }

        public class EdgeComparer : IEqualityComparer<Edge>
        {
            public bool Equals(Edge edge0, Edge edge1)
            {
                return (edge0.v0 == edge1.v0 && edge0.v1 == edge1.v1) || (edge0.v1 == edge1.v0 && edge0.v0 == edge1.v1);
            }

            public int GetHashCode(Edge edge)
            {
                int v0 = edge.v0;
                int v1 = edge.v1;

                if(edge.v1 < edge.v0)
                {
                    v0 = edge.v1;
                    v1 = edge.v0;
                }

                int hashCode = v0 << 15 | v1;
                return hashCode.GetHashCode();
            }
        }

        static public Dictionary<Edge, int> m_EdgeDictionary = new Dictionary<Edge, int>(new EdgeComparer());
        public NativeArray<Vector2> m_ProvidedVertices;
        public NativeArray<Edge>    m_ProvidedEdges;
        public NativeArray<Vector2> m_ContractionDirection;
        public NativeArray<float>   m_ContractionMaximum;

        private void ExtractEdgesFromTriangles<T>(T indices, out NativeArray<Edge> nativeOutline, ValueGetter<T> valueGetter, LengthGetter<T> lengthGetter)
        {
            // Add our edges to an edge list
            m_EdgeDictionary.Clear();
            for(int i=0;i<lengthGetter(ref indices);i+=3)
            {
                int v0Index = valueGetter(ref indices, i);
                int v1Index = valueGetter(ref indices, i + 1);
                int v2Index = valueGetter(ref indices, i + 2);

                Edge edge0 = new Edge(v0Index, v1Index);
                Edge edge1 = new Edge(v1Index, v2Index);
                Edge edge2 = new Edge(v2Index, v0Index);


                // When a contains key comparison is made edges (A, B) and (B, A) are equal (see EdgeComparer)
                if (m_EdgeDictionary.ContainsKey(edge0))
                    m_EdgeDictionary[edge0] = m_EdgeDictionary[edge0] + 1;
                else
                    m_EdgeDictionary.Add(edge0, 1);

                if (m_EdgeDictionary.ContainsKey(edge1))
                    m_EdgeDictionary[edge1] = m_EdgeDictionary[edge1] + 1;
                else
                    m_EdgeDictionary.Add(edge1, 1);

                if (m_EdgeDictionary.ContainsKey(edge2))
                    m_EdgeDictionary[edge2] = m_EdgeDictionary[edge2] + 1;
                else
                    m_EdgeDictionary.Add(edge2, 1);
            }

            // Determine how many elements to allocate
            int outsideEdges = 0;
            foreach (KeyValuePair<Edge, int> keyValuePair in m_EdgeDictionary)
            {
                if (keyValuePair.Value == 1)
                    outsideEdges++;
            }
            nativeOutline = new NativeArray<Edge>(outsideEdges, Allocator.Persistent);

            // Populate the array
            foreach (KeyValuePair<Edge, int> keyValuePair in m_EdgeDictionary)
            {
                if (keyValuePair.Value == 1)
                {
                    nativeOutline[keyValuePair.Key.v0] = keyValuePair.Key;
                }
            }
        }

        private void CalculateContractionDirection()
        {
            m_ContractionDirection = new NativeArray<Vector2>(m_ProvidedVertices.Length, Allocator.Persistent);
            m_ContractionMaximum = new NativeArray<float>(m_ProvidedVertices.Length, Allocator.Persistent);

            for (int i = 0; i < m_ProvidedEdges.Length; i++)
            {
                Edge currentEdge = m_ProvidedEdges[i];
                Edge nextEdge = m_ProvidedEdges[m_ProvidedEdges[i].v1];

                int currentVertexIndex = currentEdge.v1;

                Vector3 v0 = Vector3.Normalize(m_ProvidedVertices[currentEdge.v0]);
                Vector3 v1 = Vector3.Normalize(m_ProvidedVertices[currentEdge.v1]);
                Vector3 v2 = Vector3.Normalize(m_ProvidedVertices[nextEdge.v1]);

                Vector3 normal1 = Vector3.Normalize(Vector3.Cross(v1-v0, Vector3.forward));
                Vector3 normal2 = Vector3.Normalize(Vector3.Cross(v2-v1, Vector3.forward));

                Debug.DrawLine(m_ProvidedVertices[currentEdge.v1], (Vector3)m_ProvidedVertices[currentEdge.v1] + normal1, Color.blue, 5f);
                Debug.DrawLine(m_ProvidedVertices[currentEdge.v1], (Vector3)m_ProvidedVertices[currentEdge.v1] + normal2, Color.blue, 5f);

                m_ContractionDirection[currentVertexIndex] = 0.5f * (normal1 + normal2);
            }
        }

        private int ArrayGetter(ref ushort[] array, int index) { return array[index]; }
        private int ArrayLengthGetter(ref ushort[] array) { return array.Length; }
        private int NativeArrayGetter(ref NativeArray<int> array, int index) { return array[index]; }
        private int NativeArrayLengthGetter(ref NativeArray<int> array) { return array.Length; }

        public override void SetEdges(Vector2[] vertices, ushort[] indices, IShadowShapes2DProvider.OutlineTopology outlineTopology)
        {
            if (m_ProvidedVertices.IsCreated)
                m_ProvidedVertices.Dispose();

            if (m_ProvidedEdges.IsCreated)
                m_ProvidedEdges.Dispose();

            if (outlineTopology == IShadowShapes2DProvider.OutlineTopology.Triangles)
            {
                m_ProvidedVertices = new NativeArray<Vector2>(vertices, Allocator.Persistent);
                ExtractEdgesFromTriangles<ushort[]>(indices, out m_ProvidedEdges, ArrayGetter, ArrayLengthGetter);
            }


            CalculateContractionDirection();
        }

        public override void SetEdges(NativeArray<Vector2> vertices, NativeArray<int> indices, IShadowShapes2DProvider.OutlineTopology outlineTopology)
        {
            // Implement this later...
        }

        public override void UpdateEdges(Vector2[] vertices)
        {
            if (m_ProvidedVertices.IsCreated && vertices.Length >= m_ProvidedVertices.Length)
                m_ProvidedVertices.CopyFrom(vertices);
        }

        public override void UpdateEdges(NativeArray<Vector2> vertices)
        {
            if (m_ProvidedVertices.IsCreated && vertices.Length >= m_ProvidedVertices.Length)
                m_ProvidedVertices.CopyFrom(vertices);
           
        }

        ~ShadowShapes2D()
        {
            m_ProvidedVertices.Dispose();
            m_ProvidedEdges.Dispose();
        }
    }
}
