//
// This file was automatically generated from Assets/ScriptableRenderLoop/fptl/LightDefinitions.cs.  Please don't edit by hand.
//

//
// LightDefinitions:  static fields
//
#define MAX_NR_LIGHTS_PER_CAMERA (1024)
#define VIEWPORT_SCALE_Z (1)
#define IS_CIRCULAR_SPOT_SHAPE (1)
#define HAS_COOKIE_TEXTURE (2)
#define IS_BOX_PROJECTED (4)
#define HAS_SHADOW (8)
#define MAX_TYPES (3)
#define SPOT_LIGHT (0)
#define SPHERE_LIGHT (1)
#define BOX_LIGHT (2)
#define DIRECTIONAL_LIGHT (3)
#define NR_LIGHT_MODELS (2)
#define DIRECT_LIGHT (0)
#define REFLECTION_LIGHT (1)

// Generated from SFiniteLightData
// PackingRules = Exact
struct SFiniteLightData
{
    float fPenumbra;
    int flags;
    uint uLightType;
    uint uLightModel;
    float3 vLpos;
    float fLightIntensity;
    float3 vLaxisX;
    float fRecipRange;
    float3 vLaxisY;
    float fSphRadiusSq;
    float3 vLaxisZ;
    float cotan;
    float3 vCol;
    int iSliceIndex;
    float3 vBoxInnerDist;
    float fDecodeExp;
    float3 vBoxInvRange;
    uint uShadowLightIndex;
    float3 vLocalCubeCapturePoint;
    float fProbeBlendDistance;
};

// Generated from SFiniteLightBound
// PackingRules = Exact
struct SFiniteLightBound
{
    float3 vBoxAxisX;
    float3 vBoxAxisY;
    float3 vBoxAxisZ;
    float3 vCen;
    float2 vScaleXY;
    float fRadius;
};

// Generated from DirectionalLight
// PackingRules = Exact
struct DirectionalLight
{
    float3 vCol;
    float fLightIntensity;
    float3 vLaxisX;
    uint uShadowLightIndex;
    float3 vLaxisY;
    float fPad0;
    float3 vLaxisZ;
    float fPad1;
};

//
// Accessors for SFiniteLightData
//
float GetFPenumbra(SFiniteLightData value)
{
    return value.fPenumbra;
}
int GetFlags(SFiniteLightData value)
{
    return value.flags;
}
uint GetULightType(SFiniteLightData value)
{
    return value.uLightType;
}
uint GetULightModel(SFiniteLightData value)
{
    return value.uLightModel;
}
float3 GetVLpos(SFiniteLightData value)
{
    return value.vLpos;
}
float GetFLightIntensity(SFiniteLightData value)
{
    return value.fLightIntensity;
}
float3 GetVLaxisX(SFiniteLightData value)
{
    return value.vLaxisX;
}
float GetFRecipRange(SFiniteLightData value)
{
    return value.fRecipRange;
}
float3 GetVLaxisY(SFiniteLightData value)
{
    return value.vLaxisY;
}
float GetFSphRadiusSq(SFiniteLightData value)
{
    return value.fSphRadiusSq;
}
float3 GetVLaxisZ(SFiniteLightData value)
{
    return value.vLaxisZ;
}
float GetCotan(SFiniteLightData value)
{
    return value.cotan;
}
float3 GetVCol(SFiniteLightData value)
{
    return value.vCol;
}
int GetISliceIndex(SFiniteLightData value)
{
    return value.iSliceIndex;
}
float3 GetVBoxInnerDist(SFiniteLightData value)
{
    return value.vBoxInnerDist;
}
float GetFDecodeExp(SFiniteLightData value)
{
    return value.fDecodeExp;
}
float3 GetVBoxInvRange(SFiniteLightData value)
{
    return value.vBoxInvRange;
}
uint GetUShadowLightIndex(SFiniteLightData value)
{
    return value.uShadowLightIndex;
}
float3 GetVLocalCubeCapturePoint(SFiniteLightData value)
{
    return value.vLocalCubeCapturePoint;
}
float GetFProbeBlendDistance(SFiniteLightData value)
{
    return value.fProbeBlendDistance;
}

//
// Accessors for SFiniteLightBound
//
float3 GetVBoxAxisX(SFiniteLightBound value)
{
    return value.vBoxAxisX;
}
float3 GetVBoxAxisY(SFiniteLightBound value)
{
    return value.vBoxAxisY;
}
float3 GetVBoxAxisZ(SFiniteLightBound value)
{
    return value.vBoxAxisZ;
}
float3 GetVCen(SFiniteLightBound value)
{
    return value.vCen;
}
float2 GetVScaleXY(SFiniteLightBound value)
{
    return value.vScaleXY;
}
float GetFRadius(SFiniteLightBound value)
{
    return value.fRadius;
}

//
// Accessors for DirectionalLight
//
float3 GetVCol(DirectionalLight value)
{
    return value.vCol;
}
float GetFLightIntensity(DirectionalLight value)
{
    return value.fLightIntensity;
}
float3 GetVLaxisX(DirectionalLight value)
{
    return value.vLaxisX;
}
uint GetUShadowLightIndex(DirectionalLight value)
{
    return value.uShadowLightIndex;
}
float3 GetVLaxisY(DirectionalLight value)
{
    return value.vLaxisY;
}
float GetFPad0(DirectionalLight value)
{
    return value.fPad0;
}
float3 GetVLaxisZ(DirectionalLight value)
{
    return value.vLaxisZ;
}
float GetFPad1(DirectionalLight value)
{
    return value.fPad1;
}
