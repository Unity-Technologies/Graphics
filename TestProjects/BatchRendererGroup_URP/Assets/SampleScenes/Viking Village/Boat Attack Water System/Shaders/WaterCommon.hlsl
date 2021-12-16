#ifndef WATER_COMMON_INCLUDED
#define WATER_COMMON_INCLUDED

#define SHADOWS_SCREEN 0

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "WaterInput.hlsl"
#include "CommonUtilities.hlsl"
#include "GerstnerWaves.hlsl"
#include "WaterLighting.hlsl"

///////////////////////////////////////////////////////////////////////////////
//                  				Structs		                             //
///////////////////////////////////////////////////////////////////////////////

struct WaterVertexInput // vert struct
{
	float4	vertex 					: POSITION;		// vertex positions
	float2	texcoord 				: TEXCOORD0;	// local UVs
	UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct WaterVertexOutput // fragment struct
{
	float4	uv 						: TEXCOORD0;	// Geometric UVs stored in xy, and world(pre-waves) in zw
	float3	posWS					: TEXCOORD1;	// world position of the vertices
	half3 	normal 					: NORMAL;		// vert normals
	float3 	viewDir 				: TEXCOORD2;	// view direction
	float3	preWaveSP 				: TEXCOORD3;	// screen position of the verticies before wave distortion
	half2 	fogFactorNoise          : TEXCOORD4;	// x: fogFactor, y: noise
	float4	additionalData			: TEXCOORD5;	// x = distance to surface, y = distance to surface, z = normalized wave height, w = horizontal movement
	half4	shadowCoord				: TEXCOORD6;	// for ssshadows

	float4	clipPos					: SV_POSITION;
	UNITY_VERTEX_INPUT_INSTANCE_ID
	UNITY_VERTEX_OUTPUT_STEREO
};

///////////////////////////////////////////////////////////////////////////////
//          	   	       Water debug functions                             //
///////////////////////////////////////////////////////////////////////////////

half3 DebugWaterFX(half3 input, half4 waterFX, half screenUV)
{
    input = lerp(input, half3(waterFX.y, 1, waterFX.z), saturate(floor(screenUV + 0.7)));
    input = lerp(input, waterFX.xxx, saturate(floor(screenUV + 0.5)));
    half3 disp = lerp(0, half3(1, 0, 0), saturate((waterFX.www - 0.5) * 4));
    disp += lerp(0, half3(0, 0, 1), saturate(((1-waterFX.www) - 0.5) * 4));
    input = lerp(input, disp, saturate(floor(screenUV + 0.3)));
    return input;
}

///////////////////////////////////////////////////////////////////////////////
//          	   	      Water shading functions                            //
///////////////////////////////////////////////////////////////////////////////

half3 Scattering(half depth)
{
	return SAMPLE_TEXTURE2D(_AbsorptionScatteringRamp, sampler_AbsorptionScatteringRamp, half2(depth, 0.375h)).rgb;
}

half3 Absorption(half depth)
{
	return SAMPLE_TEXTURE2D(_AbsorptionScatteringRamp, sampler_AbsorptionScatteringRamp, half2(depth, 0.0h)).rgb;
}

float2 AdjustedDepth(half2 uvs, half4 additionalData)
{
	float rawD = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_ScreenTextures_linear_clamp, uvs);
	float d = LinearEyeDepth(rawD, _ZBufferParams);

	// TODO: Changing the usage of UNITY_REVERSED_Z this way to fix testing, but I'm not sure the original code is correct anyway.
	// In OpenGL, rawD should already have be remmapped before converting depth to linear eye depth.
#if UNITY_REVERSED_Z
	float offset = 0;
#else
	float offset = 1;
#endif
	
 	return float2(d * additionalData.x - additionalData.y, (rawD * -_ProjectionParams.x) + offset);
}

float WaterTextureDepth(float3 posWS)
{
    return (1 - SAMPLE_TEXTURE2D_LOD(_WaterDepthMap, sampler_WaterDepthMap_linear_clamp, posWS.xz * 0.002 + 0.5, 1).r) * (_MaxDepth + _VeraslWater_DepthCamParams.x) - _VeraslWater_DepthCamParams.x;
}

