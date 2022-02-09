namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesCapsuleShadows
    {
        public Vector2 _SizeRcp;
        public uint _FirstDepthMipOffsetX;
        public uint _FirstDepthMipOffsetY;
    }
}
