//
// This file was automatically generated. Please don't edit by hand.
//

#ifndef SHADERVARIABLESXR_CS_HLSL
#define SHADERVARIABLESXR_CS_HLSL
// Generated from UnityEngine.Rendering.HighDefinition.ShaderVariablesXR
// PackingRules = Exact
CBUFFER_START(ShaderVariablesXR)
    float4x4 _XRViewMatrix[2];
    float4x4 _XRInvViewMatrix[2];
    float4x4 _XRProjMatrix[2];
    float4x4 _XRInvProjMatrix[2];
    float4x4 _XRViewProjMatrix[2];
    float4x4 _XRInvViewProjMatrix[2];
    float4x4 _XRNonJitteredViewProjMatrix[2];
    float4x4 _XRPrevViewProjMatrix[2];
    float4x4 _XRPrevInvViewProjMatrix[2];
    float4x4 _XRPrevViewProjMatrixNoCameraTrans[2];
    float4x4 _XRPixelCoordToViewDirWS[2];
    float4 _XRWorldSpaceCameraPos[2];
    float4 _XRWorldSpaceCameraPosViewOffset[2];
    float4 _XRPrevWorldSpaceCameraPos[2];
CBUFFER_END


#endif
