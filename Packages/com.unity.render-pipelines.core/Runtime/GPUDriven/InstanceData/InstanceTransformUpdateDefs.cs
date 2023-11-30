using Unity.Mathematics;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering
{
    [GenerateHLSL]
    internal struct TransformUpdatePacket
    {
        public float4 localToWorld0;
        public float4 localToWorld1;
        public float4 localToWorld2;
    }

    //Must be a copy of SphericalHarmonicsL2
    [GenerateHLSL]
    [StructLayout(LayoutKind.Sequential)]
    internal struct SHUpdatePacket
    {
        public float shr0, shr1, shr2, shr3, shr4, shr5, shr6, shr7, shr8;
        public float shg0, shg1, shg2, shg3, shg4, shg5, shg6, shg7, shg8;
        public float shb0, shb1, shb2, shb3, shb4, shb5, shb6, shb7, shb8;
    }
}
