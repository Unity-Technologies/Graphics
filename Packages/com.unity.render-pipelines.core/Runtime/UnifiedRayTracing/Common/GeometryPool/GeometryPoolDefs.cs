using System;

namespace UnityEngine.Rendering.UnifiedRayTracing
{
    internal static class GeometryPoolConstants
    {
        public const int GeoPoolPosByteSize = 3 * 4;
        const int UvFieldSizeInDWords = 2;
        public const int GeoPoolUV0ByteSize = UvFieldSizeInDWords * 4;
        public const int GeoPoolUV1ByteSize = UvFieldSizeInDWords * 4;
        public const int GeoPoolNormalByteSize = 1 * 4;

        public const int GeoPoolPosByteOffset = 0;
        public const int GeoPoolUV0ByteOffset = GeoPoolPosByteOffset + GeoPoolPosByteSize;
        public const int GeoPoolUV1ByteOffset = GeoPoolUV0ByteOffset + GeoPoolUV0ByteSize;
        public const int GeoPoolNormalByteOffset = GeoPoolUV1ByteOffset + GeoPoolUV1ByteSize;

        public const int GeoPoolIndexByteSize = 4;
        public const int GeoPoolVertexByteSize = GeoPoolPosByteSize + GeoPoolUV0ByteSize + GeoPoolUV1ByteSize + GeoPoolNormalByteSize;
    }

    internal struct GeoPoolVertex
    {
        public Vector3 pos;
        public Vector2 uv0;
        public Vector2 uv1;
        public Vector3 N;
    }

    internal struct GeoPoolMeshChunk
    {
        public int indexOffset;
        public int indexCount;
        public int vertexOffset;
        public int vertexCount;
    }

    [Flags]
    internal enum GeoPoolVertexAttribs { Position = 1, Normal = 2, Uv0 = 4, Uv1 = 8 }
}
