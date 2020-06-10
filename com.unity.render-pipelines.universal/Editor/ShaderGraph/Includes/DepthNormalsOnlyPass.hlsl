#ifndef SG_DEPTH_ONLY_PASS_INCLUDED
#define SG_DEPTH_ONLY_PASS_INCLUDED

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

    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);

    #if _AlphaClip
        clip(surfaceDescription.Alpha - surfaceDescription.AlphaClipThreshold);
    #endif

    // Retrieve the normal from the bump map or mesh normal
    #if defined(_NORMALMAP)
        #if _NORMAL_DROPOFF_TS
            // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
            float crossSign = (unpacked.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
            float3 bitangent = crossSign * cross(unpacked.normalWS.xyz, unpacked.tangentWS.xyz);
            float3 normal = TransformTangentToWorld(surfaceDescription.NormalTS, half3x3(unpacked.tangentWS.xyz, bitangent, unpacked.normalWS.xyz));
        #elif _NORMAL_DROPOFF_OS
            float3 normal = TransformObjectToWorldNormal(surfaceDescription.NormalOS);
        #elif _NORMAL_DROPOFF_WS
            float3 normal = surfaceDescription.NormalWS;
        #endif
    #else
        float3 normal = unpacked.normalWS;
    #endif
    normal = NormalizeNormalPerPixel(normal);

    return float4(PackNormalOctRectEncode(TransformWorldToViewDir(normal, true)), 0.0, 0.0);
}

#endif
