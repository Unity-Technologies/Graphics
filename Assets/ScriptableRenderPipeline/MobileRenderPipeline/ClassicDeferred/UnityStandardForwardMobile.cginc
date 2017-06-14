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

float4 gPerLightData[MAX_LIGHTS];
half4 gLightColor[MAX_LIGHTS];
float4 gLightPos[MAX_LIGHTS];
half4 gLightDirection[MAX_LIGHTS];
float4x4 gLightMatrix[MAX_LIGHTS];
float4x4 gWorldToLightMatrix[MAX_LIGHTS];
float4  gLightData;

float4x4 g_mViewToWorld;
float4x4 g_mWorldToView;        // used for reflection only
float4x4 g_mScrProjection;
float4x4 g_mInvScrProjection;

static FragmentCommonData gdata;
static float occlusion;

struct LightInput
{
	float4 lightData;
    half4 pos;
    half4 color;
    half4 lightDir;
    float4x4 lightMat;
    float4x4 worldToLightMat;
};

float GetLinearZFromSVPosW(float posW)
{
#if USE_LEFTHAND_CAMERASPACE
    float linZ = posW;
#else
    float linZ = -posW;
#endif

    return linZ;
}

float3 GetViewPosFromLinDepth(float2 v2ScrPos, float fLinDepth)
{
    float fSx = g_mScrProjection[0].x;
    //float fCx = g_mScrProjection[2].x;
    float fCx = g_mScrProjection[0].z;
    float fSy = g_mScrProjection[1].y;
    //float fCy = g_mScrProjection[2].y;
    float fCy = g_mScrProjection[1].z;

#if USE_LEFTHAND_CAMERASPACE
    return fLinDepth*float3( ((v2ScrPos.x-fCx)/fSx), ((v2ScrPos.y-fCy)/fSy), 1.0 );
#else
    return fLinDepth*float3( -((v2ScrPos.x+fCx)/fSx), -((v2ScrPos.y+fCy)/fSy), 1.0 );
#endif
}

#define INITIALIZE_LIGHT(light, lightIndex) \
							light.lightData = gLightData[lightIndex]; \
                            light.pos = gLightPos[lightIndex]; \
                            light.color = gLightColor[lightIndex]; \
                            light.lightDir = gLightDirection[lightIndex]; \
                            light.lightMat = gLightMatrix[lightIndex]; \
                            light.worldToLightMat = gWorldToLightMatrix[lightIndex];

half4 fragForward(VertexOutputForwardNew i) : SV_Target
{
	float linZ = GetLinearZFromSVPosW(i.pos.w);                 // matching script side where camera space is right handed.
    float3 vP = GetViewPosFromLinDepth(i.pos.xy, linZ);
    float3 vPw = mul(g_mViewToWorld, float4(vP,1.0)).xyz;
    float3 Vworld = normalize(mul((float3x3) g_mViewToWorld, -vP).xyz);     // not same as unity_CameraToWorld

#ifdef _PARALLAXMAP
    half3 tangent = i.tangentToWorldAndParallax[0].xyz;
    half3 bitangent = i.tangentToWorldAndParallax[1].xyz;
    half3 normal = i.tangentToWorldAndParallax[2].xyz;
    float3 vDirForParallax = float3( dot(tangent, Vworld), dot(bitangent, Vworld), dot(normal, Vworld));
#else
    float3 vDirForParallax = Vworld;
#endif
    gdata = FragmentSetup(i.tex, -Vworld, vDirForParallax, i.tangentToWorldAndParallax, vPw);       // eyeVec = -Vworld

    uint2 pixCoord = ((uint2) i.pos.xy);

    float atten = 1.0;
    occlusion = Occlusion(i.tex.xy);
    UnityGI gi = FragmentGI (gdata, occlusion, i.ambientOrLightmapUV, atten, DummyLight(), false);

    uint numLightsProcessed = 0, numReflectionsProcessed = 0;
    float3 res = 0;

    // direct light contributions
    //res += ExecuteLightList(numLightsProcessed, pixCoord, vP, vPw, Vworld);

    // specular GI
    //res += ExecuteReflectionList(numReflectionsProcessed, pixCoord, vP, gdata.normalWorld, Vworld, gdata.smoothness);

    // diffuse GI
    res += UNITY_BRDF_PBS (gdata.diffColor, gdata.specColor, gdata.oneMinusReflectivity, gdata.smoothness, gdata.normalWorld, -gdata.eyeVec, gi.light, gi.indirect).xyz;
    res += UNITY_BRDF_GI (gdata.diffColor, gdata.specColor, gdata.oneMinusReflectivity, gdata.smoothness, gdata.normalWorld, -gdata.eyeVec, occlusion, gi);

//	for (int lightIndex = 0; lightIndex < gData.x; ++lightIndex)
//    {
//    	LightInput light;
//    	INITIALIZE_LIGHT(light, lightIndex);
//
//
//    }
	
	return OutputForward (float4(res,1.0), gdata.alpha);

}

#endif
