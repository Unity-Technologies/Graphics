namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesCapsuleShadowsRender
    {
        public Vector4 _CapsuleUpscaledSize;        // w, h, 1/w, 1/h

        public uint _CapsuleCasterCount;
        public uint _CapsuleOccluderCount;
        public uint _CapsuleShadowFlags;
        public float _CapsuleIndirectRangeFactor;

        public uint _CapsuleUpscaledSizeInTilesX;
        public uint _CapsuleUpscaledSizeInTilesY;
        public uint _CapsuleCoarseTileSizeInFineTilesX;
        public uint _CapsuleCoarseTileSizeInFineTilesY;

        public uint _CapsuleRenderSizeInTilesX;
        public uint _CapsuleRenderSizeInTilesY;
        public uint _CapsuleRenderSizeInCoarseTilesX;
        public uint _CapsuleRenderSizeInCoarseTilesY;

        public uint _CapsuleRenderSizeX;
        public uint _CapsuleRenderSizeY;
        public uint _CapsuleTileDebugMode;
        public uint _CapsuleDebugCasterIndex;

        public uint _CapsuleDepthMipOffsetX;
        public uint _CapsuleDepthMipOffsetY;
        public float _CapsuleIndirectCosAngle;
        public uint _CapsuleShadowsRenderPad1;

        public Vector3 _CapsuleShadowsLUTCoordScale;
        public uint _CapsuleShadowsRenderPad2;
        public Vector3 _CapsuleShadowsLUTCoordOffset;
        public uint _CapsuleShadowsRenderPad3;
    }
}
