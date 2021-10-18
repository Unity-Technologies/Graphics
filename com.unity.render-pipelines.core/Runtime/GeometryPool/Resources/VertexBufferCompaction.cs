using UnityEngine;
using UnityEngine.Rendering;

namespace UnityEngine.Rendering
{

    public static class VertexBufferCompaction
    {
        [GenerateHLSL]
        public class VisibilityBufferConstants
        {
            public static int s_ClusterSizeInTriangles = 128;
            public static int s_ClusterSizeInIndices = s_ClusterSizeInTriangles * 3;
        }

        [GenerateHLSL(needAccessors = false)]
        internal struct CompactVertex
        {
            public Vector3 pos;
            public Vector2 uv;
            public Vector2 uv1;
            public Vector3 N;
            public Vector4 T;
        }

        [GenerateHLSL(needAccessors = false)]
        internal struct InstanceVData
        {
            public Matrix4x4 localToWorld;
            public uint materialData;
            public uint chunkStartIndex;
            public Vector4 lightmapST;
        }
    }

}