float3 WaterDepth(float3 posWS, half4 additionalData, half2 screenUVs)// x = seafloor depth, y = water depth
{
	float3 outDepth = 0;
	outDepth.xz = AdjustedDepth(screenUVs, additionalData);
	float wd = WaterTextureDepth(posWS);
	outDepth.y = wd + posWS.y;
	return outDepth;
}

half3 Refraction(half2 distortion, half depth, real depthMulti)
{
	half3 output = SAMPLE_TEXTURE2D_LOD(_CameraOpaqueTexture, sampler_CameraOpaqueTexture_linear_clamp, distortion, depth * 0.25).rgb;
	output *= Absorption((depth) * depthMulti);
	return output;
}

half2 DistortionUVs(half depth, float3 normalWS)
{
    half3 viewNormal = mul((float3x3)GetWorldToHClipMatrix(), -normalWS).xyz;

    return viewNormal.xz * saturate((depth) * 0.005);
}

half4 AdditionalData(float3 postionWS, WaveStruct wave)
{
    half4 data = half4(0.0, 0.0, 0.0, 0.0);
    float3 viewPos = TransformWorldToView(postionWS);
	data.x = length(viewPos / viewPos.z);// distance to surface
    data.y = length(GetCameraPositionWS().xyz - postionWS); // local position in camera space
	data.z = wave.position.y / _MaxWaveHeight * 0.5 + 0.5; // encode the normalized wave height into additional data
	data.w = wave.position.x + wave.position.z;
	return data;
}

WaterVertexOutput WaveVertexOperations(WaterVertexOutput input)
{
#if defined(_STATIC_WATER)
	float time = 0;
#else
	float time = _Time.y;
#endif

    input.normal = float3(0, 1, 0);
	input.fogFactorNoise.y = ((noise((input.posWS.xz * 0.5) + time) + noise((input.posWS.xz * 1) + time)) * 0.25 - 0.5) + 1;

	// Detail UVs
    input.uv.zw = input.posWS.xz * 0.1h + time * 0.05h + (input.fogFactorNoise.y * 0.1);
    input.uv.xy = input.posWS.xz * 0.4h - time.xx * 0.1h + (input.fogFactorNoise.y * 0.2);

	half4 screenUV = ComputeScreenPos(TransformWorldToHClip(input.posWS));
	screenUV.xyz /= screenUV.w;

    // shallows mask
    half waterDepth = WaterTextureDepth(input.posWS);
//custom-begin: Less shallows creep
	//input.posWS.y += pow(saturate((-waterDepth + 1.5) * 0.4), 2);
	input.posWS.y += pow(saturate((-waterDepth + 0.5) * 0.2), 2);
//custom-end:

	//Gerstner here
	WaveStruct wave;
	SampleWaves(input.posWS, saturate((waterDepth * 0.1 + 0.05)), wave);
	input.normal = wave.normal;
    input.posWS += wave.position;

#ifdef SHADER_API_PS4
	input.posWS.y -= 0.5;
#endif

    // Dynamic displacement
	half4 waterFX = SAMPLE_TEXTURE2D_LOD(_WaterFXMap, sampler_ScreenTextures_linear_clamp, screenUV.xy, 0);
	input.posWS.y += waterFX.w * 2 - 1;

	// After waves
	input.clipPos = TransformWorldToHClip(input.posWS);
	input.shadowCoord = ComputeScreenPos(input.clipPos);
    input.viewDir = SafeNormalize(_WorldSpaceCameraPos - input.posWS);

    // Fog
	input.fogFactorNoise.x = ComputeFogFactor(input.clipPos.z);
	input.preWaveSP = screenUV.xyz; // pre-displaced screenUVs

	// Additional data
	input.additionalData = AdditionalData(input.posWS, wave);

	// distance blend
	half distanceBlend = saturate(abs(length((_WorldSpaceCameraPos.xz - input.posWS.xz) * 0.005)) - 0.25);
	input.normal = lerp(input.normal, half3(0, 1, 0), distanceBlend);

	return input;
}

