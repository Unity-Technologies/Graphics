//-----------------------------------------------------------------------------
// Single pass forward loop architecture
// It use  maxed list of lights of the scene - use just as proof of concept - do not used in regular game
//-----------------------------------------------------------------------------

struct LightLoopContext
{
    int sampleShadow;
    int sampleReflection;
};

//-----------------------------------------------------------------------------
// Shadow
// ----------------------------------------------------------------------------

#define SINGLE_PASS_CONTEXT_SAMPLE_SHADOWATLAS 0
#define SINGLE_PASS_CONTEXT_SAMPLE_SHADOWARRAY 1

//TEXTURE2D_ARRAY(_ShadowArray);
//SAMPLER2D_SHADOW(sampler_ShadowArray);
//SAMPLER2D(sampler_ManualShadowArray); // TODO: settings sampler individually is not supported in shader yet...

//TEXTURE2D(_ShadowAtlas);
//SAMPLER2D_SHADOW(sampler_ShadowAtlas);
//SAMPLER2D(sampler_ManualShadowAtlas); // TODO: settings sampler individually is not supported in shader yet...

TEXTURE2D(g_tShadowBuffer) // TODO: No choice, the name is hardcoded in ShadowrenderPass.cs for now. Need to change this!
SAMPLER2D_SHADOW(samplerg_tShadowBuffer);

// Use texture atlas for shadow map
StructuredBuffer<PunctualShadowData> _PunctualShadowList;

float3 GetShadowTextureCoordinate(LightLoopContext lightLoopContext, int index, float3 positionWS, float3 L)
{
    int faceIndex;
    if (_PunctualShadowList[index].shadowType == SHADOWTYPE_POINT)
    {        
        GetCubeFaceID(L, faceIndex);
    }

    // Note: scale and bias of shadow atlas are included in ShadowTransform
    float4x4 shadowTransform = _PunctualShadowList[index + faceIndex].worldToShadow;
    
    float4 positionTXS = mul(float4(positionWS, 1.0), shadowTransform);
    return positionTXS.xyz / positionTXS.w;
}

float SampleShadowCompare(LightLoopContext lightLoopContext, int index, float3 texCoord)
{
   // if (lightLoopContext.sampleShadow == SINGLE_PASS_CONTEXT_SAMPLE_SHADOWATLAS)
    {
        // Index could be use to get scale bias for uv but this is already merged into the shadow matrix
        return SAMPLE_TEXTURE2D_SHADOW(g_tShadowBuffer, samplerg_tShadowBuffer, texCoord);
    }
    /*
    else // SINGLE_PASS_CONTEXT_SAMPLE_SHADOWARRAY
    {
        return SAMPLE_TEXTURE2D_ARRAY_SHADOW(_ShadowArray, sampler_ShadowArray, texCoord, index);
    }
    */
}

/*
float SampleShadow(LightLoopContext lightLoopContext, int index, float2 texCoord)
{
    if (lightLoopContext.sampleShadow == SINGLE_PASS_CONTEXT_SAMPLE_SHADOWATLAS)
    {
        // Index could be use to get scale bias for uv but this is already merged into the shadow matrix
        return SAMPLE_TEXTURE2D(_ShadowAtlas, sampler_ManualShadowArray, texCoord);
    }
    else // SINGLE_PASS_CONTEXT_SAMPLE_SHADOWARRAY
    {
        return SAMPLE_TEXTURE2D_ARRAY(_ShadowArray, sampler_ManualShadowAtlas, texCoord, index);
    }
}
*/

//-----------------------------------------------------------------------------
// IES
// ----------------------------------------------------------------------------

#define SINGLE_PASS_CONTEXT_SAMPLE_IESARRAY 0

TEXTURE2D_ARRAY(_IESArray);
SAMPLER2D(sampler_IESArray);

// sphericalTexCoord is theta and phi spherical coordinate
float4 SampleIES(LightLoopContext lightLoopContext, int index, float2 sphericalTexCoord, float lod)
{
    return SAMPLE_TEXTURE2D_ARRAY_LOD(_IESArray, sampler_IESArray, sphericalTexCoord, index, 0);
}

//-----------------------------------------------------------------------------
// Reflection proble / Sky
// ----------------------------------------------------------------------------

// Use texture array for reflection
TEXTURECUBE_ARRAY(_EnvTextures);
SAMPLERCUBE(sampler_EnvTextures);

TEXTURECUBE(_SkyTexture);
SAMPLERCUBE(sampler_SkyTexture); // NOTE: Sampler could be share here with _EnvTextures. Don't know if the shader compiler will complain...

#define SINGLE_PASS_CONTEXT_SAMPLE_REFLECTION_PROBES 0
#define SINGLE_PASS_CONTEXT_SAMPLE_SKY 1

// Note: index is whatever the lighting architecture want, it can contain information like in which texture to sample (in case we have a compressed BC6H texture and an uncompressed for real time reflection ?)
// EnvIndex can also be use to fetch in another array of struct (to  atlas information etc...).
float4 SampleEnv(LightLoopContext lightLoopContext, int index, float3 texCoord, float lod)
{
    // This code will be inlined as lightLoopContext is hardcoded in the light loop
    if (lightLoopContext.sampleReflection == SINGLE_PASS_CONTEXT_SAMPLE_REFLECTION_PROBES)
    {
        return UNITY_SAMPLE_TEXCUBEARRAY_LOD(_EnvTextures, float4(texCoord, index), lod);
    }
    else // SINGLE_PASS_SAMPLE_SKY
    {
        return UNITY_SAMPLE_TEXCUBE_LOD(_SkyTexture, texCoord, lod);
    }
}
