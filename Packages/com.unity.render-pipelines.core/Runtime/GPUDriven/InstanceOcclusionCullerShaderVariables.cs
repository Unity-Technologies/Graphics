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
        public int _InstanceOcclusionCullerPad0;
    }
}