///////////////////////////////////////////////////////////////////////////////
//               	   Vertex and Fragment functions                         //
///////////////////////////////////////////////////////////////////////////////

// Vertex: Used for Standard non-tessellated water
WaterVertexOutput WaterVertex(WaterVertexInput v)
{
    WaterVertexOutput o;// = (WaterVertexOutput)0;
	UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
	UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.uv.xy = v.texcoord; // geo uvs
    o.posWS = TransformObjectToWorld(v.vertex.xyz);

	o = WaveVertexOperations(o);
    return o;
}

// Fragment for water
half4 WaterFragment(WaterVertexOutput IN) : SV_Target
{
	UNITY_SETUP_INSTANCE_ID(IN);
	half3 screenUV = IN.shadowCoord.xyz / IN.shadowCoord.w;//screen UVs

	half4 waterFX = SAMPLE_TEXTURE2D(_WaterFXMap, sampler_ScreenTextures_linear_clamp, IN.preWaveSP.xy);

	// Depth
	float3 depth = WaterDepth(IN.posWS, IN.additionalData, screenUV.xy);// TODO - hardcoded shore depth UVs
	//return half4(0, frac(ceil(depth.y) / _MaxDepth), frac(IN.posWS.y), 1);
	half depthMulti = 1 / _MaxDepth;

    // Detail waves
	half2 detailBump1 = SAMPLE_TEXTURE2D(_SurfaceMap, sampler_SurfaceMap, IN.uv.zw).xy * 2 - 1;
	half2 detailBump2 = SAMPLE_TEXTURE2D(_SurfaceMap, sampler_SurfaceMap, IN.uv.xy).xy * 2 - 1;
	half2 detailBump = (detailBump1 + detailBump2 * 0.5) * saturate(depth.x * 0.25 + 0.25);

	IN.normal += half3(detailBump.x, 0, detailBump.y) * _BumpScale;
//IN.normal += half3(1-waterFX.y, 0.5h, 1-waterFX.z) - 0.5;
	IN.normal = normalize(IN.normal);

    // Distortion
	half2 distortion = DistortionUVs(depth.x, IN.normal);
	distortion = screenUV.xy + distortion;// * clamp(depth.x, 0, 5);
	float d = depth.x;
	depth.xz = AdjustedDepth(distortion, IN.additionalData);
	distortion = depth.x < 0 ? screenUV.xy : distortion;
	depth.x = depth.x < 0 ? d : depth.x;

    // Fresnel
	half fresnelTerm = CalculateFresnelTerm(IN.normal, IN.viewDir.xyz);
//return fresnelTerm.xxxx;

//custom-begin: Fade out fresnel in shallows
    half waterDepth = WaterTextureDepth(IN.posWS);
	half vtxOffset = pow(saturate((-waterDepth + 0.5) * 0.2), 2);
	half fresnelFadeTerm = pow(saturate(vtxOffset * 16.f), 16.f);
	half fresnelFadeScale = pow(saturate(1.f - normalize(IN.viewDir).y), 2);
	fresnelFadeTerm = saturate(fresnelFadeTerm * fresnelFadeScale * 12);
	fresnelTerm = max(0.f, fresnelTerm - fresnelFadeTerm);
//custom-end:

	// Lighting
	Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.posWS));
    half shadow = SoftShadows(screenUV, IN.posWS, IN.viewDir.xyz, depth.x);
//custom-begin: Disabled
    half3 GI = half3(0,0,0);//SampleSH(IN.normal);
