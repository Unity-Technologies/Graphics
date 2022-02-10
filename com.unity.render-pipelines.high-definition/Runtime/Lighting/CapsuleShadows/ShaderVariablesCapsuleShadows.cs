namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesCapsuleShadows
    {
        public Vector4 _OutputSize;             // w, h, 1/w, 1/h

        public uint _FirstDepthMipOffsetX;
        public uint _FirstDepthMipOffsetY;
        public uint _CapsulesFullResolution;
        public uint _CapsuleShadowsPad0;
    }
}
