#ifndef __DEFERREDLIGHTINGTEMPLATE_H__
#define __DEFERREDLIGHTINGTEMPLATE_H__


#include "UnityCG.cginc"
#include "UnityStandardBRDF.cginc"
#include "UnityStandardUtils.cginc"
#include "UnityPBSLighting.cginc"

//uniform uint g_nNumDirLights;

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

sampler2D _CameraGBufferTexture0;
sampler2D _CameraGBufferTexture1;
sampler2D _CameraGBufferTexture2;

// from LightDefinitions.cs.hlsl
#define SPOT_LIGHT (0)
#define SPHERE_LIGHT (1)
#define BOX_LIGHT (2)
#define DIRECTIONAL_LIGHT (3)

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

//#include "../../ShaderLibrary/Shadow/Shadow.hlsl"
//#include "../../fptl/ShadowContext.hlsl"
//#include "../../fptl/ShadowDispatch.hlsl"

CBUFFER_START(ShadowLightData)

float4 g_vShadow3x3PCFTerms0;
float4 g_vShadow3x3PCFTerms1;
float4 g_vShadow3x3PCFTerms2;
float4 g_vShadow3x3PCFTerms3;

float4 g_vDirShadowSplitSpheres[MAX_DIRECTIONAL_SPLIT];
float4x4 g_matWorldToShadow[MAX_SHADOW_LIGHTS * MAX_SHADOWMAP_PER_LIGHT];

CBUFFER_END
//---------------------------------------------------------------------------------------------------------------------------------------------------------

UNITY_DECLARE_DEPTH_TEXTURE(_CameraGBufferZ);

//debug
//float3 GetViewPosFromLinDepth(float2 v2ScrPos, float fLinDepth)
//{
//	float fSx = UNITY_MATRIX_P[0].x;
//	float fCx = UNITY_MATRIX_P[0].z;
//	float fSy = UNITY_MATRIX_P[1].y;
//	float fCy = UNITY_MATRIX_P[1].z;
//
//	return fLinDepth*float3( ((v2ScrPos.x-fCx)/fSx), ((v2ScrPos.y-fCy)/fSy), 1.0 );
//}

// --------------------------------------------------------
// Common lighting data calculation (direction, attenuation, ...)

void OnChipDeferredFragSetup (
	inout unity_v2f_deferred i,
	out float2 outUV,
	out float4 outVPos,
	out float3 outWPos,
	float depth
	)
{
	i.ray = i.ray * (_ProjectionParams.z / i.ray.z);
	float2 uv = i.uv.xy / i.uv.w;
	//float2 uv = i.pos.xy / float2(_ScreenParams.xy);

	// read depth and reconstruct world position
	// if we have framebuffer fetch, its expected depth was passed in the parameter from the framebuffer so no need to fetch
	#ifndef UNITY_FRAMEBUFFER_FETCH_AVAILABLE
		depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);
	#endif

	depth = Linear01Depth (depth);

	//debug
	//float linDepth = depth * (_ProjectionParams.z-_ProjectionParams.y) + _ProjectionParams.y;
	//float4 vpos = float4(GetViewPosFromLinDepth((2*uv-1)*float2(1, -1), linDepth), 1);

	float4 vpos = float4(i.ray * depth,1);
	float3 wpos = mul (unity_CameraToWorld, vpos).xyz;

	outUV = uv;
	outVPos = vpos;
	outWPos = wpos;
}

static float4 debugLighting;

int _LightIndexForShadowMatrixArray;

void OnChipDeferredCalculateLightParams (
	unity_v2f_deferred i,
	out float3 outWorldPos,
	out float2 outUV,
	out half3 outLightDir,
	out float outAtten,
	out float outFadeDist,
	out float4 outCookieColor,
	float depth
	)
{
	float4 vpos;
	float3 wpos;
	float2 uv; 
	float4 colorCookie = float4(1, 1, 1, 1);

	ShadowContext shadowContext = InitShadowContext();

	OnChipDeferredFragSetup(i, uv, vpos, wpos, depth); 

	// needed? old shadow code is commented out, we switched to new shadow code .. aka sampleShadow()
	float fadeDist = UnityComputeShadowFadeDistance(wpos, vpos.z);

	// spot light case
	#if defined (SPOT)	
		float3 tolight = _LightPos.xyz - wpos;
		half3 lightDir = normalize (tolight);
		
		float4 uvCookie = mul (unity_WorldToLight, float4(wpos,1));
		colorCookie = tex2Dlod (_LightTexture0, float4(uvCookie.xy / uvCookie.w, 0, 0));
		float atten = colorCookie.w;
		atten *= uvCookie.w < 0;

		float att = dot(tolight, tolight) * _LightPos.w;
		atten *= tex2D (_LightTextureB0, att.rr).UNITY_ATTEN_CHANNEL;

		if (_LightIndexForShadowMatrixArray >= 0)
			atten *= GetPunctualShadowAttenuation(shadowContext, wpos, 0.0.xxx, _LightIndexForShadowMatrixArray, 0.0.xxx);
	
	// directional light case		
	#elif defined (DIRECTIONAL) || defined (DIRECTIONAL_COOKIE)
		half3 lightDir = -_LightDir.xyz;
		float atten = 1.0;

		if (_LightIndexForShadowMatrixArray >= 0)
			atten *= GetDirectionalShadowAttenuation(shadowContext, wpos, 0.0.xxx, _LightIndexForShadowMatrixArray, 0.0.xxx);

		#if defined (DIRECTIONAL_COOKIE)
		colorCookie = tex2Dlod (_LightTexture0, float4(mul(unity_WorldToLight, half4(wpos,1)).xy, 0, 0));
		atten *= colorCookie.w;
		#endif //DIRECTIONAL_COOKIE

	// point light case	
	#elif defined (POINT) || defined (POINT_COOKIE)
		float3 tolight = wpos - _LightPos.xyz;
		half3 lightDir = -normalize (tolight);
		
		float att = dot(tolight, tolight) * _LightPos.w;
		float atten = tex2D (_LightTextureB0, att.rr).UNITY_ATTEN_CHANNEL;

		if (_LightIndexForShadowMatrixArray >= 0)
			atten *= GetPunctualShadowAttenuation(shadowContext, wpos, 0.0.xxx, _LightIndexForShadowMatrixArray, lightDir);

		#if defined (POINT_COOKIE)
			colorCookie = texCUBElod(_LightTexture0, float4(mul(unity_WorldToLight, float4(wpos,1)).xyz, 0));
			atten *= colorCookie.w;
		#endif //POINT_COOKIE	
	#else
		half3 lightDir = 0;
		float atten = 0;
	#endif

	outWorldPos = wpos;
	outUV = uv;
	outLightDir = lightDir;
	outAtten = atten;
	outFadeDist = fadeDist;
	outCookieColor = colorCookie;
}

