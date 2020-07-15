#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Extraction.hlsl"

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
    inputData.normalizedScreenSpaceUV = input.positionCS.xy;
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

PackedVaryings vertExtraction(
    Attributes input,
    float2 uv2 : TEXCOORD2,
    float2 uv3 : TEXCOORD3,
    float2 uv4 : TEXCOORD4,
    float2 uv5 : TEXCOORD5,
    float2 uv6 : TEXCOORD6,
    float2 uv7 : TEXCOORD7)
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
    PackedVaryings packedOutput = (PackedVaryings)0;
    packedOutput = PackVaryings(output);

    if (UNITY_DataExtraction_Space == 0)
        packedOutput.positionCS = float4(input.uv0.xy, 0.0F, 1.0f);
    else if (UNITY_DataExtraction_Space == 1)
        packedOutput.positionCS = float4(input.uv1.xy, 0.0F,  1.0f);
    else if (UNITY_DataExtraction_Space == 2)
        packedOutput.positionCS = float4(uv2, 0.0F, 1.0f);
    else if (UNITY_DataExtraction_Space == 3)
        packedOutput.positionCS = float4(uv3, 0.0F, 1.0f);
    else if (UNITY_DataExtraction_Space == 4)
        packedOutput.positionCS = float4(uv4, 0.0F, 1.0f);
    else if (UNITY_DataExtraction_Space == 5)
        packedOutput.positionCS = float4(uv5, 0.0F, 1.0f);
    else if (UNITY_DataExtraction_Space == 6)
        packedOutput.positionCS = float4(uv6, 0.0F, 1.0f);
    else if (UNITY_DataExtraction_Space == 7)
        packedOutput.positionCS = float4(uv7, 0.0F, 1.0f);

    return packedOutput;
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

    float3 specular, diffuse, baseColor;
    float metallic;

    #ifdef _SPECULAR_SETUP
        specular = surfaceDescription.Specular;
        diffuse = surfaceDescription.BaseColor;
        ConvertSpecularToMetallic(surfaceDescription.BaseColor, surfaceDescription.Specular, baseColor, metallic);
    #else
        baseColor = surfaceDescription.BaseColor;
        metallic = surfaceDescription.Metallic;
        ConvertMetallicToSpecular(surfaceDescription.BaseColor, surfaceDescription.Specular, diffuse, specular);
    #endif

    //@TODO
    if(UNITY_DataExtraction_Mode == RENDER_OBJECT_ID)
        return asint(unity_LODFade.z);
    //@TODO
    if(UNITY_DataExtraction_Mode == RENDER_DEPTH)
        return 0;
    if(UNITY_DataExtraction_Mode == RENDER_WORLD_NORMALS_FACE_RGB)
        return float4(unpacked.normalWS, 1.0f);
    if(UNITY_DataExtraction_Mode == RENDER_WORLD_POSITION_RGB)
        return float4(inputData.positionWS, 1.0);
    if(UNITY_DataExtraction_Mode == RENDER_ENTITY_ID)
        return 0;
    if(UNITY_DataExtraction_Mode == RENDER_BASE_COLOR_RGBA)
        return float4(baseColor, alpha);
    if(UNITY_DataExtraction_Mode == RENDER_SPECULAR_RGB)
        return float4(specular, 1);
    if(UNITY_DataExtraction_Mode == RENDER_METALLIC_R)
        return float4(metallic, 0.0, 0.0, 1.0);
    if(UNITY_DataExtraction_Mode == RENDER_EMISSION_RGB)
        return float4(surfaceDescription.Emission.xyz, 1.0);
    if(UNITY_DataExtraction_Mode == RENDER_WORLD_NORMALS_PIXEL_RGB)
        return float4(inputData.normalWS, 1.0f);
    if(UNITY_DataExtraction_Mode == RENDER_SMOOTHNESS_R)
        return float4(surfaceDescription.Smoothness, 0.0, 0.0, 1.0);
    if(UNITY_DataExtraction_Mode == RENDER_OCCLUSION_R)
       return float4(surfaceDescription.Occlusion, 0.0, 0.0, 1.0);
    if(UNITY_DataExtraction_Mode == RENDER_DIFFUSE_COLOR_RGB)
       return float4(diffuse, 1.0);

    return 0;
}


