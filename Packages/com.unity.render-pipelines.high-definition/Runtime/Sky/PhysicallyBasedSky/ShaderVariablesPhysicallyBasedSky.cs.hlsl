//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef SHADERVARIABLESPHYSICALLYBASEDSKY_CS_HLSL
#define SHADERVARIABLESPHYSICALLYBASEDSKY_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.PbrSkyConfig:  static fields
//
#define PBRSKYCONFIG_GROUND_IRRADIANCE_TABLE_SIZE (256)
#define PBRSKYCONFIG_IN_SCATTERED_RADIANCE_TABLE_SIZE_X (128)
#define PBRSKYCONFIG_IN_SCATTERED_RADIANCE_TABLE_SIZE_Y (32)
#define PBRSKYCONFIG_IN_SCATTERED_RADIANCE_TABLE_SIZE_Z (16)
#define PBRSKYCONFIG_IN_SCATTERED_RADIANCE_TABLE_SIZE_W (64)

// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesPhysicallyBasedSky
// PackingRules = Exact
GLOBAL_CBUFFER_START(ShaderVariablesPhysicallyBasedSky, b2)
    float _AtmosphericRadius;
    float _AerosolAnisotropy;
    float _AerosolPhasePartConstant;
    float _AerosolSeaLevelExtinction;
    float _AirDensityFalloff;
    float _AirScaleHeight;
    float _AerosolDensityFalloff;
    float _AerosolScaleHeight;
    float4 _AirSeaLevelExtinction;
    float4 _AirSeaLevelScattering;
    float4 _AerosolSeaLevelScattering;
    float4 _GroundAlbedo_PlanetRadius;
    float4 _HorizonTint;
    float4 _ZenithTint;
    float _IntensityMultiplier;
    float _ColorSaturation;
    float _AlphaSaturation;
    float _AlphaMultiplier;
    float _HorizonZenithShiftPower;
    float _HorizonZenithShiftScale;
    uint _CelestialLightCount;
    uint _CelestialBodyCount;
    float _AtmosphericDepth;
    float _RcpAtmosphericDepth;
    float _PaddingPBS1;
    float _PaddingPBS2;
CBUFFER_END


#endif
