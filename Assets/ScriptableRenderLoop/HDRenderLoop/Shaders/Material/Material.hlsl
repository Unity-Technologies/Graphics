#ifndef UNITY_MATERIAL_INCLUDED
#define UNITY_MATERIAL_INCLUDED

#include "Packing.hlsl"
#include "BSDF.hlsl"
#include "Debug.hlsl"
#include "GeometricTools.hlsl"
#include "CommonMaterial.hlsl"
#include "EntityLighting.hlsl"

//-----------------------------------------------------------------------------
// common Encode/Decode functions
//-----------------------------------------------------------------------------

// Encode/Decode velocity in a buffer (either forward of deferred)
// Design note: We assume that VelocityVector fit into a single buffer (i.e not spread on several buffer)
void EncodeVelocity(float2 velocity, out float4 outBuffer)
{
    // RT - 16:16 float
    outBuffer = float4(velocity.xy, 0.0, 0.0);
}

float2 DecodeVelocity(float4 inBuffer)
{
    return float2(inBuffer.xy);
}

// Encode/Decode into GBuffer - This is share so others material can use it.
// Design note: We assume that BakeDiffuseLighting and emissive fit into a single buffer (i.e not spread on several buffer)
void EncodeBakedDiffuseLigthingIntoGBuffer(float3 bakeDiffuseLighting, out float4 outBuffer)
{
    // RT - 11:11:10f
    outBuffer = float4(bakeDiffuseLighting.xyz, 0.0);
}

float3 DecodeBakedDiffuseLigthingFromGBuffer(float4 inBuffer)
{
    return float3(inBuffer.xyz);
}

//-----------------------------------------------------------------------------
// BuiltinData
//-----------------------------------------------------------------------------

#include "Builtin/BuiltinData.hlsl"

//-----------------------------------------------------------------------------
// Material definition
//-----------------------------------------------------------------------------

// Here we include all the different lighting model supported by the renderloop based on define done in .shader
#ifdef UNITY_MATERIAL_LIT
#include "Lit/Lit.hlsl"
#elif defined(UNITY_MATERIAL_UNLIT)
#include "Unlit/Unlit.hlsl"
#endif

//-----------------------------------------------------------------------------
// Define for GBuffer management
//-----------------------------------------------------------------------------

#ifdef GBUFFERMATERIAL_COUNT

#if GBUFFERMATERIAL_COUNT == 3

#define OUTPUT_GBUFFER(NAME)                            \
        out float4 MERGE_NAME(NAME, 0) : SV_Target0,    \
        out float4 MERGE_NAME(NAME, 1) : SV_Target1,    \
        out float4 MERGE_NAME(NAME, 2) : SV_Target2

#define DECLARE_GBUFFER_TEXTURE(NAME)   \
        TEXTURE2D(MERGE_NAME(NAME, 0));  \
        TEXTURE2D(MERGE_NAME(NAME, 1));  \
        TEXTURE2D(MERGE_NAME(NAME, 2));

#define FETCH_GBUFFER(NAME, TEX, UV)                                        \
        float4 MERGE_NAME(NAME, 0) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 0), uint3(UV, 0)); \
        float4 MERGE_NAME(NAME, 1) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 1), uint3(UV, 0)); \
        float4 MERGE_NAME(NAME, 2) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 2), uint3(UV, 0));

#define ENCODE_INTO_GBUFFER(SURFACE_DATA, NAME) EncodeIntoGBuffer(SURFACE_DATA, MERGE_NAME(NAME,0), MERGE_NAME(NAME,1), MERGE_NAME(NAME,2))
#define DECODE_FROM_GBUFFER(NAME) DecodeFromGBuffer(MERGE_NAME(NAME,0), MERGE_NAME(NAME,1), MERGE_NAME(NAME,2))

#ifdef VELOCITY_IN_GBUFFER
#define GBUFFER_VELOCITY_NAME(NAME) MERGE_NAME(NAME, 3)
#define GBUFFER_VELOCITY_TARGET(TARGET) MERGE_NAME(TARGET, 3)
#define GBUFFER_BAKE_LIGHTING_NAME(NAME) MERGE_NAME(NAME, 4)
#define GBUFFER_BAKE_LIGHTING_TARGET(TARGET) MERGE_NAME(TARGET, 4)
#else
#define GBUFFER_BAKE_LIGHTING_NAME(NAME) MERGE_NAME(NAME, 3)
#define GBUFFER_BAKE_LIGHTING_TARGET(TARGET) MERGE_NAME(TARGET, 3)
#endif

