//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef HDCAMERA_CS_HLSL
#define HDCAMERA_CS_HLSL
// Generated from UnityEngine.Experimental.Rendering.HDPipeline.HDCamera+ViewConstants
// PackingRules = Exact
struct ViewConstants
{
    float4x4 viewMatrix;
    float4x4 invViewMatrix;
    float4x4 projMatrix;
    float4x4 invProjMatrix;
    float4x4 viewProjMatrix;
    float4x4 invViewProjMatrix;
    float4x4 nonJitteredViewProjMatrix;
    float4x4 prevViewProjMatrix;
    float4x4 prevViewProjMatrixNoCameraTrans;
    float4x4 pixelCoordToViewDirWS;
    float3 worldSpaceCameraPos;
    float pad0;
    float3 worldSpaceCameraPosViewOffset;
    float pad1;
    float3 prevWorldSpaceCameraPos;
    float pad2;
};


#endif
