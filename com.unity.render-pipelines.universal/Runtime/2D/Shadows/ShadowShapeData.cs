using System.Collections.Generic;
using System.Collections.Specialized;
using Unity.Collections;
using UnityEngine;
using UnityEngine.U2D;
using System;
   

namespace UnityEngine.Rendering.Universal
{
    internal class ShadowShapes : IShadowShapesProvider.ShadowShapes
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

        class EdgeComparer : IComparer<Edge>
        {
            public int Compare(Edge edge0, Edge edge1)
            {
                int smallest0 = edge0.v0;
                int largest0 = edge0.v1;
                int smallest1 = edge1.v0;
                int largest1 = edge1.v1;

                if (edge0.v0 > edge0.v1)
                {
                    smallest0 = edge0.v1;
                    largest0 = edge0.v0;
                }

                if(edge1.v0 > edge1.v1)
                {
                    smallest1 = edge1.v1;
                    largest1 = edge1.v0;
                }

                if (smallest0 < smallest1)
                    return -1;
                else if (smallest1 < smallest0)
                    return 1;
                else
                    return largest0 - largest1;
            }
        }

        static private SortedDictionary<Edge, int> m_EdgeDictionary = new SortedDictionary<Edge, int>(new EdgeComparer());
        private NativeArray<Vector2> m_Vertices;
        private NativeArray<Edge>    m_Outline;   

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

                if (m_EdgeDictionary.ContainsKey(edge0))
                    m_EdgeDictionary[edge0] = m_EdgeDictionary[edge0] + 1;
                else
                    m_EdgeDictionary.Add(edge0, 1);

                if (m_EdgeDictionary.ContainsKey(edge1))
                    m_EdgeDictionary[edge1] = m_EdgeDictionary[edge1] + 1;
                else
                    m_EdgeDictionary.Add(edge1, 1);

                if (m_EdgeDictionary.ContainsKey(edge2))
                    m_EdgeDictionary[edge1] = m_EdgeDictionary[edge2] + 1;
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
            bool loopStart = true;
            int loopStartIndex = -1;
            foreach (KeyValuePair<Edge, int> keyValuePair in m_EdgeDictionary)
            {
                if (keyValuePair.Value == 1)
                {
                    Edge curEdge = keyValuePair.Key;
                    curEdge.nextEdgeIndex = outsideEdges + 1;

                    // If this is the first edge in the looop
                    if (loopStart)
                    {
                        loopStartIndex = outsideEdges;
                        loopStart = false;
                    }

                    // If this is the last edge in the loop
                    if (keyValuePair.Key.v1 == loopStartIndex)
                    {
                        loopStart = true;
                        curEdge.nextEdgeIndex = loopStartIndex;
                    }

                    nativeOutline[outsideEdges++] = keyValuePair.Key;
                }
            }
        }

        private int ArrayGetter(ref int[] array, int index) { return array[index]; }
        private int ArrayLengthGetter(ref int[] array) { return array.Length; }
        private int NativeArrayGetter(ref NativeArray<int> array, int index) { return array[index]; }
        private int NativeArrayLengthGetter(ref NativeArray<int> array) { return array.Length; }

        public override void SetEdges(Vector2[] vertices, int[] indices, IShadowShapesProvider.OutlineTopology outlineTopology)
        {
            if (m_Vertices.IsCreated)
                m_Vertices.Dispose();

            if (m_Outline.IsCreated)
                m_Outline.Dispose();

            if (outlineTopology == IShadowShapesProvider.OutlineTopology.Triangles)
            {
                m_Vertices = new NativeArray<Vector2>(vertices, Allocator.Persistent);
                ExtractEdgesFromTriangles<int[]>(indices, out m_Outline, ArrayGetter, ArrayLengthGetter);
            }
        }

        public override void SetEdges(NativeArray<Vector2> vertices, NativeArray<int> indices, IShadowShapesProvider.OutlineTopology outlineTopology)
        {
            if (m_Vertices.IsCreated)
                m_Vertices.Dispose();

            if (m_Outline.IsCreated)
                m_Outline.Dispose();

            if (outlineTopology == IShadowShapesProvider.OutlineTopology.Triangles)
            {
                m_Vertices = new NativeArray<Vector2>(vertices, Allocator.Persistent);
                ExtractEdgesFromTriangles<NativeArray<int>>(indices, out m_Outline, NativeArrayGetter, NativeArrayLengthGetter);
            }
        }

        public override void UpdateEdges(Vector2[] vertices)
        {
            if (m_Vertices.IsCreated && vertices.Length >= m_Vertices.Length)
                m_Vertices.CopyFrom(vertices);
        }

        public override void UpdateEdges(NativeArray<Vector2> vertices)
        {
            if (m_Vertices.IsCreated && vertices.Length >= m_Vertices.Length)
                m_Vertices.CopyFrom(vertices);
           
        }
    }
}
