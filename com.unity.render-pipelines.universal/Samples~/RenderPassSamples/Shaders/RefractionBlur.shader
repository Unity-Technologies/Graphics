// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

// Similar to regular FX/Glass/Stained BumpDistort shader
// from standard Effects package, just without grab pass,
// and samples a texture with a different name.

Shader "FX/Glass/Stained BumpDistort (no grab)" {
	Properties{
		_BumpAmt("Distortion", range(0,64)) = 10
		_TintAmt("Tint Amount", Range(0,1)) = 0.1
		_MainTex("Tint Color (RGB)", 2D) = "white" {}
		_BumpMap("Normalmap", 2D) = "bump" {}
	}

		Category{

		// We must be transparent, so other objects are drawn before this one.
		Tags { "Queue" = "Transparent" "RenderType" = "Opaque" }

		SubShader {

			Pass {
				Name "BASE"
				Tags { "LightMode" = "Always" }

	CGPROGRAM
	#pragma vertex vert
	#pragma fragment frag
	#pragma multi_compile_fog
	#include "UnityCG.cginc"

	struct appdata_t {
		float4 vertex : POSITION;
		float2 texcoord: TEXCOORD0;
	};

	struct v2f {
		float4 vertex : POSITION;
		float4 uvgrab : TEXCOORD0;
		float2 uvbump : TEXCOORD1;
		float2 uvmain : TEXCOORD2;
		UNITY_FOG_COORDS(3)
	};

	float _BumpAmt;
	half _TintAmt;
	float4 _BumpMap;
	float4 _MainTex;

	v2f vert(appdata_t v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		#if UNITY_UV_STARTS_AT_TOP
		float scale = -1.0;
		#else
		float scale = 1.0;
		#endif
		o.uvgrab.xy = (float2(o.vertex.x, o.vertex.y * scale) + o.vertex.w) * 0.5;
		o.uvgrab.zw = o.vertex.zw;
		o.uvbump = TRANSFORM_TEX(v.texcoord, _BumpMap);
		o.uvmain = TRANSFORM_TEX(v.texcoord, _MainTex);
		UNITY_TRANSFER_FOG(o,o.vertex);
		return o;
	}

	sampler2D _GrabBlurTexture;
	float4 _GrabBlurTexture_TexelSize;
	sampler2D _BumpMap;
	sampler2D _MainTex;

	half4 frag(v2f i) : SV_Target
	{
		// calculate perturbed coordinates
		// we could optimize this by just reading the x & y without reconstructing the Z
		half2 bump = UnpackNormal(tex2D(_BumpMap, i.uvbump)).rg;
		float2 offset = bump * _BumpAmt * _GrabBlurTexture_TexelSize.xy;
		i.uvgrab.xy = offset * i.uvgrab.z + i.uvgrab.xy;

		half4 col = tex2Dproj(_GrabBlurTexture, UNITY_PROJ_COORD(i.uvgrab));
		half4 tint = tex2D(_MainTex, i.uvmain);
		col = lerp(col, tint, _TintAmt);
		UNITY_APPLY_FOG(i.fogCoord, col);
		return col;
	}
	ENDCG
			}
		}

	}

}
