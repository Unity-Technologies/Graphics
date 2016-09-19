#ifndef UNITY_VERTEX_INCLUDED
#define UNITY_VERTEX_INCLUDED

#include "../Common.hlsl"

struct VertexOutputDeferred
{
	float4 pos							: SV_POSITION;
	float4 tex							: TEXCOORD0;
	float4 tangentToWorldAndParallax[3]	: TEXCOORD1;	// [3x3:tangentToWorld | 1x3:viewDirForParallax]
};

VertexOutputDeferred vertDeferred (VertexInput v)
{
	UNITY_SETUP_INSTANCE_ID(v);
	VertexOutputDeferred o;
	UNITY_INITIALIZE_OUTPUT(VertexOutputDeferred, o);

	float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
	o.pos = UnityObjectToClipPos(v.vertex);
	o.tex = TexCoords(v);
	float3 normalWorld = UnityObjectToWorldNormal(v.normal);

	float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

	float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
	o.tangentToWorldAndParallax[0].xyz = tangentToWorld[0];
	o.tangentToWorldAndParallax[1].xyz = tangentToWorld[1];
	o.tangentToWorldAndParallax[2].xyz = tangentToWorld[2];

	return o;
}

#endif // UNITY_VERTEX_INCLUDED