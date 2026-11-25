namespace UnityEngine.Rendering
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    internal struct SurfaceCacheEmissiveTriangleRemovalConstants
    {
        public uint _RemovedInstanceIdCount;
        public uint _EmissiveTriangleRemovalPad0;
        public uint _EmissiveTriangleRemovalPad1;
        public uint _EmissiveTriangleRemovalPad2;
    }
}
