//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef DECAL_CS_HLSL
#define DECAL_CS_HLSL
//
// UnityEngine.Experimental.Rendering.HDPipeline.Decal+SurfaceData:  static fields
//
#define DEBUGVIEW_DECAL_SURFACEDATA_BASE_COLOR (2000)
#define DEBUGVIEW_DECAL_SURFACEDATA_NORMAL_WS (2001)

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.Decal+SurfaceData
// PackingRules = Exact
struct SurfaceData
{
    float4 baseColor;
    float4 normalWS;
};

//
// Debug functions
//
void GetGeneratedSurfaceDataDebug(uint paramId, SurfaceData surfacedata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_DECAL_SURFACEDATA_BASE_COLOR:
            result = surfacedata.baseColor.xyz;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_DECAL_SURFACEDATA_NORMAL_WS:
            result = surfacedata.normalWS.xyz;
            break;
    }
}


#endif
