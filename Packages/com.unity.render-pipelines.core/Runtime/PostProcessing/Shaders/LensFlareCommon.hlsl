#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/PostProcessing/LensFlareDataSRP.cs.hlsl"
#include "Packages/com.unity.render-pipelines.core/Runtime/PostProcessing/LensFlareOcclusionPermutation.cs.hlsl"

struct AttributesLensFlare
{
    uint vertexID : SV_VertexID;

#ifndef FLARE_PREVIEW
    UNITY_VERTEX_INPUT_INSTANCE_ID
#endif
};

struct VaryingsLensFlare
{
    float4 positionCS : SV_POSITION;
    float2 texcoord : TEXCOORD0;
    float2 screenPos : TEXCOORD1;
#if defined(FLARE_HAS_OCCLUSION) || defined(FLARE_COMPUTE_OCCLUSION)
    float occlusion : TEXCOORD2;
#endif

#ifndef FLARE_PREVIEW
    UNITY_VERTEX_OUTPUT_STEREO
#endif
};

TEXTURE2D(_FlareTex);
SAMPLER(sampler_FlareTex);

TEXTURE2D(_FlareOcclusionRemapTex);
SAMPLER(sampler_FlareOcclusionRemapTex);

#if defined(FLARE_HAS_OCCLUSION)
TEXTURE2D_ARRAY(_FlareOcclusionTex);
SAMPLER(sampler_FlareOcclusionTex);
#endif

#ifdef HDRP_FLARE
uint _FlareOcclusionPermutation;

TEXTURE2D_X(_FlareSunOcclusionTex);
SAMPLER(sampler_FlareSunOcclusionTex);
#endif

float4 _FlareColorValue;
float4 _FlareData0; // x: localCos0, y: localSin0, zw: PositionOffsetXY
float4 _FlareData1; // x: OcclusionRadius, y: OcclusionSampleCount, z: ScreenPosZ, w: ScreenRatio
                    // Fragment Shader:
                    // x: LensFlareType, y: ElementIndex
float4 _FlareData2; // xy: ScreenPos, zw: FlareSize
float4 _FlareData3; // x: Allow Offscreen, y: Edge Offset, z: Falloff, w: invSideCount
                    // For Ring:
                    //                                                 w: RingThickness
float4 _FlareData4; // x: SDF Roundness, y: Poly Radius, z: PolyParam0, w: PolyParam1
                    // For Ring:
                    // x: noiseAmplitude, y: noiseFrequency, z: noiseSparsity, w: noiseSpeed
float4 _FlareData5; // x: ConstantColor, y: Intensity, z: shapeCutOffSpeed, w: cutoffRadius

TEXTURE2D(_FlareRadialTint);
SAMPLER(sampler_FlareRadialTint);
int _ViewId; // Used for XR index, for SinglePass and Multipass

#ifdef FLARE_PREVIEW
float4 _FlarePreviewData;

#define _ScreenSize         _FlarePreviewData.xy;
#define _FlareScreenRatio   _FlarePreviewData.z;
#endif

float4 _FlareOcclusionIndex;

#define _LocalCos0              _FlareData0.x
#define _LocalSin0              _FlareData0.y
#define _PositionTranslate      _FlareData0.zw

// Vertex _FlareData1
#define _OcclusionRadius        _FlareData1.x
#define _OcclusionSampleCount   _FlareData1.y
#define _ScreenPosZ             _FlareData1.z
#ifndef _FlareScreenRatio
#define _FlareScreenRatio       _FlareData1.w
#endif

// Fragment _FlareData1
#define _FlareType              ((int)_FlareData1.x)
#define _FlareElementIndex      ((int)_FlareData1.y)
#define _FlareHoopFactor        _FlareData1.z

#define _ScreenPos              _FlareData2.xy
#define _FlareSize              _FlareData2.zw

#define _OcclusionOffscreen     _FlareData3.x
#define _FlareEdgeOffset        _FlareData3.y
#define _FlareFalloff           _FlareData3.z
#define _FlareShapeInvSide      _FlareData3.w
#define _FlareRingThickness     _FlareData3.w

