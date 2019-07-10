using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

namespace UnityEngine.Experimental.Rendering.LWRP
{
    internal struct ShadowGenerationInfo
    {
        public Mesh mesh;
        public NativeSlice<Vector3> vertices;
        public NativeArray<ushort> triangles;
        public NativeSlice<Vector3> normals;
        public int numberOfOutsideEdges;
        public int id;
    }
}
