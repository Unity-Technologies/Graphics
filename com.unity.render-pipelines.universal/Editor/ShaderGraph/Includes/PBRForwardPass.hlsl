void BuildInputData(Varyings input, SurfaceDescription surfaceDescription, out InputData inputData)
{
    inputData.positionWS = input.positionWS;

    #ifdef _NORMALMAP
        #if _NORMAL_DROPOFF_TS
            // IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
            float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
            float3 bitangent = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);
            inputData.normalWS = TransformTangentToWorld(surfaceDescription.NormalTS, half3x3(input.tangentWS.xyz, bitangent, input.normalWS.xyz));
        #elif _NORMAL_DROPOFF_OS
            inputData.normalWS = TransformObjectToWorldNormal(surfaceDescription.NormalOS);
        #elif _NORMAL_DROPOFF_WS
            inputData.normalWS = surfaceDescription.NormalWS;
        #endif
    #else
        inputData.normalWS = input.normalWS;
    #endif
    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = SafeNormalize(input.viewDirectionWS);

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        inputData.shadowCoord = input.shadowCoord;
    #elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
        inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
    #else
        inputData.shadowCoord = float4(0, 0, 0, 0);
    #endif

    inputData.fogCoord = input.fogFactorAndVertexLight.x;
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
    inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.sh, inputData.normalWS);
}

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
        half alpha = surfaceDescription.Alpha;
        clip(alpha - surfaceDescription.AlphaClipThreshold);
    #elif _SURFACE_TYPE_TRANSPARENT
        half alpha = surfaceDescription.Alpha;
    #else
        half alpha = 1;
    #endif

    InputData inputData;
    BuildInputData(unpacked, surfaceDescription, inputData);

    #ifdef _SPECULAR_SETUP
        float3 specular = surfaceDescription.Specular;
        float metallic = 1;
    #else
        float3 specular = 0;
        float metallic = surfaceDescription.Metallic;
    #endif

    half4 color = UniversalFragmentPBR(
			inputData,
			surfaceDescription.BaseColor,
			metallic,
			specular,
			surfaceDescription.Smoothness,
			surfaceDescription.Occlusion,
			surfaceDescription.Emission,
			alpha);

    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    return color;
}

half4 fragExtraction(PackedVaryings packedInput) : SV_TARGET
{
    Varyings unpacked = UnpackVaryings(packedInput);
    UNITY_SETUP_INSTANCE_ID(unpacked);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(unpacked);

    SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
    SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);

    #if _AlphaClip
        half alpha = surfaceDescription.Alpha;
        clip(alpha - surfaceDescription.AlphaClipThreshold);
    #elif _SURFACE_TYPE_TRANSPARENT
        half alpha = surfaceDescription.Alpha;
    #else
        half alpha = 1;
    #endif

    InputData inputData;
    BuildInputData(unpacked, surfaceDescription, inputData);

    #ifdef _SPECULAR_SETUP
        float3 specular = surfaceDescription.Specular;
        float metallic = 1;
    #else
        float3 specular = 0;
        float metallic = surfaceDescription.Metallic;
    #endif
    
    half4 color = UniversalFragmentPBR(
			inputData,
			surfaceDescription.BaseColor,
			metallic,
			specular,
			surfaceDescription.Smoothness,
			surfaceDescription.Occlusion,
			surfaceDescription.Emission,
			alpha);

    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    
    #ifdef RENDER_OBJECT_ID
    return asint(unity_LODFade.z);
    #elif defined(RENDER_DEPTH)
    return 0;
    #elif defined(RENDER_WORLD_NORMALS_FACE)
    return float4(unpacked.normalWS, 1.0f);
    #elif defined(RENDER_WORLD_NORMALS_PIXEL)
    return float4(inputData.normalWS, 1.0f);
    #elif defined(RENDER_WORLD_POSITION)
    return float4(inputData.positionWS, 1.0);
    #elif defined(RENDER_BASE_COLOR_ALPHA)
    return float4(surfaceDescription.BaseColor.xyz, alpha);
    #elif defined(RENDER_SPECULAR_METALLIC)
    return float4(specular.xyz, metallic);
    #elif defined(RENDER_EMISSION)
    return float4(surfaceDescription.Emission.xyz, 1.0);
    #elif defined(RENDER_SMOOTHNESS_OCCLUSION)
    return float4(surfaceDescription.Smoothness, surfaceDescription.Occlusion, 1.0, 1.0);
    #elif defined(RENDER_ENTITY_ID)
    return 0;
    #else
    return color;
    #endif
}