#define _FlareSDFRoundness      _FlareData4.x
#define _FlareSDFPolyRadius     _FlareData4.y
#define _FlareSDFPolyParam0     _FlareData4.z
#define _FlareSDFPolyParam1     _FlareData4.w

// For Ring only
#define _FlareNoiseAmplitude    _FlareData4.x
#define _FlareNoiseFrequency    _FlareData4.y
#define _FlareNoiseSpeed        _FlareData4.z

#define _IsFlareColorRadial     (_FlareData5.x == 1.0f)
#define _IsFlareColorAngular    (_FlareData5.x == 2.0f)

#define _FlareIntensity         _FlareData5.y
#define _FlareCutoffSpeed       _FlareData5.z
#define _FlareCutoffRadius      _FlareData5.w

void Rotate(out float2 rot, float2 v, float cos0, float sin0)
{
    rot = float2(v.x * cos0 - v.y * sin0,
                 v.x * sin0 + v.y * cos0);
}

#if defined(FLARE_COMPUTE_OCCLUSION) || defined(FLARE_OPENGL3_OR_OPENGLCORE)
float GetLinearDepthValue(float2 uv)
{
    float depth;

#if defined(HDRP_FLARE) || defined(FLARE_PREVIEW)
    if (_ViewId >= 0)
    {
        depth = LOAD_TEXTURE2D_ARRAY_LOD(_CameraDepthTexture, uint2(uv * _ScreenSize.xy), _ViewId, 0).x;
    }
    else
    {
        depth = LOAD_TEXTURE2D_X_LOD(_CameraDepthTexture, uint2(uv * _ScreenSize.xy), 0).x;
    }

#else
    depth = LOAD_TEXTURE2D_X_LOD(_CameraDepthTexture, uint2(uv * GetScaledScreenParams().xy), 0).x;

    if (_ViewId >= 0)
    {
#if defined(DISABLE_TEXTURE2D_X_ARRAY)
        // This should never happen in theory since _ViewId can only be >= 0 IF xr is enabled and so DISABLE_TEXTURE2D_X_ARRAY is disabled.
        // We just have to manage the DISABLE_TEXTURE2D_X_ARRAY variant here for avoiding warnings.
        // HDRP does not need this because it never uses DISABLE_TEXTURE2D_X_ARRAY.
        depth = LOAD_TEXTURE2D_LOD(_CameraDepthTexture, int2(uv * GetScaledScreenParams().xy), 0).x;
#else
        depth = LOAD_TEXTURE2D_ARRAY_LOD(_CameraDepthTexture, int2(uv * GetScaledScreenParams().xy), _ViewId, 0).x;
#endif
    }
    else
        depth = LOAD_TEXTURE2D_X_LOD(_CameraDepthTexture, uint2(uv * GetScaledScreenParams().xy), 0).x;

#endif

    return LinearEyeDepth(depth, _ZBufferParams);
}

