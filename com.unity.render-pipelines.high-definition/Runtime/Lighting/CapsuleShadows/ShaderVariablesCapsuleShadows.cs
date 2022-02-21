namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesCapsuleShadows
    {
        public Vector4 _CapsuleUpscaledSize;        // w, h, 1/w, 1/h
        public Vector4 _CapsuleRenderTextureSize;   // w, h, 1/w, 1/h
        public Vector4 _DepthPyramidTextureSize;    // w, h, 1/w, 1/h

        public uint _FirstDepthMipOffsetX;
        public uint _FirstDepthMipOffsetY;
        public uint _CapsuleCasterCount;
        public uint _CapsulePad0;

        public uint _CapsuleDebugCasterIndex;
        public uint _CapsuleTileDebugMode;
        public uint _CapsulePad1;
        public uint _CapsulePad2;
    }
}
