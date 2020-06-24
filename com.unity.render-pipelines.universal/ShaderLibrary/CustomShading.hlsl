#ifndef CUSTOM_SHADING
#define CUSTOM_SHADING

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/BSDF.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;

    float2 uv           : TEXCOORD0;
#if LIGHTMAP_ON
    float2 uvLightmap   : TEXCOORD1;
#endif
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv                       : TEXCOORD0;
    float2 uvLightmap               : TEXCOORD1;
    float3 positionWS               : TEXCOORD2;
    half3  normalWS                 : TEXCOORD3;

#ifdef _NORMALMAP
    half4 tangentWS                 : TEXCOORD4;
#endif

    float4 positionCS               : SV_POSITION;
};

// User defined surface data.
struct CustomSurfaceData
{
    half3 diffuse;              // diffuse color. should be black for metals.
    half3 reflectance;          // reflectance color at normal indicence. It's monochromatic for dieletrics.
    half3 normalWS;             // normal in world space
    half  ao;                   // ambient occlusion
    half  roughness;            // roughness = perceptualRoughness * perceptualRoughness;
    half3 emission;             // emissive color
    half  alpha;                // 0 for transparent materials, 1.0 for opaque.
};

struct LightingData 
{
    Light light;
    half3 halfDirectionWS;
    half3 normalWS;
    half NdotL;
    half NdotH;
    half LdotH;
};

// Forward declaration of SurfaceFunction. This function must be implemented in the shader
void SurfaceFunction(Varyings IN, out CustomSurfaceData surfaceData);

#ifdef CUSTOM_VERTEX_FUNCTION
// Forward declaration of SurfaceFunction. This function must be implemented in the shader
void CUSTOM_VERTEX_FUNCTION(inout Attributes IN);
#else
void CUSTOM_VERTEX_FUNCTION(inout Attributes IN)
{}
#endif

// Convert normal from tangent space to space of TBN matrix
// f.ex, if normal and tangent are passed in world space, per-pixel normal will return in world space.
half3 GetPerPixelNormal(TEXTURE2D_PARAM(normalMap, sampler_NormalMap), float2 uv, half3 normal, half4 tangent)
{
    half3 bitangent = cross(normal, tangent.xyz) * tangent.w;
    half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(normalMap, sampler_NormalMap, uv));
    return normalize(mul(normalTS, half3x3(tangent.xyz, bitangent, normal)));
}

// Convert normal from tangent space to space of TBN matrix and apply scale to normal
half3 GetPerPixelNormalScaled(TEXTURE2D_PARAM(normalMap, sampler_NormalMap), float2 uv, half3 normal, half4 tangent, half scale)
{
    half3 bitangent = cross(normal, tangent.xyz) * tangent.w;
    half3 normalTS = UnpackNormalScale(SAMPLE_TEXTURE2D(normalMap, sampler_NormalMap, uv), scale);
    return normalize(mul(normalTS, half3x3(tangent.xyz, bitangent, normal)));
}

half V_Kelemen(half LoH)
{
    return 0.25 / (LoH * LoH);
}

// defined in latest URP
#if SHADER_LIBRARY_VERSION_MAJOR < 9
// Computes the world space view direction (pointing towards the viewer).
float3 GetWorldSpaceViewDir(float3 positionWS)
{
    if (unity_OrthoParams.w == 0)
    {
        // Perspective
        return _WorldSpaceCameraPos - positionWS;
    }
    else
    {
        // Orthographic
        float4x4 viewMat = GetWorldToViewMatrix();
        return viewMat[2].xyz;
    }
}
#endif

half3 EnvironmentBRDF(half3 f0, half roughness, half NdotV)
{
#if 1
    // Adapted from Unity Environment BDRF Approximation
    // mmikk
    half fresnelTerm = Pow4(1.0 - NdotV);
    half3 grazingTerm = saturate((1.0 - roughness) + f0);

    // surfaceReduction = Int D(NdotH) * NdotH * Id(NdotL>0) dH = 1/(roughness^2+1)
    half surfaceReduction = 1.0 / (roughness * roughness + 1.0);
    return lerp(f0, grazingTerm, fresnelTerm) * surfaceReduction;
#else
    // Brian Karis - Physically Based Shading in Mobile
    const half4 c0 = { -1, -0.0275, -0.572, 0.022 };
    const half4 c1 = { 1, 0.0425, 1.04, -0.04 };
    half4 r = roughness * c0 + c1;
    half a004 = min( r.x * r.x, exp2( -9.28 * NdotV ) ) * r.x + r.y;
    half2 AB = half2( -1.04, 1.04 ) * a004 + r.zw;
    return f0 * AB.x + AB.y;
    return half3(0, 0, 0);
#endif
}

#ifdef CUSTOM_FINAL_COLOR
    half4 CUSTOM_FINAL_COLOR(half4 inColor);
#else
    half4 CUSTOM_FINAL_COLOR(half4 inColor)
    {
        return inColor;        
    }
#endif

#ifdef CUSTOM_GI_FUNCTION
    half3 CUSTOM_GI_FUNCTION(CustomSurfaceData surfaceData, half3 environmentLighting, half3 environmentReflections, half3 viewDirectionWS);
#else
    half3 CUSTOM_GI_FUNCTION(CustomSurfaceData surfaceData, half3 environmentLighting, half3 environmentReflections, half3 viewDirectionWS)
    {
        half3 NdotV = saturate(dot(surfaceData.normalWS, viewDirectionWS)) + HALF_MIN;
        environmentReflections *= EnvironmentBRDF(surfaceData.reflectance, surfaceData.roughness, NdotV);
        environmentLighting = environmentLighting * surfaceData.diffuse;
        
        return (environmentReflections + environmentLighting) * surfaceData.ao;
    }
