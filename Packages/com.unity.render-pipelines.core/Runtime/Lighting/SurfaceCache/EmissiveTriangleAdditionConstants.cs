namespace UnityEngine.Rendering
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    internal struct SurfaceCacheEmissiveTriangleAdditionConstants
    {
        public uint _SubmeshInstanceId;
        public uint _SubmeshTriangleCount;
        public uint _EmissiveTriangleAdditionPad0;
        public uint _EmissiveTriangleAdditionPad1;
    }
}
