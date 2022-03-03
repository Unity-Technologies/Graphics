using System;

namespace UnityEngine.Rendering
{

    [GenerateHLSL]
    public static class GeometryPoolConstants
    {
        public static int GeoPoolPosByteSize = 3 * 4;
        public static int GeoPoolUV0ByteSize = 2 * 4;
        public static int GeoPoolUV1ByteSize = 2 * 4;
        public static int GeoPoolColByteSize = 3 * 4;
        public static int GeoPoolNormalByteSize = 3 * 4;
        public static int GeoPoolTangentByteSize = 3 * 4;
        public static int GeoPoolBatchInstanceDataByteSize = 2;
        public static int GeoPoolBatchInstancesPerDword = 4 / GeoPoolBatchInstanceDataByteSize;

        public static int GeoPoolPosByteOffset = 0;
        public static int GeoPoolUV0ByteOffset = GeoPoolPosByteOffset + GeoPoolPosByteSize;
        public static int GeoPoolUV1ByteOffset = GeoPoolUV0ByteOffset + GeoPoolUV0ByteSize;
        public static int GeoPoolColByteOffset = GeoPoolUV1ByteOffset + GeoPoolUV1ByteSize;
        public static int GeoPoolNormalByteOffset = GeoPoolColByteOffset + GeoPoolColByteSize;
        public static int GeoPoolTangentByteOffset = GeoPoolNormalByteOffset + GeoPoolNormalByteSize;

        public static int GeoPoolIndexByteSize = 4;
        public static int GeoPoolVertexByteSize =
            GeoPoolPosByteSize + GeoPoolUV0ByteSize + GeoPoolUV1ByteSize + GeoPoolNormalByteSize + GeoPoolTangentByteSize + GeoPoolNormalByteSize;
    }

    [GenerateHLSL(needAccessors = false)]
    internal struct GeoPoolVertex
    {
        public Vector3 pos;
        public Vector2 uv;
        public Vector2 uv1;
        public Vector3 C;
        public Vector3 N;
        public Vector3 T;
    }

    [Flags]
    [GenerateHLSL]
    internal enum GeoPoolInputFlags
    {
        None = 0,
        HasUV1 = 1 << 0,
        HasTangent = 1 << 1,
        HasColor = 1 << 1
    }

    [GenerateHLSL]
    internal struct GeoPoolSubMeshEntry
    {
        public int baseVertex;
        public int indexStart;
        public int indexCount;
        public uint materialKey;
    }

    [GenerateHLSL]
    internal struct GeoPoolMetadataEntry
    {
        public int vertexOffset;
        public int indexOffset;
        public int subMeshLookupOffset;
        public int subMeshEntryOffset_VertexFlags; //16 bits for submesh entry, 16 bits for vertex flags.
    }

    [GenerateHLSL]
    internal struct GeoPoolBatchTableEntry
    {
        public int offset;
        public int count;
    }
}
