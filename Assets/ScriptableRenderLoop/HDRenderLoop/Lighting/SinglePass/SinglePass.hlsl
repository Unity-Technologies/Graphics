//-----------------------------------------------------------------------------
// Single pass forward loop architecture
// It use  maxed list of lights of the scene - use just as proof of concept - do not used in regular game
//-----------------------------------------------------------------------------

//-----------------------------------------------------------------------------
// Constant and structure declaration
// ----------------------------------------------------------------------------


StructuredBuffer<DirectionalLightData>  _DirectionalLightList;
StructuredBuffer<DirectionalShadowData> _DirectionalShadowList;
StructuredBuffer<LightData>             _PunctualLightList;
StructuredBuffer<PunctualShadowData>    _PunctualShadowList;
StructuredBuffer<LightData>  	        _AreaLightList;
StructuredBuffer<EnvLightData>          _EnvLightList;

//TEXTURE2D_ARRAY(_ShadowArray);
//SAMPLER2D_SHADOW(sampler_ShadowArray);
//SAMPLER2D(sampler_ManualShadowArray); // TODO: settings sampler individually is not supported in shader yet...

// Use texture atlas for shadow map
//TEXTURE2D(_ShadowAtlas);
//SAMPLER2D_SHADOW(sampler_ShadowAtlas);
//SAMPLER2D(sampler_ManualShadowAtlas); // TODO: settings sampler individually is not supported in shader yet...
TEXTURE2D(g_tShadowBuffer) // TODO: No choice, the name is hardcoded in ShadowrenderPass.cs for now. Need to change this!
SAMPLER2D_SHADOW(samplerg_tShadowBuffer);

// Use texture array for IES
TEXTURE2D_ARRAY(_IESArray);
SAMPLER2D(sampler_IESArray);

// Use texture array for reflection
TEXTURECUBE_ARRAY(_EnvTextures);
SAMPLERCUBE(sampler_EnvTextures);

TEXTURECUBE(_SkyTexture);
SAMPLERCUBE(sampler_SkyTexture); // NOTE: Sampler could be share here with _EnvTextures. Don't know if the shader compiler will complain...

CBUFFER_START(UnityPerLightLoop)
    uint _DirectionalLightCount;
    uint _PunctualLightCount;
    uint _AreaLightCount;
    uint _EnvLightCount;
    EnvLightData _EnvLightSky;
	float4 _ShadowMapSize;
    float4 _DirShadowSplitSpheres[4]; // TODO share this max between C# and hlsl
CBUFFER_END

struct LightLoopContext
{
    int sampleShadow;
    int sampleReflection;
};

//-----------------------------------------------------------------------------
// Shadow sampling function
// ----------------------------------------------------------------------------

#define SINGLE_PASS_CONTEXT_SAMPLE_SHADOWATLAS 0
#define SINGLE_PASS_CONTEXT_SAMPLE_SHADOWARRAY 1

float GetPunctualShadowAttenuation(LightLoopContext lightLoopContext, float3 positionWS, int index, float3 L, float2 unPositionSS)
{
	int faceIndex = 0;
    if (_PunctualShadowList[index].lightType == GPULIGHTTYPE_POINT)
	{
		GetCubeFaceID(L, faceIndex);
	}

	PunctualShadowData shadowData = _PunctualShadowList[index + faceIndex];

	// Note: scale and bias of shadow atlas are included in ShadowTransform but could be apply here.
	float4 positionTXS = mul(float4(positionWS, 1.0), shadowData.worldToShadow);
	positionTXS.xyz /= positionTXS.w;
	//	positionTXS.z -=  shadowData.bias; // Apply a linear bias
	positionTXS.z -= 0.001;

#if UNITY_REVERSED_Z
	positionTXS.z = 1.0 - positionTXS.z;
#endif

	// float3 shadowPosDX = ddx_fine(positionTXS);
	// float3 shadowPosDY = ddy_fine(positionTXS);

	return SAMPLE_TEXTURE2D_SHADOW(g_tShadowBuffer, samplerg_tShadowBuffer, positionTXS);
}