//custom-end:

    // SSS
    half3 directLighting = dot(mainLight.direction, half3(0, 1, 0)) * mainLight.color;
    directLighting += saturate(pow(dot(IN.viewDir, -mainLight.direction) * IN.additionalData.z, 3)) * 5 * mainLight.color;
    half3 sss = directLighting * shadow + GI;

	// Foam
	half3 foamMap = SAMPLE_TEXTURE2D(_FoamMap, sampler_FoamMap,  IN.uv.zw).rgb; //r=thick, g=medium, b=light
	half depthEdge = saturate(depth.x * 20);
	half waveFoam = saturate(IN.additionalData.z - 0.75 * 0.5); // wave tips
	half depthAdd = saturate(1 - depth.x * 4) * 0.5;
	half edgeFoam = saturate((1 - min(depth.x, depth.y) * 0.5 - 0.25) + depthAdd) * depthEdge;
	half foamBlendMask = max(max(waveFoam, edgeFoam), 0);//waterFX.r * 2);
	half3 foamBlend = SAMPLE_TEXTURE2D(_AbsorptionScatteringRamp, sampler_AbsorptionScatteringRamp, half2(foamBlendMask, 0.66)).rgb;
	half foamMask = saturate(length(foamMap * foamBlend) * 1.5 - 0.1);
//custom-begin: Fade out fresnel in shallows
	half foamFadeTerm = saturate(vtxOffset * 16.f);
	foamMask *= 1.f - foamFadeTerm;
//custom-end:

	// Foam lighting
	half3 foam = foamMask.xxx * (mainLight.shadowAttenuation * mainLight.color + GI);

    BRDFData brdfData;
    half alpha = 1;
    InitializeBRDFData(half3(0, 0, 0), 0, half3(1, 1, 1), 0.95, alpha, brdfData);
	half3 spec = DirectBDRF(brdfData, IN.normal, mainLight.direction, IN.viewDir) * shadow * mainLight.color;
//custom-begin: Fade out specular at the shoreline
	spec *= saturate(1.f - foamFadeTerm * 2);
//custom-end:
#ifdef _ADDITIONAL_LIGHTS
    uint pixelLightCount = GetAdditionalLightsCount();
    for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
    {
        Light light = GetAdditionalLight(lightIndex, IN.posWS);
        spec += LightingPhysicallyBased(brdfData, light, IN.normal, IN.viewDir);
        sss += light.distanceAttenuation * light.color;
    }
#endif

    sss *= Scattering(depth.x * depthMulti);

//custom-begin: flatten far reflections
	half3 reflNormalWS = IN.normal;
#if _REFLECTION_PLANARREFLECTION
	const half f = 0.02f;
	half3 viewVec = _WorldSpaceCameraPos - IN.posWS;
	reflNormalWS = lerp(IN.normal, normalize(float3(IN.normal.x * f , 1.f, IN.normal.z * f)), saturate(length(viewVec.xz) / 250.f));
#endif

	// Reflections
	half3 reflection = SampleReflections(reflNormalWS, IN.viewDir.xyz, screenUV.xy, 0.0);
//custom-end:

	// Refraction
	half3 refraction = Refraction(distortion, depth.x, depthMulti);

	// Do compositing
	half3 comp = lerp(lerp(refraction, reflection, fresnelTerm) + sss + spec, foam, foamMask); //lerp(refraction, color + reflection + foam, 1-saturate(1-depth.x * 25));

	// Fog
    float fogFactor = IN.fogFactorNoise.x;
    comp = MixFog(comp, fogFactor);
#if defined(_DEBUG_FOAM)
    return half4(foamMask.xxx, 1);
#elif defined(_DEBUG_SSS)
    return half4(sss, 1);
#elif defined(_DEBUG_REFRACTION)
    return half4(refraction, 1);
#elif defined(_DEBUG_REFLECTION)
    return half4(reflection, 1);
#elif defined(_DEBUG_NORMAL)
    return half4(IN.normal.x * 0.5 + 0.5, 0, IN.normal.z * 0.5 + 0.5, 1);
#elif defined(_DEBUG_FRESNEL)
    return half4(fresnelTerm.xxx, 1);
#elif defined(_DEBUG_WATEREFFECTS)
    return half4(waterFX);
#elif defined(_DEBUG_WATERDEPTH)
    return half4(frac(depth), 1);
#else
    return half4(comp, 1);
#endif
}

#endif // WATER_COMMON_INCLUDED