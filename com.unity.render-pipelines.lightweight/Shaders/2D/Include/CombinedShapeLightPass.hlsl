#if !defined(COMBINED_SHAPE_LIGHT_PASS)
#define COMBINED_SHAPE_LIGHT_PASS

struct Attributes
{
    float3 positionOS   : POSITION;
    float4 color        : COLOR;
    half2  uv           : TEXCOORD0;
};

struct Varyings
{
    float4  positionCS      : SV_POSITION;
    float4  color           : COLOR;
    half2	uv              : TEXCOORD0;
    half2	lightingUV      : TEXCOORD1;
    float4  vertexWorldPos  : TEXCOORD3;
    half2	pixelScreenPos  : TEXCOORD4;
};

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_MaskTex);
SAMPLER(sampler_MaskTex);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
half4 _MainTex_ST;
half4 _NormalMap_ST;

#if USE_SHAPE_LIGHT_TYPE_0

TEXTURE2D(_ShapeLightTexture0);
SAMPLER(sampler_ShapeLightTexture0);
float2 _ShapeLightBlendFactors0;
float4 _ShapeLightMaskFilter0;
float4 _ShapeLightInvertedFilter0;

#endif

#if USE_SHAPE_LIGHT_TYPE_1

TEXTURE2D(_ShapeLightTexture1);
SAMPLER(sampler_ShapeLightTexture1);
float2 _ShapeLightBlendFactors1;
float4 _ShapeLightMaskFilter1;
float4 _ShapeLightInvertedFilter1;

#endif

#if USE_SHAPE_LIGHT_TYPE_2

TEXTURE2D(_ShapeLightTexture2);
SAMPLER(sampler_ShapeLightTexture2);
float2 _ShapeLightBlendFactors2;
float4 _ShapeLightMaskFilter2;
float4 _ShapeLightInvertedFilter2;

#endif

#if USE_SHAPE_LIGHT_TYPE_3

TEXTURE2D(_ShapeLightTexture3);
SAMPLER(sampler_ShapeLightTexture3);
float2 _ShapeLightBlendFactors3;
float4 _ShapeLightMaskFilter3;
float4 _ShapeLightInvertedFilter3;

#endif

Varyings CombinedShapeLightVertex(Attributes v)
{
    Varyings o = (Varyings)0;

    o.positionCS = TransformObjectToHClip(v.positionOS);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    float4 clipVertex = o.positionCS / o.positionCS.w;
    o.lightingUV = ComputeScreenPos(clipVertex).xy;

#if UNITY_UV_STARTS_AT_TOP
    o.lightingUV.y = 1.0 - o.lightingUV.y;
#endif

    float4 worldVertex;
    worldVertex.xyz = TransformObjectToWorld(v.positionOS);
    worldVertex.w = 1;
    o.pixelScreenPos = ComputeScreenPos(worldVertex).xy;
    o.color = v.color;
    o.vertexWorldPos = worldVertex;
    return o;
}

half4 CombinedShapeLightFragment(Varyings i) : SV_Target
{
    half4 main = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.uv);

#if USE_SHAPE_LIGHT_TYPE_0
    half4 shapeLight0 = SAMPLE_TEXTURE2D(_ShapeLightTexture0, sampler_ShapeLightTexture0, i.lightingUV);

    if (any(_ShapeLightMaskFilter0))
    {
        float4 processedMask = (1 - _ShapeLightInvertedFilter0) * mask + _ShapeLightInvertedFilter0 * (1 - mask);
        shapeLight0 *= dot(processedMask, _ShapeLightMaskFilter0);
    }

    half4 shapeLight0Modulate = shapeLight0 * _ShapeLightBlendFactors0.x;
    half4 shapeLight0Additive = shapeLight0 * _ShapeLightBlendFactors0.y;
#else
    half4 shapeLight0Modulate = 0;
    half4 shapeLight0Additive = 0;
#endif

#if USE_SHAPE_LIGHT_TYPE_1
    half4 shapeLight1 = SAMPLE_TEXTURE2D(_ShapeLightTexture1, sampler_ShapeLightTexture1, i.lightingUV);

    if (any(_ShapeLightMaskFilter1))
    {
        float4 processedMask = (1 - _ShapeLightInvertedFilter1) * mask + _ShapeLightInvertedFilter1 * (1 - mask);
        shapeLight1 *= dot(processedMask, _ShapeLightMaskFilter1);
    }

    half4 shapeLight1Modulate = shapeLight1 * _ShapeLightBlendFactors1.x;
    half4 shapeLight1Additive = shapeLight1 * _ShapeLightBlendFactors1.y;
#else
    half4 shapeLight1Modulate = 0;
    half4 shapeLight1Additive = 0;
#endif

#if USE_SHAPE_LIGHT_TYPE_2
    half4 shapeLight2 = SAMPLE_TEXTURE2D(_ShapeLightTexture2, sampler_ShapeLightTexture2, i.lightingUV);

    if (any(_ShapeLightMaskFilter2))
    {
        float4 processedMask = (1 - _ShapeLightInvertedFilter2) * mask + _ShapeLightInvertedFilter2 * (1 - mask);
        shapeLight2 *= dot(processedMask, _ShapeLightMaskFilter2);
    }

    half4 shapeLight2Modulate = shapeLight2 * _ShapeLightBlendFactors2.x;
    half4 shapeLight2Additive = shapeLight2 * _ShapeLightBlendFactors2.y;
#else
    half4 shapeLight2Modulate = 0;
    half4 shapeLight2Additive = 0;
#endif

#if USE_SHAPE_LIGHT_TYPE_3
    half4 shapeLight3 = SAMPLE_TEXTURE2D(_ShapeLightTexture3, sampler_ShapeLightTexture3, i.lightingUV);

    if (any(_ShapeLightMaskFilter3))
    {
        float4 processedMask = (1 - _ShapeLightInvertedFilter3) * mask + _ShapeLightInvertedFilter3 * (1 - mask);
        shapeLight3 *= dot(processedMask, _ShapeLightMaskFilter3);
    }

    half4 shapeLight3Modulate = shapeLight3 * _ShapeLightBlendFactors3.x;
    half4 shapeLight3Additive = shapeLight3 * _ShapeLightBlendFactors3.y;
#else
    half4 shapeLight3Modulate = 0;
    half4 shapeLight3Additive = 0;
#endif

    half4 finalOutput;
    half4 finalModulate = shapeLight0Modulate + shapeLight1Modulate + shapeLight2Modulate + shapeLight3Modulate;
    half4 finalAdditve = shapeLight0Additive + shapeLight1Additive + shapeLight2Additive + shapeLight3Additive;
    finalOutput = main * finalModulate + finalAdditve;

    finalOutput.a = main.a;
    return finalOutput;
}
#endif