float GetOcclusion(float ratio)
{
    if (_OcclusionSampleCount == 0.0f)
        return 1.0f;

    float contrib = 0.0f;
    float sample_Contrib = 1.0f / _OcclusionSampleCount;
    float2 ratioScale = float2(1.0f / ratio, 1.0);

    for (uint i = 0; i < (uint)_OcclusionSampleCount; i++)
    {
        float2 dir = _OcclusionRadius * SampleDiskUniform(Hash(2 * i + 0), Hash(2 * i + 1));
        float2 pos0 = _ScreenPos.xy + dir;
        float2 pos = pos0 * 0.5f + 0.5f;
#ifdef UNITY_UV_STARTS_AT_TOP
        pos.y = 1.0f - pos.y;
#endif

        if (all(pos >= 0) && all(pos <= 1))
        {
            float depth0 = GetLinearDepthValue(pos);

#if UNITY_REVERSED_Z
            if (depth0 > _ScreenPosZ)
#else
            if (depth0 < _ScreenPosZ)
#endif
            {
                float occlusionValue = 1.0f;

#ifdef HDRP_FLARE
                if ((_FlareOcclusionPermutation & LENSFLAREOCCLUSIONPERMUTATION_FOG_OPACITY) != 0)
                {
                    float fogOcclusion;
                    if (_ViewId >= 0)
                        fogOcclusion = SAMPLE_TEXTURE2D_ARRAY_LOD(_FlareSunOcclusionTex, sampler_FlareSunOcclusionTex, pos * _RTHandleScale.xy, _ViewId, 0).x;
                    else
                        fogOcclusion = SAMPLE_TEXTURE2D_X_LOD(_FlareSunOcclusionTex, sampler_FlareSunOcclusionTex, pos * _RTHandleScale.xy, 0).x;
                    occlusionValue *= saturate(fogOcclusion);
                }
#endif

                contrib += sample_Contrib * occlusionValue;
            }
        }
        else if (_OcclusionOffscreen > 0.0f)
        {
            contrib += sample_Contrib;
        }
    }

    contrib = SAMPLE_TEXTURE2D_LOD(_FlareOcclusionRemapTex, sampler_FlareOcclusionRemapTex, float2(saturate(contrib), 0.0f), 0).x;
    contrib = saturate(contrib);

    return contrib;
}
#endif

#if defined(FLARE_COMPUTE_OCCLUSION)
VaryingsLensFlare vertOcclusion(AttributesLensFlare input, uint instanceID : SV_InstanceID)
{
    VaryingsLensFlare output;

    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

#if defined(HDRP_FLARE) || defined(FLARE_PREVIEW)
    float screenRatio = _FlareScreenRatio;
#else
    float2 screenParam = GetScaledScreenParams().xy;
    float screenRatio = screenParam.y / screenParam.x;
#endif

    float2 quadPos = 2.0f * GetQuadVertexPosition(input.vertexID).xy - 1.0f;
    float2 uv = GetQuadTexCoord(input.vertexID);
    uv.x = 1.0f - uv.x;
    output.positionCS.xy = quadPos;

    output.texcoord.xy = uv;

    output.positionCS.z = 1.0f;
    output.positionCS.w = 1.0f;

    float occlusion = GetOcclusion(screenRatio);

    if (_OcclusionOffscreen < 0.0f && // No lens flare off screen
        (any(_ScreenPos.xy < -1) || any(_ScreenPos.xy >= 1)))
        occlusion = 0.0f;

    output.occlusion = occlusion;
    output.screenPos = output.positionCS.xy;

    return output;
}

float4 fragOcclusion(VaryingsLensFlare input) : SV_Target
{
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    return float4(input.occlusion.xxx, 1.0f);
}
#else
VaryingsLensFlare vert(AttributesLensFlare input, uint instanceID : SV_InstanceID)
{
    VaryingsLensFlare output;

#ifndef FLARE_PREVIEW
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
#endif

#if defined(HDRP_FLARE) || defined(FLARE_PREVIEW)
    float screenRatio = _FlareScreenRatio;
#else
    float2 screenParam = GetScaledScreenParams().xy;
    float screenRatio = screenParam.y / screenParam.x;
#endif

    float4 posPreScale = float4(2.0f, 2.0f, 1.0f, 1.0f) * GetQuadVertexPosition(input.vertexID) - float4(1.0f, 1.0f, 0.0f, 0.0);
    float2 uv = GetQuadTexCoord(input.vertexID);
    uv.x = 1.0f - uv.x;

    output.texcoord.xy = uv;

    posPreScale.xy *= _FlareSize;
    float2 local;
    Rotate(local, posPreScale.xy, _LocalCos0, _LocalSin0);

    local.x *= screenRatio;

    output.positionCS.xy = local + _ScreenPos + _PositionTranslate;
    output.positionCS.z = 1.0f;
    output.positionCS.w = 1.0f;

#if defined(FLARE_HAS_OCCLUSION)
    float occlusion;

    if (_OcclusionOffscreen < 0.0f && // No lens flare off screen
        (any(_ScreenPos.xy < -1.0f) || any(_ScreenPos.xy > 1.0f)))
    {
        occlusion = 0.0f;
    }
    else
    {
#if defined(FLARE_OPENGL3_OR_OPENGLCORE)

#if defined(HDRP_FLARE) || defined(FLARE_PREVIEW)
        float screenRatio = _FlareScreenRatio;
#else
        float2 screenParam = GetScaledScreenParams().xy;
        float screenRatio = screenParam.y / screenParam.x;
#endif

        occlusion = GetOcclusion(screenRatio);

#else // defined(FLARE_OPENGL3_OR_OPENGLCORE)

        if (_ViewId >= 0)
            occlusion = LOAD_TEXTURE2D_ARRAY_LOD(_FlareOcclusionTex, uint2(_FlareOcclusionIndex.x, 0), _ViewId, 0).x;
        else
            occlusion = LOAD_TEXTURE2D_ARRAY_LOD(_FlareOcclusionTex, uint2(_FlareOcclusionIndex.x, 0), 0, 0).x;

#endif // defined(FLARE_OPENGL3_OR_OPENGLCORE)
    }

    output.occlusion = occlusion;
#endif
    output.screenPos = output.positionCS.xy;

    return output;
}
#endif