#elif GBUFFERMATERIAL_COUNT == 4

#define OUTPUT_GBUFFER(NAME)                            \
        out float4 MERGE_NAME(NAME, 0) : SV_Target0,    \
        out float4 MERGE_NAME(NAME, 1) : SV_Target1,    \
        out float4 MERGE_NAME(NAME, 2) : SV_Target2,    \
        out float4 MERGE_NAME(NAME, 3) : SV_Target3

#define DECLARE_GBUFFER_TEXTURE(NAME)   \
        TEXTURE2D(MERGE_NAME(NAME, 0));  \
        TEXTURE2D(MERGE_NAME(NAME, 1));  \
        TEXTURE2D(MERGE_NAME(NAME, 2));  \
        TEXTURE2D(MERGE_NAME(NAME, 3));

#define FETCH_GBUFFER(NAME, TEX, UV)                                        \
        float4 MERGE_NAME(NAME, 0) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 0), uint3(UV, 0)); \
        float4 MERGE_NAME(NAME, 1) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 1), uint3(UV, 0)); \
        float4 MERGE_NAME(NAME, 2) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 2), uint3(UV, 0)); \
        float4 MERGE_NAME(NAME, 3) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 3), uint3(UV, 0));

#define ENCODE_INTO_GBUFFER(SURFACE_DATA, NAME) EncodeIntoGBuffer(SURFACE_DATA, MERGE_NAME(NAME, 0), MERGE_NAME(NAME, 1), MERGE_NAME(NAME, 2), MERGE_NAME(NAME, 3))
#define DECODE_FROM_GBUFFER(NAME) DecodeFromGBuffer(MERGE_NAME(NAME, 0), MERGE_NAME(NAME, 1), MERGE_NAME(NAME, 2), MERGE_NAME(NAME, 3))

#ifdef VELOCITY_IN_GBUFFER
#define GBUFFER_VELOCITY_NAME(NAME) MERGE_NAME(NAME, 4)
#define GBUFFER_VELOCITY_TARGET(TARGET) MERGE_NAME(TARGET, 4)
#define GBUFFER_BAKE_LIGHTING_NAME(NAME) MERGE_NAME(NAME, 5)
#define GBUFFER_BAKE_LIGHTING_TARGET(TARGET) MERGE_NAME(TARGET, 5)
#else
#define GBUFFER_BAKE_LIGHTING_NAME(NAME) MERGE_NAME(NAME, 4)
#define GBUFFER_BAKE_LIGHTING_TARGET(TARGET) MERGE_NAME(TARGET, 4)
#endif

#elif GBUFFERMATERIAL_COUNT == 5

#define OUTPUT_GBUFFER(NAME)                            \
        out float4 MERGE_NAME(NAME, 0) : SV_Target0,    \
        out float4 MERGE_NAME(NAME, 1) : SV_Target1,    \
        out float4 MERGE_NAME(NAME, 2) : SV_Target2,    \
        out float4 MERGE_NAME(NAME, 3) : SV_Target3,    \
        out float4 MERGE_NAME(NAME, 4) : SV_Target4

#define DECLARE_GBUFFER_TEXTURE(NAME)   \
        TEXTURE2D(MERGE_NAME(NAME, 0));  \
        TEXTURE2D(MERGE_NAME(NAME, 1));  \
        TEXTURE2D(MERGE_NAME(NAME, 2));  \
        TEXTURE2D(MERGE_NAME(NAME, 3));  \
        TEXTURE2D(MERGE_NAME(NAME, 4));

#define FETCH_GBUFFER(NAME, TEX, UV)                                        \
        float4 MERGE_NAME(NAME, 0) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 0), uint3(UV, 0)); \
        float4 MERGE_NAME(NAME, 1) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 1), uint3(UV, 0)); \
        float4 MERGE_NAME(NAME, 2) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 2), uint3(UV, 0)); \
        float4 MERGE_NAME(NAME, 3) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 3), uint3(UV, 0)); \
        float4 MERGE_NAME(NAME, 4) = LOAD_TEXTURE2D(MERGE_NAME(TEX, 4), uint3(UV, 0));

