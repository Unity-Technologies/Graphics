#ifndef FILE_FOREST_TERRAIN_COMMON_HLSL
#define FILE_FOREST_TERRAIN_COMMON_HLSL

#if defined(TE3_TERRAIN_LEGACY_ENHANCER)
	#include "Assets/_ExternalContent/ShaggyDogStudios/TerrainEvo3/Code/Shaders/TE3TerrainLegacyEnhancer.cginc"
#endif

SAMPLER(sampler_MainTex);
TEXTURE2D(_MainTex);

SAMPLER(sampler_MetallicTex);
TEXTURE2D(_MetallicTex);

CBUFFER_START(TerrainBase)
	float4	_MainTex_ST;
	float4	_Color;
CBUFFER_END


SAMPLER(sampler_Control);
TEXTURE2D(_Control);

#if OVERRIDE_TERRAIN_PROPERTIES
sampler2D _TerrainHeightMapOverride;

#if TERRAIN_SPLAT_FIRSTPASS
SAMPLER2D(sampler_ControlOverrideSplatFirstPass);
TEXTURE2D(_ControlOverrideSplatFirstPass);
#else
SAMPLER2D(sampler_ControlOverrideSplatAdd);
TEXTURE2D(_ControlOverrideSplatAdd);
#endif // TERRAIN_BASEPASS
#endif //OVERRIDE_TERRAIN_PROPERTIES

SAMPLER(sampler_Splat0);
TEXTURE2D(_Splat0);
TEXTURE2D(_Splat1);
TEXTURE2D(_Splat2);
TEXTURE2D(_Splat3);

SAMPLER(sampler_Normal0);
TEXTURE2D(_Normal0);
TEXTURE2D(_Normal1);
TEXTURE2D(_Normal2);
TEXTURE2D(_Normal3);

CBUFFER_START(TerrainSplat)
	float4	_Control_ST;
	float4	_Splat0_ST;
	float4	_Splat1_ST;
	float4	_Splat2_ST;
	float4	_Splat3_ST;
	float	_Metallic0;
	float	_Metallic1;
	float	_Metallic2;
	float	_Metallic3;
	float	_Smoothness0;
	float	_Smoothness1;
	float	_Smoothness2;
	float	_Smoothness3;
CBUFFER_END

struct TerrainVtxAttribs {
	float3 vertex	: POSITION;
	float3 normal	: NORMAL;
	float2 texcoord	: TEXCOORD0;
};

PackedVaryingsType TerrainSharedVert(TerrainVtxAttribs input) {
#if defined(TE3_TERRAIN_LEGACY_ENHANCER) && defined(TERRAIN_SPLAT_ADDPASS)
	return (PackedVaryingsType)0;
#endif

	VaryingsMeshType output;
	ZERO_INITIALIZE(VaryingsMeshType, output);

	float3 vertex = input.vertex;

#if OVERRIDE_TERRAIN_PROPERTIES
	float2 height = tex2Dlod(_TerrainHeightMapOverride, float4(input.texcoord, 0, 0)).rg;
	vertex.y = lerp(vertex.y, height.r, height.g);
#endif

	output.positionRWS = TransformObjectToWorld(vertex);
	output.positionCS = TransformWorldToHClip(output.positionRWS);

	// Terrain height override reuses this vertex function. The rest of the function is unnecessary for shadow pass
#if SHADERPASS != SHADERPASS_DEPTH_ONLY && SHADERPASS != SHADERPASS_SHADOWS

#if TERRAIN_BASEPASS
	output.texCoord0 = TRANSFORM_TEX(input.texcoord.xy, _MainTex);
#else
	output.color = TRANSFORM_TEX(input.texcoord.xy, _Control).xyxy;
	float2 correctUV = -input.texcoord.xy;
	output.texCoord0 = TRANSFORM_TEX(correctUV, _Splat0);
	output.texCoord1 = TRANSFORM_TEX(correctUV, _Splat1);
	output.texCoord2 = TRANSFORM_TEX(correctUV, _Splat2);
	output.texCoord3 = TRANSFORM_TEX(correctUV, _Splat3);
	#if defined(TE3_TERRAIN_LEGACY_ENHANCER)
		output.texCoord0 = correctUV;
	#endif
#endif

	float3 tangentOS = cross(input.normal, float3(0, 0, -1));
	output.tangentWS = float4(TransformObjectToWorldDir(tangentOS), -1.f);
	output.normalWS = TransformObjectToWorldNormal(input.normal);

#endif //SHADERPASS != SHADERPASS_DEPTH_ONLY && SHADERPASS != SHADERPASS_SHADOWS

	VaryingsType o;
	o.vmesh = output;
	return PackVaryingsType(o);
}

