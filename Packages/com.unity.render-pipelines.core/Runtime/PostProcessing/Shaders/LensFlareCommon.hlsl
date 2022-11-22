#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Filtering.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"

#if SHADER_API_GLES
struct AttributesLensFlare
{
    float4 positionCS       : POSITION;
    float2 uv               : TEXCOORD0;

#ifndef FLARE_PREVIEW
    UNITY_VERTEX_INPUT_INSTANCE_ID
#endif
};
#else
struct AttributesLensFlare
{
    uint vertexID : SV_VertexID;

#ifndef FLARE_PREVIEW
    UNITY_VERTEX_INPUT_INSTANCE_ID
#endif
};
#endif

struct VaryingsLensFlare
{
    float4 positionCS : SV_POSITION;
    float2 texcoord : TEXCOORD0;

#if (defined(FLARE_OCCLUSION) || defined(FLARE_COMPUTE_OCCLUSION)) && !defined(FLARE_RESOLVE_OCCLUSION)
    float occlusion : TEXCOORD1;
#endif

#ifndef FLARE_PREVIEW
    UNITY_VERTEX_OUTPUT_STEREO
#endif
};

TEXTURE2D(_FlareTex);
SAMPLER(sampler_FlareTex);

TEXTURE2D(_FlareOcclusionRemapTex);
SAMPLER(sampler_FlareOcclusionRemapTex);

#if defined(HDRP_FLARE)
#if defined(FLARE_OCCLUSION)
TEXTURE2D_X(_FlareOcclusionTex);
SAMPLER(sampler_FlareOcclusionTex);
#endif
#if defined(FLARE_SAMPLE_WITH_VOLUMETRIC_CLOUD)
TEXTURE2D_X(_FlareSunOcclusionTex);
SAMPLER(sampler_FlareSunOcclusionTex);
#endif
#endif

float4 _FlareColorValue;
float4 _FlareData0; // x: localCos0, y: localSin0, zw: PositionOffsetXY
float4 _FlareData1; // x: OcclusionRadius, y: OcclusionSampleCount, z: ScreenPosZ, w: ScreenRatio
float4 _FlareData2; // xy: ScreenPos, zw: FlareSize
float4 _FlareData3; // x: Allow Offscreen, y: Edge Offset, z: Falloff, w: invSideCount
float4 _FlareData4; // x: SDF Roundness, y: Poly Radius, z: PolyParam0, w: PolyParam1

#ifdef FLARE_PREVIEW
float4 _FlarePreviewData;

#define _ScreenSize         _FlarePreviewData.xy;
#define _FlareScreenRatio   _FlarePreviewData.z;
#endif

float4 _FlareOcclusionIndex;

#define _FlareColor             _FlareColorValue

#define _LocalCos0              _FlareData0.x
#define _LocalSin0              _FlareData0.y
#define _PositionTranslate      _FlareData0.zw

#define _OcclusionRadius        _FlareData1.x
#define _OcclusionSampleCount   _FlareData1.y
#define _ScreenPosZ             _FlareData1.z
#ifndef _FlareScreenRatio
#define _FlareScreenRatio       _FlareData1.w
#endif

#define _ScreenPos              _FlareData2.xy
#define _FlareSize              _FlareData2.zw

#define _OcclusionOffscreen     _FlareData3.x
#define _FlareEdgeOffset        _FlareData3.y
#define _FlareFalloff           _FlareData3.z
#define _FlareShapeInvSide      _FlareData3.z

#define _FlareSDFRoundness      _FlareData4.x
#define _FlareSDFPolyRadius     _FlareData4.y
#define _FlareSDFPolyParam0     _FlareData4.z
#define _FlareSDFPolyParam1     _FlareData4.w

float2 Rotate(float2 v, float cos0, float sin0)
{
    return float2(v.x * cos0 - v.y * sin0,
                  v.x * sin0 + v.y * cos0);
}

#if defined(FLARE_OCCLUSION) || defined(FLARE_COMPUTE_OCCLUSION)
float GetLinearDepthValue(float2 uv)
{
#if defined(HDRP_FLARE) || defined(FLARE_PREVIEW)
    float depth = LOAD_TEXTURE2D_X_LOD(_CameraDepthTexture, uint2(uv * _ScreenSize.xy), 0).x;
#else
    float depth = LOAD_TEXTURE2D_X_LOD(_CameraDepthTexture, uint2(uv * GetScaledScreenParams().xy), 0).x;
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
        float2 pos = _ScreenPos.xy + dir;
        pos.xy = pos * 0.5f + 0.5f;
#ifdef UNITY_UV_STARTS_AT_TOP
        pos.y = 1.0f - pos.y;
#endif

        if (all(pos >= 0) && all(pos <= 1))
        {
            float depth0 = GetLinearDepthValue(pos);
#if defined(UNITY_REVERSED_Z)
            if (depth0 > _ScreenPosZ)
#else
            if (depth0 < _ScreenPosZ)
#endif
            {
#if defined(FLARE_SAMPLE_WITH_VOLUMETRIC_CLOUD)
                float cloudOcclusion = SAMPLE_TEXTURE2D_X_LOD(_FlareSunOcclusionTex, sampler_FlareSunOcclusionTex, pos, 0).w;
                contrib += sample_Contrib * saturate(cloudOcclusion);
#else
                contrib += sample_Contrib;
#endif
            }
        }
        else if (_OcclusionOffscreen > 0.0f)
        {
            contrib += sample_Contrib;
        }
    }

    contrib = SAMPLE_TEXTURE2D_LOD(_FlareOcclusionRemapTex, sampler_FlareOcclusionRemapTex, float2(saturate(contrib), 0.0f), 0.0f).x;
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

