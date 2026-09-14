#ifndef UNIVERSAL_PARTICLES_FORWARD_SIMPLE_LIT_PASS_INCLUDED
#define UNIVERSAL_PARTICLES_FORWARD_SIMPLE_LIT_PASS_INCLUDED

#include "ParticlesSimpleLitInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DistanceFog.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/Particles/ParticlesSimpleLitFeatures.hlsl"

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

#if FEATURES_NORMALMAP
    half3 viewDirWS = half3(input.normalWS.w, input.tangentWS.w, input.bitangentWS.w);
    if (UseNormalMap())
    {
        inputData.tangentToWorld = half3x3(input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz);
        inputData.normalWS = TransformTangentToWorld(normalTS, inputData.tangentToWorld);
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

    inputData.vertexLighting = half3(0.0h, 0.0h, 0.0h);
#if defined(PROBE_VOLUMES_L1) || defined(PROBE_VOLUMES_L2)
    inputData.bakedGI = SAMPLE_GI(input.vertexSH,
        GetAbsolutePositionWS(inputData.positionWS),
        inputData.normalWS,
        inputData.viewDirectionWS,
        input.clipPos.xy,
        input.probeOcclusion,
        inputData.shadowMask);
#else
    inputData.bakedGI = SampleSHPixel(input.vertexSH, inputData.normalWS);
#endif
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.clipPos);
    inputData.shadowMask = half4(1, 1, 1, 1);

    #if defined(DEBUG_DISPLAY) && !defined(PARTICLES_EDITOR_META_PASS)
    inputData.vertexSH = input.vertexSH;
    #endif

#if defined(DEBUG_DISPLAY) && defined(USE_APV_PROBE_OCCLUSION)
    inputData.probeOcclusion = input.probeOcclusion;
#endif
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

VaryingsParticle ParticlesLitVertex(AttributesParticle input)
{
    VaryingsParticle output;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetParticleVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetParticleVertexNormalInputs(input.normalOS, input.tangentOS);
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);

#if FEATURES_NORMALMAP
    output.normalWS = half4(normalInput.normalWS, viewDirWS.x);
    output.tangentWS = half4(normalInput.tangentWS, viewDirWS.y);
    output.bitangentWS = half4(normalInput.bitangentWS, viewDirWS.z);
#else
    output.normalWS = half3(normalInput.normalWS);
    output.viewDirWS = viewDirWS;
#endif

    OUTPUT_SH4(vertexInput.positionWS, output.normalWS.xyz, GetWorldSpaceNormalizeViewDir(vertexInput.positionWS), output.vertexSH, output.probeOcclusion);

    half fogFactor = 0.0;

    output.positionWS.xyz = vertexInput.positionWS.xyz;
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

#if USE_VERTEX_SHADOW_COORD_INTERPOLATOR
    output.shadowCoord = ShadowCoordInterpolatorAvailable() ? GetShadowCoord(vertexInput, IsSurfaceTypeTransparent()) : float4(0, 0, 0, 0);
#endif

    return output;
}

half4 ParticlesLitFragment(VaryingsParticle input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    ParticleParams particleParams;
    InitParticleParams(input, particleParams);

    half3 normalTS = half3(0.0, 0.0, 1.0);
    if (UseNormalMap())
        normalTS = SampleParticleNormalTS(particleParams.uv, particleParams.blendUv, UnityBuildTexture2DStructNoScaleNoTexelSize(_BumpMap), half(1.0));
    half4 albedo = SampleAlbedo(UnityBuildTexture2DStructNoScaleNoTexelSize(_BaseMap), particleParams, UseAlphaPremultiply());
    half3 diffuse = albedo.rgb;
    if (UseAlphaModulate())
        diffuse = ApplyAlphaModulate(albedo.rgb, albedo.a);
    half alpha = albedo.a;
    half3 emission = UseEmission() ? BlendTexture(UnityBuildTexture2DStructNoScaleNoTexelSize(_EmissionMap), particleParams.uv, particleParams.blendUv).rgb * _EmissionColor.rgb : half3(0, 0, 0);
    half4 specularGloss = half4(0, 0, 0, 1);
    if (UseSpecGlossMap())
        specularGloss = BlendTexture(UnityBuildTexture2DStructNoScaleNoTexelSize(_SpecGlossMap), particleParams.uv, particleParams.blendUv);
    else if (UseSpecularColor())
        specularGloss = _SpecColor;
    if (UseGlossinessFromBaseAlpha())
        specularGloss.a = albedo.a;

#if defined(_DISTORTION_ON)
    diffuse = Distortion(half4(diffuse, alpha), normalTS, _DistortionStrengthScaled, _DistortionBlend, particleParams.projectedPosition);
#endif

    InputData inputData;
    InitializeInputData(input, normalTS, inputData);

    URP_LIGHT_ACCUM4 color = UniversalFragmentBlinnPhong(inputData, diffuse, specularGloss, specularGloss.a, emission, alpha, normalTS);

    color.rgb = ClampExposed(inputData.preExposureMultiplier * BlendDistanceFog(color.rgb, input.clipPos));
#if defined(_TRANSPARENT_RECEIVE_FOG)
    if (IsSurfaceTypeTransparent())
        color.rgb = MixVolumetricFog(color.rgb, color.a, _Blend, UseAlphaPremultiply(), input.clipPos);
#endif
    color.a = OutputAlpha(color.a, IsSurfaceTypeTransparent());

    return color;
}

#endif // UNIVERSAL_PARTICLES_FORWARD_SIMPLE_LIT_PASS_INCLUDED