#endif

#ifdef CUSTOM_LIGHTING_FUNCTION
    half3 CUSTOM_LIGHTING_FUNCTION(CustomSurfaceData surfaceData, LightingData lightingData, half3 viewDirectionWS);
#else
    half3 CUSTOM_LIGHTING_FUNCTION(CustomSurfaceData surfaceData, LightingData lightingData, half3 viewDirectionWS)
    {
        half3 diffuse = surfaceData.diffuse * Lambert();
        
        // CookTorrance
        // inline D_GGX + V_SmithJoingGGX for better code generations
        half3 NdotV = saturate(dot(surfaceData.normalWS, viewDirectionWS)) + HALF_MIN;
        half DV = DV_SmithJointGGX(lightingData.NdotH, lightingData.NdotL, NdotV, surfaceData.roughness);
        
        // for microfacet fresnel we use H instead of N. In this case LdotH == VdotH, we use LdotH as it
        // seems to be more widely used convetion in the industry.
        half3 F = F_Schlick(surfaceData.reflectance, lightingData.LdotH);
        half3 specular = DV * F;
        half3 finalColor = (diffuse + specular) * lightingData.light.color * lightingData.NdotL;
        return finalColor;
    }
#endif

Varyings SurfaceVertex(Attributes IN)
{
    Varyings OUT;
    
    CUSTOM_VERTEX_FUNCTION(IN);

    // VertexPositionInputs contains position in multiple spaces (world, view, homogeneous clip space)
    // The compiler will strip all unused references.
    // Therefore there is more flexibility at no additional cost with this struct.
    VertexPositionInputs vertexInput = GetVertexPositionInputs(IN.positionOS.xyz);

    // Similar to VertexPositionInputs, VertexNormalInputs will contain normal, tangent and bitangent
    // in world space. If not used it will be stripped.
    VertexNormalInputs vertexNormalInput = GetVertexNormalInputs(IN.normalOS, IN.tangentOS);

    OUT.uv = IN.uv;
#if LIGHTMAP_ON
    OUT.uvLightmap = IN.uvLightmap.xy * unity_LightmapST.xy + unity_LightmapST.zw;
#endif

    OUT.positionWS = vertexInput.positionWS;
    OUT.normalWS = vertexNormalInput.normalWS;

#ifdef _NORMALMAP
    // tangentOS.w contains the normal sign used to construct mikkTSpace
    // We compute bitangent per-pixel to match convertion of Unity SRP.
    // https://medium.com/@bgolus/generating-perfect-normal-maps-for-unity-f929e673fc57
    OUT.tangentWS = float4(vertexNormalInput.tangentWS, IN.tangentOS.w * GetOddNegativeScale());
#endif

    OUT.positionCS = vertexInput.positionCS;
    return OUT;
}

float3 _LightDirection;
float4 GetShadowPositionHClip(Attributes input)
{
    float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
    float3 normalWS = TransformObjectToWorldNormal(input.normalOS);

    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));

#if UNITY_REVERSED_Z
    positionCS.z = min(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
#else
    positionCS.z = max(positionCS.z, positionCS.w * UNITY_NEAR_CLIP_VALUE);
#endif

    return positionCS;
}

Varyings SurfaceVertexShadowCaster(Attributes IN)
{
    Varyings OUT = SurfaceVertex(IN);
    OUT.positionCS = GetShadowPositionHClip(IN);
    return OUT;    
}

half4 CalculateColor(Varyings IN)
{
    CustomSurfaceData surfaceData;
    SurfaceFunction(IN, surfaceData);

    LightingData lightingData;

    half3 viewDirectionWS = normalize(GetWorldSpaceViewDir(IN.positionWS));
    half3 reflectionDirectionWS = reflect(-viewDirectionWS, surfaceData.normalWS);

    // shadowCoord is position in shadow light space
    float4 shadowCoord = TransformWorldToShadowCoord(IN.positionWS);
    Light light = GetMainLight(shadowCoord);
    lightingData.light = light;
    lightingData.halfDirectionWS = normalize(light.direction + viewDirectionWS);
    lightingData.normalWS = surfaceData.normalWS;
    lightingData.NdotL = saturate(dot(surfaceData.normalWS, lightingData.light.direction));
    lightingData.NdotH = saturate(dot(surfaceData.normalWS, lightingData.halfDirectionWS));
    lightingData.LdotH = saturate(dot(lightingData.light.direction, lightingData.halfDirectionWS));

    half3 environmentLighting = SAMPLE_GI(IN.uvLightmap, SampleSH(surfaceData.normalWS), surfaceData.normalWS);
    half3 environmentReflections = GlossyEnvironmentReflection(reflectionDirectionWS, surfaceData.roughness);

    // 0.089 perceptual roughness is the min value we can represent in fp16
    // to avoid denorm/division by zero as we need to do 1 / (pow(perceptualRoughness, 4)) in GGX
    surfaceData.roughness = max(surfaceData.roughness, 0.089);
    surfaceData.roughness = PerceptualRoughnessToRoughness(surfaceData.roughness);
    
    half3 finalColor = CUSTOM_GI_FUNCTION(surfaceData, environmentLighting, environmentReflections, viewDirectionWS);
    finalColor += CUSTOM_LIGHTING_FUNCTION(surfaceData, lightingData, viewDirectionWS);
    finalColor += surfaceData.emission;
    // TODO: fog? should it be applied as GI?
    
    return CUSTOM_FINAL_COLOR(half4(finalColor, surfaceData.alpha));
}

half4 SurfaceFragment(Varyings IN) : SV_Target
{
    return CalculateColor(IN);
}

half4 SurfaceFragmentDepthOnly(Varyings IN) : SV_Target
{
    CalculateColor(IN);
    return 0;
}

#endif