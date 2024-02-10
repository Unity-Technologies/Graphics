using Unity.Mathematics;
using System.Runtime.InteropServices;

namespace UnityEngine.Rendering
{
    [GenerateHLSL]
    internal static class SpeedTreeWindShaderDef
    {
        public const int kMaxWindParamsCount = (int)SpeedTreeWindParamIndex.MaxWindParamsCount;
    };
}
