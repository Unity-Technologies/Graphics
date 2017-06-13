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
    //float4 posWorld						: TEXCOORD3;

    LIGHTING_COORDS(5,6)
    UNITY_FOG_COORDS(7)

    UNITY_VERTEX_OUTPUT_STEREO
};



VertexOutputForwardNew vertForward(VertexInput v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    VertexOutputForwardNew o;
    UNITY_INITIALIZE_OUTPUT(VertexOutputForwardNew, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    float4 posWorld = mul(unity_ObjectToWorld, v.vertex);
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

//#include "LightingUtils.hlsl"

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
	float fSx = UNITY_MATRIX_P[0].x;
	float fCx = UNITY_MATRIX_P[0].z;
	float fSy = UNITY_MATRIX_P[1].y;
	float fCy = UNITY_MATRIX_P[1].z;

	return fLinDepth*float3( ((v2ScrPos.x-fCx)/fSx), ((v2ScrPos.y-fCy)/fSy), 1.0 );
}

static FragmentCommonData gdata;
static float occlusion;

//half4 fragNoLight(VertexOutputForwardNew i) : SV_Target
//{
//    float linZ = GetLinearZFromSVPosW(i.pos.w);                 // matching script side where camera space is right handed.
//    float3 vP = GetViewPosFromLinDepth(i.pos.xy, linZ);
//    float3 vPw = mul(g_mViewToWorld, float4(vP,1.0)).xyz;
//    float3 Vworld = normalize(mul((float3x3) g_mViewToWorld, -vP).xyz);     // not same as unity_CameraToWorld
//
//#ifdef _PARALLAXMAP
//    half3 tangent = i.tangentToWorldAndParallax[0].xyz;
//    half3 bitangent = i.tangentToWorldAndParallax[1].xyz;
//    half3 normal = i.tangentToWorldAndParallax[2].xyz;
//    float3 vDirForParallax = float3( dot(tangent, Vworld), dot(bitangent, Vworld), dot(normal, Vworld));
//#else
//    float3 vDirForParallax = Vworld;
//#endif
//    gdata = FragmentSetup(i.tex, -Vworld, vDirForParallax, i.tangentToWorldAndParallax, vPw);       // eyeVec = -Vworld
//
//    return OutputForward (float4(0.0,0.0,0.0,1.0), gdata.alpha);        // figure out some alpha test stuff
//}

float3 EvalMaterial(UnityLight light, UnityIndirect ind)
{
    return UNITY_BRDF_PBS(gdata.diffColor, gdata.specColor, gdata.oneMinusReflectivity, gdata.smoothness, gdata.normalWorld, -gdata.eyeVec, light, ind);
}

float3 EvalIndirectSpecular(UnityLight light, UnityIndirect ind)
{
    return occlusion * UNITY_BRDF_PBS(gdata.diffColor, gdata.specColor, gdata.oneMinusReflectivity, gdata.smoothness, gdata.normalWorld, -gdata.eyeVec, light, ind);
}


//#include "RegularForwardLightingTemplate.hlsl"
//#include "RegularForwardReflectionTemplate.hlsl"

uniform int g_numLights;
uniform int g_numReflectionProbes;

uniform float4x4 g_mViewToWorld;
uniform float4x4 g_mWorldToView;        // used for reflection only
uniform float4x4 g_mScrProjection;
uniform float4x4 g_mInvScrProjection;

void GetCountAndStart(out uint start, out uint nrLights, bool reflection)
{
    start = reflection==true ? g_numLights : 0;  // offset by numLights entries
    nrLights = reflection==true ? g_numReflectionProbes : g_numLights;
}

float3 ExecuteLightList(out uint numLightsProcessed, uint2 pixCoord, float3 vP, float3 vPw, float3 Vworld)
{
    uint start = 0, numLights = 0;
    GetCountAndStart(start, numLights, false);

    numLightsProcessed = numLights;     // mainly for debugging/heat maps
    return float3(1, 0, 0);
    //return ExecuteLightList(start, numLights, vP, vPw, Vworld);
}

float3 ExecuteReflectionList(out uint numReflectionProbesProcessed, uint2 pixCoord, float3 vP, float3 vNw, float3 Vworld, float smoothness)
{
    uint start = 0, numReflectionProbes = 0;
    GetCountAndStart(start, numReflectionProbes, true);

    numReflectionProbesProcessed = numReflectionProbes;     // mainly for debugging/heat maps
    return float3(0, 1, 0);
    //return ExecuteReflectionList(start, numReflectionProbes, vP, vNw, Vworld, smoothness);
}

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
    res += ExecuteLightList(numLightsProcessed, pixCoord, vP, vPw, Vworld);

    // specular GI
    res += ExecuteReflectionList(numReflectionsProcessed, pixCoord, vP, gdata.normalWorld, Vworld, gdata.smoothness);

    // diffuse GI
    res += UNITY_BRDF_PBS (gdata.diffColor, gdata.specColor, gdata.oneMinusReflectivity, gdata.smoothness, gdata.normalWorld, -gdata.eyeVec, gi.light, gi.indirect).xyz;
    res += UNITY_BRDF_GI (gdata.diffColor, gdata.specColor, gdata.oneMinusReflectivity, gdata.smoothness, gdata.normalWorld, -gdata.eyeVec, occlusion, gi);

    //res = OverlayHeatMap(numLightsProcessed, res);

    //UNITY_APPLY_FOG(i.fogCoord, res);
    return OutputForward (float4(res,1.0), gdata.alpha);
}

#endif
