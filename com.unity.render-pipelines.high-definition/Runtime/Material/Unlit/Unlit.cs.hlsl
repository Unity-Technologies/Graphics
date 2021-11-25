//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef UNLIT_CS_HLSL
#define UNLIT_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.Unlit+SurfaceData:  static fields
//
#define DEBUGVIEW_UNLIT_SURFACEDATA_COLOR (300)
#define DEBUGVIEW_UNLIT_SURFACEDATA_NORMAL (301)
#define DEBUGVIEW_UNLIT_SURFACEDATA_NORMAL_VIEW_SPACE (302)
#define DEBUGVIEW_UNLIT_SURFACEDATA_SHADOW_TINT (303)

//
// UnityEngine.Rendering.HighDefinition.Unlit+BSDFData:  static fields
//
#define DEBUGVIEW_UNLIT_BSDFDATA_COLOR (350)

// Generated from UnityEngine.Rendering.HighDefinition.Unlit+SurfaceData
// PackingRules = Exact
struct SurfaceData
{
    float3 color;
    float3 normalWS;
    #if defined(_ENABLE_SHADOW_MATTE) && (SHADERPASS == SHADERPASS_PATH_TRACING)
    float4 shadowTint;
    #endif
};

// Generated from UnityEngine.Rendering.HighDefinition.Unlit+BSDFData
// PackingRules = Exact
struct BSDFData
{
    float3 color;
};

//
// Debug functions
//
void GetGeneratedSurfaceDataDebug(uint paramId, SurfaceData surfacedata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_UNLIT_SURFACEDATA_COLOR:
            result = surfacedata.color;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_UNLIT_SURFACEDATA_NORMAL:
            result = surfacedata.normalWS * 0.5 + 0.5;
            break;
        case DEBUGVIEW_UNLIT_SURFACEDATA_NORMAL_VIEW_SPACE:
            result = surfacedata.normalWS * 0.5 + 0.5;
            break;
#if defined(_ENABLE_SHADOW_MATTE) && (SHADERPASS == SHADERPASS_PATH_TRACING)
        case DEBUGVIEW_UNLIT_SURFACEDATA_SHADOW_TINT:
            result = surfacedata.shadowTint.xyz;
            needLinearToSRGB = true;
            break;
#else
        case DEBUGVIEW_UNLIT_SURFACEDATA_SHADOW_TINT:
            result = 0;
            break;
#endif
    }
}

//
// Debug functions
//
void GetGeneratedBSDFDataDebug(uint paramId, BSDFData bsdfdata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_UNLIT_BSDFDATA_COLOR:
            result = bsdfdata.color;
            needLinearToSRGB = true;
            break;
    }
}


#endif
