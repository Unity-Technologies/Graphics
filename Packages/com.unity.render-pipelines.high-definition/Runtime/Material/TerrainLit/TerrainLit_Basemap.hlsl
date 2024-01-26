TEXTURE2D(_MainTex);
TEXTURE2D(_MetallicTex);
SAMPLER(sampler_MainTex);

void TerrainLitShade(float2 uv, inout TerrainLitSurfaceData surfaceData)
{
    float4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
    float4 metallicTex = SAMPLE_TEXTURE2D(_MetallicTex, sampler_MainTex, uv);
    surfaceData.albedo = mainTex.rgb;
    surfaceData.normalData = 0;
    surfaceData.smoothness = mainTex.a;
    surfaceData.metallic = metallicTex.r;
    surfaceData.ao = metallicTex.g;
}

void TerrainLitDebug(float2 uv, uint2 screenSpaceCoords, out float3 baseColor)
{
#ifdef DEBUG_DISPLAY
    baseColor = GET_TEXTURE_STREAMING_DEBUG_FOR_TERRAIN_TEX(screenSpaceCoords, uv, _MainTex);
#endif
}
