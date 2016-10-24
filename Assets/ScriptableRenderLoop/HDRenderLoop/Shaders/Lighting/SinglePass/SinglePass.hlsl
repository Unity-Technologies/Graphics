//-----------------------------------------------------------------------------
// Single pass forward loop architecture
// It use  maxed list of lights of the scene - use just as proof of concept - do not used in regular game
//-----------------------------------------------------------------------------

// Use texture array for reflection
UNITY_DECLARE_TEXCUBEARRAY(_EnvTextures);

// Note: envIndex is whatever the lighting architecture want, it can contain information like in which texture to sample (in case we have a compressed BC6H texture and an uncompressed for real time reflection ?)
// EnvIndex can also be use to fetch in another array of struct (to  atlas information etc...).
float4 SampleEnv(int envIndex, float3 dirWS, float lod)
{
    return UNITY_SAMPLE_TEXCUBEARRAY_LOD(_EnvTextures, float4(dirWS, envIndex), lod);
}

/*
float SampleShadow(int shadowIndex)
{
	PunctualShadowData shadowData = _PunctualShadowList[shadowIndex];
	getShadowTextureSpaceCoordinate(shadowData.marix);
	shadowData
	return UNITY_SAMPLE_SHADOW(_ShadowMapAtlas, ...);
}
*/