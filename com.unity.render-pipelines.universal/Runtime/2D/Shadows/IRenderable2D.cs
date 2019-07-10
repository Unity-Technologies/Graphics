using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Collections;

namespace UnityEngine.Experimental.Rendering.Universal
{
    public interface IRenderable2D
    {
        NativeArray<ushort> GetTriangles();
        NativeSlice<Vector3> GetVertices();
        NativeSlice<Vector3> GetNormals();
        int GetId();
    }
}
