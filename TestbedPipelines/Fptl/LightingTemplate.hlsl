#ifndef __LIGHTINGTEMPLATE_H__
#define __LIGHTINGTEMPLATE_H__


#include "UnityCG.cginc"
#include "UnityStandardBRDF.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityPBSLighting.cginc"


uniform uint g_nNumDirLights;

//---------------------------------------------------------------------------------------------------------------------------------------------------------
// TODO:  clean up.. -va
#define MAX_SHADOW_LIGHTS 10
#define MAX_SHADOWMAP_PER_LIGHT 6
#define MAX_DIRECTIONAL_SPLIT  4

#define CUBEMAPFACE_POSITIVE_X 0
#define CUBEMAPFACE_NEGATIVE_X 1
#define CUBEMAPFACE_POSITIVE_Y 2
#define CUBEMAPFACE_NEGATIVE_Y 3
#define CUBEMAPFACE_POSITIVE_Z 4
#define CUBEMAPFACE_NEGATIVE_Z 5

// The function of the shader library are stateless, no uniform decalare in it.
// Any function that require an explicit precision, use float or half qualifier, when the function can support both, it use real (see below)
// If a function require to have both a half and a float version, then both need to be explicitly define
#ifndef real

#ifdef SHADER_API_MOBILE
#define real half
#define real2 half2
#define real3 half3
#define real4 half4

#define real2x2 half2x2
#define real2x3 half2x3
#define real3x2 half3x2
#define real3x3 half3x3
#define real3x4 half3x4
#define real4x3 half4x3
#define real4x4 half4x4

#define REAL_MIN HALF_MIN
#define REAL_MAX HALF_MAX
#define TEMPLATE_1_REAL TEMPLATE_1_HALF
#define TEMPLATE_2_REAL TEMPLATE_2_HALF
#define TEMPLATE_3_REAL TEMPLATE_3_HALF

#else

#define real float
#define real2 float2
#define real3 float3
#define real4 float4

#define real2x2 float2x2
#define real2x3 float2x3
#define real3x2 float3x2
#define real3x3 float3x3
#define real3x4 float3x4
#define real4x3 float4x3
#define real4x4 float4x4

#define REAL_MIN FLT_MIN
#define REAL_MAX FLT_MAX
#define TEMPLATE_1_REAL TEMPLATE_1_FLT
#define TEMPLATE_2_REAL TEMPLATE_2_FLT
#define TEMPLATE_3_REAL TEMPLATE_3_FLT

#endif // SHADER_API_MOBILE

#endif // #ifndef real

#if defined(SHADER_API_D3D11)
#	include "CoreRP/ShaderLibrary/API/D3D11.hlsl"
#elif defined(SHADER_API_PSSL)
#	include "CoreRP/ShaderLibrary/API/PSSL.hlsl"
#elif defined(SHADER_API_XBOXONE)
#	include "CoreRP/ShaderLibrary/API/D3D11.hlsl"
#	include "CoreRP/ShaderLibrary/API/D3D11_1.hlsl"
#elif defined(SHADER_API_METAL)
#	include "CoreRP/ShaderLibrary/API/Metal.hlsl"
#else
#	error unsupported shader api
#endif
#include "CoreRP/ShaderLibrary/API/Validate.hlsl"

#include "Shadow.hlsl"

CBUFFER_START(ShadowLightData)

float4 g_vShadow3x3PCFTerms0;
float4 g_vShadow3x3PCFTerms1;
float4 g_vShadow3x3PCFTerms2;
float4 g_vShadow3x3PCFTerms3;

float4 g_vDirShadowSplitSpheres[MAX_DIRECTIONAL_SPLIT];
float4x4 g_matWorldToShadow[MAX_SHADOW_LIGHTS * MAX_SHADOWMAP_PER_LIGHT];

CBUFFER_END
//---------------------------------------------------------------------------------------------------------------------------------------------------------


//UNITY_DECLARE_TEX2D(_LightTextureB0);
sampler2D _LightTextureB0;
UNITY_DECLARE_TEX2DARRAY(_spotCookieTextures);
UNITY_DECLARE_ABSTRACT_CUBE_ARRAY(_pointCookieTextures);

StructuredBuffer<DirectionalLight> g_dirLightData;

