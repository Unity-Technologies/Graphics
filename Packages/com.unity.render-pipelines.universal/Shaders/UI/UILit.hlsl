#ifndef UNIVERSAL_UI_LIT_INCLUDED
#define UNIVERSAL_UI_LIT_INCLUDED

// Shared code for the Universal Render Pipeline "UI/Lit/*" shaders.
// Reproduces the per-pixel Phong lighting of the former Built-in Render Pipeline UI/Lit surface shaders.

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

TEXTURE2D(_MainTex);    SAMPLER(sampler_MainTex);
TEXTURE2D(_MainBump);   SAMPLER(sampler_MainBump);
TEXTURE2D(_DetailTex);  SAMPLER(sampler_DetailTex);
TEXTURE2D(_DetailBump); SAMPLER(sampler_DetailBump);
TEXTURE2D(_Mask);       SAMPLER(sampler_Mask);
TEXTURE2D(_DetailMask); SAMPLER(sampler_DetailMask);

CBUFFER_START(UnityPerMaterial)
    float4 _MainTex_ST;
    float4 _MainBump_ST;
    float4 _DetailTex_ST;
    float4 _DetailBump_ST;
    float4 _Mask_ST;
    float4 _DetailMask_ST;
    float4 _DetailTex_TexelSize;
    float4 _DetailBump_TexelSize;
    float4 _DetailMask_TexelSize;
    half4 _Color;
    half4 _Specular;
    half _Shininess;
    half _Strength;
    half _Focus;
CBUFFER_END

// Set by the Canvas system rather than by the material
half4 _TextureSampleAdd;
float4 _ClipRect;
int _UIVertexColorAlwaysGammaSpace;

