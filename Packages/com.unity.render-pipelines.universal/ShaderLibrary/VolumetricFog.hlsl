#ifndef UNIVERSAL_VOLUMETRIC_FOG_INCLUDED
#define UNIVERSAL_VOLUMETRIC_FOG_INCLUDED

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/VolumeRendering.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Macros.hlsl"

// The height-fog band used to attenuate the main directional light on surfaces and in the
// v-buffer lighting. Set from C# before opaque rendering; the fog type driving these values
// depends on what is active.
//
// When no Fog volume override is active, _MainLightFogDensity defaults to 0,
// which makes the helper a no-op.
float  _MainLightFogDensity;
float  _MainLightFogBaseHeight;
float2 _MainLightFogExponents;     // x = 1/H, y = H

// Returns the fraction of main-light radiance (0..1) that reaches positionWS after
// passing through the height-fog slab.
half ComputeMainLightFogAttenuation(float3 positionWS, float3 mainLightDirection)
{
    half attenuation = half(1.0);

#if defined(_VOLUMETRIC_FOG)
    if (_MainLightFogDensity > 0.0)
    {
        float cosZenithAngle = max(mainLightDirection.y, 0.001f);
        attenuation = TransmittanceHeightFog(_MainLightFogDensity, _MainLightFogBaseHeight,
                                             _MainLightFogExponents, cosZenithAngle, positionWS.y);
    }
#endif

    return attenuation;
}

// Everything below is the raster-only fog sampling API, and compute stages must skip it.
#if !defined(SHADER_STAGE_COMPUTE)

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/GlobalSamplers.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareExposureTexture.hlsl"

TEXTURE3D(_VBuffer);

float4 _DepthEncodingParams;
float4 _DepthDecodingParams;
float4 _VBufferSize;
float  _VBufferRcpSliceCount;
float  _VBufferLastSliceDist;       // Distance at the center of the last slice (along ray).
float4 _AnalyticFogColor;           // RGB used, A unused
float  _MaxFogDistance;             // Sky pixels are fogged as if at this along-ray distance.
float  _VolumetricFilteringEnabled; // 1 when the Gaussian spatial filter ran this frame.
float  _AnalyticFogEnabled;         // 1 when the analytic fog is enabled.
float  _AnalyticFogExtinction;      // Analytic fog extinction coefficient (per meter); fog density.
float  _AnalyticFogHeightMode;      // 1 to shape the analytic fog by the height falloff, 0 for uniform density.
float  _AnalyticFogBaseHeight;      // Height up to which the analytic fog stays at full density.
float2 _AnalyticFogExponents;       // x = 1/H, y = H for the analytic fog height falloff.

// Maps linear eye depth to the V-buffer's logarithmic Z slice coordinate (w), matching the
// froxel-Z parameterization the voxelizer/compute write with. The lookup is biased toward the
// camera to prevent the trilinear filter from bleeding into zeroed slices past the MaxZ early-out.
float VolumetricFogSliceCoord(float linearEyeDepth)
{
    float w = saturate(EncodeLogarithmicDepthGeneralized(linearEyeDepth, _DepthEncodingParams));
    return w - (sqrt(3.0) / 2.0) * _VBufferRcpSliceCount;
}

// A single trilinear tap (bilinear XY × linear Z).
float4 SampleVBufferTrilinear(float2 uv, float w)
{
    return SAMPLE_TEXTURE3D_LOD(_VBuffer, sampler_LinearClamp, float3(uv, w), 0);
}

