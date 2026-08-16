#ifndef GLOBALSHADERVARS_HLSL
#define GLOBALSHADERVARS_HLSL

// This CB layout is directly matching GlobalShaderVariables.cs, make sure to keep the two up to date
//CBUFFER_START(GlobalShaderVars) - WIP disabled for now to avoid a serious memory regression, we will enable it only if Persistent CB mode is ON in a next PR
    // (t/20, t, t*2, t*3)
    float4 _Time;
    // sin(t/8), sin(t/4), sin(t/2), sin(t)
    float4 _SinTime;
    // cos(t/8), cos(t/4), cos(t/2), cos(t)
    float4 _CosTime;
    // dt, 1/dt, smoothdt, 1/smoothdt
    float4 unity_DeltaTime;
    // t, sin(t), cos(t)
    float4 _TimeParameters;
    // t, sin(t), cos(t)
    float4 _LastTimeParameters;
    // x = width
    // y = height
    // z = 1 + 1.0/width
    // w = 1 + 1.0/height
    float4 _ScreenParams;
    // Values used to linearize the Z buffer (http://www.humus.name/temp/Linearize%20depth.txt)
    // x = 1-far/near
    // y = far/near
    // z = x/far
    // w = y/far
    // or in case of a reversed depth buffer (UNITY_REVERSED_Z is 1)
    // x = -1+far/near
    // y = 1
    // z = x/far
    // w = 1/far
    float4 _ZBufferParams;
    // x = orthographic camera's width
    // y = orthographic camera's height
    // z = unused
    // w = 1.0 if camera is ortho, 0.0 if perspective
    float4 unity_OrthoParams;
    // { w / RTHandle.maxWidth, h / RTHandle.maxHeight } : xy = currFrame, zw = prevFrame
    float4 _RTHandleScale;
    float4 _ScaledScreenParams;
    // {w, h, 1/w, 1/h}
    float4 _ScreenSize;
    float4 _ScreenSizeOverride;
    float4 _ScreenCoordScaleBias;
    // x = Mip Bias
    // y = 2.0 ^ [Mip Bias]
    float2 _GlobalMipBias;
    float2 _URPPadding1;
    // === Variables only present in 3D renderer start here ===
    float4 _URPDummy3D;
    // === Variables only present in 2D renderer start here ===
    float4 _URPDummy2D;
//CBUFFER_END


#endif