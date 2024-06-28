//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef WATERSYSTEMDEF_CS_HLSL
#define WATERSYSTEMDEF_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.WaterAtlasSize:  static fields
//
#define WATERATLASSIZE_ATLAS_SIZE64 (64)
#define WATERATLASSIZE_ATLAS_SIZE128 (128)
#define WATERATLASSIZE_ATLAS_SIZE256 (256)
#define WATERATLASSIZE_ATLAS_SIZE512 (512)
#define WATERATLASSIZE_ATLAS_SIZE1024 (1024)
#define WATERATLASSIZE_ATLAS_SIZE2048 (2048)

//
// UnityEngine.Rendering.HighDefinition.WaterCurrentDebugMode:  static fields
//
#define WATERCURRENTDEBUGMODE_LARGE (0)
#define WATERCURRENTDEBUGMODE_RIPPLES (1)

//
// UnityEngine.Rendering.HighDefinition.WaterDebugMode:  static fields
//
#define WATERDEBUGMODE_NONE (0)
#define WATERDEBUGMODE_WATER_MASK (1)
#define WATERDEBUGMODE_SIMULATION_FOAM_MASK (2)
#define WATERDEBUGMODE_CURRENT (3)
#define WATERDEBUGMODE_DEFORMATION (4)
#define WATERDEBUGMODE_FOAM (5)

//
// UnityEngine.Rendering.HighDefinition.WaterFoamDebugMode:  static fields
//
#define WATERFOAMDEBUGMODE_SURFACE_FOAM (0)
#define WATERFOAMDEBUGMODE_DEEP_FOAM (1)

//
// UnityEngine.Rendering.HighDefinition.WaterMaskDebugMode:  static fields
//
#define WATERMASKDEBUGMODE_RED_CHANNEL (0)
#define WATERMASKDEBUGMODE_GREEN_CHANNEL (1)
#define WATERMASKDEBUGMODE_BLUE_CHANNEL (2)

// Generated from UnityEngine.Rendering.HighDefinition.WaterDecalData
// PackingRules = Exact
struct WaterDecalData
{
    float2 positionXZ;
    float2 forwardXZ;
    float2 regionSize;
    float surfaceFoamDimmer;
    float deepFoamDimmer;
    float amplitude;
    float padding0;
    float padding1;
    float padding2;
    float4 deformFoamScaleOffset;
    float4 maskScaleOffset;
    float4 largeCurrentScaleOffset;
    float4 ripplesCurrentScaleOffset;
};

// Generated from UnityEngine.Rendering.HighDefinition.WaterSectorData
// PackingRules = Exact
struct WaterSectorData
{
    float4 dir0;
    float4 dir1;
};

// Generated from UnityEngine.Rendering.HighDefinition.WaterSurfaceProfile
// PackingRules = Exact
struct WaterSurfaceProfile
{
    float bodyScatteringHeight;
    float maxRefractionDistance;
    uint renderingLayers;
    int cameraUnderWater;
    float3 extinction;
    float extinctionMultiplier;
    float3 albedo;
    float envPerceptualRoughness;
    float3 foamColor;
    float padding1;
    float3 underwaterColor;
    float padding2;
    float3 upDirection;
    float roughnessEndValue;
    float smoothnessFadeStart;
    float smoothnessFadeDistance;
    int disableIOR;
    float tipScatteringHeight;
};


#endif