#ifdef UNITY_FRAMEBUFFER_FETCH_AVAILABLE
half4 CalculateLight (unity_v2f_deferred i, inout half4 gbuffer0, inout half4 gbuffer1, inout half4 gbuffer2, inout float vpDepth)
#else
half4 CalculateLight (unity_v2f_deferred i)
#endif
{
	float3 wpos;
	float2 uv;
	float atten, fadeDist;
	float4 colorCookie = float4(1, 1, 1, 1);

	UnityLight light;
	UNITY_INITIALIZE_OUTPUT(UnityLight, light);

	//debugLighting = float4(0.0, 0.0, 0.0, 0.0);

#ifdef UNITY_FRAMEBUFFER_FETCH_AVAILABLE
	OnChipDeferredCalculateLightParams (i, wpos, uv, light.dir, atten, fadeDist, colorCookie, vpDepth);
#else
	OnChipDeferredCalculateLightParams (i, wpos, uv, light.dir, atten, fadeDist, colorCookie, 0.0);
#endif

	#if defined (POINT_COOKIE) || defined (DIRECTIONAL_COOKIE) || defined (SPOT)
		light.color = _LightColor.rgb * colorCookie.rgb * atten;
	#else
		light.color = _LightColor.rgb * atten;
	#endif

#ifndef UNITY_FRAMEBUFFER_FETCH_AVAILABLE
	// unpack Gbuffer
	half4 gbuffer0 = tex2D (_CameraGBufferTexture0, uv);
	half4 gbuffer1 = tex2D (_CameraGBufferTexture1, uv);
	half4 gbuffer2 = tex2D (_CameraGBufferTexture2, uv);
#endif
	UnityStandardData data = UnityStandardDataFromGbuffer(gbuffer0, gbuffer1, gbuffer2);

	float3 eyeVec = normalize(wpos-_WorldSpaceCameraPos);
	half oneMinusReflectivity = 1 - SpecularStrength(data.specularColor.rgb);

	UnityIndirect ind;
	UNITY_INITIALIZE_OUTPUT(UnityIndirect, ind);
	ind.diffuse = 0;
	ind.specular = 0;

	// UNITY_BRDF_PBS1 writes out alpha 1 to our emission alpha. 
    half4 res = UNITY_BRDF_PBS (data.diffuseColor, data.specularColor, oneMinusReflectivity, data.smoothness, data.normalWorld, -eyeVec, light, ind);

    //return debugLighting;

	return res;
}

unity_v2f_deferred onchip_vert_deferred (float4 vertex : POSITION, float3 normal : NORMAL)
{
    bool lightAsQuad = _LightAsQuad!=0.0;

    unity_v2f_deferred o;

    // scaling quad by two becuase built-in unity quad.fbx ranges from -0.5 to 0.5
    o.pos = lightAsQuad ? float4(2.0*vertex.xy, 0.5, 1.0) : UnityObjectToClipPos(vertex);
    o.uv = ComputeScreenPos(o.pos);

    // normal contains a ray pointing from the camera to one of near plane's
    // corners in camera space when we are drawing a full screen quad.
    // Otherwise, when rendering 3D shapes, use the ray calculated here.
    if (lightAsQuad){
    	float2 rayXY = mul(unity_CameraInvProjection, float4(o.pos.x, -o.pos.y, -1, 1)).xy;
        o.ray = float3(rayXY, 1.0);
    }
    else
    {
    	o.ray = UnityObjectToViewPos(vertex) * float3(-1,-1,1);
    }
    return o;
}


#endif
