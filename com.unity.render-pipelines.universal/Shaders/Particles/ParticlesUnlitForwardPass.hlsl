#ifndef UNIVERSAL_PARTICLES_UNLIT_FORWARD_PASS_INCLUDED
#define UNIVERSAL_PARTICLES_UNLIT_FORWARD_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Unlit.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Particles.hlsl"

InputData CreateInputData(VaryingsParticle input, SurfaceData surfaceData)
{
    InputData output = (InputData)0;

    output.positionWS = input.positionWS.xyz;

#ifdef _NORMALMAP
    half3 viewDirWS = half3(input.normalWS.w, input.tangentWS.w, input.bitangentWS.w);
    output.tangentMatrixWS = half3x3(input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz);
    output.normalWS = TransformTangentToWorld(surfaceData.normalTS, output.tangentMatrixWS);
#else
    half3 viewDirWS = input.viewDirWS;
    output.normalWS = input.normalWS;
#endif

    output.normalWS = NormalizeNormalPerPixel(output.normalWS);

#if SHADER_HINT_NICE_QUALITY
    viewDirWS = SafeNormalize(viewDirWS);
#endif

    output.viewDirectionWS = viewDirWS;

    output.fogCoord = (half)input.positionWS.w;
    output.vertexLighting = half3(0.0h, 0.0h, 0.0h);
    output.bakedGI = SampleSHPixel(input.vertexSH, output.normalWS);
    output.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.clipPos);
    output.normalTS = surfaceData.normalTS;
    output.shadowMask = half4(1, 1, 1, 1);
    output.shadowCoord = float4(0, 0, 0, 0);

    #if defined(LIGHTMAP_ON)
    output.lightmapUV = half2(0, 0);
    #else
    output.vertexSH = input.vertexSH;
    #endif

    return output;
}

SurfaceData CreateSurfaceData(ParticleParams particleParams)
{
    SurfaceData surfaceData;
    half4 albedo = SampleAlbedo(particleParams.uv, particleParams.blendUv, _BaseColor, particleParams.baseColor, particleParams.projectedPosition, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap));
    half3 normalTS = SampleNormalTS(particleParams.uv, particleParams.blendUv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap));

    #if defined (_DISTORTION_ON)
    albedo.rgb = Distortion(albedo, normalTS, _DistortionStrengthScaled, _DistortionBlend, particleParams.projectedPosition);
    #endif

    #if defined(_EMISSION)
    half3 emission = BlendTexture(TEXTURE2D_ARGS(_EmissionMap, sampler_EmissionMap), particleParams.uv, particleParams.blendUv).rgb * _EmissionColor.rgb;
    #else
    half3 emission = half3(0, 0, 0);
    #endif

    surfaceData.albedo = albedo.rgb;
    surfaceData.specular = half3(0.0h, 0.0h, 0.0h);
    surfaceData.normalTS = normalTS;
    surfaceData.emission = emission;
    surfaceData.metallic = 0;
    surfaceData.smoothness = 1;
    surfaceData.occlusion = 1;

    surfaceData.albedo = AlphaModulate(surfaceData.albedo, albedo.a);
    surfaceData.alpha = albedo.a;

    surfaceData.clearCoatMask       = 0.0h;
    surfaceData.clearCoatSmoothness = 1.0h;

    return surfaceData;
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

VaryingsParticle vertParticleUnlit(AttributesParticle input)
{
    VaryingsParticle output = (VaryingsParticle)0;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);

    // position ws is used to compute eye depth in vertFading
    output.positionWS.xyz = vertexInput.positionWS;
    output.positionWS.w = ComputeFogFactor(vertexInput.positionCS.z);
    output.clipPos = vertexInput.positionCS;
    output.color = GetParticleColor(input.color);

    half3 viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
#if !SHADER_HINT_NICE_QUALITY
    viewDirWS = SafeNormalize(viewDirWS);
#endif

#ifdef _NORMALMAP
    output.normalWS = half4(normalInput.normalWS, viewDirWS.x);
    output.tangentWS = half4(normalInput.tangentWS, viewDirWS.y);
    output.bitangentWS = half4(normalInput.bitangentWS, viewDirWS.z);
#else
    output.normalWS = normalInput.normalWS;
    output.viewDirWS = viewDirWS;
#endif

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

    return output;
}

half4 fragParticleUnlit(VaryingsParticle input) : SV_Target
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    ParticleParams particleParams;
    InitParticleParams(input, particleParams);

    SurfaceData surfaceData = CreateSurfaceData(particleParams);
    InputData inputData = CreateInputData(input, surfaceData);
    SETUP_DEBUG_TEXTURE_DATA(inputData, input.texcoord, _BaseMap);

    half4 finalColor = UniversalFragmentUnlit(inputData, surfaceData);
    half fogFactor = input.positionWS.w;

    finalColor.rgb = MixFog(finalColor.rgb, fogFactor);
    finalColor.a = OutputAlpha(finalColor.a, _Surface);

    return finalColor;
}

#endif // UNIVERSAL_PARTICLES_UNLIT_FORWARD_PASS_INCLUDED