// Gets the cascade weights based on the world position of the fragment and the positions of the split spheres for each cascade.
// Returns an invalid split index if past shadowDistance (ie 4 is invalid for cascade)
uint GetSplitSphereIndexForDirshadows(float3 positionWS, float4 dirShadowSplitSpheres[4])
{
    float3 fromCenter0 = positionWS.xyz - dirShadowSplitSpheres[0].xyz;
    float3 fromCenter1 = positionWS.xyz - dirShadowSplitSpheres[1].xyz;
    float3 fromCenter2 = positionWS.xyz - dirShadowSplitSpheres[2].xyz;
    float3 fromCenter3 = positionWS.xyz - dirShadowSplitSpheres[3].xyz;
    float4 distances2 = float4(dot(fromCenter0, fromCenter0), dot(fromCenter1, fromCenter1), dot(fromCenter2, fromCenter2), dot(fromCenter3, fromCenter3));

    float4 dirShadowSplitSphereSqRadii;
    dirShadowSplitSphereSqRadii.x = dirShadowSplitSpheres[0].w;
    dirShadowSplitSphereSqRadii.y = dirShadowSplitSpheres[1].w;
    dirShadowSplitSphereSqRadii.z = dirShadowSplitSpheres[2].w;
    dirShadowSplitSphereSqRadii.w = dirShadowSplitSpheres[3].w;

    float4 weights = float4(distances2 < dirShadowSplitSphereSqRadii);
    weights.yzw = saturate(weights.yzw - weights.xyz);

    return uint(4.0 - dot(weights, float4(4.0, 3.0, 2.0, 1.0)));
}

float GetDirectionalShadowAttenuation(LightLoopContext lightLoopContext, float3 positionWS, int index, float3 L, float2 unPositionSS)
{
    // Note Index is 0 for now, but else we need to provide the correct index in _DirShadowSplitSpheres and _DirectionalShadowList
    uint shadowSplitIndex = GetSplitSphereIndexForDirshadows(positionWS, _DirShadowSplitSpheres);

    DirectionalShadowData shadowData = _DirectionalShadowList[shadowSplitIndex];

    // Note: scale and bias of shadow atlas are included in ShadowTransform but could be apply here.
    float4 positionTXS = mul(float4(positionWS, 1.0), shadowData.worldToShadow);
    positionTXS.xyz /= positionTXS.w;
    //	positionTXS.z -=  shadowData.bias; // Apply a linear bias
    positionTXS.z -= 0.003;

#if UNITY_REVERSED_Z
    positionTXS.z = 1.0 - positionTXS.z;
#endif

    // float3 shadowPosDX = ddx_fine(positionTXS);
    // float3 shadowPosDY = ddy_fine(positionTXS);

    return SAMPLE_TEXTURE2D_SHADOW(g_tShadowBuffer, samplerg_tShadowBuffer, positionTXS);
}

//-----------------------------------------------------------------------------
// IES sampling function
// ----------------------------------------------------------------------------

#define SINGLE_PASS_CONTEXT_SAMPLE_IESARRAY 0

// sphericalTexCoord is theta and phi spherical coordinate
float4 SampleIES(LightLoopContext lightLoopContext, int index, float2 sphericalTexCoord, float lod)
{
    return SAMPLE_TEXTURE2D_ARRAY_LOD(_IESArray, sampler_IESArray, sphericalTexCoord, index, 0);
}

//-----------------------------------------------------------------------------
// Reflection proble / Sky sampling function
// ----------------------------------------------------------------------------

#define SINGLE_PASS_CONTEXT_SAMPLE_REFLECTION_PROBES 0
#define SINGLE_PASS_CONTEXT_SAMPLE_SKY 1

// Note: index is whatever the lighting architecture want, it can contain information like in which texture to sample (in case we have a compressed BC6H texture and an uncompressed for real time reflection ?)
// EnvIndex can also be use to fetch in another array of struct (to  atlas information etc...).
float4 SampleEnv(LightLoopContext lightLoopContext, int index, float3 texCoord, float lod)
{
    // This code will be inlined as lightLoopContext is hardcoded in the light loop
    if (lightLoopContext.sampleReflection == SINGLE_PASS_CONTEXT_SAMPLE_REFLECTION_PROBES)
    {
        return SAMPLE_TEXTURECUBE_ARRAY_LOD(_EnvTextures, sampler_EnvTextures, texCoord, index, lod);
    }
    else // SINGLE_PASS_SAMPLE_SKY
    {
        return SAMPLE_TEXTURECUBE_LOD(_SkyTexture, sampler_SkyTexture, texCoord, lod);
    }
}
