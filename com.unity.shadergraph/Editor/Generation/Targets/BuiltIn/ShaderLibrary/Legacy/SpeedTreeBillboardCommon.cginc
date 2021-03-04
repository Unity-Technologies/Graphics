#ifndef SPEEDTREE_BILLBOARD_COMMON_INCLUDED
#define SPEEDTREE_BILLBOARD_COMMON_INCLUDED

#define SPEEDTREE_ALPHATEST
fixed _Cutoff;

#include "SpeedTreeCommon.cginc"

CBUFFER_START(UnityBillboardPerCamera)
    float3 unity_BillboardNormal;
    float3 unity_BillboardTangent;
    float4 unity_BillboardCameraParams;
    #define unity_BillboardCameraPosition (unity_BillboardCameraParams.xyz)
    #define unity_BillboardCameraXZAngle (unity_BillboardCameraParams.w)
CBUFFER_END

CBUFFER_START(UnityBillboardPerBatch)
    float4 unity_BillboardInfo; // x: num of billboard slices; y: 1.0f / (delta angle between slices)
    float4 unity_BillboardSize; // x: width; y: height; z: bottom
    float4 unity_BillboardImageTexCoords[16];
CBUFFER_END

struct SpeedTreeBillboardData
{
    float4 vertex       : POSITION;
    float2 texcoord     : TEXCOORD0;
    float4 texcoord1    : TEXCOORD1;
    float3 normal       : NORMAL;
    float4 tangent      : TANGENT;
    float4 color        : COLOR;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

void SpeedTreeBillboardVert(inout SpeedTreeBillboardData IN, out Input OUT)
{
    UNITY_INITIALIZE_OUTPUT(Input, OUT);

    // assume no scaling & rotation
    float3 worldPos = IN.vertex.xyz + float3(unity_ObjectToWorld[0].w, unity_ObjectToWorld[1].w, unity_ObjectToWorld[2].w);

#ifdef BILLBOARD_FACE_CAMERA_POS
    float3 eyeVec = normalize(unity_BillboardCameraPosition - worldPos);
    float3 billboardTangent = normalize(float3(-eyeVec.z, 0, eyeVec.x));            // cross(eyeVec, {0,1,0})
    float3 billboardNormal = float3(billboardTangent.z, 0, -billboardTangent.x);    // cross({0,1,0},billboardTangent)
    float3 angle = atan2(billboardNormal.z, billboardNormal.x);                     // signed angle between billboardNormal to {0,0,1}
    angle += angle < 0 ? 2 * UNITY_PI : 0;
#else
    float3 billboardTangent = unity_BillboardTangent;
    float3 billboardNormal = unity_BillboardNormal;
    float angle = unity_BillboardCameraXZAngle;
#endif

    float widthScale = IN.texcoord1.x;
    float heightScale = IN.texcoord1.y;
    float rotation = IN.texcoord1.z;

    float2 percent = IN.texcoord.xy;
    float3 billboardPos = (percent.x - 0.5f) * unity_BillboardSize.x * widthScale * billboardTangent;
    billboardPos.y += (percent.y * unity_BillboardSize.y + unity_BillboardSize.z) * heightScale;

#ifdef ENABLE_WIND
    if (_WindQuality * _WindEnabled > 0)
        billboardPos = GlobalWind(billboardPos, worldPos, true, _ST_WindVector.xyz, IN.texcoord1.w);
#endif

    IN.vertex.xyz += billboardPos;
    IN.vertex.w = 1.0f;
    IN.normal = billboardNormal.xyz;
    IN.tangent = float4(billboardTangent.xyz,-1);

    float slices = unity_BillboardInfo.x;
    float invDelta = unity_BillboardInfo.y;
    angle += rotation;

    float imageIndex = fmod(floor(angle * invDelta + 0.5f), slices);
    float4 imageTexCoords = unity_BillboardImageTexCoords[imageIndex];
    if (imageTexCoords.w < 0)
    {
        OUT.mainTexUV = imageTexCoords.xy - imageTexCoords.zw * percent.yx;
    }
    else
    {
        OUT.mainTexUV = imageTexCoords.xy + imageTexCoords.zw * percent;
    }

    OUT.color = _Color;

#ifdef EFFECT_HUE_VARIATION
    float hueVariationAmount = frac(worldPos.x + worldPos.y + worldPos.z);
    OUT.HueVariationAmount = saturate(hueVariationAmount * _HueVariation.a);
#endif
}

#endif // SPEEDTREE_BILLBOARD_COMMON_INCLUDED
