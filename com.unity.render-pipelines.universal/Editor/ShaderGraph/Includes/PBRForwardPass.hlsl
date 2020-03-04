void BuildInputData(Varyings input, float3 normal, out InputData inputData)
{
    inputData.positionWS = input.positionWS;
#ifdef _NORMALMAP

#if _NORMAL_DROPOFF_TS
	// IMPORTANT! If we ever support Flip on double sided materials ensure bitangent and tangent are NOT flipped.
    float crossSign = (input.tangentWS.w > 0.0 ? 1.0 : -1.0) * GetOddNegativeScale();
    float3 bitangent = crossSign * cross(input.normalWS.xyz, input.tangentWS.xyz);
    inputData.normalWS = TransformTangentToWorld(normal, half3x3(input.tangentWS.xyz, bitangent, input.normalWS.xyz));
#elif _NORMAL_DROPOFF_OS
	inputData.normalWS = TransformObjectToWorldNormal(normal);
#elif _NORMAL_DROPOFF_WS
	inputData.normalWS = normal;
#endif
    
#else
    inputData.normalWS = input.normalWS;
#endif
    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = SafeNormalize(input.viewDirectionWS);

#if defined(MAIN_LIGHT_CALCULATE_SHADOWS)
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

    // Fields required by feature blocks are not currently generated
    // unless the corresponding data block is present
    // Therefore we need to predefine all potential data values.
    // Required fields should be tracked properly and generated.
    half3 baseColor = half3(0.5, 0.5, 0.5);
    half3 specular = half3(0.0, 0.0, 0.0);
    half metallic = 0;
    half smoothness = 0.5;
    half3 normal = half3(0.0, 0.0, 1.0);
    half occlusion = 1;
    half3 emission = half3(0.0, 0.0, 0.0);
    half alpha = 1;
    half clipThreshold = 0.5;

    #if defined(FEATURES_GRAPH_PIXEL)
        SurfaceDescriptionInputs surfaceDescriptionInputs = BuildSurfaceDescriptionInputs(unpacked);
        SurfaceDescription surfaceDescription = SurfaceDescriptionFunction(surfaceDescriptionInputs);

        // Data is overriden if the corresponding data block is present.
        // Could use "$Tag.Field: value = surfaceDescription.Field" pattern
        // to avoid preprocessors if this was a template file.
        #ifdef SURFACEDESCRIPTION_BASECOLOR
            baseColor = surfaceDescription.BaseColor;
        #endif
        #ifdef SURFACEDESCRIPTION_SPECULAR
            specular = surfaceDescription.Specular;
        #endif
        #ifdef SURFACEDESCRIPTION_METALLIC
            metallic = surfaceDescription.Metallic;
        #endif
        #ifdef SURFACEDESCRIPTION_SMOOTHNESS
            smoothness = surfaceDescription.Smoothness;
        #endif
        #ifdef SURFACEDESCRIPTION_NORMAL
            normal = surfaceDescription.Normal;
        #endif
        #ifdef SURFACEDESCRIPTION_OCCLUSION
            occlusion = surfaceDescription.Occlusion;
        #endif
        #ifdef SURFACEDESCRIPTION_EMISSION
            emission = surfaceDescription.Emission;
        #endif
        #ifdef SURFACEDESCRIPTION_ALPHA
            alpha = surfaceDescription.Alpha;
        #endif
        #ifdef SURFACEDESCRIPTION_CLIPTHRESHOLD
            clipThreshold = surfaceDescription.ClipThreshold;
        #endif
    #endif

    #if _AlphaClip
        clip(alpha - clipThreshold);
    #endif

    InputData inputData;
    BuildInputData(unpacked, normal, inputData);

    #ifdef _SPECULAR_SETUP
        metallic = 1;
    #else   
        specular = 0;
    #endif

    half4 color = UniversalFragmentPBR(
			inputData,
			baseColor,
			metallic,
			specular,
			smoothness,
			occlusion,
			emission,
			alpha); 

    color.rgb = MixFog(color.rgb, inputData.fogCoord); 
    return color;
}
