#if !defined(COMBINED_SHAPE_LIGHT_PASS)
#define COMBINED_SHAPE_LIGHT_PASS

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
TEXTURE2D(_MaskTex);
SAMPLER(sampler_MaskTex);
TEXTURE2D(_PointLightingTex);
SAMPLER(sampler_PointLightingTex);
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
uniform half4 _MainTex_ST;
uniform half4 _NormalMap_ST;

#if USE_SHAPE_LIGHT_TYPE_0
    TEXTURE2D(_ShapeLightTexture0);
    SAMPLER(sampler_ShapeLightTexture0);
    uniform float2 _ShapeLightBlendFactors0;
    uniform float4 _ShapeLightMaskFilter0;
#endif

#if USE_SHAPE_LIGHT_TYPE_1
    TEXTURE2D(_ShapeLightTexture1);
    SAMPLER(sampler_ShapeLightTexture1);
    uniform float2 _ShapeLightBlendFactors1;
    uniform float4 _ShapeLightMaskFilter1;
#endif

#if USE_SHAPE_LIGHT_TYPE_2
    TEXTURE2D(_ShapeLightTexture2);
    SAMPLER(sampler_ShapeLightTexture2);
    uniform float2 _ShapeLightBlendFactors2;
    uniform float4 _ShapeLightMaskFilter2;
#endif

Varyings CombinedShapeLightVertex(Attributes v)
{
    Varyings o;
	o.positionCS = TransformObjectToHClip(v.positionOS);
	o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    float4 clipVertex = o.positionCS / o.positionCS.w;
	o.lightingUV = ComputeScreenPos(clipVertex);

#if UNITY_UV_STARTS_AT_TOP
    o.lightingUV.y = 1.0 - o.lightingUV.y;
#endif

	float4 worldVertex;
	worldVertex.xyz = TransformObjectToWorld(v.positionOS);
	worldVertex.w = 1;
	o.pixelScreenPos = ComputeScreenPos(worldVertex);
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
        shapeLight0 *= dot(mask, _ShapeLightMaskFilter0);

    half4 shapeLight0Modulate = shapeLight0 * _ShapeLightBlendFactors0.x;
    half4 shapeLight0Additive = shapeLight0 * _ShapeLightBlendFactors0.y;
#else
    half4 shapeLight0Modulate = 0;
    half4 shapeLight0Additive = 0;
#endif

#if USE_SHAPE_LIGHT_TYPE_1
    half4 shapeLight1 = SAMPLE_TEXTURE2D(_ShapeLightTexture1, sampler_ShapeLightTexture1, i.lightingUV);

    if (any(_ShapeLightMaskFilter1))
        shapeLight1 *= dot(mask, _ShapeLightMaskFilter1);

    half4 shapeLight1Modulate = shapeLight1 * _ShapeLightBlendFactors1.x;
    half4 shapeLight1Additive = shapeLight1 * _ShapeLightBlendFactors1.y;
#else
    half4 shapeLight1Modulate = 0;
    half4 shapeLight1Additive = 0;
#endif

#if USE_SHAPE_LIGHT_TYPE_2
    half4 shapeLight2 = SAMPLE_TEXTURE2D(_ShapeLightTexture2, sampler_ShapeLightTexture2, i.lightingUV);

    if (any(_ShapeLightMaskFilter2))
        shapeLight2 *= dot(mask, _ShapeLightMaskFilter2);

    half4 shapeLight2Modulate = shapeLight2 * _ShapeLightBlendFactors2.x;
    half4 shapeLight2Additive = shapeLight2 * _ShapeLightBlendFactors2.y;
#else
    half4 shapeLight2Modulate = 0;
    half4 shapeLight2Additive = 0;
#endif

#if USE_POINT_LIGHTS
    float2 lightingUV = i.lightingUV;

#if UNITY_UV_STARTS_AT_TOP
    lightingUV.y = 1.0 - lightingUV.y;
#endif

    half4 pointLight = SAMPLE_TEXTURE2D(_PointLightingTex, sampler_PointLightingTex, lightingUV);
#else
    half4 pointLight = 0;
#endif

    half4 finalOutput;
    half4 finalModulate = shapeLight0Modulate + shapeLight1Modulate + shapeLight2Modulate + pointLight;
    half4 finalAdditve = shapeLight0Additive + shapeLight1Additive + shapeLight2Additive;
    finalOutput = main * finalModulate + finalAdditve;

    finalOutput.a = main.a;
    return finalOutput;
}

#endif
