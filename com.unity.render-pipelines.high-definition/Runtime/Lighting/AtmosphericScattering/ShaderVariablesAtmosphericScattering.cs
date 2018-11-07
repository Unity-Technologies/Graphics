
namespace UnityEngine.Experimental.Rendering.HDPipeline
{
    [GenerateHLSL(needAccessors = false, omitStructDeclaration = true)]
    public struct ShaderVariablesAtmosphericScattering
    {
        public Vector4  _FogColorDensity; // color in rgb, density in alpha
        public Vector4  _MipFogParameters;
        // Linear fog
        public Vector4 _LinearFogParameters;
        // Exp fog
        public Vector4  _ExpFogParameters;

        public int     _AtmosphericScatteringType;
        // Common
        public float   _FogColorMode;

        public float   _SkyTextureMipCount;
    }
}

