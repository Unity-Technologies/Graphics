#ifndef LIGHTWEIGHT_PASS_LIT_INCLUDED
#define LIGHTWEIGHT_PASS_LIT_INCLUDED

#include "LWRP/ShaderLibrary/InputSurface.hlsl"
#include "LWRP/ShaderLibrary/Lighting.hlsl"

struct LightweightVertexInput
{
    float4 vertex : POSITION;
    float3 normal : NORMAL;
    float4 tangent : TANGENT;
    float2 texcoord : TEXCOORD0;
    float2 lightmapUV : TEXCOORD1;
    UNITY_VERTEX_INPUT_INSTANCE_ID

};

struct LightweightVertexOutput
{
    float2 uv                       : TEXCOORD0;
    float4 lightmapUVOrVertexSH     : TEXCOORD1; // holds either lightmapUV or vertex SH. depending on LIGHTMAP_ON
    float3 posWS                    : TEXCOORD2;
    half3  normal                   : TEXCOORD3;

#ifdef _NORMALMAP
    half3 tangent                   : TEXCOORD4;
    half3 binormal                  : TEXCOORD5;
#endif

    half3 viewDir                   : TEXCOORD6;
    half4 fogFactorAndVertexLight   : TEXCOORD7; // x: fogFactor, yzw: vertex light

    float4 shadowCoord               : TEXCOORD8;

    float4 clipPos                  : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(LightweightVertexOutput IN, half3 normalTS, out InputData inputData)
{
    inputData.positionWS = IN.posWS.xyz;

#ifdef _NORMALMAP
    inputData.normalWS = TangentToWorldNormal(normalTS, IN.tangent, IN.binormal, IN.normal);
#else
    inputData.normalWS = normalize(IN.normal);
#endif

#ifdef SHADER_API_MOBILE
    // viewDirection should be normalized here, but we avoid doing it as it's close enough and we save some ALU.
    inputData.viewDirectionWS = IN.viewDir;
#else
    inputData.viewDirectionWS = normalize(IN.viewDir);
#endif

    inputData.shadowCoord = IN.shadowCoord;

    inputData.fogCoord = IN.fogFactorAndVertexLight.x;
    inputData.vertexLighting = IN.fogFactorAndVertexLight.yzw;
    inputData.bakedGI = SampleGI(IN.lightmapUVOrVertexSH, inputData.normalWS);
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Vertex: Used for Standard and StandardSimpleLighting shaders
LightweightVertexOutput LitPassVertex(LightweightVertexInput v)
{
    LightweightVertexOutput o = (LightweightVertexOutput)0;

    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_TRANSFER_INSTANCE_ID(v, o);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

    o.posWS = TransformObjectToWorld(v.vertex.xyz);
    o.clipPos = TransformWorldToHClip(o.posWS);
    o.viewDir = SafeNormalize(GetCameraPositionWS() - o.posWS);

    // initializes o.normal and if _NORMALMAP also o.tangent and o.binormal
    OUTPUT_NORMAL(v, o);

    // We either sample GI from lightmap or SH. lightmap UV and vertex SH coefficients
    // are packed in lightmapUVOrVertexSH to save interpolator.
    // The following funcions initialize
    OUTPUT_LIGHTMAP_UV(v.lightmapUV, unity_LightmapST, o.lightmapUVOrVertexSH);
    OUTPUT_SH(o.normal, o.lightmapUVOrVertexSH);

    half3 vertexLight = VertexLighting(o.posWS, o.normal);
    half fogFactor = ComputeFogFactor(o.clipPos.z);
    o.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
    o.shadowCoord = ComputeShadowCoord(o.clipPos);

    return o;
}

// Used for Standard shader
half4 LitPassFragment(LightweightVertexOutput IN) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(IN);

    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(IN.uv, surfaceData);

    InputData inputData;
    InitializeInputData(IN, surfaceData.normalTS, inputData);

    half4 color = LightweightFragmentPBR(inputData, surfaceData.albedo, surfaceData.metallic, surfaceData.specular, surfaceData.smoothness, surfaceData.occlusion, surfaceData.emission, surfaceData.alpha);

    ApplyFog(color.rgb, inputData.fogCoord);
    return color;
}

// Used for Standard shader
half4 LitPassFragmentNull(LightweightVertexOutput IN) : SV_Target
{
    LitPassFragment(IN);
    return 0;
}

// Used for StandardSimpleLighting shader
half4 LitPassFragmentSimple(LightweightVertexOutput IN) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(IN);

    float2 uv = IN.uv;
    half4 diffuseAlpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
    half3 diffuse = diffuseAlpha.rgb * _Color.rgb;

    half alpha = diffuseAlpha.a * _Color.a;
    AlphaDiscard(alpha, _Cutoff);
#ifdef _ALPHAPREMULTIPLY_ON
    diffuse *= alpha;
#endif

#ifdef _NORMALMAP
    half3 normalTS = Normal(uv);
#else
    half3 normalTS = half3(0, 0, 1);
#endif

    half3 emission = Emission(uv);
    half4 specularGloss = SpecularGloss(uv, diffuseAlpha.a);
    half shininess = _Shininess * 128.0h;

    InputData inputData;
    InitializeInputData(IN, normalTS, inputData);

    return LightweightFragmentBlinnPhong(inputData, diffuse, specularGloss, shininess, emission, alpha);
};


// Used for StandardSimpleLighting shader
half4 LitPassFragmentSimpleNull(LightweightVertexOutput IN) : SV_Target
{
    half4 result = LitPassFragmentSimple(IN);
    return result.a;
}

#endif
