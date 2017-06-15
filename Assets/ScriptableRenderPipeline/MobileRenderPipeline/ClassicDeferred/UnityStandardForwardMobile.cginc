#ifndef UNITY_STANDARD_FORWARD_MOBILE_INCLUDED
#define UNITY_STANDARD_FORWARD_MOBILE_INCLUDED


// NOTE: had to split shadow functions into separate file,
// otherwise compiler gives trouble with LIGHTING_COORDS macro (in UnityStandardCore.cginc)

#include "UnityStandardConfig.cginc"
#include "UnityStandardCore.cginc"

#include "ShaderBase.h"

#define MAX_SHADOW_LIGHTS 10
#define MAX_SHADOWMAP_PER_LIGHT 6
#define MAX_DIRECTIONAL_SPLIT  4

#define CUBEMAPFACE_POSITIVE_X 0
#define CUBEMAPFACE_NEGATIVE_X 1
#define CUBEMAPFACE_POSITIVE_Y 2
#define CUBEMAPFACE_NEGATIVE_Y 3
#define CUBEMAPFACE_POSITIVE_Z 4
#define CUBEMAPFACE_NEGATIVE_Z 5

#define SHADOW_FPTL
#	if defined(SHADER_API_D3D11)
#		include "../../ShaderLibrary/API/D3D11.hlsl"
#	elif defined(SHADER_API_PSSL)
#		include "../../ShaderLibrary/API/PSSL.hlsl"
#	elif defined(SHADER_API_XBOXONE)
#		include "../../ShaderLibrary/API/D3D11.hlsl"
#		include "../../ShaderLibrary/API/D3D11_1.hlsl"
#	elif defined(SHADER_API_METAL)
#		include "../../ShaderLibrary/API/Metal.hlsl"
#	else
#		error unsupported shader api
#	endif
#	include "../../ShaderLibrary/API/Validate.hlsl"
#	include "../../ShaderLibrary/Shadow/Shadow.hlsl"
#undef SHADOW_FPTL

CBUFFER_START(ShadowLightData)

float4 g_vShadow3x3PCFTerms0;
float4 g_vShadow3x3PCFTerms1;
float4 g_vShadow3x3PCFTerms2;
float4 g_vShadow3x3PCFTerms3;

float4 g_vDirShadowSplitSpheres[MAX_DIRECTIONAL_SPLIT];
float4x4 g_matWorldToShadow[MAX_SHADOW_LIGHTS * MAX_SHADOWMAP_PER_LIGHT];

CBUFFER_END

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

#define USE_LEFTHAND_CAMERASPACE (0)
#define DIRECT_LIGHT (0)
#define REFLECTION_LIGHT (1)
#define SPOT_LIGHT (0)
#define SPHERE_LIGHT (1)
#define BOX_LIGHT (2)
#define DIRECTIONAL_LIGHT (3)

float4 gPerLightData[MAX_LIGHTS];
half4 gLightColor[MAX_LIGHTS];
float4 gLightPos[MAX_LIGHTS];
half4 gLightDirection[MAX_LIGHTS];
float4x4 gLightMatrix[MAX_LIGHTS];
float4x4 gWorldToLightMatrix[MAX_LIGHTS];
float4  gLightData;

int g_numLights;
int g_numReflectionProbes;

float4x4 g_mViewToWorld;
float4x4 g_mWorldToView;        // used for reflection only
float4x4 g_mScrProjection;
float4x4 g_mInvScrProjection;

sampler2D _LightTextureB0;
UNITY_DECLARE_TEX2DARRAY(_spotCookieTextures);
//UNITY_DECLARE_ABSTRACT_CUBE_ARRAY(_pointCookieTextures);

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
							light.lightData = gPerLightData[lightIndex]; \
                            light.pos = gLightPos[lightIndex]; \
                            light.color = gLightColor[lightIndex]; \
                            light.lightDir = gLightDirection[lightIndex]; \
                            light.lightMat = gLightMatrix[lightIndex]; \
                            light.worldToLightMat = gWorldToLightMatrix[lightIndex];

