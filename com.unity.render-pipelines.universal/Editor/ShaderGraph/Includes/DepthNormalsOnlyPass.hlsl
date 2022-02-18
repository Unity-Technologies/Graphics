#ifndef SG_DEPTH_NORMALS_PASS_INCLUDED
#define SG_DEPTH_NORMALS_PASS_INCLUDED

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

    #if _ALPHATEST_ON
        clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
    #endif

    #if defined(_GBUFFER_NORMALS_OCT)
        float3 normalWS = normalize(unpacked.normalWS);
        float2 octNormalWS = PackNormalOctQuadEncode(normalWS);           // values between [-1, +1], must use fp32 on some platforms
        float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);   // values between [ 0,  1]
        half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);      // values between [ 0,  1]
        return half4(packedNormalWS, 0.0);
    #else
        // Retrieve the normal from the bump map or mesh normal
        #if defined(_NORMALMAP)
            #if _NORMAL_DROPOFF_TS
                // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
                float crossSign = (unpacked.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
                float3 bitangent = crossSign * cross(unpacked.normalWS.xyz, unpacked.tangentWS.xyz);
                float3 normalWS = TransformTangentToWorld(surfaceDescription.NormalTS, half3x3(unpacked.tangentWS.xyz, bitangent, unpacked.normalWS.xyz));
            #elif _NORMAL_DROPOFF_OS
                float3 normalWS = TransformObjectToWorldNormal(surfaceDescription.NormalOS);
            #elif _NORMAL_DROPOFF_WS
                float3 normalWS = surfaceDescription.NormalWS;
            #endif
        #else
            float3 normalWS = unpacked.normalWS;
        #endif

        return half4(NormalizeNormalPerPixel(normalWS), 0.0);
    #endif
}

#endif
