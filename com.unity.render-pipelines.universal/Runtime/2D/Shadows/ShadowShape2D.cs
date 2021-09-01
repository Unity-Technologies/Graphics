using System.Collections.Generic;
using System.Collections.Specialized;
using Unity.Collections;
using UnityEngine;
using UnityEngine.U2D;
using System;
   

namespace UnityEngine.Rendering.Universal
{
    public class ShadowShape2D : IShadowShape2DProvider.ShadowShapes2D
    {
        delegate int     IndexValueGetter<T>(ref T data, int index);
        delegate Vector2 VertexValueGetter<T>(ref T data, int index);
        delegate int     LengthGetter<T>(ref T data);
    
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
        NativeArray<Vector3>         m_ProvidedVertices;
        NativeArray<Edge>            m_ProvidedEdges;
        NativeArray<int>             m_ShapeStartingIndices;

        bool                         m_SupportsContraction;

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

        private Vector2 Vector2ArrayGetter(ref Vector2[] array, int index) { return array[index]; }
        private int Vector2ArrayLengthGetter(ref Vector2[] array) { return array.Length; }
        private Vector2 Vector3ArrayGetter(ref Vector3[] array, int index) { return array[index]; }
        private int Vector3ArrayLengthGetter(ref Vector3[] array) { return array.Length; }
        private int UShortArrayGetter(ref ushort[] array, int index) { return array[index]; }
        private int UShortArrayLengthGetter(ref ushort[] array) { return array.Length; }
        private T NativeArrayGetter<T>(ref NativeArray<T> array, int index) where T : struct { return array[index]; }
        private int NativeArrayLengthGetter<T>(ref NativeArray<T> array) where T : struct { return array.Length; }

        private void CalculateEdgesFromLines<T>(T indices, IndexValueGetter<T> valueGetter, LengthGetter<T> lengthGetter)
        {
            int numOfIndices = lengthGetter(ref indices) >> 1;  // Each line is 2 indices

            NativeArray<Edge> unsortedEdges = new NativeArray<Edge>(numOfIndices, Allocator.Temp, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < numOfIndices; i+=2)
            {
                int index0 = valueGetter(ref indices, i);
                int index1 = valueGetter(ref indices, i+1);
                unsortedEdges[i] = new Edge(index0, index1); ;
            }

            SortEdges(unsortedEdges, out m_ProvidedEdges, out m_ShapeStartingIndices);
            unsortedEdges.Dispose();
        }

        private void CalculateEdgesFromLineStrip(NativeArray<int> indices)
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
            
            SortEdges(unsortedEdges, out m_ProvidedEdges, out m_ShapeStartingIndices);
            unsortedEdges.Dispose();
        }


        private void CalculateEdgesForSimpleLineStrip(int numOfIndices)
        {
            int lastIndex = numOfIndices - 1;
            m_ProvidedEdges = new NativeArray<Edge>(numOfIndices, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            for (int i = 0; i < numOfIndices; i++)
            {
                m_ProvidedEdges[i] = new Edge(lastIndex, i); ;
                lastIndex = i;
            }
        }

        private void CalculateEdgesFromTriangles(NativeArray<int> indices)
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

            SortEdges(unsortedEdges, out m_ProvidedEdges, out m_ShapeStartingIndices);
        }


        public override void SetShape(NativeArray<Vector3> vertices, NativeArray<int> indices, IShadowShape2DProvider.OutlineTopology outlineTopology)
        {
            if (m_ProvidedVertices.IsCreated)
                m_ProvidedVertices.Dispose();

            if (m_ProvidedEdges.IsCreated)
                m_ProvidedEdges.Dispose();

            Debug.Assert(vertices != null, "Vertices array cannot be null");
            if (outlineTopology == IShadowShape2DProvider.OutlineTopology.Triangles)
            {
                Debug.Assert(indices != null, "Indices array cannot be null for Triangles topology");
                CalculateEdgesFromTriangles(indices);
            }
            else if (outlineTopology == IShadowShape2DProvider.OutlineTopology.Lines)
            {

            }
            else if (outlineTopology == IShadowShape2DProvider.OutlineTopology.LineStrip)
            {
                if (indices.Length == 0)
                    CalculateEdgesForSimpleLineStrip(vertices.Length);
                else
                    CalculateEdgesFromLineStrip(indices);
            }

            // Copy the vertices
            int numVertices = vertices.Length;
            m_ProvidedVertices = new NativeArray<Vector3>(numVertices, Allocator.Persistent, NativeArrayOptions.UninitializedMemory);
            for (int i=0;i<numVertices;i++)
                m_ProvidedVertices[i] = vertices[i];
        }


        public override void UpdateVertices(NativeArray<Vector3> vertices)
        {
            if (m_ProvidedVertices.IsCreated && vertices.Length >= m_ProvidedVertices.Length)
                m_ProvidedVertices.CopyFrom(vertices);
        }

        int GetFirstUnusedIndex(NativeArray<bool> usedValues)
        {
            for(int i=0;i<usedValues.Length;i++)
            {
                if (!usedValues[i])
                    return i;
            }

            return -1;
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

        public void GenerateShadowMesh(Mesh mesh, ref BoundingSphere boundingSphere, float contractionDistance)
        {
            ShadowUtility.GenerateShadowMesh(mesh, m_ProvidedVertices, m_ProvidedEdges, m_ShapeStartingIndices, contractionDistance);
        }

        internal void GenerateShadowOutline(float contractionDistance, out NativeArray<Vector3> outline, out NativeArray<int> shapeStartingIndices)
        {
            ShadowUtility.GenerateShadowOutline(m_ProvidedVertices, m_ProvidedEdges, m_ShapeStartingIndices, contractionDistance, out outline, out shapeStartingIndices);
        }

        ~ShadowShape2D()
        {
            if (m_ProvidedVertices.IsCreated)
                m_ProvidedVertices.Dispose();

            if (m_ProvidedEdges.IsCreated)
                m_ProvidedEdges.Dispose();
        }
    }
}
