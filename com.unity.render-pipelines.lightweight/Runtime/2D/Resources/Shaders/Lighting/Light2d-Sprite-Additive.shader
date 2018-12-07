Shader "Hidden/Light2D-Sprite-Additive"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Tags{ "Queue" = "Transparent" "RenderType" = "Transparent" }
		Blend One One
		BlendOp Add, Max
		ZWrite Off
		Cull Off


		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma enable_d3d11_debug_symbols
			
			#include "UnityCG.cginc"

			struct appdata
			{
				float4 vertex : POSITION;
				half4 color : COLOR;
				half2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				half4 color : COLOR;
				half2 uv : TEXCOORD0;

				//half2 shadowUV : TEXCOORD1;
			};

			uniform sampler2D _MainTex;
			uniform float4 _MainTex_ST;
			uniform sampler2D _ShadowTex;
			uniform float	_InverseLightIntensityScale;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				//o.shadowUV.x = (o.vertex.x + 1) / 2;
				//o.shadowUV.y = 1-((o.vertex.y + 1) / 2);
				o.color = v.color;
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				//fixed4 shadow = tex2D(_ShadowTex, i.shadowUV);
				fixed4 col = tex2D(_MainTex, i.uv);
				col = i.color * i.color.a * col * col.a * _InverseLightIntensityScale;
				col.a = 1;
				////fixed4 finalCol = (1-shadow.a) * col + shadow.a * shadow;
				//fixed4 finalCol = col;
				//col.a = 1;
				//return finalCol;
				return col;
			}
			ENDCG
		}
	}
}