float3 ExecuteLightList(uint start, uint numLights, float3 vP, float3 vPw, float3 Vworld)
{
    UnityIndirect ind;
    UNITY_INITIALIZE_OUTPUT(UnityIndirect, ind);
    ind.diffuse = 0;
    ind.specular = 0;

	ShadowContext shadowContext = InitShadowContext();

    float3 ints = 0;

    for (int i = 0; i < g_nNumDirLights; i++)
    {
        DirectionalLight lightData = g_dirLightData[i];
        float atten = 1;

        UnityLight light;
        light.dir.xyz = mul((float3x3) g_mViewToWorld, -lightData.lightAxisZ).xyz;

		int shadowIdx = asint(lightData.shadowLightIndex);
		[branch]
		if (shadowIdx >= 0)
		{
			float shadow = GetDirectionalShadowAttenuation(shadowContext, vPw, 0.0.xxx, shadowIdx, normalize(light.dir.xyz));
			atten *= shadow;
		}

        light.color.xyz = lightData.color.xyz * atten;

        ints += EvalMaterial(light, ind);
    }

    uint l=0;
    // don't need the outer loop since the lights are sorted by volume type
    //while(l<numLights)
    if(numLights>0)
    {
        uint uIndex = l<numLights ? FetchIndex(start, l) : 0;
        uint uLgtType = l<numLights ? g_vLightData[uIndex].lightType : 0;

        // specialized loop for spot lights
        while(l<numLights && uLgtType==SPOT_LIGHT)
        {
            SFiniteLightData lgtDat = g_vLightData[uIndex];
            float3 vLp = lgtDat.lightPos.xyz;

            float3 toLight  = vLp - vP;
            float dist = length(toLight);
            float3 vL = toLight / dist;

            float attLookUp = dist*lgtDat.recipRange; attLookUp *= attLookUp;
            float atten = tex2Dlod(_LightTextureB0, float4(attLookUp.rr, 0.0, 0.0)).UNITY_ATTEN_CHANNEL;

            // spot attenuation
            const float fProjVec = -dot(vL, lgtDat.lightAxisZ.xyz);        // spotDir = lgtDat.lightAxisZ.xyz
            float2 cookCoord = (-lgtDat.cotan)*float2( dot(vL, lgtDat.lightAxisX.xyz), dot(vL, lgtDat.lightAxisY.xyz) ) / fProjVec;

            const bool bHasCookie = (lgtDat.flags&IS_CIRCULAR_SPOT_SHAPE)==0;       // all square spots have cookies
            float d0 = 0.65;
            float4 angularAtt = float4(1,1,1,smoothstep(0.0, 1.0-d0, 1.0-length(cookCoord)));
            [branch]if(bHasCookie)
            {
                cookCoord = cookCoord*0.5 + 0.5;
                angularAtt = UNITY_SAMPLE_TEX2DARRAY_LOD(_spotCookieTextures, float3(cookCoord, lgtDat.sliceIndex), 0.0);
            }
            atten *= angularAtt.w*(fProjVec>0.0);                           // finally apply this to the dist att.

            UnityLight light;
            light.dir.xyz = mul((float3x3) g_mViewToWorld, vL).xyz;     //unity_CameraToWorld

			int shadowIdx = asint(lgtDat.shadowLightIndex);
			[branch]
			if (shadowIdx >= 0)
			{
				float shadow = GetPunctualShadowAttenuation(shadowContext, vPw, 0.0.xxx, shadowIdx, float4(normalize(light.dir.xyz), dist));
				atten *= shadow;
			}

            light.color.xyz = lgtDat.color.xyz*atten*angularAtt.xyz;

            ints += EvalMaterial(light, ind);

            ++l; uIndex = l<numLights ? FetchIndex(start, l) : 0;
            uLgtType = l<numLights ? g_vLightData[uIndex].lightType : 0;
        }

        // specialized loop for sphere lights
        while(l<numLights && uLgtType==SPHERE_LIGHT)
        {
            SFiniteLightData lgtDat = g_vLightData[uIndex];
            float3 vLp = lgtDat.lightPos.xyz;

            float3 toLight  = vLp - vP;
            float dist = length(toLight);
            float3 vL = toLight / dist;
            float3 vLw = mul((float3x3) g_mViewToWorld, vL).xyz;        //unity_CameraToWorld

            float attLookUp = dist*lgtDat.recipRange; attLookUp *= attLookUp;
            float atten = tex2Dlod(_LightTextureB0, float4(attLookUp.rr, 0.0, 0.0)).UNITY_ATTEN_CHANNEL;

            float4 cookieColor = float4(1,1,1,1);

            const bool bHasCookie = (lgtDat.flags&HAS_COOKIE_TEXTURE)!=0;
            [branch]if(bHasCookie)
            {
                float3 cookieCoord = -float3(dot(vL, lgtDat.lightAxisX.xyz), dot(vL, lgtDat.lightAxisY.xyz), dot(vL, lgtDat.lightAxisZ.xyz));    // negate to make vL a fromLight vector
                cookieColor = UNITY_SAMPLE_ABSTRACT_CUBE_ARRAY_LOD(_pointCookieTextures, float4(cookieCoord, lgtDat.sliceIndex), 0.0);
                atten *= cookieColor.w;
            }

			int shadowIdx = asint(lgtDat.shadowLightIndex);
			[branch]
			if (shadowIdx >= 0)
			{
				float shadow = GetPunctualShadowAttenuation(shadowContext, vPw, 0.0.xxx, shadowIdx, float4(vLw, dist));
				atten *= shadow;
			}

            UnityLight light;
            light.color.xyz = lgtDat.color.xyz*atten*cookieColor.xyz;
            light.dir.xyz = vLw;

            ints += EvalMaterial(light, ind);

            ++l; uIndex = l<numLights ? FetchIndex(start, l) : 0;
            uLgtType = l<numLights ? g_vLightData[uIndex].lightType : 0;
        }

        //if(uLgtType!=SPOT_LIGHT && uLgtType!=SPHERE_LIGHT) ++l;
    }

    return ints;
}





#endif
