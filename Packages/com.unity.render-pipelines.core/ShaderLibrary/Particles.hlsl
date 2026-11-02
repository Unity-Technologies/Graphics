#ifndef CORE_PARTICLES_INCLUDED
#define CORE_PARTICLES_INCLUDED
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/ParticlesInstancing.hlsl"

///<funchints>
///     <sg:ProviderKey>Core.Particles.GetParticleColor</sg:ProviderKey>
///     <sg:DisplayName>Particle Color</sg:DisplayName>
///     <sg:ReturnDisplayName>Color</sg:ReturnDisplayName>
///     <sg:SearchCategory>VFX/Particles</sg:SearchCategory>
///     <sg:SearchName>Color</sg:SearchName>
///     <sg:SearchTerms>Particle, Color</sg:SearchTerms>
///</funchints>
///<paramhints name = "color">
///     <sg:DisplayName>Color</sg:DisplayName>
///     <VertexColor />
///</paramhints>
UNITY_EXPORT_REFLECTION
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

///<funchints>
///     <sg:ProviderKey>Core.Particles.GetParticleTexcoords</sg:ProviderKey>
///     <sg:DisplayName>Flipbook</sg:DisplayName>
///     <sg:GroupKey>Core.Particles.TexCoords, 10, Particle Texcoords, Mode</sg:GroupKey>
///     <sg:SearchCategory>VFX/Particles</sg:SearchCategory>
///     <sg:SearchName>Texcoords</sg:SearchName>
///     <sg:SearchTerms>Particle, Texcoords, UV</sg:SearchTerms>
///</funchints>
///<paramhints name = "inputTexcoord">
///     <sg:DisplayName>UV</sg:DisplayName>
///     <UV />
///     <Default>UV0</Default>
///</paramhints>
///<paramhints name = "outputTexcoord">
///     <sg:DisplayName>UV</sg:DisplayName>
///</paramhints>
UNITY_EXPORT_REFLECTION
void GetParticleTexcoords(out float2 outputTexcoord, in float2 inputTexcoord)
{
    float3 dummyTexcoord2AndBlend = 0.0;
    GetParticleTexcoords(outputTexcoord, dummyTexcoord2AndBlend, inputTexcoord.xyxy, 0.0);
}

///<funchints>
///     <sg:ProviderKey>Core.Particles.TexCoordsFlipbookBlending</sg:ProviderKey>
///     <sg:DisplayName>Flipbook Blending</sg:DisplayName>
///     <sg:GroupKey>Core.Particles.TexCoords</sg:GroupKey>
///     <sg:SearchCategory>VFX/Particles</sg:SearchCategory>
///     <sg:SearchTerms>Particle, Flipbook, Texcoords, UV</sg:SearchTerms>
///</funchints>
///<paramhints name = "inputTexcoord">
///     <sg:DisplayName>UV</sg:DisplayName>
///</paramhints>
///<paramhints name = "inputBlend">
///     <sg:DisplayName>Blend Vertex Stream</sg:DisplayName>
///</paramhints>
///<paramhints name = "outputTexcoord">
///     <sg:DisplayName>UV</sg:DisplayName>
///</paramhints>
///<paramhints name = "texcoord2">
///     <sg:DisplayName>UV2</sg:DisplayName>
///</paramhints>
///<paramhints name = "blend">
///     <sg:DisplayName>Blend</sg:DisplayName>
///</paramhints>
UNITY_EXPORT_REFLECTION
void GetParticleTexcoords(float4 inputTexcoord, float inputBlend, out float2 outputTexcoord, out float2 texcoord2, out float blend)
{
    float3 dummyTexcoord2AndBlend = 0.0;
#if defined(UNITY_PARTICLE_INSTANCING_ENABLED)
    GetParticleTexcoords(outputTexcoord, dummyTexcoord2AndBlend, inputTexcoord, inputBlend);
#else
    outputTexcoord = inputTexcoord.xy;
    dummyTexcoord2AndBlend = float3(inputTexcoord.zw, inputBlend);
#endif
    texcoord2 = dummyTexcoord2AndBlend.xy;
    blend = dummyTexcoord2AndBlend.z;
}

///<funchints>
///     <sg:ProviderKey>Core.Particles.AnimFrame</sg:ProviderKey>
///     <sg:DisplayName>Particle Anim Frame</sg:DisplayName>
///     <sg:SearchCategory>VFX/Particles</sg:SearchCategory>
///     <sg:SearchName>Anim Frame</sg:SearchName>
///     <sg:SearchTerms>Particle, Anim, Frame</sg:SearchTerms>
///</funchints>
UNITY_EXPORT_REFLECTION
void GetParticleAnimFrame(inout float animFrame)
{
#if defined(UNITY_PARTICLE_INSTANCING_ENABLED) && !defined(UNITY_PARTICLE_INSTANCE_DATA_NO_ANIM_FRAME)
    UNITY_PARTICLE_INSTANCE_DATA data = unity_ParticleInstanceData[unity_InstanceID];
    animFrame = data.animFrame;
#endif
}

#endif // CORE_PARTICLES_INCLUDED