float GetTerrainBase(FragInputs input, out float3 diffuse, out float smoothness, out float metallic, out float3 normalTS) {
	float4 baseSmooth = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.texCoord0);
	diffuse = baseSmooth.rgb;
	smoothness = baseSmooth.a;
	metallic = SAMPLE_TEXTURE2D(_MetallicTex, sampler_MetallicTex, input.texCoord0).r;
	normalTS = float3(0, 0, 0);
	return 1;
}

#define SPLATDIFFUSESMOOTH(mixed, texname, samplername, texcoord, weight, smooth)		\
	float4 _splatSample##texname = SAMPLE_TEXTURE2D(texname, samplername, texcoord);	\
	mixed.rgb += _splatSample##texname.rgb * weight;									\
	mixed.a += _splatSample##texname.a * weight * smooth;

#define SPLATNORMAL(mixed, texname, samplername, texcoord, weight)						\
	float4 _splatSampleN##texname = SAMPLE_TEXTURE2D(texname, samplername, texcoord);\
	mixed += UnpackNormalmapRGorAG(_splatSampleN##texname) * weight;

float GetTerrainSplat(FragInputs input, out float3 diffuse, out float smoothness, out float metallic, out float3 normalTS) {

#if OVERRIDE_TERRAIN_PROPERTIES
#if TERRAIN_SPLAT_FIRSTPASS
	float4 splatControl = SAMPLE_TEXTURE2D(_ControlOverrideSplatFirstPass, sampler_ControlOverrideSplatFirstPass, input.color.xy);
#else
	float4 splatControl = SAMPLE_TEXTURE2D(_ControlOverrideSplatAdd, sampler_ControlOverrideSplatAdd, input.color.xy);
#endif // TERRAIN_FIRSTPASS
#else
	float4 splatControl = SAMPLE_TEXTURE2D(_Control, sampler_Control, input.color.xy);
#endif // OVERRIDE_TERRAIN_PROPERTIES

	float weight = dot(splatControl, 1.f);

#if defined(TERRAIN_SPLAT_ADDPASS)
	clip(weight == 0.0f ? -1 : 1);
#endif

	// Normalize weights before lighting and restore weights in final modifier functions so that the overall
	// lighting result can be correctly weighted.
	splatControl /= (weight + 1e-3f);

	float4 mixedDiffuseSmooth = float4(0, 0, 0, 0);
	SPLATDIFFUSESMOOTH(mixedDiffuseSmooth, _Splat0, sampler_Splat0, input.texCoord0, splatControl.r, _Smoothness0);
	SPLATDIFFUSESMOOTH(mixedDiffuseSmooth, _Splat1, sampler_Splat0, input.texCoord1, splatControl.g, _Smoothness1);
	SPLATDIFFUSESMOOTH(mixedDiffuseSmooth, _Splat2, sampler_Splat0, input.texCoord2, splatControl.b, _Smoothness2);
	SPLATDIFFUSESMOOTH(mixedDiffuseSmooth, _Splat3, sampler_Splat0, input.texCoord3, splatControl.a, _Smoothness3);
	diffuse = mixedDiffuseSmooth.rgb;
	smoothness = mixedDiffuseSmooth.a;
	metallic = dot(splatControl, float4(_Metallic0, _Metallic1, _Metallic2, _Metallic3));

	normalTS = float3(0, 0, weight < 1e-3f ? 1 : 0);
#ifdef _TERRAIN_NORMAL_MAP
	SPLATNORMAL(normalTS, _Normal0, sampler_Normal0, input.texCoord0, splatControl.r);
	SPLATNORMAL(normalTS, _Normal1, sampler_Normal0, input.texCoord1, splatControl.g);
	SPLATNORMAL(normalTS, _Normal2, sampler_Normal0, input.texCoord2, splatControl.b);
	SPLATNORMAL(normalTS, _Normal3, sampler_Normal0, input.texCoord3, splatControl.a);
	normalTS = normalize(normalTS);
#endif

	return weight;
}

#undef SPLATDIFFUSESMOOTH
#undef SPLATNORMAL

// This function convert the tangent space normal/tangent to world space and orthonormalize it + apply a correction of the normal if it is not pointing towards the near plane
void GetNormalAndTangentWS(FragInputs input, float3 V, float3 normalTS, inout float3 normalWS, inout float3 tangentWS)
{
    #ifdef SURFACE_GRADIENT
    normalWS = SurfaceGradientResolveNormal(input.worldToTangent[2], normalTS);
    #else
    // We need to normalize as we use mikkt tangent space and this is expected (tangent space is not normalize)
    normalWS = normalize(TransformTangentToWorld(normalTS, input.worldToTangent));
    #endif

    // Orthonormalize the basis vectors using the Gram-Schmidt process.
    // We assume that the length of the surface normal is sufficiently close to 1.
    // This is use with anisotropic material
    tangentWS = normalize(tangentWS - dot(tangentWS, normalWS) * normalWS);
}

SurfaceData GetSurfaceDataTerrain(FragInputs input, float3 V, inout PositionInputs posInput, out float weight, out float depthOffset, out float grassOcclusion) {
	float3 diffuse, normalTS;
	float occlusion, smoothness, metallic, displacement;
#if TERRAIN_BASEPASS
	weight = GetTerrainBase(input, diffuse, smoothness, metallic, normalTS);
	occlusion = 1.f;
	displacement = 1.f;
	depthOffset = 0.f;
#else
	#if defined(TE3_TERRAIN_LEGACY_ENHANCER)
		MixedLayers ml = FetchLayers(input.positionWS, input.worldToTangent, V, input.color.xy, input.texCoord0.xy);
		diffuse = ml.albedo;
		normalTS = ml.normalTS;
		occlusion = ml.occlusion;
		smoothness = ml.smoothness;
		metallic = ml.metallic;
		displacement = ml.displacement;
		weight = 1.f;
		depthOffset = ml.depthOffset;
	#else
		weight = GetTerrainSplat(input, diffuse, smoothness, metallic, normalTS);
		occlusion = 1.f;
		displacement = 1.f;
		depthOffset = 0.f;
	#endif
#endif

#ifdef _DEPTHOFFSET_ON
    ApplyDepthOffsetPositionInput(GetWorldSpaceNormalizeViewDir(GetCameraRelativePositionWS(input.positionWS)), depthOffset, GetWorldToHClipMatrix(), posInput);
#endif

	SurfaceData surfaceData;
	ZERO_INITIALIZE(SurfaceData, surfaceData);
	surfaceData.tangentWS = input.worldToTangent[0].xyz;
#ifdef _TERRAIN_NORMAL_MAP
	GetNormalAndTangentWS(input, V, normalTS, surfaceData.normalWS, surfaceData.tangentWS);
#else
	surfaceData.normalWS = input.worldToTangent[2].xyz;
#endif
    float NdotV;
    surfaceData.normalWS = GetViewReflectedNormal(surfaceData.normalWS, V, NdotV);
	surfaceData.baseColor = diffuse;
	surfaceData.perceptualSmoothness = smoothness;
	surfaceData.metallic = metallic;
	surfaceData.ambientOcclusion = occlusion;
	surfaceData.specularOcclusion = GetSpecularOcclusionFromAmbientOcclusion(NdotV, surfaceData.ambientOcclusion, PerceptualSmoothnessToRoughness(surfaceData.perceptualSmoothness));
    surfaceData.materialFeatures = 0; // standard
	surfaceData.skyOcclusion = SampleSkyOcclusion(input.positionRWS, input.color.xy, grassOcclusion);
	grassOcclusion = 1;
	surfaceData.treeOcclusion = 1;

	return surfaceData;
}

// From forward
// deviceDepth and linearDepth come directly from .zw of SV_Position
void UpdatePositionInput(float deviceDepth, float linearDepth, float3 positionWS, inout PositionInputs posInput)
{
    posInput.deviceDepth = deviceDepth;
    posInput.linearDepth = linearDepth;
    posInput.positionWS  = positionWS;
}

void GetBuiltinDataTerrain(FragInputs input, SurfaceData surfaceData, float alpha, float depthOffset, float grassOcclusion, out BuiltinData builtinData) 	{
	input.texCoord1 = input.texCoord2 = input.color.xy;
	GetBuiltinData(input, surfaceData, alpha, surfaceData.normalWS, depthOffset, grassOcclusion, builtinData);
}

#if SHADERPASS == SHADERPASS_GBUFFER
void TerrainSharedFrag(
	PackedVaryingsToPS packedInput,
	OUTPUT_GBUFFER(outGBuffer)
#ifdef _DEPTHOFFSET_ON
	, out float outputDepth : SV_Depth
#endif
) {
#if defined(TE3_TERRAIN_LEGACY_ENHANCER) && defined(TERRAIN_SPLAT_ADDPASS)
	clip(-1);
	return;
#endif

	//---------------------------
	// Standard frag prologue
	//
	FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw);
    UpdatePositionInput(input.positionSS.z, input.positionSS.w, input.positionRWS, posInput);
    float3 V = GetWorldSpaceNormalizeViewDir(input.positionRWS);


	//---------------------------
	// Custom Terrain splatting bit
	//
	float weight, depthOffset, grassOcclusion;
	SurfaceData surfaceData = GetSurfaceDataTerrain(input, V, posInput, weight, depthOffset, grassOcclusion);
	BuiltinData builtinData;
	GetBuiltinDataTerrain(input, surfaceData, 1.f, depthOffset, grassOcclusion, builtinData);


	//---------------------------
	// Standard frag epilogue
	//
	BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);
	PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);
	float3 bakeDiffuseLighting = GetBakedDiffuseLighting(surfaceData, builtinData, bsdfData, preLightData);

	ENCODE_INTO_GBUFFER(surfaceData, bakeDiffuseLighting, posInput.positionSS, outGBuffer);

	// Custom terrain splat weighing
	outGBuffer0 *= weight;
	outGBuffer1.rgb *= weight;
