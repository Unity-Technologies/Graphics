#ifndef UNITY_GRAPHFUNCTIONS_HD_INCLUDED
#define UNITY_GRAPHFUNCTIONS_HD_INCLUDED

#include "Packages/com.unity.render-pipelines.high-definition/Runtime/Material/NormalBuffer.hlsl"

#define SHADERGRAPH_SAMPLE_SCENE_DEPTH(uv) shadergraph_HDSampleSceneDepth(uv)
#define SHADERGRAPH_LOAD_SCENE_COLOR(uv, lod) shadergraph_HDLoadSceneColor(uv, lod)
#define SHADERGRAPH_SAMPLE_SCENE_COLOR(uv, s, lod) shadergraph_HDSampleSceneColor(uv, s, lod)
#define SHADERGRAPH_BAKED_GI(positionWS, normalWS, uvStaticLightmap, uvDynamicLightmap, applyScaling) shadergraph_HDBakedGI(positionWS, normalWS, uvStaticLightmap, uvDynamicLightmap, applyScaling)
#define SHADERGRAPH_OBJECT_POSITION GetAbsolutePositionWS(UNITY_MATRIX_M._m03_m13_m23)
#define SHADERGRAPH_LOAD_CUSTOM_SCENE_COLOR(uv) shadergraph_HDLoadCustomSceneColor(uv)
#define SHADERGRAPH_SAMPLE_CUSTOM_SCENE_COLOR(uv, s) shadergraph_HDSampleCustomSceneColor(uv, s)
#define SHADERGRAPH_LOAD_CUSTOM_SCENE_DEPTH(uv) shadergraph_HDLoadCustomSceneDepth(uv)
#define SHADERGRAPH_LOAD_SCENE_NORMAL(uv) shadergraph_HDLoadSceneNormal(uv)
#define SHADERGRAPH_LOAD_SCENE_ROUGHNESS(uv) shadergraph_HDLoadSceneRoughness(uv)
#define SHADERGRAPH_LOAD_SCENE_MOTIONVECTOR(uv) shadergraph_HDLoadSceneMotionVector(uv)
#define SHADERGRAPH_LOAD_SCENE_SSAO(uv) shadergraph_HDLoadSceneSSAO(uv)
#define SHADERGRAPH_LOAD_SCENE_SSR(uv) shadergraph_HDLoadSceneSSR(uv)
#define SHADERGRAPH_LOAD_SCENE_VOLUMETRIC(uv) shadergraph_HDLoadSceneVolumetric(uv)


float shadergraph_HDSampleSceneDepth(float2 uv)
{
#if defined(BLIT_PASS)
    return LoadCameraDepth(uv);
#else
#if defined(REQUIRE_DEPTH_TEXTURE)
    return SampleCameraDepth(uv);
#endif
    return 0;
#endif
}

float3 shadergraph_HDLoadSceneColor(float2 uv, float lod)
{
    float mipLevel;
    float width0, widthLod;
    float height0, heightLod;
    float elements;
    float numberOfLevels;
    _ColorPyramidTexture.GetDimensions(0, width0, height0, elements, numberOfLevels);
    _ColorPyramidTexture.GetDimensions(lod, widthLod, heightLod, elements, numberOfLevels);
    float2 screenScale = float2(_ScreenSize.x / width0, _ScreenSize.y / height0);
#if defined(BLIT_PASS)
    float2 lodScale = float2(widthLod / width0, heightLod / height0);
    return LoadCameraColor(uv * lodScale * screenScale, lod) * GetInverseCurrentExposureMultiplier();
#else
#if defined(REQUIRE_OPAQUE_TEXTURE) && defined(_SURFACE_TYPE_TRANSPARENT) && defined(SHADERPASS) && (SHADERPASS != SHADERPASS_LIGHT_TRANSPORT) 
    // We always remove the pre-exposure when we sample the scene color
    float2 lodScale = float2(widthLod, heightLod); 
    return LoadCameraColor(uv * lodScale * screenScale, lod) * GetInverseCurrentExposureMultiplier();
#endif
    return float3(0, 0, 0);
#endif
}

float3 shadergraph_HDSampleSceneColor(float2 uv, SAMPLER(s), float lod)
{
#if defined(BLIT_PASS)
    float width, height, elements;
    _ColorPyramidTexture.GetDimensions(width, height, elements);
    return SampleCameraColor(uv / float2(width, height), s, lod) * GetInverseCurrentExposureMultiplier();
#else
#if (defined(REQUIRE_OPAQUE_TEXTURE) && defined(_SURFACE_TYPE_TRANSPARENT) && defined(SHADERPASS) && (SHADERPASS != SHADERPASS_LIGHT_TRANSPORT)) 
    // We always remove the pre-exposure when we sample the scene color
    return SampleCameraColor(uv, s, lod) * GetInverseCurrentExposureMultiplier();
#endif
    return float3(0, 0, 0);
#endif
}

float4 shadergraph_HDLoadCustomSceneColor(float2 uv)
{
    return LOAD_TEXTURE2D_X(_CustomColorTexture, uv);
}

float4 shadergraph_HDSampleCustomSceneColor(float2 uv, SAMPLER(s))
{
    float width, height, elements;
    _CustomColorTexture.GetDimensions(width, height, elements);                                                                
    return SAMPLE_TEXTURE2D_X(_CustomColorTexture, s, uv / float2(width, height));
}

float shadergraph_HDLoadCustomSceneDepth(float2 uv)
{
    return LOAD_TEXTURE2D_X(_CustomDepthTexture, uv).r;
}

float3 shadergraph_HDLoadSceneNormal(float2 uv)
{
    NormalData normalData;
    DecodeFromNormalBuffer(uv, normalData);
    return normalData.normalWS;
}

float shadergraph_HDLoadSceneRoughness(float2 uv)
{
    NormalData normalData;
    DecodeFromNormalBuffer(uv, normalData);
    return normalData.perceptualRoughness;
}

float2 shadergraph_HDLoadSceneMotionVector(float2 uv)
{
    float2 velocity;
    DecodeMotionVector(LOAD_TEXTURE2D_X(_CameraMotionVectorsTexture, uv), velocity);
    return velocity;
}

float4 shadergraph_HDLoadSceneSSAO(float2 uv)
{
    return LOAD_TEXTURE2D_X(_AmbientOcclusionTexture, uv);
}

float4 shadergraph_HDLoadSceneSSR(float2 uv)
{
   return LOAD_TEXTURE2D_X(_SsrLightingTexture, uv);
}

float4 shadergraph_HDLoadSceneVolumetric(float2 uv)
{
    return 0;
}

float3 shadergraph_HDBakedGI(float3 positionWS, float3 normalWS, float2 uvStaticLightmap, float2 uvDynamicLightmap, bool applyScaling)
{
    float3 positionRWS = GetCameraRelativePositionWS(positionWS);
    return SampleBakedGI(positionRWS, normalWS, uvStaticLightmap, uvDynamicLightmap);
}

// Always include Shader Graph version
// Always include last to avoid double macros
#include "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl" 

#endif // UNITY_GRAPHFUNCTIONS_HD_INCLUDED