float3 EvalMaterial(UnityLight light, UnityIndirect ind)
{
    return UNITY_BRDF_PBS(gdata.diffColor, gdata.specColor, gdata.oneMinusReflectivity, gdata.smoothness, gdata.normalWorld, -gdata.eyeVec, light, ind);
}

float3 EvalIndirectSpecular(UnityLight light, UnityIndirect ind)
{
    return occlusion * UNITY_BRDF_PBS(gdata.diffColor, gdata.specColor, gdata.oneMinusReflectivity, gdata.smoothness, gdata.normalWorld, -gdata.eyeVec, light, ind);
}

float3 RenderLightList(uint start, uint numLights, float3 vPw, float3 Vworld)
{
    UnityIndirect ind;
    UNITY_INITIALIZE_OUTPUT(UnityIndirect, ind);
    ind.diffuse = 0;
    ind.specular = 0;

    ShadowContext shadowContext = InitShadowContext();

    float3 ints = 0;

    for (int lightIndex = 0; lightIndex < gLightData.x; ++lightIndex)
    {
  		if (gPerLightData[lightIndex].x == DIRECTIONAL_LIGHT) 
  		{
  			float atten = 1;

	  		int shadowIdx = asint(gPerLightData[lightIndex].y);
			[branch]
			if (shadowIdx >= 0)
			{
				float shadow = GetDirectionalShadowAttenuation(shadowContext, vPw, 0.0.xxx, shadowIdx, 0.0.xxx);
				atten *= shadow;
			}

			float4 cookieColor = float4(1,1,1,1);
//			float4 uvCookie = mul (gLightMatrix[lightIndex], float4(vPw,1));
//            float2 cookCoord = uvCookie.xy / uvCookie.w;
//			const bool bHasCookie = gPerLightData[lightIndex].z > 0;
//            [branch]if(bHasCookie)
//            {
//       			cookieColor *= UNITY_SAMPLE_TEX2DARRAY_LOD(_spotCookieTextures, float3(cookCoord, gPerLightData[lightIndex].z), 0.0);
//       			atten *= (-cookieColor.w>0.0);
//            }

	        UnityLight light;
	        light.color.xyz = gLightColor[lightIndex].xyz*atten*cookieColor.xyz;
	        light.dir.xyz = -gLightDirection[lightIndex].xyz;

	        ints += EvalMaterial(light, ind);
  		}
  		else if (gPerLightData[lightIndex].x == SPHERE_LIGHT)
  		{
  			float3 vLp = gLightPos[lightIndex].xyz;

            float3 toLight  = vLp - vPw;
            float dist = length(toLight);
            float3 vLw = toLight / dist;

            float att = dot(toLight, toLight) * gLightPos[lightIndex].w;
			float atten = tex2D (_LightTextureB0, att.rr).UNITY_ATTEN_CHANNEL;

            float4 cookieColor = float4(1,1,1,1);
//            const bool bHasCookie = gPerLightData[lightIndex].z > 0;
//            [branch]if(bHasCookie)
//            {
//                float3 cookieCoord = -float3(dot(vL, lgtDat.lightAxisX.xyz), dot(vL, lgtDat.lightAxisY.xyz), dot(vL, lgtDat.lightAxisZ.xyz));    // negate to make vL a fromLight vector
//                cookieColor = UNITY_SAMPLE_ABSTRACT_CUBE_ARRAY_LOD(_pointCookieTextures, float4(cookieCoord, lgtDat.sliceIndex), 0.0);
//                atten *= cookieColor.w;
//            }

			int shadowIdx = asint(gPerLightData[lightIndex].y);
			[branch]
			if (shadowIdx >= 0)
			{
				float shadow = GetPunctualShadowAttenuation(shadowContext, vPw, 0.0.xxx, shadowIdx, vLw);
				atten *= shadow;
			}

            UnityLight light;
            light.color.xyz = gLightColor[lightIndex].xyz*atten*cookieColor.xyz;
            light.dir.xyz = vLw;

            ints += EvalMaterial(light, ind);
  		}
  		else if (gPerLightData[lightIndex].x == SPOT_LIGHT)
  		{
            float3 vLp = gLightPos[lightIndex].xyz;

            float3 toLight  = vLp - vPw;
            float dist = length(toLight);
            float3 vLw = toLight / dist;

            // distance atten
			float att = dot(toLight, toLight) * gLightPos[lightIndex].w;
			float atten = tex2D (_LightTextureB0, att.rr).UNITY_ATTEN_CHANNEL;

            // For debug: spot attenuation -- programatic no cookie
            //const float fProjVec = -dot(vL, gLightDirection[lightIndex].xyz);        // spotDir = lgtDat.lightAxisZ.xyz
            //float2 cookCoord = (-lgtDat.cotan)*float2( dot(vLw, lgtDat.lightAxisX.xyz), dot(vL, lgtDat.lightAxisY.xyz) ) / fProjVec;

            float4 uvCookie = mul (gLightMatrix[lightIndex], float4(vPw,1));
            float2 cookCoord = uvCookie.xy / uvCookie.w;

            float d0 = 0.65;
            float4 angularAtt = float4(1,1,1,smoothstep(0.0, 1.0-d0, 1.0-length(2*cookCoord-1)));
            const bool bHasCookie = gPerLightData[lightIndex].z > 0;
            [branch]if(bHasCookie)
            {
               angularAtt = UNITY_SAMPLE_TEX2DARRAY_LOD(_spotCookieTextures, float3(cookCoord, gPerLightData[lightIndex].z), 0.0);
            }
            atten *= angularAtt.w*(-uvCookie.w>0.0);                           // finally apply this to the dist att.

			int shadowIdx = asint(gPerLightData[lightIndex].y);
			[branch]
			if (shadowIdx >= 0)
			{
				float shadow = GetPunctualShadowAttenuation(shadowContext, vPw, 0.0.xxx, shadowIdx, 0.0.xxx);
				atten *= shadow;
			}

            UnityLight light;
            light.color.xyz = gLightColor[lightIndex].xyz*atten*angularAtt.xyz;
            light.dir.xyz = vLw.xyz;     //unity_CameraToWorld

            ints += EvalMaterial(light, ind);
  		}
    }

    return ints;
}

