#ifndef UNIVERSAL_PARTICLES_INCLUDED
#define UNIVERSAL_PARTICLES_INCLUDED
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ParticlesInstancing.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/Particles.hlsl"

struct ParticleParams
{
    float4 positionWS;
    float4 vertexColor;
    float4 projectedPosition;
    half4 baseColor;
    float3 blendUv;
    float2 uv;
};

void InitParticleParams(VaryingsParticle input, out ParticleParams output)
{
    output = (ParticleParams) 0;
    output.uv = input.texcoord;
    output.vertexColor = input.color;

    #if defined(_FLIPBOOKBLENDING_ON)
        output.blendUv = input.texcoord2AndBlend;
    #else
        output.blendUv = float3(0,0,0);
    #endif

    #if !defined(PARTICLES_EDITOR_META_PASS)
        output.positionWS = input.positionWS;
        output.baseColor = _BaseColor;

        #if defined(_SOFTPARTICLES_ON) || defined(_FADING_ON) || defined(_DISTORTION_ON)
            output.projectedPosition = input.projectedPosition;
        #else
            output.projectedPosition = float4(0,0,0,0);
        #endif
    #endif
}

// Pre-multiplied alpha helper
#if defined(_ALPHAPREMULTIPLY_ON)
    #define ALBEDO_MUL albedo
#else
    #define ALBEDO_MUL albedo.a
#endif

#if defined(_ALPHAPREMULTIPLY_ON)
    #define SOFT_PARTICLE_MUL_ALBEDO(albedo, val) albedo * val
#elif defined(_ALPHAMODULATE_ON)
    #define SOFT_PARTICLE_MUL_ALBEDO(albedo, val) half4(lerp(half3(1.0, 1.0, 1.0), albedo.rgb, albedo.a * val), albedo.a * val)
#else
    #define SOFT_PARTICLE_MUL_ALBEDO(albedo, val) albedo * half4(1.0, 1.0, 1.0, val)
#endif

// Color blending fragment function
half4 MixParticleColor(half4 baseColor, half4 particleColor, half4 colorAddSubDiff)
{
#if defined(_COLOROVERLAY_ON) // Overlay blend
    half4 output = baseColor;
    output.rgb = lerp(1 - 2 * (1 - baseColor.rgb) * (1 - particleColor.rgb), 2 * baseColor.rgb * particleColor.rgb, step(baseColor.rgb, 0.5));
    output.a *= particleColor.a;
    return output;
#elif defined(_COLORCOLOR_ON) // Color blend
    half3 aHSL = RgbToHsv(baseColor.rgb);
    half3 bHSL = RgbToHsv(particleColor.rgb);
    half3 rHSL = half3(bHSL.x, bHSL.y, aHSL.z);
    return half4(HsvToRgb(rHSL), baseColor.a * particleColor.a);
#elif defined(_COLORADDSUBDIFF_ON) // Additive, Subtractive and Difference blends based on 'colorAddSubDiff'
    half4 output = baseColor;
    output.rgb = baseColor.rgb + particleColor.rgb * colorAddSubDiff.x;
    output.rgb = lerp(output.rgb, abs(output.rgb), colorAddSubDiff.y);
    output.a *= particleColor.a;
    return output;
#else // Default to Multiply blend
    return baseColor * particleColor;
#endif
}

// Soft particles - returns alpha value for fading particles based on the depth to the background pixel
float SoftParticles(float near, float far, float4 projection)
{
    float fade = 1;
    if (near > 0.0 || far > 0.0)
    {
        float2 uv = UnityStereoTransformScreenSpaceTex(projection.xy / projection.w);
#if defined(UNITY_PRETRANSFORM_TO_DISPLAY_ORIENTATION)
        uv = RemovePretransformRotation(uv);
#endif
        uv = FoveatedRemapLinearToNonUniform(uv);

        float rawDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
        float sceneZ = (unity_OrthoParams.w == 0) ? LinearEyeDepth(rawDepth, _ZBufferParams) : LinearDepthToEyeDepth(rawDepth);
        float thisZ = LinearEyeDepth(projection.z / projection.w, _ZBufferParams);
        fade = saturate(far * ((sceneZ - near) - thisZ));
    }
    return fade;
}

// Soft particles - returns alpha value for fading particles based on the depth to the background pixel
float SoftParticles(float near, float far, ParticleParams params)
{
    float fade = 1;
    if (near > 0.0 || far > 0.0)
    {
        float2 uv = UnityStereoTransformScreenSpaceTex(params.projectedPosition.xy / params.projectedPosition.w);
#if defined(UNITY_PRETRANSFORM_TO_DISPLAY_ORIENTATION)
        uv = RemovePretransformRotation(uv);
#endif
        uv = FoveatedRemapLinearToNonUniform(uv);
        float rawDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, uv).r;
        float sceneZ = (unity_OrthoParams.w == 0) ? LinearEyeDepth(rawDepth, _ZBufferParams) : LinearDepthToEyeDepth(rawDepth);
        float thisZ = LinearEyeDepth(params.positionWS.xyz, GetWorldToViewMatrix());
        fade = saturate(far * ((sceneZ - near) - thisZ));
    }
    return fade;
}

// Camera fade - returns alpha value for fading particles based on camera distance
half CameraFade(float near, float far, float4 projection)
{
    float thisZ = LinearEyeDepth(projection.z / projection.w, _ZBufferParams);
    return half(saturate((thisZ - near) * far));
}

half3 AlphaModulateAndPremultiply(half3 albedo, half alpha)
{
#if defined(_ALPHAMODULATE_ON)
    return AlphaModulate(albedo, alpha);
#elif defined(_ALPHAPREMULTIPLY_ON)
    return AlphaPremultiply(albedo, alpha);
#endif
    return albedo;
}

half3 Distortion(float4 baseColor, float3 normal, half strength, half blend, float4 projection)
{
    float2 screenUV = (projection.xy / projection.w) + normal.xy * strength * baseColor.a;
    screenUV = UnityStereoTransformScreenSpaceTex(screenUV);
    float3 distortion = SampleSceneColor(screenUV);
    return half3(lerp(distortion, baseColor.rgb, saturate(baseColor.a - blend)));
}

// Sample a texture and do blending for texture sheet animation if needed
half4 BlendTexture(UnityTexture2D _Texture, float2 uv, float3 blendUv)
{
    half4 color = half4(SAMPLE_TEXTURE2D(_Texture.tex, _Texture.samplerstate, uv));
#ifdef _FLIPBOOKBLENDING_ON
    half4 color2 = half4(SAMPLE_TEXTURE2D(_Texture.tex, _Texture.samplerstate, blendUv.xy));
    color = lerp(color, color2, half(blendUv.z));
#endif
    return color;
}

// Sample a normal map in tangent space
half3 SampleNormalTS(float2 uv, float3 blendUv, UnityTexture2D bumpMap, half scale = half(1.0))
{
#if defined(_NORMALMAP)
    half4 n = BlendTexture(bumpMap, uv, blendUv);
    #if BUMP_SCALE_NOT_SUPPORTED
        return UnpackNormal(n);
    #else
        return UnpackNormalScale(n, scale);
    #endif
#else
    return half3(0.0, 0.0, 1.0);
#endif
}

#endif // UNIVERSAL_PARTICLES_INCLUDED
