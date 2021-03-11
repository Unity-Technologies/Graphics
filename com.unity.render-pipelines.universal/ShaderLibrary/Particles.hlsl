#ifndef UNIVERSAL_PARTICLES_INCLUDED
#define UNIVERSAL_PARTICLES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ParticlesInstancing.hlsl"

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
        float rawDepth = SAMPLE_TEXTURE2D_X(_CameraDepthTexture, sampler_CameraDepthTexture, UnityStereoTransformScreenSpaceTex(projection.xy / projection.w)).r;
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
        float rawDepth = SampleSceneDepth(params.projectedPosition.xy / params.projectedPosition.w);
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

half3 AlphaModulate(half3 albedo, half alpha)
{
#if defined(_ALPHAMODULATE_ON)
    return lerp(half3(1.0h, 1.0h, 1.0h), albedo, alpha);
#elif defined(_ALPHAPREMULTIPLY_ON)
    return albedo * alpha;
#endif
    return albedo;
}

half3 Distortion(float4 baseColor, float3 normal, half strength, half blend, float4 projection)
{
    float2 screenUV = (projection.xy / projection.w) + normal.xy * strength * baseColor.a;
    screenUV = UnityStereoTransformScreenSpaceTex(screenUV);
    float4 Distortion = SAMPLE_TEXTURE2D_X(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV);
    return half3(lerp(Distortion.rgb, baseColor.rgb, saturate(baseColor.a - blend)));
}

// Sample a texture and do blending for texture sheet animation if needed
half4 BlendTexture(TEXTURE2D_PARAM(_Texture, sampler_Texture), float2 uv, float3 blendUv)
{
    half4 color = half4(SAMPLE_TEXTURE2D(_Texture, sampler_Texture, uv));
#ifdef _FLIPBOOKBLENDING_ON
    half4 color2 = half4(SAMPLE_TEXTURE2D(_Texture, sampler_Texture, blendUv.xy));
    color = lerp(color, color2, half(blendUv.z));
#endif
    return color;
}

// Sample a normal map in tangent space
half3 SampleNormalTS(float2 uv, float3 blendUv, TEXTURE2D_PARAM(bumpMap, sampler_bumpMap), half scale = half(1.0))
{
#if defined(_NORMALMAP)
    half4 n = BlendTexture(TEXTURE2D_ARGS(bumpMap, sampler_bumpMap), uv, blendUv);
    #if BUMP_SCALE_NOT_SUPPORTED
        return UnpackNormal(n);
    #else
        return UnpackNormalScale(n, scale);
    #endif
#else
    return half3(0.0, 0.0, 1.0);
#endif
}

half4 GetParticleColor(half4 color)
{
#if defined(UNITY_PARTICLE_INSTANCING_ENABLED)
#if !defined(UNITY_PARTICLE_INSTANCE_DATA_NO_COLOR)
    UNITY_PARTICLE_INSTANCE_DATA data = unity_ParticleInstanceData[unity_InstanceID];
    color = lerp(half4(1.0, 1.0, 1.0, 1.0), color, unity_ParticleUseMeshColors);
    color *= half4(UnpackFromR8G8B8A8(data.color));
#endif
#endif
    return color;
}

void GetParticleTexcoords(out float2 outputTexcoord, out float3 outputTexcoord2AndBlend, in float4 inputTexcoords, in float inputBlend)
{
#if defined(UNITY_PARTICLE_INSTANCING_ENABLED)
    if (unity_ParticleUVShiftData.x != 0.0)
    {
        UNITY_PARTICLE_INSTANCE_DATA data = unity_ParticleInstanceData[unity_InstanceID];

        float numTilesX = unity_ParticleUVShiftData.y;
        float2 animScale = unity_ParticleUVShiftData.zw;
#ifdef UNITY_PARTICLE_INSTANCE_DATA_NO_ANIM_FRAME
        float sheetIndex = 0.0;
#else
        float sheetIndex = data.animFrame;
#endif

        float index0 = floor(sheetIndex);
        float vIdx0 = floor(index0 / numTilesX);
        float uIdx0 = floor(index0 - vIdx0 * numTilesX);
        float2 offset0 = float2(uIdx0 * animScale.x, (1.0 - animScale.y) - vIdx0 * animScale.y); // Copied from built-in as is and it looks like upside-down flip

        outputTexcoord = inputTexcoords.xy * animScale.xy + offset0.xy;

#ifdef _FLIPBOOKBLENDING_ON
        float index1 = floor(sheetIndex + 1.0);
        float vIdx1 = floor(index1 / numTilesX);
        float uIdx1 = floor(index1 - vIdx1 * numTilesX);
        float2 offset1 = float2(uIdx1 * animScale.x, (1.0 - animScale.y) - vIdx1 * animScale.y);

        outputTexcoord2AndBlend.xy = inputTexcoords.xy * animScale.xy + offset1.xy;
        outputTexcoord2AndBlend.z = frac(sheetIndex);
#endif
    }
    else
#endif
    {
        outputTexcoord = inputTexcoords.xy;
#ifdef _FLIPBOOKBLENDING_ON
        outputTexcoord2AndBlend.xy = inputTexcoords.zw;
        outputTexcoord2AndBlend.z = inputBlend;
#endif
    }

#ifndef _FLIPBOOKBLENDING_ON
    outputTexcoord2AndBlend.xy = inputTexcoords.xy;
    outputTexcoord2AndBlend.z = 0.5;
#endif
}

void GetParticleTexcoords(out float2 outputTexcoord, in float2 inputTexcoord)
{
    float3 dummyTexcoord2AndBlend = 0.0;
    GetParticleTexcoords(outputTexcoord, dummyTexcoord2AndBlend, inputTexcoord.xyxy, 0.0);
}

#endif // UNIVERSAL_PARTICLES_INCLUDED
