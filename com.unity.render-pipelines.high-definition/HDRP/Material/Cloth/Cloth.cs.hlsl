//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef SHADERNAME_CS_HLSL
#define SHADERNAME_CS_HLSL
//
// UnityEngine.Experimental.Rendering.HDPipeline.ShaderName+SurfaceData:  static fields
//
#define DEBUGVIEW_SHADERNAME_SURFACEDATA_BASE_COLOR (10000)
#define DEBUGVIEW_SHADERNAME_SURFACEDATA_NORMAL (10001)

//
// UnityEngine.Experimental.Rendering.HDPipeline.ShaderName+BSDFData:  static fields
//
#define DEBUGVIEW_SHADERNAME_BSDFDATA_DIFFUSE_COLOR (10100)
#define DEBUGVIEW_SHADERNAME_BSDFDATA_NORMAL (10101)

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.ShaderName+SurfaceData
// PackingRules = Exact
struct SurfaceData
{
    float3 baseColor;
    float3 normalWS;
};

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.ShaderName+BSDFData
// PackingRules = Exact
struct BSDFData
{
    float3 diffuseColor;
    float3 normalWS;
};

//
// Debug functions
//
void GetGeneratedSurfaceDataDebug(uint paramId, SurfaceData surfacedata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_SHADERNAME_SURFACEDATA_BASE_COLOR:
            result = surfacedata.baseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_SHADERNAME_SURFACEDATA_NORMAL:
            result = surfacedata.normalWS * 0.5 + 0.5;
            break;
    }
}

//
// Debug functions
//
void GetGeneratedBSDFDataDebug(uint paramId, BSDFData bsdfdata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_SHADERNAME_BSDFDATA_DIFFUSE_COLOR:
            result = bsdfdata.diffuseColor;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_SHADERNAME_BSDFDATA_NORMAL:
            result = bsdfdata.normalWS * 0.5 + 0.5;
            break;
    }
}


#endif
