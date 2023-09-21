//
// This file was automatically generated. Please don't edit by hand. Execute Editor command [ Edit > Rendering > Generate Shader Includes ] instead
//

#ifndef BUILTINDATA_CS_HLSL
#define BUILTINDATA_CS_HLSL
//
// UnityEngine.Rendering.HighDefinition.Builtin+BuiltinData:  static fields
//
#define DEBUGVIEW_BUILTIN_BUILTINDATA_OPACITY (100)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_ALPHA_CLIP_TRESHOLD (101)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_BAKED_DIFFUSE_LIGHTING (102)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_BACK_BAKED_DIFFUSE_LIGHTING (103)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_SHADOWMASK_0 (104)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_SHADOWMASK_1 (105)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_SHADOWMASK_2 (106)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_SHADOWMASK_3 (107)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_EMISSIVE_COLOR (108)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_MOTION_VECTOR (109)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_DISTORTION (110)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_DISTORTION_BLUR (111)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_IS_LIGHTMAP (112)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_RENDERING_LAYERS (113)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_DEPTH_OFFSET (114)
#define DEBUGVIEW_BUILTIN_BUILTINDATA_VT_PACKED_FEEDBACK (115)

// Generated from UnityEngine.Rendering.HighDefinition.Builtin+BuiltinData
// PackingRules = Exact
struct BuiltinData
{
    real opacity;
    real alphaClipTreshold;
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
    uint isLightmap;
    uint renderingLayers;
    float depthOffset;
    #if defined(UNITY_VIRTUAL_TEXTURING)
    real4 vtPackedFeedback;
    #endif
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
        case DEBUGVIEW_BUILTIN_BUILTINDATA_ALPHA_CLIP_TRESHOLD:
            result = builtindata.alphaClipTreshold.xxx;
            break;
        case DEBUGVIEW_BUILTIN_BUILTINDATA_BAKED_DIFFUSE_LIGHTING:
            result = builtindata.bakeDiffuseLighting;
            needLinearToSRGB = true;
            break;
        case DEBUGVIEW_BUILTIN_BUILTINDATA_BACK_BAKED_DIFFUSE_LIGHTING:
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
        case DEBUGVIEW_BUILTIN_BUILTINDATA_IS_LIGHTMAP:
            result = GetIndexColor(builtindata.isLightmap);
            break;
        case DEBUGVIEW_BUILTIN_BUILTINDATA_RENDERING_LAYERS:
            result = GetIndexColor(builtindata.renderingLayers);
            break;
        case DEBUGVIEW_BUILTIN_BUILTINDATA_DEPTH_OFFSET:
            result = builtindata.depthOffset.xxx;
            break;
#if defined(UNITY_VIRTUAL_TEXTURING)
        case DEBUGVIEW_BUILTIN_BUILTINDATA_VT_PACKED_FEEDBACK:
            result = builtindata.vtPackedFeedback.xyz;
            break;
#else
        case DEBUGVIEW_BUILTIN_BUILTINDATA_VT_PACKED_FEEDBACK:
            result = 0;
            break;
#endif
    }
}


#endif
