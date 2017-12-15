//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef DECAL_CS_HLSL
#define DECAL_CS_HLSL
//
// UnityEngine.Experimental.Rendering.HDPipeline.Decal+DecalSurfaceData:  static fields
//
#define DEBUGVIEW_DECAL_DECALSURFACEDATA_BASE_COLOR (10000)
#define DEBUGVIEW_DECAL_DECALSURFACEDATA_NORMAL_WS (10001)
#define DEBUGVIEW_DECAL_DECALSURFACEDATA_MASK (10002)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Decal+DBufferMaterial:  static fields
//
#define DBUFFERMATERIAL_COUNT (3)

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.Decal+DecalSurfaceData
// PackingRules = Exact
struct DecalSurfaceData
{
    float4 baseColor;
    float4 normalWS;
    float4 mask;
};

//
// Debug functions
//
void GetGeneratedDecalSurfaceDataDebug(uint paramId, DecalSurfaceData decalsurfacedata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_DECAL_DECALSURFACEDATA_BASE_COLOR:
            result = decalsurfacedata.baseColor.xyz;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_DECAL_DECALSURFACEDATA_NORMAL_WS:
            result = decalsurfacedata.normalWS.xyz;
            break;
        case DEBUGVIEW_DECAL_DECALSURFACEDATA_MASK:
            result = decalsurfacedata.mask.xyz;
            break;
    }
}


#endif
