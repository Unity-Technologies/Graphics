using Unity.Mathematics;

namespace UnityEngine.Rendering
{
    [GenerateHLSL]
    internal struct BRGGpuTransformUpdate
    {
        public float4 localToWorld0;
        public float4 localToWorld1;
        public float4 localToWorld2;

        public float4 worldToLocal0;
        public float4 worldToLocal1;
        public float4 worldToLocal2;
    }
}
