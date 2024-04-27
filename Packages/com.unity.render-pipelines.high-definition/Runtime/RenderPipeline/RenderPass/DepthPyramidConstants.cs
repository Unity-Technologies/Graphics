namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    internal unsafe struct DepthPyramidConstants
    {
        public uint _MinDstCount;
        public uint _CbDstCount;
        public uint _DepthPyramidPad0;
        public uint _DepthPyramidPad1;

        public Vector2Int _SrcOffset;
        public Vector2Int _SrcLimit;

        public Vector2Int _DstSize0;
        public Vector2Int _DstSize1;
        public Vector2Int _DstSize2;
        public Vector2Int _DstSize3;

        public Vector2Int _MinDstOffset0;
        public Vector2Int _MinDstOffset1;
        public Vector2Int _MinDstOffset2;
        public Vector2Int _MinDstOffset3;

        public Vector2Int _CbDstOffset0;
        public Vector2Int _CbDstOffset1;
    }
}
