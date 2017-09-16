#ifndef LIGHTWEIGHT_PASS_INCLUDED
#define LIGHTWEIGHT_PASS_INCLUDED

float4 shadowVert(float4 pos : POSITION) : SV_POSITION
{
    float4 clipPos = UnityObjectToClipPos(pos);
#if defined(UNITY_REVERSED_Z)
    clipPos.z = min(clipPos.z, UNITY_NEAR_CLIP_VALUE);
#else
    clipPos.z = max(clipPos.z, UNITY_NEAR_CLIP_VALUE);
#endif
    return clipPos;
}

half4 shadowFrag() : SV_TARGET
{
    return 0;
}

float4 depthVert(float4 pos : POSITION) : SV_POSITION
{
	return UnityObjectToClipPos(pos);
}

half4 depthFrag() : SV_TARGET
{
	return 0;
}

// --------------------------------
// Meta

#include "UnityStandardMeta.cginc"
#include "LightweightFastBlinn.cginc"

struct VertOutput_Meta
{
	float4 meshUV0       : TEXCOORD0;
	float4 pos      : SV_POSITION;
};

VertOutput_Meta Vert_Meta(VertexInput v)
{
	VertOutput_Meta o;
	o.pos = UnityMetaVertexPosition(v.vertex, v.uv1.xy, v.uv2.xy, unity_LightmapST, unity_DynamicLightmapST);
	o.meshUV0 = TexCoords(v);
	return o;
}

void DefineSurfaceMeta(VertOutput_Meta i, inout SurfaceFastBlinn s);

fixed4 frag_meta_ld(VertOutput_Meta i) : SV_Target
{
    UnityMetaInput o;
    UNITY_INITIALIZE_OUTPUT(UnityMetaInput, o);

	SurfaceFastBlinn s = InitializeSurfaceFastBlinn();
	DefineSurfaceMeta(i, s);

	o.Albedo = s.Diffuse;
	o.SpecularColor = s.Specular;
	o.Emission = s.Emission;

    return UnityMetaFragment(o);
}

#endif