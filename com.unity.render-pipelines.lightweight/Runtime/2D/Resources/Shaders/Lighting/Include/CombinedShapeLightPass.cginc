#if !defined(COMBINED_SHAPE_LIGHT_PASS)
#define COMBINED_SHAPE_LIGHT_PASS

#include "UnityCG.cginc"

struct appdata
{
	float4 vertex : POSITION;
	fixed2 uv : TEXCOORD0;
	fixed4 color : COLOR;
};

struct v2f
{
	float4 vertex : SV_POSITION;
	fixed4 color : COLOR;
	fixed2 uv : TEXCOORD0;
	fixed2 lightingUV : TEXCOORD1;
	float4 vertexWorldPos : TEXCOORD3;
	fixed2 pixelScreenPos : TEXCOORD4;
};

uniform sampler2D _MainTex;
uniform fixed4 _MainTex_ST;
uniform sampler2D _MaskTex;
uniform fixed4 _MaskTex_ST;
uniform sampler2D _NormalMap;
uniform fixed4 _NormalMap_ST;

uniform sampler2D _PointLightingTex;

#if USE_SHAPE_LIGHT_TYPE_0
    uniform sampler2D _ShapeLightTexture0;
    uniform float2 _ShapeLightBlendFactors0;
    uniform float4 _ShapeLightMaskFilter0;
#endif

#if USE_SHAPE_LIGHT_TYPE_1
    uniform sampler2D _ShapeLightTexture1;
    uniform float2 _ShapeLightBlendFactors1;
    uniform float4 _ShapeLightMaskFilter1;
#endif

#if USE_SHAPE_LIGHT_TYPE_2
    uniform sampler2D _ShapeLightTexture2;
    uniform float2 _ShapeLightBlendFactors2;
    uniform float4 _ShapeLightMaskFilter2;
#endif

v2f CombinedShapeLightVertex(appdata v)
{
	v2f o;
	o.vertex = UnityObjectToClipPos(v.vertex);
	o.uv = TRANSFORM_TEX(v.uv, _MainTex);
	float4 clipVertex = o.vertex / o.vertex.w;
	o.lightingUV = ComputeScreenPos(clipVertex);
    o.lightingUV.y = 1.0 - o.lightingUV.y;

	float4 worldVertex = mul(unity_ObjectToWorld, v.vertex);
	o.pixelScreenPos = ComputeScreenPos(worldVertex);
	o.color = v.color;

	o.vertexWorldPos = mul(unity_ObjectToWorld, v.vertex);
	return o;
}

fixed4 CombinedShapeLightFragment(v2f i) : SV_Target
{
    fixed4 main = i.color * tex2D(_MainTex, i.uv);
    fixed4 mask = tex2D(_MaskTex, i.uv);

#if USE_SHAPE_LIGHT_TYPE_0
    fixed4 shapeLight0 = tex2D(_ShapeLightTexture0, i.lightingUV);

    if (any(_ShapeLightMaskFilter0))
        shapeLight0 *= dot(mask, _ShapeLightMaskFilter0);

    fixed4 shapeLight0Modulate = shapeLight0 * _ShapeLightBlendFactors0.x;
    fixed4 shapeLight0Additive = shapeLight0 * _ShapeLightBlendFactors0.y;
#else
    fixed4 shapeLight0Modulate = 0;
    fixed4 shapeLight0Additive = 0;
#endif

#if USE_SHAPE_LIGHT_TYPE_1
    fixed4 shapeLight1 = tex2D(_ShapeLightTexture1, i.lightingUV);

    if (any(_ShapeLightMaskFilter1))
        shapeLight1 *= dot(mask, _ShapeLightMaskFilter1);

    fixed4 shapeLight1Modulate = shapeLight1 * _ShapeLightBlendFactors1.x;
    fixed4 shapeLight1Additive = shapeLight1 * _ShapeLightBlendFactors1.y;
#else
    fixed4 shapeLight1Modulate = 0;
    fixed4 shapeLight1Additive = 0;
#endif

#if USE_SHAPE_LIGHT_TYPE_2
    fixed4 shapeLight2 = tex2D(_ShapeLightTexture2, i.lightingUV);

    if (any(_ShapeLightMaskFilter2))
        shapeLight2 *= dot(mask, _ShapeLightMaskFilter2);

    fixed4 shapeLight2Modulate = shapeLight2 * _ShapeLightBlendFactors2.x;
    fixed4 shapeLight2Additive = shapeLight2 * _ShapeLightBlendFactors2.y;
#else
    fixed4 shapeLight2Modulate = 0;
    fixed4 shapeLight2Additive = 0;
#endif

#if USE_POINT_LIGHTS
    float2 lightingUV = i.lightingUV;
    lightingUV.y = 1.0 - lightingUV.y;
    fixed4 pointLight = tex2D(_PointLightingTex, lightingUV);
#else
    fixed4 pointLight = 0;
#endif

    fixed4 finalOutput;
    fixed4 finalModulate = shapeLight0Modulate + shapeLight1Modulate + shapeLight2Modulate + pointLight;
    fixed4 finalAdditve = shapeLight0Additive + shapeLight1Additive + shapeLight2Additive;
    finalOutput = main * finalModulate + finalAdditve;

    finalOutput.a = main.a;
    return finalOutput;
}

#endif
