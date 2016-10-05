#ifndef UNITY_MATERIAL_INCLUDED
#define UNITY_MATERIAL_INCLUDED

#include "Assets/ScriptableRenderLoop/ShaderLibrary/Packing.hlsl"
#include "Assets/ScriptableRenderLoop/ShaderLibrary/BSDF.hlsl"
#include "Assets/ScriptableRenderLoop/ShaderLibrary/CommonLighting.hlsl"

#include "Assets/ScriptableRenderLoop/HDRenderLoop/Shaders/LightDefinition.cs.hlsl"
#include "CommonMaterial.hlsl"

// Here we include all the different lighting model supported by the renderloop based on define done in .shader
#ifdef UNITY_MATERIAL_DISNEYGXX
#include "DisneyGGX.hlsl"
#endif

//-----------------------------------------------------------------------------
// Define for GBuffer
//-----------------------------------------------------------------------------

#ifdef GBUFFER_COUNT
#if GBUFFER_COUNT == 3

#define OUTPUT_GBUFFER(NAME)							\
		out float4 MERGE_NAME(NAME, 0) : SV_Target0,	\
		out float4 MERGE_NAME(NAME, 1) : SV_Target1,	\
		out float4 MERGE_NAME(NAME, 2) : SV_Target2

#define DECLARE_GBUFFER_TEXTURE(NAME)	\
		Texture2D MERGE_NAME(NAME, 0);	\
		Texture2D MERGE_NAME(NAME, 1);	\
		Texture2D MERGE_NAME(NAME, 2);

#define FETCH_GBUFFER(NAME, TEX, UV)										\
		float4 MERGE_NAME(NAME, 0) = MERGE_NAME(TEX, 0).Load(uint3(UV, 0));	\
		float4 MERGE_NAME(NAME, 1) = MERGE_NAME(TEX, 1).Load(uint3(UV, 0));	\
		float4 MERGE_NAME(NAME, 2) = MERGE_NAME(TEX, 2).Load(uint3(UV, 0));

#define ENCODE_INTO_GBUFFER(SURFACE_DATA, NAME) EncodeIntoGBuffer(SURFACE_DATA, MERGE_NAME(NAME,0), MERGE_NAME(NAME,1), MERGE_NAME(NAME,2))
#define DECODE_FROM_GBUFFER(NAME) DecodeFromGBuffer(MERGE_NAME(NAME,0), MERGE_NAME(NAME,1), MERGE_NAME(NAME,2))

#elif GBUFFER_COUNT == 4

#define OUTPUT_GBUFFER(NAME)							\
		out float4 MERGE_NAME(NAME, 0) : SV_Target0,	\
		out float4 MERGE_NAME(NAME, 1) : SV_Target1,	\
		out float4 MERGE_NAME(NAME, 2) : SV_Target2,	\
		out float4 MERGE_NAME(NAME, 3) : SV_Target3

#define DECLARE_GBUFFER_TEXTURE(NAME)	\
		Texture2D MERGE_NAME(NAME, 0);	\
		Texture2D MERGE_NAME(NAME, 1);	\
		Texture2D MERGE_NAME(NAME, 2);	\
		Texture2D MERGE_NAME(NAME, 3);

#define FETCH_GBUFFER(NAME, TEX, UV)										\
		float4 MERGE_NAME(NAME, 0) = MERGE_NAME(TEX, 0).Load(uint3(UV, 0));	\
		float4 MERGE_NAME(NAME, 1) = MERGE_NAME(TEX, 1).Load(uint3(UV, 0));	\
		float4 MERGE_NAME(NAME, 2) = MERGE_NAME(TEX, 2).Load(uint3(UV, 0));	\
		float4 MERGE_NAME(NAME, 3) = MERGE_NAME(TEX, 3).Load(uint3(UV, 0));

#define ENCODE_INTO_GBUFFER(SURFACE_DATA, NAME) EncodeIntoGBuffer(SURFACE_DATA, MERGE_NAME(NAME, 0), MERGE_NAME(NAME, 1), MERGE_NAME(NAME, 2), MERGE_NAME(NAME, 3))
#define DECODE_FROM_GBUFFER(NAME) DecodeFromGBuffer(MERGE_NAME(NAME, 0), MERGE_NAME(NAME, 1), MERGE_NAME(NAME, 2), MERGE_NAME(NAME, 3))

#endif

#endif // #ifdef GBUFFER_COUNT

#endif // UNITY_MATERIAL_INCLUDED