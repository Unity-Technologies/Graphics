#ifndef UNIVERSAL_PARTICLES_EDITOR_PASS_INCLUDED
#define UNIVERSAL_PARTICLES_EDITOR_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/Shaders/Particles/ParticlesInput.hlsl"
#include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Particles.hlsl"

float _ObjectId;
float _PassValue;
float4 _SelectionID;



///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

VaryingsParticle vertParticleEditor(AttributesParticle input)
{
    VaryingsParticle output = (VaryingsParticle)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

    output.clipPos = vertexInput.positionCS;
    output.color = GetParticleColor(input.color);

#if defined(_FLIPBOOKBLENDING_ON) && !defined(UNITY_PARTICLE_INSTANCING_ENABLED)
    GetParticleTexcoords(output.texcoord, output.texcoord2AndBlend, input.texcoords, input.texcoordBlend);
#else
    GetParticleTexcoords(output.texcoord, input.texcoords.xy);
#endif

    return output;
}

void fragParticleSceneClip(VaryingsParticle input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float2 uv = input.texcoord;
    float3 blendUv = float3(0, 0, 0);
#if defined(_FLIPBOOKBLENDING_ON)
    blendUv = input.texcoord2AndBlend;
#endif

    float4 projectedPosition = float4(0, 0, 0, 0);

    half4 albedo = SampleAlbedo(uv, blendUv, _BaseColor, input.color, projectedPosition, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    half alpha = albedo.a;

#ifdef _ALPHATEST_ON
    clip(alpha - _Cutoff);
#endif
}

half4 fragParticleSceneHighlight(VaryingsParticle input) : SV_Target
{
    fragParticleSceneClip(input);
    return float4(_ObjectId, _PassValue, 1, 1);
}

half4 fragParticleScenePicking(VaryingsParticle input) : SV_Target
{
    fragParticleSceneClip(input);
    return _SelectionID;
}

#endif // UNIVERSAL_PARTICLES_EDITOR_PASS_INCLUDED
