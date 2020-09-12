//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef PHYSICALLYBASEDSKYRENDERER_CS_HLSL
#define PHYSICALLYBASEDSKYRENDERER_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.PhysicallyBasedSkyRenderer+PbrSkyConfig:  static fields
//
#define PBRSKYCONFIG_GROUND_IRRADIANCE_TABLE_SIZE (256)
#define PBRSKYCONFIG_IN_SCATTERED_RADIANCE_TABLE_SIZE_X (128)
#define PBRSKYCONFIG_IN_SCATTERED_RADIANCE_TABLE_SIZE_Y (32)
#define PBRSKYCONFIG_IN_SCATTERED_RADIANCE_TABLE_SIZE_Z (16)
#define PBRSKYCONFIG_IN_SCATTERED_RADIANCE_TABLE_SIZE_W (64)

// Generated from UnityEngine.Rendering.HighDefinition.PhysicallyBasedSkyRenderer+ShaderVariablesPhysicallyBasedSky
// PackingRules = Exact
GLOBAL_CBUFFER_START(ShaderVariablesPhysicallyBasedSky, b2)
    float _PlanetaryRadius;
    float _RcpPlanetaryRadius;
    float _AtmosphericDepth;
    float _RcpAtmosphericDepth;
    float _AtmosphericRadius;
    float _AerosolAnisotropy;
    float _AerosolPhasePartConstant;
    float _Unused;
    float _AirDensityFalloff;
    float _AirScaleHeight;
    float _AerosolDensityFalloff;
    float _AerosolScaleHeight;
    float3 _AirSeaLevelExtinction;
    float _AerosolSeaLevelExtinction;
    float3 _AirSeaLevelScattering;
    float _IntensityMultiplier;
    float3 _AerosolSeaLevelScattering;
    float _ColorSaturation;
    float3 _GroundAlbedo;
    float _AlphaSaturation;
    float3 _PlanetCenterPosition;
    float _AlphaMultiplier;
    float3 _HorizonTint;
    float _HorizonZenithShiftPower;
    float3 _ZenithTint;
    float _HorizonZenithShiftScale;
CBUFFER_END


#endif