void GetCountAndStart(out uint start, out uint nrLights, uint model)
{
    start = model==REFLECTION_LIGHT ? g_numLights : 0;  // offset by numLights entries
    nrLights = model==REFLECTION_LIGHT ? g_numReflectionProbes : g_numLights;
}

float3 ExecuteLightList(out uint numLightsProcessed, uint2 pixCoord, float3 vPw, float3 Vworld)
{
    uint start = 0, numLights = 0;
    GetCountAndStart(start, numLights, DIRECT_LIGHT);

    numLightsProcessed = numLights;     // mainly for debugging/heat maps
    return RenderLightList(start, numLights, vPw, Vworld);
}
                            
half4 fragForward(VertexOutputForwardNew i) : SV_Target
{
	//float linZ = GetLinearZFromSVPosW(i.pos.w);                 // matching script side where camera space is right handed.
    //float3 vP = GetViewPosFromLinDepth(i.pos.xy, linZ);
    //float3 vPw = mul(g_mViewToWorld, float4(vP,1.0)).xyz;
    //float3 Vworld = normalize(mul((float3x3) g_mViewToWorld, -vP).xyz);     // not same as unity_CameraToWorld

    float3 vPw = i.posWorld;
    float3 Vworld = normalize(_WorldSpaceCameraPos.xyz - vPw);

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
    res += ExecuteLightList(numLightsProcessed, pixCoord, vPw, Vworld);

    // specular GI
    //res += ExecuteReflectionList(numReflectionsProcessed, pixCoord, vP, gdata.normalWorld, Vworld, gdata.smoothness);

    // diffuse GI
    res += UNITY_BRDF_PBS (gdata.diffColor, gdata.specColor, gdata.oneMinusReflectivity, gdata.smoothness, gdata.normalWorld, -gdata.eyeVec, gi.light, gi.indirect).xyz;
    res += UNITY_BRDF_GI (gdata.diffColor, gdata.specColor, gdata.oneMinusReflectivity, gdata.smoothness, gdata.normalWorld, -gdata.eyeVec, occlusion, gi);
    	
	return OutputForward (float4(res,1.0), gdata.alpha);

}

#endif
