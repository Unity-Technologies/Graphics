namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(needAccessors = false, generateCBuffer = true)]
    unsafe struct ShaderVariablesCapsuleShadows
    {
        public Vector4 _OutputSize;             // w, h, 1/w, 1/h

        public Vector3 _CapsuleLightDir;
        public float _CapsuleLightCosTheta;

        public float _CapsuleLightTanTheta;
        public float _CapsuleShadowRange;
        public uint _CapsulePad0;
        public uint _CapsulePad1;

        public uint _FirstDepthMipOffsetX;
        public uint _FirstDepthMipOffsetY;
        public uint _CapsulesFullResolution;
        public uint _CapsuleTileDebugMode;
    }
}
