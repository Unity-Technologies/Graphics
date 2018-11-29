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

uniform float _SpecularMultiplier;
uniform float _AmbientMultiplier;
uniform float _RimMultiplier;

uniform sampler2D _SpecularLightingTex;
uniform sampler2D _AmbientLightingTex;
uniform sampler2D _RimLightingTex;
uniform sampler2D _PointLightingTex;
uniform sampler2D _MainTex;
uniform sampler2D _PointLightCookieTex;
uniform fixed4 _MainTex_ST;
uniform fixed4 _AmbientColor;
uniform fixed4 _RimColor;

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

	fixed4 shapeLight = 0;
	#if USE_SPECULAR_TEXTURE
		shapeLight = tex2D(_SpecularLightingTex, i.lightingUV);
	#else
		shapeLight.rgb = 0;
		shapeLight.a = 0;
	#endif
	shapeLight = shapeLight * _LightIntensityScale;

	fixed maxValue = max(shapeLight.r, max(shapeLight.g, shapeLight.b));
	fixed3 shapeLightClampColor = (shapeLight.rgb / maxValue);

	fixed4 ambientColor;
	#if USE_AMBIENT_TEXTURE
		ambientColor = tex2D(_AmbientLightingTex, i.lightingUV);
	#else
		ambientColor = _AmbientColor;
	#endif
	ambientColor = _AmbientMultiplier * ambientColor * mask.b * _LightIntensityScale; // mask is ambient occulusion


	fixed4 rimColor;
	#if USE_RIM_TEXTURE
		rimColor = tex2D(_RimLightingTex, i.lightingUV);
	#else
		rimColor = _RimColor;
	#endif
	rimColor = _RimMultiplier * rimColor * _LightIntensityScale;

	fixed3 pointLightColor = tex2D(_PointLightingTex, i.lightingUV) *  _LightIntensityScale;
	//float pointMaxValue = max(pointLightColor.r, max(pointLightColor.g, pointLightColor.b));
	//fixed3 pointLightClampColor = (_PointLightColor.rgb / pointMaxValue);
	fixed3 pointLightClampColor = fixed3(1, 1, 1);

	// Diffuse calculation
	fixed3 diffuseAmbientLight = ambientColor.rgb * main.rgb;
	fixed3 diffuseShapeLight = clamp(shapeLight.rgb * shapeLight.a * main.rgb, 0, shapeLightClampColor);  // Clamp is pretty expensive
	fixed3 diffusePointLight = clamp(pointLightColor * main.rgb, 0, pointLightClampColor);   // Clamp is pretty expensive
	fixed3 diffuseColor = diffuseShapeLight + diffusePointLight + diffuseAmbientLight;

	// Specular calculation
	fixed specularAlpha = mask.r;
	fixed3 specularShapeLight = clamp(shapeLight.a * shapeLight.rgb, 0, shapeLightClampColor);
	fixed3 specularPointLight = clamp(pointLightColor, 0, pointLightClampColor);
	fixed3 specularColor = (specularAlpha * _SpecularMultiplier * (specularShapeLight + specularPointLight)) + diffuseColor.rgb;

	fixed rimAlpha = rimColor.a * mask.g;
	fixed3 appliedRimColor = rimAlpha * rimColor.rgb + specularColor;

	fixed4 finalOutput;
	finalOutput.rgb = appliedRimColor;
	finalOutput.a = main.a;
	return finalOutput;
}

#endif