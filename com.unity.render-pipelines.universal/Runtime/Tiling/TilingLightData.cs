using System.Runtime.InteropServices;
using Unity.Mathematics;

namespace UnityEngine.Rendering.Universal
{
    [StructLayout(LayoutKind.Explicit)]
    struct ShapeUnion
    {
        [FieldOffset(0)]
        public SphereShape sphere;

        [FieldOffset(0)]
        public ConeShape cone;
    }

    struct TilingLightData
    {
        public float4x4 worldToLightMatrix;

        public float3 viewOriginL;

        public LightType lightType;

        public Rect screenRect;

        public float radius;

        public float coneHeight;

        public float S;

        public float C;

        public float coneAngle;

        public float3 directionW;

        public float3 originW;

        // public ShapeUnion shape;
    }
}
