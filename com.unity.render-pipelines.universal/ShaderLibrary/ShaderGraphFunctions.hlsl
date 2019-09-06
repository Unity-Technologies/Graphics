#ifndef UNITY_GRAPHFUNCTIONS_LW_INCLUDED
#define UNITY_GRAPHFUNCTIONS_LW_INCLUDED

#define SHADERGRAPH_SAMPLE_SCENE_DEPTH(uv) shadergraph_LWSampleSceneDepth(uv)
#define SHADERGRAPH_SAMPLE_SCENE_COLOR(uv) shadergraph_LWSampleSceneColor(uv)
#define SHADERGRAPH_BAKED_GI(positionWS, normalWS, uvStaticLightmap, uvDynamicLightmap, applyScaling) shadergraph_LWBakedGI(positionWS, normalWS, uvStaticLightmap, uvDynamicLightmap, applyScaling)
#define SHADERGRAPH_REFLECTION_PROBE(viewDir, normalOS, lod) shadergraph_LWReflectionProbe(viewDir, normalOS, lod)
#define SHADERGRAPH_FOG(position, color, density) shadergraph_LWFog(position, color, density)
#define SHADERGRAPH_AMBIENT_SKY unity_AmbientSky
#define SHADERGRAPH_AMBIENT_EQUATOR unity_AmbientEquator
#define SHADERGRAPH_AMBIENT_GROUND unity_AmbientGround

#if defined(REQUIRE_DEPTH_TEXTURE)
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    TEXTURE2D_ARRAY(_CameraDepthTexture);
#else
    TEXTURE2D(_CameraDepthTexture);
#endif
    SAMPLER(sampler_CameraDepthTexture);
#endif // REQUIRE_DEPTH_TEXTURE

#if defined(REQUIRE_OPAQUE_TEXTURE)
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    TEXTURE2D_ARRAY(_CameraOpaqueTexture);
#else
    TEXTURE2D(_CameraOpaqueTexture);
#endif
    SAMPLER(sampler_CameraOpaqueTexture);
#endif // REQUIRE_OPAQUE_TEXTURE

float shadergraph_LWSampleSceneDepth(float2 uv)
{
#if defined(REQUIRE_DEPTH_TEXTURE)
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    float rawDepth = SAMPLE_TEXTURE2D_ARRAY(_CameraDepthTexture, sampler_CameraDepthTexture, uv, unity_StereoEyeIndex).r;
#else
    float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv);
#endif
	return rawDepth;
#endif // REQUIRE_DEPTH_TEXTURE
    return 0;
}

float3 shadergraph_LWSampleSceneColor(float2 uv)
{
#if defined(REQUIRE_OPAQUE_TEXTURE)
#if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    return SAMPLE_TEXTURE2D_ARRAY(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv, unity_StereoEyeIndex);
#else
    return SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, uv);
#endif
#endif // REQUIRE_DEPTH_TEXTURE
    return 0;
}

float3 shadergraph_LWBakedGI(float3 positionWS, float3 normalWS, float2 uvStaticLightmap, float2 uvDynamicLightmap, bool applyScaling)
{
#ifdef LIGHTMAP_ON
    if (applyScaling)
        uvStaticLightmap = uvStaticLightmap * unity_LightmapST.xy + unity_LightmapST.zw;

    return SampleLightmap(uvStaticLightmap, normalWS);
#else
    return SampleSH(normalWS);
#endif
}

float3 shadergraph_LWReflectionProbe(float3 viewDir, float3 normalOS, float lod)
{
    float3 reflectVec = reflect(-viewDir, normalOS);
    return DecodeHDREnvironment(SAMPLE_TEXTURECUBE_LOD(unity_SpecCube0, samplerunity_SpecCube0, reflectVec, lod), unity_SpecCube0_HDR);
}

void shadergraph_LWFog(float3 position, out float4 color, out float density)
{
    color = unity_FogColor;
    density = ComputeFogFactor(TransformObjectToHClip(position).z);
}

// This function assumes the bitangent flip is encoded in tangentWS.w
float3x3 BuildTangentToWorld(float4 tangentWS, float3 normalWS)
{
    // tangentWS must not be normalized (mikkts requirement)

    // Normalize normalWS vector but keep the renormFactor to apply it to bitangent and tangent
    float3 unnormalizedNormalWS = normalWS;
    float renormFactor = 1.0 / length(unnormalizedNormalWS);

    // bitangent on the fly option in xnormal to reduce vertex shader outputs.
    // this is the mikktspace transformation (must use unnormalized attributes)
    float3x3 tangentToWorld = CreateTangentToWorld(unnormalizedNormalWS, tangentWS.xyz, tangentWS.w > 0.0 ? 1.0 : -1.0);

    // surface gradient based formulation requires a unit length initial normal. We can maintain compliance with mikkts
    // by uniformly scaling all 3 vectors since normalization of the perturbed normal will cancel it.
    tangentToWorld[0] = tangentToWorld[0] * renormFactor;
    tangentToWorld[1] = tangentToWorld[1] * renormFactor;
    tangentToWorld[2] = tangentToWorld[2] * renormFactor;		// normalizes the interpolated vertex normal

    return tangentToWorld;
}

// Always include Shader Graph version
// Always include last to avoid double macros
#include "Packages/com.unity.shadergraph/ShaderGraphLibrary/Functions.hlsl" 

#endif // UNITY_GRAPHFUNCTIONS_LW_INCLUDED
