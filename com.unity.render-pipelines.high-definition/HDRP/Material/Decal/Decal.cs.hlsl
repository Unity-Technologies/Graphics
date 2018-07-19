//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef DECAL_CS_HLSL
#define DECAL_CS_HLSL
//
// UnityEngine.Experimental.Rendering.HDPipeline.Decal+DecalSurfaceData:  static fields
//
#define DEBUGVIEW_DECAL_DECALSURFACEDATA_BASE_COLOR (200)
#define DEBUGVIEW_DECAL_DECALSURFACEDATA_NORMAL (201)
#define DEBUGVIEW_DECAL_DECALSURFACEDATA_MASK (202)
#define DEBUGVIEW_DECAL_DECALSURFACEDATA_HTILE_MASK (203)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Decal+DBufferMaterial:  static fields
//
#define DBUFFERMATERIAL_COUNT (3)

//
// UnityEngine.Experimental.Rendering.HDPipeline.Decal+DBufferHTileBit:  static fields
//
#define DBUFFERHTILEBIT_DIFFUSE (1)
#define DBUFFERHTILEBIT_NORMAL (2)
#define DBUFFERHTILEBIT_MASK (4)

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.Decal+DecalSurfaceData
// PackingRules = Exact
struct DecalSurfaceData
{
    float4 baseColor;
    float4 normalWS;
    float4 mask;
    uint HTileMask;
};

// Generated from UnityEngine.Experimental.Rendering.HDPipeline.DecalData
// PackingRules = Exact
struct DecalData
{
    float4x4 worldToDecal;
    float4x4 normalToWorld;
    float4 diffuseScaleBias;
    float4 normalScaleBias;
    float4 maskScaleBias;
    float4 baseColor;
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
        case DEBUGVIEW_DECAL_DECALSURFACEDATA_NORMAL:
            result = decalsurfacedata.normalWS.xyz;
            break;
        case DEBUGVIEW_DECAL_DECALSURFACEDATA_MASK:
            result = decalsurfacedata.mask.xyz;
            break;
        case DEBUGVIEW_DECAL_DECALSURFACEDATA_HTILE_MASK:
            result = GetIndexColor(decalsurfacedata.HTileMask);
            break;
    }
}


#endif