#if SHADER_API_GLES
    float4 posPreScale = input.positionCS;
    float2 uv = input.uv;
#else
    float4 posPreScale = float4(2.0f, 2.0f, 1.0f, 1.0f) * GetQuadVertexPosition(input.vertexID) - float4(1.0f, 1.0f, 0.0f, 0.0);
    float2 uv = GetQuadTexCoord(input.vertexID);
    uv.x = 1.0f - uv.x;
#endif

    output.texcoord.xy = uv;

    posPreScale.xy *= _FlareSize;
    float2 local = Rotate(posPreScale.xy, _LocalCos0, _LocalSin0);

    local.x *= screenRatio;

    output.positionCS.xy = local + _ScreenPos + _PositionTranslate;
    output.positionCS.z = 1.0f;
    output.positionCS.w = 1.0f;

#if defined(FLARE_OCCLUSION) && !defined(FLARE_RESOLVE_OCCLUSION)
    float occlusion = GetOcclusion(screenRatio);

    if (_OcclusionOffscreen < 0.0f && // No lens flare off screen
        (any(_ScreenPos.xy < -1) || any(_ScreenPos.xy >= 1)))
        occlusion = 0.0f;

    output.occlusion = occlusion;
#endif

    return output;
}
#endif

float InverseGradient(float x)
{
    // Do *not* simplify as 1.0f - x
    return x * (1.0f - x) / (x + 1e-6f);
}

float4 ComputeCircle(float2 uv)
{
    float2 v = (uv - 0.5f) * 2.0f;

    const float epsilon = 1e-3f;
    const float epsCoef = pow(epsilon, 1.0f / _FlareFalloff);

    float x = length(v);

    float sdf = saturate((x - 1.0f) / ((_FlareEdgeOffset - 1.0f)));

#if defined(FLARE_INVERSE_SDF)
    sdf = saturate(sdf);
    sdf = InverseGradient(sdf);
#endif

    return pow(sdf, _FlareFalloff);
}

// Modfied from ref: https://www.shadertoy.com/view/MtKcWW
// https://www.shadertoy.com/view/3tGBDt
float4 ComputePolygon(float2 uv_)
{
    float2 p = uv_ * 2.0f - 1.0f;

    float r = _FlareSDFPolyRadius;
    float an = _FlareSDFPolyParam0;
    float he = _FlareSDFPolyParam1;

    float bn = an * floor((atan2(p.y, p.x) + 0.5f * an) / an);
    float cos0 = cos(bn);
    float sin0 = sin(bn);
    p = float2( cos0 * p.x + sin0 * p.y,
               -sin0 * p.x + cos0 * p.y);

    // side of polygon
    float sdf = length(p - float2(r, clamp(p.y, -he, he))) * sign(p.x - r) - _FlareSDFRoundness;

    sdf *= _FlareEdgeOffset;

#if defined(FLARE_INVERSE_SDF)
    sdf = saturate(-sdf);
    sdf = InverseGradient(sdf);
#else
    sdf = saturate(-sdf);
#endif

    return saturate(pow(sdf, _FlareFalloff));
}

float4 GetFlareShape(float2 uv)
{
#ifdef FLARE_CIRCLE
    return ComputeCircle(uv);
#elif defined(FLARE_POLYGON)
    return ComputePolygon(uv);
#else
    return SAMPLE_TEXTURE2D(_FlareTex, sampler_FlareTex, uv);
#endif
}

float4 frag(VaryingsLensFlare input) : SV_Target
{
#ifndef FLARE_PREVIEW
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
#endif

    float4 col = GetFlareShape(input.texcoord);

#if defined(HDRP_FLARE)
    // HDRP
#if defined(FLARE_OCCLUSION)
#if defined(FLARE_RESOLVE_OCCLUSION)
    float occ = LOAD_TEXTURE2D_X_LOD(_FlareOcclusionTex, uint2(_FlareOcclusionIndex.x, 0), 0).x;

    return col * _FlareColor * occ;
#else
    return col * _FlareColor * input.occlusion;
#endif
#else
    return col * _FlareColor;
#endif
    // Not-HDRP => URP
#elif defined(FLARE_OCCLUSION)
    return col * _FlareColor * input.occlusion;
#else
    return col * _FlareColor;
#endif
}
