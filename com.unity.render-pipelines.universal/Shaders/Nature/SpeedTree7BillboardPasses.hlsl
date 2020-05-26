#ifndef UNIVERSAL_SPEEDTREE7BILLBOARD_PASSES_INCLUDED
#define UNIVERSAL_SPEEDTREE7BILLBOARD_PASSES_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "SpeedTree7CommonPasses.hlsl"

void InitializeData(inout SpeedTreeVertexInput input, out half2 outUV, out half outHueVariation)
{
    // assume no scaling & rotation
    float3 worldPos = input.vertex.xyz + float3(UNITY_MATRIX_M[0].w, UNITY_MATRIX_M[1].w, UNITY_MATRIX_M[2].w);

#ifdef BILLBOARD_FACE_CAMERA_POS
    float3 eyeVec = normalize(unity_BillboardCameraPosition - worldPos);
    float3 billboardTangent = normalize(float3(-eyeVec.z, 0, eyeVec.x));            // cross(eyeVec, {0,1,0})
    float3 billboardNormal = float3(billboardTangent.z, 0, -billboardTangent.x);    // cross({0,1,0},billboardTangent)
    float angle = atan2(billboardNormal.z, billboardNormal.x);                     // signed angle between billboardNormal to {0,0,1}
    angle += angle < 0 ? 2 * SPEEDTREE_PI : 0;
#else
    float3 billboardTangent = unity_BillboardTangent;
    float3 billboardNormal = unity_BillboardNormal;
    float angle = unity_BillboardCameraXZAngle;
#endif

    float widthScale = input.texcoord1.x;
    float heightScale = input.texcoord1.y;
    float rotation = input.texcoord1.z;

    float2 percent = input.texcoord.xy;
    float3 billboardPos = (percent.x - 0.5f) * unity_BillboardSize.x * widthScale * billboardTangent;
    billboardPos.y += (percent.y * unity_BillboardSize.y + unity_BillboardSize.z) * heightScale;

#ifdef ENABLE_WIND
    if (_WindQuality * _WindEnabled > 0)
    {
        billboardPos = GlobalWind(billboardPos, worldPos, true, _ST_WindVector.xyz, input.texcoord1.w);
    }
#endif

    input.vertex.xyz += billboardPos;
    input.vertex.w = 1.0f;
    input.normal = billboardNormal.xyz;
    input.tangent = float4(billboardTangent.xyz, -1);

    float slices = unity_BillboardInfo.x;
    float invDelta = unity_BillboardInfo.y;
    angle += rotation;

    float imageIndex = fmod(floor(angle * invDelta + 0.5f), slices);
    float4 imageTexCoords = unity_BillboardImageTexCoords[imageIndex];
    if (imageTexCoords.w < 0)
    {
        outUV = imageTexCoords.xy - imageTexCoords.zw * percent.yx;
    }
    else
    {
        outUV = imageTexCoords.xy + imageTexCoords.zw * percent;
    }

#ifdef EFFECT_HUE_VARIATION
    float hueVariationAmount = frac(worldPos.x + worldPos.y + worldPos.z);
    outHueVariation = saturate(hueVariationAmount * _HueVariation.a);
#else
    outHueVariation = 0;
#endif
}

SpeedTreeVertexOutput SpeedTree7Vert(SpeedTreeVertexInput input)
{
    SpeedTreeVertexOutput output = (SpeedTreeVertexOutput)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    // handle speedtree wind and lod
    InitializeData(input, output.uvHueVariation.xy, output.uvHueVariation.z);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.vertex.xyz);
    half3 normalWS = input.normal; // Already calculated in world space. Can probably get rid of the world space transform in GetVertexPositionInputs too.

    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalWS);
    half fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);

    half3 viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);
    #ifdef EFFECT_BUMP
        real sign = input.tangent.w * GetOddNegativeScale();
        output.normalWS.xyz = TransformObjectToWorldNormal(input.normal);
        output.tangentWS.xyz = TransformObjectToWorldDir(input.tangent.xyz);
        output.bitangentWS.xyz = cross(output.normalWS.xyz, output.tangentWS.xyz) * sign;

        // View dir packed in w.
        output.normalWS.w = viewDirWS.x;
        output.tangentWS.w = viewDirWS.y;
        output.bitangentWS.w = viewDirWS.z;
    #else
        output.normalWS.xyz = TransformObjectToWorldNormal(input.normal);
        output.viewDirWS = viewDirWS;
    #endif

    output.positionWS = vertexInput.positionWS;

    output.clipPos = vertexInput.positionCS;

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
        output.shadowCoord = GetShadowCoord(vertexInput);
    #endif

    return output;
}

SpeedTreeVertexDepthOutput SpeedTree7VertDepth(SpeedTreeVertexInput input)
{
    SpeedTreeVertexDepthOutput output = (SpeedTreeVertexDepthOutput)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    // handle speedtree wind and lod
    InitializeData(input, output.uvHueVariation.xy, output.uvHueVariation.z);
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
