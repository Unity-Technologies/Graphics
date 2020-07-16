#ifndef UNIVERSAL_FORWARD_LIT_PASS_INCLUDED
#define UNIVERSAL_FORWARD_LIT_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Extraction.hlsl"

// keep this file in sync with LitGBufferPass.hlsl

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 texcoord     : TEXCOORD0;
    float2 lightmapUV   : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv                       : TEXCOORD0;
    DECLARE_LIGHTMAP_OR_SH(lightmapUV, vertexSH, 1);

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    float3 positionWS               : TEXCOORD2;
#endif

    float3 normalWS                 : TEXCOORD3;
#ifdef _NORMALMAP
    float4 tangentWS                : TEXCOORD4;    // xyz: tangent, w: sign
#endif
    float3 viewDirWS                : TEXCOORD5;

    half4 fogFactorAndVertexLight   : TEXCOORD6; // x: fogFactor, yzw: vertex light

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord              : TEXCOORD7;
#endif

    float4 positionCS               : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    inputData.positionWS = input.positionWS;
#endif

    half3 viewDirWS = SafeNormalize(input.viewDirWS);
#ifdef _NORMALMAP
    float sgn = input.tangentWS.w;      // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
    inputData.normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz));
#else
    inputData.normalWS = input.normalWS;
#endif

    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = viewDirWS;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif

    inputData.fogCoord = input.fogFactorAndVertexLight.x;
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
    inputData.bakedGI = SAMPLE_GI(input.lightmapUV, input.vertexSH, inputData.normalWS);
    inputData.normalizedScreenSpaceUV = input.positionCS.xy;
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Used in Standard (Physically Based) shader
Varyings LitPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

    // normalWS and tangentWS already normalize.
    // this is required to avoid skewing the direction during interpolation
    // also required for per-vertex lighting and SH evaluation
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    half3 viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
    half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

    // already normalized from normal transform to WS.
    output.normalWS = normalInput.normalWS;
    output.viewDirWS = viewDirWS;
#ifdef _NORMALMAP
    real sign = input.tangentOS.w * GetOddNegativeScale();
    output.tangentWS = half4(normalInput.tangentWS.xyz, sign);
#endif

    OUTPUT_LIGHTMAP_UV(input.lightmapUV, unity_LightmapST, output.lightmapUV);
    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    output.positionWS = vertexInput.positionWS;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif

    output.positionCS = vertexInput.positionCS;

    return output;
}


// Used in Standard (Physically Based) shader
float4 LitPassFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.uv, surfaceData);

    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);

    half4 color = UniversalFragmentPBR(inputData, surfaceData);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    color.a = OutputAlpha(color.a);

    return color;
}

Varyings ExtractionVertex(Attributes input,
    float2 uv2 : TEXCOORD2,
    float2 uv3 : TEXCOORD3,
    float2 uv4 : TEXCOORD4,
    float2 uv5 : TEXCOORD5,
    float2 uv6 : TEXCOORD6,
    float2 uv7 : TEXCOORD7)
{
    Varyings output = (Varyings)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

    // normalWS and tangentWS already normalize.
    // this is required to avoid skewing the direction during interpolation
    // also required for per-vertex lighting and SH evaluation
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    half3 viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

    // already normalized from normal transform to WS.
    output.normalWS = normalInput.normalWS;
    output.viewDirWS = viewDirWS;
#ifdef _NORMALMAP
    real sign = input.tangentOS.w * GetOddNegativeScale();
    output.tangentWS = half4(normalInput.tangentWS.xyz, sign);
#endif

    output.positionWS = vertexInput.positionWS;
    output.positionCS = vertexInput.positionCS;

    if (UNITY_DataExtraction_Space == 0)
        output.positionCS = float4(input.texcoord, 0.0F, 1.0f);
    else if (UNITY_DataExtraction_Space == 1)
        output.positionCS = float4(input.lightmapUV.xy, 0.0F, 1.0f);
    else if (UNITY_DataExtraction_Space == 2)
        output.positionCS = float4(uv2, 0.0F, 1.0f);
    else if (UNITY_DataExtraction_Space == 3)
        output.positionCS = float4(uv3, 0.0F, 1.0f);
    else if (UNITY_DataExtraction_Space == 4)
        output.positionCS = float4(uv4, 0.0F, 1.0f);
    else if (UNITY_DataExtraction_Space == 5)
        output.positionCS = float4(uv5, 0.0F, 1.0f);
    else if (UNITY_DataExtraction_Space == 6)
        output.positionCS = float4(uv6, 0.0F, 1.0f);
    else if (UNITY_DataExtraction_Space == 7)
        output.positionCS = float4(uv7, 0.0F, 1.0f);


    return output;
}

float4 ExtractionFragment(Varyings input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.uv, surfaceData);

    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);

    half4 color = UniversalFragmentPBR(inputData, surfaceData);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    color.a = OutputAlpha(color.a);


    float3 specular, diffuse, baseColor;
    float metallic;

    #ifdef _SPECULAR_SETUP
        specular = surfaceData.specular;
        diffuse = surfaceData.albedo;
        ConvertSpecularToMetallic(surfaceData.albedo, surfaceData.specular, baseColor, metallic);
    #else
        baseColor = surfaceData.albedo;
        metallic = surfaceData.metallic;
        ConvertMetallicToSpecular(surfaceData.albedo, surfaceData.metallic, diffuse, specular);
    #endif


    if(UNITY_DataExtraction_Mode == RENDER_OBJECT_ID)
        return asint(unity_LODFade.z);
    if(UNITY_DataExtraction_Mode == RENDER_DEPTH)
        return 0;
    if(UNITY_DataExtraction_Mode == RENDER_WORLD_NORMALS_FACE_RGB)
        return float4(input.normalWS, 1.0f);
    if(UNITY_DataExtraction_Mode == RENDER_WORLD_POSITION_RGB)
        return float4(inputData.positionWS, 1.0);
    if(UNITY_DataExtraction_Mode == RENDER_ENTITY_ID)
        return 0;
    if(UNITY_DataExtraction_Mode == RENDER_BASE_COLOR_RGBA)
        return float4(baseColor, surfaceData.alpha);
    if(UNITY_DataExtraction_Mode == RENDER_SPECULAR_RGB)
        return float4(specular, 1);
    if(UNITY_DataExtraction_Mode == RENDER_METALLIC_R)
        return float4(metallic, 0.0, 0.0, 1.0);
    if(UNITY_DataExtraction_Mode == RENDER_EMISSION_RGB)
        return float4(surfaceData.emission.xyz, 1.0);
    if(UNITY_DataExtraction_Mode == RENDER_WORLD_NORMALS_PIXEL_RGB)
        return float4(inputData.normalWS, 1.0f);
    if(UNITY_DataExtraction_Mode == RENDER_SMOOTHNESS_R)
        return float4(surfaceData.smoothness, 0.0, 0.0, 1.0);
    if(UNITY_DataExtraction_Mode == RENDER_OCCLUSION_R)
       return float4(surfaceData.occlusion, 0.0, 0.0, 1.0);
    if(UNITY_DataExtraction_Mode == RENDER_DIFFUSE_COLOR_RGBA)
       return float4(diffuse, 1.0);

    return 0;
}


#endif