struct UILitAttributes
{
    float4 positionOS : POSITION;
    half4 color : COLOR;
    float2 uv0 : TEXCOORD0;
    float2 uv1 : TEXCOORD1;
    float3 normalOS : NORMAL;
    float4 tangentOS : TANGENT;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct UILitVaryings
{
    float4 positionCS : SV_POSITION;
    half4 color : COLOR;
    float4 uv : TEXCOORD0;          // xy: _MainTex, zw: _MainBump
    float4 detailUV : TEXCOORD1;    // xy: _DetailTex, zw: _DetailBump
    float4 maskUV : TEXCOORD2;      // xy: _DetailMask, zw: object space position for the clip rect
    float3 positionWS : TEXCOORD3;
    float3 normalWS : TEXCOORD4;
    float4 tangentWS : TEXCOORD5;
    float4 screenPos : TEXCOORD6;
#ifdef _ADDITIONAL_LIGHTS_VERTEX
    half3 vertexLighting : TEXCOORD7;
#endif
    UNITY_VERTEX_OUTPUT_STEREO
};

half3 UIGammaToLinear(half3 value)
{
    half3 low = 0.0849710 * value - 0.000163029;
    half3 high = value * (value * (value * 0.265885 + 0.736584) - 0.00980184) + 0.00319697;

    // We should be 0.5 away from any actual gamma value stored in an 8 bit channel
    const half3 split = (half3)0.0725490; // Equals 18.5 / 255
    return (value < split) ? low : high;
}

float UnityGet2DClipping(float2 position, float4 clipRect)
{
    float2 inside = step(clipRect.xy, position.xy) * step(position.xy, clipRect.zw);
    return inside.x * inside.y;
}

// Matches the Built-in UI/Lit refraction, which projected the object-space position rather than the clip-space one
// The Built-in GrabPass needed a flip where UVs start at the top, but the opaque texture follows the render texture convention on every API
float4 UILitRefractionCoords(float4 positionOS)
{
    return float4((positionOS.xy + positionOS.w) * 0.5, positionOS.zw);
}

UILitVaryings UILitVertex(UILitAttributes input)
{
    UILitVaryings output = (UILitVaryings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInputs = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    output.positionCS = positionInputs.positionCS;
    output.positionWS = positionInputs.positionWS;
    output.screenPos = UILitRefractionCoords(input.positionOS);
    output.normalWS = normalInputs.normalWS;
    output.tangentWS = float4(normalInputs.tangentWS, input.tangentOS.w * GetOddNegativeScale());
#ifdef _ADDITIONAL_LIGHTS_VERTEX
    output.vertexLighting = VertexLighting(positionInputs.positionWS, normalInputs.normalWS);
#endif

    half4 color = input.color;
#ifndef UNITY_COLORSPACE_GAMMA
    if (_UIVertexColorAlwaysGammaSpace)
        color.rgb = UIGammaToLinear(color.rgb);
#endif
    output.color = color * _Color;

    output.uv.xy = TRANSFORM_TEX(input.uv0, _MainTex);
    output.uv.zw = TRANSFORM_TEX(input.uv0, _MainBump);
    output.detailUV.xy = TRANSFORM_TEX(input.uv1 * _DetailTex_TexelSize.xy, _DetailTex);
    output.detailUV.zw = TRANSFORM_TEX(input.uv1 * _DetailBump_TexelSize.xy, _DetailBump);
    output.maskUV.xy = TRANSFORM_TEX(input.uv1 * _DetailMask_TexelSize.xy, _DetailMask);
    output.maskUV.zw = input.positionOS.xy;
    return output;
}

float3 UILitTangentToWorld(UILitVaryings input, half3 normalTS)
{
    float3 bitangentWS = input.tangentWS.w * cross(input.normalWS, input.tangentWS.xyz);
    float3x3 tangentToWorld = float3x3(input.tangentWS.xyz, bitangentWS, input.normalWS);
    return NormalizeNormalPerPixel(TransformTangentToWorld(normalTS, tangentToWorld));
}

half3 UILitPhong(half3 albedo, half specularIntensity, half gloss, float3 normalWS, float3 viewDirWS, Light light)
{
    half shininess = gloss * 250.0 + 4.0;
    half reflectiveFactor = max(0.0, dot(-viewDirWS, reflect(light.direction, normalWS)));
    half diffuseFactor = max(0.0, dot(normalWS, light.direction));
    half specularFactor = pow(reflectiveFactor, shininess) * specularIntensity;

    half3 color = (albedo * diffuseFactor + _Specular.rgb * specularFactor) * light.color;
    return color * light.distanceAttenuation * light.shadowAttenuation;
}

half3 UILitApplyRefraction(UILitVaryings input, half3 color, half3 normalTS, half amount)
{
    // Offset in screen texels like the Built-in GrabPass, since the opaque texture may be downsampled
    float2 offset = normalTS.xy * _ScreenSize.zw * _Focus;
    float4 proj = input.screenPos;
    proj.xy = offset * proj.z + proj.xy;
    return lerp(color, SampleSceneColor(proj.xy / proj.w), amount);
}

// Applies cookies without the shadows and height fog that the full URP light getters add
Light UILitGetMainLight(float3 positionWS)
{
    Light light = GetMainLight();
    // unity_LightData is per-renderer data that overlay canvases never receive, so its culling flag is ignored
    light.distanceAttenuation = 1.0;
    light.color *= SampleMainLightCookie(positionWS);
    return light;
}

Light UILitGetAdditionalLight(uint i, float3 positionWS)
{
#if USE_CLUSTER_LIGHT_LOOP
    int perObjectLightIndex = i;
#else
    int perObjectLightIndex = GetPerObjectLightIndex(i);
#endif
    Light light = GetAdditionalPerObjectLight(perObjectLightIndex, positionWS);
    light.color *= SampleAdditionalLightCookie(perObjectLightIndex, positionWS);
    return light;
}

half3 UILitShade(UILitVaryings input, half3 albedo, half specularIntensity, half gloss, float3 normalWS)
{
    float3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);

    // No ambient or light probe term is applied
    // Light layers are not matched because canvas draws carry no rendering layer mask
    half3 color = UILitPhong(albedo, specularIntensity, gloss, normalWS, viewDirWS, UILitGetMainLight(input.positionWS));

#if defined(_ADDITIONAL_LIGHTS)
    InputData inputData = (InputData)0;
    inputData.positionWS = input.positionWS;
    inputData.normalWS = normalWS;
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);

    uint pixelLightCount = GetAdditionalLightsCount();

#if USE_CLUSTER_LIGHT_LOOP
    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
    {
        color += UILitPhong(albedo, specularIntensity, gloss, normalWS, viewDirWS, UILitGetAdditionalLight(lightIndex, input.positionWS));
    }
#endif

    LIGHT_LOOP_BEGIN(pixelLightCount)
        color += UILitPhong(albedo, specularIntensity, gloss, normalWS, viewDirWS, UILitGetAdditionalLight(lightIndex, input.positionWS));
    LIGHT_LOOP_END
#endif

    // Per Vertex additional lights are diffuse only and use the interpolated vertex normal
#if defined(_ADDITIONAL_LIGHTS_VERTEX)
    color += albedo * input.vertexLighting;
#endif

    return color;
}

half4 UILitOutput(UILitVaryings input, half3 color, half alpha)
{
#ifdef UNITY_UI_CLIP_RECT
    alpha *= UnityGet2DClipping(input.maskUV.zw, _ClipRect);
#endif

#ifdef UNITY_UI_ALPHACLIP
    clip(alpha - 0.001);
#endif

    return half4(color, alpha);
}

#endif // UNIVERSAL_UI_LIT_INCLUDED