#if defined(TERRAIN_SPLAT_ADDPASS)
	outGBuffer1.a = 0;
#endif
	outGBuffer2 *= weight;
	outGBuffer3 *= weight;

#ifdef _DEPTHOFFSET_ON
	outputDepth = posInput.depthRaw;
#endif
}
#endif

#if SHADERPASS == SHADERPASS_LIGHT_TRANSPORT
PackedVaryingsToPS TerrainMetaVert(AttributesMesh inputMesh)
{
	VaryingsToPS output;

	// Output UV coordinate in vertex shader
	if (unity_MetaVertexControl.x)
	{
		inputMesh.positionOS.xy = inputMesh.uv1 * unity_LightmapST.xy + unity_LightmapST.zw;
		// OpenGL right now needs to actually use incoming vertex position,
		// so use it in a very dummy way
		//v.positionOS.z = vertex.z > 0 ? 1.0e-4 : 0.0;
	}
	if (unity_MetaVertexControl.y)
	{
		inputMesh.positionOS.xy = inputMesh.uv2 * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
		// OpenGL right now needs to actually use incoming vertex position,
		// so use it in a very dummy way
		//v.positionOS.z = vertex.z > 0 ? 1.0e-4 : 0.0;
	}

	float3 positionWS = TransformObjectToWorld(inputMesh.positionOS);
	output.vmesh.positionCS = TransformWorldToHClip(positionWS);
	output.vmesh.texCoord0 = inputMesh.uv0;
	output.vmesh.texCoord1 = inputMesh.uv1;

#if defined(VARYINGS_NEED_COLOR)
	output.vmesh.color = inputMesh.color;
#endif

	return PackVaryingsToPS(output);
}

