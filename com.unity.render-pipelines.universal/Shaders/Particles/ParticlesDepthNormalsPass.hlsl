#ifndef UNIVERSAL_PARTICLES_LIT_DEPTH_NORMALS_PASS_INCLUDED
#define UNIVERSAL_PARTICLES_LIT_DEPTH_NORMALS_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

VaryingsDepthNormalsParticle DepthNormalsVertex(AttributesDepthNormalsParticle input)
{
    VaryingsDepthNormalsParticle output = (VaryingsDepthNormalsParticle)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);
    VertexNormalInputs normalInput = GetVertexNormalInputs(input.normal, input.tangent);

    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);

    #if defined(_NORMALMAP)
        output.normalWS = half4(normalInput.normalWS, viewDirWS.x);
        output.tangentWS = half4(normalInput.tangentWS, viewDirWS.y);
        output.bitangentWS = half4(normalInput.bitangentWS, viewDirWS.z);
    #else
        output.normalWS = normalInput.normalWS;
        output.viewDirWS = viewDirWS;
    #endif

    output.clipPos = vertexInput.positionCS;

    #if defined(_ALPHATEST_ON)
        output.color = GetParticleColor(input.color);
    #endif

    #if defined(_ALPHATEST_ON) || defined(_NORMALMAP)
        #if defined(_FLIPBOOKBLENDING_ON)
            #if defined(UNITY_PARTICLE_INSTANCING_ENABLED)
                GetParticleTexcoords(output.texcoord, output.texcoord2AndBlend, input.texcoords.xyxy, 0.0);
            #else
                GetParticleTexcoords(output.texcoord, output.texcoord2AndBlend, input.texcoords, input.texcoordBlend);
            #endif
        #else
            GetParticleTexcoords(output.texcoord, input.texcoords.xy);
        #endif
    #endif

    return output;
}

half4 DepthNormalsFragment(VaryingsDepthNormalsParticle input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    // Inputs...
    #if defined(_ALPHATEST_ON) || defined(_NORMALMAP)
        float2 uv = input.texcoord;

        #if defined(_FLIPBOOKBLENDING_ON)
            float3 blendUv = input.texcoord2AndBlend;
        #else
            float3 blendUv = float3(0,0,0);
        #endif
    #endif

    // Check if we need to discard...
    #if defined(_ALPHATEST_ON)
        half4 vertexColor = input.color;
        half4 baseColor = _BaseColor;
        half4 albedo = BlendTexture(TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap), uv, blendUv) * baseColor;

        half4 colorAddSubDiff = half4(0, 0, 0, 0);
        #if defined(_COLORADDSUBDIFF_ON)
            colorAddSubDiff = _BaseColorAddSubDiff;
        #endif

        albedo = MixParticleColor(albedo, vertexColor, colorAddSubDiff);
        AlphaDiscard(albedo.a, _Cutoff);
    #endif

    // Normals...
    #ifdef _NORMALMAP
        half3 normalTS = SampleNormalTS(uv, blendUv, TEXTURE2D_ARGS(_BumpMap, sampler_BumpMap), _BumpScale);
        float3 normalWS = TransformTangentToWorld(normalTS, half3x3(input.tangentWS.xyz, input.bitangentWS.xyz, input.normalWS.xyz));
    #else
        float3 normalWS = input.normalWS;
    #endif

    // Output...
    #if defined(_GBUFFER_NORMALS_OCT)
        float2 octNormalWS = PackNormalOctQuadEncode(normalWS);           // values between [-1, +1], must use fp32 on some platforms
        float2 remappedOctNormalWS = saturate(octNormalWS * 0.5 + 0.5);   // values between [ 0,  1]
        half3 packedNormalWS = PackFloat2To888(remappedOctNormalWS);      // values between [ 0,  1]
        return half4(packedNormalWS, 0.0);
    #else
        return half4(NormalizeNormalPerPixel(normalWS), 0.0);
    #endif
}

#endif // UNIVERSAL_PARTICLES_LIT_DEPTH_NORMALS_PASS_INCLUDED
