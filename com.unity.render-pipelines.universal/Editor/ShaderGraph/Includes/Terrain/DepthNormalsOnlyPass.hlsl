#ifndef SG_TERRAIN_DEPTH_NORMALS_PASS_INCLUDED
#define SG_TERRAIN_DEPTH_NORMALS_PASS_INCLUDED

PackedVaryings vert(Attributes input)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
    PackedVaryings packedOutput = (PackedVaryings)0;
    packedOutput = PackVaryings(output);
    return packedOutput;
}

half4 frag(PackedVaryings packedInput) : SV_TARGET
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);
    SurfaceDescription surfaceDescription = BuildSurfaceDescription(unpacked);

#ifdef _ALPHATEST_ON
    ClipHoles(unpacked.texCoord0.xy);
#endif

#if defined(_NORMALMAP) && !defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
    half3 tangentToWorld = half3x3(-unpacked.tangent.xyz, unpacked.bitangent.xyz, unpacked.normal.xyz);
    half3 normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
#elif defined(ENABLE_TERRAIN_PERPIXEL_NORMAL)
    float2 sampleCoords = (unpacked.texCoord0.xy / _TerrainHeightmapRecipSize.zw + 0.5f) * _TerrainHeightmapRecipSize.xy;
    half3 normalTS = half3(0.0h, 0.0h, 1.0h);
    half3 normalWS = TransformObjectToWorldNormal(normalize(SAMPLE_TEXTURE2D(_TerrainNormalmapTexture, sampler_TerrainNormalmapTexture, sampleCoords).rgb * 2 - 1));
    half3 tangentWS = cross(GetObjectToWorldMatrix()._13_23_33, normalWS);
    half3 normalWS = TransformTangentToWorld(normalTS, half3x3(-tangentWS, cross(normalWS, tangentWS), normalWS));
#else
    half3 normalWS = unpacked.normalWS;
#endif

    return half4(NormalizeNormalPerPixel(normalWS), 0.0);
}

#endif
