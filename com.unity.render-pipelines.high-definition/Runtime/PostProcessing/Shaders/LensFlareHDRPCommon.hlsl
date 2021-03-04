#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Random.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Sampling/Sampling.hlsl"

struct Attributes
{
    uint vertexID : SV_VertexID;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS : SV_POSITION;
    float2 texcoord : TEXCOORD0;
    float occlusion : TEXCOORD1;
    UNITY_VERTEX_OUTPUT_STEREO
};

sampler2D _FlareTex;
TEXTURE2D_X(_FlareOcclusionBufferTex);

float4 _FlareColor;
float4 _FlareData0; // x: localCos0, y: localSin0, zw: PositionOffsetXY
float4 _FlareData1; // x: OcclusionRadius, y: OcclusionSampleCount, z: ScreenPosZ, w: Falloff
float4 _FlareData2; // xy: ScreenPos, zw: FlareSize
float4 _FlareData3; // xy: RayOffset, z: invSideCount, w: Edge Offset
float4 _FlareData4; // x: SDF Roundness

#define _LocalCos0          _FlareData0.x
#define _LocalSin0          _FlareData0.y
#define _PositionOffset     _FlareData0.zw

#define _ScreenPosZ         _FlareData1.z
#define _FlareFalloff       _FlareData1.w

#define _ScreenPos          _FlareData2.xy
#define _FlareSize          _FlareData2.zw

#define _FlareRayOffset     _FlareData3.xy
#define _FlareShapeInvSide  _FlareData3.z
#define _FlareEdgeOffset    _FlareData3.w

#define _FlareSDFRoundness  _FlareData4.x

float2 Rotate(float2 v, float cos0, float sin0)
{
    return float2(v.x * cos0 - v.y * sin0,
                  v.x * sin0 + v.y * cos0);
}

Varyings vert(Attributes input)
{
    Varyings output;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    float screenRatio = _ScreenSize.y / _ScreenSize.x;

    float4 posPreScale = float4(2.0f, 2.0f, 1.0f, 1.0f) * GetQuadVertexPosition(input.vertexID) - float4(1.0f, 1.0f, 0.0f, 0.0);
    output.texcoord = GetQuadTexCoord(input.vertexID);

    posPreScale.xy *= _FlareSize;
    float2 local = Rotate(posPreScale.xy, _LocalCos0, _LocalSin0);

    local.x *= screenRatio;

    output.positionCS.xy = local + _ScreenPos + _FlareRayOffset + _PositionOffset;
    output.positionCS.zw = posPreScale.zw;

    output.occlusion = 1.0f;

    return output;
}

float4 ComputeGlow(float2 uv)
{
    float2 v = (uv - 0.5f) * 2.0f;

#if FLARE_INVERSE_SDF
    float sdf = saturate(-length(v) + _FlareEdgeOffset);
    // Cannot be simplify as 1 - sdf
    sdf = sdf * (1.0f - sdf) / (sdf + 1e-6f);
#else
    float sdf = saturate(-length(v) + 1.0f + _FlareEdgeOffset);
#endif

    return pow(sdf, _FlareFalloff);
}

// Ref: https://www.shadertoy.com/view/MtKcWW
float4 ComputeIris(float2 uv_)
{
    const float r = _FlareEdgeOffset - _FlareSDFRoundness;

    // these 2 lines can be precomputed
    float an = 6.2831853f * _FlareShapeInvSide;
    float he = r * tan(0.5f * an);

    float2 p = (uv_ - 0.5f) * 2.0f;

    p = -p.yx;
    float bn = an * floor((atan2(p.y, p.x) + 0.5f * an) / an);
    float cos0 = cos(bn);
    float sin0 = sin(bn);
    p = float2( cos0 * p.x + sin0 * p.y,
               -sin0 * p.x + cos0 * p.y);

    // side of polygon
    float sdf = length(p - float2(r, clamp(p.y, -he, he))) * sign(p.x - r) - _FlareSDFRoundness;

#if FLARE_INVERSE_SDF
    sdf = saturate(-sdf);
    sdf = sdf * (1.0f - sdf) / (sdf + 1e-6f);
#else
    sdf = sdf * (1.0f - sdf) / (sdf + 1e-6f);
    sdf = saturate(sdf);
#endif

    return saturate(pow(sdf, _FlareFalloff));
}

float4 GetFlareShape(float2 uv)
{
#if FLARE_GLOW
    return ComputeGlow(uv);
#elif FLARE_IRIS
    return ComputeIris(uv);
#else
    return tex2D(_FlareTex, uv);
#endif
}

