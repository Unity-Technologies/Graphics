#ifndef UNIVERSAL_SPEEDTREE7_PASSES_INCLUDED
#define UNIVERSAL_SPEEDTREE7_PASSES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "SpeedTree7CommonPasses.hlsl"

void InitializeData(inout SpeedTreeVertexInput input, float lodValue)
{
    float3 finalPosition = input.vertex.xyz;

    #ifdef ENABLE_WIND
        half windQuality = _WindQuality * _WindEnabled;

        float3 rotatedWindVector, rotatedBranchAnchor;
        if (windQuality <= WIND_QUALITY_NONE)
        {
            rotatedWindVector = float3(0.0f, 0.0f, 0.0f);
            rotatedBranchAnchor = float3(0.0f, 0.0f, 0.0f);
        }
        else
        {
            // compute rotated wind parameters
            rotatedWindVector = normalize(mul(_ST_WindVector.xyz, (float3x3)UNITY_MATRIX_M));
            rotatedBranchAnchor = normalize(mul(_ST_WindBranchAnchor.xyz, (float3x3)UNITY_MATRIX_M)) * _ST_WindBranchAnchor.w;
        }
    #endif

    #if defined(GEOM_TYPE_BRANCH) || defined(GEOM_TYPE_FROND)

        // smooth LOD
        #ifdef LOD_FADE_PERCENTAGE
            finalPosition = lerp(finalPosition, input.texcoord1.xyz, lodValue);
        #endif

        // frond wind, if needed
        #if defined(ENABLE_WIND) && defined(GEOM_TYPE_FROND)
            if (windQuality == WIND_QUALITY_PALM)
                finalPosition = RippleFrond(finalPosition, input.normal, input.texcoord.x, input.texcoord.y, input.texcoord2.x, input.texcoord2.y, input.texcoord2.z);
        #endif

    #elif defined(GEOM_TYPE_LEAF)

        // remove anchor position
        finalPosition -= input.texcoord1.xyz;

        bool isFacingLeaf = input.color.a == 0;
        if (isFacingLeaf)
        {
            #ifdef LOD_FADE_PERCENTAGE
                finalPosition *= lerp(1.0, input.texcoord1.w, lodValue);
            #endif
            // face camera-facing leaf to camera
            float offsetLen = length(finalPosition);
            finalPosition = mul(finalPosition.xyz, (float3x3)UNITY_MATRIX_IT_MV); // inv(MV) * finalPosition
            finalPosition = normalize(finalPosition) * offsetLen; // make sure the offset vector is still scaled
        }
        else
        {
            #ifdef LOD_FADE_PERCENTAGE
                float3 lodPosition = float3(input.texcoord1.w, input.texcoord3.x, input.texcoord3.y);
                finalPosition = lerp(finalPosition, lodPosition, lodValue);
            #endif
        }

        #ifdef ENABLE_WIND
            // leaf wind
            if (windQuality > WIND_QUALITY_FASTEST && windQuality < WIND_QUALITY_PALM)
            {
                float leafWindTrigOffset = input.texcoord1.x + input.texcoord1.y;
                finalPosition = LeafWind(windQuality == WIND_QUALITY_BEST, input.texcoord2.w > 0.0, finalPosition, input.normal, input.texcoord2.x, float3(0,0,0), input.texcoord2.y, input.texcoord2.z, leafWindTrigOffset, rotatedWindVector);
            }
        #endif

        // move back out to anchor
        finalPosition += input.texcoord1.xyz;

    #endif

    #ifdef ENABLE_WIND
        float3 treePos = float3(UNITY_MATRIX_M[0].w, UNITY_MATRIX_M[1].w, UNITY_MATRIX_M[2].w);

        #ifndef GEOM_TYPE_MESH
            if (windQuality >= WIND_QUALITY_BETTER)
            {
                // branch wind (applies to all 3D geometry)
                finalPosition = BranchWind(windQuality == WIND_QUALITY_PALM, finalPosition, treePos, float4(input.texcoord.zw, 0, 0), rotatedWindVector, rotatedBranchAnchor);
            }
        #endif

        if (windQuality > WIND_QUALITY_NONE)
        {
            // global wind
            finalPosition = GlobalWind(finalPosition, treePos, true, rotatedWindVector, _ST_WindGlobal.x);
        }
    #endif

    input.vertex.xyz = finalPosition;
}

SpeedTreeVertexOutput SpeedTree7Vert(SpeedTreeVertexInput input)
{
    SpeedTreeVertexOutput output = (SpeedTreeVertexOutput)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    // handle speedtree wind and lod
    InitializeData(input, unity_LODFade.x);

    #ifdef VERTEX_COLOR
        output.color = _Color;
        output.color.rgb *= input.color.r; // ambient occlusion factor
    #endif

    output.uvHueVariation.xy = input.texcoord.xy;

    #ifdef EFFECT_HUE_VARIATION
        half hueVariationAmount = frac(UNITY_MATRIX_M[0].w + UNITY_MATRIX_M[1].w + UNITY_MATRIX_M[2].w);
        hueVariationAmount += frac(input.vertex.x + input.normal.y + input.normal.x) * 0.5 - 0.3;
        output.uvHueVariation.z = saturate(hueVariationAmount * _HueVariation.a);
    #endif

    #ifdef GEOM_TYPE_BRANCH_DETAIL
        // The two types are always in different sub-range of the mesh so no interpolation (between detail and blend) problem.
        output.detail.xy = input.texcoord2.xy;
        output.detail.z = input.color.a == 0 ? input.texcoord2.z : 2.5; // stay out of Blend's .z range
    #endif

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);
    half3 normalWS = TransformObjectToWorldNormal(input.normal);

    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalWS);
    half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

    half3 viewDirWS = GetCameraPositionWS() - vertexInput.positionWS;

    #ifdef EFFECT_BUMP
        real sign = input.tangent.w * GetOddNegativeScale();
        output.normalWS.xyz = normalWS;
        output.tangentWS.xyz = TransformObjectToWorldDir(input.tangent.xyz);
        output.bitangentWS.xyz = cross(output.normalWS.xyz, output.tangentWS.xyz) * sign;

        // View dir packed in w.
        output.normalWS.w = viewDirWS.x;
        output.tangentWS.w = viewDirWS.y;
        output.bitangentWS.w = viewDirWS.z;
    #else
        output.normalWS = normalWS;
        output.viewDirWS = viewDirWS;
    #endif

    #ifdef _MAIN_LIGHT_SHADOWS
        output.shadowCoord = GetShadowCoord(vertexInput);
    #endif

    output.positionWS = vertexInput.positionWS;
    output.clipPos = vertexInput.positionCS;

    return output;
}

SpeedTreeVertexDepthOutput SpeedTree7VertDepth(SpeedTreeVertexInput input)
{
    SpeedTreeVertexDepthOutput output = (SpeedTreeVertexDepthOutput)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    // handle speedtree wind and lod
    InitializeData(input, unity_LODFade.x);
    output.uvHueVariation.xy = input.texcoord.xy;
    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);

#ifdef SHADOW_CASTER
    half3 normalWS = TransformObjectToWorldNormal(input.normal);
    output.clipPos = TransformWorldToHClip(ApplyShadowBias(vertexInput.positionWS, normalWS, _LightDirection));
#else
    output.clipPos = vertexInput.positionCS;
#endif
    return output;
}

#endif
