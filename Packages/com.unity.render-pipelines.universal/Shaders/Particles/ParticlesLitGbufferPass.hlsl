#ifndef UNIVERSAL_PARTICLES_GBUFFER_LIT_PASS_INCLUDED
#define UNIVERSAL_PARTICLES_GBUFFER_LIT_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/GBufferOutput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Particles/ParticlesLitFeatures.hlsl"

// Realtime shadows are never sampled when Receive Shadows is off at compile time;
// drop the vertex shadow-coord interpolator.
#if _RECEIVE_SHADOWS_OFF_STATICALLY_ENABLED
    #undef USE_VERTEX_SHADOW_COORD_INTERPOLATOR
    #define USE_VERTEX_SHADOW_COORD_INTERPOLATOR 0
#endif

void InitializeInputData(VaryingsParticle input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;
    inputData.preExposureMultiplier = GetPreExposureMultiplier();

    inputData.positionWS = input.positionWS.xyz;
    inputData.positionCS = input.clipPos;

#if FEATURES_NORMALMAP
    half3 viewDirWS = half3(input.normalWS.w, input.tangentWS.w, input.bitangentWS.w);
    if (UseNormalMap())
    {
        inputData.normalWS = TransformTangentToWorld(normalTS,
            half3x3(input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz));
    }
    else
    {
        inputData.normalWS = input.normalWS.xyz;
    }
#else
    half3 viewDirWS = input.viewDirWS;
    inputData.normalWS = input.normalWS;
#endif

    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS, UseNormalMap());

    viewDirWS = SafeNormalize(viewDirWS);

    inputData.viewDirectionWS = viewDirWS;

#if USE_VERTEX_SHADOW_COORD_INTERPOLATOR
    inputData.shadowCoord = ShadowCoordInterpolatorAvailable() ? input.shadowCoord : TransformWorldToShadowCoord(inputData.positionWS, IsSurfaceTypeTransparent());
#else
    inputData.shadowCoord = MainLightShadowsAvailable() ? TransformWorldToShadowCoord(inputData.positionWS, IsSurfaceTypeTransparent()) : float4(0, 0, 0, 0);
#endif

    inputData.fogCoord = 0.0; // not used for deferred shading
    inputData.vertexLighting = half3(0.0h, 0.0h, 0.0h);
    GIParams giParams = (GIParams)0;
#ifdef USE_APV_PROBE_OCCLUSION
    giParams.vertexProbeOcclusion = input.probeOcclusion;
#endif
    giParams.vertexSH = input.vertexSH;
    giParams.positionWS = inputData.positionWS;
    giParams.normalWS = inputData.normalWS;
    giParams.viewDirWS = inputData.viewDirectionWS;
    giParams.positionSS = inputData.positionCS.xy;
    half4 unusedShadowMask;
    InitializeBakedGI(giParams, inputData.bakedGI, unusedShadowMask);
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.clipPos);
    inputData.shadowMask = half4(1, 1, 1, 1);

#if defined(DEBUG_DISPLAY) && defined(USE_APV_PROBE_OCCLUSION)
    inputData.probeOcclusion = input.probeOcclusion;
#endif

    #if defined(DEBUG_DISPLAY) && !defined(PARTICLES_EDITOR_META_PASS)
    inputData.vertexSH = input.vertexSH;
    #endif
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

VaryingsParticle ParticlesGBufferVertex(AttributesParticle input)
{
    VaryingsParticle output = (VaryingsParticle)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetParticleVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetParticleVertexNormalInputs(input.normalOS, input.tangentOS);

    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
    URP_LIGHT_ACCUM3 vertexLight = VertexLighting(vertexInput.positionWS, half3(normalInput.normalWS));

#if FEATURES_NORMALMAP
    output.normalWS = half4(normalInput.normalWS, viewDirWS.x);
    output.tangentWS = half4(normalInput.tangentWS, viewDirWS.y);
    output.bitangentWS = half4(normalInput.bitangentWS, viewDirWS.z);
#else
    output.normalWS = half3(normalInput.normalWS);
    output.viewDirWS = viewDirWS;
#endif

    OUTPUT_SH4(vertexInput.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), output.vertexSH, output.probeOcclusion);

    output.positionWS.xyz = vertexInput.positionWS;
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

#if USE_VERTEX_SHADOW_COORD_INTERPOLATOR
    output.shadowCoord = ShadowCoordInterpolatorAvailable() ? GetShadowCoord(vertexInput, IsSurfaceTypeTransparent()) : float4(0, 0, 0, 0);
#endif

    return output;
}

GBufferFragOutput ParticlesGBufferFragment(VaryingsParticle input)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    float3 blendUv = float3(0, 0, 0);
#if defined(_FLIPBOOKBLENDING_ON)
    blendUv = input.texcoord2AndBlend;
#endif

    float4 projectedPosition = float4(0,0,0,0);
#if defined(_SOFTPARTICLES_ON) || defined(_FADING_ON) || defined(_DISTORTION_ON)
    projectedPosition = input.projectedPosition;
#endif

    SurfaceData surfaceData;
    InitializeParticleLitSurfaceData(input.texcoord, blendUv, input.color, projectedPosition, surfaceData,
        UseAlphaPremultiply(), UseMetallicSpecGlossMap(), UseEmission(),
        UseNormalMap(), UseAlphaModulate());

    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);
    SETUP_DEBUG_TEXTURE_DATA_FOR_TEX(inputData, input.texcoord, _BaseMap);

    // Stripped down version of UniversalFragmentPBR().

    // in LitForwardPass GlobalIllumination (and temporarily LightingPhysicallyBased) are called inside UniversalFragmentPBR
    // in Deferred rendering we store the sum of these values (and of emission as well) in the GBuffer
    BRDFData brdfData = InitializeBRDFData(surfaceData, IsSpecularSetup(), UseAlphaPremultiply());

    Light mainLight = GetMainLight(inputData.shadowCoord, inputData.positionWS, inputData.shadowMask, ReceiveShadows(), IsSurfaceTypeTransparent());
    MixRealtimeAndBakedGI(mainLight, inputData.normalWS, inputData.bakedGI);
    URP_LIGHT_ACCUM3 color = GlobalIllumination(brdfData, (BRDFData)0, 0, inputData.bakedGI, surfaceData.occlusion, inputData.positionWS,
                                     inputData.normalWS, inputData.viewDirectionWS, inputData.normalizedScreenSpaceUV,
                                     UseClearCoat() || UseClearCoatMap(), UseEnvironmentReflections());

    return PackGBuffersBRDFData(brdfData, inputData, surfaceData.smoothness, ClampExposed(inputData.preExposureMultiplier * (surfaceData.emission + color)), surfaceData.occlusion, ReceiveShadows(), IsSpecularSetup(), UseSpecularHighlights());
}

#endif // UNIVERSAL_PARTICLES_GBUFFER_LIT_PASS_INCLUDED