float4 TerrainSharedFrag(PackedVaryingsToPS packedInput) : SV_Target
{
	//---------------------------
	// Standard frag prologue
	//
    FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput.vmesh);
    // input.unPositionSS is SV_Position
    PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, uint2(0, 0));
    // No position and depth in case of light transport
    float3 V = float3(0, 0, 1); // No vector view in case of light transport

	//---------------------------
	// Custom Terrain splatting bit
	//
	float weight, depthOffset, grassOcclusion;
	input.texCoord0.xy = TRANSFORM_TEX(input.texCoord0.xy, _MainTex);
	SurfaceData surfaceData = GetSurfaceDataTerrain(input, V, posInput, weight, depthOffset, grassOcclusion);
	BuiltinData builtinData;
	GetBuiltinDataTerrain(input, surfaceData, 1.f, depthOffset, grassOcclusion, builtinData);

	//---------------------------
	// Standard frag epilogue
	//
	BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);
    LightTransportData lightTransportData = GetLightTransportData(surfaceData, builtinData, bsdfData);

    // This shader is call two time. Once for getting emissiveColor, the other time to get diffuseColor
    // We use unity_MetaFragmentControl to make the distinction.

    float4 res = float4(0.0, 0.0, 0.0, 1.0);

    // TODO: No if / else in original code from Unity, why ? keep like original code but should be either diffuse or emissive
    if (unity_MetaFragmentControl.x)
    {
        // Apply diffuseColor Boost from LightmapSettings.
        // put abs here to silent a warning, no cost, no impact as color is assume to be positive.
		res.rgb = clamp(pow(abs(lightTransportData.diffuseColor), saturate(unity_OneOverOutputBoost)), 0, unity_MaxOutputValue);
    }
    
    if (unity_MetaFragmentControl.y)
    {
        // TODO: THIS LIMIT MUST BE REMOVE, IT IS NOT HDR, change when RGB9e5 is here.
        // Do we assume here that emission is [0..1] ?
        res = PackEmissiveRGBM(lightTransportData.emissiveColor);
    }

    return res;
}
#endif

