namespace UnityEngine.Rendering.HighDefinition
{
    [GenerateHLSL(needAccessors = false, omitStructDeclaration = true)]
    struct ShaderVariablesAtmosphericScattering
    {
        // Common
        public int     _FogEnabled;
        public int     _PBRFogEnabled;
        public float   _MaxFogDistance;
        public float   _FogColorMode;
        public Vector4 _FogColor; // color in rgb
        public Vector4 _MipFogParameters;

        // Volumetrics
        public float  _VBufferLastSliceDist;       // The distance to the middle of the last slice
        public int _EnableVolumetricFog;           // bool...
    }
}

