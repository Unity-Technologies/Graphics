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

PackedVaryings vertExtraction(
    Attributes input 
    /*float2 uv2 : TEXCOORD2,
    float2 uv3 : TEXCOORD3,
    float2 uv4 : TEXCOORD4,
    float2 uv5 : TEXCOORD5,
    float2 uv6 : TEXCOORD6,
    float2 uv7 : TEXCOORD7,
    float2 uv8 : TEXCOORD8 */    )
{
    Varyings output = (Varyings)0;
    output = BuildVaryings(input);
    PackedVaryings packedOutput = (PackedVaryings)0;
    packedOutput = PackVaryings(output);
    
  //  packedOutput.positionCS = float4(input.uv0.xy, 0.5f, 1.0f);
    /*#if defined(RENDER_SPACE_UV0)
    packedOutput.positionCS = float4(input.uv0.xyz, 1.0f);
    #elif defined(RENDER_SPACE_UV1)
    packedOutput.positionCS = input.uv1;
    #elif defined(RENDER_SPACE_UV2)
    packedOutput.positionCS = uv2;
    #elif defined(RENDER_SPACE_UV3)
    packedOutput.positionCS = uv3;
    #elif defined(RENDER_SPACE_UV4)
    packedOutput.positionCS = uv4;
    #elif defined(RENDER_SPACE_UV5)
    packedOutput.positionCS = uv5;
    #elif defined(RENDER_SPACE_UV6)
    packedOutput.positionCS = uv6;
    #elif defined(RENDER_SPACE_UV7)
    packedOutput.positionCS = uv7;
    #elif defined(RENDER_SPACE_UV8)
    packedOutput.positionCS = uv8;
    #endif
    */
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

int UNITY_DataExtraction_Mode;
int UNITY_DataExtraction_Space;

#define RENDER_OBJECT_ID 1
#define RENDER_DEPTH 2
#define RENDER_WORLD_NORMALS_FACE 3
#define RENDER_WORLD_POSITION 4
#define RENDER_ENTITY_ID 5
#define RENDER_BASE_COLOR 6
#define RENDER_SPECULAR 7
#define RENDER_METALLIC 8
#define RENDER_EMISSION 9
#define RENDER_WORLD_NORMALS_PIXEL 10
#define RENDER_SMOOTHNESS 11
#define RENDER_OCCLUSION 12

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
        float3 specular = surfaceDescription.Metallic;
        float metallic = 1;
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
    
    if(UNITY_DataExtraction_Mode == RENDER_OBJECT_ID)
        return asint(unity_LODFade.z);
    if(UNITY_DataExtraction_Mode == RENDER_DEPTH)
        return 0;
    if(UNITY_DataExtraction_Mode == RENDER_WORLD_NORMALS_FACE)
        return float4(unpacked.normalWS, 1.0f);
    if(UNITY_DataExtraction_Mode == RENDER_WORLD_POSITION)
        return float4(inputData.positionWS, 1.0);
    if(UNITY_DataExtraction_Mode == RENDER_ENTITY_ID)
        return 0;
    if(UNITY_DataExtraction_Mode == RENDER_BASE_COLOR)
        return float4(surfaceDescription.BaseColor.xyz, alpha);
    if(UNITY_DataExtraction_Mode == RENDER_SPECULAR)  
        return float4(specular.xyz, 1);
    if(UNITY_DataExtraction_Mode == RENDER_METALLIC)        
        return float4(metallic, 0.0, 0.0, 1.0);
    if(UNITY_DataExtraction_Mode == RENDER_EMISSION)
        return float4(surfaceDescription.Emission.xyz, 1.0);
    if(UNITY_DataExtraction_Mode == RENDER_WORLD_NORMALS_PIXEL)
        return float4(inputData.normalWS, 1.0f);
    if(UNITY_DataExtraction_Mode == RENDER_SMOOTHNESS)
        return float4(surfaceDescription.Smoothness, 0.0, 0.0, 1.0);
    if(UNITY_DataExtraction_Mode == RENDER_OCCLUSION)
       return float4(surfaceDescription.Occlusion, 0.0, 0.0, 1.0); 
    
    return color;
}