// Constrains: x in [0.0f, 1.0f]
float InverseGradient(float x)
{
    // Before:

    // Do *not* simplify as 1.0f - x
    //return x * (1.0f - x) / (x + eps);
    // Kind of equivalent of (without the edge smoothness control):
    //if (0.0f < x && x < 1.0f)
    //    return 1.0f - x;
    //else
    //    return 0.0f;

    // After:

    // Larger eps give smoother boundary-edges
    const float eps = 1e-3f;
    // cf. https://www.desmos.com/calculator/0hprry9l90
    // Rescale to always have the max at 1.0f;
    //
    // The max of 'x * (1.0f - x) / (x + eps)'
    // Is when x == sqrt(eps (1 + eps)) - eps
    // which is => 1 + 2*eps - 2*sqrt(eps*(1 + eps))
    // We can rescale by scaling with 1/maxValue
    // => x*(1 - x)/(x + eps)*(1/(1 + 2*eps - 2*sqrt(eps*(1 + eps))))
    // Simplifed as:
    // Compile-time variables:
    const float eps2 = 2.0f * eps;
    const float scale = 2.0f * sqrt(eps * (1.0f + eps));
    const float a = scale - eps2 - 1.0f;
    const float b = a * eps;

    //return x * (x - 1.0f) / (a * x + b);
    // to:
    return mad(x, x, -x) * rcp(mad(a, x, b));
}

float CircleSDF(float2 center, float2 pos, float r)
{
    return length(pos - center) - r;
}

float SDFBlocker(float2 pos, float2 screenPos, float ar, float r)
{
    float2 localPos = _ScreenPos - screenPos;
    localPos.y = -localPos.y;

    float2 offset = localPos * _FlareCutoffSpeed;

    float2 rot;
    Rotate(rot, offset, _LocalCos0, _LocalSin0);

    pos.y *= ar;

    return CircleSDF(pos, rot, _FlareCutoffRadius * r);
}

float ComputeCircle(float2 uv, float2 screenPos)
{
    float2 v = 2.0f * uv - 1.0f;

    float radius = 1.0f;
    float sdfBlocker = SDFBlocker(v, screenPos, _FlareSize.y / _FlareSize.x, radius);
    float sdfSrc = CircleSDF(v, float2(0.0f, 0.0f), radius);

    float sdf = max(sdfSrc, sdfBlocker);

    sdf = saturate(sdf / ((_FlareEdgeOffset - radius)));

#if defined(FLARE_INVERSE_SDF)
    sdf = saturate(sdf);
    sdf = InverseGradient(sdf);
#endif

    sdf = pow(saturate(sdf), _FlareFalloff);

    return sdf;
}

