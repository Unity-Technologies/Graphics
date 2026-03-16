using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;


internal struct VertexDictionary
{
    static Dictionary<Vector3, int> m_VertexDictionary = new Dictionary<Vector3, int>();

    public NativeArray<int> GetIndexRemap(NativeArray<Vector3> vertices, NativeArray<int> indices)
    {
        // Create a mapping to unique vertices
        NativeArray<int> vertexMapping = new NativeArray<int>(vertices.Length, Allocator.Temp);
        m_VertexDictionary.Clear();
        m_VertexDictionary.EnsureCapacity(vertices.Length);
        for (int i = 0; i < vertices.Length; i++)
        {
            Vector3 vertex = vertices[i];
            if (!m_VertexDictionary.ContainsKey(vertex))
            {
                vertexMapping[i] = i;
                m_VertexDictionary.Add(vertex, i);
            }
            else
            {
                vertexMapping[i] = m_VertexDictionary[vertex];
            }
        }

        NativeArray<int> remappedIndices = new NativeArray<int>(indices.Length, Allocator.Temp);
        for (int i = 0; i < indices.Length; i++)
        {
            remappedIndices[i] = vertexMapping[indices[i]];
        }

        return remappedIndices;
    }
}
