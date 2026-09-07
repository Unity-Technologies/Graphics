#if !UNITY_WEBGL_RENDERER_ONLY
using System;

namespace UnityEngine.Rendering
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    internal unsafe struct InstanceOcclusionCullerShaderVariables
    {
        public uint _DrawInfoAllocIndex;
        public uint _DrawInfoCount;
        public uint _InstanceInfoAllocIndex;
        public uint _InstanceInfoCount;
        public int _BoundingSphereInstanceDataAddress;
        public int _DebugCounterIndex;
        public int _InstanceMultiplierShift;
        public int _LocalBoundsInstanceDataAddress;
        public int _ObjectToWorldInstanceDataAddress;
        public int _InstanceOcclusionCullerPad0;
        public int _InstanceOcclusionCullerPad1;
        public int _InstanceOcclusionCullerPad2;
    }
}

#endif // !UNITY_WEBGL_RENDERER_ONLY
