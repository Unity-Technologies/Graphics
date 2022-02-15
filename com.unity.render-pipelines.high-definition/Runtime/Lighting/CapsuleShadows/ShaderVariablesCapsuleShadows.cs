namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesCapsuleShadows
    {
        public Vector4 _CapsuleRenderSize;      // w, h, 1/w, 1/h

        public Vector3 _CapsuleLightDir;
        public float _CapsuleLightCosTheta;

        public float _CapsuleLightTanTheta;
        public float _CapsuleShadowRange;
        public uint _CapsulePad0;
        public uint _CapsulePad1;

        public Vector4 _CapsuleUpscaledSize;    // w, h, 1/w, 1/h
        public Vector4 _DepthPyramidSize;       // w, h, 1/w, 1/h
        public uint _FirstDepthMipOffsetX;
        public uint _FirstDepthMipOffsetY;
        public uint _CapsuleTileDebugMode;
        public uint _CapsulePad2;
    }
}
