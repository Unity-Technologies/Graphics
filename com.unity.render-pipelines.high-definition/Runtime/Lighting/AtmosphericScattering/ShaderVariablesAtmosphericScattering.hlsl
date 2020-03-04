#ifdef SHADER_VARIABLES_INCLUDE_CB
    #include "Packages/com.unity.render-pipelines.high-definition/Runtime/Lighting/AtmosphericScattering/ShaderVariablesAtmosphericScattering.cs.hlsl"
#else
    TEXTURE3D(_VBufferLighting);
    TEXTURECUBE_ARRAY(_SkyTexture);
    bool            _SkyTextureImportanceSamplerReady;
    TEXTURE2D_ARRAY(_SkyTextureIntegrals);
    TEXTURE2D_ARRAY(_SkyTextureMarginals);
    TEXTURE2D_ARRAY(_SkyTextureConditionalMarginals);

    #define _MipFogNear                     _MipFogParameters.x
    #define _MipFogFar                      _MipFogParameters.y
    #define _MipFogMaxMip                   _MipFogParameters.z

    #define _FogColor                       _FogColor
#endif