#if SHADERPASS == SHADERPASS_DEPTH_ONLY || SHADERPASS == SHADERPASS_SHADOWS
void TerrainSharedFrag(PackedVaryingsToPS packedInput)
{
}
#endif //SHADERPASS == SHADERPASS_DEPTH_ONLY || SHADERPASS == SHADERPASS_SHADOWS

#if SHADERPASS == SHADERPASS_FORWARD
void TerrainSharedFrag(PackedVaryingsToPS packedInput,
    out float4 outColor : SV_Target0
#ifdef _DEPTHOFFSET_ON
    , out float outputDepth : SV_Depth
#endif
){
#if defined(TE3_TERRAIN_LEGACY_ENHANCER) && defined(TERRAIN_SPLAT_ADDPASS)
	clip(-1);
	return;
#endif

	//---------------------------
	// Standard frag prologue
	//
	FragInputs input = UnpackVaryingsMeshToFragInputs(packedInput);
	PositionInputs posInput = GetPositionInput(input.positionSS.xy, _ScreenSize.zw, uint2(0, 0));
	UpdatePositionInput(input.positionSS.z, input.positionSS.w, input.positionWS, posInput);
	float3 V = GetWorldSpaceNormalizeViewDir(input.positionWS);


	//---------------------------
	// Custom Terrain splatting bit
	//
	float weight, depthOffset, grassOcclusion;
	SurfaceData surfaceData = GetSurfaceDataTerrain(input, V, posInput, weight, depthOffset, grassOcclusion);
	BuiltinData builtinData;
	GetBuiltinDataTerrain(input, surfaceData, 1.f, depthOffset, grassOcclusion, builtinData);

	//---------------------------
	// Standard frag epilogue
	//
    BSDFData bsdfData = ConvertSurfaceDataToBSDFData(input.positionSS.xy, surfaceData);

    PreLightData preLightData = GetPreLightData(V, posInput, bsdfData);

    uint featureFlags = 0xFFFFFFFF;
    float3 diffuseLighting;
    float3 specularLighting;
    BakeLightingData bakeLightingData;
    bakeLightingData.bakeDiffuseLighting = GetBakedDiffuseLighting(surfaceData, builtinData, bsdfData, preLightData);
    LightLoop(V, posInput, preLightData, bsdfData, bakeLightingData, featureFlags, diffuseLighting, specularLighting);

    outColor = float4(diffuseLighting + specularLighting, builtinData.opacity);

#ifdef _DEPTHOFFSET_ON
    outputDepth = posInput.depthRaw;
#endif

	// Custom terrain splat weighing
	outColor *= weight;

#ifdef DEBUG_DISPLAY
    if (_DebugViewMaterial != 0)
    {
		float3 result = float3(1.0, 0.0, 1.0);
        bool needLinearToSRGB = false;

        GetVaryingsDataDebug(_DebugViewMaterial, input, result, needLinearToSRGB);
        GetBuiltinDataDebug(_DebugViewMaterial, builtinData, result, needLinearToSRGB);
        GetSurfaceDataDebug(_DebugViewMaterial, surfaceData, result, needLinearToSRGB);
        GetBSDFDataDebug(_DebugViewMaterial, bsdfData, result, needLinearToSRGB); // TODO: This required to initialize all field from BSDFData...

		// Sum of weights across first pass and add passes dips below 1 on splat borders,
		// so it doesn't work well with showing data that doesn't care about splats.
		/*if (_DebugViewMaterial == DEBUGVIEW_LIT_BSDFDATA_SKY_OCCLUSION || _DebugViewMaterial == DEBUGVIEW_LIT_SURFACEDATA_SKY_OCCLUSION)
		{
			weight = 1;
		#if TERRAIN_SPLAT_ADDPASS
			weight = 0;
		#endif
		}*/
		
		result *= weight;

        // TEMP!
        // For now, the final blit in the backbuffer performs an sRGB write
        // So in the meantime we apply the inverse transform to linear data to compensate.
        if (!needLinearToSRGB)
            result = SRGBToLinear(max(0, result));

        outColor = float4(result, 1.0);
    }
#endif
}
#endif //SHADERPASS == SHADERPASS_FORWARD

#endif //FILE_FOREST_TERRAIN_COMMON_HLSL