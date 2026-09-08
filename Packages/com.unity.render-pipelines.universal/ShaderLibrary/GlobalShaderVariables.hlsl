#ifndef UNIVERSAL_GLOBAL_SHADER_VARIABLES_INCLUDED
#define UNIVERSAL_GLOBAL_SHADER_VARIABLES_INCLUDED

#define MAX_SHADOW_CASCADES 4

// This CB layout is directly matching GlobalShaderVariables.cs, make sure to keep the two up to date
// URP_GLOBAL_CONSTANT_BUFFER is a shader constant define driven by the Project Settings > Graphics > URP > Rendering Capabilities > Use Persistent Constant Buffers
// Only available in Editor Developer mode for now, still wip
#if defined(URP_GLOBAL_CONSTANT_BUFFER)
CBUFFER_START(GlobalShaderVariables)
#endif
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
    // Ambient gradient, filled from RenderSettings.
#if defined(URP_GLOBAL_CONSTANT_BUFFER)
    float4 unity_AmbientSky;
    float4 unity_AmbientEquator;
    float4 unity_AmbientGround;
#else
    half4 unity_AmbientSky;
    half4 unity_AmbientEquator;
    half4 unity_AmbientGround;
#endif
    // x = Mip Bias
    // y = 2.0 ^ [Mip Bias]
    float2 _GlobalMipBias;
    float _DitheringTextureInvSize;
    float _URPPadding1;
    // Environment
#if defined(URP_GLOBAL_CONSTANT_BUFFER)
    float4 _GlossyEnvironmentColor;
    float4 _SubtractiveShadowColor;
    float4 _GlossyEnvironmentCubeMap_HDR;
#else
    half4 _GlossyEnvironmentColor;
    half4 _SubtractiveShadowColor;
    half4 _GlossyEnvironmentCubeMap_HDR;
#endif
    // === Variables only present in 3D renderer start here ===
    // Main Light
    float4x4 _MainLightWorldToLight;
    float4 _MainLightPosition;
    // In Forward+, .a stores whether the main light is using subtractive mixed mode.
#if defined(URP_GLOBAL_CONSTANT_BUFFER)
    float4 _MainLightColor;
    float4 _MainLightOcclusionProbes;
#else
    half4 _MainLightColor;
    half4 _MainLightOcclusionProbes;
#endif
    // Forward+ Parameters. Declared unconditionally, unlike the URP_FP_* accessors in Input.hlsl that stay behind
    // USE_CLUSTER_LIGHT_LOOP: a constant buffer member cannot be conditional or the offsets stop matching C#.
    float4 _FPParams0;
    float4 _FPParams1;
    float4 _FPParams2;
    // Motion Vectors, non jittered. Declared unconditionally so the offsets stay fixed.
    float4x4 _NonJitteredViewProjMatrix;
    float4x4 _PrevViewProjMatrix;
    // Additional Lights. The arrays themselves stay loose uniforms, only the count lives here.
#if defined(URP_GLOBAL_CONSTANT_BUFFER)
    float4 _AdditionalLightsCount; // x holds the count
#else
    half4 _AdditionalLightsCount; // x holds the count
#endif
    // Main Light Shadows. Sized MAX_SHADOW_CASCADES + 1, spelled out because that define lives in Shadows.hlsl which is
    // included later. The last cascade is initialized with a no-op matrix, always transforming the shadow coord to
    // half3(0, 0, NEAR_PLANE), which avoids branching since ComputeCascadeIndex can return MAX_SHADOW_CASCADES.
    float4x4 _MainLightWorldToShadow[MAX_SHADOW_CASCADES + 1];
    float4 _CascadeShadowSplitSpheres0;
    float4 _CascadeShadowSplitSpheres1;
    float4 _CascadeShadowSplitSpheres2;
    float4 _CascadeShadowSplitSpheres3; // Last cascade is no-op, we don't provide an accessor to it
    float4 _CascadeShadowSplitSphereRadii;
    float4 _MainLightShadowOffset0; // xy: offset0, zw: offset1
    float4 _MainLightShadowOffset1; // xy: offset2, zw: offset3
    float4 _MainLightShadowmapSize; // xy: 1/width and 1/height, zw: width and height
    // Additional Lights Shadows
    float4 _AdditionalShadowOffset0; // xy: offset0, zw: offset1
    float4 _AdditionalShadowOffset1; // xy: offset2, zw: offset3
    float4 _AdditionalShadowmapSize; // xy: 1/width and 1/height, zw: width and height
    // Misc. The four scalars below pack into a single 16 byte row, keep them together.
    float _MainLightCookieTextureFormat;
    float _AdditionalLightsCookieAtlasTextureFormat;
    uint _RenderingLayerMaxInt;
    uint _EnableProbeVolumes;
    // Kept last on purpose: these scalars share a single 16 byte row, so the next 3D fields we add can consume the
    // remaining padding slots instead of needing padding of their own.
    uint _MainLightLayerMask;
    uint _URPPadding2;
    uint _URPPadding3;
    uint _URPPadding4;
    // === Variables only present in 2D renderer start here ===
    // float4 _URPDummy2D;
#if defined(URP_GLOBAL_CONSTANT_BUFFER)
CBUFFER_END
#endif

#endif // UNIVERSAL_GLOBAL_SHADER_VARIABLES_INCLUDED
