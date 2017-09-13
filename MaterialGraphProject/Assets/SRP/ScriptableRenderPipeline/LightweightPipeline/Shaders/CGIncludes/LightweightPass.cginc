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

void DefineSurfaceMeta(v2f_meta i, inout SurfaceFastBlinn s);

fixed4 frag_meta_ld(v2f_meta i) : SV_Target
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