// Modfied from ref: https://www.shadertoy.com/view/MtKcWW
// https://www.shadertoy.com/view/3tGBDt
float ComputePolygon(float2 uv_, float2 screenPos)
{
    float2 v = 2.0f * uv_ - 1.0f;

    float r  = _FlareSDFPolyRadius;
    float an = _FlareSDFPolyParam0;
    float he = _FlareSDFPolyParam1;

    float bn = an * floor((atan2(v.y, v.x) + 0.5f * an) / an);
    float cos0 = cos(bn);
    float sin0 = sin(bn);
    float2 p = float2( cos0 * v.x + sin0 * v.y,
                      -sin0 * v.x + cos0 * v.y);

    // side of polygon
    float sdf = length(p - float2(r, clamp(p.y, -he, he))) * sign(p.x - r) - _FlareSDFRoundness;

    float sdfBlocker = SDFBlocker(v, screenPos, _FlareSize.y / _FlareSize.x, r);
    sdf = max(sdf, sdfBlocker);

    sdf *= _FlareEdgeOffset;

#if defined(FLARE_INVERSE_SDF)
    sdf = saturate(-sdf);
    sdf = InverseGradient(sdf);
#else
    sdf = saturate(-sdf);
#endif

    sdf = pow(saturate(sdf), _FlareFalloff);

    return sdf;
}

float4 Interpolation_C2_InterpAndDeriv(float2 x)
{
    return x.xyxy * x.xyxy * (x.xyxy * (x.xyxy * (x.xyxy * float2(6.0f, 0.0f).xxyy + float2(-15.0f, 30.0f).xxyy) + float2(10.0f, -60.0f).xxyy) + float2(0.0f, 30.0f).xxyy);
}

// Generates a random number for each of the 4 cell corners
float4 NoiseHash2D(float2 gridcell)
{
    float2 kOffset = float2(26.0f, 161.0f);
    float kDomain = 71.0f;
    float kLargeFloat = 1.0f / 951.135664f;

    float4 P = float4(gridcell.xy, gridcell.xy + 1.0f);
    P = P - floor(P * (1.0f / kDomain)) * kDomain;  // truncate the domain
    P += kOffset.xyxy;                              // offset to interesting part of the noise
    P *= P;                                         // calculate and return the hash
    return frac(P.xzxz * P.yyww * kLargeFloat);
}

float GenerateValueNoise2D(float2 coordinate)
{
    float2 i = floor(coordinate);
    float2 f = coordinate - i;

    float4 hash = NoiseHash2D(i);

    float4 blend = Interpolation_C2_InterpAndDeriv(f);
    float4 res0 = lerp(hash.xyxz, hash.zwyw, blend.yyxx);
    float2 resDelta = res0.yw - res0.xz;

    float noise = res0.x + resDelta.x * blend.x;

    return noise;
}

float ComputeRing(float2 uv_, float2 screenPos)
{
    float2 pos = 2.0f * uv_ - 1.0f;

#ifndef FLARE_PREVIEW
    float iTime = _Time.y;
#else
    float iTime = 0;
#endif

    float r = 1.0f;

    float ang = FastAtan2(pos.y, pos.x);

    float2 sc;
    float noiseAng = ang * _FlareNoiseFrequency;
    sincos(noiseAng, sc.x, sc.y);

    float noise = abs(abs(GenerateValueNoise2D(_FlareNoiseAmplitude * sc + _FlareElementIndex.xx + _FlareNoiseSpeed * iTime)));

    float sdfBlocker = CircleSDF(pos, float2(0.0f, 0.0f), r);
    float sdf;
    sdf = abs(CircleSDF(pos, 0.0f.xx, (1.0f - _FlareRingThickness))) - _FlareRingThickness;
    sdf = max(noise * sdf, sdfBlocker);

    sdf = saturate(sdf / ((_FlareEdgeOffset - r)));

#if defined(FLARE_INVERSE_SDF)
    sdf = saturate(sdf);
    sdf = InverseGradient(sdf);
#endif

    sdf = pow(saturate(sdf), _FlareFalloff);

    float sdfBlockerCutoff = SDFBlocker(pos, screenPos, _FlareSize.y / _FlareSize.x, 1.0f);
    sdf *= saturate(-sdfBlockerCutoff > 0.0f);

    return sdf;
}

