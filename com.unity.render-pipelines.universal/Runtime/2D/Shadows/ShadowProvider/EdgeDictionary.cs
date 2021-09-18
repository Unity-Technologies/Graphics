using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal
{
    internal struct EdgeDictionary : IEdgeStore
    {
        static private Dictionary<ShadowEdge, int> m_EdgeDictionary = new Dictionary<ShadowEdge, int>(new EdgeComparer());  // This is done so we don't create garbage allocating and deallocating a dictionary object

        private class EdgeComparer : IEqualityComparer<ShadowEdge>
        {
            public bool Equals(ShadowEdge edge0, ShadowEdge edge1)
            {
                return (edge0.v0 == edge1.v0 && edge0.v1 == edge1.v1) || (edge0.v1 == edge1.v0 && edge0.v0 == edge1.v1);
            }

            public int GetHashCode(ShadowEdge edge)
            {
                int v0 = edge.v0;
                int v1 = edge.v1;

                if (edge.v1 < edge.v0)
                {
                    v0 = edge.v1;
                    v1 = edge.v0;
                }

                int hashCode = v0 << 15 | v1;
                return hashCode.GetHashCode();
            }
        }
        public NativeArray<ShadowEdge> GetOutsideEdges(NativeArray<Vector3> vertices, NativeArray<int> indices)
        {
            m_EdgeDictionary.Clear();
            m_EdgeDictionary.EnsureCapacity(indices.Length);

            for (int i = 0; i < indices.Length; i += 3)
            {
                int v0Index = indices[i];
                int v1Index = indices[i + 1];
                int v2Index = indices[i + 2];

                ShadowEdge edge0 = new ShadowEdge(v0Index, v1Index);
                ShadowEdge edge1 = new ShadowEdge(v1Index, v2Index);
                ShadowEdge edge2 = new ShadowEdge(v2Index, v0Index);

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

            // Count the outside edges
            int outsideEdges = 0;
            foreach (KeyValuePair<ShadowEdge, int> keyValuePair in m_EdgeDictionary)
            {
                if (keyValuePair.Value == 1)
                    outsideEdges++;
            }

            // Create unsorted edges array
            int edgeIndex = 0;
            NativeArray<ShadowEdge> edges = new NativeArray<ShadowEdge>(outsideEdges, Allocator.Temp);
            foreach (KeyValuePair<ShadowEdge, int> keyValuePair in m_EdgeDictionary)
            {
                if (keyValuePair.Value == 1)
                {
                    edges[edgeIndex++] = keyValuePair.Key;
                }
            }

            return edges;
        }
    }
}
