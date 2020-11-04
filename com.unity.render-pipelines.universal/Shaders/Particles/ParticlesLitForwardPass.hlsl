#ifndef UNIVERSAL_PARTICLES_FORWARD_LIT_PASS_INCLUDED
#define UNIVERSAL_PARTICLES_FORWARD_LIT_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Particles.hlsl"

void InitializeInputData(VaryingsParticle input, half3 normalTS, out InputData output)
{
    output = (InputData)0;

    output.positionWS = input.positionWS.xyz;

#ifdef _NORMALMAP
    half3 viewDirWS = half3(input.normalWS.w, input.tangentWS.w, input.bitangentWS.w);
    output.normalWS = TransformTangentToWorld(normalTS,
        half3x3(input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz));
#else
    half3 viewDirWS = input.viewDirWS;
    output.normalWS = input.normalWS;
#endif

    output.normalWS = NormalizeNormalPerPixel(output.normalWS);

#if SHADER_HINT_NICE_QUALITY
    viewDirWS = SafeNormalize(viewDirWS);
#endif

    output.viewDirectionWS = viewDirWS;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    output.shadowCoord = TransformWorldToShadowCoord(output.positionWS);
#else
    output.shadowCoord = float4(0, 0, 0, 0);
#endif

    output.fogCoord = (half)input.positionWS.w;
    output.vertexLighting = half3(0.0h, 0.0h, 0.0h);
    output.bakedGI = SampleSHPixel(input.vertexSH, output.normalWS);
    output.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.clipPos);
    output.shadowMask = half4(1, 1, 1, 1);
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

VaryingsParticle ParticlesLitVertex(AttributesParticle input)
{
    VaryingsParticle output = (VaryingsParticle)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normal, input.tangent);

    half3 viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
#if !SHADER_HINT_NICE_QUALITY
    viewDirWS = SafeNormalize(viewDirWS);
#endif

    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);
    half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);

#ifdef _NORMALMAP
    output.normalWS = half4(normalInput.normalWS, viewDirWS.x);
    output.tangentWS = half4(normalInput.tangentWS, viewDirWS.y);
    output.bitangentWS = half4(normalInput.bitangentWS, viewDirWS.z);
#else
    output.normalWS = normalInput.normalWS;
    output.viewDirWS = viewDirWS;
#endif

    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);

    output.positionWS.xyz = vertexInput.positionWS;
    output.positionWS.w = fogFactor;
    output.clipPos = vertexInput.positionCS;
    output.color = GetParticleColor(input.color);

#if defined(_FLIPBOOKBLENDING_ON)
#if defined(UNITY_PARTICLE_INSTANCING_ENABLED)
    GetParticleTexcoords(output.texcoord, output.texcoord2AndBlend, input.texcoords.xyxy, 0.0);
#else
    GetParticleTexcoords(output.texcoord, output.texcoord2AndBlend, input.texcoords, input.texcoordBlend);
#endif
#else
    GetParticleTexcoords(output.texcoord, input.texcoords.xy);
#endif

#if defined(_SOFTPARTICLES_ON) || defined(_FADING_ON) || defined(_DISTORTION_ON)
    output.projectedPosition = vertexInput.positionNDC;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif

    return output;
}

half4 ParticlesLitFragment(VaryingsParticle input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    ParticleParams particleParams;
    InitParticleParams(input, particleParams);

    SurfaceData surfaceData;
    InitializeParticleLitSurfaceData(particleParams, surfaceData);

    InputData inputData = (InputData)0;
    InitializeInputData(input, surfaceData.normalTS, inputData);

    half4 color = UniversalFragmentPBR(inputData, surfaceData);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    color.a = OutputAlpha(color.a, _Surface);

    return color;
}

#endif // UNIVERSAL_PARTICLES_FORWARD_LIT_PASS_INCLUDED
