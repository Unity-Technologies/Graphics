#ifndef HDRP_SPEEDTREE7BILLBOARD_PASSES_INCLUDED
#define HDRP_SPEEDTREE7BILLBOARD_PASSES_INCLUDED

void InitializeData(inout SpeedTreeVertexInput input, out half2 outUV, out half outHueVariation)
{
    float4x4 objToWorld = UNITY_MATRIX_M;
    // assume no scaling & rotation
    input.vertex.xyz += float3(objToWorld[0].w, objToWorld[1].w, objToWorld[2].w);
    float3 worldPos = input.vertex.xyz;

#ifdef BILLBOARD_FACE_CAMERA_POS
    float3 eyeVec = normalize(unity_BillboardCameraPosition - worldPos);
    float3 billboardTangent = normalize(float3(-eyeVec.z, 0, eyeVec.x));            // cross(eyeVec, {0,1,0})
    float3 billboardNormal = float3(billboardTangent.z, 0, -billboardTangent.x);    // cross({0,1,0},billboardTangent)
    float3 angle = atan2(billboardNormal.z, billboardNormal.x);                     // signed angle between billboardNormal to {0,0,1}
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
        billboardPos = GlobalWind(billboardPos, worldPos, true, _ST_WindVector.xyz, input.texcoord1.w);
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


#include "Packages/com.unity.render-pipelines.high-definition/Runtime/RenderPipeline/ShaderPass/VertMesh.hlsl"

PackedVaryingsType SpeedTree7Vert(SpeedTreeVertexInput input)
{
    PackedVaryingsType output = (PackedVaryingsType)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output.vmesh);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output.vmesh);

    // handle speedtree wind and lod
    // interpolators3 is used for uvHueVariation
    InitializeData(input, output.vmesh.interpolators3.xy, output.vmesh.interpolators3.z);

    // For the billboards, it's already in worldspace.
    float3 normalWS = input.normal; //TransformObjectToWorldNormal(input.normal);
    float3 viewDirWS = _WorldSpaceCameraPos - input.vertex.xyz;
    float4 positionCS = TransformWorldToHClip(input.vertex);

#ifdef EFFECT_BUMP
    float sign = input.tangent.w * GetOddNegativeScale();
    output.vmesh.interpolators1 = normalWS;
    output.vmesh.interpolators2.xyz = TransformObjectToWorldDir(input.tangent.xyz);
    output.vmesh.interpolators2.w = sign;
#else
    output.vmesh.interpolators1 = normalWS;
    output.vmesh.interpolators2.xyz = viewDirWS;
    output.vmesh.interpolators2.w = -1.0;
#endif

    output.vmesh.interpolators5.rgb = _Color.rgb;
    output.vmesh.interpolators5.a = input.color.r;      // ambient occlusion factor

    output.vmesh.interpolators0.xyz = input.vertex.xyz;
    output.vmesh.positionCS = positionCS;

    return output;
}

#endif
