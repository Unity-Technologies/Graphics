#ifndef HDRP_SPEEDTREE7BILLBOARD_INPUT_INCLUDED
#define HDRP_SPEEDTREE7BILLBOARD_INPUT_INCLUDED

#define SPEEDTREE_PI 3.14159265359

#include "SpeedTree7Input.hlsl"

#define SPEEDTREE_ALPHATEST
float _Cutoff;

#define VARYINGS_NEED_TEXCOORD0
#define VARYINGS_NEED_TEXCOORD1

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

#endif
