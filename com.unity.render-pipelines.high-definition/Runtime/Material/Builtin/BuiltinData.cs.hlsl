//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef BUILTINDATA_CS_HLSL
#define BUILTINDATA_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.Builtin+BuiltinData:  static fields
//
#define DEBUGVIEW_BUILTIN_BUILTINDATA_OPACITY (100)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_BAKE_DIFFUSE_LIGHTING (101)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_BACK_BAKE_DIFFUSE_LIGHTING (102)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_SHADOWMASK_0 (103)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_SHADOWMASK_1 (104)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_SHADOWMASK_2 (105)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_SHADOWMASK_3 (106)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_EMISSIVE_COLOR (107)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_MOTION_VECTOR (108)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_DISTORTION (109)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_DISTORTION_BLUR (110)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_RENDERING_LAYERS (111)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_DEPTH_OFFSET (112)

// Generated from UnityEngine.Rendering.HighDefinition.Builtin+BuiltinData
// PackingRules = Exact
struct BuiltinData
{
    real opacity;
    real3 bakeDiffuseLighting;
    real3 backBakeDiffuseLighting;
    real shadowMask0;
    real shadowMask1;
    real shadowMask2;
    real shadowMask3;
    real3 emissiveColor;
    real2 motionVector;
    real2 distortion;
    real distortionBlur;
    uint renderingLayers;
    float depthOffset;
};

// Generated from UnityEngine.Rendering.HighDefinition.Builtin+LightTransportData
// PackingRules = Exact
struct LightTransportData
{
    real3 diffuseColor;
    real3 emissiveColor;
};

//
// Debug functions
//
void GetGeneratedBuiltinDataDebug(uint paramId, BuiltinData builtindata, inout float3 result, inout bool needLinearToSRGB)
{
    switch (paramId)
    {
        case DEBUGVIEW_BUILTIN_BUILTINDATA_OPACITY:
            result = builtindata.opacity.xxx;
            break;
        case DEBUGVIEW_BUILTIN_BUILTINDATA_BAKE_DIFFUSE_LIGHTING:
            result = builtindata.bakeDiffuseLighting;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_BUILTIN_BUILTINDATA_BACK_BAKE_DIFFUSE_LIGHTING:
            result = builtindata.backBakeDiffuseLighting;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_BUILTIN_BUILTINDATA_SHADOWMASK_0:
            result = builtindata.shadowMask0.xxx;
            break;
        case DEBUGVIEW_BUILTIN_BUILTINDATA_SHADOWMASK_1:
            result = builtindata.shadowMask1.xxx;
            break;
        case DEBUGVIEW_BUILTIN_BUILTINDATA_SHADOWMASK_2:
            result = builtindata.shadowMask2.xxx;
            break;
        case DEBUGVIEW_BUILTIN_BUILTINDATA_SHADOWMASK_3:
            result = builtindata.shadowMask3.xxx;
            break;
        case DEBUGVIEW_BUILTIN_BUILTINDATA_EMISSIVE_COLOR:
            result = builtindata.emissiveColor;
            break;
        case DEBUGVIEW_BUILTIN_BUILTINDATA_MOTION_VECTOR:
            result = float3(builtindata.motionVector, 0.0);
            break;
        case DEBUGVIEW_BUILTIN_BUILTINDATA_DISTORTION:
            result = float3(builtindata.distortion, 0.0);
            break;
        case DEBUGVIEW_BUILTIN_BUILTINDATA_DISTORTION_BLUR:
            result = builtindata.distortionBlur.xxx;
            break;
        case DEBUGVIEW_BUILTIN_BUILTINDATA_RENDERING_LAYERS:
            result = GetIndexColor(builtindata.renderingLayers);
            break;
        case DEBUGVIEW_BUILTIN_BUILTINDATA_DEPTH_OFFSET:
            result = builtindata.depthOffset.xxx;
            break;
    }
}


#endif