#define ENCODE_INTO_GBUFFER(SURFACE_DATA, NAME) EncodeIntoGBuffer(SURFACE_DATA, MERGE_NAME(NAME, 0), MERGE_NAME(NAME, 1), MERGE_NAME(NAME, 2), MERGE_NAME(NAME, 3), MERGE_NAME(NAME, 4))
#define DECODE_FROM_GBUFFER(NAME) DecodeFromGBuffer(MERGE_NAME(NAME, 0), MERGE_NAME(NAME, 1), MERGE_NAME(NAME, 2), MERGE_NAME(NAME, 3), MERGE_NAME(NAME, 4))

#ifdef VELOCITY_IN_GBUFFER
#define GBUFFER_VELOCITY_NAME(NAME) MERGE_NAME(NAME, 5)
#define GBUFFER_VELOCITY_TARGET(TARGET) MERGE_NAME(TARGET, 5)
#define GBUFFER_BAKE_LIGHTING_NAME(NAME) MERGE_NAME(NAME, 6)
#define GBUFFER_BAKE_LIGHTING_TARGET(TARGET) MERGE_NAME(TARGET, 6)
#else
#define GBUFFER_BAKE_LIGHTING_NAME(NAME) MERGE_NAME(NAME, 5)
#define GBUFFER_BAKE_LIGHTING_TARGET(TARGET) MERGE_NAME(TARGET, 5)
#endif

#endif // #if GBUFFERMATERIAL_COUNT == 3

// Generic whatever the number of GBuffer
#ifdef VELOCITY_IN_GBUFFER
#define OUTPUT_GBUFFER_VELOCITY(NAME) out float4 GBUFFER_VELOCITY_NAME(NAME) : GBUFFER_VELOCITY_TARGET(SV_Target)
#define DECLARE_GBUFFER_VELOCITY_TEXTURE(NAME) TEXTURE2D(GBUFFER_VELOCITY_NAME(NAME));
#define ENCODE_VELOCITY_INTO_GBUFFER(VELOCITY, NAME) EncodeVelocity(VELOCITY, GBUFFER_VELOCITY_NAME(NAME))
#endif

#define OUTPUT_GBUFFER_BAKE_LIGHTING(NAME) out float4 GBUFFER_BAKE_LIGHTING_NAME(NAME) : GBUFFER_BAKE_LIGHTING_TARGET(SV_Target)
#define DECLARE_GBUFFER_BAKE_LIGHTING(NAME) TEXTURE2D(GBUFFER_BAKE_LIGHTING_NAME(NAME));
#define ENCODE_BAKE_LIGHTING_INTO_GBUFFER(BAKE_DIFFUSE_LIGHTING, NAME) EncodeBakedDiffuseLigthingIntoGBuffer(BAKE_DIFFUSE_LIGHTING, GBUFFER_BAKE_LIGHTING_NAME(NAME))
#define FETCH_BAKE_LIGHTING_GBUFFER(NAME, TEX, UV) float4 GBUFFER_BAKE_LIGHTING_NAME(NAME) = GBUFFER_BAKE_LIGHTING_NAME(TEX).Load(uint3(UV, 0));
#define DECODE_BAKE_LIGHTING_FROM_GBUFFER(NAME) DecodeBakedDiffuseLigthingFromGBuffer(GBUFFER_BAKE_LIGHTING_NAME(NAME))

#endif // #ifdef GBUFFERMATERIAL_COUNT

// Decode velocity need to be accessible in both forward and deferred
#ifdef VELOCITY_IN_GBUFFER
#define DECODE_VELOCITY_BUFFER(NAME) DecodeVelocity(GBUFFER_VELOCITY_NAME(NAME))
#else
#define DECODE_VELOCITY_BUFFER(NAME) DecodeVelocity(GBUFFER_VELOCITY_NAME(NAME))
#endif

#endif // UNITY_MATERIAL_INCLUDED
