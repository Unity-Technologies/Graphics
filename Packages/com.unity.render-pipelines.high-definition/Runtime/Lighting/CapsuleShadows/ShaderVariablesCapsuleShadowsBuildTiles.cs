namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesCapsuleShadowsBuildTiles
    {
        public Vector4 _CapsuleUpscaledSize;        // w, h, 1/w, 1/h

        public uint _CapsuleCoarseTileSizeInFineTilesX;
        public uint _CapsuleCoarseTileSizeInFineTilesY;
        public uint _CapsuleRenderSizeInCoarseTilesX;
        public uint _CapsuleRenderSizeInCoarseTilesY;

        public uint _CapsuleOccluderCount;
        public uint _CapsuleCasterCount;
        public uint _CapsuleShadowFlags;
        public uint _CapsuleShadowsViewCount;

        public uint _CapsuleDepthMipOffsetX;
        public uint _CapsuleDepthMipOffsetY;
        public float _CapsuleIndirectRangeFactor;
        public uint _CapsuleBuildTilesPad0;
    }
}
