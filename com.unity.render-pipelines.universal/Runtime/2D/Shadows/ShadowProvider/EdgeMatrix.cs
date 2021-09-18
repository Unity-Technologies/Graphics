using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

namespace UnityEngine.Rendering.Universal
{
    internal struct EdgeMatrix : IEdgeStore
    {
        public NativeArray<ShadowEdge> GetOutsideEdges(NativeArray<Vector3> vertices, NativeArray<int> indices)
        {
            return new NativeArray<ShadowEdge>(0, Allocator.Temp);
        }
    }
}