// Composites the analytic fog covering (startDist, tFrag) along the ray onto (fogRadiance,
// opacity), which hold the fog already accumulated between the camera and startDist. Density
// (_AnalyticFogExtinction) is the sole extinction coefficient here; in Constant mode the fog is
// uniform (exp(-density * distance), matching Built-in exponential fog), and in Height mode it is
// shaped by the same height falloff as the volumetric fog.
void ApplyAnalyticFog(float cosZenith, float cameraHeight, float startDist, float tFrag,
                      inout float3 fogRadiance, inout float opacity)
{
    float distDelta = tFrag - startDist;
    if (_AnalyticFogEnabled != 0.0 && distDelta > 0.0)
    {
        float startHeight = cameraHeight + startDist * cosZenith;
        float odFallback = _AnalyticFogHeightMode != 0.0
            ? OpticalDepthHeightFog(_AnalyticFogExtinction, _AnalyticFogBaseHeight,
                                    _AnalyticFogExponents, cosZenith, startHeight, distDelta)
            : _AnalyticFogExtinction * distDelta;
        float trFallback = TransmittanceFromOpticalDepth(odFallback);
        float trCamera = 1.0 - opacity;
        fogRadiance += trCamera * _AnalyticFogColor.rgb * (1.0 - trFallback);
        opacity = 1.0 - (trCamera * trFallback);
    }
}

// Applies the volume-driven fog to a transparent surface's shaded color, before hardware
// blending. Sampling a transparent fragment's own depth is safe with respect to the zeroed
// slices past the MaxZ early-out because the fragment is always nearer than the opaque depth
// that built the mask, and VolumetricFogSliceCoord biases the lookup toward the camera.
// 'blendMode' follows BaseShaderGUI.BlendMode: 0 = Alpha, 1 = Premultiply, 2 = Additive,
// 3 = Multiply. 'positionCS' is the fragment's SV_Position.
half3 MixVolumetricFog(half3 color, half alpha, half blendMode, bool useAlphaPremultiply, float4 positionCS)
{
#if defined(_FOG_ANALYTIC) || defined(_FOG_VOLUMETRIC)
    float2 uv = GetNormalizedScreenSpaceUV(positionCS);
    float3 positionWS = ComputeWorldSpacePosition(uv, positionCS.z, UNITY_MATRIX_I_VP);
    float3 toFragment = positionWS - _WorldSpaceCameraPos.xyz;
    float tFrag = length(toFragment);

#if defined(_FOG_VOLUMETRIC)
    // positionCS.w is the fragment's linear eye depth for perspective projections; the fog
    // feature never runs for orthographic cameras.
    float w = VolumetricFogSliceCoord(positionCS.w);
    float4 fogSample = DelinearizeRGBD(SampleVBufferTrilinear(uv, w));
    float3 fogRadiance = fogSample.rgb;
    float opacity = OpacityFromOpticalDepth(fogSample.a);
    float lastSliceDist = _VBufferLastSliceDist;
#else
    float3 fogRadiance = 0.0;
    float opacity = 0.0;
    float lastSliceDist = 0.0;
#endif

    ApplyAnalyticFog(toFragment.y / tFrag, _WorldSpaceCameraPos.y, lastSliceDist, tFrag, fogRadiance, opacity);

    // The fog stack accumulates unexposed radiance, while callers pass in their already
    // pre-exposed shaded color, so the in-scattering must be exposed here to match.
    fogRadiance *= GetPreExposureMultiplier();

    bool premultipliedBlend = blendMode == 1.0 || useAlphaPremultiply;

    if (blendMode == 2.0)
    {
        // Additive surfaces only add light on top of an already fogged background, so the fog's
        // in-scattering is already on screen; extinction alone fades them out.
        color *= half(1.0 - opacity);
    }
    else if (blendMode == 3.0)
    {
        // Multiplicative surfaces scale the background; deep fog fades the factor to 1.
        color = lerp(color, half3(1.0, 1.0, 1.0), half(opacity));
    }
    else if (premultipliedBlend)
    {
        // Blended One / OneMinusSrcAlpha, so scale the in-scattering by coverage here: the
        // (1 - alpha) share of the pixel shows background that was already fogged at its own
        // depth by the fullscreen pass.
        color = color * half(1.0 - opacity) + half3(fogRadiance) * alpha;
    }
    else
    {
        // Plain alpha: the blend unit multiplies this output by alpha, which supplies the same
        // coverage scaling on the in-scattering.
        color = color * half(1.0 - opacity) + half3(fogRadiance);
    }
#endif

    return color;
}

#endif // !defined(SHADER_STAGE_COMPUTE)

#endif
