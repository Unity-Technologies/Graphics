using System;

namespace UnityEngine.Rendering
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    internal unsafe struct OcclusionCullingDebugShaderVariables
    {
        public Vector4 _DepthSizeInOccluderPixels;
        public Vector4 _DebugPyramidSize;

        [HLSLArray(OccluderContext.k_MaxOccluderMips, typeof(ShaderGenUInt4))]
        public fixed uint _OccluderMipBounds[OccluderContext.k_MaxOccluderMips * 4];
    }
}
