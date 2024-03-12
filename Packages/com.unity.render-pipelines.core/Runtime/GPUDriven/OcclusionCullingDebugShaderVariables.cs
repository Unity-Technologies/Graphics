using System;

namespace UnityEngine.Rendering
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    internal unsafe struct OcclusionCullingDebugShaderVariables
    {
        public Vector4 _DepthSizeInOccluderPixels;

        [HLSLArray(OccluderContext.k_MaxOccluderMips, typeof(ShaderGenUInt4))]
        public fixed uint _OccluderMipBounds[OccluderContext.k_MaxOccluderMips * 4];

        public uint _OccluderMipLayoutSizeX;
        public uint _OccluderMipLayoutSizeY;
        public uint _OcclusionCullingDebugPad0;
        public uint _OcclusionCullingDebugPad1;
    }
}
