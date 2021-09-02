using System.Collections.Generic;
using System.Collections.Specialized;
using Unity.Collections;
using UnityEngine;
using UnityEngine.U2D;
using System;
   

namespace UnityEngine.Rendering.Universal
{
    internal class ShadowShape2D : IShadowShape2DProvider.ShadowShapes2D
    {
        internal struct Edge
        {
            public int v0;
            public int v1;

            public Edge(int indexA, int indexB)
            {
                v0 = indexA;
                v1 = indexB;
            }
        }

        private class EdgeComparer : IEqualityComparer<Edge>
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

        static Dictionary<Edge, int> m_EdgeDictionary = new Dictionary<Edge, int>(new EdgeComparer());  // This is done so we don't create garbage allocating and deallocating a dictionary object

        Mesh                         m_Mesh;
        BoundingSphere               m_BoundingSphere;

        bool                         m_SupportsContraction;

        public  Mesh mesh { get => m_Mesh; }
        public  BoundingSphere boundingSphere { get => m_BoundingSphere; }

        public ShadowShape2D()
        {
            m_Mesh = new Mesh();
        }

        public override bool supportsContraction
        {
            get
            {
                return m_SupportsContraction;
            }
            set
            {
                m_SupportsContraction = value;
            }
        }

        private void CalculateEdgesFromLineStrip(NativeArray<int> indices, out NativeArray<Edge> outEdges, out NativeArray<int> outShapeStartingIndices)
        {
            int numOfIndices = indices.Length;
            int lastIndex = indices[numOfIndices - 1];

            NativeArray<Edge> unsortedEdges = new NativeArray<Edge>(numOfIndices, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < numOfIndices; i++)
            {
                int curIndex = indices[i];
                unsortedEdges[i] = new Edge(lastIndex, curIndex); ;
                lastIndex = curIndex;
            }
            
            SortEdges(unsortedEdges, out outEdges, out outShapeStartingIndices);
            unsortedEdges.Dispose();
        }


        private void CalculateEdgesForSimpleLineStrip(int numOfIndices, out NativeArray<Edge> outEdges, out NativeArray<int> outShapeStartingIndices)
        {
            outShapeStartingIndices = new NativeArray<int>(1, Allocator.Persistent);
            outShapeStartingIndices[0] = 0;

            int lastIndex = numOfIndices - 1;
            outEdges = new NativeArray<Edge>(numOfIndices, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < numOfIndices; i++)
            {
                outEdges[i] = new Edge(lastIndex, i); ;
                lastIndex = i;
            }
        }

        private void CalculateEdgesFromTriangles(NativeArray<int> indices, out NativeArray<Edge> outEdges, out NativeArray<int> outShapeStartingIndices)
        {
            // Add our edges to an edge list
            m_EdgeDictionary.Clear();
            for(int i=0;i<indices.Length;i+=3)
            {
                int v0Index = indices[i];
                int v1Index = indices[i + 1];
                int v2Index = indices[i + 2];

                Edge edge0 = new Edge(v0Index, v1Index);
                Edge edge1 = new Edge(v1Index, v2Index);
                Edge edge2 = new Edge(v2Index, v0Index);


                // When a key comparison is made edges (A, B) and (B, A) are equal (see EdgeComparer)
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

            // Populate the array
            NativeArray<Edge> unsortedEdges = new NativeArray<Edge>(outsideEdges, Allocator.Temp);
            foreach (KeyValuePair<Edge, int> keyValuePair in m_EdgeDictionary)
            {
                if (keyValuePair.Value == 1)
                {
                    unsortedEdges[keyValuePair.Key.v0] = keyValuePair.Key;
                }
            }

            SortEdges(unsortedEdges, out outEdges, out outShapeStartingIndices);
        }

        void SortEdges(NativeArray<Edge> unsortedEdges, out NativeArray<Edge> sortedEdges, out NativeArray<int> shapeStartingIndices)
        {
            sortedEdges = new NativeArray<Edge>(unsortedEdges.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            shapeStartingIndices = new NativeArray<int>(unsortedEdges.Length, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);

            NativeArray<int>  edgeMap = new NativeArray<int>(unsortedEdges.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            NativeArray<bool> usedEdges = new NativeArray<bool>(unsortedEdges.Length, Allocator.Temp, NativeArrayOptions.UninitializedMemory);

            for (int i = 0; i < unsortedEdges.Length; i++)
            {
                edgeMap[unsortedEdges[i].v0] = i;
                usedEdges[i] = false;
                shapeStartingIndices[i] = -1;
            }

            int currentShape = 0;
            bool findStartingEdge = true;
            int edgeIndex = -1;
            int startingEdge = 0;
            for (int i = 0; i < unsortedEdges.Length; i++)
            {
                if (findStartingEdge)
                {
                    edgeIndex = GetFirstUnusedIndex(usedEdges);
                    startingEdge = edgeIndex;
                    shapeStartingIndices[currentShape++] = edgeIndex;
                    findStartingEdge = false;
                }

                usedEdges[edgeIndex] = true;
                sortedEdges[i] = unsortedEdges[edgeIndex];
                int nextVertex = unsortedEdges[edgeIndex].v1;
                edgeIndex = edgeMap[nextVertex];

                if (edgeIndex == startingEdge)
                    findStartingEdge = true;
            }

            usedEdges.Dispose();
            edgeMap.Dispose();
        }

        public override void SetShape(NativeArray<Vector3> vertices, NativeArray<int> indices, IShadowShape2DProvider.OutlineTopology outlineTopology)
        {
            NativeArray<Edge> edges;
            NativeArray<int> shapeStartingIndices;

            if (outlineTopology == IShadowShape2DProvider.OutlineTopology.Triangles)
            {
                CalculateEdgesFromTriangles(indices, out edges, out shapeStartingIndices);

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
                    CalculateEdgesForSimpleLineStrip(vertices.Length, out edges, out shapeStartingIndices);
                else
                    CalculateEdgesFromLineStrip(indices, out edges, out shapeStartingIndices);

                m_BoundingSphere = ShadowUtility.GenerateShadowMesh(m_Mesh, vertices, edges, shapeStartingIndices);
                edges.Dispose();
                shapeStartingIndices.Dispose();
            }
        }

        public override void UpdateVertices(NativeArray<Vector3> vertices)
        {
            // TODO
        }

        int GetFirstUnusedIndex(NativeArray<bool> usedValues)
        {
            for (int i = 0; i < usedValues.Length; i++)
            {
                if (!usedValues[i])
                    return i;
            }

            return -1;
        }

        ~ShadowShape2D()
        {
        }
    }
}
