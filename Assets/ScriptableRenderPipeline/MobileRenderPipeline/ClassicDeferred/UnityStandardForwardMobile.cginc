#ifndef UNITY_STANDARD_FORWARD_MOBILE_INCLUDED
#define UNITY_STANDARD_FORWARD_MOBILE_INCLUDED


// NOTE: had to split shadow functions into separate file,
// otherwise compiler gives trouble with LIGHTING_COORDS macro (in UnityStandardCore.cginc)

#include "UnityStandardConfig.cginc"
#include "UnityStandardCore.cginc"

struct VertexOutputForwardNew
{
    float4 pos                          : SV_POSITION;
    float4 tex                          : TEXCOORD0;
    half4 ambientOrLightmapUV           : TEXCOORD1;    // SH or Lightmap UV
    half4 tangentToWorldAndParallax[3]  : TEXCOORD2;    // [3x3:tangentToWorld | 1x3:empty]
    float4 posWorld						: TEXCOORD8;

    LIGHTING_COORDS(5,6)
    UNITY_FOG_COORDS(7)

    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};


VertexOutputForwardNew vertForward(VertexInput v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    VertexOutputForwardNew o;
    UNITY_INITIALIZE_OUTPUT(VertexOutputForwardNew, o);
    UNITY_TRANSFER_INSTANCE_ID(v, o);

    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
    o.posWorld = posWorld;
    o.pos = UnityObjectToClipPos(v.vertex);
    o.tex = TexCoords(v);

    float3 normalWorld = UnityObjectToWorldNormal(v.normal);
    #ifdef _TANGENT_TO_WORLD
        float4 tangentWorld = float4(UnityObjectToWorldDir(v.tangent.xyz), v.tangent.w);

        float3x3 tangentToWorld = CreateTangentToWorldPerVertex(normalWorld, tangentWorld.xyz, tangentWorld.w);
        o.tangentToWorldAndParallax[0].xyz = tangentToWorld[0];
        o.tangentToWorldAndParallax[1].xyz = tangentToWorld[1];
        o.tangentToWorldAndParallax[2].xyz = tangentToWorld[2];
    #else
        o.tangentToWorldAndParallax[0].xyz = 0;
        o.tangentToWorldAndParallax[1].xyz = 0;
        o.tangentToWorldAndParallax[2].xyz = normalWorld;
    #endif

    o.ambientOrLightmapUV = VertexGIForward(v, posWorld, normalWorld);

    UNITY_TRANSFER_FOG(o,o.pos);

    return o;
}

// todo: put this is LightDefinitions common file
#define MAX_LIGHTS 10

float4 gLightData[MAX_LIGHTS];
half4 gLightColor[MAX_LIGHTS];
float4 gLightPos[MAX_LIGHTS];
half4 gLightDirection[MAX_LIGHTS];
float4x4 gLightMatrix[MAX_LIGHTS];
float4x4 gWorldToLightMatrix[MAX_LIGHTS];
float4  gData;

struct LightInput
{
	float4 lightData;
    half4 pos;
    half4 color;
    half4 lightDir;
    float4x4 lightMat;
    float4x4 worldToLightMat;
};

#define INITIALIZE_LIGHT(light, lightIndex) \
							light.lightData = gLightData[lightIndex]; \
                            light.pos = gLightPos[lightIndex]; \
                            light.color = gLightColor[lightIndex]; \
                            light.lightDir = gLightDirection[lightIndex]; \
                            light.lightMat = gLightMatrix[lightIndex]; \
                            light.worldToLightMat = gWorldToLightMatrix[lightIndex];

half4 fragForward(VertexOutputForwardNew i) : SV_Target
{
	half4 ret = half4(1, 0, 0, 0.5);

	for (int lightIndex = 0; lightIndex < gData.x; ++lightIndex)
    {
    	LightInput light;
    	INITIALIZE_LIGHT(light, lightIndex);

    	ret += 0.01*light.color;
    }
	
	return ret;
}

#endif
