#ifndef SURFACE_FUNCTIONS
#define SURFACE_FUNCTIONS

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/CustomShading.hlsl"

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

#ifdef CUSTOM_FINAL_COLOR
    half4 CUSTOM_FINAL_COLOR(half4 inColor);
#else
    half4 CUSTOM_FINAL_COLOR(half4 inColor)
    {
        return inColor;        
    }
#endif

// Forward declaration of SurfaceFunction. This function must be implemented in the shader
void SurfaceFunction(Varyings IN, out CustomSurfaceData surfaceData);

#ifdef CUSTOM_VERTEX_FUNCTION
// Forward declaration of SurfaceFunction. This function must be implemented in the shader
void CUSTOM_VERTEX_FUNCTION(inout Attributes IN);
#else
void CUSTOM_VERTEX_FUNCTION(inout Attributes IN)
{}
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
    lightingData.light = GetMainLight(shadowCoord);
    lightingData.halfDirectionWS = normalize(lightingData.light.direction + viewDirectionWS);
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
    
    half3 finalColor = GlobalIlluminationFunction(surfaceData, environmentLighting, environmentReflections, viewDirectionWS);

    // main lighting
    finalColor += LightingFunction(surfaceData, lightingData, viewDirectionWS);

#ifdef _ADDITIONAL_LIGHTS
    uint pixelLightCount = GetAdditionalLightsCount();
    for (uint lightIndex = 0u; lightIndex < pixelLightCount; ++lightIndex)
    {
        lightingData.light = GetAdditionalLight(lightIndex, IN.positionWS);
        lightingData.halfDirectionWS = normalize(lightingData.light.direction + viewDirectionWS);
        lightingData.normalWS = surfaceData.normalWS;
        lightingData.NdotL = saturate(dot(surfaceData.normalWS, lightingData.light.direction));
        lightingData.NdotH = saturate(dot(surfaceData.normalWS, lightingData.halfDirectionWS));
        lightingData.LdotH = saturate(dot(lightingData.light.direction, lightingData.halfDirectionWS));
        finalColor += LightingFunction(surfaceData, lightingData, viewDirectionWS);
    }
#endif

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