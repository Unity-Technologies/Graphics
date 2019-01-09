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
	fixed2 lightScreenPos : TEXCOORD2;
	float4 vertexWorldPos : TEXCOORD3;
	fixed2 pixelScreenPos : TEXCOORD4;

	#if USE_POINT_LIGHT_COOKIES
		fixed2 cookieUV : TEXCOORD5;
	#endif
};

uniform sampler2D _MaskTex;
uniform fixed4 _MaskTex_ST;
uniform sampler2D _NormalMap;
uniform fixed4 _NormalMap_ST;

uniform sampler2D _ShapeLightTexture0;
uniform sampler2D _ShapeLightTexture1;
uniform sampler2D _ShapeLightTexture2;

uniform sampler2D _PointLightingTex;
uniform sampler2D _PointLightCookieTex;
uniform sampler2D _MainTex;
uniform fixed4 _MainTex_ST;

uniform float4 _PointLightPosition;
uniform float4 _PointLightColor;
uniform fixed  _PointLightOuterRadius;
uniform fixed  _PointLightInnerRadius;
uniform fixed4 _PointLightUp;

uniform float  _PointLightInnerAngle;
uniform float  _PointLightOuterAngle;
uniform float4 _PointLightForward;
uniform float4 _PointLightOrigin;

uniform float  _LightIntensityScale;

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

	o.lightScreenPos = ComputeScreenPos(_PointLightPosition);
	o.color = v.color;

	o.vertexWorldPos = mul(unity_ObjectToWorld, v.vertex);

	#if USE_POINT_LIGHT_COOKIES
		o.cookieUV = saturate(abs(o.pixelScreenPos - o.lightScreenPos) / _PointLightOuterRadius);
	#endif

	return o;
}

fixed4 CombinedShapeLightFragment(v2f i) : SV_Target
{
	fixed4 main = i.color * tex2D(_MainTex, i.uv);
	fixed4 mask = tex2D(_MaskTex, i.uv);									// Mask Order (For RGBA) - Specular, Rim, Ambient Occlusion

	fixed4 specular = 0;
	#if USE_SHAPE_LIGHT_TYPE_0
		specular = tex2D(_ShapeLightTexture0, i.lightingUV) * _LightIntensityScale;
	#else
		specular = 0;
	#endif

	fixed4 ambientColor;
	#if USE_SHAPE_LIGHT_TYPE_1
		ambientColor = tex2D(_ShapeLightTexture1, i.lightingUV) * mask.b * _LightIntensityScale;  // mask.b is the ambient occlusion channel
	#else
		ambientColor = 0;
	#endif
	
	fixed4 rimColor;
	#if USE_SHAPE_LIGHT_TYPE_2
		rimColor = tex2D(_ShapeLightTexture2, i.lightingUV) * _LightIntensityScale;
	#else
		rimColor = 0;
	#endif

	fixed3 pointLightColor;
	#if USE_POINT_LIGHTS
        float2 lightingUV = i.lightingUV;
        lightingUV.y = 1.0 - lightingUV.y;
		pointLightColor = tex2D(_PointLightingTex, lightingUV) *  _LightIntensityScale;
	#else
		pointLightColor = 0;
	#endif

	// Diffuse calculation
	fixed3 diffuseColor = main.rgb * (specular.rgb + pointLightColor + ambientColor.rgb);

	// Specular calculation
	fixed3 appliedSpecularColor;
	#if USE_SHAPE_LIGHT_TYPE_0
		appliedSpecularColor = (mask.r * (specular.rgb + pointLightColor)) + diffuseColor.rgb;  // mask.r is the specular channel
	#else
		#if USE_POINT_LIGHTS
			appliedSpecularColor = (mask.r * pointLightColor) + diffuseColor.rgb;
		#else
			appliedSpecularColor = diffuseColor.rgb;
		#endif
	#endif

	// Rim calculation
	fixed3 appliedRimColor;
	#if USE_SHAPE_LIGHT_TYPE_2
		appliedRimColor = mask.g * rimColor.rgb + appliedSpecularColor;  // mask.g is the rim channel
	#else
		appliedRimColor = appliedSpecularColor;
	#endif

	fixed4 finalOutput;
	finalOutput.rgb = appliedRimColor;
	finalOutput.a = main.a;
	return finalOutput;
}

#endif