float4 GetFlareShape(float2 uv, float2 screenPos)
{
    float4 flareColor = 1.0f;

    float shape;

    if (_FlareType == SRPLENSFLARETYPE_CIRCLE)
        shape = ComputeCircle(uv, screenPos);
    else if (_FlareType == SRPLENSFLARETYPE_POLYGON)
        shape = ComputePolygon(uv, screenPos);
    else if (_FlareType == SRPLENSFLARETYPE_RING)
        shape = ComputeRing(uv, screenPos);
    else if (_FlareType == SRPLENSFLARETYPE_IMAGE)
    {
        shape = 1.0f;
        flareColor = SAMPLE_TEXTURE2D(_FlareTex, sampler_FlareTex, uv);
        float sdfBlocker = SDFBlocker(2.0f * uv - 1.0f, screenPos, _FlareSize.y / _FlareSize.x, 1.0f);
        flareColor *= saturate(-sdfBlocker > 0.0f);
    }
    else
    {
        // Not possible, if the code execute this lines, we have a bug on the code
        // SRPLENSFLARETYPE_LENS_FLARE_DATA_SRP should never be executed
        shape = -1.0f;
    }

#if defined(HDRP_FLARE) || defined(FLARE_PREVIEW)
    float screenRatio = _FlareScreenRatio;
#else
    float2 screenParam = GetScaledScreenParams().xy;
    float screenRatio = screenParam.y / screenParam.x;
#endif

    float2 pos = 2.0f * uv - 1.0f;
    if (_IsFlareColorRadial)
    {
        float radius = length(pos);
        if (_FlareType == SRPLENSFLARETYPE_RING)
        {
            float offset = 1.0f - _FlareRingThickness;
            float a = (1.0f / clamp(2.0f * _FlareRingThickness, _FlareRingThickness, 1.0f));
            float b = 1.0f - a;
            radius = saturate(a * radius + b);
        }
        float4 grad = SAMPLE_TEXTURE2D_LOD(_FlareRadialTint, sampler_FlareRadialTint, float2(saturate(radius), 0.0f), 0);
        flareColor *= grad;
    }
    else if (_IsFlareColorAngular)
    {
        float angle01 = (atan2(pos.y, pos.x) + PI) / TWO_PI;
        float4 grad = SAMPLE_TEXTURE2D_LOD(_FlareRadialTint, sampler_FlareRadialTint, float2(saturate(angle01), 0.0f), 0);
        flareColor *= grad;
    }
    flareColor *= _FlareColorValue;
    flareColor.rgb *= _FlareIntensity;

#ifdef FLARE_ADDITIVE_BLEND
    float4 finalValue = float4(flareColor.rgb * shape, shape * flareColor.a);
#elif defined(FLARE_SCREEN_BLEND)
    float4 finalValue = float4(flareColor.rgb * shape, shape * flareColor.a);
#elif defined(FLARE_PREMULTIPLIED_BLEND)
    float4 finalValue = float4(flareColor.rgb * shape, shape * flareColor.a);
#elif defined(FLARE_LERP_BLEND)
    float4 finalValue = float4(flareColor.rgb, shape * flareColor.a);
#else
    float4 finalValue = flareColor * shape;
#endif

    return finalValue;
}

float4 frag(VaryingsLensFlare input) : SV_Target
{
#ifndef FLARE_PREVIEW
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
#endif

#if defined(FLARE_HAS_OCCLUSION)
    if (input.occlusion > 0.0f)
    {
        float4 col = GetFlareShape(input.texcoord, input.screenPos.xy);

        return col * input.occlusion;
    }
    else
    {
        return float4(0.0f, 0.0f, 0.0f, 0.0f);
    }
#else
    float4 col = GetFlareShape(input.texcoord, input.screenPos.xy);

    return col;
#endif
}
